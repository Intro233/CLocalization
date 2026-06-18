using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 基于 Resources 的本地化加载器（默认实现）。
    /// 文本：从 Resources/{localesPath}/{code}.json 加载（以 TextAsset 形式读取）。
    /// 资源：从 Resources/{assetsPath}/{code}/{key} 加载。
    /// 同步加载瞬时完成；异步方法用 <see cref="UniTask.FromResult"/> 包装同步结果，满足接口契约。
    /// 路径通过构造函数注入（来自 LocalizationSettings 的可配置路径），默认值取自 <see cref="LocalizationPaths"/>。
    /// </summary>
    public class ResourcesLocalizationLoader : ILocalizationLoader
    {
        /// <summary>已加载语言文本的缓存，避免重复读盘与解析。</summary>
        private readonly Dictionary<string, LocaleData> _localeCache = new Dictionary<string, LocaleData>();

        /// <summary>语言文本 JSON 的 Resources 子路径（不含 "Resources/" 前缀与扩展名）。</summary>
        private readonly string _localesPath;

        /// <summary>本地化资源的 Resources 子路径。</summary>
        private readonly string _assetsPath;

        /// <summary>无参构造：使用 LocalizationPaths 默认路径（向后兼容）。</summary>
        public ResourcesLocalizationLoader()
            : this(LocalizationPaths.LocalesFolder, LocalizationPaths.AssetsFolder) { }

        /// <summary>带路径配置的构造（由 Localization.Initialize 工厂调用）。</summary>
        /// <param name="localesPath">语言文本子路径，如 CLocalization/Locales</param>
        /// <param name="assetsPath">本地化资源子路径，如 CLocalization/Assets</param>
        public ResourcesLocalizationLoader(string localesPath, string assetsPath)
        {
            _localesPath = string.IsNullOrEmpty(localesPath) ? LocalizationPaths.LocalesFolder : localesPath;
            _assetsPath = string.IsNullOrEmpty(assetsPath) ? LocalizationPaths.AssetsFolder : assetsPath;
        }

        /// <summary>
        /// 加载指定语言的文本数据。优先读缓存，否则从 Resources 读取并反序列化。
        /// </summary>
        public LocaleData LoadLocale(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return null;

            // 命中缓存直接返回
            if (_localeCache.TryGetValue(languageCode, out var cached))
            {
                return cached;
            }

            // 按 Resources 路径约定加载（Resources.Load 不带扩展名）
            string path = GetLocalePath(languageCode);
            var textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
            {
                LocalizationLog.Warning($"未找到语言文件: Resources/{path}.json");
                return null;
            }

            LocaleData data;
            try
            {
                data = LocaleData.FromJson(textAsset.text);
            }
            catch (System.Exception ex)
            {
                LocalizationLog.Error($"解析语言文件失败: Resources/{path}.json  错误: {ex.Message}");
                return null;
            }

            // 反序列化完成后立即卸载 TextAsset 的原始字节（LocaleData 是独立托管对象，不依赖底层字节）。
            // UnloadAsset 仅释放该 TextAsset 的原生数据，不影响 LocaleData 及其他引用。
            Resources.UnloadAsset(textAsset);

            _localeCache[languageCode] = data;
            return data;
        }

        /// <summary>
        /// 加载指定语言、指定 key 的本地化资源。不做缓存（资源通常由调用方持有引用）。
        /// </summary>
        public T LoadAsset<T>(string key, string languageCode) where T : Object
        {
            if (string.IsNullOrEmpty(languageCode) || string.IsNullOrEmpty(key)) return null;

            string path = GetAssetPath(languageCode, key);
            return Resources.Load<T>(path);
        }

        /// <summary>
        /// 扫描 Resources 下语言目录，返回其中所有语言代码。
        /// </summary>
        public IReadOnlyList<string> GetAvailableLanguageCodes()
        {
            // Resources.LoadAll 返回目录下所有资源，文件名（去掉扩展名）即语言代码
            var assets = Resources.LoadAll<TextAsset>(_localesPath);
            var codes = new List<string>(assets.Length);
            foreach (var asset in assets)
            {
                if (asset != null && !string.IsNullOrEmpty(asset.name))
                {
                    codes.Add(asset.name);
                }
            }
            return codes;
        }

        // ---------- 路径拼接（使用注入的路径） ----------

        /// <summary>拼接语言 JSON 的 Resources 加载路径：{localesPath}/{code}。</summary>
        private string GetLocalePath(string languageCode)
        {
            return string.Concat(_localesPath, "/", languageCode);
        }

        /// <summary>拼接本地化资源的 Resources 加载路径：{assetsPath}/{code}/{key}。</summary>
        private string GetAssetPath(string languageCode, string key)
        {
            return string.Concat(_assetsPath, "/", languageCode, "/", key);
        }

        // ---------- 异步加载（Resources 是瞬时操作，用 UniTask.FromResult 包装同步结果） ----------

        /// <summary>
        /// 【异步】加载语言文本。Resources 加载是同步瞬时操作，直接包装同步结果。
        /// </summary>
        public UniTask<LocaleData> LoadLocaleAsync(string languageCode)
        {
            return UniTask.FromResult(LoadLocale(languageCode));
        }

        /// <summary>
        /// 【异步】加载本地化资源。Resources 加载是同步瞬时操作，直接包装同步结果。
        /// </summary>
        public UniTask<T> LoadAssetAsync<T>(string key, string languageCode) where T : Object
        {
            return UniTask.FromResult(LoadAsset<T>(key, languageCode));
        }

        /// <summary>清空已缓存的语言文本（切换 Loader 或热更新数据后调用）。</summary>
        public void ClearCache()
        {
            _localeCache.Clear();
        }
    }
}
