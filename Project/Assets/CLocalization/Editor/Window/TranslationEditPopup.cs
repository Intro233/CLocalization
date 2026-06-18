using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 翻译文本编辑弹窗。用于在词条表格里点击单元格后，弹出大文本框编辑长文本/多行翻译。
    /// 解决表格单元格高度受限、长文本挤在一行不好编辑的问题。
    ///
    /// 用法：<c>TranslationEditPopup.Edit(key, langCode, currentValue, OnChanged)</c>
    /// 编辑实时回调（每次输入触发），关闭后数据已在调用方内存，配合「保存全部」落盘。
    /// </summary>
    public class TranslationEditPopup : EditorWindow
    {
        /// <summary>正在编辑的 key。</summary>
        private string _key;
        /// <summary>语言代码。</summary>
        private string _langCode;
        /// <summary>当前文本（编辑中）。</summary>
        private string _text;
        /// <summary>文本变化回调（实时回写）。</summary>
        private System.Action<string> _onChanged;
        /// <summary>大文本框的滚动位置（文本超长时）。</summary>
        private Vector2 _scroll;
        /// <summary>是否首次绘制（用于自动聚焦文本框 + 初始化光标到末尾）。</summary>
        private bool _firstDraw = true;

        /// <summary>弹出编辑窗口。</summary>
        /// <param name="key">正在编辑的 key（仅用于标题展示）</param>
        /// <param name="langCode">语言代码（仅用于标题展示）</param>
        /// <param name="currentValue">当前翻译文本</param>
        /// <param name="onChanged">文本变化回调，参数为最新文本（实时回写调用方）</param>
        public static void Edit(string key, string langCode, string currentValue, System.Action<string> onChanged)
        {
            var window = GetWindow<TranslationEditPopup>(true, "编辑翻译", true);
            window.minSize = new Vector2(420, 240);
            // 居中显示
            window.position = new Rect(new Vector2(Screen.currentResolution.width / 2f - 260, Screen.currentResolution.height / 2f - 180), new Vector2(520, 360));
            window.Init(key, langCode, currentValue, onChanged);
            window.Show();
        }

        /// <summary>初始化。</summary>
        private void Init(string key, string langCode, string currentValue, System.Action<string> onChanged)
        {
            _key = key;
            _langCode = langCode;
            _text = currentValue ?? "";
            _onChanged = onChanged;
            _firstDraw = true;
        }

        private void OnGUI()
        {
            // 标题信息
            EditorGUILayout.LabelField($"Key: {_key}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"语言: {_langCode}", EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            // 大文本框（自适应高度，带滚动）
            EditorGUILayout.LabelField("翻译内容:", EditorStyles.miniBoldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            // TextArea 用 wordWrap 自动换行显示；命名以便首次聚焦
            GUI.SetNextControlName("TranslationTextArea");
            string newText = EditorGUILayout.TextArea(_text, GUI.skin.textArea, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndScrollView();

            // 字符数提示
            EditorGUILayout.LabelField($"字符数: {(_text?.Length ?? 0)}", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            // 底部按钮
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("完成", GUILayout.Width(80)))
                {
                    Close();
                }
            }

            // 首次绘制自动聚焦文本框
            if (_firstDraw)
            {
                EditorGUI.FocusTextInControl("TranslationTextArea");
                _firstDraw = false;
            }

            // 实时回写（每次输入触发回调）
            if (newText != _text)
            {
                _text = newText;
                _onChanged?.Invoke(_text);
            }
        }

        private void OnLostFocus()
        {
            // 失焦时不自动关闭（避免点字符数标签等就关），仅靠「完成」按钮或窗口管理器关闭
        }
    }
}
