using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>LocalizeFont 的 Inspector 增强：字体 key 下拉选择 + 校验。</summary>
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
            EditorGUILayout.HelpBox(
                "字体资源需放在：Resources/CLocalization/Assets/{语言代码}/{key}\n" +
                "TMP 用 TMP_FontAsset，传统 Text 用 Font。根据挂载目标自动选择类型。",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
