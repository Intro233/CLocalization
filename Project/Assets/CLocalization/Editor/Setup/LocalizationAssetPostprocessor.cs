using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 资源导入处理器。当 Locales 目录下的语言 JSON 文件发生增删/修改时，
    /// 自动把 Settings 的语言列表与磁盘文件保持同步（merge 策略，不删除已有配置）。
    /// 解决「用户拖一个 fr-FR.json 进目录但 Settings 不更新、运行时切不到」的隐形坑。
    /// </summary>
    public class LocalizationAssetPostprocessor : AssetPostprocessor
    {
        /// <summary>Resources 模式下 Locales 目录的 Assets 相对前缀。</summary>
        private const string ResourcesLocalesPrefix = "Assets/CLocalization/Resources/CLocalization/Locales/";

        /// <summary>StreamingAssets 模式下 Locales 目录的 Assets 相对前缀。</summary>
        private const string StreamingAssetsLocalesPrefix = "Assets/StreamingAssets/CLocalization/Locales/";

        /// <summary>
        /// 获取当前模式对应的 Locales 目录 Assets 相对前缀。
        /// 注意：StreamingAssets 的 JSON 是原始文本，Unity 默认不导入为 Asset，
        /// 因此 OnPostprocessAllAssets 在 StreamingAssets 模式下可能不触发——需手动点「刷新语言列表」。
        /// </summary>
        private static string GetCurrentLocalesPrefix()
        {
            var settings = LocalizationSetup.LoadOrCreateSettings();
            if (settings == null) return ResourcesLocalesPrefix;
            string containerRoot = settings.AssetLoadMode == AssetLoadMode.StreamingAssets
                ? "StreamingAssets"
                : "CLocalization/Resources";
            return "Assets/" + containerRoot + "/" + settings.LocalesPath + "/";
        }

        /// <summary>
        /// Unity 在资源导入完成后回调。签名固定，不可改名。
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!ShouldHandle(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths))
            {
                return;
            }

            // 延迟到编辑器空闲时执行，避免在导入流程中嵌套 AssetDatabase 操作
            EditorApplication.delayCall += () =>
            {
                bool changed = LocalizationSetup.SyncSettingsFromLocales();
                if (changed)
                {
                    LocalizationLog.Info("检测到语言文件变化，已自动同步 Settings 语言列表。");
                }
            };
        }

        /// <summary>判断本次导入是否涉及 Locales 目录下的 JSON 文件。</summary>
        private static bool ShouldHandle(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            return AffectsLocales(importedAssets)
                || AffectsLocales(deletedAssets)
                || AffectsLocales(movedAssets)
                || AffectsLocales(movedFromAssetPaths);
        }

        /// <summary>给定路径列表中是否有任一模式的 Locales 目录下的 .json 文件。</summary>
        /// <remarks>同时检查当前模式与另一种模式的前缀，确保迁移期间也能感知变化。</remarks>
        private static bool AffectsLocales(string[] paths)
        {
            if (paths == null) return false;
            // 当前模式前缀 + 另一模式前缀都纳入监控（迁移时文件在两目录间移动）
            string currentPrefix = GetCurrentLocalesPrefix();
            foreach (string path in paths)
            {
                if (path == null) continue;
                if (!path.EndsWith(LocalizationPaths.LocaleExtension, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // 命中当前模式或任一固定模式前缀
                if (path.StartsWith(currentPrefix, System.StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(ResourcesLocalesPrefix, System.StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(StreamingAssetsLocalesPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
