using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoAudioReactiveLight : MonoBehaviour
    {
        public enum ReactiveLightKind
        {
            Bell,
            Tinnitus,
            BossTinnitus,
        }

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private FinalDemoLightRouter lightRouter;
        [SerializeField] private ReactiveLightKind lightKind;
        [SerializeField] private Vector3 worldPosition;
        [SerializeField] private float intensityScale = 1f;
        [SerializeField] private float sensitivity = 2.35f;
        [SerializeField] private float updateIntervalSeconds = 0.12f;
        [SerializeField] private float attackSpeed = 8f;
        [SerializeField] private float releaseSpeed = 2.2f;
        [SerializeField] private bool emissionEnabled = true;

        private readonly float[] _samples = new float[128];
        private float _envelope01;
        private float _emittedEnvelope01;
        private float _previousEnvelope01;
        private float _nextEmitAtRealtime;
        private bool _hasEmitted;

        public float Envelope01 => _envelope01;
        public bool EmissionEnabled
        {
            get => emissionEnabled;
            set => emissionEnabled = value;
        }

        public float IntensityScale
        {
            get => intensityScale;
            set => intensityScale = Mathf.Clamp01(value);
        }

        public void Configure(
            AudioSource source,
            FinalDemoLightRouter router,
            ReactiveLightKind kind,
            Vector3 position,
            float scale = 1f,
            float envelopeSensitivity = 2.35f,
            float intervalSeconds = 0.05f)
        {
            audioSource = source;
            lightRouter = router;
            lightKind = kind;
            worldPosition = position;
            intensityScale = Mathf.Clamp01(scale);
            sensitivity = Mathf.Max(0.1f, envelopeSensitivity);
            updateIntervalSeconds = Mathf.Clamp(intervalSeconds, 0.09f, 0.25f);
            _nextEmitAtRealtime = 0f;
        }

        public void SetWorldPosition(Vector3 position)
        {
            worldPosition = position;
        }

        private void Update()
        {
            if (audioSource == null || lightRouter == null)
            {
                return;
            }

            UpdateEnvelope();
            if (Time.realtimeSinceStartup < _nextEmitAtRealtime)
            {
                return;
            }

            _nextEmitAtRealtime = Time.realtimeSinceStartup + updateIntervalSeconds;
            float emitFactor = 1f - Mathf.Exp(-4.2f * updateIntervalSeconds);
            _emittedEnvelope01 = Mathf.Lerp(_emittedEnvelope01, _envelope01, emitFactor);
            float onset01 = Mathf.Clamp01((_emittedEnvelope01 - _previousEnvelope01) * 1.35f);
            _previousEnvelope01 = _emittedEnvelope01;

            if (!emissionEnabled)
            {
                return;
            }

            if (_emittedEnvelope01 <= 0.015f && _hasEmitted && !audioSource.isPlaying)
            {
                return;
            }

            Emit(onset01);
            _hasEmitted = true;
        }

        private void UpdateEnvelope()
        {
            float rawEnvelope = SampleOutputEnvelope();
            float speed = rawEnvelope >= _envelope01 ? attackSpeed : releaseSpeed;
            float factor = 1f - Mathf.Exp(-Mathf.Max(0.1f, speed) * Time.unscaledDeltaTime);
            _envelope01 = Mathf.Lerp(_envelope01, rawEnvelope, factor);
        }

        private float SampleOutputEnvelope()
        {
            if (audioSource == null)
            {
                return 0f;
            }

            audioSource.GetOutputData(_samples, 0);
            float sum = 0f;
            float peak = 0f;
            for (int i = 0; i < _samples.Length; i++)
            {
                float absolute = Mathf.Abs(_samples[i]);
                sum += absolute * absolute;
                peak = Mathf.Max(peak, absolute);
            }

            float rms = Mathf.Sqrt(sum / Mathf.Max(1, _samples.Length));
            float envelope = (rms * 2.35f) + (peak * 0.12f);
            envelope *= Mathf.Lerp(0.55f, 1.15f, Mathf.Clamp01(audioSource.volume));
            return Mathf.Clamp01(envelope * sensitivity);
        }

        private void Emit(float onset01)
        {
            switch (lightKind)
            {
                case ReactiveLightKind.Bell:
                    lightRouter.ShowBellWave(worldPosition, _emittedEnvelope01, onset01, intensityScale);
                    break;
                case ReactiveLightKind.BossTinnitus:
                    lightRouter.ShowTinnitusWave(worldPosition, _emittedEnvelope01, onset01, intensityScale, true);
                    break;
                default:
                    lightRouter.ShowTinnitusWave(worldPosition, _emittedEnvelope01, onset01, intensityScale, false);
                    break;
            }
        }
    }
}
