using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 导入/导出 Tab。提供 CSV 导入导出工作流。
    /// 导出：把当前所有语言合并为一个 CSV（key 列 + 每语言一列），交翻译人员。
    /// 导入：把编辑后的 CSV 合并回各语言 JSON（需点「保存全部」才写盘）。
    /// </summary>
    public class ImportExportTab
    {
        public void Draw(LocalizationWindow window, List<LocaleData> locales)
        {
            EditorGUILayout.LabelField("CSV 导出", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "导出会把所有语言的词条合并为一个 CSV 文件（第一列为 key，其后每列为一种语言）。\n" +
                "可直接用 Excel/WPS 打开编辑，编辑后再导入回写各语言 JSON。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("导出为 CSV...", GUILayout.Width(160)))
                {
                    ExportCsv(locales);
                }
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(16);
            EditorGUILayout.LabelField("CSV 导入", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "导入会读取 CSV 并合并到内存中的语言数据（key 不存在则新增，存在则覆盖翻译）。\n" +
                "导入后请点击窗口右上角「保存全部」才会写入磁盘 JSON。",
                MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("从 CSV 导入...", GUILayout.Width(160)))
                {
                    ImportCsv(window, locales);
                }
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(16);
            EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);
            DrawPreview(locales);
        }

        /// <summary>导出 CSV：弹文件保存对话框。</summary>
        private void ExportCsv(List<LocaleData> locales)
        {
            if (locales == null || locales.Count == 0)
            {
                EditorUtility.DisplayDialog("导出", "没有可导出的语言数据。", "确定");
                return;
            }
            string path = EditorUtility.SaveFilePanel("导出 CSV", "", "localization", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                LocalizationImportExport.ExportToCsv(path, locales);
                EditorUtility.DisplayDialog("导出成功", $"已导出到:\n{path}", "确定");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("导出失败", ex.Message, "确定");
            }
        }

        /// <summary>导入 CSV：弹文件打开对话框，导入后刷新缓存（不读盘覆盖）。</summary>
        private void ImportCsv(LocalizationWindow window, List<LocaleData> locales)
        {
            string path = EditorUtility.OpenFilePanel("导入 CSV", "", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var stats = LocalizationImportExport.ImportFromCsv(path, locales);
                // 关键：导入数据已写入 locales（窗口的 _locales 引用），
                // 这里只能刷新各 Tab 的 key 缓存，绝不能调 Reload（Reload 会从磁盘重新加载，覆盖刚导入的内存数据）。
                window.RefreshCaches();
                window.MarkDirty(); // 标记未保存，提示用户点「保存全部」写入磁盘
                EditorUtility.DisplayDialog("导入成功",
                    $"新增 {stats.AddedKeys} 个 key，更新 {stats.UpdatedKeys} 个翻译。\n请点击窗口右上角「保存全部」写入磁盘。",
                    "确定");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("导入失败", ex.Message, "确定");
            }
        }

        /// <summary>预览当前数据的 CSV 文本（前若干行）。</summary>
        private void DrawPreview(List<LocaleData> locales)
        {
            if (locales == null || locales.Count == 0) return;
            string csv = LocalizationImportExport.ExportToCsvString(locales);
            // 截断显示前 1500 字符避免卡顿
            int max = 1500;
            string preview = csv.Length > max ? csv.Substring(0, max) + "\n...（已截断）" : csv;
            EditorGUILayout.TextArea(preview, GUILayout.MinHeight(120));
        }
    }
}
