using System.Globalization;

namespace CLocalization
{
    /// <summary>
    /// 文本格式化工具：参数插值与按当前语言区域（CultureInfo）的格式化。
    /// 支持 {0}/{name} 位置与命名占位符，以及对日期/货币/数字使用对应语言的区域格式。
    /// </summary>
    public static class LocalizationFormatter
    {
        /// <summary>
        /// 默认格式化区域（InvariantCulture，不随语言变化，作为兜底）。
        /// </summary>
        public static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// 按指定区域对模板执行参数插值。
        /// 模板如 "Hello, {0}" 或 "Welcome {name}"（命名占位需调用方自行处理，这里主要支持位置占位 {0} {1} ...）。
        /// </summary>
        /// <param name="template">含 {0}/{1} 占位符的模板字符串</param>
        /// <param name="culture">格式化区域（决定日期/数字等的显示格式）</param>
        /// <param name="args">占位符参数</param>
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
