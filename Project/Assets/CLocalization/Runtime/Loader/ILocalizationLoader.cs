using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 本地化数据加载接口。抽象资源加载层，使运行时不绑定具体加载方式。
    /// 同时提供同步与异步两套加载方法：
    ///  - 同步方法适用于 Resources 等瞬时加载场景（默认实现 <see cref="ResourcesLocalizationLoader"/>）。
    ///  - 异步方法适用于 Addressables / 热更新 / 远端下载等需要等待的场景。
    /// 接入 Addressables 时实现本接口的异步方法即可，运行时核心通过 <see cref="Localization.SetLanguageAsync"/> 调用。
    /// </summary>
    public interface ILocalizationLoader
    {
        // ---------- 同步加载（Resources 等瞬时场景） ----------

        /// <summary>
        /// 【同步】加载指定语言的文本数据。
        /// </summary>
        /// <param name="languageCode">语言代码，如 zh-CN</param>
        /// <returns>反序列化后的语言数据；若文件不存在或解析失败返回 null。</returns>
        LocaleData LoadLocale(string languageCode);

        /// <summary>
        /// 【同步】加载指定语言、指定 key 对应的本地化资源（Sprite/AudioClip/Font 等）。
        /// </summary>
        T LoadAsset<T>(string key, string languageCode) where T : Object;

        /// <summary>
        /// 获取该 Loader 实际可用的所有语言代码。
        /// </summary>
        IReadOnlyList<string> GetAvailableLanguageCodes();

        // ---------- 异步加载（Addressables / 热更新场景） ----------

        /// <summary>
        /// 【异步】加载指定语言的文本数据。
        /// 异步实现可在后台线程/远端拉取，结果由调用方在主线程应用。
        /// Resources 实现可用 <c>UniTask.FromResult</c> 直接返回同步结果。
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        UniTask<LocaleData> LoadLocaleAsync(string languageCode);

        /// <summary>
        /// 【异步】加载指定语言、指定 key 的本地化资源。
        /// </summary>
        UniTask<T> LoadAssetAsync<T>(string key, string languageCode) where T : Object;

        // ---------- 路径版资源加载（资源映射表方案） ----------

        /// <summary>
        /// 【同步】按资源路径加载本地化资源。资源映射表存储路径而非强引用，由 Loader 决定如何加载。
        /// Resources 路径用 Resources.Load；FullPath 需自定义实现（AssetBundle/Addressables）。
        /// </summary>
        /// <param name="path">资源路径（Resources 相对路径 或 完整工程路径）</param>
        /// <param name="pathType">路径类型</param>
        T LoadAssetByPath<T>(string path, AssetPathType pathType) where T : Object;

        /// <summary>
        /// 【异步】按资源路径加载本地化资源。
        /// </summary>
        UniTask<T> LoadAssetByPathAsync<T>(string path, AssetPathType pathType) where T : Object;
    }
}
