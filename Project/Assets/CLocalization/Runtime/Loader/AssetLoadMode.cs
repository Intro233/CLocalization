namespace CLocalization
{
    /// <summary>
    /// 本地化资源加载方式。决定运行时从哪个容器加载语言文本 JSON 与本地化资源。
    /// 在 <see cref="LocalizationSettings"/> 中配置，<see cref="Localization.Initialize"/> 据此自动选择 Loader。
    /// </summary>
    public enum AssetLoadMode
    {
        /// <summary>
        /// 从 Resources 容器加载（Resources.Load）。默认方式，零配置，随包打进。
        /// 支持文本（JSON）与 Unity 资源（Sprite/Audio/Font）。
        /// </summary>
        Resources = 0,

        /// <summary>
        /// 从 StreamingAssets 容器加载。文本为原始 JSON 文件，可被外部修改/热更新。
        /// 注意：StreamingAssets 仅支持【文本】本地化；Unity 资源（Sprite/Audio/Font）需用 Resources 或自定义 Loader。
        /// Android 平台必须用异步加载（SetLanguageAsync），同步 LoadLocale 会抛 NotSupportedException。
        /// </summary>
        StreamingAssets = 1,
    }
}
