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
        /// <summary>语言 JSON 文件所在的绝对目录（Assets 下，即 Resources 目录）。</summary>
        public static string LocalesDirectory
        {
            get
            {
                // Application.dataPath 指向 Assets，拼接 Resources/CLocalization/Locales
                return Path.Combine(Application.dataPath, "CLocalization/Resources/CLocalization/Locales");
            }
        }

        /// <summary>Settings 资源相对路径（相对 Assets）。</summary>
        public const string SettingsAssetPath = "Assets/CLocalization/Resources/CLocalization/LocalizationSettings.asset";

        /// <summary>加载所有语言 JSON 文件为 LocaleData 列表。</summary>
        public static List<LocaleData> LoadAllLocales()
        {
            var result = new List<LocaleData>();
            string dir = LocalesDirectory;
            if (!Directory.Exists(dir))
            {
                return result;
            }

            string[] files = Directory.GetFiles(dir, "*.json");
            foreach (string file in files)
            {
                LocaleData data = LoadLocaleFile(file);
                if (data != null) result.Add(data);
            }
            return result;
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
