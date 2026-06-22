using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 资源映射配置 Tab。可视化配置 key × 语言 → Sprite/AudioClip/Font 的映射。
    /// 顶部切换资源类型（Sprite/AudioClip/Font），左侧 key 列表，右侧按语言配置资源引用。
    /// 操作对应三张映射表 ScriptableObject（SpriteAssetMap/AudioClipAssetMap/FontAssetMap）。
    /// </summary>
    public class AssetsTab
    {
        /// <summary>资源类型标签。</summary>
        private static readonly string[] TypeNames = { "Sprite", "AudioClip", "Font" };

        /// <summary>当前选中的资源类型（0=Sprite, 1=AudioClip, 2=Font）。</summary>
        private int _currentType;

        /// <summary>搜索关键字。</summary>
        private string _search = "";

        /// <summary>当前选中的 key。</summary>
        private string _selectedKey;

        /// <summary>新增 key 输入。</summary>
        private string _newKey = "";

        /// <summary>左侧 key 列表滚动。</summary>
        private Vector2 _keyListScroll;
        /// <summary>右侧配置区滚动。</summary>
        private Vector2 _detailScroll;

        /// <summary>当前窗口引用。</summary>
        private LocalizationWindow _window;

        public void Draw(LocalizationWindow window, List<LocaleData> locales)
        {
            _window = window;
            // 确保三张映射表已创建
            LocalizationSetup.LoadOrCreateAssetMaps();
            // 旧数据迁移：asset 强引用 → assetPath（仅首次检测，迁移后不再触发）
            var settings = LocalizationSetup.LoadOrCreateSettings();
            if (settings != null)
            {
                MigrateLegacyAssetRefs(settings.SpriteMap);
                MigrateLegacyAssetRefs(settings.AudioMap);
                MigrateLegacyAssetRefs(settings.FontMap);
            }

            // 顶部：资源类型切换
            _currentType = GUILayout.Toolbar(_currentType, TypeNames, GUILayout.Height(24));
            EditorGUILayout.Space(4);

            // 获取当前操作的映射表
            AssetMapBase map = GetCurrentMap();
            if (map == null)
            {
                EditorGUILayout.HelpBox("映射表加载失败，请检查 Settings 的资源映射表字段。", MessageType.Error);
                return;
            }

            DrawKeyListAndDetail(map, locales);
        }

        /// <summary>获取当前类型对应的映射表。</summary>
        private AssetMapBase GetCurrentMap()
        {
            var settings = LocalizationSetup.LoadOrCreateSettings();
            if (settings == null) return null;
            switch (_currentType)
            {
                case 0: return settings.SpriteMap;
                case 1: return settings.AudioMap;
                case 2: return settings.FontMap;
                default: return null;
            }
        }

        /// <summary>获取当前类型对应的资源 .NET 类型（用于 ObjectField 约束）。</summary>
        private System.Type GetCurrentAssetType()
        {
            switch (_currentType)
            {
                case 0: return typeof(Sprite);
                case 1: return typeof(AudioClip);
                case 2: return typeof(Object); // Font 可能是 TMP_FontAsset 或 Font，用 Object 兼容
                default: return typeof(Object);
            }
        }

        /// <summary>左侧 key 列表 + 右侧配置区。</summary>
        private void DrawKeyListAndDetail(AssetMapBase map, List<LocaleData> locales)
        {
            var allKeys = map.CollectKeys();
            var languages = LocalizationSetup.LoadOrCreateSettings()?.Languages;

            using (new EditorGUILayout.HorizontalScope())
            {
                // ---------- 左侧：key 列表 ----------
                DrawKeyListPanel(map, allKeys, languages);

                // ---------- 右侧：配置区 ----------
                DrawDetailPanel(map, allKeys, languages);
            }
        }

        /// <summary>左侧 key 列表面板。</summary>
        private void DrawKeyListPanel(AssetMapBase map, List<string> allKeys, IReadOnlyList<LanguageInfo> languages)
        {
            // 搜索 + 新增
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220));
            _search = EditorGUILayout.TextField("搜索", _search);
            using (new EditorGUILayout.HorizontalScope())
            {
                _newKey = EditorGUILayout.TextField(_newKey);
                if (GUILayout.Button("+", GUILayout.Width(24)))
                {
                    AddKey(map, _newKey, languages);
                    _newKey = "";
                    GUI.FocusControl(null);
                }
            }

            _keyListScroll = EditorGUILayout.BeginScrollView(_keyListScroll);
            foreach (var key in allKeys)
            {
                if (!string.IsNullOrEmpty(_search) && !key.ToLowerInvariant().Contains(_search.ToLowerInvariant()))
                    continue;

                bool isSelected = key == _selectedKey;
                var style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                // 显示 key + 已配置语言数
                int configured = map.CountConfiguredLanguages(key);
                string label = $"{key}  ({configured}/{(languages?.Count ?? 0)})";
                if (GUILayout.Button(label, isSelected ? EditorStyles.whiteBoldLabel : EditorStyles.miniButton))
                {
                    _selectedKey = key;
                }
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>右侧配置面板：选中 key 的各语言资源字段。</summary>
        private void DrawDetailPanel(AssetMapBase map, List<string> allKeys, IReadOnlyList<LanguageInfo> languages)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            if (string.IsNullOrEmpty(_selectedKey) || !allKeys.Contains(_selectedKey))
            {
                EditorGUILayout.HelpBox("← 在左侧选择一个 key，或点「+」新增，然后为各语言配置资源。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Key: {_selectedKey}", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                if (languages == null || languages.Count == 0)
                {
                    EditorGUILayout.HelpBox("无可用语言，请先在「语言」Tab 添加语言。", MessageType.Warning);
                }
                else
                {
                    // 先确保该 key 在所有语言都有条目（直接操作 map 实际数据），
                    // 再构造 SerializedObject 读取最新状态编辑（避免 SerializedObject 快照与新条目不同步）
                    bool entriesAdded = false;
                    foreach (var lang in languages)
                    {
                        string code = lang?.Code;
                        if (string.IsNullOrEmpty(code)) continue;
                        int beforeCount = map.Entries.Count;
                        map.EnsureEntry(_selectedKey, code);
                        if (map.Entries.Count > beforeCount) entriesAdded = true;
                    }
                    if (entriesAdded)
                    {
                        EditorUtility.SetDirty(map);
                        AssetDatabase.SaveAssets();
                    }

                    // 构造 SerializedObject 读取最新 map 状态（条目已齐全）
                    var so = new SerializedObject(map);
                    var entries = so.FindProperty("entries");

                    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

                    System.Type assetType = GetCurrentAssetType();
                    foreach (var lang in languages)
                    {
                        string code = lang?.Code;
                        if (string.IsNullOrEmpty(code)) continue;

                        // 查找该 key+code 的条目索引（此时已确保存在）
                        int entryIdx = FindEntryIndex(map, _selectedKey, code);
                        if (entryIdx < 0 || entryIdx >= entries.arraySize) continue;

                        var element = entries.GetArrayElementAtIndex(entryIdx);
                        var assetProp = element.FindPropertyRelative("asset");
                        var assetPathProp = element.FindPropertyRelative("assetPath");
                        var pathTypeProp = element.FindPropertyRelative("pathType");

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(code, GUILayout.Width(80));

                            // ObjectField 拖拽：显示当前预览引用（用 asset 字段）
                            var currentPreview = assetProp != null ? assetProp.objectReferenceValue : null;
                            var newAsset = EditorGUILayout.ObjectField(currentPreview, assetType, false);
                            if (newAsset != currentPreview)
                            {
                                // 拖入/替换资源：存路径 + 回填预览
                                if (newAsset != null)
                                {
                                    string fullPath = AssetDatabase.GetAssetPath(newAsset);
                                    var (storedPath, pType) = ConvertToStoredPath(fullPath);
                                    if (assetPathProp != null) assetPathProp.stringValue = storedPath;
                                    if (pathTypeProp != null) pathTypeProp.enumValueIndex = (int)pType;
                                    if (assetProp != null) assetProp.objectReferenceValue = newAsset;
                                }
                                else
                                {
                                    // 清空
                                    if (assetPathProp != null) assetPathProp.stringValue = "";
                                    if (assetProp != null) assetProp.objectReferenceValue = null;
                                }
                            }

                            // 配置状态标记（基于路径是否非空）
                            bool configured = assetPathProp != null && !string.IsNullOrEmpty(assetPathProp.stringValue);
                            GUILayout.Label(configured ? "✓" : "未配置", EditorStyles.miniLabel, GUILayout.Width(50));
                        }
                    }

                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.Space(6);

                    if (so.ApplyModifiedProperties())
                    {
                        SaveMap(map);
                    }
                }

                // 删除该 key（在 languages 块外，与是否有语言无关）
                EditorGUILayout.Space(6);
                if (GUILayout.Button("删除该 key 的所有映射", GUILayout.Width(180)))
                {
                    if (EditorUtility.DisplayDialog("删除映射", $"删除 key \"{_selectedKey}\" 的所有语言映射？", "删除", "取消"))
                    {
                        map.RemoveKey(_selectedKey);
                        _selectedKey = null;
                        SaveMap(map);
                    }
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>新增 key：为所有语言创建空映射条目。</summary>
        private void AddKey(AssetMapBase map, string key, IReadOnlyList<LanguageInfo> languages)
        {
            key = key?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                LocalizationLog.Warning("key 不能为空。");
                return;
            }
            var codes = new List<string>();
            if (languages != null)
            {
                foreach (var l in languages)
                {
                    if (l != null && !string.IsNullOrEmpty(l.Code)) codes.Add(l.Code);
                }
            }
            map.AddKey(key, codes);
            _selectedKey = key;
            SaveMap(map);
        }

        /// <summary>查找 key+code 的条目索引（只读，条目应已由 EnsureEntry 创建）。</summary>
        private int FindEntryIndex(AssetMapBase map, string key, string code)
        {
            var entries = map.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key && entries[i].languageCode == code) return i;
            }
            return -1;
        }

        /// <summary>
        /// 将完整工程路径转换为存储路径 + 路径类型。
        /// 若路径含 /Resources/，截取为 Resources 相对路径（去扩展名）+ type=Resources；
        /// 否则存完整路径 + type=FullPath。
        /// </summary>
        public static (string path, AssetPathType type) ConvertToStoredPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return ("", AssetPathType.Resources);

            // 检查是否在 Resources 目录下
            int resIdx = fullPath.IndexOf("/Resources/", System.StringComparison.OrdinalIgnoreCase);
            if (resIdx >= 0)
            {
                // 截取 Resources/ 之后的部分，去掉扩展名
                string afterResources = fullPath.Substring(resIdx + "/Resources/".Length);
                string withoutExt = System.IO.Path.ChangeExtension(afterResources, null);
                return (withoutExt, AssetPathType.Resources);
            }

            // 不在 Resources 下：存完整工程路径
            return (fullPath, AssetPathType.FullPath);
        }

        /// <summary>检测并迁移旧数据（asset 强引用 → assetPath）。首次打开资源 Tab 时调用。</summary>
        private static void MigrateLegacyAssetRefs(AssetMapBase map)
        {
            if (map == null) return;
            bool changed = false;
            foreach (var entry in map.Entries)
            {
                // asset 有值但 assetPath 为空 → 迁移
                if (entry.Asset != null && string.IsNullOrEmpty(entry.assetPath))
                {
                    string fullPath = AssetDatabase.GetAssetPath(entry.Asset);
                    var (storedPath, pType) = ConvertToStoredPath(fullPath);
                    entry.assetPath = storedPath;
                    entry.pathType = pType;
                    changed = true;
                }
            }
            if (changed)
            {
                EditorUtility.SetDirty(map);
                AssetDatabase.SaveAssets();
                LocalizationLog.Info($"已迁移 {map.name} 的旧强引用为路径存储。");
            }
        }

        /// <summary>保存映射表（SetDirty + SaveAssets）。</summary>
        private void SaveMap(AssetMapBase map)
        {
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            _window?.Repaint();
        }
    }
}
