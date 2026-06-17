using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// Localize 组件 key 字段的共用绘制器。
    /// 封装「key 下拉选择 + 手动输入 + 存在性校验 + 默认语言预览」逻辑，
    /// 供所有 Localize* 子类的 CustomEditor 复用，避免每个 Drawer 重复实现。
    /// </summary>
    public static class LocalizeKeyFieldDrawer
    {
        /// <summary>
        /// 绘制一个可搜索的 key 选择器，并支持手动输入。
        /// 修改后通过 SerializedProperty 回写（保证多选编辑与 Undo 正常）。
        /// </summary>
        /// <param name="keyProp">绑定的 key 序列化属性（如 localizationKey / fontKey）</param>
        /// <param name="availableKeys">可选 key 列表（已排序）</param>
        /// <param name="label">字段标签</param>
        public static void DrawKeySelector(SerializedProperty keyProp, string[] availableKeys, string label)
        {
            if (keyProp == null) return;
            string current = keyProp.stringValue;

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                // 下拉选择（当前值在列表中的索引）
                int currentIndex = System.Array.IndexOf(availableKeys, current);
                int newIndex = EditorGUILayout.Popup(currentIndex >= 0 ? currentIndex : 0, availableKeys);
                if (newIndex >= 0 && newIndex < availableKeys.Length
                    && (currentIndex < 0 || newIndex != currentIndex))
                {
                    string selected = availableKeys[newIndex];
                    // 跳过占位项 "(无可用 key)"
                    if (selected != current && !selected.StartsWith("("))
                    {
                        keyProp.stringValue = selected;
                    }
                }

                // 手动输入/编辑
                string manual = EditorGUILayout.TextField(current);
                if (manual != current)
                {
                    keyProp.stringValue = manual;
                }
            }

            // 存在性校验
            if (!string.IsNullOrEmpty(current) && availableKeys.Length > 0 && !availableKeys[0].StartsWith("("))
            {
                if (System.Array.IndexOf(availableKeys, current) < 0)
                {
                    EditorGUILayout.HelpBox($"⚠ key \"{current}\" 不在任何语言文件中，运行时将显示 key 本身或回退。", MessageType.Warning);
                }
            }
        }

        /// <summary>获取所有可选 key（从磁盘加载并排序）。供 Drawer 在 OnEnable/OnInspectorGUI 调用。</summary>
        public static string[] LoadAvailableKeys()
        {
            var locales = LocalizationEditorData.LoadAllLocales();
            var keys = LocalizationEditorData.CollectAllKeys(locales);
            keys.Sort();
            return keys.Count > 0 ? keys.ToArray() : new[] { "(无可用 key)" };
        }

        /// <summary>获取某 key 在默认语言中的预览文本（用于 Inspector 显示）。</summary>
        public static string GetPreview(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            var locales = LocalizationEditorData.LoadAllLocales();
            // 优先取第一个语言（默认约定第一个为默认语言或由 Settings 决定）
            foreach (var locale in locales)
            {
                if (locale?.Entries != null && locale.Entries.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
                {
                    return v;
                }
            }
            return "";
        }
    }
}
