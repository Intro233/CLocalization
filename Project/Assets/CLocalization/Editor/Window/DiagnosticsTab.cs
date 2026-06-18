using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 诊断 Tab。检测并展示：
    /// - 缺失翻译：某 key 在部分语言存在，在另一些语言缺失（空值）。
    /// - 未使用 key：在 JSON 中存在，但没有任何 Prefab/Scene 组件引用、也没有源码字面量引用的 key。
    /// 帮助译者与开发者定位未完成的工作与可清理的废 key。
    /// </summary>
    public class DiagnosticsTab
    {
        /// <summary>诊断结果缓存。</summary>
        private DiagnosticsResult _result;
        /// <summary>缺失翻译滚动位置。</summary>
        private Vector2 _scroll;
        /// <summary>未使用 key 滚动位置。</summary>
        private Vector2 _unusedScroll;
        /// <summary>当前窗口引用（供跳转编辑调用 NavigateToKey）。</summary>
        private LocalizationWindow _window;

        public void OnDataChanged(List<LocaleData> locales)
        {
            // 数据变化后清除缓存，下次 Draw 时重新计算
            _result = null;
        }

        public void Draw(LocalizationWindow window, List<LocaleData> locales)
        {
            _window = window;
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
                "检测各语言的翻译完整性（缺失明细），以及未被任何组件/源码引用的「未使用 key」。\n" +
                "注意：未使用 key 检测基于静态扫描，动态拼接的 key 可能误报。",
                MessageType.Info);

            if (_result == null)
            {
                EditorGUILayout.LabelField("点击「运行检测」开始。", EditorStyles.miniLabel);
                return;
            }

            DrawSummary(locales);
            EditorGUILayout.Space(8);
            DrawMissingDetail();
            EditorGUILayout.Space(8);
            DrawUnusedKeys(locales);
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
                    // 跳转到词条 Tab 编辑该 key
                    if (GUILayout.Button("编辑", GUILayout.Width(50)))
                    {
                        _window?.NavigateToKey(entry.Key);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>绘制未使用 key 列表（JSON 中有但无组件/源码引用）。</summary>
        private void DrawUnusedKeys(List<LocaleData> locales)
        {
            EditorGUILayout.LabelField($"未使用 Key ({_result.UnusedKeys.Count} 项)", EditorStyles.boldLabel);

            if (_result.UnusedKeys.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ 所有 key 均有组件或源码引用，无未使用 key。", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "这些 key 存在于语言文件中，但未在任何 Prefab/Scene 的 Localize 组件、也未在源码 Localization.Get(\"...\") 中被引用。\n" +
                "可能是废弃 key，可考虑清理。注意动态拼接的 key 可能被误报。",
                MessageType.Warning);

            _unusedScroll = EditorGUILayout.BeginScrollView(_unusedScroll, GUILayout.MinHeight(140));
            foreach (var key in _result.UnusedKeys)
            {
                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField(key);
                    GUILayout.FlexibleSpace();
                }
            }
            EditorGUILayout.EndScrollView();
        }
        private DiagnosticsResult RunDiagnostics(List<LocaleData> locales)
        {
            var result = new DiagnosticsResult();
            var allKeys = LocalizationEditorData.CollectAllKeys(locales);

            // 1) 缺失翻译检测
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

            // 2) 未使用 key 检测：所有 key − (组件引用 ∪ 源码字面量引用)
            var referenced = new HashSet<string>();
            // 组件字段引用（Prefab/Scene）
            var compRefs = LocalizationReferenceScanner.ScanAllReferences();
            foreach (var k in compRefs.Keys) referenced.Add(k);
            // 源码字面量引用（Localization.Get("xxx")）
            var srcRefs = LocalizationReferenceScanner.ScanSourceCodeKeyLiterals();
            foreach (var k in srcRefs) referenced.Add(k);

            foreach (var key in allKeys)
            {
                if (!referenced.Contains(key))
                {
                    result.UnusedKeys.Add(key);
                }
            }
            result.UnusedKeys.Sort();
            return result;
        }

        /// <summary>诊断结果。</summary>
        private class DiagnosticsResult
        {
            /// <summary>key → 缺失它的语言代码列表。</summary>
            public Dictionary<string, List<string>> MissingEntries = new Dictionary<string, List<string>>();
            /// <summary>未被任何组件/源码引用的 key 列表（可能可清理）。</summary>
            public List<string> UnusedKeys = new List<string>();
        }
    }
}
