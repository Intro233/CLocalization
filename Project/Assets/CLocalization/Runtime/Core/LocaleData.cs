using System.Collections.Generic;
using Newtonsoft.Json;

namespace CLocalization
{
    /// <summary>
    /// 单语言的文本数据。对应磁盘上一个 JSON 文件（如 zh-CN.json）。
    /// JSON 结构示例：
    /// {
    ///   "meta": { "code": "zh-CN", "displayName": "中文", "version": "1.0" },
    ///   "entries": { "ui.start": "开始", "ui.hello": "你好,{0}" }
    /// }
    /// entries 使用 Dictionary，借助 Newtonsoft.Json 可直接序列化（JsonUtility 不支持 Dictionary）。
    /// </summary>
    [System.Serializable]
    public class LocaleData
    {
        /// <summary>该语言文件的元信息（语言代码、版本等），便于校验。</summary>
        [JsonProperty("meta")]
        public LocaleMeta Meta = new LocaleMeta();

        /// <summary>该语言的所有词条：key → 翻译文本。运行时查询的核心容器。</summary>
        [JsonProperty("entries")]
        public Dictionary<string, string> Entries = new Dictionary<string, string>();

        /// <summary>从 JSON 字符串反序列化为 LocaleData。</summary>
        public static LocaleData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<LocaleData>(json);
        }

        /// <summary>序列化为 JSON 字符串（带缩进，便于人工阅读与版本管理）。</summary>
        public string ToJson(bool indented = true)
        {
            var formatting = indented ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(this, formatting);
        }

        /// <summary>查询某 key 是否存在。</summary>
        public bool HasEntry(string key)
        {
            return Entries != null && Entries.ContainsKey(key);
        }

        /// <summary>尝试获取某 key 的文本，失败返回 false。</summary>
        public bool TryGetEntry(string key, out string value)
        {
            if (Entries != null && Entries.TryGetValue(key, out var v))
            {
                value = v;
                return true;
            }
            value = null;
            return false;
        }
    }

    /// <summary>语言文件元信息。</summary>
    [System.Serializable]
    public class LocaleMeta
    {
        /// <summary>语言代码。</summary>
        [JsonProperty("code")]
        public string Code;

        /// <summary>显示名称（可选，仅做标识）。</summary>
        [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName;

        /// <summary>版本号（可选，便于热更新校验）。</summary>
        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version;
    }
}
