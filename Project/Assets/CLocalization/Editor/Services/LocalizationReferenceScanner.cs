using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        /// <summary>缓存的组件引用扫描结果（避免每次诊断都全量扫描 Scene/Prefab，场景扫描开销大）。</summary>
        private static Dictionary<string, List<KeyReference>> _cachedCompRefs;
        /// <summary>缓存的源码字面量引用（避免每次重读所有 .cs）。</summary>
        private static HashSet<string> _cachedSrcRefs;

        /// <summary>清除引用扫描缓存（场景/Prefab 保存、资源导入、或用户强制重扫时调用）。</summary>
        public static void InvalidateCache()
        {
            _cachedCompRefs = null;
            _cachedSrcRefs = null;
        }

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
        /// 结果缓存：首次扫描后缓存，重复调用直接返回（用 InvalidateCache 失效）。
        /// key 为空字符串的引用会被跳过。
        /// </summary>
        public static Dictionary<string, List<KeyReference>> ScanAllReferences()
        {
            // 缓存命中：避免每次诊断都全量扫描（场景扫描开销大）
            if (_cachedCompRefs != null) return _cachedCompRefs;

            var result = new Dictionary<string, List<KeyReference>>();

            // 收集所有 Prefab 和 Scene 资源，分开处理（Scene 必须用 OpenScene，否则 LoadAllAssetsAtPath 会触发警告）
            string[] guids = AssetDatabase.FindAssets("t:Prefab t:Scene");
            int total = guids.Length;
            bool cancelled = false;
            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                // 进度条（模态，扫描大量资源时告知用户进度）
                if (total > 5 && i % 3 == 0)
                {
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "扫描本地化引用",
                        $"扫描中 ({i + 1}/{total}): {System.IO.Path.GetFileName(path)}",
                        (float)i / total);
                    if (cancel)
                    {
                        LocalizationLog.Info("用户取消了引用扫描。");
                        cancelled = true;
                        break;
                    }
                }
                ScanAsset(path, result);
            }
            EditorUtility.ClearProgressBar();
            // 仅在完整扫描完成时缓存（取消时不缓存，下次重新扫描）
            if (!cancelled) _cachedCompRefs = result;
            return result;
        }

        /// <summary>扫描单个资源（Prefab/Scene）内所有 LocalizeBase 子类的 key 字段。</summary>
        private static void ScanAsset(string assetPath, Dictionary<string, List<KeyReference>> result)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            bool isScene = assetPath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase);
            if (isScene)
            {
                ScanScene(assetPath, result);
            }
            else
            {
                ScanPrefab(assetPath, result);
            }
        }

        /// <summary>
        /// 扫描 Prefab：用 LoadAllAssetsAtPath 加载所有对象，查找 LocalizeBase 组件。
        /// （Prefab 不是场景，LoadAllAssetsAtPath 不会触发警告。）
        /// </summary>
        private static void ScanPrefab(string assetPath, Dictionary<string, List<KeyReference>> result)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return;

            foreach (var obj in assets)
            {
                if (obj is LocalizeBase comp && comp != null)
                {
                    CollectFromComponent(comp, assetPath, result);
                }
            }
        }

        /// <summary>
        /// 扫描 Scene：用 EditorSceneManager 以只读方式打开场景，遍历所有 LocalizeBase 组件，扫完不保存关闭。
        /// （Scene 不能用 LoadAllAssetsAtPath——会触发 "Do not use ReadObjectThreaded on scene objects" 警告。）
        /// </summary>
        private static void ScanScene(string assetPath, Dictionary<string, List<KeyReference>> result)
        {
            Scene scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
            if (!scene.isLoaded) return;

            try
            {
                // 遍历场景所有根对象的全部子对象，收集 LocalizeBase 组件
                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    // includeInactive: true 包含未激活的组件
                    var comps = root.GetComponentsInChildren<LocalizeBase>(true);
                    if (comps == null) continue;
                    foreach (var comp in comps)
                    {
                        if (comp != null) CollectFromComponent(comp, assetPath, result);
                    }
                }
            }
            finally
            {
                // 扫描完关闭该场景（不保存，丢弃打开）
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>从单个组件读取 localizationKey / fontKey 并登记到结果。</summary>
        private static void CollectFromComponent(LocalizeBase comp, string assetPath, Dictionary<string, List<KeyReference>> result)
        {
            using (var so = new SerializedObject(comp))
            {
                CollectKeyField(so, assetPath, comp.GetType().Name, "localizationKey", result);
                // LocalizeFont 额外有 fontKey
                CollectKeyField(so, assetPath, comp.GetType().Name, "fontKey", result);
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
        /// 结果缓存：首次扫描后缓存，重复调用直接返回（用 InvalidateCache 失效）。
        /// </summary>
        public static HashSet<string> ScanSourceCodeKeyLiterals()
        {
            if (_cachedSrcRefs != null) return _cachedSrcRefs;

            var keys = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:Script");
            int total = guids.Length;
            bool cancelled = false;
            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (total > 20 && i % 10 == 0)
                {
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "扫描源码引用",
                        $"扫描脚本 ({i + 1}/{total})",
                        (float)i / total);
                    if (cancel) { cancelled = true; break; }
                }
                if (!path.EndsWith(".cs")) continue;
                string text = System.IO.File.ReadAllText(path);
                ExtractKeysFromSource(text, keys);
            }
            EditorUtility.ClearProgressBar();
            if (!cancelled) _cachedSrcRefs = keys;
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
