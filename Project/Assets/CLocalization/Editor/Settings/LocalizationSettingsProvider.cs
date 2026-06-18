using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 在 Project Settings 窗口中注册「Localization」配置面板。
    /// 用户可在 Edit > Project Settings > CLocalization 中管理语言列表、默认语言、回退策略等。
    /// 自动加载/创建 Settings 资源（若无则调引导脚本生成）。
    /// </summary>
    public class LocalizationSettingsProvider : SettingsProvider
    {
        /// <summary>Settings 资源序列化对象，用于 Inspector 风格编辑。</summary>
        private SerializedObject _serializedObject;

        /// <summary>已落盘/已应用的模式（用于检测下拉值变化，持久显示迁移预览，避免单帧闪烁）。</summary>
        private AssetLoadMode _appliedMode;

        /// <summary>_appliedMode 是否已初始化（首次访问时从 Settings 同步）。</summary>
        private bool _appliedModeInitialized;

        /// <summary>已应用的 localesPath 基准（用于检测路径变化，触发文件迁移预览）。</summary>
        private string _appliedLocalesPath;
        private bool _appliedLocalesPathInitialized;

        public LocalizationSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        /// <summary>注册到 Project Settings 窗口。</summary>
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new LocalizationSettingsProvider("Project/CLocalization", SettingsScope.Project)
            {
                label = "CLocalization",
                keywords = new[] { "Localization", "Language", "多语言", "本地化" }
            };
        }

        /// <summary>懒加载 SerializedObject（不 override OnActivate 以规避跨版本签名差异）。</summary>
        private void EnsureSerializedObject()
        {
            if (_serializedObject != null && _serializedObject.targetObject != null) return;
            var settings = LocalizationSetup.LoadOrCreateSettings();
            if (settings != null)
            {
                _serializedObject = new SerializedObject(settings);
            }
        }

        public override void OnGUI(string searchContext)
        {
            EnsureSerializedObject();

            if (_serializedObject == null || _serializedObject.targetObject == null)
            {
                EditorGUILayout.HelpBox("未找到 LocalizationSettings 资源。点击下方按钮创建。", MessageType.Warning);
                if (GUILayout.Button("创建 Settings 资源", GUILayout.Width(200)))
                {
                    LocalizationSetup.CreateSettingsAsset();
                    var s = LocalizationSetup.LoadOrCreateSettings();
                    if (s != null) _serializedObject = new SerializedObject(s);
                }
                return;
            }

            _serializedObject.Update();

            EditorGUILayout.LabelField("语言列表", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("languages"), true);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("行为设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("defaultLanguageCode"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("persistLanguageChoice"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("useSystemLanguageByDefault"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("fallbackToDefaultLanguage"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("logMissingKeys"));

            EditorGUILayout.Space(8);
            DrawAssetLoadSection();

            if (_serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_serializedObject.targetObject);
                AssetDatabase.SaveAssets();
                LocalizationEditorData.InvalidateDirectoryCache();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "提示：修改语言列表后，请到 Tools > CLocalization > Localization Window 中编辑各语言词条，或通过 CSV 导入导出协作。\n" +
                "切换资源加载方式后，语言文件会自动迁移到新目录。",
                MessageType.Info);
        }

        /// <summary>绘制资源加载方式配置区：LoadMode 选择 + 路径配置 + 当前目录显示 + 迁移按钮。</summary>
        private void DrawAssetLoadSection()
        {
            EditorGUILayout.LabelField("资源加载方式", EditorStyles.boldLabel);

            var modeProp = _serializedObject.FindProperty("assetLoadMode");
            var localesPathProp = _serializedObject.FindProperty("localesPath");
            var assetsPathProp = _serializedObject.FindProperty("assetsPath");

            // 首次访问时，用 Settings 当前落盘的值初始化已应用基准（模式 + 路径）
            if (!_appliedModeInitialized)
            {
                _appliedMode = (AssetLoadMode)modeProp.enumValueIndex;
                _appliedModeInitialized = true;
            }
            if (!_appliedLocalesPathInitialized)
            {
                _appliedLocalesPath = localesPathProp.stringValue;
                _appliedLocalesPathInitialized = true;
            }

            EditorGUILayout.PropertyField(modeProp);
            EditorGUILayout.PropertyField(localesPathProp);

            // localesPath 非法字符校验（拒绝绝对路径、盘符、非法字符，避免 Path.Combine 错乱/崩溃）
            string localesPathRaw = localesPathProp.stringValue;
            if (!IsPathSafe(localesPathRaw, out string pathError))
            {
                EditorGUILayout.HelpBox("⚠ 语言路径非法：" + pathError + "\n只允许字母、数字、下划线、横杠、正斜杠（相对路径），不能含盘符或绝对路径。", MessageType.Error);
            }

            EditorGUILayout.PropertyField(assetsPathProp);
            string assetsPathRaw = assetsPathProp.stringValue;
            if (!IsPathSafe(assetsPathRaw, out string assetsError))
            {
                EditorGUILayout.HelpBox("⚠ 资源路径非法：" + assetsError, MessageType.Error);
            }

            AssetLoadMode currentMode = (AssetLoadMode)modeProp.enumValueIndex;
            string localesPath = string.IsNullOrEmpty(localesPathRaw) ? "CLocalization/Locales" : localesPathRaw;

            // 显示当前下拉所选模式对应的实际磁盘目录
            string currentDir = LocalizationEditorData.GetLocalesDirectory(currentMode, localesPath);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("当前语言目录（所选模式）:", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(currentDir, MessageType.None);

            // StreamingAssets 模式限制提示
            if (currentMode == AssetLoadMode.StreamingAssets)
            {
                EditorGUILayout.HelpBox(
                    "⚠ StreamingAssets 模式限制：\n" +
                    "• 仅支持【文本】本地化，Sprite/Audio/Font 需用 Resources 或自定义 Loader\n" +
                    "• Android 平台必须用异步加载（SetLanguageAsync）\n" +
                    "• 该模式下 Project 窗口拖入 JSON 不会自动同步，需点下方「刷新语言列表」",
                    MessageType.Warning);
            }

            // localesPath 变化检测：路径改变时，把文件从旧路径迁到新路径（同模式下）
            // _appliedLocalesPath 是已应用基准，localesPathRaw 是当前输入；不同时显示迁移预览
            if (_appliedLocalesPathInitialized
                && IsPathSafe(localesPathRaw, out _)
                && localesPathRaw != _appliedLocalesPath
                && !string.IsNullOrEmpty(_appliedLocalesPath))
            {
                EditorGUILayout.Space(6);
                string fromDir = LocalizationEditorData.GetLocalesDirectory(currentMode, _appliedLocalesPath);
                string toDir = LocalizationEditorData.GetLocalesDirectory(currentMode, localesPathRaw);
                int fileCount = CountJsonFiles(fromDir);
                EditorGUILayout.HelpBox(
                    $"检测到语言路径变更：\n  {_appliedLocalesPath} → {localesPathRaw}\n" +
                    $"{fromDir}\n  → {toDir}\n" +
                    $"将迁移 {fileCount} 个语言文件。",
                    fileCount > 0 ? MessageType.Info : MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("迁移文件并应用路径", GUILayout.Height(28)))
                    {
                        if (EditorUtility.DisplayDialog("迁移路径",
                            $"确认把 {fileCount} 个语言文件从\n{_appliedLocalesPath}\n迁到\n{localesPathRaw}？",
                            "迁移", "取消"))
                        {
                            MigrateBetweenPaths(fromDir, toDir);
                            _appliedLocalesPath = localesPathRaw;
                            _serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(_serializedObject.targetObject);
                            AssetDatabase.SaveAssets();
                            LocalizationEditorData.InvalidateDirectoryCache();
                            LocalizationSetup.SyncSettingsFromLocales();
                        }
                    }
                    if (GUILayout.Button("撤销路径修改", GUILayout.Width(110), GUILayout.Height(28)))
                    {
                        localesPathProp.stringValue = _appliedLocalesPath;
                    }
                }
            }

            // 模式变化检测：_appliedMode 是已应用基准，currentMode 是下拉当前值
            // 两者不同时，持续显示迁移预览（直到用户点迁移或撤销），避免单帧闪烁
            if (currentMode != _appliedMode)
            {
                AssetLoadMode fromMode = _appliedMode;
                AssetLoadMode toMode = currentMode;
                EditorGUILayout.Space(6);
                var preview = LocalizationAssetMigrator.Preview(fromMode, toMode, localesPath);
                if (preview.HasFiles)
                {
                    EditorGUILayout.HelpBox(
                        $"检测到加载方式变更（{fromMode} → {toMode}）。\n" +
                        $"将迁移 {preview.FileCount} 个语言 JSON 文件：\n  {preview.SourceDirectory}\n  → {preview.TargetDirectory}",
                        MessageType.Info);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("迁移资源并应用", GUILayout.Height(30)))
                        {
                            // 资源提示：切到 StreamingAssets 时，本地化资源（Sprite/Audio/Font）无法迁移且不再支持加载
                            string assetNote = toMode == AssetLoadMode.StreamingAssets
                                ? "\n\n⚠ 注意：本地化资源（Sprite/Audio/Font）不会迁移，且 StreamingAssets 模式不支持加载它们。\n" +
                                  "如使用了 LocalizeSprite/AudioSource/Font 组件，请改回 Resources 模式，或保留资源在原 Resources 目录手动处理。"
                                : "";
                            if (EditorUtility.DisplayDialog("迁移资源",
                                $"确认迁移 {preview.FileCount} 个语言文件从 {fromMode} 到 {toMode}？\n\n" +
                                $"源文件将被移动到新目录，旧目录的 JSON 会删除。\n（可通过版本控制回滚）{assetNote}",
                                "迁移", "取消"))
                            {
                                int migrated = LocalizationAssetMigrator.Migrate(fromMode, toMode, localesPath);
                                _serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(_serializedObject.targetObject);
                                AssetDatabase.SaveAssets();
                                LocalizationEditorData.InvalidateDirectoryCache();
                                // 迁移成功后，更新已应用基准为新模式
                                _appliedMode = toMode;
                                LocalizationSetup.SyncSettingsFromLocales();
                                // 结果提示（含资源处理说明）
                                string resultAssetNote = toMode == AssetLoadMode.StreamingAssets
                                    ? $"\n\n⚠ 已迁移 {migrated} 个语言文件。本地化资源（Sprite/Audio/Font）未迁移，StreamingAssets 模式下不再支持加载，请手动处理。"
                                    : $"\n\n已迁移 {migrated} 个语言文件。";
                                EditorUtility.DisplayDialog("迁移完成", "迁移完成。" + resultAssetNote, "确定");
                            }
                        }
                        if (GUILayout.Button("撤销切换", GUILayout.Width(90), GUILayout.Height(30)))
                        {
                            // 恢复下拉值为已应用的模式
                            modeProp.enumValueIndex = (int)_appliedMode;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"源目录（{fromMode}）无语言文件，无需迁移。点下方「直接应用」保存新模式即可。",
                        MessageType.Info);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("直接应用新模式", GUILayout.Height(28)))
                        {
                            _appliedMode = toMode;
                            _serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(_serializedObject.targetObject);
                            AssetDatabase.SaveAssets();
                            LocalizationEditorData.InvalidateDirectoryCache();
                        }
                        if (GUILayout.Button("撤销切换", GUILayout.Width(90), GUILayout.Height(28)))
                        {
                            modeProp.enumValueIndex = (int)_appliedMode;
                        }
                    }
                }
            }

            // 手动刷新语言列表按钮（StreamingAssets 模式 AssetPostprocessor 不触发）
            EditorGUILayout.Space(4);
            if (GUILayout.Button("刷新语言列表（同步 Settings）", GUILayout.Height(24)))
            {
                LocalizationEditorData.InvalidateDirectoryCache();
                LocalizationSetup.SyncSettingsFromLocales();
            }
        }

        /// <summary>
        /// 校验相对路径安全性：禁止绝对路径、盘符、非法字符。
        /// 只允许字母/数字/下划线/横杠/正斜杠组成的相对路径。
        /// </summary>
        private static bool IsPathSafe(string path, out string error)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "路径为空。";
                return false;
            }
            // 绝对路径/盘符（Windows 盘符 D: 或 Unix 绝对路径 /）
            if (System.IO.Path.IsPathRooted(path))
            {
                error = "不能使用绝对路径，必须是相对路径（如 CLocalization/Locales）。";
                return false;
            }
            if (path.Contains(":") || path.Contains("\\"))
            {
                error = "不能含盘符(:)或反斜杠(\\)，请用正斜杠 /。";
                return false;
            }
            // GetInvalidPathChars 校验
            char[] invalid = System.IO.Path.GetInvalidPathChars();
            foreach (char c in path)
            {
                foreach (char ic in invalid)
                {
                    if (c == ic)
                    {
                        error = $"含非法字符 '{c}'。";
                        return false;
                    }
                }
            }
            error = null;
            return true;
        }

        /// <summary>统计目录下 .json 文件数（精确后缀，排除 .json5 等）。</summary>
        private static int CountJsonFiles(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir)) return 0;
            int count = 0;
            foreach (var f in System.IO.Directory.GetFiles(dir, "*"))
            {
                if (f.EndsWith(LocalizationPaths.LocaleExtension, System.StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }

        /// <summary>在同模式下的两个路径间迁移语言文件（复制+删源+删 .meta+刷新）。</summary>
        private static void MigrateBetweenPaths(string fromDir, string toDir)
        {
            if (!System.IO.Directory.Exists(fromDir)) return;
            System.IO.Directory.CreateDirectory(toDir);
            int n = 0;
            foreach (var f in System.IO.Directory.GetFiles(fromDir, "*"))
            {
                if (!f.EndsWith(LocalizationPaths.LocaleExtension, System.StringComparison.OrdinalIgnoreCase)) continue;
                string name = System.IO.Path.GetFileName(f);
                System.IO.File.Copy(f, System.IO.Path.Combine(toDir, name), overwrite: true);
                System.IO.File.Delete(f);
                string meta = f + ".meta";
                if (System.IO.File.Exists(meta)) System.IO.File.Delete(meta);
                n++;
            }
            // 尝试删除空源目录
            try
            {
                if (System.IO.Directory.Exists(fromDir) && System.IO.Directory.GetFileSystemEntries(fromDir).Length == 0)
                {
                    System.IO.Directory.Delete(fromDir);
                    string dmeta = fromDir + ".meta";
                    if (System.IO.File.Exists(dmeta)) System.IO.File.Delete(dmeta);
                }
            }
            catch (System.Exception ex) { LocalizationLog.Warning($"删除空目录失败（可忽略）: {fromDir}  {ex.Message}"); }

            AssetDatabase.Refresh();
            LocalizationLog.Info($"路径迁移完成：{n} 个文件  {fromDir} → {toDir}");
        }
    }
}
