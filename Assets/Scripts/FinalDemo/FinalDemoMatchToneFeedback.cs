using BellRinger.Audio;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class FinalDemoMatchToneFeedback : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.2f)] private float volume = 0.0135f;
        [SerializeField] private Vector2 positionFrequencyRange = new Vector2(40f, 100f);
        [SerializeField] private Vector2 rotationFrequencyRange = new Vector2(45f, 110f);
        [SerializeField, Range(0f, 1f)] private float rotationVolumeMultiplier = 0.35f;
        [SerializeField] private float smoothingSpeed = 10f;

        private AudioSource _source;
        private AudioClip _carrierClip;
        private Transform _listener;
        private bool _binauralEnabled;
        private bool _active;
        private bool _rotationEnabled;
        private float _targetPositionMatch01;
        private float _targetRotationMatch01;
        private float _targetVolumeScale01;
        private float _positionMatch01;
        private float _rotationMatch01;
        private float _volumeScale01;
        private int _sampleRate;
        private double _positionPhase;
        private double _rotationPhase;

        public void ConfigureAudio(Transform listener, bool binauralEnabled)
        {
            EnsureSource();
            _listener = listener;
            _binauralEnabled = binauralEnabled;
            ApplySpatialization();
        }

        public void ConfigureSettings(
            float outputVolume,
            Vector2 positionRange,
            Vector2 rotationRange,
            float rotationMultiplier)
        {
            volume = Mathf.Clamp(outputVolume, 0f, 0.2f);
            positionFrequencyRange = ClampFrequencyRange(positionRange, new Vector2(40f, 100f));
            rotationFrequencyRange = ClampFrequencyRange(rotationRange, new Vector2(45f, 110f));
            rotationVolumeMultiplier = Mathf.Clamp01(rotationMultiplier);
        }

        public void SetSpatialRadius(float radiusMeters)
        {
            EnsureSource();
            float radius = Mathf.Max(0.1f, radiusMeters);
            _source.maxDistance = radius;
            _source.minDistance = Mathf.Min(_source.minDistance, radius * 0.25f);
        }

        public void SetMatch(
            Vector3 worldPosition,
            float positionMatch01,
            float rotationMatch01,
            bool rotationEnabled,
            float volumeScale01)
        {
            EnsureSource();
            transform.position = worldPosition;
            _targetPositionMatch01 = Mathf.Clamp01(positionMatch01);
            _targetRotationMatch01 = Mathf.Clamp01(rotationMatch01);
            _targetVolumeScale01 = Mathf.Clamp01(volumeScale01);
            _rotationEnabled = rotationEnabled;
            _active = volume > 0.0001f && _targetVolumeScale01 > 0.0001f;

            if (_active && !_source.isPlaying)
            {
                _source.Play();
            }
        }

        public void StopTone()
        {
            _active = false;
            _targetVolumeScale01 = 0f;
        }

        private void Awake()
        {
            EnsureSource();
        }

        private void Update()
        {
            EnsureSource();
            float step = Mathf.Max(1f, smoothingSpeed) * Time.unscaledDeltaTime;
            _positionMatch01 = Mathf.MoveTowards(_positionMatch01, _targetPositionMatch01, step);
            _rotationMatch01 = Mathf.MoveTowards(_rotationMatch01, _targetRotationMatch01, step);
            _volumeScale01 = Mathf.MoveTowards(_volumeScale01, _targetVolumeScale01, step);

            if (!_active && _volumeScale01 <= 0.001f && _source.isPlaying)
            {
                _source.Stop();
            }
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp(volume, 0f, 0.2f);
            positionFrequencyRange = ClampFrequencyRange(positionFrequencyRange, new Vector2(40f, 100f));
            rotationFrequencyRange = ClampFrequencyRange(rotationFrequencyRange, new Vector2(45f, 110f));
            rotationVolumeMultiplier = Mathf.Clamp01(rotationVolumeMultiplier);
            smoothingSpeed = Mathf.Max(1f, smoothingSpeed);
        }

        private void EnsureSource()
        {
            if (_source == null)
            {
                _source = GetComponent<AudioSource>();
            }

            if (_carrierClip != null)
            {
                return;
            }

            _sampleRate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            _carrierClip = AudioClip.Create("Final Demo Match Tone Carrier", _sampleRate, 1, _sampleRate, false);
            _source.clip = _carrierClip;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = 1f;
            _source.spatialBlend = 1f;
            _source.minDistance = 0.45f;
            _source.maxDistance = 8f;
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.dopplerLevel = 0f;
            ApplySpatialization();
        }

        private void ApplySpatialization()
        {
            if (_source == null)
            {
                return;
            }

            BellRingerBinauralSpatializer binaural = _source.GetComponent<BellRingerBinauralSpatializer>();
            if (_binauralEnabled)
            {
                _source.spatialBlend = 0f;
                _source.spatialize = false;
                if (binaural == null)
                {
                    binaural = _source.gameObject.AddComponent<BellRingerBinauralSpatializer>();
                }

                binaural.Configure(_source, _listener, true, 1f);
                return;
            }

            _source.spatialBlend = 1f;
            _source.spatialize = false;
            if (binaural != null)
            {
                binaural.Configure(_source, _listener, false, 1f);
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_sampleRate <= 0)
            {
                _sampleRate = 48000;
            }

            float positionMatch = Mathf.Clamp01(_positionMatch01);
            float rotationMatch = Mathf.Clamp01(_rotationMatch01);
            float scale = Mathf.Clamp01(_volumeScale01);
            float outputVolume = volume * scale;
            if (!_active && outputVolume <= 0.0001f)
            {
                return;
            }

            float positionFrequency = Mathf.Lerp(positionFrequencyRange.x, positionFrequencyRange.y, positionMatch);
            float rotationFrequency = Mathf.Lerp(rotationFrequencyRange.x, rotationFrequencyRange.y, rotationMatch);
            float positionGain = Mathf.Lerp(0.22f, 1f, positionMatch);
            float rotationGain = _rotationEnabled ? Mathf.Lerp(0.12f, 1f, rotationMatch) * rotationVolumeMultiplier : 0f;

            for (int frame = 0; frame < data.Length; frame += channels)
            {
                _positionPhase += positionFrequency / _sampleRate;
                _rotationPhase += rotationFrequency / _sampleRate;
                if (_positionPhase >= 1.0)
                {
                    _positionPhase -= System.Math.Floor(_positionPhase);
                }

                if (_rotationPhase >= 1.0)
                {
                    _rotationPhase -= System.Math.Floor(_rotationPhase);
                }

                float sine = Mathf.Sin((float)_positionPhase * Mathf.PI * 2f) * positionGain;
                float square = ((float)_rotationPhase < 0.5f ? 1f : -1f) * rotationGain;
                float sample = (sine * 0.62f + square * 0.28f) * outputVolume;
                sample = Mathf.Clamp(sample, -0.45f, 0.45f);
                for (int channel = 0; channel < channels; channel++)
                {
                    data[frame + channel] += sample;
                }
            }
        }

        private static Vector2 ClampFrequencyRange(Vector2 range, Vector2 fallback)
        {
            if (range.x <= 0f || range.y <= 0f)
            {
                range = fallback;
            }

            float min = Mathf.Clamp(Mathf.Min(range.x, range.y), 20f, 16000f);
            float max = Mathf.Clamp(Mathf.Max(range.x, range.y), min + 1f, 16000f);
            return new Vector2(min, max);
        }
    }
}
