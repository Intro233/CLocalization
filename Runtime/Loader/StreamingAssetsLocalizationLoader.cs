using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace CLocalization
{
    /// <summary>
    /// 基于 StreamingAssets 的本地化加载器。
    /// 文本：从 StreamingAssets/{localesPath}/{code}.json 加载（原始 JSON 文件，可外部修改/热更新）。
    /// 资源：StreamingAssets 无法加载 Unity Object（Sprite/Audio/Font），LoadAsset 返回 null + 告警。
    ///
    /// 平台差异：
    ///  - 编辑器 / 桌面 / iOS：StreamingAssets 是真实文件，LoadLocale 可同步 File.ReadAllText。
    ///  - Android：StreamingAssets 在 APK 内压缩，必须用 UnityWebRequest 异步读取，同步 LoadLocale 抛 NotSupportedException。
    /// 因此 Android 平台请用 Localization.SetLanguageAsync 切换语言。
    /// </summary>
    public class StreamingAssetsLocalizationLoader : ILocalizationLoader
    {
        /// <summary>已加载语言文本的缓存，避免重复读盘与解析。</summary>
        private readonly Dictionary<string, LocaleData> _localeCache = new Dictionary<string, LocaleData>();

        /// <summary>语言文本 JSON 的子路径（相对 StreamingAssets 根，如 CLocalization/Locales）。</summary>
        private readonly string _localesPath;

        /// <summary>本地化资源子路径（StreamingAssets 模式仅文本，此字段保留供自定义扩展）。</summary>
        private readonly string _assetsPath;

        /// <summary>无参构造：使用 LocalizationPaths 默认路径。</summary>
        public StreamingAssetsLocalizationLoader()
            : this(LocalizationPaths.LocalesFolder, LocalizationPaths.AssetsFolder) { }

        /// <summary>带路径配置的构造（由 Localization.Initialize 工厂调用）。</summary>
        public StreamingAssetsLocalizationLoader(string localesPath, string assetsPath)
        {
            _localesPath = string.IsNullOrEmpty(localesPath) ? LocalizationPaths.LocalesFolder : localesPath;
            _assetsPath = string.IsNullOrEmpty(assetsPath) ? LocalizationPaths.AssetsFolder : assetsPath;
        }

        // ---------- 同步加载 ----------

        /// <summary>
        /// 【同步】加载语言文本。
        /// 编辑器/桌面/iOS：直接 File.ReadAllText。
        /// Android：StreamingAssets 在 APK 内压缩，无法同步读取，抛 NotSupportedException（请用 LoadLocaleAsync）。
        /// </summary>
        public LocaleData LoadLocale(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return null;

            if (_localeCache.TryGetValue(languageCode, out var cached))
            {
                return cached;
            }

            // Android 平台 StreamingAssets 在 APK 内，不可同步读
            if (Application.platform == RuntimePlatform.Android)
            {
                throw new System.NotSupportedException(
                    "Android 平台 StreamingAssets 不可同步加载，请用 LoadLocaleAsync / Localization.SetLanguageAsync。");
            }

            // 编辑器/桌面/iOS：StreamingAssets 是真实文件
            string filePath = GetLocaleFilePath(languageCode);
            if (!System.IO.File.Exists(filePath))
            {
                LocalizationLog.Warning($"未找到语言文件: {filePath}");
                return null;
            }

            string json;
            try
            {
                json = System.IO.File.ReadAllText(filePath);
            }
            catch (System.Exception ex)
            {
                LocalizationLog.Error($"读取语言文件失败: {filePath}  错误: {ex.Message}");
                return null;
            }

            return ParseAndCache(languageCode, json, filePath);
        }

        /// <summary>
        /// 【同步】加载本地化资源。
        /// StreamingAssets 是原始字节，无法加载 Unity Object（Sprite/Audio/Font），返回 null + 告警。
        /// 需本地化资源请用 Resources 模式或自定义 Loader。
        /// </summary>
        public T LoadAsset<T>(string key, string languageCode) where T : Object
        {
            LocalizationLog.Warning(
                $"StreamingAssets 模式不支持加载 Unity 资源（Sprite/Audio/Font）。key=\"{key}\" " +
                "如需本地化资源，请切换为 Resources 模式或实现自定义 Loader。");
            return null;
        }

        /// <summary>扫描 StreamingAssets 语言目录，返回文件名（语言代码）。</summary>
        public IReadOnlyList<string> GetAvailableLanguageCodes()
        {
            // Android 平台无法同步枚举 StreamingAssets，返回空（运行时依赖 Settings 配置）
            var codes = new List<string>();
            if (Application.platform == RuntimePlatform.Android)
            {
                return codes;
            }

            string dir = System.IO.Path.Combine(Application.streamingAssetsPath, _localesPath);
            if (!System.IO.Directory.Exists(dir)) return codes;

            var files = System.IO.Directory.GetFiles(dir, "*" + LocalizationPaths.LocaleExtension);
            foreach (var file in files)
            {
                if (file.EndsWith(LocalizationPaths.LocaleExtension, System.StringComparison.OrdinalIgnoreCase))
                {
                    codes.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }
            codes.Sort();
            return codes;
        }

        // ---------- 异步加载（跨平台，统一用 UnityWebRequest） ----------

        /// <summary>
        /// 【异步】加载语言文本。跨平台兼容：编辑器/桌面用 File.ReadAllText，Android 用 UnityWebRequest。
        /// </summary>
        public async UniTask<LocaleData> LoadLocaleAsync(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return null;

            if (_localeCache.TryGetValue(languageCode, out var cached))
            {
                return cached;
            }

            string filePath = GetLocaleFilePath(languageCode);

            // Android 平台用 UnityWebRequest 读取 APK 内的 StreamingAssets
            if (Application.platform == RuntimePlatform.Android)
            {
                string json = await ReadStreamingAssetAsync(filePath);
                if (string.IsNullOrEmpty(json))
                {
                    LocalizationLog.Warning($"未找到语言文件: {filePath}");
                    return null;
                }
                return ParseAndCache(languageCode, json, filePath);
            }

            // 编辑器/桌面/iOS：可同步读文件，但为统一异步语义用 Task.Run 读取
            if (!System.IO.File.Exists(filePath))
            {
                LocalizationLog.Warning($"未找到语言文件: {filePath}");
                return null;
            }
            string text;
            try
            {
                // 切到线程池读取文件，避免主线程 I/O 阻塞
                text = await UniTask.RunOnThreadPool(() => System.IO.File.ReadAllText(filePath));
            }
            catch (System.Exception ex)
            {
                LocalizationLog.Error($"读取语言文件失败: {filePath}  错误: {ex.Message}");
                return null;
            }
            // 切回主线程再解析与写缓存（_localeCache 是非线程安全的 Dictionary，必须主线程访问）
            await UniTask.SwitchToMainThread();
            return ParseAndCache(languageCode, text, filePath);
        }

        /// <summary>
        /// 【异步】加载本地化资源。StreamingAssets 不支持 Unity Object，返回 null + 告警。
        /// </summary>
        public UniTask<T> LoadAssetAsync<T>(string key, string languageCode) where T : Object
        {
            LocalizationLog.Warning(
                $"StreamingAssets 模式不支持加载 Unity 资源（Sprite/Audio/Font）。key=\"{key}\" " +
                "如需本地化资源，请切换为 Resources 模式或实现自定义 Loader。");
            return UniTask.FromResult<T>(null);
        }

        // ---------- 路径版资源加载（StreamingAssets 不支持，需自定义 Loader） ----------

        /// <summary>StreamingAssets 模式不支持按路径加载 Unity 资源。</summary>
        public T LoadAssetByPath<T>(string path, AssetPathType pathType) where T : Object
        {
            LocalizationLog.Warning(
                $"StreamingAssets 模式不支持加载 Unity 资源。路径: {path} " +
                "如需本地化资源，请实现自定义 Loader（AssetBundle/Addressables）。");
            return null;
        }

        /// <summary>StreamingAssets 模式不支持按路径加载 Unity 资源。</summary>
        public UniTask<T> LoadAssetByPathAsync<T>(string path, AssetPathType pathType) where T : Object
        {
            return UniTask.FromResult<T>(LoadAssetByPath<T>(path, pathType));
        }

        // ---------- 内部工具 ----------

        /// <summary>解析 JSON 并缓存，失败返回 null。</summary>
        private LocaleData ParseAndCache(string languageCode, string json, string filePath)
        {
            LocaleData data;
            try
            {
                data = LocaleData.FromJson(json);
            }
            catch (System.Exception ex)
            {
                LocalizationLog.Error($"解析语言文件失败: {filePath}  错误: {ex.Message}");
                return null;
            }
            _localeCache[languageCode] = data;
            return data;
        }

        /// <summary>用 UnityWebRequest 异步读取 StreamingAssets 文件内容（Android 跨平台兼容）。</summary>
        private static async UniTask<string> ReadStreamingAssetAsync(string uri)
        {
            try
            {
                using (var request = UnityWebRequest.Get(uri))
                {
                    await request.SendWebRequest().ToUniTask();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        return request.downloadHandler.text;
                    }
                    LocalizationLog.Error($"UnityWebRequest 读取失败: {uri}  result: {request.result}");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                LocalizationLog.Error($"UnityWebRequest 异常: {uri}  错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>拼接语言 JSON 的完整路径：{streamingAssetsPath}/{localesPath}/{code}.json。</summary>
        private string GetLocaleFilePath(string languageCode)
        {
            return string.Concat(Application.streamingAssetsPath, "/", _localesPath, "/", languageCode, LocalizationPaths.LocaleExtension);
        }

        /// <summary>清空已缓存的语言文本。</summary>
        public void ClearCache()
        {
            _localeCache.Clear();
        }
    }
}
