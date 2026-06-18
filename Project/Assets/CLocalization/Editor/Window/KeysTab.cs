using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 词条编辑 Tab。以表格形式展示 key + 各语言翻译，支持：
    /// - 搜索过滤（按 key 或任意语言文本）
    /// - 【表头点击排序】（key 列升/降序、各语言列按翻译排序）
    /// - 【可拖拽列宽】（key 列与各语言列宽度可调）
    /// - 【仅看未翻译】过滤开关
    /// - 【复制 key】到剪贴板
    /// - 【多行文本编辑】（长翻译可换行）
    /// - 新增/删除 key、直接单元格编辑、未翻译高亮
    /// - 分页（避免万 key 卡死）
    /// </summary>
    public class KeysTab
    {
        // ---------- 搜索与过滤 ----------
        /// <summary>搜索关键字。</summary>
        private string _search = "";
        /// <summary>是否仅显示未翻译（空值）的 key。</summary>
        private bool _onlyMissing;

        // ---------- 排序 ----------
        /// <summary>当前排序列：null=默认 key 字母序；否则为语言代码（按该语言翻译排序）。</summary>
        private string _sortColumn;
        /// <summary>是否降序。</summary>
        private bool _sortDescending;

        // ---------- 列宽（可拖拽，每列独立） ----------
        /// <summary>key 列宽。</summary>
        private float _keyColumnWidth = 180f;
        /// <summary>各语言列宽（按语言代码独立存储，拖某一列只影响该列）。</summary>
        private readonly Dictionary<string, float> _langColumnWidths = new Dictionary<string, float>();
        /// <summary>语言列默认宽度。</summary>
        private const float DefaultLangColumnWidth = 200f;
        private const float MinColumnWidth = 80f;

        /// <summary>获取指定语言列的宽度（无记录则用默认值）。</summary>
        private float GetLangColumnWidth(string code)
        {
            return _langColumnWidths.TryGetValue(code, out var w) ? w : DefaultLangColumnWidth;
        }

        /// <summary>设置指定语言列的宽度。</summary>
        private void SetLangColumnWidth(string code, float width)
        {
            _langColumnWidths[code] = Mathf.Max(MinColumnWidth, width);
        }

        // ---------- key 列表与分页 ----------
        /// <summary>排序后的 key 列表（缓存）。</summary>
        private List<string> _keys = new List<string>();
        /// <summary>当前正在新增的 key 输入。</summary>
        private string _newKey = "";
        /// <summary>滚动位置。</summary>
        private Vector2 _scroll;
        /// <summary>每页最大行数。</summary>
        private const int PageSize = 200;
        /// <summary>当前页码（从 0 开始）。</summary>
        private int _page;
        /// <summary>当前窗口引用。</summary>
        private LocalizationWindow _window;

        // ---------- 过滤排序结果缓存（避免每帧全量重算） ----------
        /// <summary>缓存的过滤+排序结果。</summary>
        private List<string> _visibleKeysCache = new List<string>();
        /// <summary>缓存是否需要重算（脏标记）。</summary>
        private bool _cacheDirty = true;
        /// <summary>生成缓存时的参数快照，用于检测参数变化。</summary>
        private string _cachedSearch;
        private bool _cachedOnlyMissing;
        private string _cachedSortColumn;
        private bool _cachedSortDescending;
        private int _cachedKeyCount = -1;

        // ---------- 行选中（供诊断跳转定位） ----------
        /// <summary>当前选中的 key（高亮 + 滚动定位）。</summary>
        private string _selectedKey;

        /// <summary>数据变化时重建 key 列表。</summary>
        public void OnDataChanged(List<LocaleData> locales)
        {
            _keys = LocalizationEditorData.CollectAllKeys(locales);
            _cacheDirty = true; // 标记过滤排序缓存失效，下次 GetVisibleKeys 重算
        }

        /// <summary>绘制 Tab 内容。</summary>
        public void Draw(LocalizationWindow window, List<LocaleData> locales)
        {
            _window = window;
            DrawSearchBar(locales);
            EditorGUILayout.Space(2);
            DrawKeyTable(locales);
        }

        // ---------- 搜索栏 ----------

        /// <summary>搜索栏 + 仅看未翻译 + 新增 key + 批量操作。</summary>
        private void DrawSearchBar(List<LocaleData> locales)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // 命名搜索框，供 Ctrl+F 快捷键聚焦
                GUI.SetNextControlName("KeysTabSearch");
                _search = EditorGUILayout.TextField("搜索", _search);

                GUILayout.Space(8);
                // 仅看未翻译开关（译者定位缺失项）
                bool newOnlyMissing = EditorGUILayout.ToggleLeft("仅看未翻译", _onlyMissing, GUILayout.Width(110));
                if (newOnlyMissing != _onlyMissing)
                {
                    _onlyMissing = newOnlyMissing;
                    _page = 0;
                }

                GUILayout.Space(8);
                _newKey = EditorGUILayout.TextField("新增 key", _newKey);
                if (GUILayout.Button("+", GUILayout.Width(24)))
                {
                    AddKey(locales, _newKey);
                    _newKey = "";
                    GUI.FocusControl(null);
                }
            }

            // 批量操作栏（作用于当前过滤结果）
            DrawBatchOperations(locales);
        }

        /// <summary>批量操作栏：填充空值（从默认语言）、删除过滤结果。</summary>
        private void DrawBatchOperations(List<LocaleData> locales)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("批量操作:", GUILayout.Width(60));

                // 从默认语言填充所有可见 key 的空翻译
                if (GUILayout.Button("填充空值(默认语言)", GUILayout.Width(150)))
                {
                    DoFillMissingFromDefault(locales);
                }

                GUILayout.Space(8);
                // 删除当前过滤结果的所有 key
                if (GUILayout.Button("删除过滤结果", GUILayout.Width(110)))
                {
                    DoDeleteFiltered(locales);
                }

                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>把当前过滤结果中所有语言的空翻译，用默认语言的值填充。</summary>
        private void DoFillMissingFromDefault(List<LocaleData> locales)
        {
            if (locales == null || locales.Count == 0) return;
            List<string> visible = GetFilteredKeys(locales);
            // 默认语言取 settings.DefaultLanguageCode 对应的 locale（非列表第一个，顺序不可靠）
            string defaultCode = LocalizationSetup.LoadOrCreateSettings()?.DefaultLanguageCode;
            LocaleData defaultLocale = null;
            if (!string.IsNullOrEmpty(defaultCode))
            {
                foreach (var l in locales)
                {
                    if (l?.Meta?.Code == defaultCode) { defaultLocale = l; break; }
                }
            }
            if (defaultLocale == null)
            {
                LocalizationLog.Warning("未找到默认语言，无法填充。请在 Settings 中设置 DefaultLanguageCode。");
                return;
            }

            int filled = 0;
            foreach (var key in visible)
            {
                foreach (var locale in locales)
                {
                    if (locale == defaultLocale) continue;
                    if (locale.Entries == null) continue;
                    bool has = locale.Entries.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v);
                    if (!has && defaultLocale.Entries != null
                        && defaultLocale.Entries.TryGetValue(key, out var dv) && !string.IsNullOrEmpty(dv))
                    {
                        locale.Entries[key] = dv;
                        filled++;
                    }
                }
            }
            if (filled > 0) _window?.MarkDirty();
            LocalizationLog.Info($"批量填充完成：共填充 {filled} 个空翻译（来源：默认语言）。");
        }

        /// <summary>删除当前过滤结果中的所有 key。</summary>
        private void DoDeleteFiltered(List<LocaleData> locales)
        {
            List<string> visible = GetFilteredKeys(locales);
            if (visible.Count == 0)
            {
                LocalizationLog.Warning("当前无可见 key 可删除。");
                return;
            }
            if (!EditorUtility.DisplayDialog("批量删除",
                $"确认删除当前过滤结果中的 {visible.Count} 个 key 及其所有语言翻译？\n（此操作不可通过 Ctrl+Z 撤销，保存前可点「重新加载」丢弃）",
                "全部删除", "取消"))
            {
                return;
            }
            foreach (var key in visible)
            {
                foreach (var locale in locales)
                {
                    if (locale.Entries != null) locale.Entries.Remove(key);
                }
                _keys.Remove(key);
            }
            OnDataChanged(locales);
            _window?.MarkDirty();
            LocalizationLog.Info($"批量删除完成：已删除 {visible.Count} 个 key。");
        }

        // ---------- 表格 ----------

        /// <summary>绘制词条表格（表头 + 滚动数据行 + 分页）。</summary>
        private void DrawKeyTable(List<LocaleData> locales)
        {
            if (locales == null || locales.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无语言数据。请到「语言」Tab 添加语言，或检查语言目录：" + LocalizationEditorData.LocalesDirectory, MessageType.Info);
                return;
            }

            // 按需重算过滤+排序结果（仅在搜索/过滤/排序/数据变化时），避免每帧全量重算卡顿
            List<string> visibleKeys = GetVisibleKeys(locales);

            // 定位到选中 key（供诊断跳转：滚动到包含选中 key 的页）
            EnsureSelectionVisible(visibleKeys);

            // 分页
            int totalPages = (visibleKeys.Count + PageSize - 1) / PageSize;
            if (totalPages == 0) totalPages = 1;
            if (_page >= totalPages) _page = totalPages - 1;
            if (_page < 0) _page = 0;
            int start = _page * PageSize;
            int end = System.Math.Min(start + PageSize, visibleKeys.Count);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // 表头（可点击排序 + 可拖拽列宽）
            DrawTableHeader(locales);

            // 数据行（仅当前页）
            for (int i = start; i < end; i++)
            {
                DrawRow(visibleKeys[i], locales, i % 2 == 0);
            }

            EditorGUILayout.EndScrollView();

            // 底部：分页 + 计数
            DrawFooter(visibleKeys, totalPages);
        }

        /// <summary>绘制表头：可点击排序 + 可拖拽列宽分隔条。</summary>
        private void DrawTableHeader(List<LocaleData> locales)
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                // Key 列表头（点击排序）
                DrawSortableHeader("Key", null, _keyColumnWidth, isKeyColumn: true);
                foreach (var locale in locales)
                {
                    string code = locale.Meta?.Code ?? "?";
                    DrawSortableHeader(code, code, GetLangColumnWidth(code), isKeyColumn: false);
                }
                GUILayout.Label("操作", EditorStyles.boldLabel, GUILayout.Width(50));
            }
        }

        /// <summary>绘制可排序的列表头：点击切换该列排序，显示 ▲/▼ 指示。</summary>
        /// <param name="isKeyColumn">是否为 key 列（决定拖拽调整的是 key 列宽还是语言列宽）。</param>
        private void DrawSortableHeader(string displayName, string languageCode, float width, bool isKeyColumn)
        {
            bool isActive = languageCode == _sortColumn || (languageCode == null && _sortColumn == null);
            string arrow = isActive ? (_sortDescending ? " ▼" : " ▲") : "";
            var style = new GUIStyle(EditorStyles.boldLabel);
            if (isActive) style.normal.textColor = new Color(0.25f, 0.55f, 0.95f);

            Rect headerRect = GUILayoutUtility.GetRect(new GUIContent(displayName + arrow), style, GUILayout.Width(width));
            GUI.Label(headerRect, displayName + arrow, style);

            // 点击切换排序
            if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
            {
                if (isActive)
                {
                    // 同列：切换升降序
                    _sortDescending = !_sortDescending;
                }
                else
                {
                    // 新列：设为该列，默认升序
                    _sortColumn = languageCode;
                    _sortDescending = false;
                }
                _page = 0;
                Event.current.Use();
            }

            // 列宽拖拽：在表头右边缘画一个可拖拽分隔条
            DrawColumnResizer(headerRect, isKeyColumn, languageCode);
        }

        /// <summary>在表头右边缘绘制可拖拽的列宽调整条。</summary>
        /// <param name="isKeyColumn">是否调整 key 列宽（否则调整 languageCode 对应的语言列宽）。</param>
        /// <param name="languageCode">语言列的代码（isKeyColumn=false 时用）。</param>
        private void DrawColumnResizer(Rect headerRect, bool isKeyColumn, string languageCode)
        {
            float handleWidth = 6f;
            Rect resizeRect = new Rect(headerRect.xMax - handleWidth / 2f, headerRect.yMin, handleWidth, headerRect.height);
            EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);

            int ctrlId = GUIUtility.GetControlID(FocusType.Passive, resizeRect);
            Event e = Event.current;
            if (e.GetTypeForControl(ctrlId) == EventType.MouseDown && resizeRect.Contains(e.mousePosition))
            {
                GUIUtility.hotControl = ctrlId;
                e.Use();
            }
            else if (e.GetTypeForControl(ctrlId) == EventType.MouseDrag && GUIUtility.hotControl == ctrlId)
            {
                float delta = e.delta.x;
                if (isKeyColumn)
                {
                    _keyColumnWidth = Mathf.Max(MinColumnWidth, _keyColumnWidth + delta);
                }
                else
                {
                    // 按语言代码独立调整该列宽（拖一列只影响该列）
                    SetLangColumnWidth(languageCode, GetLangColumnWidth(languageCode) + delta);
                }
                e.Use();
            }
            else if (e.GetTypeForControl(ctrlId) == EventType.MouseUp && GUIUtility.hotControl == ctrlId)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        /// <summary>绘制一行（key + 各语言翻译 + 操作）。</summary>
        private void DrawRow(string key, List<LocaleData> locales, bool alternate)
        {
            // 选中行高亮
            bool isSelected = key == _selectedKey;
            Color oldBg = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(0.4f, 0.6f, 0.95f, 1f);
            else if (alternate) GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);

            using (var rowScope = new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                // key + 复制按钮
                using (new EditorGUILayout.HorizontalScope(GUILayout.Width(_keyColumnWidth)))
                {
                    EditorGUILayout.LabelField(key, GUILayout.MinWidth(_keyColumnWidth - 28));
                    if (GUILayout.Button("⧉", EditorStyles.miniButton, GUILayout.Width(24)))
                    {
                        EditorGUIUtility.systemCopyBuffer = key;
                        LocalizationLog.Info($"已复制 key: {key}");
                    }
                }

                foreach (var locale in locales)
                {
                    DrawLocaleCell(key, locale);
                }

                // 删除按钮
                if (GUILayout.Button("×", GUILayout.Width(40)))
                {
                    if (EditorUtility.DisplayDialog("删除 key", $"确认删除 key \"{key}\" 及其在所有语言中的翻译？", "删除", "取消"))
                    {
                        RemoveKey(locales, key);
                        GUI.changed = true;
                    }
                }

                // 在 HorizontalScope 内用其 rect 判定点击选中（避免 GetLastRect 在 scope 外语义模糊）
                // 子控件（按钮/输入框）会先消费各自的点击事件，只有点击空白区域才会到这里
                if (Event.current.type == EventType.MouseDown && rowScope.rect.Contains(Event.current.mousePosition))
                {
                    _selectedKey = key;
                    Event.current.Use();
                }
            }
            GUI.backgroundColor = oldBg;
        }

        /// <summary>绘制单个语言单元格：空值高亮，多行可编辑。</summary>
        private void DrawLocaleCell(string key, LocaleData locale)
        {
            string current = "";
            if (locale.Entries != null && locale.Entries.TryGetValue(key, out var v)) current = v ?? "";

            Color oldBg = GUI.backgroundColor;
            if (string.IsNullOrEmpty(current)) GUI.backgroundColor = new Color(1f, 0.95f, 0.6f);

            // 多行文本编辑：检测是否含换行符，含则用 TextArea，否则用 TextField（节省垂直空间）
            // 列宽按该语言独立取（拖拽时只影响对应列）
            float colWidth = GetLangColumnWidth(locale.Meta?.Code ?? "?");
            string newVal;
            if (current.Contains("\n") || current.Length > 60)
            {
                newVal = EditorGUILayout.TextArea(current, GUILayout.Width(colWidth), GUILayout.MinHeight(40));
            }
            else
            {
                newVal = EditorGUILayout.TextField(current, GUILayout.Width(colWidth));
            }
            GUI.backgroundColor = oldBg;

            if (newVal != current)
            {
                if (locale.Entries == null) locale.Entries = new Dictionary<string, string>();
                locale.Entries[key] = newVal;
                _window?.MarkDirty();
            }
        }

        /// <summary>底部：分页控件 + 计数。</summary>
        private void DrawFooter(List<string> visibleKeys, int totalPages)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (totalPages > 1)
                {
                    if (GUILayout.Button("上一页", GUILayout.Width(60))) _page = System.Math.Max(0, _page - 1);
                    GUILayout.Label($"第 {_page + 1} / {totalPages} 页", EditorStyles.miniLabel, GUILayout.Width(100));
                    if (GUILayout.Button("下一页", GUILayout.Width(60))) _page = System.Math.Min(totalPages - 1, _page + 1);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"共 {visibleKeys.Count} / {_keys.Count} 个 key", EditorStyles.miniLabel);
            }
        }

        // ---------- 过滤与排序 ----------

        /// <summary>
        /// 获取过滤+排序后的可见 key 列表（带缓存）。
        /// 仅当搜索/过滤开关/排序列/排序方向/底层数据数量变化时才重算，避免每帧全量遍历。
        /// </summary>
        private List<string> GetVisibleKeys(List<LocaleData> locales)
        {
            // 检测参数是否变化（任一变化则标记脏，需重算）
            bool changed = _cacheDirty
                || _cachedSearch != _search
                || _cachedOnlyMissing != _onlyMissing
                || _cachedSortColumn != _sortColumn
                || _cachedSortDescending != _sortDescending
                || _cachedKeyCount != _keys.Count;

            if (!changed) return _visibleKeysCache;

            // 重算
            _visibleKeysCache = GetFilteredKeys(locales);
            ApplySort(locales, _visibleKeysCache);

            // 更新快照
            _cachedSearch = _search;
            _cachedOnlyMissing = _onlyMissing;
            _cachedSortColumn = _sortColumn;
            _cachedSortDescending = _sortDescending;
            _cachedKeyCount = _keys.Count;
            _cacheDirty = false;
            _page = 0; // 过滤条件变化回到第一页
            return _visibleKeysCache;
        }

        /// <summary>按搜索 + 仅看未翻译过滤 key。</summary>
        private List<string> GetFilteredKeys(List<LocaleData> locales)
        {
            var result = new List<string>();
            string needle = string.IsNullOrEmpty(_search) ? null : _search.ToLowerInvariant();

            foreach (var key in _keys)
            {
                // 1) 仅看未翻译过滤
                if (_onlyMissing)
                {
                    bool allFilled = true;
                    foreach (var locale in locales)
                    {
                        bool has = locale?.Entries != null
                            && locale.Entries.TryGetValue(key, out var v)
                            && !string.IsNullOrEmpty(v);
                        if (!has) { allFilled = false; break; }
                    }
                    if (allFilled) continue; // 全部翻译完的不显示
                }

                // 2) 搜索关键字过滤（key 或任意翻译）
                if (needle != null)
                {
                    bool matchKey = key.ToLowerInvariant().Contains(needle);
                    bool matchValue = false;
                    if (!matchKey && locales != null)
                    {
                        foreach (var locale in locales)
                        {
                            if (locale?.Entries != null
                                && locale.Entries.TryGetValue(key, out var v)
                                && v != null
                                && v.ToLowerInvariant().Contains(needle))
                            {
                                matchValue = true;
                                break;
                            }
                        }
                    }
                    if (!matchKey && !matchValue) continue;
                }

                result.Add(key);
            }
            return result;
        }

        /// <summary>对 key 列表（或 visibleKeys）按当前排序规则排序。</summary>
        private void ApplySort(List<LocaleData> locales, List<string> target = null)
        {
            List<string> list = target ?? _keys;
            if (list.Count <= 1) return;

            if (string.IsNullOrEmpty(_sortColumn) || _sortColumn == null)
            {
                // 按 key 排序
                list.Sort((a, b) => _sortDescending
                    ? string.CompareOrdinal(b, a)
                    : string.CompareOrdinal(a, b));
            }
            else
            {
                // 按某语言翻译排序
                LocaleData locale = null;
                foreach (var l in locales)
                {
                    if (l?.Meta?.Code == _sortColumn) { locale = l; break; }
                }
                if (locale == null) return;

                list.Sort((a, b) =>
                {
                    locale.Entries.TryGetValue(a, out var va); va = va ?? "";
                    locale.Entries.TryGetValue(b, out var vb); vb = vb ?? "";
                    int cmp = string.CompareOrdinal(va, vb);
                    return _sortDescending ? -cmp : cmp;
                });
            }
        }

        /// <summary>确保选中 key 在可见列表中时滚动到对应页。</summary>
        private void EnsureSelectionVisible(List<string> visibleKeys)
        {
            if (string.IsNullOrEmpty(_selectedKey)) return;
            int idx = visibleKeys.IndexOf(_selectedKey);
            if (idx >= 0)
            {
                int targetPage = idx / PageSize;
                if (_page != targetPage) _page = targetPage;
            }
        }

        // ---------- key 增删 ----------

        /// <summary>新增 key（添加到所有语言，初始为空）。</summary>
        private void AddKey(List<LocaleData> locales, string key)
        {
            key = key?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                LocalizationLog.Warning("key 不能为空。");
                return;
            }
            if (_keys.Contains(key))
            {
                LocalizationLog.Warning($"key \"{key}\" 已存在。");
                return;
            }
            foreach (var locale in locales)
            {
                if (locale.Entries == null) locale.Entries = new Dictionary<string, string>();
                locale.Entries[key] = "";
            }
            _keys.Add(key);
            _cacheDirty = true; // 新增 key 后缓存失效，下次重算
            _window?.MarkDirty();
            LocalizationLog.Info($"已新增 key \"{key}\"（已在所有语言中创建空翻译，请填写）。");
        }

        /// <summary>删除 key（从所有语言移除）。</summary>
        private void RemoveKey(List<LocaleData> locales, string key)
        {
            foreach (var locale in locales)
            {
                if (locale.Entries != null) locale.Entries.Remove(key);
            }
            _keys.Remove(key);
            if (_selectedKey == key) _selectedKey = null;
            _window?.MarkDirty();
        }

        // ---------- 外部接口（供诊断跳转 + 快捷键） ----------

        /// <summary>定位到某 key：选中、滚动到对应页、可选设置搜索过滤（供 DiagnosticsTab 跳转）。</summary>
        public void FocusKey(string key, string searchFilter = null)
        {
            _selectedKey = key;
            if (searchFilter != null)
            {
                _search = searchFilter;
                _onlyMissing = false;
            }
            _page = 0; // EnsureSelectionVisible 会修正到正确页
        }

        /// <summary>聚焦搜索框（供 Ctrl+F 快捷键）。</summary>
        public void FocusSearch()
        {
            EditorGUI.FocusTextInControl("KeysTabSearch");
        }

        /// <summary>获取当前选中的 key（供 Delete 快捷键）。</summary>
        public string GetSelectedKey()
        {
            return _selectedKey;
        }

        /// <summary>删除当前选中的 key（供 Delete 快捷键）。</summary>
        public void DeleteSelectedKey(List<LocaleData> locales)
        {
            if (string.IsNullOrEmpty(_selectedKey)) return;
            RemoveKey(locales, _selectedKey);
            OnDataChanged(locales);
        }

        /// <summary>切换到本 Tab 的请求（由窗口协调）。供诊断跳转后自动切到词条 Tab。</summary>
        public void Activate() { }
    }
}
