using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 多语言导入导出服务。
    /// 把所有语言的词条转换为「key 列 + 每语言一列」的扁平表格（CSV），
    /// 便于翻译人员/策划在 Excel 中协作编辑，编辑后再导回各语言 JSON。
    ///
    /// 导出表结构：
    /// | key | zh-CN | en-US | ja-JP | ko-KR |
    /// | app.title | 多语言示例 | Demo | ...   | ...   |
    /// </summary>
    public static class LocalizationImportExport
    {
        /// <summary>第一列（key 列）的表头名称。</summary>
        public const string KeyColumnHeader = "key";

        /// <summary>
        /// 导出所有 locale 为一个 CSV 文件。语言列顺序按 locale 加载顺序。
        /// </summary>
        public static void ExportToCsv(string filePath, List<LocaleData> locales)
        {
            var table = BuildExportTable(locales);
            CsvUtil.WriteFile(filePath, table);
            LocalizationLog.Info($"已导出 {locales.Count} 种语言、{table.Count - 1} 个词条到: {filePath}");
        }

        /// <summary>导出为 CSV 字符串（不写盘，便于预览或拷贝）。</summary>
        public static string ExportToCsvString(List<LocaleData> locales)
        {
            var table = BuildExportTable(locales);
            return CsvUtil.Write(table);
        }

        /// <summary>
        /// 从 CSV 文件导入，合并/覆盖各语言 JSON。
        /// 行为：CSV 中存在的 key 会覆盖 JSON；JSON 中存在但 CSV 中没有的 key 保留。
        /// </summary>
        /// <param name="filePath">CSV 文件路径</param>
        /// <param name="locales">现有 locale 列表（会被原地更新）</param>
        /// <returns>导入统计（新增/更新 key 数量）</returns>
        public static ImportStats ImportFromCsv(string filePath, List<LocaleData> locales)
        {
            var stats = new ImportStats();
            var table = CsvUtil.ReadFile(filePath);
            if (table.Count < 2)
            {
                LocalizationLog.Warning("CSV 为空或只有表头，无可导入内容。");
                return stats;
            }

            // 解析表头：第 0 列是 key，后续列是语言代码
            var header = table[0];
            var codeToColumn = new Dictionary<string, int>();
            for (int c = 1; c < header.Count; c++)
            {
                string code = header[c]?.Trim();
                if (!string.IsNullOrEmpty(code)) codeToColumn[code] = c;
            }

            // 建立 code → locale 索引；CSV 中出现但 locale 列表没有的语言会被跳过
            var codeToLocale = new Dictionary<string, LocaleData>();
            foreach (var locale in locales)
            {
                if (locale?.Meta != null && !string.IsNullOrEmpty(locale.Meta.Code))
                {
                    codeToLocale[locale.Meta.Code] = locale;
                }
            }

            // 逐行解析数据
            for (int r = 1; r < table.Count; r++)
            {
                var row = table[r];
                if (row.Count == 0) continue;
                string key = row[0];
                if (string.IsNullOrEmpty(key)) continue;

                foreach (var kv in codeToColumn)
                {
                    string code = kv.Key;
                    int col = kv.Value;
                    if (!codeToLocale.TryGetValue(code, out var locale)) continue;
                    if (locale.Entries == null) locale.Entries = new Dictionary<string, string>();

                    string value = col < row.Count ? row[col] : "";
                    // 空字符串视为「未翻译」，保留为空（不删除 key）
                    bool existed = locale.Entries.ContainsKey(key);
                    if (!existed) stats.AddedKeys++;
                    else if (locale.Entries[key] != value) stats.UpdatedKeys++;
                    locale.Entries[key] = value;
                }
            }

            LocalizationLog.Info($"CSV 导入完成: 新增 {stats.AddedKeys} 个 key，更新 {stats.UpdatedKeys} 个翻译。");
            return stats;
        }

        /// <summary>构建导出表格（表头 + 数据行）。</summary>
        private static List<IList<string>> BuildExportTable(List<LocaleData> locales)
        {
            var table = new List<IList<string>>();

            // 表头
            var header = new List<string> { KeyColumnHeader };
            foreach (var locale in locales)
            {
                header.Add(locale?.Meta?.Code ?? "?");
            }
            table.Add(header);

            // 收集所有 key（保持稳定顺序：先按第一个 locale 的顺序，再补其他 locale 独有的）
            var orderedKeys = new List<string>();
            var seen = new HashSet<string>();
            foreach (var locale in locales)
            {
                if (locale?.Entries == null) continue;
                foreach (var key in locale.Entries.Keys)
                {
                    if (seen.Add(key)) orderedKeys.Add(key);
                }
            }

            // 数据行
            foreach (var key in orderedKeys)
            {
                var row = new List<string> { key };
                foreach (var locale in locales)
                {
                    string value = "";
                    if (locale?.Entries != null && locale.Entries.TryGetValue(key, out var v))
                    {
                        value = v ?? "";
                    }
                    row.Add(value);
                }
                table.Add(row);
            }
            return table;
        }

        /// <summary>导入统计。</summary>
        public struct ImportStats
        {
            /// <summary>新增的 key 数量。</summary>
            public int AddedKeys;
            /// <summary>更新（值变化）的翻译数量。</summary>
            public int UpdatedKeys;
        }
    }
}
