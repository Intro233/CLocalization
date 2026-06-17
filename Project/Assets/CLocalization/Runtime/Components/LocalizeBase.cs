using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 本地化 UI 组件的抽象基类。
    /// 统一处理「订阅语言切换事件 → 自动刷新」的生命周期逻辑，
    /// 子类只需实现 <see cref="ApplyLocalization"/> 描述具体刷新行为。
    /// </summary>
    public abstract class LocalizeBase : MonoBehaviour
    {
        /// <summary>
        /// 是否在 OnEnable 时即刷新一次。
        /// 多数情况应为 true，保证组件激活时立即显示当前语言内容。
        /// </summary>
        [Tooltip("组件启用时是否立即刷新一次（通常开启）")]
        [SerializeField] protected bool refreshOnEnable = true;

        /// <summary>
        /// 本地化 key。子类负责按此 key 取数据并应用到目标 UI 元素。
        /// 基类不直接使用，仅为统一字段命名约定；子类可隐藏或覆盖。
        /// </summary>
        [Tooltip("本地化词条 key，例如 ui.title")]
        [SerializeField] protected string localizationKey;

        /// <summary>对外暴露的 key（读写）。</summary>
        public string Key
        {
            get => localizationKey;
            set
            {
                localizationKey = value;
                if (isActiveAndEnabled) ApplyLocalization();
            }
        }

        /// <summary>是否已绑定到事件。</summary>
        private bool _isBound;

        protected virtual void OnEnable()
        {
            Bind();
            if (refreshOnEnable)
            {
                ApplyLocalization();
            }
        }

        protected virtual void OnDisable()
        {
            Unbind();
        }

        /// <summary>订阅语言切换事件。若未初始化则延后到首次刷新时再尝试绑定。</summary>
        private void Bind()
        {
            if (_isBound) return;
            Localization.OnLanguageChanged += HandleLanguageChanged;
            _isBound = true;
        }

        /// <summary>取消订阅语言切换事件。</summary>
        private void Unbind()
        {
            if (!_isBound) return;
            Localization.OnLanguageChanged -= HandleLanguageChanged;
            _isBound = false;
        }

        /// <summary>语言切换事件回调：触发一次刷新。</summary>
        private void HandleLanguageChanged(LanguageInfo language)
        {
            ApplyLocalization();
        }

        /// <summary>
        /// 子类实现：根据当前语言与 <see cref="localizationKey"/> 刷新目标 UI 元素。
        /// 该方法会在组件启用、语言切换、key 变更时被调用。
        /// 注意：未初始化时应做容错（Localization.Get 在未初始化时会回退返回 key）。
        /// </summary>
        public abstract void ApplyLocalization();

        /// <summary>供编辑器/外部强制刷新。</summary>
        public void Refresh()
        {
            ApplyLocalization();
        }
    }
}
