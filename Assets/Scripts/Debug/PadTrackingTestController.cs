using BellRinger.Gameplay;
using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Debug
{
    public sealed class PadTrackingTestController : MonoBehaviour
    {
        [SerializeField] private bool showRuntimeOverlay = true;
        [SerializeField] private Color trackedColor = new Color(0.08f, 0.92f, 0.72f, 1f);
        [SerializeField] private Color staleColor = new Color(1f, 0.72f, 0.16f, 1f);
        [SerializeField] private Color imuFreshColor = new Color(0.72f, 0.92f, 1f, 1f);
        [SerializeField] private Color imuStaleColor = new Color(0.56f, 0.58f, 0.64f, 1f);
        [SerializeField] private Color imuPreviewColor = new Color(0.98f, 0.62f, 0.18f, 1f);
        [SerializeField] private Vector3 imuPreviewAnchor = new Vector3(-0.48f, -0.08f, 0.92f);
        [SerializeField] private float imuPreviewShakeDistance = 0.025f;

        private Camera _camera;
        private PadTrackingReceiver _receiver;
        private PadImuReceiver _imuReceiver;
        private Renderer _ghostRenderer;
        private Transform _ghostTransform;
        private Transform _orientationMarkerTransform;
        private Renderer _orientationMarkerRenderer;
        private Transform _imuPreviewRootTransform;
        private Transform _imuPreviewPlateTransform;
        private Renderer _imuPreviewPlateRenderer;
        private Transform _imuPreviewMarkerTransform;
        private bool _cameraInitialized;
        private bool _staticSceneCreated;

        private void Start()
        {
            EnsureScene();
        }

        private void Update()
        {
            EnsureScene();
            UpdateGhost();

            if (_camera != null)
            {
                UpdateImuPreview(_camera.transform);
            }
        }

        private void OnGUI()
        {
            if (!showRuntimeOverlay || _receiver == null)
            {
                return;
            }

            DrawOverlay(new Rect(16f, 16f, 620f, 680f));
            DrawScreenPreview(new Rect(652f, 16f, 300f, 220f));
        }

        private void EnsureScene()
        {
            _camera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (_camera == null)
            {
                GameObject cameraObject = new GameObject("PadTrackingTestCamera");
                _camera = cameraObject.AddComponent<Camera>();
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(0.025f, 0.03f, 0.042f, 1f);
                cameraObject.tag = "MainCamera";
            }

            if (!_cameraInitialized)
            {
                _camera.transform.position = new Vector3(0f, 1.55f, -0.2f);
                _camera.transform.rotation = Quaternion.identity;
                _cameraInitialized = true;
            }

            if (_camera.GetComponent<AudioListener>() == null)
            {
                _camera.gameObject.AddComponent<AudioListener>();
            }

            if (_camera.GetComponent<BellRingerSimpleMoveLookController>() == null)
            {
                _camera.gameObject.AddComponent<BellRingerSimpleMoveLookController>();
            }

            if (_receiver == null)
            {
                _receiver = GetComponent<PadTrackingReceiver>();
                if (_receiver == null)
                {
                    _receiver = gameObject.AddComponent<PadTrackingReceiver>();
                }
            }

            if (_imuReceiver == null)
            {
                _imuReceiver = GetComponent<PadImuReceiver>();
                if (_imuReceiver == null)
                {
                    _imuReceiver = gameObject.AddComponent<PadImuReceiver>();
                }
            }

            if (_staticSceneCreated)
            {
                return;
            }

            CreateFloor();
            CreateReferenceObjects();
            CreateLight();
            _staticSceneCreated = true;
        }

        private void UpdateGhost()
        {
            if (_camera == null || _receiver == null || _ghostTransform == null || _ghostRenderer == null || !_receiver.HasPose)
            {
                return;
            }

            Vector3 cameraSpace = _receiver.ApproximateCameraSpacePosition;
            Transform cameraTransform = _camera.transform;
            _ghostTransform.position = cameraTransform.position
                + (cameraTransform.right * cameraSpace.x)
                + (cameraTransform.up * cameraSpace.y)
                + (cameraTransform.forward * cameraSpace.z);

            if (_imuReceiver != null && _imuReceiver.HasFreshSample)
            {
                _ghostTransform.rotation = cameraTransform.rotation * _imuReceiver.RelativeRotation;
            }
            else
            {
                _ghostTransform.rotation = Quaternion.LookRotation(cameraTransform.forward, Vector3.up);
            }

            _ghostRenderer.sharedMaterial.color = _receiver.HasFreshDetection ? trackedColor : staleColor;

            if (_orientationMarkerTransform != null && _orientationMarkerRenderer != null)
            {
                _orientationMarkerTransform.position = _ghostTransform.position + (_ghostTransform.forward * 0.11f);
                _orientationMarkerRenderer.sharedMaterial.color = _imuReceiver != null && _imuReceiver.HasFreshSample ? imuFreshColor : imuStaleColor;
            }

        }

        private void DrawOverlay(Rect rect)
        {
            GUILayout.BeginArea(rect, "Pad Tracking Test", GUI.skin.window);
            GUILayout.Label("1. Make the phone appear as a Windows webcam.");
            GUILayout.Label("2. Run tools/Run-PadTracker.ps1.");
            GUILayout.Label("3. Hold the V-board by hand and move it in front of the camera.");
            GUILayout.Space(8f);
            GUILayout.Label($"UDP port: {_receiver.ListenPort}  packet fresh: {_receiver.HasFreshPacket}  detection fresh: {_receiver.HasFreshDetection}");
            GUILayout.Label($"Markers: {_receiver.MarkerIds}  count: {_receiver.MarkerCount}  confidence: {_receiver.Confidence:0.00}");
            GUILayout.Label($"Frame: {_receiver.FrameWidth} x {_receiver.FrameHeight}  FPS: {_receiver.FramesPerSecond:0.0}");
            GUILayout.Label($"Screen center: {_receiver.ScreenX01:0.000}, {_receiver.ScreenY01:0.000}");
            GUILayout.Label($"Approx camera-space meters: X {_receiver.ApproximateCameraSpacePosition.x:0.000}  Y {_receiver.ApproximateCameraSpacePosition.y:0.000}  Z {_receiver.ApproximateCameraSpacePosition.z:0.000}");
            GUILayout.Label($"Marker size px: {_receiver.MarkerSizePixels:0.0}  packet age: {_receiver.LastPacketAgeSeconds:0.000}s");
            GUILayout.Space(10f);

            if (_imuReceiver != null)
            {
                string ports = _imuReceiver.AvailablePorts == null || _imuReceiver.AvailablePorts.Length == 0
                    ? "<none>"
                    : string.Join(", ", _imuReceiver.AvailablePorts);
                string imuSource = _imuReceiver.UsingSharedHardwareBridgeTelemetry ? "Shared HardwareBridge" : "Dedicated Serial";

                GUILayout.Label($"IMU source: {imuSource}");
                GUILayout.Label($"IMU port: {_imuReceiver.ActivePortName}  connected: {_imuReceiver.IsConnected}  fresh: {_imuReceiver.HasFreshSample}");
                GUILayout.Label($"Available COM: {ports}");
                GUILayout.Label($"IMU raw yaw/pitch/roll: {_imuReceiver.YawDegrees:0.00} / {_imuReceiver.PitchDegrees:0.00} / {_imuReceiver.RollDegrees:0.00}");
                GUILayout.Label($"IMU mapped yaw/pitch/roll: {_imuReceiver.MappedYawDegrees:0.00} / {_imuReceiver.MappedPitchDegrees:0.00} / {_imuReceiver.MappedRollDegrees:0.00}");
                GUILayout.Label($"IMU sample age: {_imuReceiver.LastSampleAgeSeconds:0.000}s  baud: {_imuReceiver.ActiveBaudRate}");
                GUILayout.Label($"IMU motion: {_imuReceiver.MotionIntensity01:0.00}");
                GUILayout.Label($"IMU quaternion: {_imuReceiver.HasQuaternionTelemetry}");
                GUILayout.Label($"IMU last line: {_imuReceiver.LastRawLine}");
                GUILayout.Space(6f);
                if (_imuReceiver.HasQuaternionTelemetry)
                {
                    Vector3 trim = _imuReceiver.LocalRotationTrimEuler;
                    GUILayout.Label($"Mount trim XYZ: {trim.x:0} / {trim.y:0} / {trim.z:0}");
                    DrawTrimButtonRow("Trim X", _imuReceiver.RotateTrimXPositive, _imuReceiver.RotateTrimXNegative);
                    DrawTrimButtonRow("Trim Y", _imuReceiver.RotateTrimYPositive, _imuReceiver.RotateTrimYNegative);
                    DrawTrimButtonRow("Trim Z", _imuReceiver.RotateTrimZPositive, _imuReceiver.RotateTrimZNegative);

                    if (GUILayout.Button("Reset Mount Trim", GUILayout.Height(24f)))
                    {
                        _imuReceiver.ResetMountTrim();
                    }
                }
                else
                {
                    GUILayout.Label("Axis Mapping");
                    DrawAxisMappingRow("Yaw", _imuReceiver.YawAxisLabel, _imuReceiver.CycleYawAxis, _imuReceiver.ToggleYawInvert);
                    DrawAxisMappingRow("Pitch", _imuReceiver.PitchAxisLabel, _imuReceiver.CyclePitchAxis, _imuReceiver.TogglePitchInvert);
                    DrawAxisMappingRow("Roll", _imuReceiver.RollAxisLabel, _imuReceiver.CycleRollAxis, _imuReceiver.ToggleRollInvert);

                    if (GUILayout.Button("Reset Axis Mapping", GUILayout.Height(24f)))
                    {
                        _imuReceiver.ResetAxisMapping();
                    }
                }

                if (GUILayout.Button("Reconnect IMU", GUILayout.Height(28f)))
                {
                    _imuReceiver.RefreshAndReconnect();
                }

                if (GUILayout.Button("Recenter IMU", GUILayout.Height(28f)))
                {
                    _imuReceiver.Recenter();
                }

                if (!string.IsNullOrWhiteSpace(_imuReceiver.LastError))
                {
                    GUILayout.Label($"IMU error: {_imuReceiver.LastError}");
                }

                GUILayout.Label("If you change preferredPortName in the Inspector during Play, press Reconnect IMU.");
                GUILayout.Label("Orange preview rectangle = IMU-only visualizer. Shake grows with faster movement.");
                GUILayout.Label("Quaternion mode uses Mount Trim buttons instead of Euler axis remapping.");
            }

            if (!string.IsNullOrWhiteSpace(_receiver.LastError))
            {
                GUILayout.Space(8f);
                GUILayout.Label($"Receiver error: {_receiver.LastError}");
            }

            GUILayout.Space(8f);
            GUILayout.Label("Ghost color: cyan = live detection, amber = last held pose.");
            GUILayout.Label("Front sphere: pale blue = fresh IMU rotation, gray = no fresh IMU sample.");
            GUILayout.EndArea();
        }

        private void DrawScreenPreview(Rect rect)
        {
            GUILayout.BeginArea(rect, "Camera Screen Preview", GUI.skin.window);
            Rect preview = new Rect(16f, 40f, 268f, 150f);
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.DrawTexture(preview, Texture2D.whiteTexture);

            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            GUI.DrawTexture(new Rect(preview.x + (preview.width * 0.5f) - 1f, preview.y, 2f, preview.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(preview.x, preview.y + (preview.height * 0.5f) - 1f, preview.width, 2f), Texture2D.whiteTexture);

            float dotX = preview.x + Mathf.Clamp01(_receiver.ScreenX01) * preview.width;
            float dotY = preview.y + Mathf.Clamp01(_receiver.ScreenY01) * preview.height;
            float dotSize = Mathf.Lerp(12f, 24f, Mathf.Clamp01(_receiver.Confidence));
            GUI.color = _receiver.HasFreshDetection ? trackedColor : staleColor;
            GUI.DrawTexture(new Rect(dotX - (dotSize * 0.5f), dotY - (dotSize * 0.5f), dotSize, dotSize), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(16f, 196f, 260f, 20f), "Dot is the tracker center in camera space.");
            GUILayout.EndArea();
        }

        private void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "PadTrackingFloor";
            floor.transform.localScale = new Vector3(1.6f, 1f, 1.6f);
            floor.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(new Color(0.05f, 0.055f, 0.07f, 1f));
        }

        private void CreateReferenceObjects()
        {
            GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "TrackedPadGhost";
            ghost.transform.localScale = new Vector3(0.24f, 0.035f, 0.13f);
            _ghostTransform = ghost.transform;
            _ghostRenderer = ghost.GetComponent<Renderer>();
            _ghostRenderer.sharedMaterial = CreateRuntimeMaterial(staleColor);

            GameObject orientationMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orientationMarker.name = "TrackedPadOrientationMarker";
            orientationMarker.transform.localScale = Vector3.one * 0.032f;
            _orientationMarkerTransform = orientationMarker.transform;
            _orientationMarkerRenderer = orientationMarker.GetComponent<Renderer>();
            _orientationMarkerRenderer.sharedMaterial = CreateRuntimeMaterial(imuStaleColor);

            GameObject previewRoot = new GameObject("ImuPreviewRoot");
            _imuPreviewRootTransform = previewRoot.transform;

            GameObject previewPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            previewPlate.name = "ImuPreviewPlate";
            previewPlate.transform.SetParent(_imuPreviewRootTransform, false);
            previewPlate.transform.localScale = new Vector3(0.28f, 0.018f, 0.16f);
            _imuPreviewPlateTransform = previewPlate.transform;
            _imuPreviewPlateRenderer = previewPlate.GetComponent<Renderer>();
            _imuPreviewPlateRenderer.sharedMaterial = CreateRuntimeMaterial(imuPreviewColor);

            GameObject previewMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            previewMarker.name = "ImuPreviewMarker";
            previewMarker.transform.SetParent(_imuPreviewPlateTransform, false);
            previewMarker.transform.localPosition = new Vector3(0f, 0.035f, 0.055f);
            previewMarker.transform.localScale = new Vector3(0.045f, 0.04f, 0.03f);
            previewMarker.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(Color.white);
            _imuPreviewMarkerTransform = previewMarker.transform;

            GameObject centerMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            centerMarker.name = "NeutralReference";
            centerMarker.transform.position = new Vector3(0f, 1.55f, 0.9f);
            centerMarker.transform.localScale = Vector3.one * 0.08f;
            centerMarker.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(new Color(0.45f, 0.48f, 0.56f, 1f));
        }

        private void UpdateImuPreview(Transform cameraTransform)
        {
            if (_imuPreviewRootTransform == null || _imuPreviewPlateTransform == null || _imuReceiver == null)
            {
                return;
            }

            float shake = _imuReceiver.MotionIntensity01;
            float time = Time.realtimeSinceStartup;
            Vector3 basePosition = cameraTransform.position
                + (cameraTransform.right * imuPreviewAnchor.x)
                + (cameraTransform.up * imuPreviewAnchor.y)
                + (cameraTransform.forward * imuPreviewAnchor.z);
            Vector3 shakeOffset =
                (cameraTransform.right * Mathf.Sin(time * 19f) * imuPreviewShakeDistance * shake) +
                (cameraTransform.up * Mathf.Cos(time * 23f) * imuPreviewShakeDistance * 0.7f * shake);

            _imuPreviewRootTransform.position = basePosition + shakeOffset;
            _imuPreviewRootTransform.rotation = cameraTransform.rotation * (_imuReceiver.HasFreshSample ? _imuReceiver.RelativeRotation : Quaternion.identity);
            _imuPreviewPlateTransform.localScale = new Vector3(0.28f + (shake * 0.02f), 0.018f, 0.16f + (shake * 0.01f));

            if (_imuPreviewPlateRenderer != null)
            {
                _imuPreviewPlateRenderer.sharedMaterial.color = _imuReceiver.HasFreshSample ? imuPreviewColor : imuStaleColor;
            }

            if (_imuPreviewMarkerTransform != null)
            {
                _imuPreviewMarkerTransform.localPosition = new Vector3(0f, 0.035f + (shake * 0.01f), 0.055f);
            }
        }

        private static void DrawAxisMappingRow(string label, string axisLabel, System.Action onCycleAxis, System.Action onToggleInvert)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", GUILayout.Width(48f));
            if (GUILayout.Button(axisLabel, GUILayout.Width(96f)))
            {
                onCycleAxis?.Invoke();
            }

            if (GUILayout.Button("Flip", GUILayout.Width(64f)))
            {
                onToggleInvert?.Invoke();
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawTrimButtonRow(string label, System.Action onPositive90, System.Action onNegative90)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", GUILayout.Width(64f));
            if (GUILayout.Button("+90", GUILayout.Width(64f)))
            {
                onPositive90?.Invoke();
            }

            if (GUILayout.Button("-90", GUILayout.Width(64f)))
            {
                onNegative90?.Invoke();
            }

            GUILayout.EndHorizontal();
        }

        private void CreateLight()
        {
            GameObject lightObject = new GameObject("PadTrackingKeyLight");
            lightObject.transform.rotation = Quaternion.Euler(46f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
        }

        private static Material CreateRuntimeMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            return material;
        }
    }
}
