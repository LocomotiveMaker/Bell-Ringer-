using UnityEngine;

namespace BellRinger.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BellGazeTutorialController : MonoBehaviour
    {
        private enum TutorialPhase
        {
            Idle,
            Orbit,
            CueMove,
            Gaze,
            Complete,
        }

        [SerializeField] private Transform listenerTransform;
        [SerializeField] private Transform bellTransform;
        [SerializeField] private AudioClip bellClip;
        [SerializeField] private float orbitSeconds = 4f;
        [SerializeField] private float gazeSeconds = 2.2f;
        [SerializeField] private float gazeAngleDegrees = 16f;
        [SerializeField] private float approachDistanceMeters = 0.8f;
        [SerializeField] private float ringIntervalSeconds = 1.15f;

        private readonly Vector3[] _cueOffsets =
        {
            new Vector3(1.6f, 0f, 2.2f),
            new Vector3(-1.4f, 0.1f, 2.6f),
            new Vector3(0.4f, 0f, 1.4f),
        };

        private TutorialPhase _phase;
        private Vector3 _cueStartPosition;
        private int _cueIndex;
        private float _phaseStartRealtime;
        private float _gazeProgressSeconds;
        private float _nextRingRealtime;
        private AudioSource _bellAudioSource;

        public bool IsRunning => _phase != TutorialPhase.Idle && _phase != TutorialPhase.Complete;
        public bool IsComplete => _phase == TutorialPhase.Complete;
        public string PhaseName => _phase.ToString();
        public float GazeProgress01 => Mathf.Clamp01(_gazeProgressSeconds / Mathf.Max(0.1f, gazeSeconds));
        public float GazeAngleDegrees { get; private set; } = 180f;

        private void Update()
        {
            Tick();
        }

        public void Configure(Transform listener, Transform bell, AudioClip clip)
        {
            listenerTransform = listener;
            bellTransform = bell;
            bellClip = clip;
            EnsureAudioSource();
        }

        public void StartTutorial()
        {
            if (listenerTransform == null || bellTransform == null)
            {
                return;
            }

            _phase = TutorialPhase.Orbit;
            _cueIndex = 0;
            _gazeProgressSeconds = 0f;
            _phaseStartRealtime = Time.unscaledTime;
            _nextRingRealtime = 0f;
            bellTransform.gameObject.SetActive(true);
        }

        public void ResetTutorial()
        {
            _phase = TutorialPhase.Idle;
            _cueIndex = 0;
            _gazeProgressSeconds = 0f;
            GazeAngleDegrees = 180f;
        }

        public void Tick()
        {
            if (_phase == TutorialPhase.Idle || _phase == TutorialPhase.Complete || listenerTransform == null || bellTransform == null)
            {
                return;
            }

            if (_phase == TutorialPhase.Orbit)
            {
                UpdateOrbit();
            }
            else if (_phase == TutorialPhase.CueMove)
            {
                UpdateCueMove();
            }
            else if (_phase == TutorialPhase.Gaze)
            {
                UpdateGaze();
            }

            TryPlayRingCue();
        }

        private void UpdateOrbit()
        {
            float elapsed = Time.unscaledTime - _phaseStartRealtime;
            float angle = elapsed * Mathf.PI * 2f * 0.45f;
            bellTransform.position = listenerTransform.position +
                                     new Vector3(Mathf.Cos(angle) * 1.35f, 0.02f + Mathf.Sin(elapsed * 3.2f) * 0.1f, Mathf.Sin(angle) * 1.35f);
            bellTransform.LookAt(listenerTransform.position + Vector3.up * 0.2f);

            if (elapsed >= orbitSeconds)
            {
                BeginCueMove();
            }
        }

        private void BeginCueMove()
        {
            _phase = TutorialPhase.CueMove;
            _phaseStartRealtime = Time.unscaledTime;
            _cueStartPosition = bellTransform.position;
        }

        private void UpdateCueMove()
        {
            Vector3 target = listenerTransform.position + _cueOffsets[Mathf.Clamp(_cueIndex, 0, _cueOffsets.Length - 1)];
            float travel01 = Mathf.Clamp01((Time.unscaledTime - _phaseStartRealtime) / 1.1f);
            bellTransform.position = Vector3.Lerp(_cueStartPosition, target, Smooth01(travel01));
            bellTransform.LookAt(listenerTransform.position + Vector3.up * 0.2f);

            if (travel01 < 1f)
            {
                return;
            }

            float distance = Vector3.Distance(listenerTransform.position, bellTransform.position);
            if (distance <= approachDistanceMeters)
            {
                _cueIndex++;
                if (_cueIndex >= _cueOffsets.Length)
                {
                    _phase = TutorialPhase.Gaze;
                    _phaseStartRealtime = Time.unscaledTime;
                    return;
                }

                BeginCueMove();
            }
        }

        private void UpdateGaze()
        {
            float hoverTime = Time.unscaledTime - _phaseStartRealtime;
            Vector3 basePosition = listenerTransform.position +
                                   (listenerTransform.forward * 1.2f) +
                                   (listenerTransform.right * Mathf.Sin(hoverTime * 1.8f) * 0.35f) +
                                   (listenerTransform.up * (Mathf.Sin(hoverTime * 2.7f) * 0.12f));
            bellTransform.position = basePosition;
            bellTransform.LookAt(listenerTransform.position + Vector3.up * 0.1f);

            Vector3 toBell = (bellTransform.position - listenerTransform.position).normalized;
            GazeAngleDegrees = Vector3.Angle(listenerTransform.forward, toBell);
            if (GazeAngleDegrees <= gazeAngleDegrees)
            {
                _gazeProgressSeconds = Mathf.Min(gazeSeconds, _gazeProgressSeconds + Time.unscaledDeltaTime);
            }

            if (_gazeProgressSeconds >= gazeSeconds)
            {
                _phase = TutorialPhase.Complete;
            }
        }

        private void TryPlayRingCue()
        {
            if (Time.unscaledTime < _nextRingRealtime)
            {
                return;
            }

            _nextRingRealtime = Time.unscaledTime + Mathf.Max(0.1f, ringIntervalSeconds);
            EnsureAudioSource();
            if (_bellAudioSource != null && bellClip != null)
            {
                _bellAudioSource.PlayOneShot(bellClip, 0.7f);
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 8f) * 0.08f;
            bellTransform.localScale = Vector3.one * (0.22f * pulse);
        }

        private void EnsureAudioSource()
        {
            if (bellTransform == null || _bellAudioSource != null)
            {
                return;
            }

            _bellAudioSource = bellTransform.GetComponent<AudioSource>();
            if (_bellAudioSource == null)
            {
                _bellAudioSource = bellTransform.gameObject.AddComponent<AudioSource>();
            }

            _bellAudioSource.spatialBlend = 1f;
            _bellAudioSource.minDistance = 0.5f;
            _bellAudioSource.maxDistance = 12f;
            _bellAudioSource.rolloffMode = AudioRolloffMode.Linear;
            _bellAudioSource.dopplerLevel = 0f;
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }
    }
}
