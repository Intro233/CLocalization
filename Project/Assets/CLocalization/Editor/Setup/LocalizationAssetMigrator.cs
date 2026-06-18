using System.IO;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 资源迁移工具。切换资源加载方式时，把语言 JSON 文件从旧目录复制到新目录并删除旧目录。
    /// 供 Settings 面板的「迁移资源」按钮调用。
    /// </summary>
    public static class LocalizationAssetMigrator
    {
        /// <summary>迁移预览信息（供确认对话框展示）。</summary>
        public struct MigrationPreview
        {
            /// <summary>源目录绝对路径。</summary>
            public string SourceDirectory;
            /// <summary>目标目录绝对路径。</summary>
            public string TargetDirectory;
            /// <summary>将迁移的 JSON 文件数量。</summary>
            public int FileCount;
            /// <summary>源目录是否存在（无文件则无需迁移）。</summary>
            public bool HasFiles;
        }

        /// <summary>
        /// 预览迁移：计算从 from 模式迁移到 to 模式时的源/目标目录与文件数。
        /// </summary>
        public static MigrationPreview Preview(AssetLoadMode from, AssetLoadMode to, string localesPath)
        {
            var preview = new MigrationPreview();
            preview.SourceDirectory = LocalizationEditorData.GetLocalesDirectory(from, localesPath);
            preview.TargetDirectory = LocalizationEditorData.GetLocalesDirectory(to, localesPath);

            if (Directory.Exists(preview.SourceDirectory))
            {
                var files = GetJsonFiles(preview.SourceDirectory);
                preview.FileCount = files.Length;
                preview.HasFiles = files.Length > 0;
            }
            else
            {
                preview.HasFiles = false;
            }
            return preview;
        }

        /// <summary>
        /// 执行迁移：复制 JSON 文件到目标目录，删除源目录文件，刷新 AssetDatabase。
        /// 调用前建议先用 Preview 展示确认对话框。
        /// </summary>
        /// <returns>实际迁移的文件数。</returns>
        public static int Migrate(AssetLoadMode from, AssetLoadMode to, string localesPath)
        {
            if (from == to)
            {
                LocalizationLog.Info("源模式与目标模式相同，无需迁移。");
                return 0;
            }

            string sourceDir = LocalizationEditorData.GetLocalesDirectory(from, localesPath);
            string targetDir = LocalizationEditorData.GetLocalesDirectory(to, localesPath);

            if (!Directory.Exists(sourceDir))
            {
                LocalizationLog.Warning($"源目录不存在，无文件可迁移: {sourceDir}");
                return 0;
            }

            var files = GetJsonFiles(sourceDir);
            if (files.Length == 0)
            {
                LocalizationLog.Warning($"源目录无 JSON 文件: {sourceDir}");
                return 0;
            }

            // 确保目标目录存在
            Directory.CreateDirectory(targetDir);

            // 复制每个文件到目标目录
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string targetPath = Path.Combine(targetDir, fileName);
                // 目标已存在则覆盖
                File.Copy(file, targetPath, overwrite: true);
            }

            // 删除源目录下的 JSON 文件（保留目录或删除空目录）
            foreach (var file in files)
            {
                File.Delete(file);
            }
            // 删除对应的 .meta 文件（在 Assets 下时）
            foreach (var file in files)
            {
                string meta = file + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
            }
            // 尝试删除空源目录
            TryDeleteEmptyDirectory(sourceDir);

            AssetDatabase.Refresh();
            LocalizationEditorData.InvalidateDirectoryCache();

            LocalizationLog.Info($"迁移完成：从 {from} 迁移 {files.Length} 个文件到 {to}。\n  {sourceDir} → {targetDir}");
            return files.Length;
        }

        /// <summary>获取目录下所有精确匹配 .json 后缀的文件（排除 .json5/.bak 等）。</summary>
        private static string[] GetJsonFiles(string dir)
        {
            var all = Directory.GetFiles(dir, "*");
            var list = new System.Collections.Generic.List<string>();
            foreach (var f in all)
            {
                if (f.EndsWith(LocalizationPaths.LocaleExtension, System.StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(f);
                }
            }
            return list.ToArray();
        }

        /// <summary>若目录为空则删除（含 .meta，避免空目录残留）。</summary>
        private static void TryDeleteEmptyDirectory(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return;
                if (Directory.GetFileSystemEntries(dir).Length == 0)
                {
                    Directory.Delete(dir);
                    // 删除目录的 .meta（若在 Assets 下）
                    string meta = dir + ".meta";
                    if (File.Exists(meta)) File.Delete(meta);
                }
            }
            catch (System.Exception ex)
            {
                LocalizationLog.Warning($"删除空目录失败（可忽略）: {dir}  {ex.Message}");
            }
        }
    }
}
