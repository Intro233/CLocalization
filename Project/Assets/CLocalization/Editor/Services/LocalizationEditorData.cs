using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 编辑器数据服务。负责直接读写磁盘上的语言 JSON 文件（绕过运行时缓存），
    /// 供编辑器窗口、导入导出等模块共享使用。
    /// 与运行时 <see cref="ResourcesLocalizationLoader"/> 不同，这里每步都直接读盘/写盘，
    /// 以保证编辑期间看到的是最新文件内容。
    /// </summary>
    public static class LocalizationEditorData
    {
        /// <summary>缓存当前模式的语言目录绝对路径，避免每帧重复 LoadOrCreateSettings。</summary>
        private static string _cachedLocalesDirectory;
        /// <summary>缓存时的 LoadMode + LocalesPath，用于检测配置变化并失效缓存。</summary>
        private static AssetLoadMode _cachedMode;
        private static string _cachedLocalesPath;

        /// <summary>当前模式下的语言 JSON 文件绝对目录（按 Settings 的 AssetLoadMode 解析，带缓存）。</summary>
        public static string LocalesDirectory
        {
            get
            {
                var settings = LocalizationSetup.LoadOrCreateSettings();
                if (settings == null)
                {
                    // 无 Settings 时回退默认 Resources 目录
                    return Path.Combine(Application.dataPath, "CLocalization/Resources/CLocalization/Locales");
                }

                // 缓存命中：mode 与 path 未变时直接返回
                if (_cachedLocalesDirectory != null
                    && _cachedMode == settings.AssetLoadMode
                    && _cachedLocalesPath == settings.LocalesPath)
                {
                    return _cachedLocalesDirectory;
                }

                _cachedMode = settings.AssetLoadMode;
                _cachedLocalesPath = settings.LocalesPath;
                _cachedLocalesDirectory = GetLocalesDirectory(settings.AssetLoadMode, settings.LocalesPath);
                return _cachedLocalesDirectory;
            }
        }

        /// <summary>
        /// 根据加载方式与 localesPath 计算语言 JSON 文件的绝对磁盘目录。
        /// Resources 模式：{Assets}/CLocalization/Resources/{localesPath}
        /// StreamingAssets 模式：{Assets}/StreamingAssets/{localesPath}
        /// 供 LocalesDirectory 属性与迁移工具共用。
        /// </summary>
        public static string GetLocalesDirectory(AssetLoadMode mode, string localesPath)
        {
            string containerRoot = mode == AssetLoadMode.StreamingAssets
                ? "StreamingAssets"
                : "CLocalization/Resources";
            string sub = string.IsNullOrEmpty(localesPath) ? "CLocalization/Locales" : localesPath;
            return Path.Combine(Application.dataPath, Path.Combine(containerRoot, sub));
        }

        /// <summary>失效缓存的目录（迁移或 Settings 变更后调用，强制下次重新解析）。</summary>
        public static void InvalidateDirectoryCache()
        {
            _cachedLocalesDirectory = null;
        }

        /// <summary>
        /// 获取当前模式下本地化资源（Sprite/Audio/Font）的放置提示文案。
        /// Resources 模式：Resources/{assetsPath}/{语言}/{key}
        /// StreamingAssets 模式：提示不支持 Unity 资源。
        /// </summary>
        /// <summary>
        /// 获取资源映射配置提示文案（提示去「资源」Tab 配置，并显示某 key 的已配置状态）。
        /// </summary>
        /// <param name="key">当前 key（用于查询映射状态，可空）</param>
        /// <param name="map">对应类型的映射表（可为空）</param>
        public static string GetAssetsHintMessage(string key = null, AssetMapBase map = null)
        {
            string baseHint = "本地化资源在「Tools > CLocalization > Localization Window」的「资源」Tab 配置：\n" +
                              "为每个 key 在各语言拖入对应的 Sprite/AudioClip/Font 即可，切换语言时自动加载。";
            if (!string.IsNullOrEmpty(key) && map != null)
            {
                int configured = map.CountConfiguredLanguages(key);
                var settings = LocalizationSetup.LoadOrCreateSettings();
                int total = settings?.Languages?.Count ?? 0;
                return baseHint + $"\n\n当前 key \"{key}\" 映射状态：{configured}/{total} 语言已配置。" +
                       (configured == 0 ? "（尚未配置，运行时不会显示资源）" : "");
            }
            return baseHint;
        }

        /// <summary>Settings 资源相对路径（相对 Assets）。</summary>
        public const string SettingsAssetPath = "Assets/CLocalization/Resources/CLocalization/LocalizationSettings.asset";

        /// <summary>加载所有语言 JSON 文件为 LocaleData 列表（精确匹配 .json 后缀，按语言代码排序，顺序稳定）。</summary>
        public static List<LocaleData> LoadAllLocales()
        {
            var result = new List<LocaleData>();
            string dir = LocalesDirectory;
            if (!Directory.Exists(dir))
            {
                return result;
            }

            // 注意：Windows 下 Directory.GetFiles(dir, "*.json") 会误匹配 .json5/.json.bak 等，
            // 因此改为获取全部文件后用精确后缀过滤，并排序保证 locale 顺序稳定（影响 CSV 语言列顺序）。
            string[] files = Directory.GetFiles(dir, "*");
            var filtered = new List<string>();
            foreach (string file in files)
            {
                if (file.EndsWith(LocalizationPaths.LocaleExtension, System.StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add(file);
                }
            }
            filtered.Sort(System.StringComparer.OrdinalIgnoreCase);

            foreach (string file in filtered)
            {
                LocaleData data = LoadLocaleFile(file);
                if (data != null) result.Add(data);
            }
            // 按 Settings 的语言列表顺序重排（用户在 LanguagesTab 调整的顺序持久化在 Settings），
            // 而非文件名字母序——否则用户调整的顺序在重新打开窗口后会丢失。
            SortBySettingsOrder(result);
            return result;
        }

        /// <summary>
        /// 按 Settings.languages 的顺序对 locale 列表排序。
        /// Settings 中有的按其顺序排；Settings 中没有的按原顺序放最后。
        /// 若 Settings 不存在或为空，保持原顺序（文件名序）。
        /// </summary>
        public static void SortBySettingsOrder(List<LocaleData> locales)
        {
            if (locales == null || locales.Count <= 1) return;
            var settings = LocalizationSetup.LoadOrCreateSettings();
            if (settings == null || settings.Languages == null || settings.Languages.Count == 0) return;

            // 建立 code → 期望顺序索引
            var order = new System.Collections.Generic.Dictionary<string, int>();
            for (int i = 0; i < settings.Languages.Count; i++)
            {
                var code = settings.Languages[i]?.Code;
                if (!string.IsNullOrEmpty(code) && !order.ContainsKey(code))
                {
                    order[code] = i;
                }
            }

            // 稳定排序：Settings 中有的按 orderIndex 升序；没有的给一个大值放最后，保持原相对顺序
            int fallbackBase = settings.Languages.Count;
            locales.Sort((a, b) =>
            {
                string ca = a?.Meta?.Code;
                string cb = b?.Meta?.Code;
                int idxA = (ca != null && order.TryGetValue(ca, out int ia)) ? ia : fallbackBase;
                int idxB = (cb != null && order.TryGetValue(cb, out int ib)) ? ib : fallbackBase;
                return idxA.CompareTo(idxB);
            });
        }

        /// <summary>读取单个 JSON 文件为 LocaleData。失败返回 null。</summary>
        public static LocaleData LoadLocaleFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            string json = File.ReadAllText(filePath);
            try
            {
                return LocaleData.FromJson(json);
            }
            catch (System.Exception ex)
            {
                LocalizationLog.Error($"解析失败: {filePath}  错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>保存 LocaleData 到对应语言代码的 JSON 文件（带缩进）。</summary>
        public static void SaveLocale(LocaleData data)
        {
            if (data == null || data.Meta == null || string.IsNullOrEmpty(data.Meta.Code))
            {
                LocalizationLog.Error("保存失败：LocaleData 或其 Meta.Code 为空。");
                return;
            }

            EnsureDirectoryExists();
            string filePath = GetLocaleFilePath(data.Meta.Code);
            File.WriteAllText(filePath, data.ToJson(true));
        }

        /// <summary>保存所有 locale。</summary>
        public static void SaveAllLocales(IEnumerable<LocaleData> locales)
        {
            foreach (var locale in locales)
            {
                SaveLocale(locale);
            }
        }

        /// <summary>根据语言代码获取 JSON 文件绝对路径。</summary>
        public static string GetLocaleFilePath(string code)
        {
            return Path.Combine(LocalesDirectory, code + LocalizationPaths.LocaleExtension);
        }

        /// <summary>确保语言目录存在。</summary>
        public static void EnsureDirectoryExists()
        {
            string dir = LocalesDirectory;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>新建一个空白 LocaleData（仅含 meta，无 entries）。</summary>
        public static LocaleData CreateLocale(string code, string displayName)
        {
            return new LocaleData
            {
                Meta = new LocaleMeta { Code = code, DisplayName = displayName, Version = "1.0.0" },
                Entries = new Dictionary<string, string>()
            };
        }

        /// <summary>删除某语言的 JSON 文件。</summary>
        public static bool DeleteLocale(string code)
        {
            string filePath = GetLocaleFilePath(code);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }

        /// <summary>收集所有 locale 中出现过的 key（并集）。</summary>
        public static List<string> CollectAllKeys(IEnumerable<LocaleData> locales)
        {
            var set = new HashSet<string>();
            foreach (var locale in locales)
            {
                if (locale?.Entries == null) continue;
                foreach (var key in locale.Entries.Keys)
                {
                    set.Add(key);
                }
            }
            return new List<string>(set);
        }
    }
}
