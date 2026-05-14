using UnityEngine;
using UnityEngine.InputSystem;
using BellRinger.Hardware;

namespace BellRinger.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BellRingerSimpleMoveLookController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.75f;
        [SerializeField] private float mouseLookSensitivity = 0.08f;
        [SerializeField] private float gamepadLookDegreesPerSecond = 110f;
        [SerializeField] private float fixedHeight = 1.6f;
        [SerializeField] private float maximumPitchDegrees = 60f;
        [SerializeField] private bool useHeadTiltProviderWhenFresh = true;
        [SerializeField] private bool allowGamepadRightStickLook;

        private float _yawDegrees;
        private float _pitchDegrees;
        private HeadTiltInputProvider _headTiltInputProvider;

        public bool MovementEnabled { get; set; } = true;

        private void Start()
        {
            Vector3 eulerAngles = transform.rotation.eulerAngles;
            _yawDegrees = eulerAngles.y;
            _pitchDegrees = NormalizePitch(eulerAngles.x);
        }

        private void Update()
        {
            UpdateLook();
            UpdateMovement();
        }

        private void UpdateLook()
        {
            if (useHeadTiltProviderWhenFresh)
            {
                _headTiltInputProvider ??= GetComponent<HeadTiltInputProvider>() ?? FindFirstObjectByType<HeadTiltInputProvider>();
                if (_headTiltInputProvider != null && _headTiltInputProvider.HasFreshSample)
                {
                    _yawDegrees = _headTiltInputProvider.VirtualYawDegrees;
                    _pitchDegrees = Mathf.Clamp(_headTiltInputProvider.VirtualPitchDegrees, -maximumPitchDegrees, maximumPitchDegrees);
                    transform.rotation = _headTiltInputProvider.VirtualRotation;
                    return;
                }
            }

            Vector2 lookDelta = Vector2.zero;

            if (allowGamepadRightStickLook && Gamepad.current != null)
            {
                lookDelta += Gamepad.current.rightStick.ReadValue() * (gamepadLookDegreesPerSecond * Time.unscaledDeltaTime);
            }

            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                lookDelta += Mouse.current.delta.ReadValue() * mouseLookSensitivity;
            }

            _yawDegrees += lookDelta.x;
            _pitchDegrees = Mathf.Clamp(_pitchDegrees - lookDelta.y, -maximumPitchDegrees, maximumPitchDegrees);

            transform.rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
        }

        private void UpdateMovement()
        {
            if (!MovementEnabled)
            {
                Vector3 lockedPosition = transform.position;
                lockedPosition.y = fixedHeight;
                transform.position = lockedPosition;
                return;
            }

            Vector2 moveInput = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed)
                {
                    moveInput.y += 1f;
                }

                if (Keyboard.current.sKey.isPressed)
                {
                    moveInput.y -= 1f;
                }

                if (Keyboard.current.dKey.isPressed)
                {
                    moveInput.x += 1f;
                }

                if (Keyboard.current.aKey.isPressed)
                {
                    moveInput.x -= 1f;
                }
            }

            if (Gamepad.current != null)
            {
                moveInput += Gamepad.current.leftStick.ReadValue();
            }

            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            Quaternion flatRotation = Quaternion.Euler(0f, _yawDegrees, 0f);
            Vector3 moveVector = (flatRotation * Vector3.forward * moveInput.y) + (flatRotation * Vector3.right * moveInput.x);
            Vector3 nextPosition = transform.position + (moveVector * (moveSpeed * Time.unscaledDeltaTime));
            nextPosition.y = fixedHeight;
            transform.position = nextPosition;
        }

        private static float NormalizePitch(float rawPitchDegrees)
        {
            if (rawPitchDegrees > 180f)
            {
                rawPitchDegrees -= 360f;
            }

            return rawPitchDegrees;
        }
    }
}
