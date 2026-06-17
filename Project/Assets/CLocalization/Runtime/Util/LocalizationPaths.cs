namespace CLocalization
{
    /// <summary>
    /// Resources 路径约定。所有语言数据文件按统一目录结构存放，便于 Loader 定位。
    /// 结构：Resources/CLocalization/Locales/{语言代码}.json
    /// 本地化资源（Sprite/Audio/Font）按 Resources/CLocalization/Assets/{语言代码}/{key} 组织。
    /// </summary>
    public static class LocalizationPaths
    {
        /// <summary>Resources 根目录（不含 "Resources/" 前缀，加载时使用）。</summary>
        public const string Root = "CLocalization";

        /// <summary>语言文本 JSON 所在的 Resources 子目录。</summary>
        public const string LocalesFolder = "CLocalization/Locales";

        /// <summary>本地化资源（Sprite/Audio/Font）所在的 Resources 子目录。</summary>
        public const string AssetsFolder = "CLocalization/Assets";

        /// <summary>语言文件扩展名。</summary>
        public const string LocaleExtension = ".json";

        /// <summary>
        /// 拼接某语言的 JSON 资源加载路径（传入 Resources.Load）。
        /// 例如 code=zh-CN 时返回 "CLocalization/Locales/zh-CN"。
        /// </summary>
        public static string GetLocalePath(string languageCode)
        {
            return LocalesFolder + "/" + languageCode;
        }

        /// <summary>
        /// 拼接某语言某 key 的本地化资源加载路径（传入 Resources.Load）。
        /// 例如 code=en-US、key=ui/logo 时返回 "CLocalization/Assets/en-US/ui/logo"。
        /// </summary>
        public static string GetAssetPath(string languageCode, string key)
        {
            return AssetsFolder + "/" + languageCode + "/" + key;
        }
    }
}
