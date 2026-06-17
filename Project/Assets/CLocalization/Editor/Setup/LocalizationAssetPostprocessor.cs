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
        /// <summary>Locales 目录在 Assets 下的相对路径前缀（用于判断资源是否属于语言文件）。</summary>
        private const string LocalesPathPrefix = "Assets/CLocalization/Resources/CLocalization/Locales/";

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

        /// <summary>给定路径列表中是否有 Locales 目录下的 .json 文件。</summary>
        private static bool AffectsLocales(string[] paths)
        {
            if (paths == null) return false;
            foreach (string path in paths)
            {
                if (path == null) continue;
                // 必须在 Locales 目录下且是 .json 后缀
                if (path.StartsWith(LocalesPathPrefix, System.StringComparison.OrdinalIgnoreCase)
                    && path.EndsWith(LocalizationPaths.LocaleExtension, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
