using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 本地化系统自动初始化器（可选组件）。
    /// 将其挂在场景中任意 GameObject 上，并在 Inspector 指定 LocalizationSettings，
    /// 即可在 Awake 阶段自动完成初始化，无需手动调用 Localization.Initialize。
    ///
    /// 如果项目已有自己的初始化流程，可不用本组件，直接调用：
    ///     Localization.Initialize(settings);
    /// </summary>
    [HelpURL("https://github.com/")]
    [DisallowMultipleComponent]
    public class LocalizationInitializer : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("本地化配置资源。如留空，将尝试从 Resources/CLocalization/LocalizationSettings 加载默认配置。")]
        [SerializeField] private LocalizationSettings settings;

        /// <summary>是否在 Awake 即初始化（早于多数游戏逻辑，推荐）。</summary>
        [Tooltip("是否在 Awake 即初始化（推荐）")]
        [SerializeField] private bool initializeOnAwake = true;

        /// <summary>Settings 访问器，便于外部读取。</summary>
        public LocalizationSettings Settings => settings;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 手动触发初始化（当 initializeOnAwake=false 或外部接管时调用）。
        /// </summary>
        public void Initialize()
        {
            LocalizationSettings s = settings;
            if (s == null)
            {
                // 兜底：从默认 Resources 路径加载
                s = Resources.Load<LocalizationSettings>(LocalizationPaths.Root + "/LocalizationSettings");
            }

            if (s == null)
            {
                LocalizationLog.Error("LocalizationInitializer 找不到 LocalizationSettings，请指定或放入 Resources/CLocalization/。");
                return;
            }

            if (!Localization.IsInitialized)
            {
                Localization.Initialize(s);
            }
        }
    }
}
