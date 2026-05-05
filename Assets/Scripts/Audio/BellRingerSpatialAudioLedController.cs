using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Audio
{
    [DisallowMultipleComponent]
    public sealed class BellRingerSpatialAudioLedController : MonoBehaviour
    {
        [SerializeField] private Transform listenerTransform;
        [SerializeField] private float maximumBrightness = 0.22f;
        [SerializeField] private float minimumVisibleBrightness = 0.06f;
        [SerializeField] private float horizontalAngleLimitDegrees = 90f;
        [SerializeField] private float verticalAngleLimitDegrees = 55f;
        [SerializeField] private float brightnessRisePerSecond = 8f;
        [SerializeField] private float brightnessFallPerSecond = 6f;
        [SerializeField] private float positionFollowPixelsPerSecond = 24f;
        [SerializeField] private float serialRefreshRate = 30f;
        [SerializeField] private Color ledColor = BellRingerLightStyle.PadOrange;
        [SerializeField] [Range(0.2f, 1f)] private float averageLightScale = 0.58f;
        [SerializeField] [Range(1f, 3f)] private float peakContrast = 1.85f;
        [SerializeField] [Range(0.25f, 1f)] private float litPixelScale = 0.55f;
        [SerializeField] [Range(1f, 2f)] private float peakIntensityScale = 1.25f;
        [SerializeField] [Range(0.2f, 1.4f)] private float coreSizePixels = 0.55f;

        private Vector2 _currentPixel = new Vector2(8f, 4f);
        private float _currentBrightness;
        private float _nextRefreshTime;
        private int _lastSentX = -1;
        private int _lastSentY = -1;
        private int _lastSentBrightness = -1;

        private void Awake()
        {
            ResolveListenerTransform();
        }

        private void Update()
        {
            ResolveListenerTransform();

            if (listenerTransform == null)
            {
                return;
            }

            bool hasFrame = TryFindBestFrame(out BellRingerLedDotFrame targetFrame);
            float deltaTime = Time.unscaledDeltaTime;

            if (hasFrame)
            {
                Vector2 targetPixel = new Vector2(targetFrame.x, targetFrame.y);
                _currentPixel = Vector2.MoveTowards(_currentPixel, targetPixel, positionFollowPixelsPerSecond * deltaTime);
                _currentBrightness = Mathf.MoveTowards(_currentBrightness, targetFrame.brightnessNormalized, brightnessRisePerSecond * deltaTime);
            }
            else
            {
                _currentBrightness = Mathf.MoveTowards(_currentBrightness, 0f, brightnessFallPerSecond * deltaTime);
            }

            float refreshInterval = serialRefreshRate <= 0f ? 0.05f : 1f / serialRefreshRate;
            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + refreshInterval;
                PushFrameToHardware();
            }
        }

        private void ResolveListenerTransform()
        {
            if (listenerTransform != null)
            {
                return;
            }

            AudioListener localAudioListener = GetComponent<AudioListener>();
            if (localAudioListener != null)
            {
                listenerTransform = localAudioListener.transform;
                return;
            }

            AudioListener discoveredAudioListener = Object.FindFirstObjectByType<AudioListener>();
            if (discoveredAudioListener != null)
            {
                listenerTransform = discoveredAudioListener.transform;
            }
        }

        private bool TryFindBestFrame(out BellRingerLedDotFrame bestFrame)
        {
            bestFrame = default;
            float bestBrightness = 0f;
            var activeTargets = BellRingerSpatialSoundTarget.ActiveTargets;

            for (int i = 0; i < activeTargets.Count; i++)
            {
                BellRingerSpatialSoundTarget target = activeTargets[i];
                if (target == null || !target.isActiveAndEnabled)
                {
                    continue;
                }

                AudioSource source = target.Source;
                if (source == null || source.clip == null || !source.isPlaying || source.volume <= 0f)
                {
                    continue;
                }

                if (!BellRingerAudioLedMapper.TryMap(
                        listenerTransform,
                        source.transform.position,
                        source.minDistance,
                        source.maxDistance,
                        source.volume,
                        target.LedIntensityMultiplier,
                        maximumBrightness,
                        minimumVisibleBrightness,
                        horizontalAngleLimitDegrees,
                        verticalAngleLimitDegrees,
                        out BellRingerLedDotFrame candidateFrame))
                {
                    continue;
                }

                if (candidateFrame.brightnessNormalized <= bestBrightness)
                {
                    continue;
                }

                bestBrightness = candidateFrame.brightnessNormalized;
                bestFrame = candidateFrame;
            }

            return bestBrightness > 0f;
        }

        private void PushFrameToHardware()
        {
            HardwareBridge hardwareBridge = HardwareBridge.Instance;
            if (hardwareBridge == null)
            {
                return;
            }

            float styledBrightness = BellRingerLightStyle.ScaleLevel(_currentBrightness, averageLightScale, peakIntensityScale);
            int targetBrightness = Mathf.Clamp(Mathf.RoundToInt(styledBrightness * 255f), 0, 255);

            if (targetBrightness <= 0)
            {
                if (_lastSentBrightness != 0)
                {
                    hardwareBridge.ClearLedDisplay();
                    _lastSentBrightness = 0;
                    _lastSentX = -1;
                    _lastSentY = -1;
                }

                return;
            }

            int targetX = Mathf.Clamp(Mathf.RoundToInt(_currentPixel.x), 0, BellRingerAudioLedMapper.DisplayWidth - 1);
            int targetY = Mathf.Clamp(Mathf.RoundToInt(_currentPixel.y), 0, BellRingerAudioLedMapper.DisplayHeight - 1);

            if (targetX == _lastSentX && targetY == _lastSentY && targetBrightness == _lastSentBrightness)
            {
                return;
            }

            float coreSize = coreSizePixels * Mathf.Lerp(0.75f, 1.2f, litPixelScale);
            float width = Mathf.Lerp(0.45f, 0.9f, litPixelScale);
            hardwareBridge.SendLedPulseCore(targetX, targetY, 0f, coreSize, width, ledColor, targetBrightness / 255f, peakContrast);
            _lastSentX = targetX;
            _lastSentY = targetY;
            _lastSentBrightness = targetBrightness;
        }
    }
}
