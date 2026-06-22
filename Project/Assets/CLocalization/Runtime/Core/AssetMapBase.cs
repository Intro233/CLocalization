using System.Collections.Generic;
using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 单个 key 在某个语言的资源映射条目。
    /// asset 字段存储实际资源（Sprite/AudioClip/Font/TMP_FontAsset），由具体映射表约束类型。
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

        /// <summary>该语言对应的资源（具体类型由映射表约束）。</summary>
        [Tooltip("该语言对应的资源")]
        public Object asset;
    }

    /// <summary>
    /// 资源映射表基类。存储 key × 语言 → 资源的映射，供 <see cref="Localization.GetAsset{T}"/> 查询。
    /// 每种资源类型（Sprite/AudioClip/Font）各一个子类，约束 asset 类型。
    /// 查询为线性查找（表内条目通常几十到几百，可接受）。
    /// </summary>
    public abstract class AssetMapBase : ScriptableObject
    {
        /// <summary>所有映射条目。</summary>
        [SerializeField]
        protected List<AssetMapping> entries = new List<AssetMapping>();

        /// <summary>所有映射条目（只读视图，供编辑器遍历）。</summary>
        public IReadOnlyList<AssetMapping> Entries => entries;

        /// <summary>
        /// 按 key + 语言代码查找资源。找不到返回 null。
        /// </summary>
        public Object Lookup(string key, string languageCode)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(languageCode)) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.key == key && e.languageCode == languageCode)
                {
                    return e.asset;
                }
            }
            return null;
        }

        /// <summary>统计某 key 已配置（非空）的语言数（供 Inspector 状态提示）。</summary>
        public int CountConfiguredLanguages(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key && entries[i].asset != null) count++;
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
            // 先收集该 key 已有的语言，避免重复
            var existing = new HashSet<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key) existing.Add(entries[i].languageCode);
            }
            foreach (var code in languageCodes)
            {
                if (!existing.Contains(code))
                {
                    entries.Add(new AssetMapping { key = key, languageCode = code, asset = null });
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
            entries.Add(new AssetMapping { key = key, languageCode = languageCode, asset = null });
            return entries.Count - 1;
        }
    }
}
