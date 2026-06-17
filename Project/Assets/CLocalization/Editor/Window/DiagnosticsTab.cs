using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 诊断 Tab。检测并展示：
    /// - 缺失翻译：某 key 在部分语言存在，在另一些语言缺失（空值）。
    /// - 重复 key：理论上 JSON 内不会重复（Dictionary 自动去重），但用于跨文件一致性校验。
    /// 帮助译者快速定位未完成的工作。
    /// </summary>
    public class DiagnosticsTab
    {
        /// <summary>诊断结果缓存。</summary>
        private DiagnosticsResult _result;
        /// <summary>滚动位置。</summary>
        private Vector2 _scroll;

        public void OnDataChanged(List<LocaleData> locales)
        {
            // 数据变化后清除缓存，下次 Draw 时重新计算
            _result = null;
        }

        public void Draw(LocalizationWindow window, List<LocaleData> locales)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("诊断", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("运行检测", GUILayout.Width(100)))
                {
                    _result = RunDiagnostics(locales);
                }
            }
            EditorGUILayout.HelpBox(
                "检测各语言的翻译完整性：列出在某语言中缺失（空值）的 key，以及各语言词条数对比。",
                MessageType.Info);

            if (_result == null)
            {
                EditorGUILayout.LabelField("点击「运行检测」开始。", EditorStyles.miniLabel);
                return;
            }

            DrawSummary(locales);
            EditorGUILayout.Space(8);
            DrawMissingDetail();
        }

        /// <summary>绘制汇总信息：各语言词条数。</summary>
        private void DrawSummary(List<LocaleData> locales)
        {
            EditorGUILayout.LabelField("语言概况", EditorStyles.boldLabel);
            foreach (var locale in locales)
            {
                int count = locale.Entries?.Count ?? 0;
                int filled = 0;
                if (locale.Entries != null)
                {
                    foreach (var kv in locale.Entries)
                    {
                        if (!string.IsNullOrEmpty(kv.Value)) filled++;
                    }
                }
                float ratio = count > 0 ? (float)filled / count : 0f;
                string bar = new string('█', Mathf.RoundToInt(ratio * 20));
                EditorGUILayout.LabelField($"{locale.Meta?.Code,-8}  {filled}/{count}  [{bar}]  ({ratio:P0})");
            }
        }

        /// <summary>绘制缺失翻译详情。</summary>
        private void DrawMissingDetail()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"缺失翻译明细 ({_result.MissingEntries.Count} 项)", EditorStyles.boldLabel);

            if (_result.MissingEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ 所有 key 在所有语言中均有翻译，无缺失。", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(180));
            foreach (var entry in _result.MissingEntries)
            {
                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField(entry.Key, GUILayout.Width(200));
                    GUILayout.Label("缺失于:", GUILayout.Width(60));
                    GUILayout.Label(string.Join(", ", entry.Value));
                    GUILayout.FlexibleSpace();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>执行检测：找出每个 key 缺失于哪些语言。</summary>
        private DiagnosticsResult RunDiagnostics(List<LocaleData> locales)
        {
            var result = new DiagnosticsResult();
            var allKeys = LocalizationEditorData.CollectAllKeys(locales);

            foreach (var key in allKeys)
            {
                var missingIn = new List<string>();
                foreach (var locale in locales)
                {
                    bool present = locale.Entries != null
                        && locale.Entries.TryGetValue(key, out var v)
                        && !string.IsNullOrEmpty(v);
                    if (!present)
                    {
                        missingIn.Add(locale.Meta?.Code ?? "?");
                    }
                }
                // 只在「部分语言缺失」时记录（全语言都缺失的视为草稿 key，不报）
                if (missingIn.Count > 0 && missingIn.Count < locales.Count)
                {
                    result.MissingEntries[key] = missingIn;
                }
            }
            return result;
        }

        /// <summary>诊断结果。</summary>
        private class DiagnosticsResult
        {
            /// <summary>key → 缺失它的语言代码列表。</summary>
            public Dictionary<string, List<string>> MissingEntries = new Dictionary<string, List<string>>();
        }
    }
}
