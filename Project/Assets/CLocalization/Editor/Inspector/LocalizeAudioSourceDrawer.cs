using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>LocalizeAudioSource 的 Inspector 增强：key 下拉选择 + 校验。</summary>
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
            EditorGUILayout.HelpBox(
                "AudioClip 资源需放在：Resources/CLocalization/Assets/{语言代码}/{key}\n" +
                "切换语言时自动加载对应音频（若之前正在播放则恢复播放）。",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
