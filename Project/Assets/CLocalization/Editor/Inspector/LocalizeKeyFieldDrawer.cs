using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// Localize 组件 key 字段的共用绘制器。
    /// 封装「key 弹出搜索选择 + 手动输入 + 存在性校验 + 可切换预览语言」逻辑，
    /// 供所有 Localize* 子类的 CustomEditor 复用，避免每个 Drawer 重复实现。
    /// </summary>
    public static class LocalizeKeyFieldDrawer
    {
        /// <summary>当前预览语言代码（跨所有 Drawer 共享，用户切换一次后所有 Inspector 沿用）。</summary>
        private static string _previewLanguageCode;

        /// <summary>当前预览语言代码（公开访问）。</summary>
        public static string PreviewLanguageCode => _previewLanguageCode;

        /// <summary>
        /// 绘制 key 字段：手动输入框 + 「选择...」按钮（点击弹出搜索窗口）+ 存在性校验。
        /// </summary>
        /// <param name="keyProp">绑定的 key 序列化属性</param>
        /// <param name="availableKeys">可选 key 列表（已排序）</param>
        /// <param name="label">字段标签</param>
        /// <param name="onKeyChanged">key 改变后的回调（可选，用于触发 Repaint 等）</param>
        public static void DrawKeySelector(SerializedProperty keyProp, string[] availableKeys, string label,
            System.Action onKeyChanged = null)
        {
            if (keyProp == null) return;
            string current = keyProp.stringValue;

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                // 手动输入/编辑（主要交互入口，可直接粘贴 key）
                string manual = EditorGUILayout.TextField(current);
                if (manual != current)
                {
                    keyProp.stringValue = manual;
                    onKeyChanged?.Invoke();
                }

                // 「选择...」按钮：弹出搜索窗口（key 多时快速选取）
                if (GUILayout.Button("选择...", GUILayout.Width(60)))
                {
                    KeyPickerWindow.Pick(availableKeys, current, selected =>
                    {
                        keyProp.stringValue = selected;
                        keyProp.serializedObject.ApplyModifiedProperties();
                        onKeyChanged?.Invoke();
                    });
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

        /// <summary>
        /// 绘制预览区：预览语言下拉 + 该 key 在所选语言中的实际文本（应用插值参数）。
        /// </summary>
        /// <param name="key">要预览的 key</param>
        /// <param name="formatArgs">插值参数（位置占位 {0}），可为 null</param>
        public static void DrawPreview(string key, string[] formatArgs = null)
        {
            if (string.IsNullOrEmpty(key)) return;

            var locales = LocalizationEditorData.LoadAllLocales();
            if (locales == null || locales.Count == 0) return;

            // 预览语言下拉（跨 Drawer 共享选择）
            EnsurePreviewLanguage(locales);
            string[] codes = GetLanguageCodes(locales);
            int currentIdx = System.Array.IndexOf(codes, _previewLanguageCode);
            if (currentIdx < 0) currentIdx = 0;

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("预览语言", GUILayout.Width(60));
                int newIdx = EditorGUILayout.Popup(currentIdx, codes, GUILayout.Width(120));
                if (newIdx != currentIdx)
                {
                    _previewLanguageCode = codes[newIdx];
                }
                GUILayout.FlexibleSpace();
            }

            // 取该 key 在预览语言中的文本
            LocaleData locale = FindLocale(locales, _previewLanguageCode);
            if (locale == null || locale.Entries == null) return;
            if (!locale.Entries.TryGetValue(key, out var template) || string.IsNullOrEmpty(template))
            {
                EditorGUILayout.HelpBox($"该 key 在 {_previewLanguageCode} 中无翻译（空或缺失）。", MessageType.Info);
                return;
            }

            // 应用插值参数（位置占位 {0}）
            string preview = template;
            if (formatArgs != null && formatArgs.Length > 0)
            {
                try
                {
                    preview = string.Format(preview, formatArgs);
                }
                catch
                {
                    // 插值失败保留原模板
                }
            }

            EditorGUILayout.LabelField($"预览（{_previewLanguageCode}）", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(preview, MessageType.None);
        }

        /// <summary>获取所有可选 key（从磁盘加载并排序）。</summary>
        public static string[] LoadAvailableKeys()
        {
            var locales = LocalizationEditorData.LoadAllLocales();
            var keys = LocalizationEditorData.CollectAllKeys(locales);
            keys.Sort();
            return keys.Count > 0 ? keys.ToArray() : new[] { "(无可用 key)" };
        }

        /// <summary>确保预览语言已初始化（首次默认取第一个语言）。</summary>
        private static void EnsurePreviewLanguage(List<LocaleData> locales)
        {
            if (!string.IsNullOrEmpty(_previewLanguageCode)) return;
            if (locales.Count > 0 && locales[0]?.Meta != null)
            {
                _previewLanguageCode = locales[0].Meta.Code;
            }
        }

        /// <summary>从 locales 提取语言代码数组（用于下拉）。</summary>
        private static string[] GetLanguageCodes(List<LocaleData> locales)
        {
            var codes = new string[locales.Count];
            for (int i = 0; i < locales.Count; i++)
            {
                codes[i] = locales[i]?.Meta?.Code ?? "?";
            }
            return codes;
        }

        /// <summary>按语言代码查找 locale。</summary>
        private static LocaleData FindLocale(List<LocaleData> locales, string code)
        {
            foreach (var l in locales)
            {
                if (l?.Meta?.Code == code) return l;
            }
            return null;
        }
    }
}
