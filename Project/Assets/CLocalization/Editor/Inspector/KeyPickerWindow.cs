using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// Key 选择器弹出窗口。提供搜索框 + 虚拟滚动列表，用于在 key 数量很多时快速选取。
    /// 用 <see cref="TreeView"/> 实现虚拟滚动，上万 key 也不卡。
    ///
    /// 用法：<c>KeyPickerWindow.Pick(allKeys, currentKey, OnSelected)</c>
    /// </summary>
    public class KeyPickerWindow : EditorWindow
    {
        /// <summary>所有可选 key。</summary>
        private string[] _allKeys;
        /// <summary>当前已选 key（高亮定位）。</summary>
        private string _currentKey;
        /// <summary>选择回调。</summary>
        private System.Action<string> _onSelected;
        /// <summary>TreeView 树模型。</summary>
        private KeyTreeView _treeView;
        /// <summary>TreeView 滚动区。</summary>
        private SearchField _searchField;

        /// <summary>窗口期望尺寸。</summary>
        private static readonly Vector2 WindowSize = new Vector2(360, 460);

        /// <summary>
        /// 弹出选择窗口。
        /// </summary>
        /// <param name="allKeys">所有可选 key（已排序）</param>
        /// <param name="currentKey">当前选中的 key（用于初始定位高亮）</param>
        /// <param name="onSelected">选择某 key 后的回调</param>
        public static void Pick(string[] allKeys, string currentKey, System.Action<string> onSelected)
        {
            if (allKeys == null || allKeys.Length == 0)
            {
                EditorUtility.DisplayDialog("无可用 Key", "当前没有任何语言 key，请先在编辑窗口添加词条。", "确定");
                return;
            }

            var window = GetWindow<KeyPickerWindow>(true, "选择 Key", true);
            window.minSize = new Vector2(280, 320);
            window.position = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition) - new Vector2(180, 230), WindowSize);
            window.Init(allKeys, currentKey, onSelected);
            window.Show();
        }

        /// <summary>初始化窗口数据。</summary>
        private void Init(string[] allKeys, string currentKey, System.Action<string> onSelected)
        {
            _allKeys = allKeys;
            _currentKey = currentKey;
            _onSelected = onSelected;

            _treeView = new KeyTreeView(allKeys, currentKey);
            _treeView.OnKeySelected += HandleSelected;

            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += () => _treeView.SetFocusAndEnsureSelectedItem();
        }

        private void OnGUI()
        {
            // 顶部搜索框
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("搜索 Key", EditorStyles.miniLabel, GUILayout.Width(50));
                _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString);
            }

            // 列表区
            Rect treeRect = GUILayoutUtility.GetRect(0, 99999, 0, 99999);
            if (_treeView != null)
            {
                _treeView.OnGUI(treeRect);
            }

            // 底部状态栏
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                int shown = _treeView != null ? _treeView.GetVisibleItemCount() : 0;
                GUILayout.Label($"显示 {shown} / {_allKeys?.Length ?? 0} 个 key", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("取消", EditorStyles.miniButton))
                {
                    Close();
                }
            }
        }

        /// <summary>选中某 key 的处理：回调并关闭窗口。</summary>
        private void HandleSelected(string key)
        {
            _onSelected?.Invoke(key);
            Close();
        }

        private void OnLostFocus()
        {
            // 失焦自动关闭（类似下拉行为）
            Close();
        }
    }

    /// <summary>
    /// Key 列表的 TreeView 实现。虚拟滚动，支持搜索过滤、键盘导航、双击选中。
    /// </summary>
    public class KeyTreeView : TreeView
    {
        /// <summary>所有 key 列表（过滤前）。</summary>
        private readonly List<string> _allKeys;
        /// <summary>过滤后的可见 key。</summary>
        private readonly List<string> _filtered = new List<string>();
        /// <summary>选中回调。</summary>
        public event System.Action<string> OnKeySelected;

        public KeyTreeView(string[] allKeys, string currentKey) : base(new TreeViewState())
        {
            _allKeys = new List<string>(allKeys);
            showAlternatingRowBackgrounds = true;
            // 定位到当前 key
            if (!string.IsNullOrEmpty(currentKey))
            {
                int idx = _allKeys.IndexOf(currentKey);
                if (idx >= 0) state.selectedIDs = new List<int> { idx + 1 };
            }
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            _filtered.Clear();

            string search = searchString;
            bool hasSearch = !string.IsNullOrEmpty(search);
            string needle = hasSearch ? search.ToLowerInvariant() : null;

            // 父子分组：按 key 的第一段（点号前）分组，便于浏览
            // 这里采用扁平列表 + 可选分组。为简单与快速，先做扁平列表（key 多时分组反而干扰搜索）
            int id = 1;
            foreach (var key in _allKeys)
            {
                bool visible = !hasSearch || key.ToLowerInvariant().Contains(needle);
                if (visible)
                {
                    _filtered.Add(key);
                    root.AddChild(new TreeViewItem { id = id, depth = 0, displayName = key });
                    id++;
                }
            }

            // 若无可见项，加一个占位
            if (_filtered.Count == 0)
            {
                root.AddChild(new TreeViewItem { id = -1, depth = 0, displayName = "(无匹配 key)" });
            }
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            // 当前行对应的 key（按过滤后的索引）
            int visibleIndex = args.item.id - 1;
            string key = (visibleIndex >= 0 && visibleIndex < _filtered.Count) ? _filtered[visibleIndex] : null;
            if (key == null)
            {
                base.RowGUI(args);
                return;
            }

            // 自定义绘制：key 名 + 右侧灰色提示
            var rect = args.rowRect;
            rect.xMin += GetContentIndent(args.item);
            EditorGUI.LabelField(rect, key, key == searchString ? EditorStyles.boldLabel : DefaultStyles.label);
        }

        protected override void DoubleClickedItem(int id)
        {
            // 双击选中
            int visibleIndex = id - 1;
            if (visibleIndex >= 0 && visibleIndex < _filtered.Count)
            {
                OnKeySelected?.Invoke(_filtered[visibleIndex]);
            }
        }

        protected override void KeyEvent()
        {
            // 回车选中当前高亮项
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (state.selectedIDs.Count > 0)
                {
                    int id = state.selectedIDs[0];
                    int visibleIndex = id - 1;
                    if (visibleIndex >= 0 && visibleIndex < _filtered.Count)
                    {
                        OnKeySelected?.Invoke(_filtered[visibleIndex]);
                        Event.current.Use();
                    }
                }
            }
            base.KeyEvent();
        }

        /// <summary>获取当前过滤后可见的 key 数量。</summary>
        public int GetVisibleItemCount() => _filtered.Count;

        /// <summary>设置焦点并确保有选中项（搜索框上下键导航时调用）。</summary>
        public void SetFocusAndEnsureSelectedItem()
        {
            SetFocus();
            if (state.selectedIDs.Count == 0 && _filtered.Count > 0)
            {
                state.selectedIDs = new List<int> { 1 };
            }
        }
    }
}
