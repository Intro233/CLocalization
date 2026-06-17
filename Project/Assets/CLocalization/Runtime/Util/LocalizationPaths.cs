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
        /// 结果按 languageCode 缓存，避免热路径反复拼接。
        /// </summary>
        public static string GetLocalePath(string languageCode)
        {
            if (languageCode == null) return LocalesFolder + "/";
            // 缓存：语言代码种类有限，复用率高
            if (_localePathCache.TryGetValue(languageCode, out var cached))
            {
                return cached;
            }
            string path = string.Concat(LocalesFolder, "/", languageCode);
            _localePathCache[languageCode] = path;
            return path;
        }

        /// <summary>locale 路径缓存（languageCode → 完整路径）。</summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> _localePathCache =
            new System.Collections.Generic.Dictionary<string, string>();

        /// <summary>
        /// 拼接某语言某 key 的本地化资源加载路径（传入 Resources.Load）。
        /// 例如 code=en-US、key=ui/logo 时返回 "CLocalization/Assets/en-US/ui/logo"。
        /// 用 string.Concat 减少 3 段拼接的中间分配。
        /// </summary>
        public static string GetAssetPath(string languageCode, string key)
        {
            // string.Concat 一次性分配，比 + 多次拼接少 2 次中间字符串分配
            return string.Concat(AssetsFolder, "/", languageCode, "/", key);
        }
    }
}
