using BellRinger.Gameplay;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoAuthoringCapture : MonoBehaviour
    {
        [SerializeField] private FinalDemoSceneReferences sceneReferences;
        [SerializeField] private PadPoseProvider padPoseProvider;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private bool showCapturePanel = true;
        [SerializeField] private int selectedPathIndex;
        [SerializeField] private int selectedWaypointIndex;

        private string _lastCaptureStatus = "No capture yet.";
        private readonly string[] _pathLabels =
        {
            "Bell Follow",
            "Bell Gaze",
            "Boss Path 1",
            "Boss Path 2",
            "Boss Path 3",
        };

        private Rect _panelRect = new Rect(590f, 18f, 430f, 430f);

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();
            if (Input.GetKeyDown(KeyCode.F5))
            {
                CapturePose(sceneReferences != null ? sceneReferences.TinnitusOneHealMarker : null);
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                CapturePose(sceneReferences != null ? sceneReferences.TinnitusTwoHealMarker : null);
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                CapturePose(sceneReferences != null ? sceneReferences.BossPoseOneMarker : null);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                CapturePose(sceneReferences != null ? sceneReferences.BossPoseTwoMarker : null);
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                CapturePose(sceneReferences != null ? sceneReferences.BossPoseThreeMarker : null);
            }
        }

        private void OnGUI()
        {
            if (!showCapturePanel)
            {
                return;
            }

            _panelRect = GUI.Window(7104, _panelRect, DrawWindow, "Authoring Capture");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label("Pose hotkeys: F5/F6 Tinnitus, F7/F8/F9 Boss.");
            GUILayout.Label("Press a capture button/key while the pad is exactly at the desired answer pose.");
            GUILayout.Label(BuildPadSummary());
            GUILayout.Label(_lastCaptureStatus);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture Tinnitus A"))
            {
                CapturePose(sceneReferences != null ? sceneReferences.TinnitusOneHealMarker : null);
            }

            if (GUILayout.Button("Capture Tinnitus B"))
            {
                CapturePose(sceneReferences != null ? sceneReferences.TinnitusTwoHealMarker : null);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture Boss 1"))
            {
                CapturePose(sceneReferences != null ? sceneReferences.BossPoseOneMarker : null);
            }

            if (GUILayout.Button("Capture Boss 2"))
            {
                CapturePose(sceneReferences != null ? sceneReferences.BossPoseTwoMarker : null);
            }

            if (GUILayout.Button("Capture Boss 3"))
            {
                CapturePose(sceneReferences != null ? sceneReferences.BossPoseThreeMarker : null);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("General tinnitus uses captured position + yaw/pitch/roll.");
            GUILayout.Label("Boss currently uses path waypoint position only; boss pose captures are kept for debug/future use.");
            DrawMarkerSummary("Tinnitus A target", sceneReferences != null ? sceneReferences.TinnitusOneHealMarker : null);
            DrawMarkerSummary("Tinnitus B target", sceneReferences != null ? sceneReferences.TinnitusTwoHealMarker : null);
            DrawMarkerSummary("Boss 1 pose", sceneReferences != null ? sceneReferences.BossPoseOneMarker : null);

            GUILayout.Space(6f);
            GUILayout.Label("Waypoint capture uses current pad camera-space position converted through player camera.");
            selectedPathIndex = Mathf.Clamp(GUILayout.Toolbar(selectedPathIndex, _pathLabels), 0, _pathLabels.Length - 1);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-"))
            {
                selectedWaypointIndex = Mathf.Max(0, selectedWaypointIndex - 1);
            }

            GUILayout.Label($"Waypoint {selectedWaypointIndex + 1}", GUILayout.Width(120f));
            if (GUILayout.Button("+"))
            {
                selectedWaypointIndex++;
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Capture Selected Waypoint From Pad Position"))
            {
                CaptureSelectedWaypoint();
            }

            GUILayout.Label("Play Mode captures apply immediately. For permanent scene authoring, repeat in Edit Mode or copy values before stopping.");
            GUI.DragWindow();
        }

        private void CapturePose(FinalDemoPoseAuthoringMarker marker)
        {
            ResolveReferences();
            if (marker == null || padPoseProvider == null)
            {
                _lastCaptureStatus = marker == null
                    ? "Capture failed: target marker is missing."
                    : "Capture failed: PadPoseProvider is missing.";
                return;
            }

            if (!padPoseProvider.HasFreshPosition || !padPoseProvider.HasResolvedRotation)
            {
                _lastCaptureStatus = "Capture failed: pad position/rotation is not fresh.";
                return;
            }

            marker.CaptureFrom(padPoseProvider);
            Vector3 position = marker.TargetCameraSpacePosition;
            Vector3 ypr = marker.TargetYawPitchRollDegrees;
            _lastCaptureStatus = $"Captured {marker.name}: pos {position.x:0.000}, {position.y:0.000}, {position.z:0.000}  y/p/r {ypr.x:0.0}, {ypr.y:0.0}, {ypr.z:0.0}";
            MarkDirty(marker);
        }

        private void CaptureSelectedWaypoint()
        {
            ResolveReferences();
            FinalDemoAuthoringPath path = ResolveSelectedPath();
            if (path == null || padPoseProvider == null || playerCamera == null)
            {
                _lastCaptureStatus = "Waypoint capture failed: path, pad, or player camera missing.";
                return;
            }

            Vector3 worldPosition = playerCamera.transform.TransformPoint(padPoseProvider.CameraSpacePosition);
            if (path.TrySetWorldPoint(selectedWaypointIndex, worldPosition))
            {
                _lastCaptureStatus = $"Captured waypoint {selectedWaypointIndex + 1} on {_pathLabels[selectedPathIndex]}: {worldPosition.x:0.00}, {worldPosition.y:0.00}, {worldPosition.z:0.00}";
                MarkDirty(path);
            }
        }

        private FinalDemoAuthoringPath ResolveSelectedPath()
        {
            if (sceneReferences == null)
            {
                return null;
            }

            return selectedPathIndex switch
            {
                1 => sceneReferences.BellGazeAuthoringPath,
                2 => sceneReferences.BossWeakpointPathOneAuthoring,
                3 => sceneReferences.BossWeakpointPathTwoAuthoring,
                4 => sceneReferences.BossWeakpointPathThreeAuthoring,
                _ => sceneReferences.BellFollowAuthoringPath,
            };
        }

        private string BuildPadSummary()
        {
            if (padPoseProvider == null)
            {
                return "PadPoseProvider: missing";
            }

            Vector3 position = padPoseProvider.CameraSpacePosition;
            return $"Pad pos {position.x:0.000}, {position.y:0.000}, {position.z:0.000}  y/p/r {padPoseProvider.ResolvedYawDegrees:0.0}, {padPoseProvider.ResolvedPitchDegrees:0.0}, {padPoseProvider.ResolvedRollDegrees:0.0}";
        }

        private static void DrawMarkerSummary(string label, FinalDemoPoseAuthoringMarker marker)
        {
            if (marker == null)
            {
                GUILayout.Label($"{label}: missing");
                return;
            }

            Vector3 position = marker.TargetCameraSpacePosition;
            Vector3 ypr = marker.TargetYawPitchRollDegrees;
            GUILayout.Label($"{label}: pos {position.x:0.00}, {position.y:0.00}, {position.z:0.00}  y/p/r {ypr.x:0}, {ypr.y:0}, {ypr.z:0}");
        }

        private void ResolveReferences()
        {
            sceneReferences ??= FindFirstObjectByType<FinalDemoSceneReferences>();
            padPoseProvider ??= sceneReferences != null ? sceneReferences.PadPoseProvider : FindFirstObjectByType<PadPoseProvider>();
            playerCamera ??= sceneReferences != null ? sceneReferences.PlayerCamera : Camera.main;
        }

        private static void MarkDirty(Object target)
        {
#if UNITY_EDITOR
            if (target != null)
            {
                UnityEditor.EditorUtility.SetDirty(target);
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }
            }
#endif
        }
    }
}
