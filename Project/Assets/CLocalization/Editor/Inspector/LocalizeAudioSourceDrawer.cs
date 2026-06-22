using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>LocalizeAudioSource 的 Inspector 增强：key 搜索选择 + 校验。</summary>
    [CustomEditor(typeof(LocalizeAudioSource))]
    public class LocalizeAudioSourceDrawer : UnityEditor.Editor
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoPlayIfPlaying"), true);

            EditorGUILayout.Space(4);
            var settings = LocalizationSetup.LoadOrCreateSettings();
            string key = serializedObject.FindProperty("localizationKey").stringValue;
            EditorGUILayout.HelpBox(
                "AudioClip 资源映射：\n" + LocalizationEditorData.GetAssetsHintMessage(key, settings?.AudioMap),
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
