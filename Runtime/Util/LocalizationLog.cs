using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 统一日志工具。所有插件内部输出均走此入口，便于统一加前缀与按级别过滤。
    /// </summary>
    public static class LocalizationLog
    {
        /// <summary>日志统一前缀，便于在 Console 中筛选。</summary>
        public const string Prefix = "[CLoc] ";

        /// <summary>是否输出 Warning 级别日志（可在 Settings 中关闭，减少噪声）。</summary>
        public static bool LogWarnings = true;

        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void Warning(string message)
        {
            if (!LogWarnings) return;
            Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }
    }
}
