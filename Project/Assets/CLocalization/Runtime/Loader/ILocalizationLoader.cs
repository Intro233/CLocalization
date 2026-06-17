using System.Collections.Generic;
using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 本地化数据加载接口。抽象资源加载层，使运行时不绑定具体加载方式。
    /// 默认提供 <see cref="ResourcesLocalizationLoader"/>（Resources 直接加载）。
    /// 若项目未来引入 Addressables 或热更新，只需实现本接口并注入到 <see cref="Localization.Initialize"/>，
    /// 运行时核心代码无需修改。
    /// </summary>
    public interface ILocalizationLoader
    {
        /// <summary>
        /// 加载指定语言的文本数据。
        /// </summary>
        /// <param name="languageCode">语言代码，如 zh-CN</param>
        /// <returns>反序列化后的语言数据；若文件不存在或解析失败返回 null。</returns>
        LocaleData LoadLocale(string languageCode);

        /// <summary>
        /// 加载指定语言、指定 key 对应的本地化资源（Sprite/AudioClip/Font 等）。
        /// </summary>
        /// <typeparam name="T">资源类型（需继承 UnityEngine.Object）</typeparam>
        /// <param name="key">资源 key</param>
        /// <param name="languageCode">语言代码</param>
        /// <returns>资源对象；找不到返回 null。</returns>
        T LoadAsset<T>(string key, string languageCode) where T : Object;

        /// <summary>
        /// 获取该 Loader 实际可用的所有语言代码（例如扫描 Resources 目录或远端清单）。
        /// </summary>
        IReadOnlyList<string> GetAvailableLanguageCodes();
    }
}
