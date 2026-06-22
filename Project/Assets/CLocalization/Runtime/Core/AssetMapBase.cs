using System.Collections.Generic;
using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 单个 key 在某个语言的资源映射条目。
    /// 存储资源路径（assetPath + pathType），运行时按路径加载，避免强引用导致资源无法打 AB 分包。
    /// asset 字段保留用于编辑器预览（运行时不读），向后兼容旧数据迁移。
    /// </summary>
    [System.Serializable]
    public class AssetMapping
    {
        /// <summary>资源 key（与组件的 localizationKey 对应）。</summary>
        [Tooltip("资源 key，与 Localize 组件的 key 对应")]
        public string key;

        /// <summary>语言代码（如 zh-CN、en-US）。</summary>
        [Tooltip("语言代码")]
        public string languageCode;

        /// <summary>
        /// 资源路径。运行时加载从此字段读取（配合 pathType 决定加载方式）。
        /// Resources 路径（无扩展名）或完整工程路径（含 Assets/ 和扩展名）。
        /// </summary>
        [Tooltip("资源路径（Resources 相对路径或完整工程路径）")]
        public string assetPath;

        /// <summary>路径类型，决定运行时如何加载。</summary>
        [Tooltip("路径类型：Resources（运行时 Resources.Load）或 FullPath（需自定义 Loader/AB）")]
        public AssetPathType pathType;

        /// <summary>
        /// 旧版强引用字段（已废弃，保留用于编辑器预览和向后兼容迁移）。
        /// 运行时不读此字段，仅编辑器用于 ObjectField 显示。
        /// </summary>
        [Tooltip("（编辑器预览用，运行时读 assetPath）")]
        [SerializeField] private Object asset;

        /// <summary>编辑器预览引用（运行时不可用）。</summary>
        public Object Asset => asset;

        /// <summary>设置编辑器预览引用（仅编辑器调用）。</summary>
        public void SetPreviewAsset(Object obj) => asset = obj;

        /// <summary>该条目是否已配置资源（路径非空）。</summary>
        public bool IsConfigured => !string.IsNullOrEmpty(assetPath);
    }

    /// <summary>
    /// 资源映射表基类。存储 key × 语言 → 资源路径的映射，供 <see cref="Localization.GetAsset{T}"/> 查询路径后加载。
    /// 每种资源类型（Sprite/AudioClip/Font）各一个子类。查询为线性查找（表内条目通常几十到几百，可接受）。
    /// </summary>
    public abstract class AssetMapBase : ScriptableObject
    {
        /// <summary>所有映射条目。</summary>
        [SerializeField]
        protected List<AssetMapping> entries = new List<AssetMapping>();

        /// <summary>所有映射条目（只读视图，供编辑器遍历）。</summary>
        public IReadOnlyList<AssetMapping> Entries => entries;

        /// <summary>
        /// 按 key + 语言代码查找资源路径。找不到返回 false。
        /// </summary>
        /// <param name="key">资源 key</param>
        /// <param name="languageCode">语言代码</param>
        /// <param name="path">输出：资源路径</param>
        /// <param name="pathType">输出：路径类型</param>
        /// <returns>是否找到有效路径</returns>
        public bool LookupPath(string key, string languageCode, out string path, out AssetPathType pathType)
        {
            path = null;
            pathType = AssetPathType.Resources;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(languageCode)) return false;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.key == key && e.languageCode == languageCode && e.IsConfigured)
                {
                    path = e.assetPath;
                    pathType = e.pathType;
                    return true;
                }
            }
            return false;
        }

        /// <summary>统计某 key 已配置（路径非空）的语言数（供 Inspector 状态提示）。</summary>
        public int CountConfiguredLanguages(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key && entries[i].IsConfigured) count++;
            }
            return count;
        }

        /// <summary>收集表中出现过的所有 key（去重，供编辑器列出 key）。</summary>
        public List<string> CollectKeys()
        {
            var keys = new List<string>();
            var seen = new HashSet<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key != null && seen.Add(entries[i].key))
                {
                    keys.Add(entries[i].key);
                }
            }
            keys.Sort();
            return keys;
        }

        // ---------- 编辑器用的增删方法（运行时也可用，但通常编辑期调用） ----------

        /// <summary>为某 key 在所有指定语言创建空映射条目（已存在的语言跳过）。</summary>
        public void AddKey(string key, IReadOnlyList<string> languageCodes)
        {
            if (string.IsNullOrEmpty(key)) return;
            var existing = new HashSet<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key) existing.Add(entries[i].languageCode);
            }
            foreach (var code in languageCodes)
            {
                if (!existing.Contains(code))
                {
                    entries.Add(new AssetMapping { key = key, languageCode = code });
                }
            }
        }

        /// <summary>删除某 key 的所有映射条目（所有语言）。</summary>
        public void RemoveKey(string key)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].key == key) entries.RemoveAt(i);
            }
        }

        /// <summary>确保某 key 在指定语言有条目（没有则创建空条目），返回该条目的索引。</summary>
        public int EnsureEntry(string key, string languageCode)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key && entries[i].languageCode == languageCode) return i;
            }
            entries.Add(new AssetMapping { key = key, languageCode = languageCode });
            return entries.Count - 1;
        }

        /// <summary>设置某条目的资源路径（编辑器调用）。</summary>
        public void SetAssetPath(int entryIndex, string path, AssetPathType pathType)
        {
            if (entryIndex < 0 || entryIndex >= entries.Count) return;
            entries[entryIndex].assetPath = path;
            entries[entryIndex].pathType = pathType;
        }
    }
}
