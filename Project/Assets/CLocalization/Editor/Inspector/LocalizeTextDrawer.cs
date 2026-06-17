using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// LocalizeText 组件的 Inspector 增强。
    /// 把 localizationKey 字段替换为带下拉选择的 key 选择器（从所有 locale 的 key 并集选取），
    /// 并实时显示当前 key 在默认语言中的预览文本，方便配置时确认。
    /// </summary>
    [CustomEditor(typeof(LocalizeText))]
    public class LocalizeTextDrawer : UnityEditor.Editor
    {
        /// <summary>所有可选 key（缓存）。</summary>
        private string[] _availableKeys;
        /// <summary>当前 key 在下拉中的索引。</summary>
        private int _currentIndex = -1;

        public override void OnInspectorGUI()
        {
            var comp = (LocalizeText)target;

            // 绘制默认字段（除 localizationKey 外，由我们自定义绘制）
            DrawProperty("tmpText");
            DrawProperty("uiText");
            DrawProperty("formatArgs");

            EditorGUILayout.Space(6);
            DrawKeySelector(comp);

            EditorGUILayout.Space(4);
            DrawProperty("refreshOnEnable");

            DrawPreview(comp);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        /// <summary>绘制 key 选择器：下拉 + 文本输入 + 新值回填。</summary>
        private void DrawKeySelector(LocalizeText comp)
        {
            EnsureKeysLoaded();

            EditorGUILayout.LabelField("Localization Key", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                // 下拉选择
                int newIdx = EditorGUILayout.Popup(_currentIndex >= 0 ? _currentIndex : 0, _availableKeys);
                if (newIdx != _currentIndex && _availableKeys.Length > 0)
                {
                    _currentIndex = newIdx;
                    Undo.RecordObject(comp, "Change Localization Key");
                    comp.Key = _availableKeys[newIdx];
                }

                // 手动输入/编辑 key（同步更新下拉索引）
                string manual = EditorGUILayout.TextField(comp.Key);
                if (manual != comp.Key)
                {
                    Undo.RecordObject(comp, "Change Localization Key");
                    comp.Key = manual;
                    _currentIndex = System.Array.IndexOf(_availableKeys, manual);
                }
            }

            // 校验提示
            if (!string.IsNullOrEmpty(comp.Key))
            {
                int found = System.Array.IndexOf(_availableKeys, comp.Key);
                if (found < 0)
                {
                    EditorGUILayout.HelpBox($"⚠ key \"{comp.Key}\" 不在任何语言文件中，运行时将显示 key 本身。", MessageType.Warning);
                }
            }
        }

        /// <summary>绘制预览：显示默认语言下该 key 的实际文本。</summary>
        private void DrawPreview(LocalizeText comp)
        {
            if (string.IsNullOrEmpty(comp.Key)) return;
            string preview = Localization.Get(comp.Key);
            if (!string.IsNullOrEmpty(preview) && preview != comp.Key)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("预览（默认语言）", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(preview, MessageType.None);
            }
        }

        /// <summary>按属性名绘制字段（带异常保护）。</summary>
        private void DrawProperty(string propName)
        {
            var prop = serializedObject.FindProperty(propName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, true);
            }
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>懒加载所有 key（仅加载一次，编辑窗口修改后需重新编译才刷新）。</summary>
        private void EnsureKeysLoaded()
        {
            if (_availableKeys != null) return;
            var locales = LocalizationEditorData.LoadAllLocales();
            var keys = LocalizationEditorData.CollectAllKeys(locales);
            keys.Sort();
            _availableKeys = keys.Count > 0 ? keys.ToArray() : new[] { "(无可用 key)" };

            var comp = (LocalizeText)target;
            _currentIndex = System.Array.IndexOf(_availableKeys, comp.Key);
        }
    }
}
