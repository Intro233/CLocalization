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

        // ---------- 事件 ----------

        /// <summary>语言切换事件。Localize 组件订阅此事件以自动刷新显示。</summary>
        public static event Action<LanguageInfo> OnLanguageChanged;

        // ---------- 状态访问 ----------

        /// <summary>是否已初始化。</summary>
        public static bool IsInitialized => _settings != null;

        /// <summary>当前语言代码。</summary>
        public static string CurrentLanguageCode => _currentCode;

        /// <summary>当前语言信息（找不到返回 null）。</summary>
        public static LanguageInfo CurrentLanguage
        {
            get
            {
                if (_settings == null || string.IsNullOrEmpty(_currentCode)) return null;
                return _settings.FindLanguage(_currentCode);
            }
        }

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

            LocalizationLog.Info($"本地化系统已初始化，当前语言: {_currentCode}");
        }

        // ---------- 文本查询 ----------

        /// <summary>
        /// 获取 key 对应的本地化文本（不带参数）。
        /// 查询失败时按回退策略处理：回退默认语言 → 回退 key 字符串本身。
        /// </summary>
        public static string Get(string key)
        {
            return Resolve(key, out _);
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
        /// 尝试获取 key 文本，不触发回退。成功返回 true。
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
        /// </summary>
        private static void ApplyLanguage(string code, bool persist, bool fireEvent)
        {
            var locale = _loader.LoadLocale(code);
            if (locale == null)
            {
                LocalizationLog.Warning($"语言 \"{code}\" 数据加载失败。");
                // 即使数据加载失败也记录 code，避免反复报错；但保持旧 locale
                _currentCode = code;
                _currentCulture = LocalizationFormatter.GetCulture(code);
            }
            else
            {
                _currentCode = code;
                _currentLocale = locale;
                _currentCulture = LocalizationFormatter.GetCulture(code);
            }

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

            // 持久化
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

            // 当前语言命中
            if (_currentLocale != null && _currentLocale.TryGetEntry(key, out var value))
            {
                found = true;
                return value;
            }

            // 回退默认语言
            if (_settings.FallbackToDefaultLanguage && _defaultLocale != null && _defaultLocale != _currentLocale)
            {
                if (_defaultLocale.TryGetEntry(key, out value))
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
