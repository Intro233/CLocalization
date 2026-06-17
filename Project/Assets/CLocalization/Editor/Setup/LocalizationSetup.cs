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
