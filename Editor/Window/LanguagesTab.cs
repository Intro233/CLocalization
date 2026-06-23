using System.Collections.Generic;
using TMPro;
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

        /// <summary>各语言字体配置区的展开状态（语言代码 → 是否展开）。</summary>
        private readonly Dictionary<string, bool> _fontFoldout = new Dictionary<string, bool>();

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
                string code = locale.Meta?.Code ?? "?";

                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    // 序号
                    EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(28));
                    EditorGUILayout.LabelField(code, GUILayout.Width(100));
                    EditorGUILayout.LabelField(locale.Meta?.DisplayName ?? "", GUILayout.Width(120));
                    int entryCount = locale.Entries?.Count ?? 0;
                    EditorGUILayout.LabelField($"{entryCount} 词条", GUILayout.Width(80));
                    GUILayout.FlexibleSpace();

                    // 字体配置展开/折叠按钮
                    bool foldout = _fontFoldout.TryGetValue(code, out var f) && f;
                    bool newFoldout = GUILayout.Toggle(foldout, "字体", EditorStyles.miniButton, GUILayout.Width(50));
                    if (newFoldout != foldout) _fontFoldout[code] = newFoldout;

                    // 上移（第一个禁用）
                    using (new EditorGUI.DisabledScope(i == 0))
                    {
                        if (GUILayout.Button("↑", GUILayout.Width(28)))
                        {
                            window.MoveLanguage(i, i - 1);
                            SyncSettings(window);
                        }
                    }
                    // 下移（最后一个禁用）
                    using (new EditorGUI.DisabledScope(i == locales.Count - 1))
                    {
                        if (GUILayout.Button("↓", GUILayout.Width(28)))
                        {
                            window.MoveLanguage(i, i + 1);
                            SyncSettings(window);
                        }
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(50)))
                    {
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

                // 字体折叠配置区（展开时显示该语言的 TMP_FontAsset + Font 字段）
                if (_fontFoldout.TryGetValue(code, out var expanded) && expanded)
                {
                    DrawFontConfig(code);
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

        /// <summary>
        /// 绘制某语言的全局字体配置区：TMP_FontAsset + 传统 Font 字段。
        /// 通过 SerializedObject 操作 Settings.languages 对应条目。
        /// </summary>
        private void DrawFontConfig(string code)
        {
            var settings = LocalizationSetup.LoadOrCreateSettings();
            if (settings == null) return;

            var so = new SerializedObject(settings);
            var list = so.FindProperty("languages");
            if (list == null) return;

            // 找到对应 code 的 LanguageInfo 列表项
            SerializedProperty langProp = null;
            for (int i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                var codeProp = element.FindPropertyRelative("languageCode");
                if (codeProp != null && codeProp.stringValue == code)
                {
                    langProp = element;
                    break;
                }
            }

            if (langProp == null)
            {
                EditorGUILayout.HelpBox($"未在 Settings 找到语言 \"{code}\"，无法配置字体。", MessageType.Warning);
                return;
            }

            // 缩进绘制字体字段（路径存储 + ObjectField 预览）
            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField($"全局字体配置 — {code}", EditorStyles.miniBoldLabel);

                // 旧数据迁移：tmpFont 强引用 → tmpFontPath
                MigrateLegacyFont(langProp, "tmpFont", "tmpFontPath", "tmpFontPathType");
                MigrateLegacyFont(langProp, "fallbackFont", "fallbackFontPath", "fallbackFontPathType");

                // TMP 字体（ObjectField 预览 + 存路径）
                DrawPathObjectField(langProp, "tmpFont", "tmpFontPath", "tmpFontPathType",
                    "TMP 字体", typeof(TMP_FontAsset));

                // 传统 Font
                DrawPathObjectField(langProp, "fallbackFont", "fallbackFontPath", "fallbackFontPathType",
                    "传统 Font", typeof(Font));

                EditorGUILayout.HelpBox("留空则保持文本组件原有字体。切换到该语言时，所有 LocalizeText 自动应用此字体。\n资源可在工程任意位置（在 Resources 下可运行时加载，其他位置需自定义 Loader/AB）。",
                    MessageType.Info);
            }

            if (so.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>绘制路径存储的 ObjectField：拖拽资源时存路径，显示时用预览引用回填。</summary>
        /// <param name="langProp">LanguageInfo 的 SerializedProperty</param>
        /// <param name="previewField">预览强引用字段名（如 "tmpFont"）</param>
        /// <param name="pathField">路径字段名（如 "tmpFontPath"）</param>
        /// <param name="typeField">路径类型字段名</param>
        /// <param name="label">显示标签</param>
        /// <param name="assetType">ObjectField 约束的资源类型</param>
        private void DrawPathObjectField(SerializedProperty langProp, string previewField,
            string pathField, string typeField, string label, System.Type assetType)
        {
            var previewProp = langProp.FindPropertyRelative(previewField);
            var pathProp = langProp.FindPropertyRelative(pathField);
            var typeProp = langProp.FindPropertyRelative(typeField);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(80));
                var currentPreview = previewProp != null ? previewProp.objectReferenceValue : null;
                var newAsset = EditorGUILayout.ObjectField(currentPreview, assetType, false);
                if (newAsset != currentPreview)
                {
                    if (newAsset != null)
                    {
                        string fullPath = AssetDatabase.GetAssetPath(newAsset);
                        var (storedPath, pType) = AssetsTab.ConvertToStoredPath(fullPath);
                        if (pathProp != null) pathProp.stringValue = storedPath;
                        if (typeProp != null) typeProp.enumValueIndex = (int)pType;
                        if (previewProp != null) previewProp.objectReferenceValue = newAsset;
                    }
                    else
                    {
                        if (pathProp != null) pathProp.stringValue = "";
                        if (previewProp != null) previewProp.objectReferenceValue = null;
                    }
                }
            }
        }

        /// <summary>旧数据迁移：强引用字段 → 路径字段（仅当强引用有值且路径为空时）。</summary>
        private void MigrateLegacyFont(SerializedProperty langProp, string legacyField, string pathField, string typeField)
        {
            var legacyProp = langProp.FindPropertyRelative(legacyField);
            var pathProp = langProp.FindPropertyRelative(pathField);
            if (legacyProp == null || pathProp == null) return;
            if (legacyProp.objectReferenceValue != null && string.IsNullOrEmpty(pathProp.stringValue))
            {
                string fullPath = AssetDatabase.GetAssetPath(legacyProp.objectReferenceValue);
                var (storedPath, pType) = AssetsTab.ConvertToStoredPath(fullPath);
                pathProp.stringValue = storedPath;
                var typeProp = langProp.FindPropertyRelative(typeField);
                if (typeProp != null) typeProp.enumValueIndex = (int)pType;
            }
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
        /// <remarks>
        /// 基于窗口当前的内存 locale 列表，用增量合并方式同步 Settings.languages：
        /// 已存在的语言保留其全部字段（含字体/国旗/RTL 等配置），只更新顺序与 displayName；
        /// 新语言追加空配置；不再存在的语言移除。
        /// 避免清空重建导致已配置的字体等字段丢失。
        /// </remarks>
        private void SyncSettings(LocalizationWindow window)
        {
            var settings = LocalizationSetup.LoadOrCreateSettings();
            if (settings == null) return;

            var locales = window.GetCurrentLocales();
            var so = new SerializedObject(settings);
            var list = so.FindProperty("languages");
            if (list == null) return;

            // 收集当前内存中应有的语言 code（按 locales 顺序）
            var desiredCodes = new List<string>();
            foreach (var locale in locales)
            {
                if (locale?.Meta != null && !string.IsNullOrEmpty(locale.Meta.Code))
                {
                    desiredCodes.Add(locale.Meta.Code);
                }
            }

            // 增量合并：按 desiredCodes 顺序重建列表，但复用已有条目（保留字体等字段）
            // 先收集已有条目（code → SerializedProperty 内容快照不便，改为就地操作）
            // 策略：遍历 desiredCodes，对每个 code：
            //   - 若已有同名条目 → 保留，仅更新 displayName
            //   - 若无 → 追加新条目
            // 最后移除不在 desiredCodes 中的条目

            // 收集 code → displayName 映射（用于更新名称）
            var codeToName = new Dictionary<string, string>();
            foreach (var l in locales)
            {
                if (l?.Meta != null && !string.IsNullOrEmpty(l.Meta.Code))
                {
                    codeToName[l.Meta.Code] = l.Meta.DisplayName ?? l.Meta.Code;
                }
            }

            // 先处理移除：从后往前删不在 desiredCodes 的条目
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                var codeProp = list.GetArrayElementAtIndex(i).FindPropertyRelative("languageCode");
                if (codeProp == null || !desiredCodes.Contains(codeProp.stringValue))
                {
                    list.DeleteArrayElementAtIndex(i);
                }
            }

            // 现有条目的 code 集合（删除后剩余的）
            var remainingCodes = new HashSet<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var codeProp = list.GetArrayElementAtIndex(i).FindPropertyRelative("languageCode");
                if (codeProp != null) remainingCodes.Add(codeProp.stringValue);
            }

            // 追加缺失的新条目
            foreach (var code in desiredCodes)
            {
                if (!remainingCodes.Contains(code))
                {
                    int idx = list.arraySize;
                    list.arraySize = idx + 1;
                    var element = list.GetArrayElementAtIndex(idx);
                    var codeProp = element.FindPropertyRelative("languageCode");
                    var nameProp = element.FindPropertyRelative("displayName");
                    if (codeProp != null) codeProp.stringValue = code;
                    if (nameProp != null) nameProp.stringValue = codeToName.TryGetValue(code, out var dn) ? dn : code;
                }
            }

            // 按 desiredCodes 顺序重排现有条目（用移动而非重建，保留字段）
            // SerializedProperty 数组重排：把每个 desiredCode 对应的条目移到正确位置
            for (int targetIdx = 0; targetIdx < desiredCodes.Count; targetIdx++)
            {
                string targetCode = desiredCodes[targetIdx];
                // 找到该 code 当前在列表中的位置
                int currentIdx = -1;
                for (int i = targetIdx; i < list.arraySize; i++)
                {
                    var codeProp = list.GetArrayElementAtIndex(i).FindPropertyRelative("languageCode");
                    if (codeProp != null && codeProp.stringValue == targetCode)
                    {
                        currentIdx = i;
                        break;
                    }
                }
                if (currentIdx > targetIdx)
                {
                    list.MoveArrayElement(currentIdx, targetIdx);
                }
                // 更新 displayName（顺序对了之后）
                var elem = list.GetArrayElementAtIndex(targetIdx);
                var nameProp = elem.FindPropertyRelative("displayName");
                if (nameProp != null && codeToName.TryGetValue(targetCode, out var dn))
                {
                    nameProp.stringValue = dn;
                }
            }

            // 如果 desiredCodes 为空但 list 还有残余（理论不会），清空
            while (list.arraySize > desiredCodes.Count)
            {
                list.DeleteArrayElementAtIndex(list.arraySize - 1);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            // 仅刷新各 Tab 的 key 缓存，不做全量 Reload（避免清空待删集合）
            window.RefreshCaches();
        }
    }
}
