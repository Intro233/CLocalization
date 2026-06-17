using System.IO;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 本地化插件引导/设置工具。
    /// 提供菜单项一键创建 LocalizationSettings 资源（含默认 4 语言配置），
    /// 避免手工构造 .asset 文件（手工 .asset 的脚本 GUID 难以保证正确）。
    /// </summary>
    public static class LocalizationSetup
    {
        /// <summary>Settings 资源默认存放的相对路径（相对于 Assets，不含扩展名）。</summary>
        public const string SettingsAssetPath = "Assets/CLocalization/Resources/CLocalization/LocalizationSettings.asset";

        /// <summary>Settings 资源在 Resources 下的加载路径（不含扩展名）。</summary>
        public const string SettingsResourcesPath = "CLocalization/LocalizationSettings";

        [MenuItem("Tools/CLocalization/Create Settings Asset", priority = 0)]
        public static void CreateSettingsAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsAssetPath);
            if (existing != null)
            {
                LocalizationLog.Info($"LocalizationSettings 已存在: {SettingsAssetPath}");
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = existing;
                return;
            }

            // 确保目录存在
            string dir = Path.GetDirectoryName(SettingsAssetPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            // 创建实例并预置默认 4 语言
            var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            ConfigureDefaults(settings);

            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LocalizationLog.Info($"已创建 LocalizationSettings: {SettingsAssetPath}");
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;
        }

        /// <summary>
        /// 尝试加载 Settings 资源；若不存在则自动创建。供编辑器窗口等模块复用。
        /// </summary>
        public static LocalizationSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsAssetPath);
            if (settings == null)
            {
                CreateSettingsAsset();
                settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsAssetPath);
            }
            return settings;
        }

        /// <summary>
        /// 根据磁盘上实际存在的 locale 文件，把 Settings 的语言列表与文件保持同步（merge 策略）。
        /// - 文件存在但 Settings 没有的语言 → 新增到 Settings；
        /// - Settings 有但文件不存在的语言 → 保留（不删除，避免丢失用户手动配置），仅会被后续 UI 提示；
        /// - 两边都有的 → 用文件的 displayName 更新。
        /// 供 AssetPostprocessor（文件增删时）与编辑器窗口共用。
        /// </summary>
        /// <returns>是否有变化。</returns>
        public static bool SyncSettingsFromLocales()
        {
            var settings = LoadOrCreateSettings();
            if (settings == null) return false;

            var locales = LocalizationEditorData.LoadAllLocales();
            var so = new SerializedObject(settings);
            var list = so.FindProperty("languages");
            if (list == null) return false;

            // 收集文件中现有的语言代码
            var fileCodes = new System.Collections.Generic.HashSet<string>();
            foreach (var locale in locales)
            {
                if (locale?.Meta == null) continue;
                fileCodes.Add(locale.Meta.Code);
            }

            bool changed = false;

            // 1) 文件有、Settings 没有的 → 追加
            var existingCodes = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var codeProp = list.GetArrayElementAtIndex(i).FindPropertyRelative("languageCode");
                if (codeProp != null && !string.IsNullOrEmpty(codeProp.stringValue))
                {
                    existingCodes.Add(codeProp.stringValue);
                }
            }

            foreach (var locale in locales)
            {
                if (locale?.Meta == null) continue;
                if (!existingCodes.Contains(locale.Meta.Code))
                {
                    int idx = list.arraySize;
                    list.arraySize = idx + 1;
                    var element = list.GetArrayElementAtIndex(idx);
                    var codeProp = element.FindPropertyRelative("languageCode");
                    var nameProp = element.FindPropertyRelative("displayName");
                    if (codeProp != null) codeProp.stringValue = locale.Meta.Code;
                    if (nameProp != null) nameProp.stringValue = locale.Meta.DisplayName ?? locale.Meta.Code;
                    changed = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            return changed;
        }

        /// <summary>为 settings 填入默认 4 语言（中/英/日/韩），仅当列表为空时填充。</summary>
        public static void ConfigureDefaults(LocalizationSettings settings)
        {
            if (settings.Languages != null && settings.Languages.Count > 0) return;

            // 反射式写入私有字段较繁琐；此处借助 SerializedObject 添加（编辑器环境）
            var so = new SerializedObject(settings);
            var list = so.FindProperty("languages");
            if (list != null)
            {
                list.arraySize = 0;
                AppendLanguage(list, "zh-CN", "中文");
                AppendLanguage(list, "en-US", "English");
                AppendLanguage(list, "ja-JP", "日本語");
                AppendLanguage(list, "ko-KR", "한국어");
            }
            var defCode = so.FindProperty("defaultLanguageCode");
            if (defCode != null && string.IsNullOrEmpty(defCode.stringValue))
            {
                defCode.stringValue = "en-US";
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>向 SerializedProperty 表示的语言列表追加一个条目。</summary>
        private static void AppendLanguage(SerializedProperty list, string code, string displayName)
        {
            int idx = list.arraySize;
            list.arraySize = idx + 1;
            var element = list.GetArrayElementAtIndex(idx);
            var codeProp = element.FindPropertyRelative("languageCode");
            var nameProp = element.FindPropertyRelative("displayName");
            if (codeProp != null) codeProp.stringValue = code;
            if (nameProp != null) nameProp.stringValue = displayName;
        }
    }
}
