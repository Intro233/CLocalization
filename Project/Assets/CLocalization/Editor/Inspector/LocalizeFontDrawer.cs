using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>LocalizeFont 的 Inspector 增强：字体 key 搜索选择 + 校验。</summary>
    [CustomEditor(typeof(LocalizeFont))]
    public class LocalizeFontDrawer : UnityEditor.Editor
    {
        private string[] _availableKeys;

        private void OnEnable()
        {
            _availableKeys = LocalizeKeyFieldDrawer.LoadAvailableKeys();
        }

        public override void OnInspectorGUI()
        {
            // LocalizeFont 用独立的 fontKey（优先），回退到 localizationKey
            var fontKeyProp = serializedObject.FindProperty("fontKey");
            var locKeyProp = serializedObject.FindProperty("localizationKey");

            LocalizeKeyFieldDrawer.DrawKeySelector(fontKeyProp, _availableKeys, "Font Key（字体专用，推荐）");

            EditorGUILayout.Space(2);
            // 折叠显示回退的 localizationKey
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("回退：若 Font Key 为空，使用以下通用 Key", EditorStyles.miniLabel);
                LocalizeKeyFieldDrawer.DrawKeySelector(locKeyProp, _availableKeys, "通用 Key（回退）");
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("refreshOnEnable"), true);

            EditorGUILayout.Space(4);
            var settings = LocalizationSetup.LoadOrCreateSettings();
            // 字体用 fontKey（优先），回退到 localizationKey
            string fontKey = serializedObject.FindProperty("fontKey").stringValue;
            string locKey = serializedObject.FindProperty("localizationKey").stringValue;
            string key = !string.IsNullOrEmpty(fontKey) ? fontKey : locKey;
            EditorGUILayout.HelpBox(
                "字体资源映射（TMP 用 TMP_FontAsset，传统 Text 用 Font）：\n" + LocalizationEditorData.GetAssetsHintMessage(key, settings?.FontMap),
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "⚠ 此组件为单点覆盖：优先级高于语言全局字体配置（LanguageInfo 的 tmpFont/fallbackFont）。\n" +
                "未挂此组件的文本将使用语言配置的全局字体。仅在需要个别文本使用特殊字体时挂载。",
                MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
