using UnityEngine;
using UnityEngine.Audio;

namespace BellRinger.Audio
{
    [CreateAssetMenu(menuName = "Bell Ringer/Audio Source Preset", fileName = "BellRingerAudioSourcePreset")]
    public sealed class BellRingerAudioSourcePreset : ScriptableObject
    {
        [SerializeField] private string displayName = "Audio Preset";
        [SerializeField] private BellRingerAudioBus bus = BellRingerAudioBus.Bell;
        [SerializeField] private AudioMixerGroup outputMixerGroup;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.85f;
        [SerializeField] [Range(-3f, 3f)] private float pitch = 1f;
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField] private bool spatialize;
        [SerializeField] private bool spatializePostEffects;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
        [SerializeField] private float minDistance = 0.75f;
        [SerializeField] private float maxDistance = 8f;
        [SerializeField] [Range(0f, 5f)] private float dopplerLevel;
        [SerializeField] [Range(0f, 360f)] private float spread;
        [SerializeField] private bool enableLowPass;
        [SerializeField] [Range(10f, 22000f)] private float lowPassCutoffHz = 22000f;
        [SerializeField] [Range(1f, 10f)] private float lowPassResonanceQ = 1f;
        [SerializeField] private bool enableReverbFilter;
        [SerializeField] private AudioReverbPreset reverbPreset = AudioReverbPreset.Generic;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public BellRingerAudioBus Bus => bus;
        public float Volume => volume;

        public void ApplyTo(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.outputAudioMixerGroup = outputMixerGroup;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, -3f, 3f);
            source.spatialBlend = Mathf.Clamp01(spatialBlend);
            source.spatialize = spatialize;
            source.spatializePostEffects = spatializePostEffects;
            source.rolloffMode = rolloffMode;
            source.minDistance = Mathf.Max(0.01f, minDistance);
            source.maxDistance = Mathf.Max(maxDistance, source.minDistance + 0.01f);
            source.dopplerLevel = Mathf.Max(0f, dopplerLevel);
            source.spread = Mathf.Clamp(spread, 0f, 360f);

            ApplyLowPass(source.gameObject);
            ApplyReverb(source.gameObject);
        }

        private void ApplyLowPass(GameObject target)
        {
            AudioLowPassFilter filter = target.GetComponent<AudioLowPassFilter>();
            if (!enableLowPass)
            {
                if (filter != null)
                {
                    filter.enabled = false;
                }

                return;
            }

            if (filter == null)
            {
                filter = target.AddComponent<AudioLowPassFilter>();
            }

            filter.enabled = true;
            filter.cutoffFrequency = Mathf.Clamp(lowPassCutoffHz, 10f, 22000f);
            filter.lowpassResonanceQ = Mathf.Clamp(lowPassResonanceQ, 1f, 10f);
        }

        private void ApplyReverb(GameObject target)
        {
            AudioReverbFilter filter = target.GetComponent<AudioReverbFilter>();
            if (!enableReverbFilter)
            {
                if (filter != null)
                {
                    filter.enabled = false;
                }

                return;
            }

            if (filter == null)
            {
                filter = target.AddComponent<AudioReverbFilter>();
            }

            filter.enabled = true;
            filter.reverbPreset = reverbPreset;
        }
    }
}
