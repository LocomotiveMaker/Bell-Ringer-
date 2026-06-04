using BellRinger.Gameplay;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoFootstepAudio : MonoBehaviour
    {
        [SerializeField] private FinalDemoAudioRouter audioRouter;
        [SerializeField] private Transform playerRig;
        [SerializeField] private BellRingerSimpleMoveLookController movementController;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 0.42f;
        [SerializeField] private float footHeightOffset = -1.55f;
        [SerializeField] private float minimumPlanarSpeed = 0.08f;
        [SerializeField] private float fallbackStepSeconds = 0.34f;

        private Vector3 _lastPlayerPosition;
        private bool _hasLastPlayerPosition;
        private bool _playFirstStepNext = true;
        private float _nextStepAtRealtime;

        public void Initialize(
            FinalDemoAudioRouter router,
            Transform player,
            BellRingerSimpleMoveLookController controller,
            float playerFixedHeight)
        {
            audioRouter = router;
            playerRig = player;
            movementController = controller;
            footHeightOffset = -Mathf.Max(0.1f, playerFixedHeight);
            ResetTracking();
        }

        public void ResetTracking()
        {
            _lastPlayerPosition = playerRig != null ? playerRig.position : Vector3.zero;
            _hasLastPlayerPosition = playerRig != null;
            _playFirstStepNext = true;
            _nextStepAtRealtime = 0f;
        }

        private void Update()
        {
            if (audioRouter == null || playerRig == null)
            {
                return;
            }

            float deltaSeconds = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            Vector3 currentPosition = playerRig.position;
            if (!_hasLastPlayerPosition)
            {
                _lastPlayerPosition = currentPosition;
                _hasLastPlayerPosition = true;
                return;
            }

            Vector3 delta = currentPosition - _lastPlayerPosition;
            delta.y = 0f;
            _lastPlayerPosition = currentPosition;

            bool movementAllowed = movementController == null || movementController.MovementEnabled;
            float planarSpeed = delta.magnitude / deltaSeconds;
            if (!movementAllowed || planarSpeed < Mathf.Max(0.001f, minimumPlanarSpeed))
            {
                _nextStepAtRealtime = 0f;
                return;
            }

            if (_nextStepAtRealtime > 0f && Time.realtimeSinceStartup < _nextStepAtRealtime)
            {
                return;
            }

            FinalDemoCueId cueId = _playFirstStepNext ? FinalDemoCueId.FootstepOne : FinalDemoCueId.FootstepTwo;
            audioRouter.PlayOneShot(cueId, ResolveFootPosition(), volumeScale);
            _playFirstStepNext = !_playFirstStepNext;

            float cueSeconds = audioRouter.GetCueLengthSeconds(cueId);
            _nextStepAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0.05f, cueSeconds > 0f ? cueSeconds : fallbackStepSeconds);
        }

        private Vector3 ResolveFootPosition()
        {
            Vector3 position = playerRig.position;
            position.y += footHeightOffset;
            return position;
        }
    }
}
