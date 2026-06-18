using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// LocalizeText 组件的 Inspector 增强。复用 <see cref="LocalizeKeyFieldDrawer"/> 的共用逻辑。
    /// 提供：key 弹出搜索选择、预览语言可切换、预览应用插值参数。
    /// </summary>
    [CustomEditor(typeof(LocalizeText))]
    public class LocalizeTextDrawer : UnityEditor.Editor
    {
        private string[] _availableKeys;

        private void OnEnable()
        {
            _availableKeys = LocalizeKeyFieldDrawer.LoadAvailableKeys();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 目标组件引用
            DrawProperty("tmpText");
            DrawProperty("uiText");

            EditorGUILayout.Space(6);

            // key 选择器（复用共用绘制器）
            var keyProp = serializedObject.FindProperty("localizationKey");
            LocalizeKeyFieldDrawer.DrawKeySelector(keyProp, _availableKeys, "Localization Key",
                onKeyChanged: () => Repaint());

            EditorGUILayout.Space(6);

            // 插值参数（先于预览绘制，预览会用到它）
            DrawProperty("formatArgs");

            EditorGUILayout.Space(4);
            DrawProperty("refreshOnEnable");

            // 预览（应用插值参数，可切换语言）
            string[] args = ReadFormatArgs();
            LocalizeKeyFieldDrawer.DrawPreview(keyProp.stringValue, args);

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        /// <summary>从 SerializedProperty 读取 formatArgs 数组。</summary>
        private string[] ReadFormatArgs()
        {
            var prop = serializedObject.FindProperty("formatArgs");
            if (prop == null || !prop.isArray || prop.arraySize == 0) return null;
            var arr = new string[prop.arraySize];
            for (int i = 0; i < prop.arraySize; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                arr[i] = elem.stringValue;
            }
            return arr;
        }

        /// <summary>按属性名绘制字段。</summary>
        private void DrawProperty(string propName)
        {
            var prop = serializedObject.FindProperty(propName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }
    }
}
