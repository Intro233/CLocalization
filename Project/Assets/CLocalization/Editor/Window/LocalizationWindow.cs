using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 多语言编辑主窗口。菜单：Tools > CLocalization > Localization Window。
    /// 提供词条编辑表、语言管理、导入导出、诊断四个 Tab。
    /// 数据从磁盘 JSON 加载，编辑后显式保存才写回（避免误改）。
    /// </summary>
    public class LocalizationWindow : EditorWindow
    {
        /// <summary>当前编辑的所有语言数据（内存副本，保存才写盘）。</summary>
        private List<LocaleData> _locales = new List<LocaleData>();

        /// <summary>待删除的语言代码集合（延迟删盘，SaveAll 时才真正删磁盘文件）。</summary>
        private readonly HashSet<string> _pendingDeleteCodes = new HashSet<string>();

        /// <summary>是否有未保存的修改（用于工具栏提示）。</summary>
        private bool _dirty;

        /// <summary>当前选中的 Tab。</summary>
        private int _currentTab;

        /// <summary>各 Tab 的绘制器。</summary>
        private KeysTab _keysTab;
        private LanguagesTab _languagesTab;
        private ImportExportTab _importExportTab;
        private DiagnosticsTab _diagnosticsTab;

        /// <summary>Tab 名称。</summary>
        private static readonly string[] TabNames = { "词条", "语言", "导入/导出", "诊断" };

        [MenuItem("Tools/CLocalization/Localization Window", priority = 1)]
        public static void Open()
        {
            var window = GetWindow<LocalizationWindow>("CLocalization");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            InitTabs();
            Reload();
        }

        /// <summary>初始化各 Tab 绘制器。</summary>
        private void InitTabs()
        {
            _keysTab = new KeysTab();
            _languagesTab = new LanguagesTab();
            _importExportTab = new ImportExportTab();
            _diagnosticsTab = new DiagnosticsTab();
        }

        /// <summary>从磁盘重新加载所有 locale 数据。</summary>
        public void Reload()
        {
            _locales = LocalizationEditorData.LoadAllLocales();
            _pendingDeleteCodes.Clear();
            _dirty = false;
            _keysTab?.OnDataChanged(_locales);
            _diagnosticsTab?.OnDataChanged(_locales);
            Repaint();
        }

        /// <summary>保存所有 locale 到磁盘，并执行延迟的删除操作。</summary>
        public void SaveAll()
        {
            // 先执行延迟删除：真正删除磁盘 JSON 文件
            foreach (var code in _pendingDeleteCodes)
            {
                LocalizationEditorData.DeleteLocale(code);
            }
            _pendingDeleteCodes.Clear();

            // 保存内存中的语言数据
            LocalizationEditorData.SaveAllLocales(_locales);
            AssetDatabase.Refresh();
            _dirty = false;
            LocalizationLog.Info($"已保存 {_locales.Count} 种语言数据。");
            Repaint();
        }

        /// <summary>标记有未保存的修改（供各 Tab 在编辑后调用）。</summary>
        public void MarkDirty()
        {
            _dirty = true;
            Repaint();
        }

        /// <summary>获取当前内存中的 locale 列表（供外部模块基于编辑中的数据同步 Settings 等）。</summary>
        public List<LocaleData> GetCurrentLocales()
        {
            return _locales;
        }

        /// <summary>仅刷新各 Tab 的 key 缓存，不做全量 Reload（不清空待删集合，不重新读盘）。</summary>
        public void RefreshCaches()
        {
            _keysTab?.OnDataChanged(_locales);
            _diagnosticsTab?.OnDataChanged(_locales);
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);

            // 顶部状态条
            DrawStatusBar();
            EditorGUILayout.Space(4);

            _currentTab = GUILayout.Toolbar(_currentTab, TabNames, GUILayout.Height(24));
            EditorGUILayout.Space(4);

            // 按 Tab 分发绘制
            switch (_currentTab)
            {
                case 0:
                    _keysTab.Draw(this, _locales);
                    break;
                case 1:
                    _languagesTab.Draw(this, _locales);
                    break;
                case 2:
                    _importExportTab.Draw(this, _locales);
                    break;
                case 3:
                    _diagnosticsTab.Draw(this, _locales);
                    break;
            }
        }

        /// <summary>顶部工具栏：重载、保存按钮。</summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("CLocalization 编辑器", EditorStyles.boldLabel, GUILayout.Width(160));
                // 未保存提示标记
                if (_dirty)
                {
                    var old = GUI.color;
                    GUI.color = new Color(1f, 0.7f, 0.2f);
                    GUILayout.Label("* 未保存", EditorStyles.miniLabel, GUILayout.Width(60));
                    GUI.color = old;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("重新加载", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    if (_dirty && !EditorUtility.DisplayDialog("重新加载", "有未保存的修改，重新加载将丢弃这些修改。继续？", "丢弃并重载", "取消"))
                    {
                        return;
                    }
                    Reload();
                }
                if (GUILayout.Button("保存全部", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    SaveAll();
                }
                GUILayout.Space(4);
            }
        }

        /// <summary>状态条：显示当前语言数与词条数概览。</summary>
        private void DrawStatusBar()
        {
            int totalKeys = LocalizationEditorData.CollectAllKeys(_locales).Count;
            string info = $"语言: {_locales.Count}    词条(key 并集): {totalKeys}";
            EditorGUILayout.HelpBox(info, MessageType.None);
        }

        /// <summary>新增一种语言（供 LanguagesTab 调用）。</summary>
        public void AddLanguage(string code, string displayName)
        {
            // 防止重复
            foreach (var l in _locales)
            {
                if (l.Meta.Code == code)
                {
                    LocalizationLog.Warning($"语言 {code} 已存在。");
                    return;
                }
            }
            var locale = LocalizationEditorData.CreateLocale(code, displayName);
            _locales.Add(locale);
            _keysTab.OnDataChanged(_locales);
            _diagnosticsTab.OnDataChanged(_locales);
            MarkDirty();
        }

        /// <summary>删除一种语言（供 LanguagesTab 调用）。仅移除内存副本并记录待删 code，SaveAll 时才真正删盘。</summary>
        public void RemoveLanguage(string code)
        {
            for (int i = 0; i < _locales.Count; i++)
            {
                if (_locales[i].Meta.Code == code)
                {
                    _locales.RemoveAt(i);
                    _pendingDeleteCodes.Add(code); // 延迟删盘：SaveAll 时才真正删 JSON 文件
                    _keysTab.OnDataChanged(_locales);
                    _diagnosticsTab.OnDataChanged(_locales);
                    MarkDirty();
                    return;
                }
            }
        }
    }
}
