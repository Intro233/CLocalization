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
            EditorGUILayout.HelpBox("AudioClip 资源位置（切换语言时自动加载并恢复播放）：\n" + LocalizationEditorData.GetAssetsHintMessage(), MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
