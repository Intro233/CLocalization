using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CLocalization
{
    /// <summary>
    /// 文本本地化组件。同时支持 TextMeshPro（TMP_Text）与传统 UGUI Text。
    /// 自动获取目标文本组件，并在语言切换时刷新显示。
    /// 支持参数插值：通过 <see cref="formatArgs"/> 注入 {0}/{1}... 参数。
    /// </summary>
    [AddComponentMenu("CLocalization/Localize Text")]
    public class LocalizeText : LocalizeBase
    {
        [Header("文本目标")]
        [Tooltip("目标 TMP_Text 组件。留空则自动获取。")]
        [SerializeField] private TMP_Text tmpText;

        [Tooltip("目标传统 UI.Text 组件。留空则自动获取（仅当无 TMP 时使用）。")]
        [SerializeField] private Text uiText;

        [Header("插值参数")]
        [Tooltip("格式化参数（对应 {0}/{1}... 位置占位符）。运行时也可通过 SetArgs 设置。")]
        [SerializeField] private string[] formatArgs;

        /// <summary>命名占位符参数（对应 {name} 占位符）。运行时通过 SetNamedArgs 设置（不序列化到 Inspector）。</summary>
        private Dictionary<string, object> _namedArgs;

        /// <summary>格式化参数读写访问（位置占位 {0}）。</summary>
        public string[] FormatArgs
        {
            get => formatArgs;
            set
            {
                formatArgs = value;
                if (isActiveAndEnabled) ApplyLocalization();
            }
        }

        /// <summary>命名占位符参数读写访问（命名占位 {name}）。</summary>
        public IReadOnlyDictionary<string, object> NamedArgs => _namedArgs;

        protected override void OnEnable()
        {
            // 先确保拿到目标组件，再走基类逻辑（基类会触发 ApplyLocalization）
            ResolveTargets();
            base.OnEnable();
        }

        /// <summary>解析目标文本组件：优先使用显式引用，否则自动获取。</summary>
        private void ResolveTargets()
        {
            if (tmpText == null) tmpText = GetComponent<TMP_Text>();
            if (uiText == null && tmpText == null) uiText = GetComponent<Text>();
            if (tmpText == null && uiText == null)
            {
                LocalizationLog.Warning($"[{nameof(LocalizeText)}] {gameObject.name} 未找到 TMP_Text 或 Text 组件。");
            }
        }

        /// <summary>应用本地化：取 key 文本，做参数插值，设置到目标组件。</summary>
        public override void ApplyLocalization()
        {
            if (string.IsNullOrEmpty(localizationKey)) return;
            
            // 取原始文本（含缺失回退逻辑），按参数类型选择插值方式
            string text;
            bool hasPositional = formatArgs != null && formatArgs.Length > 0;
            bool hasNamed = _namedArgs != null && _namedArgs.Count > 0;

            if (hasNamed && hasPositional)
            {
                // 混合占位：命名 {name} + 位置 {0} 共存
                text = Localization.Get(localizationKey, _namedArgs, formatArgs);
            }
            else if (hasNamed)
            {
                // 仅命名占位 {name}
                text = Localization.Get(localizationKey, _namedArgs);
            }
            else if (hasPositional)
            {
                // 仅位置占位 {0}
                text = Localization.Get(localizationKey, formatArgs);
            }
            else
            {
                text = Localization.Get(localizationKey);
            }

            ApplyToTarget(text);
        }

        /// <summary>把文本写入目标组件（TMP 优先），并根据当前语言应用 RTL 与全局字体。</summary>
        private void ApplyToTarget(string text)
        {
            // 读取当前语言信息（RTL 标志 + 全局字体路径配置）
            LanguageInfo lang = Localization.CurrentLanguage;
            bool isRtl = Localization.IsInitialized && lang != null && lang.IsRightToLeft;

            if (tmpText != null)
            {
                // TMP 原生支持 RTL 文本方向
                tmpText.isRightToLeftText = isRtl;
                // 应用全局字体（从 LanguageInfo 路径加载，未配置则保持原字体）
                if (lang != null && lang.HasTmpFont)
                {
                    var font = Localization.LoadAssetByPath<TMP_FontAsset>(lang.TmpFontPath, lang.TmpFontPathType);
                    if (font != null) tmpText.font = font;
                }
                tmpText.text = text;
            }
            else if (uiText != null)
            {
                // 传统 UI.Text 全局字体（路径加载，未配置保持原字体）
                if (lang != null && lang.HasFallbackFont)
                {
                    var font = Localization.LoadAssetByPath<Font>(lang.FallbackFontPath, lang.FallbackFontPathType);
                    if (font != null) uiText.font = font;
                }
                // 传统 UI.Text 无原生 RTL 支持，仅做文本设置；
                // 完整 RTL（含 bidi 文本整形）需调用方自行处理，或使用 TMP。
                uiText.text = text;
            }
        }

        /// <summary>运行时动态设置【位置占位】参数并刷新（便捷方法）。</summary>
        public void SetArgs(params string[] args)
        {
            FormatArgs = args;
        }

        /// <summary>
        /// 运行时动态设置【命名占位】参数并刷新。对应模板中的 {name} 占位符。
        /// 例如：<c>SetNamedArgs(new Dictionary&lt;string,object&gt;{{ {"name","Player"} }})</c>
        /// </summary>
        public void SetNamedArgs(IDictionary<string, object> namedArgs)
        {
            _namedArgs = namedArgs != null && namedArgs.Count > 0
                ? new Dictionary<string, object>(namedArgs)
                : null;
            if (isActiveAndEnabled) ApplyLocalization();
        }

        /// <summary>
        /// 运行时动态设置【命名占位】参数（通过匿名对象属性）。
        /// 例如：<c>SetNamedArgs(new {{ name = "Player", count = 3 }})</c>
        /// </summary>
        public void SetNamedArgs(object argsObject)
        {
            if (argsObject == null)
            {
                _namedArgs = null;
            }
            else
            {
                // 复用 Localization 的反射转换逻辑：构造命名字典
                var dict = new Dictionary<string, object>();
                foreach (var prop in argsObject.GetType().GetProperties())
                {
                    try { dict[prop.Name] = prop.GetValue(argsObject, null); }
                    catch { /* 读取失败的属性跳过 */ }
                }
                _namedArgs = dict.Count > 0 ? dict : null;
            }
            if (isActiveAndEnabled) ApplyLocalization();
        }

        /// <summary>运行时动态设置 key（便捷方法）。</summary>
        public void SetKey(string newKey)
        {
            Key = newKey;
        }
    }
}
