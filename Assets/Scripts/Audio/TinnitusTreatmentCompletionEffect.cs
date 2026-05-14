using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Audio
{
    [DisallowMultipleComponent]
    public sealed class TinnitusTreatmentCompletionEffect : MonoBehaviour
    {
        [SerializeField] private AudioClip resolveClip;
        [SerializeField] [Range(0f, 1f)] private float resolveVolume = 0.55f;
        [SerializeField] private float echoDelayMilliseconds = 180f;
        [SerializeField] [Range(0f, 1f)] private float echoDecayRatio = 0.72f;
        [SerializeField] [Range(0f, 1f)] private float echoWetMix = 0.9f;
        [SerializeField] private bool clearHardwareOnComplete = true;

        private AudioSource _resolveAudioSource;
        private AudioClip _fallbackResolveClip;

        public AudioClip ResolveClip
        {
            get => resolveClip;
            set => resolveClip = value;
        }

        public void Complete(
            Transform origin,
            TinnitusAudioController audioController,
            TinnitusLightPatternController lightController)
        {
            if (audioController != null)
            {
                audioController.CleanseStability = 1f;
                audioController.Volume = 0f;
            }

            if (lightController != null)
            {
                lightController.CleanseStability = 1f;
                lightController.CoreIntensity = 0f;
            }

            if (clearHardwareOnComplete && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.ClearLedDisplay();
            }

            PlayResolveCue(origin);
        }

        private void PlayResolveCue(Transform origin)
        {
            EnsureAudioSource();
            _resolveAudioSource.transform.position = origin != null ? origin.position : transform.position;
            _resolveAudioSource.PlayOneShot(resolveClip != null ? resolveClip : GetFallbackResolveClip(), resolveVolume);
        }

        private void EnsureAudioSource()
        {
            if (_resolveAudioSource != null)
            {
                return;
            }

            GameObject sourceObject = new GameObject("Tinnitus Resolve Echo Source");
            sourceObject.transform.SetParent(transform, false);
            _resolveAudioSource = sourceObject.AddComponent<AudioSource>();
            _resolveAudioSource.spatialBlend = 1f;
            _resolveAudioSource.minDistance = 0.5f;
            _resolveAudioSource.maxDistance = 12f;
            _resolveAudioSource.rolloffMode = AudioRolloffMode.Linear;
            _resolveAudioSource.dopplerLevel = 0f;

            AudioEchoFilter echoFilter = sourceObject.AddComponent<AudioEchoFilter>();
            echoFilter.delay = echoDelayMilliseconds;
            echoFilter.decayRatio = echoDecayRatio;
            echoFilter.dryMix = 0.65f;
            echoFilter.wetMix = echoWetMix;
        }

        private AudioClip GetFallbackResolveClip()
        {
            if (_fallbackResolveClip != null)
            {
                return _fallbackResolveClip;
            }

            int sampleRate = Mathf.Max(16000, AudioSettings.outputSampleRate);
            int sampleCount = Mathf.RoundToInt(sampleRate * 0.65f);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Exp(-t * 4.8f);
                float sweep = Mathf.Lerp(1320f, 620f, Mathf.Clamp01(t / 0.65f));
                float tone = Mathf.Sin(t * sweep * Mathf.PI * 2f) * 0.45f;
                tone += Mathf.Sin(t * sweep * 1.5f * Mathf.PI * 2f) * 0.18f;
                samples[i] = tone * envelope;
            }

            _fallbackResolveClip = AudioClip.Create("Synthetic Tinnitus Resolve", sampleCount, 1, sampleRate, false);
            _fallbackResolveClip.SetData(samples, 0);
            return _fallbackResolveClip;
        }
    }
}
