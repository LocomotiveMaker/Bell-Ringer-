using BellRinger.Audio;
using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Debug
{
    [DisallowMultipleComponent]
    public sealed class BellRingerLightTextureTestDriver : MonoBehaviour
    {
        private const string DefaultBellClipPath = "BellRingerDemo/default-bell";

        [SerializeField] private BellRingerLightTexturePlayer texturePlayer;
        [SerializeField] private Transform listenerTransform;
        [SerializeField] private Transform bellTransform;
        [SerializeField] private AudioClip bellClip;
        [SerializeField] private AudioSource bellAudioSource;
        [SerializeField] private float orbitRadius = 4f;
        [SerializeField] private float orbitHeight = 1.35f;
        [SerializeField] private float orbitSpeedDegreesPerSecond = 28f;
        [SerializeField] private float ringIntervalSeconds = 1.35f;
        [SerializeField] private bool sendStartupHardwarePulse = true;
        [SerializeField] [Range(0f, 1f)] private float startupPulseBrightness = 0.35f;
        [SerializeField] private float startupPulseDurationSeconds = 0.35f;
        [SerializeField] private float startupPulseTimeoutSeconds = 5f;

        private float _nextRingTime;
        private float _startupPulseTimeoutAt;
        private float _clearStartupPulseAt = -1f;
        private bool _startupPulseSent;
        private bool _startupPulseCleared = true;

        private void Start()
        {
            ResolveReferences();
            _nextRingTime = Time.unscaledTime + 1.2f;
            _startupPulseTimeoutAt = Time.unscaledTime + Mathf.Max(0.1f, startupPulseTimeoutSeconds);
        }

        private void Update()
        {
            ResolveReferences();
            UpdateStartupHardwarePulse();

            if (listenerTransform == null || bellTransform == null || texturePlayer == null)
            {
                return;
            }

            float angle = Time.unscaledTime * orbitSpeedDegreesPerSecond * Mathf.Deg2Rad;
            Vector3 orbitOffset = new Vector3(Mathf.Sin(angle) * orbitRadius, orbitHeight, Mathf.Cos(angle) * orbitRadius);
            bellTransform.position = listenerTransform.position + orbitOffset;

            if (Time.unscaledTime < _nextRingTime)
            {
                return;
            }

            _nextRingTime = Time.unscaledTime + Mathf.Max(0.1f, ringIntervalSeconds);
            PlayBellSound();
            texturePlayer.TriggerAtWorldPosition(bellTransform.position, ResolveTriggerIntensity());
        }

        private void ResolveReferences()
        {
            if (texturePlayer == null)
            {
                texturePlayer = Object.FindFirstObjectByType<BellRingerLightTexturePlayer>();
            }

            if (listenerTransform == null)
            {
                AudioListener listener = Object.FindFirstObjectByType<AudioListener>();
                if (listener != null)
                {
                    listenerTransform = listener.transform;
                }
            }

            if (bellAudioSource == null && bellTransform != null)
            {
                bellAudioSource = bellTransform.GetComponent<AudioSource>();
                if (bellAudioSource == null)
                {
                    bellAudioSource = bellTransform.gameObject.AddComponent<AudioSource>();
                    bellAudioSource.volume = 0.85f;
                    bellAudioSource.spatialBlend = 1f;
                    bellAudioSource.minDistance = 0.5f;
                    bellAudioSource.maxDistance = 8f;
                    bellAudioSource.rolloffMode = AudioRolloffMode.Linear;
                }
            }

            if (bellAudioSource != null && bellAudioSource.clip == null)
            {
                bellAudioSource.clip = bellClip != null ? bellClip : Resources.Load<AudioClip>(DefaultBellClipPath);
            }
        }

        private void PlayBellSound()
        {
            if (bellAudioSource == null || bellAudioSource.clip == null)
            {
                UnityEngine.Debug.LogWarning("[BellRingerLightTextureTestDriver] Bell AudioSource or AudioClip is missing. Light texture still triggered for hardware diagnosis.");
                return;
            }

            bellAudioSource.Stop();
            bellAudioSource.Play();
        }

        private float ResolveTriggerIntensity()
        {
            return bellAudioSource == null ? 1f : bellAudioSource.volume;
        }

        private void UpdateStartupHardwarePulse()
        {
            if (!sendStartupHardwarePulse || (_startupPulseSent && _startupPulseCleared))
            {
                return;
            }

            HardwareBridge hardwareBridge = HardwareBridge.Instance;
            if (!_startupPulseSent)
            {
                if (hardwareBridge == null || !hardwareBridge.IsConnected)
                {
                    if (Time.unscaledTime >= _startupPulseTimeoutAt)
                    {
                        _startupPulseSent = true;
                        _startupPulseCleared = true;
                    }

                    return;
                }

                hardwareBridge.SendLedFill(startupPulseBrightness);
                UnityEngine.Debug.Log("[BellRingerLightTextureTestDriver] Sent startup LED fill pulse.");
                _startupPulseSent = true;
                _startupPulseCleared = false;
                _clearStartupPulseAt = Time.unscaledTime + Mathf.Max(0.05f, startupPulseDurationSeconds);
                _nextRingTime = Mathf.Max(_nextRingTime, _clearStartupPulseAt + 0.25f);
                return;
            }

            if (!_startupPulseCleared && Time.unscaledTime >= _clearStartupPulseAt)
            {
                if (hardwareBridge != null && hardwareBridge.IsConnected)
                {
                    hardwareBridge.ClearLedDisplay();
                }

                _startupPulseCleared = true;
                _clearStartupPulseAt = -1f;
            }
        }
    }
}
