using System;
using System.Collections.Generic;
using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 本地化配置（ScriptableObject）。存放可用语言列表、默认语言、查询回退策略等全局设置。
    /// 推荐放在 Resources 目录下（默认 Resources/CLocalization/LocalizationSettings.asset），
    /// 运行时由 <see cref="Localization"/> 加载并据此初始化。
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationSettings", menuName = "CLocalization/Localization Settings", order = 1)]
    public class LocalizationSettings : ScriptableObject
    {
        [Header("语言列表")]

        /// <summary>所有可用语言。运行时语言切换只能在这些语言中选取。</summary>
        [Tooltip("所有可用语言，运行时切换语言只能在此列表中选取")]
        [SerializeField] private List<LanguageInfo> languages = new List<LanguageInfo>();

        /// <summary>默认语言代码（兜底回退语言，通常为开发/母语语言）。</summary>
        [Tooltip("默认（回退）语言代码，如 zh-CN")]
        [SerializeField] private string defaultLanguageCode = "en-US";

        [Header("行为设置")]

        /// <summary>切换语言后是否自动持久化到 PlayerPrefs（下次启动自动恢复）。</summary>
        [Tooltip("切换语言后是否自动持久化到 PlayerPrefs（下次启动自动恢复）")]
        [SerializeField] private bool persistLanguageChoice = true;

        /// <summary>初始化时是否尝试使用系统语言（若可用），否则用 PlayerPrefs 上次语言，再否则用默认语言。</summary>
        [Tooltip("初始化时是否尝试使用系统语言（若可用）")]
        [SerializeField] private bool useSystemLanguageByDefault = true;

        /// <summary>查询到缺失 key 时，是否回退到默认语言查询。</summary>
        [Tooltip("查询缺失 key 时是否回退到默认语言")]
        [SerializeField] private bool fallbackToDefaultLanguage = true;

        /// <summary>查询最终失败时，是否输出警告日志（频繁触发可关闭以减少噪声）。</summary>
        [Tooltip("查询最终失败时是否输出警告日志")]
        [SerializeField] private bool logMissingKeys = true;

        [Header("资源加载")]
        /// <summary>资源加载方式（Resources / StreamingAssets）。决定运行时从哪个容器加载。</summary>
        [Tooltip("资源加载方式：Resources（默认，支持文本+资源）或 StreamingAssets（仅文本，可热更新）")]
        [SerializeField] private AssetLoadMode assetLoadMode = AssetLoadMode.Resources;

        /// <summary>语言文本 JSON 子路径（不含容器根，如 CLocalization/Locales）。</summary>
        [Tooltip("语言文本 JSON 子路径（不含容器根），如 CLocalization/Locales")]
        [SerializeField] private string localesPath = "CLocalization/Locales";

        /// <summary>本地化资源（Sprite/Audio/Font）子路径（如 CLocalization/Assets）。仅 Resources 模式生效。</summary>
        [Tooltip("本地化资源（Sprite/Audio/Font）子路径，如 CLocalization/Assets。仅 Resources 模式支持")]
        [SerializeField] private string assetsPath = "CLocalization/Assets";

        // ---------- 运行时访问器 ----------

        /// <summary>可用语言列表（只读视图）。</summary>
        public IReadOnlyList<LanguageInfo> Languages => languages;

        /// <summary>默认语言代码。</summary>
        public string DefaultLanguageCode => defaultLanguageCode;

        /// <summary>是否持久化语言选择。</summary>
        public bool PersistLanguageChoice => persistLanguageChoice;

        /// <summary>是否默认使用系统语言。</summary>
        public bool UseSystemLanguageByDefault => useSystemLanguageByDefault;

        /// <summary>是否回退到默认语言。</summary>
        public bool FallbackToDefaultLanguage => fallbackToDefaultLanguage;

        /// <summary>是否输出缺失 key 警告。</summary>
        public bool LogMissingKeys => logMissingKeys;

        /// <summary>资源加载方式。</summary>
        public AssetLoadMode AssetLoadMode => assetLoadMode;

        /// <summary>语言文本 JSON 子路径。</summary>
        public string LocalesPath => string.IsNullOrEmpty(localesPath) ? "CLocalization/Locales" : localesPath;

        /// <summary>本地化资源子路径。</summary>
        public string AssetsPath => string.IsNullOrEmpty(assetsPath) ? "CLocalization/Assets" : assetsPath;

        /// <summary>根据语言代码查找 LanguageInfo，找不到返回 null。</summary>
        public LanguageInfo FindLanguage(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i].Code == code) return languages[i];
            }
            return null;
        }

        /// <summary>判断某语言代码是否在可用列表中。</summary>
        public bool HasLanguage(string code)
        {
            return FindLanguage(code) != null;
        }
    }
}
