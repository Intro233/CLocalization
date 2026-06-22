using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>LocalizeSprite 的 Inspector 增强：key 搜索选择 + 校验 + 资源目录提示。</summary>
    [CustomEditor(typeof(LocalizeSprite))]
    public class LocalizeSpriteDrawer : UnityEditor.Editor
    {
        private string[] _availableKeys;

        private void OnEnable()
        {
            _availableKeys = LocalizeKeyFieldDrawer.LoadAvailableKeys();
        }

        public override void OnInspectorGUI()
        {
            var keyProp = serializedObject.FindProperty("localizationKey");
            LocalizeKeyFieldDrawer.DrawKeySelector(keyProp, _availableKeys, "Localization Key");

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("refreshOnEnable"), true);

            // 资源映射状态提示（按当前 key 显示配置状态）
            EditorGUILayout.Space(4);
            var settings = LocalizationSetup.LoadOrCreateSettings();
            string key = keyProp.stringValue;
            EditorGUILayout.HelpBox(
                "Sprite 资源映射：\n" + LocalizationEditorData.GetAssetsHintMessage(key, settings?.SpriteMap),
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
