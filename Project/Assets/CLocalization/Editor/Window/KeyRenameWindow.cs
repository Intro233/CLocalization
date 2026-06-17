using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// Key 重命名重构窗口。菜单：Tools/CLocalization/Rename Key。
    /// 功能：把一个旧 key 重命名为新 key，同时：
    ///   1) 在所有语言的 JSON 中迁移该 key（保留翻译值）
    ///   2) 批量更新所有 Prefab/Scene 中 Localize 组件引用该 key 的字段
    /// 所有引用更新支持 Undo（Ctrl+Z 可回滚）。
    ///
    /// 注意：源码中的 Localization.Get("旧key") 字面量无法自动重构，需手动改。
    /// </summary>
    public class KeyRenameWindow : EditorWindow
    {
        /// <summary>可选 key 列表（来自所有 locale 的 key 并集）。</summary>
        private string[] _allKeys;
        /// <summary>当前选中的旧 key 索引。</summary>
        private int _selectedKeyIndex;
        /// <summary>新 key 名（默认与旧 key 相同，用户修改）。</summary>
        private string _newKey = "";
        /// <summary>扫描得到的引用（旧 key → 引用列表），用于预览受影响范围。</summary>
        private Dictionary<string, List<LocalizationReferenceScanner.KeyReference>> _refs;
        /// <summary>引用列表滚动。</summary>
        private Vector2 _scroll;

        [MenuItem("Tools/CLocalization/Rename Key", priority = 5)]
        public static void Open()
        {
            var window = GetWindow<KeyRenameWindow>("重命名 Key");
            window.minSize = new Vector2(480, 420);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshKeys();
            _refs = LocalizationReferenceScanner.ScanAllReferences();
        }

        /// <summary>刷新可选 key 列表。</summary>
        private void RefreshKeys()
        {
            var locales = LocalizationEditorData.LoadAllLocales();
            var keys = LocalizationEditorData.CollectAllKeys(locales);
            keys.Sort();
            _allKeys = keys.Count > 0 ? keys.ToArray() : new[] { "(无 key)" };
            if (_selectedKeyIndex >= _allKeys.Length) _selectedKeyIndex = 0;
            _newKey = _allKeys.Length > 0 ? _allKeys[_selectedKeyIndex] : "";
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Key 重命名重构", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "选择一个 key 并输入新名称。重命名会：\n" +
                "1) 迁移所有语言 JSON 中的该 key（保留翻译值）\n" +
                "2) 更新所有 Prefab/Scene 中引用该 key 的 Localize 组件\n\n" +
                "源码 Localization.Get(\"旧key\") 字面量无法自动重构，需手动修改。",
                MessageType.Info);

            EditorGUILayout.Space(8);

            // 旧 key 选择
            EditorGUI.BeginChangeCheck();
            _selectedKeyIndex = EditorGUILayout.Popup("选择旧 Key", _selectedKeyIndex, _allKeys);
            if (EditorGUI.EndChangeCheck())
            {
                _newKey = _allKeys[_selectedKeyIndex];
            }

            // 新 key 输入
            _newKey = EditorGUILayout.TextField("新 Key 名称", _newKey);

            // 显示引用预览
            string oldKey = _allKeys.Length > 0 ? _allKeys[_selectedKeyIndex] : "";
            int refCount = (_refs != null && _refs.ContainsKey(oldKey)) ? _refs[oldKey].Count : 0;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"受影响引用: {refCount} 处", EditorStyles.miniBoldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(120));
            if (refCount > 0 && _refs.ContainsKey(oldKey))
            {
                foreach (var r in _refs[oldKey])
                {
                    EditorGUILayout.LabelField("• " + r.ToString(), EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField("（无组件字段引用，仅迁移 JSON）", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);

            // 校验
            bool canRename = !string.IsNullOrEmpty(_newKey)
                             && _newKey != oldKey
                             && !_allKeys.Length.Equals(0);

            using (new EditorGUI.DisabledScope(!canRename))
            {
                if (GUILayout.Button("执行重命名", GUILayout.Height(30)))
                {
                    ConfirmAndRename(oldKey, _newKey, refCount);
                }
            }

            if (!canRename && !string.IsNullOrEmpty(oldKey))
            {
                EditorGUILayout.HelpBox("新 key 不能为空，且不能与旧 key 相同。", MessageType.Warning);
            }
        }

        /// <summary>确认并执行重命名。</summary>
        private void Confirm(string oldKey, string newKey, int refCount)
        {
            bool ok = EditorUtility.DisplayDialog("确认重命名",
                $"将 key \"{oldKey}\" 重命名为 \"{newKey}\"？\n" +
                $"将迁移所有语言 JSON，并更新 {refCount} 处组件引用。\n" +
                $"（可通过 Ctrl+Z 撤销组件引用修改）",
                "重命名", "取消");
            if (!ok) return;

            DoRename(oldKey, newKey);
        }

        private void ConfirmAndRename(string oldKey, string newKey, int refCount)
        {
            Confirm(oldKey, newKey, refCount);
        }

        /// <summary>实际执行：迁移 JSON key + 更新组件引用。</summary>
        private void DoRename(string oldKey, string newKey)
        {
            // 1) 迁移所有语言 JSON
            var locales = LocalizationEditorData.LoadAllLocales();
            int migratedLocales = 0;
            foreach (var locale in locales)
            {
                if (locale.Entries == null) continue;
                if (locale.Entries.TryGetValue(oldKey, out var value))
                {
                    locale.Entries.Remove(oldKey);
                    // 若新 key 已存在则覆盖（合并）
                    locale.Entries[newKey] = value;
                    migratedLocales++;
                }
            }
            LocalizationEditorData.SaveAllLocales(locales);

            // 2) 更新所有组件引用（带 Undo）
            int updatedRefs = RenameComponentReferences(oldKey, newKey);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LocalizationLog.Info($"Key 重命名完成: \"{oldKey}\" → \"{newKey}\"，迁移 {migratedLocales} 个语言 JSON，更新 {updatedRefs} 处组件引用。");
            EditorUtility.DisplayDialog("重命名完成",
                $"已迁移 {migratedLocales} 个语言 JSON，更新 {updatedRefs} 处组件引用。\n\n请检查源码中的 Localization.Get(\"{oldKey}\") 字面量并手动修改。",
                "确定");

            // 刷新窗口
            RefreshKeys();
            _refs = LocalizationReferenceScanner.ScanAllReferences();
            Repaint();
        }

        /// <summary>批量更新所有 Prefab/Scene 中引用 oldKey 的 Localize 组件字段为 newKey。</summary>
        /// <returns>实际更新的字段数。</returns>
        private int RenameComponentReferences(string oldKey, string newKey)
        {
            int updated = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab t:Scene");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                bool changed = false;
                foreach (var obj in assets)
                {
                    if (!(obj is LocalizeBase comp)) continue;
                    using (var so = new SerializedObject(comp))
                    {
                        changed |= RenameField(so, "localizationKey", oldKey, newKey, ref updated);
                        changed |= RenameField(so, "fontKey", oldKey, newKey, ref updated);
                    }
                }
                if (changed)
                {
                    EditorUtility.SetDirty(AssetDatabase.LoadMainAssetAtPath(path));
                }
            }
            return updated;
        }

        /// <summary>重命名单个字段（若其值等于 oldKey）。返回是否发生修改。</summary>
        private bool RenameField(SerializedObject so, string fieldName, string oldKey, string newKey, ref int updated)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null) return false;
            if (prop.stringValue == oldKey)
            {
                // Undo 登记（对组件所在资源）
                prop.serializedObject.Update();
                prop.stringValue = newKey;
                prop.serializedObject.ApplyModifiedProperties();
                updated++;
                return true;
            }
            return false;
        }
    }
}
