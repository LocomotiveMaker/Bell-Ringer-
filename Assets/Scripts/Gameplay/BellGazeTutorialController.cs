using UnityEngine;

namespace BellRinger.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BellGazeTutorialController : MonoBehaviour
    {
        private enum TutorialPhase
        {
            Idle,
            CueMove,
            Gaze,
            Complete,
        }

        [SerializeField] private Transform listenerTransform;
        [SerializeField] private Transform bellTransform;
        [SerializeField] private AudioClip bellClip;
        [SerializeField] private float gazeSeconds = 2.2f;
        [SerializeField] private float gazeAngleDegrees = 16f;
        [SerializeField] private float ringIntervalSeconds = 1.15f;

        private readonly Vector3[] _cueOffsets =
        {
            new Vector3(0.15f, -0.02f, 1.45f),
            new Vector3(-0.85f, 0.06f, 1.75f),
            new Vector3(0.9f, -0.04f, 1.65f),
        };

        private TutorialPhase _phase;
        private Vector3 _cueStartPosition;
        private Vector3 _cueTargetPosition;
        private int _cueIndex;
        private float _phaseStartRealtime;
        private float _gazeProgressSeconds;
        private float _nextRingRealtime;
        private AudioSource _bellAudioSource;

        public bool IsRunning => _phase != TutorialPhase.Idle && _phase != TutorialPhase.Complete;
        public bool IsComplete => _phase == TutorialPhase.Complete;
        public string PhaseName => _phase.ToString();
        public int CueNumber => Mathf.Clamp(_cueIndex + 1, 1, _cueOffsets.Length);
        public int CueCount => _cueOffsets.Length;
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

            _phase = TutorialPhase.Gaze;
            _cueIndex = 0;
            _gazeProgressSeconds = 0f;
            _phaseStartRealtime = Time.unscaledTime;
            _nextRingRealtime = 0f;
            bellTransform.gameObject.SetActive(true);
            PlaceAtCue(_cueIndex);
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

            if (_phase == TutorialPhase.CueMove)
            {
                UpdateCueMove();
            }
            else if (_phase == TutorialPhase.Gaze)
            {
                UpdateGaze();
            }

            TryPlayRingCue();
        }

        private void BeginCueMove()
        {
            _phase = TutorialPhase.CueMove;
            _phaseStartRealtime = Time.unscaledTime;
            _cueStartPosition = bellTransform.position;
            _cueTargetPosition = CueToWorld(_cueOffsets[Mathf.Clamp(_cueIndex, 0, _cueOffsets.Length - 1)]);
        }

        private void UpdateCueMove()
        {
            float travel01 = Mathf.Clamp01((Time.unscaledTime - _phaseStartRealtime) / 1.1f);
            bellTransform.position = Vector3.Lerp(_cueStartPosition, _cueTargetPosition, Smooth01(travel01));
            bellTransform.LookAt(listenerTransform.position + Vector3.up * 0.2f);

            if (travel01 >= 1f)
            {
                _phase = TutorialPhase.Gaze;
                _phaseStartRealtime = Time.unscaledTime;
                _gazeProgressSeconds = 0f;
            }
        }

        private void UpdateGaze()
        {
            bellTransform.LookAt(listenerTransform.position + Vector3.up * 0.1f);

            Vector3 toBell = (bellTransform.position - listenerTransform.position).normalized;
            GazeAngleDegrees = Vector3.Angle(listenerTransform.forward, toBell);
            if (GazeAngleDegrees <= gazeAngleDegrees)
            {
                _gazeProgressSeconds = Mathf.Min(gazeSeconds, _gazeProgressSeconds + Time.unscaledDeltaTime);
            }

            if (_gazeProgressSeconds >= gazeSeconds)
            {
                _cueIndex++;
                if (_cueIndex >= _cueOffsets.Length)
                {
                    _phase = TutorialPhase.Complete;
                    return;
                }

                BeginCueMove();
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

        private void PlaceAtCue(int cueIndex)
        {
            bellTransform.position = CueToWorld(_cueOffsets[Mathf.Clamp(cueIndex, 0, _cueOffsets.Length - 1)]);
            bellTransform.LookAt(listenerTransform.position + Vector3.up * 0.2f);
        }

        private Vector3 CueToWorld(Vector3 cameraSpaceOffset)
        {
            return listenerTransform.position
                   + listenerTransform.right * cameraSpaceOffset.x
                   + listenerTransform.up * cameraSpaceOffset.y
                   + listenerTransform.forward * cameraSpaceOffset.z;
        }
    }
}
