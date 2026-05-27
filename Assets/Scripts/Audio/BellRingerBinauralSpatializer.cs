using UnityEngine;

namespace BellRinger.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class BellRingerBinauralSpatializer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Transform listenerTransform;
        [SerializeField] private bool bypass = true;
        [SerializeField, Range(0f, 1f)] private float strength = 1f;

        private const int MaxDelaySamples = 96;
        private readonly float[] _delayBuffer = new float[MaxDelaySamples];
        private int _writeIndex;
        private float _leftGain = 1f;
        private float _rightGain = 1f;
        private float _side;
        private float _distanceGain = 1f;
        private float _farEarLowPassAlpha = 1f;
        private float _behindLowPassAlpha = 1f;
        private int _delaySamples;
        private float _leftFiltered;
        private float _rightFiltered;

        public bool Bypass
        {
            get => bypass;
            set => bypass = value;
        }

        public void Configure(AudioSource source, Transform listener, bool enabled, float binauralStrength = 1f)
        {
            audioSource = source != null ? source : GetComponent<AudioSource>();
            listenerTransform = listener;
            bypass = !enabled;
            strength = Mathf.Clamp01(binauralStrength);
            UpdateParameters();
        }

        private void Awake()
        {
            audioSource ??= GetComponent<AudioSource>();
        }

        private void Update()
        {
            UpdateParameters();
        }

        private void UpdateParameters()
        {
            if (audioSource == null || listenerTransform == null)
            {
                _leftGain = 1f;
                _rightGain = 1f;
                _side = 0f;
                _distanceGain = 1f;
                _farEarLowPassAlpha = 1f;
                _behindLowPassAlpha = 1f;
                _delaySamples = 0;
                return;
            }

            Vector3 local = listenerTransform.InverseTransformPoint(transform.position);
            float horizontalMagnitude = Mathf.Max(0.001f, new Vector2(local.x, local.z).magnitude);
            float side = Mathf.Clamp((local.x / horizontalMagnitude) * 1.18f, -1f, 1f);
            float absSide = Mathf.Abs(side);
            bool behind = local.z < -0.05f;

            float ild = Mathf.Lerp(0f, 0.58f, absSide) * strength;
            float nearBoost = Mathf.Lerp(0f, 0.11f, absSide) * strength;
            _leftGain = side > 0f ? 1f - ild : 1f + nearBoost;
            _rightGain = side > 0f ? 1f + nearBoost : 1f - ild;

            float sampleRate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            _delaySamples = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(0f, 0.00082f * sampleRate, absSide) * strength), 0, MaxDelaySamples - 1);
            _side = side;

            float distance = Vector3.Distance(listenerTransform.position, transform.position);
            float minDistance = Mathf.Max(0.01f, audioSource.minDistance);
            float maxDistance = Mathf.Max(minDistance + 0.01f, audioSource.maxDistance);
            _distanceGain = distance <= minDistance ? 1f : 1f - Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));

            _farEarLowPassAlpha = Mathf.Lerp(1f, 0.38f, absSide * strength);
            _behindLowPassAlpha = behind ? Mathf.Lerp(1f, 0.58f, strength) : 1f;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (bypass || data == null || channels <= 0)
            {
                return;
            }

            if (channels < 2)
            {
                ApplyMonoDistance(data);
                return;
            }

            float dryMix = Mathf.Clamp01(1f - strength);
            float wetMix = Mathf.Clamp01(strength);
            int leftChannel = 0;
            int rightChannel = 1;

            for (int frame = 0; frame < data.Length; frame += channels)
            {
                float inLeft = data[frame + leftChannel];
                float inRight = data[frame + rightChannel];
                float mono = (inLeft + inRight) * 0.5f;
                float delayed = ReadDelayedSample(_delaySamples);
                WriteDelaySample(mono);

                float wetLeft;
                float wetRight;
                if (_side > 0f)
                {
                    wetLeft = ApplyOnePole(ref _leftFiltered, delayed * _leftGain, _farEarLowPassAlpha);
                    wetRight = ApplyOnePole(ref _rightFiltered, mono * _rightGain, 1f);
                }
                else if (_side < 0f)
                {
                    wetLeft = ApplyOnePole(ref _leftFiltered, mono * _leftGain, 1f);
                    wetRight = ApplyOnePole(ref _rightFiltered, delayed * _rightGain, _farEarLowPassAlpha);
                }
                else
                {
                    wetLeft = mono * _leftGain;
                    wetRight = mono * _rightGain;
                }

                wetLeft = ApplyOnePole(ref _leftFiltered, wetLeft, _behindLowPassAlpha) * _distanceGain;
                wetRight = ApplyOnePole(ref _rightFiltered, wetRight, _behindLowPassAlpha) * _distanceGain;

                data[frame + leftChannel] = Mathf.Clamp((inLeft * dryMix) + (wetLeft * wetMix), -1f, 1f);
                data[frame + rightChannel] = Mathf.Clamp((inRight * dryMix) + (wetRight * wetMix), -1f, 1f);
            }
        }

        private void ApplyMonoDistance(float[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= _distanceGain;
            }
        }

        private float ReadDelayedSample(int delaySamples)
        {
            int readIndex = _writeIndex - delaySamples;
            while (readIndex < 0)
            {
                readIndex += MaxDelaySamples;
            }

            return _delayBuffer[readIndex % MaxDelaySamples];
        }

        private void WriteDelaySample(float sample)
        {
            _delayBuffer[_writeIndex] = sample;
            _writeIndex = (_writeIndex + 1) % MaxDelaySamples;
        }

        private static float ApplyOnePole(ref float state, float input, float alpha)
        {
            state += Mathf.Clamp01(alpha) * (input - state);
            return state;
        }
    }
}
