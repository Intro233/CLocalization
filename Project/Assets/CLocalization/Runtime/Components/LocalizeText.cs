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
        [Tooltip("格式化参数（对应 {0}/{1}... 占位符）。运行时也可通过 SetArgs 设置。")]
        [SerializeField] private string[] formatArgs;

        /// <summary>格式化参数读写访问。</summary>
        public string[] FormatArgs
        {
            get => formatArgs;
            set
            {
                formatArgs = value;
                if (isActiveAndEnabled) ApplyLocalization();
            }
        }

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

            // 取原始文本（含缺失回退逻辑）
            string text;
            if (formatArgs != null && formatArgs.Length > 0)
            {
                text = Localization.Get(localizationKey, formatArgs);
            }
            else
            {
                text = Localization.Get(localizationKey);
            }

            ApplyToTarget(text);
        }

        /// <summary>把文本写入目标组件（TMP 优先）。</summary>
        private void ApplyToTarget(string text)
        {
            if (tmpText != null)
            {
                tmpText.text = text;
            }
            else if (uiText != null)
            {
                uiText.text = text;
            }
        }

        /// <summary>运行时动态设置参数并刷新（便捷方法）。</summary>
        public void SetArgs(params string[] args)
        {
            FormatArgs = args;
        }

        /// <summary>运行时动态设置 key（便捷方法）。</summary>
        public void SetKey(string newKey)
        {
            Key = newKey;
        }
    }
}
