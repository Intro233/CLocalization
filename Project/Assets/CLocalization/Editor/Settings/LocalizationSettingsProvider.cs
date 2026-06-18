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

            // 首次访问时，用 Settings 当前落盘的模式初始化 _appliedMode（作为"已应用"基准）
            if (!_appliedModeInitialized)
            {
                _appliedMode = (AssetLoadMode)modeProp.enumValueIndex;
                _appliedModeInitialized = true;
            }

            EditorGUILayout.PropertyField(modeProp);
            EditorGUILayout.PropertyField(localesPathProp);
            EditorGUILayout.PropertyField(assetsPathProp);

            AssetLoadMode currentMode = (AssetLoadMode)modeProp.enumValueIndex;
            string localesPath = localesPathProp.stringValue;

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
                            if (EditorUtility.DisplayDialog("迁移资源",
                                $"确认迁移 {preview.FileCount} 个语言文件从 {fromMode} 到 {toMode}？\n\n" +
                                $"源文件将被移动到新目录，旧目录的 JSON 会删除。\n（可通过版本控制回滚）",
                                "迁移", "取消"))
                            {
                                LocalizationAssetMigrator.Migrate(fromMode, toMode, localesPath);
                                _serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(_serializedObject.targetObject);
                                AssetDatabase.SaveAssets();
                                LocalizationEditorData.InvalidateDirectoryCache();
                                // 迁移成功后，更新已应用基准为新模式
                                _appliedMode = toMode;
                                LocalizationSetup.SyncSettingsFromLocales();
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
    }
}
