using UnityEngine;

namespace BellRinger.Audio
{
    [DisallowMultipleComponent]
    public sealed class BellRingerAudioPresetApplier : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private BellRingerAudioSourcePreset preset;
        [SerializeField] private bool applyOnAwake = true;

        public BellRingerAudioSourcePreset Preset => preset;

        public void Apply()
        {
            ResolveAudioSource();
            preset?.ApplyTo(audioSource);
        }

        public void SetPreset(BellRingerAudioSourcePreset nextPreset)
        {
            preset = nextPreset;
            Apply();
        }

        private void Awake()
        {
            if (applyOnAwake)
            {
                Apply();
            }
        }

        private void OnValidate()
        {
            ResolveAudioSource();
            if (!Application.isPlaying)
            {
                preset?.ApplyTo(audioSource);
            }
        }

        private void ResolveAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }
    }
}
