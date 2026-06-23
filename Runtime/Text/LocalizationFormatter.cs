using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CLocalization
{
    /// <summary>
    /// 文本格式化工具：参数插值与按当前语言区域（CultureInfo）的格式化。
    /// 支持两类占位符，可混合使用：
    ///  - 位置占位 <c>{0}</c> <c>{1}</c> ...（纯数字，由 <see cref="System.String.Format"/> 处理）
    ///  - 命名占位 <c>{name}</c> <c>{count}</c> ...（非纯数字，由正则匹配字典值替换）
    /// 命名占位的名称须以字母/下划线开头，可含字母、数字、下划线。
    /// </summary>
    public static class LocalizationFormatter
    {
        /// <summary>
        /// 默认格式化区域（InvariantCulture，不随语言变化，作为兜底）。
        /// </summary>
        public static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// 命名占位符正则：匹配 {name} 形式，name 以字母或下划线开头，后接字母/数字/下划线。
        /// 用命名的捕获组 "name" 提取占位符名称。
        /// 注意：纯数字的 {0} 不匹配（由位置占位 string.Format 处理）。
        /// </summary>
        private static readonly Regex NamedPlaceholderRegex =
            new Regex(@"\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

        /// <summary>
        /// 按指定区域对模板执行【位置占位符】插值（{0}/{1} ...）。
        /// 模板中的命名占位 {name} 不会被处理（原样保留）。
        /// </summary>
        /// <param name="template">含 {0}/{1} 占位符的模板字符串</param>
        /// <param name="culture">格式化区域（决定日期/数字等的显示格式）</param>
        /// <param name="args">位置占位符参数</param>
        public static string Format(string template, CultureInfo culture, params object[] args)
        {
            if (string.IsNullOrEmpty(template)) return template;
            if (args == null || args.Length == 0) return template;

            CultureInfo useCulture = culture ?? InvariantCulture;
            try
            {
                return string.Format(useCulture, template, args);
            }
            catch (System.FormatException ex)
            {
                // 模板占位符与参数不匹配时，回退原模板并告警，避免崩溃
                LocalizationLog.Warning($"文本插值失败，模板: \"{template}\"  错误: {ex.Message}");
                return template;
            }
        }

        /// <summary>
        /// 按指定区域对模板执行【命名占位符】插值（{name} {count} ...）。
        /// 仅替换字典中存在的命名占位；字典中无对应 key 的占位符原样保留。
        /// 模板中的位置占位 {0} 不受影响（原样保留）。
        /// </summary>
        /// <param name="template">含 {name} 命名占位符的模板字符串</param>
        /// <param name="culture">格式化区域（决定数值等的显示格式）</param>
        /// <param name="namedArgs">命名占位符的值（key 为占位符名称，不含大括号）</param>
        public static string FormatNamed(string template, CultureInfo culture, IDictionary<string, object> namedArgs)
        {
            if (string.IsNullOrEmpty(template) || namedArgs == null || namedArgs.Count == 0) return template;

            // 无命名占位符时直接返回，避免正则开销
            if (!NamedPlaceholderRegex.IsMatch(template)) return template;

            CultureInfo useCulture = culture ?? InvariantCulture;
            return NamedPlaceholderRegex.Replace(template, match =>
            {
                string name = match.Groups["name"].Value;
                if (namedArgs.TryGetValue(name, out var value) && value != null)
                {
                    string str;
                    // 数值类型用区域格式化，其它直接 ToString
                    if (value is System.IFormattable formattable)
                    {
                        str = formattable.ToString(null, useCulture);
                    }
                    else
                    {
                        str = value.ToString();
                    }
                    // 转义值中的大括号，防止后续 string.Format 把值里的 {0}/{name} 当占位符误解析
                    return EscapeBraces(str);
                }
                // 字典里没有该名称，保留原占位符（便于发现缺失参数）
                return match.Value;
            });
        }

        /// <summary>把字符串中的 { 和 } 转义为 {{ 和 }}，使其在后续 string.Format 中作为字面量。</summary>
        private static string EscapeBraces(string s)
        {
            if (string.IsNullOrEmpty(s) || (s.IndexOf('{') < 0 && s.IndexOf('}') < 0)) return s;
            return s.Replace("{", "{{").Replace("}", "}}");
        }

        /// <summary>
        /// 按指定区域对模板执行【混合占位符】插值：先处理命名占位 {name}，再处理位置占位 {0}。
        /// 这样一个模板可同时含两类占位，例如 "Hello {name}, you have {0} items"。
        /// </summary>
        /// <param name="template">模板字符串</param>
        /// <param name="culture">格式化区域</param>
        /// <param name="namedArgs">命名占位符值（可为 null）</param>
        /// <param name="positionalArgs">位置占位符参数（可为 null）</param>
        public static string FormatMixed(string template, CultureInfo culture,
            IDictionary<string, object> namedArgs, params object[] positionalArgs)
        {
            if (string.IsNullOrEmpty(template)) return template;

            // 1) 先替换命名占位
            string intermediate = namedArgs != null && namedArgs.Count > 0
                ? FormatNamed(template, culture, namedArgs)
                : template;

            // 2) 再处理位置占位
            if (positionalArgs != null && positionalArgs.Length > 0)
            {
                intermediate = Format(intermediate, culture, positionalArgs);
            }
            return intermediate;
        }

        /// <summary>
        /// 根据语言代码构造对应的 CultureInfo。失败时回退到 InvariantCulture。
        /// </summary>
        public static CultureInfo GetCulture(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return InvariantCulture;
            try
            {
                return CultureInfo.CreateSpecificCulture(languageCode);
            }
            catch
            {
                // 某些自造代码无法映射区域，回退兜底
                return InvariantCulture;
            }
        }
    }
}
