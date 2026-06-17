using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CLocalization.Editor
{
    /// <summary>
    /// 本地化 key 引用扫描工具。扫描所有 Prefab / Scene 中 LocalizeBase 子类组件（含 LocalizeText/Sprite/AudioSource/Font）
    /// 引用的 key 字段（localizationKey / fontKey），建立「key → 引用位置列表」映射。
    /// 供「key 重命名」「未使用 key 检测」等工具复用。
    ///
    /// 注意：本工具只扫描序列化字段引用（Inspector 配置的 key），不扫描源码中的 Localization.Get("xxx") 字面量。
    /// 源码字面量引用需用文本搜索（grep .cs），准确率有限，单列一个方法。
    /// </summary>
    public static class LocalizationReferenceScanner
    {
        /// <summary>单个 key 引用位置。</summary>
        public struct KeyReference
        {
            /// <summary>引用所在的资源路径（Prefab/Scene 的 Assets 路径）。</summary>
            public string assetPath;
            /// <summary>组件类型名（如 LocalizeText）。</summary>
            public string componentType;
            /// <summary>字段名（localizationKey 或 fontKey）。</summary>
            public string fieldName;

            public override string ToString()
            {
                return $"{assetPath} [{componentType}.{fieldName}]";
            }
        }

        /// <summary>
        /// 扫描所有 Prefab 与 Scene，返回「key → 引用列表」。
        /// key 为空字符串的引用会被跳过。
        /// </summary>
        public static Dictionary<string, List<KeyReference>> ScanAllReferences()
        {
            var result = new Dictionary<string, List<KeyReference>>();

            // 收集所有 Prefab 和 Scene 资源
            string[] guids = AssetDatabase.FindAssets("t:Prefab t:Scene");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScanAsset(path, result);
            }
            return result;
        }

        /// <summary>扫描单个资源（Prefab/Scene）内所有 LocalizeBase 子类的 key 字段。</summary>
        private static void ScanAsset(string assetPath, Dictionary<string, List<KeyReference>> result)
        {
            // 加载资源，查找所有 LocalizeBase 子类组件
            // 注意：Scene 需用 GetAssetPath + OpenScene 方式，这里用较轻量的 LoadAllAssetsAtPath
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return;

            foreach (var obj in assets)
            {
                if (obj is LocalizeBase comp && comp != null)
                {
                    // localizationKey（基类字段，所有子类都有）
                    // 通过 SerializedObject 读取，因为字段是 protected，反射/序列化均可
                    using (var so = new SerializedObject(comp))
                    {
                        CollectKeyField(so, assetPath, comp.GetType().Name, "localizationKey", result);
                        // LocalizeFont 额外有 fontKey
                        CollectKeyField(so, assetPath, comp.GetType().Name, "fontKey", result);
                    }
                }
            }
        }

        /// <summary>读取一个序列化字段并登记到结果。</summary>
        private static void CollectKeyField(SerializedObject so, string assetPath, string typeName, string fieldName,
            Dictionary<string, List<KeyReference>> result)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            string key = prop.stringValue;
            if (string.IsNullOrEmpty(key)) return;

            if (!result.TryGetValue(key, out var list))
            {
                list = new List<KeyReference>();
                result[key] = list;
            }
            list.Add(new KeyReference { assetPath = assetPath, componentType = typeName, fieldName = fieldName });
        }

        /// <summary>
        /// 在所有 .cs 源码中搜索 Localization.Get("xxx") / GetAsset 等字面量引用的 key。
        /// 返回匹配到的 key 集合（仅作辅助参考，无法保证 100% 准确，因为可能有动态拼接的 key）。
        /// </summary>
        public static HashSet<string> ScanSourceCodeKeyLiterals()
        {
            var keys = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:Script");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs")) continue;
                string text = System.IO.File.ReadAllText(path);
                ExtractKeysFromSource(text, keys);
            }
            return keys;
        }

        /// <summary>从一段源码文本中提取 Localization.Get("...") 等字面量 key。</summary>
        private static void ExtractKeysFromSource(string text, HashSet<string> keys)
        {
            if (string.IsNullOrEmpty(text)) return;
            // 简易匹配：Localization.Get( "xxx" ) / Localization.Get("xxx"  / TryGet / GetAsset
            // 匹配形如 .Get( "任意非引号字符" )
            int idx = 0;
            while (idx < text.Length)
            {
                int getPos = text.IndexOf(".Get", idx, System.StringComparison.Ordinal);
                if (getPos < 0) break;
                // 从 getPos 往后找第一个引号
                int quote1 = text.IndexOf('"', getPos);
                if (quote1 < 0 || quote1 - getPos > 20) { idx = getPos + 4; continue; }
                int quote2 = text.IndexOf('"', quote1 + 1);
                if (quote2 < 0) break;
                string key = text.Substring(quote1 + 1, quote2 - quote1 - 1);
                // 过滤明显非 key 的（含空格过长的、为空的）
                if (!string.IsNullOrEmpty(key) && key.Length < 100)
                {
                    keys.Add(key);
                }
                idx = quote2 + 1;
            }
        }

        /// <summary>把引用映射格式化为可读字符串（用于日志/预览）。</summary>
        public static string FormatReferences(Dictionary<string, List<KeyReference>> refs)
        {
            var sb = new StringBuilder();
            foreach (var kv in refs)
            {
                sb.AppendLine($"[{kv.Key}] ({kv.Value.Count} 处引用)");
                foreach (var r in kv.Value)
                {
                    sb.AppendLine($"    - {r}");
                }
            }
            return sb.ToString();
        }
    }
}
