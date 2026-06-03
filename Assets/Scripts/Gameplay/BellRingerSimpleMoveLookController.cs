using UnityEngine;
using UnityEngine.InputSystem;
using BellRinger.Hardware;

namespace BellRinger.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BellRingerSimpleMoveLookController : MonoBehaviour
    {
        private const float DefaultCollisionRadius = 0.24f;
        private const float DefaultCollisionHeight = 1.55f;
        private const float DefaultCollisionSkinWidth = 0.03f;

        [SerializeField] private float moveSpeed = 1.75f;
        [SerializeField] private float mouseLookSensitivity = 0.08f;
        [SerializeField] private float gamepadLookDegreesPerSecond = 110f;
        [SerializeField] private float fixedHeight = 1.6f;
        [SerializeField] private float maximumPitchDegrees = 60f;
        [SerializeField] private bool useHeadTiltProviderWhenFresh = true;
        [SerializeField] private bool allowGamepadRightStickLook;
        [SerializeField] private bool usePhysicsCollision = true;
        [SerializeField] private float collisionRadius = 0.24f;
        [SerializeField] private float collisionHeight = 1.55f;
        [SerializeField] private float collisionSkinWidth = 0.03f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private float _yawDegrees;
        private float _pitchDegrees;
        private HeadTiltInputProvider _headTiltInputProvider;

        public bool MovementEnabled { get; set; } = true;

        private void Awake()
        {
            EnsureCollisionConfiguration();
        }

        private void OnValidate()
        {
            EnsureCollisionConfiguration();
        }

        private void Start()
        {
            EnsureCollisionConfiguration();
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
            Vector3 nextPosition = transform.position + ResolveMovementDelta(moveVector * (moveSpeed * Time.unscaledDeltaTime));
            nextPosition.y = fixedHeight;
            transform.position = nextPosition;
        }

        private Vector3 ResolveMovementDelta(Vector3 requestedDelta)
        {
            Vector3 planarDelta = new Vector3(requestedDelta.x, 0f, requestedDelta.z);
            if (!usePhysicsCollision || planarDelta.sqrMagnitude <= 0.0000001f)
            {
                return planarDelta;
            }

            Vector3 direction = planarDelta.normalized;
            float distance = planarDelta.magnitude;
            BuildCollisionCapsule(transform.position, out Vector3 bottom, out Vector3 top);
            if (!Physics.CapsuleCast(
                    bottom,
                    top,
                    Mathf.Max(0.05f, collisionRadius),
                    direction,
                    out RaycastHit hit,
                    distance + collisionSkinWidth,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return planarDelta;
            }

            float allowedDistance = Mathf.Max(0f, hit.distance - collisionSkinWidth);
            return direction * Mathf.Min(distance, allowedDistance);
        }

        private void BuildCollisionCapsule(Vector3 currentPosition, out Vector3 bottom, out Vector3 top)
        {
            float radius = Mathf.Max(0.05f, collisionRadius);
            float height = Mathf.Max(radius * 2f + 0.01f, collisionHeight);
            float groundY = currentPosition.y - fixedHeight;
            float bottomCenterY = groundY + radius;
            float topCenterY = groundY + height - radius;
            bottom = new Vector3(currentPosition.x, bottomCenterY, currentPosition.z);
            top = new Vector3(currentPosition.x, topCenterY, currentPosition.z);
        }

        private static float NormalizePitch(float rawPitchDegrees)
        {
            if (rawPitchDegrees > 180f)
            {
                rawPitchDegrees -= 360f;
            }

            return rawPitchDegrees;
        }

        private void EnsureCollisionConfiguration()
        {
            bool legacyCollisionConfigurationMissing =
                collisionRadius <= 0f &&
                collisionHeight <= 0f &&
                collisionSkinWidth <= 0f &&
                collisionMask.value == 0;

            if (legacyCollisionConfigurationMissing)
            {
                usePhysicsCollision = true;
                collisionRadius = DefaultCollisionRadius;
                collisionHeight = DefaultCollisionHeight;
                collisionSkinWidth = DefaultCollisionSkinWidth;
                collisionMask = ~0;
                return;
            }

            if (collisionRadius <= 0f)
            {
                collisionRadius = DefaultCollisionRadius;
            }

            if (collisionHeight <= 0f)
            {
                collisionHeight = DefaultCollisionHeight;
            }

            if (collisionSkinWidth <= 0f)
            {
                collisionSkinWidth = DefaultCollisionSkinWidth;
            }

            if (usePhysicsCollision && collisionMask.value == 0)
            {
                collisionMask = ~0;
            }
        }
    }
}
