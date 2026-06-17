using System.Collections.Generic;
using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 语言选择的持久化与系统语言检测。
    /// 通过 PlayerPrefs 存储用户上次选择的语言代码，下次启动自动恢复；
    /// 并提供将 Unity SystemLanguage 映射到本插件语言代码的能力。
    /// </summary>
    public static class LocalizationPrefs
    {
        /// <summary>PlayerPrefs 中存储当前语言代码的键名。</summary>
        private const string PrefsKey = "CLocalization.LanguageCode";

        /// <summary>
        /// 读取上次保存的语言代码。无记录返回 null。
        /// </summary>
        public static string LoadSavedLanguage()
        {
            if (!PlayerPrefs.HasKey(PrefsKey)) return null;
            return PlayerPrefs.GetString(PrefsKey, null);
        }

        /// <summary>
        /// 保存语言代码到 PlayerPrefs。null/空字符串表示清除记录。
        /// </summary>
        public static void SaveLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                PlayerPrefs.DeleteKey(PrefsKey);
            }
            else
            {
                PlayerPrefs.SetString(PrefsKey, languageCode);
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 获取操作系统语言对应的语言代码（BCP 47 风格）。
        /// 例如 SystemLanguage.Chinese → "zh-CN"，English → "en-US"。
        /// </summary>
        public static string GetSystemLanguageCode()
        {
            return SystemLanguageToCode(Application.systemLanguage);
        }

        /// <summary>
        /// 将 Unity 的 SystemLanguage 枚举映射为 BCP 47 语言代码。
        /// 仅覆盖常见语言，未覆盖的回退到 "en-US"。
        /// </summary>
        public static string SystemLanguageToCode(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    return "zh-CN";
                case SystemLanguage.ChineseTraditional:
                    return "zh-TW";
                case SystemLanguage.English:
                    return "en-US";
                case SystemLanguage.Japanese:
                    return "ja-JP";
                case SystemLanguage.Korean:
                    return "ko-KR";
                case SystemLanguage.French:
                    return "fr-FR";
                case SystemLanguage.German:
                    return "de-DE";
                case SystemLanguage.Spanish:
                    return "es-ES";
                case SystemLanguage.Portuguese:
                    return "pt-PT";
                case SystemLanguage.Russian:
                    return "ru-RU";
                case SystemLanguage.Italian:
                    return "it-IT";
                case SystemLanguage.Arabic:
                    return "ar-SA";
                default:
                    return "en-US";
            }
        }

        /// <summary>
        /// 决定初始化时应使用的语言代码，优先级：
        /// 1) Settings 中允许且存在 PlayerPrefs 记录 → 上次选择的语言
        /// 2) Settings 中允许且系统语言在可用列表中 → 系统语言
        /// 3) 默认语言
        /// </summary>
        public static string ResolveInitialLanguage(LocalizationSettings settings, IReadOnlyList<string> availableCodes)
        {
            string defaultCode = settings != null ? settings.DefaultLanguageCode : "en-US";

            // 1) 上次选择
            if (settings == null || settings.PersistLanguageChoice)
            {
                string saved = LoadSavedLanguage();
                if (!string.IsNullOrEmpty(saved) && ContainsCode(availableCodes, saved))
                {
                    return saved;
                }
            }

            // 2) 系统语言
            if (settings == null || settings.UseSystemLanguageByDefault)
            {
                string sys = GetSystemLanguageCode();
                if (ContainsCode(availableCodes, sys))
                {
                    return sys;
                }
            }

            // 3) 默认语言；若默认语言也不在可用列表，则取列表首个（若有）
            if (ContainsCode(availableCodes, defaultCode)) return defaultCode;
            if (availableCodes != null && availableCodes.Count > 0) return availableCodes[0];
            return defaultCode;
        }

        private static bool ContainsCode(IReadOnlyList<string> codes, string code)
        {
            if (codes == null || string.IsNullOrEmpty(code)) return false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i] == code) return true;
            }
            return false;
        }
    }
}
