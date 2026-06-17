using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// 默认使用 <see cref="ResourcesLocalizationLoader"/>，并按优先级解析初始语言
        /// （上次选择 → 系统语言 → 默认语言）。
        /// </summary>
        public static void Initialize(LocalizationSettings settings)
        {
            Initialize(settings, new ResourcesLocalizationLoader());
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

            _settings = settings;
            _loader = loader;
            LocalizationLog.LogWarnings = settings.LogMissingKeys;

            // 收集可用语言代码（以 Settings 配置为准）
            var codes = new List<string>();
            for (int i = 0; i < settings.Languages.Count; i++)
            {
                if (settings.Languages[i] != null && settings.Languages[i].IsValid)
                {
                    codes.Add(settings.Languages[i].Code);
                }
            }

            // 解析初始语言并加载。初始化后触发一次事件，
            // 让所有「先于初始化激活」的组件能刷新（后激活的组件会在 OnEnable 时自行刷新）。
            string initialCode = LocalizationPrefs.ResolveInitialLanguage(settings, codes);
            ApplyLanguage(initialCode, persist: false, fireEvent: true);

            // 初始化兜底：若初始语言加载失败（ApplyLanguage 内部已 return，_currentCode 仍为 null），
            // 则尝试回退到默认语言；再失败则尝试可用列表中第一个能加载成功的语言。
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

            if (string.IsNullOrEmpty(_currentCode))
            {
                LocalizationLog.Error("所有语言数据加载失败，本地化系统处于降级状态（查询将返回 key 本身）。请检查 Resources/CLocalization/Locales 目录及 Settings 配置。");
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
        /// 找不到返回 null。资源加载不做回退（资源通常需精确匹配语言）。
        /// </summary>
        public static T GetAsset<T>(string key) where T : Object
        {
            if (!IsInitialized || _loader == null || string.IsNullOrEmpty(_currentCode))
            {
                return null;
            }
            return _loader.LoadAsset<T>(key, _currentCode);
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

            ApplyLanguage(languageCode, persist: _settings.PersistLanguageChoice, fireEvent: true);
        }

        /// <summary>切换到默认语言（回退语言）。</summary>
        public static void SetDefaultLanguage()
        {
            if (!IsInitialized) return;
            SetLanguage(_settings.DefaultLanguageCode);
        }

        // ---------- 内部实现 ----------

        /// <summary>
        /// 应用某语言：加载对应 LocaleData，更新区域信息，按需持久化与触发事件。
        /// 加载失败时保持旧状态不变，不持久化、不触发事件，避免坏语言代码被记住导致下次启动死循环。
        /// </summary>
        private static void ApplyLanguage(string code, bool persist, bool fireEvent)
        {
            var locale = _loader.LoadLocale(code);
            if (locale == null)
            {
                // 加载失败：保持旧语言状态（code/locale/culture 都不动），不持久化、不触发事件。
                // 这样可避免把一个加载失败的语言代码写入 PlayerPrefs，导致下次启动恢复坏 code 再失败的死循环。
                LocalizationLog.Warning($"语言 \"{code}\" 数据加载失败，已保持当前语言 \"{_currentCode}\"。");
                return;
            }

            _currentCode = code;
            _currentLocale = locale;
            _currentCulture = LocalizationFormatter.GetCulture(code);
            // 缓存当前语言信息，供 CurrentLanguage O(1) 访问
            _currentLanguageInfo = _settings.FindLanguage(code);

            // 预加载默认语言数据（首次切换时用于回退）
            if (_defaultLocale == null && _settings.FallbackToDefaultLanguage)
            {
                string defaultCode = _settings.DefaultLanguageCode;
                if (!string.IsNullOrEmpty(defaultCode) && defaultCode != code)
                {
                    _defaultLocale = _loader.LoadLocale(defaultCode);
                }
                else if (defaultCode == code)
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
