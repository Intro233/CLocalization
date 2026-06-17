using UnityEngine;
using UnityEngine.UI;

namespace CLocalization
{
    /// <summary>
    /// 图片本地化组件。通过 key 从当前语言的资源中加载 Sprite，应用到 Image。
    /// 资源路径约定：Resources/CLocalization/Assets/{语言代码}/{key}
    /// 切换语言时自动重新加载并刷新。
    /// </summary>
    [AddComponentMenu("CLocalization/Localize Sprite")]
    [RequireComponent(typeof(Image))]
    public class LocalizeSprite : LocalizeBase
    {
        [Tooltip("目标 Image 组件。留空则自动获取（本组件必须挂在含 Image 的物体上）。")]
        [SerializeField] private Image targetImage;

        protected override void OnEnable()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            base.OnEnable();
        }

        /// <summary>应用本地化：按 key 加载当前语言的 Sprite 并设置。</summary>
        public override void ApplyLocalization()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (targetImage == null) return;
            if (string.IsNullOrEmpty(localizationKey)) return;

            Sprite sprite = Localization.GetAsset<Sprite>(localizationKey);
            if (sprite != null)
            {
                targetImage.sprite = sprite;
                // 确保图片可见（有时本地化前为占位空图）
                targetImage.enabled = true;
            }
            else
            {
                LocalizationLog.Warning($"[{nameof(LocalizeSprite)}] 未找到 Sprite 资源 key=\"{localizationKey}\" 语言={Localization.CurrentLanguageCode}。");
            }
        }

        /// <summary>运行时动态设置 key。</summary>
        public void SetKey(string newKey)
        {
            Key = newKey;
        }
    }
}
