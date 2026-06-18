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
        /// <param name="separator">分隔符（一般用 ReadFile 的自动嗅探结果）。</param>
        public static List<List<string>> Read(string content, char separator = ',')
        {
            var rows = new List<List<string>>();
            var current = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            // 剥离开头可能残留的 UTF-8 BOM（防止首字段名带 \uFEFF 导致表头匹配失败）
            if (content.Length > 0 && content[0] == '\uFEFF')
            {
                content = content.Substring(1);
            }
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

        /// <summary>从文件读取 CSV。自动嗅探分隔符（逗号或分号），兼容不同区域设置的 Excel 导出。</summary>
        public static List<List<string>> ReadFile(string path, char separator = ',')
        {
            if (!File.Exists(path)) return new List<List<string>>();
            string content = File.ReadAllText(path);
            // 嗅探：取第一行（表头），比较逗号与分号出现次数，选多的作为分隔符。
            // 中文/德语等区域的 Excel 默认用分号分隔，写死逗号会导致整行被当成一个字段，数据导不进来。
            char detected = DetectSeparator(content, separator);
            return Read(content, detected);
        }

        /// <summary>
        /// 嗅探 CSV 内容的分隔符：统计第一行（到首个换行）中逗号与分号的数量，取较多者。
        /// 两者都为 0 或相同时回退到 fallback（默认逗号）。
        /// </summary>
        public static char DetectSeparator(string content, char fallback = ',')
        {
            if (string.IsNullOrEmpty(content)) return fallback;
            // 取第一行（首个换行前的内容，忽略引号内分隔符的精度——表头一般不含引号）
            int nl = content.IndexOfAny(new[] { '\r', '\n' });
            string firstLine = nl > 0 ? content.Substring(0, nl) : content;

            int comma = 0, semicolon = 0;
            for (int i = 0; i < firstLine.Length; i++)
            {
                if (firstLine[i] == ',') comma++;
                else if (firstLine[i] == ';') semicolon++;
            }
            if (semicolon > comma) return ';';
            if (comma > 0) return ',';
            return fallback;
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
