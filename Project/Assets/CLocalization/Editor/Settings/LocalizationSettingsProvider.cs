using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 在 Project Settings 窗口中注册「Localization」配置面板。
    /// 用户可在 Edit > Project Settings > CLocalization 中管理语言列表、默认语言、回退策略等。
    /// 自动加载/创建 Settings 资源（若无则调引导脚本生成）。
    /// </summary>
    public class LocalizationSettingsProvider : SettingsProvider
    {
        /// <summary>Settings 资源序列化对象，用于 Inspector 风格编辑。</summary>
        private SerializedObject _serializedObject;

        public LocalizationSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        /// <summary>注册到 Project Settings 窗口。</summary>
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new LocalizationSettingsProvider("Project/CLocalization", SettingsScope.Project)
            {
                label = "CLocalization",
                keywords = new[] { "Localization", "Language", "多语言", "本地化" }
            };
        }

        /// <summary>懒加载 SerializedObject（不 override OnActivate 以规避跨版本签名差异）。</summary>
        private void EnsureSerializedObject()
        {
            if (_serializedObject != null && _serializedObject.targetObject != null) return;
            var settings = LocalizationSetup.LoadOrCreateSettings();
            if (settings != null)
            {
                _serializedObject = new SerializedObject(settings);
            }
        }

        public override void OnGUI(string searchContext)
        {
            EnsureSerializedObject();

            if (_serializedObject == null || _serializedObject.targetObject == null)
            {
                EditorGUILayout.HelpBox("未找到 LocalizationSettings 资源。点击下方按钮创建。", MessageType.Warning);
                if (GUILayout.Button("创建 Settings 资源", GUILayout.Width(200)))
                {
                    LocalizationSetup.CreateSettingsAsset();
                    var s = LocalizationSetup.LoadOrCreateSettings();
                    if (s != null) _serializedObject = new SerializedObject(s);
                }
                return;
            }

            _serializedObject.Update();

            EditorGUILayout.LabelField("语言列表", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("languages"), true);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("行为设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("defaultLanguageCode"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("persistLanguageChoice"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("useSystemLanguageByDefault"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("fallbackToDefaultLanguage"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("logMissingKeys"));

            if (_serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_serializedObject.targetObject);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "提示：修改语言列表后，请到 Tools > CLocalization > Localization Window 中编辑各语言词条，或通过 CSV 导入导出协作。\n" +
                "新增语言后记得在 Locales 目录创建对应的 JSON 文件。",
                MessageType.Info);
        }
    }
}
