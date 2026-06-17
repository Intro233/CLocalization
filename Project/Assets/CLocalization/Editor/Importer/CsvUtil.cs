using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CLocalization.Editor
{
    /// <summary>
    /// 轻量 CSV 读写工具（零第三方依赖）。
    /// 支持带引号的字段、字段内换行、转义引号（""），符合 RFC 4180 基本规范。
    /// 供导入导出功能与 Excel/表格软件协作使用。
    /// </summary>
    public static class CsvUtil
    {
        /// <summary>
        /// 将二维表格写入 CSV 字符串。
        /// </summary>
        /// <param name="rows">行集合，每行是字段列表</param>
        /// <param name="separator">分隔符，默认逗号</param>
        public static string Write(IList<IList<string>> rows, char separator = ',')
        {
            var sb = new StringBuilder();
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int c = 0; c < row.Count; c++)
                {
                    if (c > 0) sb.Append(separator);
                    sb.Append(EscapeField(row[c], separator));
                }
                sb.Append("\r\n"); // Windows 换行，Excel 友好
            }
            return sb.ToString();
        }

        /// <summary>把表格写入文件（UTF-8 with BOM，Excel 中文不乱码）。</summary>
        public static void WriteFile(string path, IList<IList<string>> rows, char separator = ',')
        {
            string content = Write(rows, separator);
            // UTF8 with BOM：Excel 打开中文 CSV 需 BOM 才能正确识别编码
            File.WriteAllText(path, content, new UTF8Encoding(true));
        }

        /// <summary>从字符串解析 CSV 为二维表格。</summary>
        public static List<List<string>> Read(string content, char separator = ',')
        {
            var rows = new List<List<string>>();
            var current = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int i = 0;
            while (i < content.Length)
            {
                char ch = content[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        // 连续两个引号表示转义的一个引号
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                            continue;
                        }
                        // 单个引号：结束引号区
                        inQuotes = false;
                        i++;
                        continue;
                    }
                    field.Append(ch);
                    i++;
                    continue;
                }

                // 非引号区
                if (ch == '"')
                {
                    inQuotes = true;
                    i++;
                    continue;
                }
                if (ch == separator)
                {
                    current.Add(field.ToString());
                    field.Clear();
                    i++;
                    continue;
                }
                if (ch == '\r')
                {
                    // 处理 \r\n 换行
                    current.Add(field.ToString());
                    field.Clear();
                    rows.Add(current);
                    current = new List<string>();
                    i++;
                    if (i < content.Length && content[i] == '\n') i++;
                    continue;
                }
                if (ch == '\n')
                {
                    current.Add(field.ToString());
                    field.Clear();
                    rows.Add(current);
                    current = new List<string>();
                    i++;
                    continue;
                }
                field.Append(ch);
                i++;
            }

            // 处理末尾未结束的字段/行（文件不以换行结尾的情况）
            if (field.Length > 0 || current.Count > 0)
            {
                current.Add(field.ToString());
                rows.Add(current);
            }
            return rows;
        }

        /// <summary>从文件读取 CSV。</summary>
        public static List<List<string>> ReadFile(string path, char separator = ',')
        {
            if (!File.Exists(path)) return new List<List<string>>();
            string content = File.ReadAllText(path);
            return Read(content, separator);
        }

        /// <summary>字段转义：含逗号/引号/换行的字段需用引号包裹，内部引号翻倍。</summary>
        private static string EscapeField(string field, char separator)
        {
            if (field == null) return "";
            bool needQuote = field.IndexOfAny(new[] { separator, '"', '\r', '\n' }) >= 0;
            if (!needQuote) return field;
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
    }
}
