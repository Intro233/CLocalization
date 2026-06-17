using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 语言管理 Tab。展示当前所有语言，支持新增/删除语言。
    /// 新增语言会在内存创建空白 LocaleData（保存后写入磁盘 JSON）。
    /// 同时联动 Settings 资源的语言列表，保持同步。
    /// </summary>
    public class LanguagesTab
    {
        /// <summary>新增语言输入：代码。</summary>
        private string _newCode = "";
        /// <summary>新增语言输入：显示名。</summary>
        private string _newName = "";

        public void Draw(LocalizationWindow window, List<LocaleData> locales)
        {
            EditorGUILayout.LabelField("已有语言", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (locales.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无语言。在下方添加，或运行 Tools > CLocalization > Create Settings Asset 生成默认配置。", MessageType.Info);
            }

            for (int i = 0; i < locales.Count; i++)
            {
                var locale = locales[i];
                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField(locale.Meta?.Code ?? "?", GUILayout.Width(100));
                    EditorGUILayout.LabelField(locale.Meta?.DisplayName ?? "", GUILayout.Width(120));
                    int entryCount = locale.Entries?.Count ?? 0;
                    EditorGUILayout.LabelField($"{entryCount} 词条", GUILayout.Width(80));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("删除", GUILayout.Width(50)))
                    {
                        string code = locale.Meta?.Code;
                        if (EditorUtility.DisplayDialog("删除语言",
                            $"确认删除语言 \"{code}\"？\n\n该操作将从编辑器移除该语言，并在点击「保存全部」后删除磁盘上的 JSON 文件。\n（保存前可点「重新加载」撤销）",
                            "删除", "取消"))
                        {
                            // 仅从内存移除并记录待删 code，真正删盘在 SaveAll 时执行（延迟删盘，可撤销）
                            window.RemoveLanguage(code);
                            SyncSettings(window);
                        }
                    }
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("添加新语言", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _newCode = EditorGUILayout.TextField("语言代码", _newCode);
                _newName = EditorGUILayout.TextField("显示名称", _newName);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("添加语言", GUILayout.Width(120)))
                {
                    AddLanguage(window, locales);
                }
            }
            EditorGUILayout.HelpBox(
                "语言代码请用 BCP 47 风格，如 zh-CN、en-US、ja-JP、ko-KR、fr-FR。\n" +
                "新增语言会同步到 Settings 资源的语言列表（供运行时切换）。",
                MessageType.Info);
        }

        /// <summary>添加语言：创建内存 LocaleData + 同步 Settings。</summary>
        private void AddLanguage(LocalizationWindow window, List<LocaleData> locales)
        {
            string code = _newCode?.Trim();
            string name = string.IsNullOrEmpty(_newName) ? code : _newName.Trim();
            if (string.IsNullOrEmpty(code))
            {
                LocalizationLog.Warning("语言代码不能为空。");
                return;
            }
            window.AddLanguage(code, name);
            SyncSettings(window);
            _newCode = "";
            _newName = "";
            GUI.FocusControl(null);
        }

        /// <summary>同步 Settings 资源的语言列表（运行时切换需要 Settings 配置）。</summary>
        /// <remarks>基于窗口当前的内存 locale 列表重建 Settings，不从磁盘重新加载，避免清空待删集合。</remarks>
        private void SyncSettings(LocalizationWindow window)
        {
            var settings = LocalizationSetup.LoadOrCreateSettings();
            if (settings == null) return;

            // 使用窗口内存中的 locale 列表（已反映增删），而非从磁盘重新加载，
            // 否则待删的语言文件仍在磁盘上会被重新加载回来，且 Reload 会清空 _pendingDeleteCodes。
            var locales = window.GetCurrentLocales();
            var so = new SerializedObject(settings);
            var list = so.FindProperty("languages");
            if (list == null) return;
            list.arraySize = 0;
            foreach (var locale in locales)
            {
                if (locale?.Meta == null) continue;
                int idx = list.arraySize;
                list.arraySize = idx + 1;
                var element = list.GetArrayElementAtIndex(idx);
                var codeProp = element.FindPropertyRelative("languageCode");
                var nameProp = element.FindPropertyRelative("displayName");
                if (codeProp != null) codeProp.stringValue = locale.Meta.Code;
                if (nameProp != null) nameProp.stringValue = locale.Meta.DisplayName ?? locale.Meta.Code;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            // 仅刷新各 Tab 的 key 缓存，不做全量 Reload（避免清空待删集合）
            window.RefreshCaches();
        }
    }
}
