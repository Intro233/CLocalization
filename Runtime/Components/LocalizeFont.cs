using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CLocalization
{
    /// <summary>
    /// 字体本地化组件。按当前语言切换 Text/TMP_Text 的字体。
    /// 典型用途：CJK（中日韩）或阿拉伯语需要专用字体，拉丁语用通用字体。
    /// 资源路径约定：
    ///   - TMP 字体：Resources/CLocalization/Assets/{语言代码}/{key}（类型为 TMP_FontAsset）
    ///   - 传统字体：Resources/CLocalization/Assets/{语言代码}/{key}（类型为 Font）
    /// 本组件根据挂载目标的类型自动选择加载哪种字体资源。
    /// </summary>
    [AddComponentMenu("CLocalization/Localize Font")]
    public class LocalizeFont : LocalizeBase
    {
        [Tooltip("目标 TMP_Text 组件（用于切 TMP_FontAsset）。留空则自动获取。")]
        [SerializeField] private TMP_Text tmpText;

        [Tooltip("目标传统 UI.Text 组件（用于切 Font）。留空则自动获取（仅当无 TMP 时使用）。")]
        [SerializeField] private Text uiText;

        [Tooltip("字体资源 key（与文本 key 解耦，专门用于字体）。例如 font.cjk、font.latin")]
        [SerializeField] private string fontKey;

        /// <summary>字体 key 读写访问（与文本 key 分离，避免与 LocalizeText 冲突）。</summary>
        public string FontKey
        {
            get => fontKey;
            set
            {
                fontKey = value;
                if (isActiveAndEnabled) ApplyLocalization();
            }
        }

        protected override void OnEnable()
        {
            ResolveTargets();
            base.OnEnable();
        }

        /// <summary>解析目标组件。</summary>
        private void ResolveTargets()
        {
            if (tmpText == null) tmpText = GetComponent<TMP_Text>();
            if (uiText == null && tmpText == null) uiText = GetComponent<Text>();
            if (tmpText == null && uiText == null)
            {
                LocalizationLog.Warning($"[{nameof(LocalizeFont)}] {gameObject.name} 未找到 TMP_Text 或 Text 组件。");
            }
        }

        /// <summary>
        /// 应用本地化：按 fontKey 加载当前语言字体并设置。
        /// 注意：本组件忽略基类的 localizationKey，改用 fontKey，以支持与文本 key 解耦的字体切换。
        /// </summary>
        public override void ApplyLocalization()
        {
            // 字体组件优先使用 fontKey，回退到 localizationKey
            string key = !string.IsNullOrEmpty(fontKey) ? fontKey : localizationKey;
            if (string.IsNullOrEmpty(key)) return;

            if (tmpText != null)
            {
                var fontAsset = Localization.GetAsset<TMP_FontAsset>(key);
                if (fontAsset != null)
                {
                    tmpText.font = fontAsset;
                }
                else
                {
                    LocalizationLog.Warning($"[{nameof(LocalizeFont)}] 未找到 TMP_FontAsset key=\"{key}\" 语言={Localization.CurrentLanguageCode}。");
                }
            }
            else if (uiText != null)
            {
                var font = Localization.GetAsset<Font>(key);
                if (font != null)
                {
                    uiText.font = font;
                }
                else
                {
                    LocalizationLog.Warning($"[{nameof(LocalizeFont)}] 未找到 Font key=\"{key}\" 语言={Localization.CurrentLanguageCode}。");
                }
            }
        }

        /// <summary>运行时动态设置字体 key。</summary>
        public void SetFontKey(string newKey)
        {
            FontKey = newKey;
        }
    }
}
