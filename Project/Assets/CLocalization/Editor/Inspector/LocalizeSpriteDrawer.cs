using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>LocalizeSprite 的 Inspector 增强：key 下拉选择 + 校验。</summary>
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

            // 提示资源目录约定
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Sprite 资源需放在：Resources/CLocalization/Assets/{语言代码}/{key}\n" +
                "切换语言时自动加载对应 Sprite。",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
