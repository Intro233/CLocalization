using UnityEngine;

namespace CLocalization
{
    /// <summary>
    /// 音频本地化组件。通过 key 从当前语言的资源中加载 AudioClip，设置到 AudioSource。
    /// 典型用途：不同语言的配音（旁白、角色语音）。
    /// 资源路径约定：Resources/CLocalization/Assets/{语言代码}/{key}
    /// </summary>
    [AddComponentMenu("CLocalization/Localize Audio Source")]
    [RequireComponent(typeof(AudioSource))]
    public class LocalizeAudioSource : LocalizeBase
    {
        [Tooltip("目标 AudioSource 组件。留空则自动获取。")]
        [SerializeField] private AudioSource targetSource;

        [Tooltip("切换语言时是否自动播放新加载的音频（若 AudioSource 之前正在播放）")]
        [SerializeField] private bool autoPlayIfPlaying = true;

        /// <summary>记录切换前是否正在播放，用于切换后恢复播放。</summary>
        private bool _wasPlaying;

        protected override void OnEnable()
        {
            if (targetSource == null) targetSource = GetComponent<AudioSource>();
            base.OnEnable();
        }

        /// <summary>应用本地化：按 key 加载当前语言的 AudioClip 并设置。</summary>
        public override void ApplyLocalization()
        {
            if (targetSource == null) targetSource = GetComponent<AudioSource>();
            if (targetSource == null) return;
            if (string.IsNullOrEmpty(localizationKey)) return;

            // 切换前若正在播放，记录状态以便恢复
            _wasPlaying = targetSource.isPlaying;

            AudioClip clip = Localization.GetAsset<AudioClip>(localizationKey);
            if (clip != null)
            {
                targetSource.clip = clip;
                if (autoPlayIfPlaying && _wasPlaying && isActiveAndEnabled)
                {
                    targetSource.Play();
                }
            }
            else
            {
                LocalizationLog.Warning($"[{nameof(LocalizeAudioSource)}] 未找到 AudioClip 资源 key=\"{localizationKey}\" 语言={Localization.CurrentLanguageCode}。");
            }
        }

        /// <summary>运行时动态设置 key。</summary>
        public void SetKey(string newKey)
        {
            Key = newKey;
        }
    }
}
