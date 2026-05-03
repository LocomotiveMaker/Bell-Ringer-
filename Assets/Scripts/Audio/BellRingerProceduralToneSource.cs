using UnityEngine;

namespace BellRinger.Audio
{
    public enum BellRingerToneWaveform
    {
        Sine,
        Triangle,
        Square,
        Saw,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class BellRingerProceduralToneSource : MonoBehaviour
    {
        [SerializeField] private BellRingerToneWaveform waveform = BellRingerToneWaveform.Sine;
        [SerializeField] [Range(20f, 20000f)] private float frequencyHz = 440f;
        [SerializeField] [Range(0f, 0.5f)] private float amplitude = 0.12f;
        [SerializeField] private float fadeSeconds = 0.02f;
        [SerializeField] private bool playOnStart;

        private AudioSource _audioSource;
        private AudioClip _clip;
        private float _phase;
        private float _currentGain;
        private bool _toneEnabled;
        private int _sampleRate;

        public float FrequencyHz
        {
            get => frequencyHz;
            set => frequencyHz = Mathf.Clamp(value, 20f, 20000f);
        }

        public BellRingerToneWaveform Waveform
        {
            get => waveform;
            set => waveform = value;
        }

        public bool IsToneEnabled => _toneEnabled;

        public void StartTone()
        {
            EnsureAudioSource();
            _toneEnabled = true;
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        public void StopTone()
        {
            _toneEnabled = false;
        }

        public void ToggleTone()
        {
            if (_toneEnabled)
            {
                StopTone();
            }
            else
            {
                StartTone();
            }
        }

        private void Awake()
        {
            EnsureAudioSource();
        }

        private void Start()
        {
            if (playOnStart)
            {
                StartTone();
            }
        }

        private void OnEnable()
        {
            EnsureAudioSource();
        }

        private void OnDisable()
        {
            _toneEnabled = false;
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        private void OnValidate()
        {
            frequencyHz = Mathf.Clamp(frequencyHz, 20f, 20000f);
            amplitude = Mathf.Clamp(amplitude, 0f, 0.5f);
            fadeSeconds = Mathf.Max(0f, fadeSeconds);
        }

        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_clip != null)
            {
                return;
            }

            _sampleRate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            _clip = AudioClip.Create("BellRinger Procedural Tone", _sampleRate, 1, _sampleRate, true, OnAudioRead, OnAudioSetPosition);
            _audioSource.clip = _clip;
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
        }

        private void OnAudioRead(float[] data)
        {
            float targetGain = _toneEnabled ? amplitude : 0f;
            float gainStep = fadeSeconds <= 0f ? 1f : 1f / (fadeSeconds * Mathf.Max(1, _sampleRate));
            float phaseStep = Mathf.Clamp(frequencyHz, 20f, 20000f) / Mathf.Max(1, _sampleRate);

            for (int i = 0; i < data.Length; i++)
            {
                if (_currentGain < targetGain)
                {
                    _currentGain = Mathf.Min(targetGain, _currentGain + gainStep);
                }
                else if (_currentGain > targetGain)
                {
                    _currentGain = Mathf.Max(targetGain, _currentGain - gainStep);
                }

                data[i] = EvaluateWaveform(_phase) * _currentGain;
                _phase += phaseStep;
                if (_phase >= 1f)
                {
                    _phase -= Mathf.Floor(_phase);
                }
            }
        }

        private void OnAudioSetPosition(int newPosition)
        {
            _phase = 0f;
            _currentGain = 0f;
        }

        private float EvaluateWaveform(float phase)
        {
            switch (waveform)
            {
                case BellRingerToneWaveform.Triangle:
                    return 1f - 4f * Mathf.Abs(Mathf.Round(phase - 0.25f) - (phase - 0.25f));
                case BellRingerToneWaveform.Square:
                    return phase < 0.5f ? 1f : -1f;
                case BellRingerToneWaveform.Saw:
                    return (2f * phase) - 1f;
                default:
                    return Mathf.Sin(phase * Mathf.PI * 2f);
            }
        }
    }
}
