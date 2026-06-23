using System;
using TMPro;
using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 语言元数据。描述一种语言的标识、显示名、文化信息等。
    /// 字体/国旗通过路径存储（避免强引用导致资源无法打 AB 分包），运行时由 Localization 层按路径加载。
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

        /// <summary>是否从右向左书写的语言（阿拉伯语、希伯来语等为 true）。</summary>
        [Tooltip("是否从右向左书写（如阿拉伯语、希伯来语）")]
        [SerializeField] private bool isRightToLeft;

        [Header("全局字体（路径存储）")]
        /// <summary>该语言全局 TMP 字体的资源路径。</summary>
        [Tooltip("该语言全局 TMP 字体路径（留空保持原字体）")]
        [SerializeField] private string tmpFontPath;

        /// <summary>TMP 字体路径类型。</summary>
        [SerializeField] private AssetPathType tmpFontPathType = AssetPathType.Resources;

        /// <summary>该语言全局传统 Font 的资源路径。</summary>
        [Tooltip("该语言全局传统 Font 路径（留空保持原字体）")]
        [SerializeField] private string fallbackFontPath;

        /// <summary>传统 Font 路径类型。</summary>
        [SerializeField] private AssetPathType fallbackFontPathType = AssetPathType.Resources;

        [Header("国旗（路径存储）")]
        /// <summary>国旗 Sprite 资源路径。</summary>
        [Tooltip("国旗 Sprite 路径（留空无国旗）")]
        [SerializeField] private string flagPath;

        /// <summary>国旗路径类型。</summary>
        [SerializeField] private AssetPathType flagPathType = AssetPathType.Resources;

        // ---------- 旧版强引用字段（保留用于编辑器预览和向后兼容） ----------
        [SerializeField] private TMP_FontAsset tmpFont;        // 旧字段，运行时不读
        [SerializeField] private Font fallbackFont;            // 旧字段，运行时不读
        [SerializeField] private Sprite flag;                  // 旧字段，运行时不读

        /// <summary>无参构造（序列化需要）。</summary>
        public LanguageInfo() { }

        /// <summary>带参构造，便于代码创建。</summary>
        public LanguageInfo(string code, string displayName, bool isRightToLeft = false)
        {
            this.languageCode = code;
            this.displayName = displayName;
            this.isRightToLeft = isRightToLeft;
        }

        // ---------- 访问器 ----------

        /// <summary>语言代码。</summary>
        public string Code => languageCode;

        /// <summary>显示名称。</summary>
        public string DisplayName => displayName;

        /// <summary>是否 RTL。</summary>
        public bool IsRightToLeft => isRightToLeft;

        /// <summary>是否已设置有效的语言代码。</summary>
        public bool IsValid => !string.IsNullOrEmpty(languageCode);

        // ---------- 字体路径访问器 ----------

        /// <summary>TMP 字体路径（null 表示未配置，保持原字体）。</summary>
        public string TmpFontPath => tmpFontPath;
        public AssetPathType TmpFontPathType => tmpFontPathType;
        public bool HasTmpFont => !string.IsNullOrEmpty(tmpFontPath);

        /// <summary>传统 Font 路径。</summary>
        public string FallbackFontPath => fallbackFontPath;
        public AssetPathType FallbackFontPathType => fallbackFontPathType;
        public bool HasFallbackFont => !string.IsNullOrEmpty(fallbackFontPath);

        /// <summary>国旗路径。</summary>
        public string FlagPath => flagPath;
        public AssetPathType FlagPathType => flagPathType;
        public bool HasFlag => !string.IsNullOrEmpty(flagPath);

        // ---------- 旧版强引用访问器（编辑器预览用，运行时不推荐使用） ----------

        /// <summary>编辑器预览：TMP 字体强引用（运行时用 TmpFontPath + Loader 加载）。</summary>
        public TMP_FontAsset PreviewTmpFont => tmpFont;
        /// <summary>编辑器预览：传统 Font 强引用。</summary>
        public Font PreviewFallbackFont => fallbackFont;
        /// <summary>编辑器预览：国旗 Sprite 强引用。</summary>
        public Sprite PreviewFlag => flag;

        public override string ToString()
        {
            return $"[{languageCode}] {displayName}";
        }
    }
}
