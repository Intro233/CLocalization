using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CLocalization
{
    /// <summary>
    /// 本地化系统静态入口。提供文本查询、资源加载、语言切换等核心 API。
    /// 采用静态 API 风格，全局任意位置可通过 Localization.Get(...) 直接访问。
    /// </summary>
    public static class Localization
    {
        // ---------- 内部状态 ----------

        /// <summary>当前加载的配置。</summary>
        private static LocalizationSettings _settings;

        /// <summary>当前使用的加载器。</summary>
        private static ILocalizationLoader _loader;

        /// <summary>当前语言代码。</summary>
        private static string _currentCode;

        /// <summary>当前语言数据（当前语言对应的 LocaleData）。</summary>
        private static LocaleData _currentLocale;

        /// <summary>默认语言数据（用于缺失 key 回退）。</summary>
        private static LocaleData _defaultLocale;

        /// <summary>当前语言的区域信息（用于日期/货币/数字格式化）。</summary>
        private static CultureInfo _currentCulture;

        /// <summary>当前语言信息缓存（ApplyLanguage 成功时赋值，避免 CurrentLanguage 每次 O(n) 查找）。</summary>
        private static LanguageInfo _currentLanguageInfo;

        /// <summary>语言切换请求序号（单调递增）。用于异步切换时取消过期请求，避免竞态。</summary>
        private static long _applyEpoch;

        /// <summary>资源路径→已加载资源的缓存（避免重复 Resources.Load）。key = path，value = Object。</summary>
        private static readonly Dictionary<string, Object> _assetCache = new Dictionary<string, Object>();

        // ---------- 事件 ----------

        /// <summary>语言切换事件。Localize 组件订阅此事件以自动刷新显示。</summary>
        public static event Action<LanguageInfo> OnLanguageChanged;

        // ---------- 状态访问 ----------

        /// <summary>是否已初始化。</summary>
        public static bool IsInitialized => _settings != null;

        /// <summary>当前语言代码。</summary>
        public static string CurrentLanguageCode => _currentCode;

        /// <summary>当前语言信息（找不到返回 null）。直接返回缓存，O(1)。</summary>
        public static LanguageInfo CurrentLanguage => _currentLanguageInfo;

        /// <summary>当前语言的区域信息（格式化用）。</summary>
        public static CultureInfo CurrentCulture => _currentCulture ?? CultureInfo.InvariantCulture;

        /// <summary>可用语言列表（只读）。</summary>
        public static IReadOnlyList<LanguageInfo> AvailableLanguages
        {
            get { return _settings != null ? _settings.Languages : Array.Empty<LanguageInfo>(); }
        }

        /// <summary>当前加载的配置。</summary>
        public static LocalizationSettings Settings => _settings;

        // ---------- 初始化 ----------

        /// <summary>
        /// 使用指定配置初始化本地化系统。
        /// 根据 settings.AssetLoadMode 自动选择对应 Loader（Resources / StreamingAssets）。
        /// 若需自定义 Loader（如 Addressables / 远端），用 <see cref="Initialize(LocalizationSettings, ILocalizationLoader)"/> 重载手动注入。
        /// 初始化按优先级解析初始语言（上次选择 → 系统语言 → 默认语言）。
        /// </summary>
        public static void Initialize(LocalizationSettings settings)
        {
            if (settings == null)
            {
                LocalizationLog.Error("初始化失败：LocalizationSettings 为空。");
                return;
            }

            // 根据配置的加载方式选择内置 Loader
            ILocalizationLoader loader = CreateDefaultLoader(settings);
            Initialize(settings, loader);
        }

        /// <summary>
        /// 根据 AssetLoadMode 创建对应内置 Loader（注入 Settings 中配置的路径）。
        /// </summary>
        private static ILocalizationLoader CreateDefaultLoader(LocalizationSettings settings)
        {
            switch (settings.AssetLoadMode)
            {
                case AssetLoadMode.StreamingAssets:
                    return new StreamingAssetsLocalizationLoader(settings.LocalesPath, settings.AssetsPath);
                case AssetLoadMode.Resources:
                default:
                    return new ResourcesLocalizationLoader(settings.LocalesPath, settings.AssetsPath);
            }
        }

        /// <summary>
        /// 使用指定配置与自定义加载器初始化（用于接入 Addressables 或热更新实现）。
        /// </summary>
        public static void Initialize(LocalizationSettings settings, ILocalizationLoader loader)
        {
            if (settings == null)
            {
                LocalizationLog.Error("初始化失败：LocalizationSettings 为空。");
                return;
            }
            if (loader == null)
            {
                LocalizationLog.Error("初始化失败：ILocalizationLoader 为空。");
                return;
            }

            // 重置所有静态状态，避免再次 Initialize（换 Loader/Settings）时旧 _defaultLocale 等残留
            ResetState();
            _settings = settings;
            _loader = loader;
            LocalizationLog.LogWarnings = settings.LogMissingKeys;

            // 收集可用语言代码（以 Settings 配置为准）
            var codes = CollectAvailableCodes(settings);

            // 解析初始语言并加载。
            string initialCode = LocalizationPrefs.ResolveInitialLanguage(settings, codes);
            ApplyLanguage(initialCode, persist: false, fireEvent: true);

            // 初始化兜底：若初始语言加载失败，回退到默认语言；再失败则遍历可用列表。
            if (string.IsNullOrEmpty(_currentCode))
            {
                LocalizationLog.Warning($"初始语言 \"{initialCode}\" 加载失败，尝试回退到默认语言。");
                ApplyLanguage(settings.DefaultLanguageCode, persist: false, fireEvent: true);
            }
            if (string.IsNullOrEmpty(_currentCode))
            {
                foreach (var c in codes)
                {
                    ApplyLanguage(c, persist: false, fireEvent: true);
                    if (!string.IsNullOrEmpty(_currentCode)) break;
                }
            }

            LogInitResult();
        }

        /// <summary>
        /// 【异步】使用指定配置初始化（根据 AssetLoadMode 自动选 Loader）。
        /// 适用于 Android + StreamingAssets 等同步加载不可用的场景。
        /// </summary>
        public static async UniTask<bool> InitializeAsync(LocalizationSettings settings)
        {
            if (settings == null)
            {
                LocalizationLog.Error("初始化失败：LocalizationSettings 为空。");
                return false;
            }
            return await InitializeAsync(settings, CreateDefaultLoader(settings));
        }

        /// <summary>
        /// 【异步】使用指定配置与自定义加载器初始化。
        /// 异步加载初始语言与默认回退语言，全程主线程应用状态。
        /// </summary>
        public static async UniTask<bool> InitializeAsync(LocalizationSettings settings, ILocalizationLoader loader)
        {
            if (settings == null || loader == null)
            {
                LocalizationLog.Error("异步初始化失败：settings 或 loader 为空。");
                return false;
            }

            ResetState();
            _settings = settings;
            _loader = loader;
            LocalizationLog.LogWarnings = settings.LogMissingKeys;

            var codes = CollectAvailableCodes(settings);
            string initialCode = LocalizationPrefs.ResolveInitialLanguage(settings, codes);

            // 异步应用初始语言（含异步预加载默认语言）
            bool ok = await ApplyLanguageAsync(initialCode, persist: false, fireEvent: true);
            if (!ok)
            {
                LocalizationLog.Warning($"初始语言 \"{initialCode}\" 异步加载失败，尝试默认语言。");
                ok = await ApplyLanguageAsync(settings.DefaultLanguageCode, persist: false, fireEvent: true);
            }
            if (!ok)
            {
                foreach (var c in codes)
                {
                    if (await ApplyLanguageAsync(c, persist: false, fireEvent: true)) break;
                }
            }

            LogInitResult();
            return !string.IsNullOrEmpty(_currentCode);
        }

        /// <summary>重置所有运行时静态状态（再次 Initialize 前调用，避免旧数据残留）。</summary>
        private static void ResetState()
        {
            _currentCode = null;
            _currentLocale = null;
            _defaultLocale = null;
            _currentCulture = null;
            _currentLanguageInfo = null;
            _applyEpoch = 0;
        }

        /// <summary>从 Settings 收集可用语言代码列表。</summary>
        private static List<string> CollectAvailableCodes(LocalizationSettings settings)
        {
            var codes = new List<string>();
            for (int i = 0; i < settings.Languages.Count; i++)
            {
                if (settings.Languages[i] != null && settings.Languages[i].IsValid)
                {
                    codes.Add(settings.Languages[i].Code);
                }
            }
            return codes;
        }

        /// <summary>输出初始化结果日志。</summary>
        private static void LogInitResult()
        {
            if (string.IsNullOrEmpty(_currentCode))
            {
                LocalizationLog.Error("所有语言数据加载失败，本地化系统处于降级状态（查询将返回 key 本身）。请检查语言目录及 Settings 配置。");
            }
            else
            {
                LocalizationLog.Info($"本地化系统已初始化，当前语言: {_currentCode}");
            }
        }

        // ---------- 文本查询 ----------

        /// <summary>
        /// 获取 key 对应的本地化文本（不带参数）。
        /// 查询失败时按回退策略处理：回退默认语言 → 回退 key 字符串本身。
        /// 注意：返回值始终非 null（失败时返回 key 本身），如需区分"查到"与"回退"，用带 out found 的重载。
        /// </summary>
        public static string Get(string key)
        {
            return Resolve(key, out _);
        }

        /// <summary>
        /// 获取 key 对应的本地化文本，并通过 found 标识是否真正命中（而非回退到 key 字符串）。
        /// found=true 表示在当前语言或默认语言中查到；found=false 表示最终回退返回 key 本身。
        /// </summary>
        public static string Get(string key, out bool found)
        {
            return Resolve(key, out found);
        }

        /// <summary>
        /// 获取 key 对应的本地化文本并执行参数插值（{0}/{1} ... 占位符）。
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string raw = Resolve(key, out _);
            if (args == null || args.Length == 0) return raw;
            return LocalizationFormatter.Format(raw, _currentCulture, args);
        }

        /// <summary>
        /// 获取 key 对应的本地化文本并执行【命名占位符】插值（{name}/{count} ... 占位符）。
        /// 模板如 "Hello, {name}! You have {count} items."。
        /// 字典中不存在的占位符原样保留。
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <param name="namedArgs">命名占位符的值（key 为占位符名称，不含大括号）</param>
        public static string Get(string key, IDictionary<string, object> namedArgs)
        {
            string raw = Resolve(key, out _);
            if (namedArgs == null || namedArgs.Count == 0) return raw;
            return LocalizationFormatter.FormatNamed(raw, _currentCulture, namedArgs);
        }

        /// <summary>
        /// 获取 key 对应的本地化文本并执行【命名占位符】插值，参数通过匿名对象的属性提供。
        /// 用法：<c>Localization.Get("greet", new { name = "Player", count = 3 })</c>，对应模板 "Hi {name}, {count} items"。
        /// 内部通过反射读取对象属性构造字典。
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <param name="argsObject">含命名属性的对象（如匿名对象）</param>
        public static string Get(string key, object argsObject)
        {
            string raw = Resolve(key, out _);
            if (argsObject == null) return raw;
            var dict = ToNamedArgs(argsObject);
            if (dict.Count == 0) return raw;
            return LocalizationFormatter.FormatNamed(raw, _currentCulture, dict);
        }

        /// <summary>
        /// 获取 key 对应的本地化文本并执行【混合占位符】插值：命名占位 {name} + 位置占位 {0} 可共存。
        /// 例如模板 "Hello {name}, you have {0} items"。
        /// </summary>
        public static string Get(string key, IDictionary<string, object> namedArgs, params object[] positionalArgs)
        {
            string raw = Resolve(key, out _);
            return LocalizationFormatter.FormatMixed(raw, _currentCulture, namedArgs, positionalArgs);
        }

        /// <summary>把对象（含匿名对象）的公开属性转为命名参数字典（反射）。</summary>
        private static Dictionary<string, object> ToNamedArgs(object argsObject)
        {
            var dict = new Dictionary<string, object>();
            if (argsObject == null) return dict;
            // 若传入的本身就是字典，直接返回（便于调用方两种方式都支持）
            if (argsObject is IDictionary<string, object> existing)
            {
                return new Dictionary<string, object>(existing);
            }
            var type = argsObject.GetType();
            foreach (var prop in type.GetProperties())
            {
                try
                {
                    object val = prop.GetValue(argsObject, null);
                    dict[prop.Name] = val;
                }
                catch
                {
                    // 读取失败的属性跳过
                }
            }
            return dict;
        }

        /// <summary>
        /// 尝试获取 key 文本，不触发回退。成功（在【当前语言】中命中）返回 true。
        /// 注意：本方法的语义与 <see cref="Get(string)"/> 不同——Get 会回退到默认语言甚至返回 key 本身，
        /// 而 TryGet 只检查当前语言，不回退。若需要带回退且知晓是否命中，请用 Get(key, out found)。
        /// </summary>
        public static bool TryGet(string key, out string value)
        {
            if (!IsInitialized)
            {
                value = null;
                return false;
            }
            if (_currentLocale != null && _currentLocale.TryGetEntry(key, out value))
            {
                return true;
            }
            value = null;
            return false;
        }

        // ---------- 资源查询 ----------

        /// <summary>
        /// 加载当前语言下某 key 对应的本地化资源（Sprite/AudioClip/Font 等）。
        /// 按 T 类型从对应的资源映射表查询（SpriteAssetMap/AudioClipAssetMap/FontAssetMap）。
        /// 找不到返回 null。资源不做语言回退（通常需精确匹配）。
        /// </summary>
        public static T GetAsset<T>(string key) where T : Object
        {
            if (!IsInitialized || _settings == null || string.IsNullOrEmpty(_currentCode) || string.IsNullOrEmpty(key))
            {
                return null;
            }
            return LookupAsset<T>(key);
        }

        /// <summary>按类型从对应映射表查询当前语言资源路径，再通过 Loader 加载（带缓存）。</summary>
        private static T LookupAsset<T>(string key) where T : Object
        {
            System.Type t = typeof(T);
            AssetMapBase map = null;

            if (t == typeof(Sprite))
            {
                map = _settings.SpriteMap;
            }
            else if (t == typeof(AudioClip))
            {
                map = _settings.AudioMap;
            }
            else if (t == typeof(TMPro.TMP_FontAsset) || t == typeof(Font))
            {
                map = _settings.FontMap;
            }

            if (map == null) return null;

            // 1. 从映射表查路径
            if (!map.LookupPath(key, _currentCode, out var path, out var pathType))
            {
                return null;
            }

            // 2. 通过 Loader 加载（LoadAssetByPath 内部带缓存）
            return LoadAssetByPath<T>(path, pathType);
        }

        /// <summary>清除资源缓存（热更新资源后调用，强制重新加载）。</summary>
        public static void ClearAssetCache()
        {
            _assetCache.Clear();
        }

        /// <summary>
        /// 按资源路径加载资源（带缓存，供 LanguageInfo 字体/国旗等全局配置使用）。
        /// </summary>
        public static T LoadAssetByPath<T>(string path, AssetPathType pathType) where T : Object
        {
            if (string.IsNullOrEmpty(path)) return null;
            string cacheKey = pathType + ":" + path;
            if (_assetCache.TryGetValue(cacheKey, out var cached))
            {
                return cached as T;
            }
            T asset = _loader.LoadAssetByPath<T>(path, pathType);
            if (asset != null) _assetCache[cacheKey] = asset;
            return asset;
        }

        // ---------- 语言切换 ----------

        /// <summary>按语言代码切换当前语言。切换成功会触发 OnLanguageChanged 事件。</summary>
        public static void SetLanguage(string languageCode)
        {
            if (!IsInitialized)
            {
                LocalizationLog.Warning("未初始化，无法切换语言。请先调用 Localization.Initialize。");
                return;
            }
            if (string.IsNullOrEmpty(languageCode))
            {
                LocalizationLog.Warning("语言代码为空，无法切换。");
                return;
            }
            if (!_settings.HasLanguage(languageCode))
            {
                LocalizationLog.Warning($"语言 \"{languageCode}\" 不在可用列表中，已忽略。");
                return;
            }
            if (languageCode == _currentCode) return;

            // 递增请求序号，使进行中的异步切换请求失效（避免异步续体回来覆盖本次同步切换）
            System.Threading.Interlocked.Increment(ref _applyEpoch);
            ApplyLanguage(languageCode, persist: _settings.PersistLanguageChoice, fireEvent: true);
        }

        /// <summary>切换到默认语言（回退语言）。</summary>
        public static void SetDefaultLanguage()
        {
            if (!IsInitialized) return;
            SetLanguage(_settings.DefaultLanguageCode);
        }

        /// <summary>
        /// 【异步】按语言代码切换当前语言。适用于 Addressables / 热更新等需要等待加载的场景。
        /// 加载在后台进行，状态写入与事件触发在主线程完成（保证线程安全）。
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>是否切换成功（加载失败返回 false）。</returns>
        public static async UniTask<bool> SetLanguageAsync(string languageCode)
        {
            if (!IsInitialized)
            {
                LocalizationLog.Warning("未初始化，无法切换语言。请先调用 Localization.Initialize。");
                return false;
            }
            if (string.IsNullOrEmpty(languageCode))
            {
                LocalizationLog.Warning("语言代码为空，无法切换。");
                return false;
            }
            if (!_settings.HasLanguage(languageCode))
            {
                LocalizationLog.Warning($"语言 \"{languageCode}\" 不在可用列表中，已忽略。");
                return false;
            }
            if (languageCode == _currentCode)
            {
                // 即使目标等于当前语言，也递增 epoch 取消进行中的异步切换请求，
                // 防止旧的异步续体回来覆盖（用户显式选择了"保持当前"语义）
                System.Threading.Interlocked.Increment(ref _applyEpoch);
                return true;
            }

            return await ApplyLanguageAsync(languageCode, persist: _settings.PersistLanguageChoice, fireEvent: true);
        }

        /// <summary>【异步】切换到默认语言。</summary>
        public static UniTask<bool> SetDefaultLanguageAsync()
        {
            if (!IsInitialized) return UniTask.FromResult(false);
            return SetLanguageAsync(_settings.DefaultLanguageCode);
        }

        // ---------- 内部实现 ----------

        /// <summary>
        /// 应用某语言：加载对应 LocaleData，更新区域信息，按需持久化与触发事件。
        /// 加载失败时保持旧状态不变，不持久化、不触发事件，避免坏语言代码被记住导致下次启动死循环。
        /// </summary>
        private static void ApplyLanguage(string code, bool persist, bool fireEvent)
        {
            LocaleData locale;
            try
            {
                locale = _loader.LoadLocale(code);
            }
            catch (System.NotSupportedException ex)
            {
                // 某些 Loader（如 StreamingAssets 在 Android）不支持同步加载，降级告警而非崩溃。
                // 调用方应改用 SetLanguageAsync / ApplyLanguageAsync。
                LocalizationLog.Warning($"同步加载语言 \"{code}\" 不受支持（{ex.Message}）。请使用 SetLanguageAsync。");
                return;
            }
            if (locale == null)
            {
                // 加载失败：保持旧语言状态（code/locale/culture 都不动），不持久化、不触发事件。
                // 这样可避免把一个加载失败的语言代码写入 PlayerPrefs，导致下次启动恢复坏 code 再失败的死循环。
                LocalizationLog.Warning($"语言 \"{code}\" 数据加载失败，已保持当前语言 \"{_currentCode}\"。");
                return;
            }
            // 同步预加载默认语言（带异常保护：Android+SA 同步 LoadLocale 会抛 NotSupportedException，此处降级不崩）
            PreloadDefaultLocaleSync(code);
            ApplyLoadedLocale(code, locale, persist, fireEvent);
        }

        /// <summary>
        /// 【同步】预加载默认语言（用于回退）。同步 ApplyLanguage 路径使用。
        /// 用 try-catch 容错：不支持同步加载的 Loader（Android+SA）会抛 NotSupportedException，此时降级不预加载。
        /// </summary>
        private static void PreloadDefaultLocaleSync(string currentCode)
        {
            if (_defaultLocale != null || !_settings.FallbackToDefaultLanguage) return;
            string defaultCode = _settings.DefaultLanguageCode;
            if (string.IsNullOrEmpty(defaultCode) || defaultCode == currentCode) return;
            try
            {
                _defaultLocale = _loader.LoadLocale(defaultCode);
            }
            catch (System.NotSupportedException)
            {
                // Android+SA 等不支持同步加载，跳过预加载（回退语言暂不可用，不崩溃）
                LocalizationLog.Warning($"同步预加载默认语言 \"{defaultCode}\" 不受支持，回退语言暂不可用。建议用 InitializeAsync / SetLanguageAsync。");
            }
            catch (Exception ex)
            {
                LocalizationLog.Warning($"预加载默认语言 \"{defaultCode}\" 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 【异步】应用某语言：异步加载 locale（后台），切回主线程应用状态。
        /// 状态写入与事件触发保证在主线程执行（线程安全）。
        /// </summary>
        /// <returns>是否切换成功（加载失败或被后续请求取消返回 false）。</returns>
        private static async UniTask<bool> ApplyLanguageAsync(string code, bool persist, bool fireEvent)
        {
            // 分配本次请求的序号；任何更新的请求会让本序号过期
            long myEpoch = System.Threading.Interlocked.Increment(ref _applyEpoch);

            // 1) 异步加载语言数据（可能在后台线程，如 Addressables / StreamingAssets 文件读取）
            LocaleData locale;
            try
            {
                locale = await _loader.LoadLocaleAsync(code);
            }
            catch (Exception ex)
            {
                LocalizationLog.Error($"异步加载语言 \"{code}\" 异常: {ex.Message}");
                return false;
            }

            // 2) 切回主线程后再应用状态（保证 _currentLocale 等可变字段的主线程安全）
            await UniTask.SwitchToMainThread();

            // 竞态取消：若期间有更新的切换请求发起，丢弃本次结果（最后发起者胜）
            if (myEpoch != _applyEpoch)
            {
                LocalizationLog.Info($"语言 \"{code}\" 的异步加载已完成，但被更新的切换请求取代，丢弃。");
                return false;
            }

            if (locale == null)
            {
                LocalizationLog.Warning($"语言 \"{code}\" 数据加载失败，已保持当前语言 \"{_currentCode}\"。");
                return false;
            }

            // 3) 异步预加载默认语言（Android+SA 同步 LoadLocale 会抛异常，必须异步）
            await PreloadDefaultLocaleAsync(code, myEpoch);
            // 预加载期间若有新请求，同样取消
            if (myEpoch != _applyEpoch) return false;

            ApplyLoadedLocale(code, locale, persist, fireEvent);
            return true;
        }

        /// <summary>
        /// 异步预加载默认语言（用于回退）。Android+SA 同步 LoadLocale 不可用，故异步加载。
        /// 仅在首次需要时（_defaultLocale == null）执行。
        /// </summary>
        private static async UniTask PreloadDefaultLocaleAsync(string currentCode, long myEpoch)
        {
            if (_defaultLocale != null || !_settings.FallbackToDefaultLanguage) return;
            string defaultCode = _settings.DefaultLanguageCode;
            if (string.IsNullOrEmpty(defaultCode)) return;
            if (defaultCode == currentCode)
            {
                // 当前语言即默认语言，加载时一并缓存
                return;
            }
            try
            {
                LocaleData def = await _loader.LoadLocaleAsync(defaultCode);
                // 仅当仍是本次请求时才写入，避免被并发请求污染
                if (myEpoch == _applyEpoch)
                {
                    _defaultLocale = def;
                }
            }
            catch (Exception ex)
            {
                LocalizationLog.Warning($"异步预加载默认语言 \"{defaultCode}\" 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 把已加载的 locale 应用到当前状态（更新 code/locale/culture/languageInfo，持久化，触发事件）。
        /// 同步/异步路径共用。必须在主线程调用。
        /// 注意：默认语言的预加载由调用方负责（同步 ApplyLanguage 用 PreloadDefaultLocaleSync，异步用 PreloadDefaultLocaleAsync），
        /// 此处仅处理「当前语言即默认语言」的快捷赋值。
        /// </summary>
        private static void ApplyLoadedLocale(string code, LocaleData locale, bool persist, bool fireEvent)
        {
            _currentCode = code;
            _currentLocale = locale;
            _currentCulture = LocalizationFormatter.GetCulture(code);
            // 缓存当前语言信息，供 CurrentLanguage O(1) 访问
            _currentLanguageInfo = _settings.FindLanguage(code);

            // 若当前语言即默认语言，直接缓存引用（其他情况的预加载由调用方处理）
            if (_defaultLocale == null && _settings.FallbackToDefaultLanguage)
            {
                string defaultCode = _settings.DefaultLanguageCode;
                if (!string.IsNullOrEmpty(defaultCode) && defaultCode == code)
                {
                    _defaultLocale = _currentLocale;
                }
            }

            // 持久化（仅成功切换时才写入，避免坏 code 被记住）
            if (persist)
            {
                LocalizationPrefs.SaveLanguage(_currentCode);
            }

            // 触发事件（Localize 组件据此刷新）
            if (fireEvent)
            {
                try
                {
                    OnLanguageChanged?.Invoke(CurrentLanguage);
                }
                catch (Exception ex)
                {
                    LocalizationLog.Error($"OnLanguageChanged 事件订阅者抛出异常: {ex}");
                }
            }
        }

        /// <summary>
        /// 解析文本：当前语言 → 回退默认语言 → 返回 key 本身。
        /// 把当前/默认 locale 先捕获到局部变量，避免在解析过程中被并发切换语言读到半途替换的引用。
        /// </summary>
        private static string Resolve(string key, out bool found)
        {
            found = false;
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!IsInitialized)
            {
                // 未初始化直接返回 key，避免阻塞调用方
                return key;
            }

            // 局部捕获：一次解析内使用稳定的引用，避免主线程内的事件回调嵌套切换语言导致引用中途变化
            LocaleData current = _currentLocale;
            LocaleData fallback = _defaultLocale;

            // 当前语言命中
            if (current != null && current.TryGetEntry(key, out var value))
            {
                found = true;
                return value;
            }

            // 回退默认语言
            if (_settings.FallbackToDefaultLanguage && fallback != null && fallback != current)
            {
                if (fallback.TryGetEntry(key, out value))
                {
                    found = true;
                    return value;
                }
            }

            // 全部失败
            if (_settings.LogMissingKeys)
            {
                LocalizationLog.Warning($"缺少本地化词条 key=\"{key}\"（当前语言 {_currentCode}）。");
            }
            return key;
        }
    }
}
