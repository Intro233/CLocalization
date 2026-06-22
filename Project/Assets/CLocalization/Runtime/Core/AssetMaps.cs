using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// Sprite 资源映射表。存储 key × 语言 → Sprite 的映射。
    /// <see cref="Localization.GetAsset{Sprite}"/> 通过此表查询。
    /// </summary>
    [CreateAssetMenu(fileName = "SpriteAssetMap", menuName = "CLocalization/Sprite Asset Map", order = 10)]
    public class SpriteAssetMap : AssetMapBase
    {
        /// <summary>按 key + 语言查找 Sprite。找不到返回 null。</summary>
        public Sprite LookupSprite(string key, string languageCode)
        {
            return Lookup(key, languageCode) as Sprite;
        }
    }

    /// <summary>
    /// AudioClip 资源映射表。存储 key × 语言 → AudioClip 的映射。
    /// <see cref="Localization.GetAsset{AudioClip}"/> 通过此表查询。
    /// </summary>
    [CreateAssetMenu(fileName = "AudioClipAssetMap", menuName = "CLocalization/AudioClip Asset Map", order = 11)]
    public class AudioClipAssetMap : AssetMapBase
    {
        /// <summary>按 key + 语言查找 AudioClip。找不到返回 null。</summary>
        public AudioClip LookupAudio(string key, string languageCode)
        {
            return Lookup(key, languageCode) as AudioClip;
        }
    }

    /// <summary>
    /// 字体资源映射表。存储 key × 语言 → TMP_FontAsset / Font 的映射。
    /// asset 字段统一存为 Object，加载时按需转型（TMP_FontAsset 或 Font）。
    /// <see cref="Localization.GetAsset{TMP_FontAsset}"/> / <see cref="GetAsset{Font}"/> 通过此表查询。
    /// </summary>
    [CreateAssetMenu(fileName = "FontAssetMap", menuName = "CLocalization/Font Asset Map", order = 12)]
    public class FontAssetMap : AssetMapBase
    {
        /// <summary>按 key + 语言查找 TMP_FontAsset。找不到返回 null。</summary>
        public TMPro.TMP_FontAsset LookupTMPFont(string key, string languageCode)
        {
            return Lookup(key, languageCode) as TMPro.TMP_FontAsset;
        }

        /// <summary>按 key + 语言查找传统 Font。找不到返回 null。</summary>
        public Font LookupFont(string key, string languageCode)
        {
            return Lookup(key, languageCode) as Font;
        }
    }
}
