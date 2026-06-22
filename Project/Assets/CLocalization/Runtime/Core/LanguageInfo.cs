using System;
using TMPro;
using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 语言元数据。描述一种语言的标识、显示名、文化信息等。
    /// 同时作为 ScriptableObject 列表中的一个条目，可在 Settings 的 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class LanguageInfo
    {
        /// <summary>
        /// 语言代码（BCP 47 风格），如 zh-CN / en-US / ja-JP / ko-KR。
        /// 既作为语言唯一标识，也用于构造 CultureInfo 与定位资源路径。
        /// </summary>
        [Tooltip("语言代码，如 zh-CN、en-US、ja-JP、ko-KR")]
        [SerializeField] private string languageCode;

        /// <summary>该语言在 UI 上展示的名称（通常以其本地语言书写，如「中文」「English」）。</summary>
        [Tooltip("UI 上展示的语言名称，建议用该语言自身书写，如「中文」「日本語」")]
        [SerializeField] private string displayName;

        /// <summary>可选：该语言的国旗 / 标识 Sprite，供语言切换 UI 使用。</summary>
        [Tooltip("可选：该语言的国旗 / 标识 Sprite")]
        [SerializeField] private Sprite flag;

        /// <summary>是否从右向左书写的语言（阿拉伯语、希伯来语等为 true）。预留给 RTL 扩展。</summary>
        [Tooltip("是否从右向左书写（如阿拉伯语、希伯来语）")]
        [SerializeField] private bool isRightToLeft;

        /// <summary>该语言全局使用的 TMP 字体（留空则保持文本组件原有字体，不强制覆盖）。</summary>
        [Tooltip("该语言全局 TMP 字体（留空保持原字体）。所有 LocalizeText 切到该语言时自动应用")]
        [SerializeField] private TMP_FontAsset tmpFont;

        /// <summary>该语言全局使用的传统 Font（用于 UI.Text，留空保持原字体）。</summary>
        [Tooltip("该语言全局传统 Font（用于 UI.Text，留空保持原字体）")]
        [SerializeField] private Font fallbackFont;

        /// <summary>无参构造（序列化需要）。</summary>
        public LanguageInfo() { }

        /// <summary>带参构造，便于代码创建。</summary>
        public LanguageInfo(string code, string displayName, bool isRightToLeft = false)
        {
            this.languageCode = code;
            this.displayName = displayName;
            this.isRightToLeft = isRightToLeft;
        }

        /// <summary>语言代码。</summary>
        public string Code => languageCode;

        /// <summary>显示名称。</summary>
        public string DisplayName => displayName;

        /// <summary>国旗 Sprite。</summary>
        public Sprite Flag => flag;

        /// <summary>是否 RTL。</summary>
        public bool IsRightToLeft => isRightToLeft;

        /// <summary>该语言全局 TMP 字体（null 表示保持原字体）。</summary>
        public TMP_FontAsset TmpFont => tmpFont;

        /// <summary>该语言全局传统 Font（null 表示保持原字体）。</summary>
        public Font FallbackFont => fallbackFont;

        /// <summary>是否已设置有效的语言代码。</summary>
        public bool IsValid => !string.IsNullOrEmpty(languageCode);

        public override string ToString()
        {
            return $"[{languageCode}] {displayName}";
        }
    }
}
