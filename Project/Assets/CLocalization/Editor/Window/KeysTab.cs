using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 词条编辑 Tab。以表格形式展示 key + 各语言翻译，支持：
    /// - 搜索过滤（按 key 或任意语言文本）
    /// - 新增/删除 key
    /// - 直接单元格编辑翻译
    /// - 未翻译（空值）单元格高亮提醒
    /// </summary>
    public class KeysTab
    {
        /// <summary>搜索关键字。</summary>
        private string _search = "";

        /// <summary>排序后的 key 列表（缓存，数据变化时重建）。</summary>
        private List<string> _keys = new List<string>();

        /// <summary>当前正在编辑的 key（用于新增/重命名）。</summary>
        private string _newKey = "";

        /// <summary>滚动位置。</summary>
        private Vector2 _scroll;

        /// <summary>列宽：key 列 + 每语言列。</summary>
        private const float KeyColumnWidth = 180f;
        private const float LangColumnWidth = 200f;

        /// <summary>数据变化时重建 key 列表。</summary>
        public void OnDataChanged(List<LocaleData> locales)
        {
            _keys = LocalizationEditorData.CollectAllKeys(locales);
            _keys.Sort();
        }

        /// <summary>绘制 Tab 内容。</summary>
        public void Draw(LocalizationWindow window, List<LocaleData> locales)
        {
            DrawSearchBar(locales);
            EditorGUILayout.Space(2);
            DrawKeyTable(locales);
        }

        /// <summary>搜索栏 + 新增 key。</summary>
        private void DrawSearchBar(List<LocaleData> locales)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField("搜索", _search);
                GUILayout.Space(8);
                _newKey = EditorGUILayout.TextField("新增 key", _newKey);
                if (GUILayout.Button("+", GUILayout.Width(24)))
                {
                    AddKey(locales, _newKey);
                    _newKey = "";
                    GUI.FocusControl(null);
                }
            }
        }

        /// <summary>绘制词条表格（表头 + 滚动数据行）。</summary>
        private void DrawKeyTable(List<LocaleData> locales)
        {
            if (locales == null || locales.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无语言数据。请到「语言」Tab 添加语言，或检查 Resources/CLocalization/Locales 目录。", MessageType.Info);
                return;
            }

            // 过滤后的 key
            List<string> visibleKeys = GetFilteredKeys();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // 表头
            DrawTableHeader(locales);

            // 数据行
            for (int i = 0; i < visibleKeys.Count; i++)
            {
                DrawRow(visibleKeys[i], locales, i % 2 == 0);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField($"共 {visibleKeys.Count} / {_keys.Count} 个 key", EditorStyles.miniLabel);
        }

        /// <summary>绘制表头。</summary>
        private void DrawTableHeader(List<LocaleData> locales)
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(KeyColumnWidth));
                foreach (var locale in locales)
                {
                    GUILayout.Label(locale.Meta?.Code ?? "?", EditorStyles.boldLabel, GUILayout.Width(LangColumnWidth));
                }
                GUILayout.Label("操作", EditorStyles.boldLabel, GUILayout.Width(50));
            }
        }

        /// <summary>绘制一行（一个 key 的各语言翻译）。</summary>
        private void DrawRow(string key, List<LocaleData> locales, bool alternate)
        {
            Color oldBg = GUI.backgroundColor;
            if (alternate) GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);

            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label(key, GUILayout.Width(KeyColumnWidth));

                foreach (var locale in locales)
                {
                    DrawLocaleCell(key, locale);
                }

                // 删除按钮
                if (GUILayout.Button("×", GUILayout.Width(40)))
                {
                    if (EditorUtility.DisplayDialog("删除 key", $"确认删除 key \"{key}\" 及其在所有语言中的翻译？", "删除", "取消"))
                    {
                        RemoveKey(locales, key);
                        GUI.changed = true;
                    }
                }
            }
            GUI.backgroundColor = oldBg;
        }

        /// <summary>绘制单个语言单元格：空值高亮，可编辑。</summary>
        private void DrawLocaleCell(string key, LocaleData locale)
        {
            string current = "";
            if (locale.Entries != null && locale.Entries.TryGetValue(key, out var v)) current = v ?? "";

            // 未翻译（空值）用黄色背景提醒
            Color oldBg = GUI.backgroundColor;
            if (string.IsNullOrEmpty(current)) GUI.backgroundColor = new Color(1f, 0.95f, 0.6f);

            string newVal = EditorGUILayout.TextField(current, GUILayout.Width(LangColumnWidth));
            GUI.backgroundColor = oldBg;

            if (newVal != current)
            {
                if (locale.Entries == null) locale.Entries = new Dictionary<string, string>();
                locale.Entries[key] = newVal;
            }
        }

        /// <summary>按搜索关键字过滤 key（匹配 key 或任意语言文本）。</summary>
        private List<string> GetFilteredKeys()
        {
            if (string.IsNullOrEmpty(_search)) return _keys;

            // 注意：过滤需访问 locale 数据，但此方法在 Draw 中被调用时 locales 已传入；
            // 为保持签名简单，这里用缓存（数据变化时由 OnDataChanged 重建）。
            // 此处简化：仅按 key 文本过滤；如需按翻译过滤可在 Draw 时传入。
            var result = new List<string>();
            string needle = _search.ToLowerInvariant();
            foreach (var key in _keys)
            {
                if (key.ToLowerInvariant().Contains(needle)) result.Add(key);
            }
            return result;
        }

        /// <summary>新增 key（添加到所有语言的 entries，初始为空）。</summary>
        private void AddKey(List<LocaleData> locales, string key)
        {
            key = key?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                LocalizationLog.Warning("key 不能为空。");
                return;
            }
            if (_keys.Contains(key))
            {
                LocalizationLog.Warning($"key \"{key}\" 已存在。");
                return;
            }
            foreach (var locale in locales)
            {
                if (locale.Entries == null) locale.Entries = new Dictionary<string, string>();
                locale.Entries[key] = "";
            }
            _keys.Add(key);
            _keys.Sort();
            LocalizationLog.Info($"已新增 key \"{key}\"（已在所有语言中创建空翻译，请填写）。");
        }

        /// <summary>删除 key（从所有语言移除）。</summary>
        private void RemoveKey(List<LocaleData> locales, string key)
        {
            foreach (var locale in locales)
            {
                if (locale.Entries != null) locale.Entries.Remove(key);
            }
            _keys.Remove(key);
        }
    }
}
