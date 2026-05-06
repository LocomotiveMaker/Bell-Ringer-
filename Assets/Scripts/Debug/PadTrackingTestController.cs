using BellRinger.Gameplay;
using UnityEngine;

namespace BellRinger.Debug
{
    public sealed class PadTrackingTestController : MonoBehaviour
    {
        [SerializeField] private bool showRuntimeOverlay = true;
        [SerializeField] private Color trackedColor = new Color(0.08f, 0.92f, 0.72f, 1f);
        [SerializeField] private Color staleColor = new Color(1f, 0.72f, 0.16f, 1f);

        private Camera _camera;
        private PadTrackingReceiver _receiver;
        private Renderer _ghostRenderer;
        private Transform _ghostTransform;
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
        }

        private void OnGUI()
        {
            if (!showRuntimeOverlay || _receiver == null)
            {
                return;
            }

            DrawOverlay(new Rect(16f, 16f, 520f, 370f));
            DrawScreenPreview(new Rect(552f, 16f, 300f, 220f));
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
            _ghostTransform.rotation = Quaternion.LookRotation(cameraTransform.forward, Vector3.up);
            _ghostRenderer.sharedMaterial.color = _receiver.HasFreshDetection ? trackedColor : staleColor;
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

            if (!string.IsNullOrWhiteSpace(_receiver.LastError))
            {
                GUILayout.Space(8f);
                GUILayout.Label($"Receiver error: {_receiver.LastError}");
            }

            GUILayout.Space(8f);
            GUILayout.Label("Ghost color: cyan = live detection, amber = last held pose.");
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

            GameObject centerMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            centerMarker.name = "NeutralReference";
            centerMarker.transform.position = new Vector3(0f, 1.55f, 0.9f);
            centerMarker.transform.localScale = Vector3.one * 0.08f;
            centerMarker.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(new Color(0.45f, 0.48f, 0.56f, 1f));
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
