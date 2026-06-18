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

            // 提示资源目录约定（按当前加载模式动态显示）
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("Sprite 资源位置：\n" + LocalizationEditorData.GetAssetsHintMessage(), MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
