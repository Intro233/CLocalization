using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// Sprite 资源映射表。存储 key × 语言 → Sprite 路径的映射。
    /// <see cref="Localization.GetAsset{Sprite}"/> 通过此表查询路径后加载。
    /// </summary>
    [CreateAssetMenu(fileName = "SpriteAssetMap", menuName = "CLocalization/Sprite Asset Map", order = 10)]
    public class SpriteAssetMap : AssetMapBase
    {
    }

    /// <summary>
    /// AudioClip 资源映射表。存储 key × 语言 → AudioClip 路径的映射。
    /// <see cref="Localization.GetAsset{AudioClip}"/> 通过此表查询路径后加载。
    /// </summary>
    [CreateAssetMenu(fileName = "AudioClipAssetMap", menuName = "CLocalization/AudioClip Asset Map", order = 11)]
    public class AudioClipAssetMap : AssetMapBase
    {
    }

    /// <summary>
    /// 字体资源映射表。存储 key × 语言 → TMP_FontAsset / Font 路径的映射。
    /// 加载时按需转型（TMP_FontAsset 或 Font）。
    /// <see cref="Localization.GetAsset{TMP_FontAsset}"/> / <see cref="GetAsset{Font}"/> 通过此表查询路径后加载。
    /// </summary>
    [CreateAssetMenu(fileName = "FontAssetMap", menuName = "CLocalization/Font Asset Map", order = 12)]
    public class FontAssetMap : AssetMapBase
    {
    }
}
