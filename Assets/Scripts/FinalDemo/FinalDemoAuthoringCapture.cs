using BellRinger.Gameplay;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoAuthoringCapture : MonoBehaviour
    {
        [SerializeField] private FinalDemoDirector director;
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
        };

        private Rect _panelRect = new Rect(590f, 18f, 430f, 560f);

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

            if (Input.GetKeyDown(KeyCode.F7))
            {
                CaptureBossPose(0);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                CaptureBossPose(1);
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                CaptureBossPose(2);
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
            GUILayout.Label("Pose hotkeys: F5 Tinnitus A. Boss points: F7 Boss1, F8 Boss2, F9 Boss3.");
            GUILayout.Label("Press a capture button/key while the pad is exactly at the desired answer pose.");
            GUILayout.Label(BuildPadSummary());
            GUILayout.Label(_lastCaptureStatus);

            if (GUILayout.Button("Capture Tinnitus A"))
            {
                CapturePose(sceneReferences != null ? sceneReferences.TinnitusOneHealMarker : null);
            }

            GUILayout.Label("General tinnitus uses captured position + yaw/pitch/roll.");
            GUILayout.Label("Tinnitus B is skipped in FinalDemo; first tinnitus goes directly to boss.");
            GUILayout.Label("Boss now uses three fixed answer points. Rotation is ignored.");
            DrawMarkerSummary("Tinnitus A target", sceneReferences != null ? sceneReferences.TinnitusOneHealMarker : null);

            GUILayout.Space(6f);
            DrawBossCaptureRow("Boss 1", 0);
            DrawBossCaptureRow("Boss 2", 1);
            DrawBossCaptureRow("Boss 3", 2);
            DrawMarkerSummary("Boss 1 target", sceneReferences != null ? sceneReferences.BossPoseOneMarker : null);
            DrawMarkerSummary("Boss 2 target", sceneReferences != null ? sceneReferences.BossPoseTwoMarker : null);
            DrawMarkerSummary("Boss 3 target", sceneReferences != null ? sceneReferences.BossPoseThreeMarker : null);

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
            Vector3 position = marker.StoredTargetCameraSpacePosition;
            Vector3 ypr = marker.StoredTargetYawPitchRollDegrees;
            bool appliedNow = director != null && director.TryApplyCapturedGeneralTinnitusTarget(marker);
            _lastCaptureStatus = $"Captured {marker.name}: pos {position.x:0.000}, {position.y:0.000}, {position.z:0.000}  y/p/r {ypr.x:0.0}, {ypr.y:0.0}, {ypr.z:0.0}{(appliedNow ? "  applied now" : string.Empty)}";
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

        private void CaptureBossPose(int patternIndex)
        {
            ResolveReferences();
            FinalDemoPoseAuthoringMarker marker = ResolveBossMarker(patternIndex);
            if (marker == null || padPoseProvider == null)
            {
                _lastCaptureStatus = marker == null
                    ? "Boss point capture failed: marker is missing."
                    : "Boss point capture failed: PadPoseProvider is missing.";
                return;
            }

            if (!padPoseProvider.HasFreshPosition || !padPoseProvider.HasResolvedRotation)
            {
                _lastCaptureStatus = "Boss point capture failed: pad position/rotation is not fresh.";
                return;
            }

            marker.CaptureFrom(padPoseProvider);
            Vector3 position = marker.StoredTargetCameraSpacePosition;
            _lastCaptureStatus = $"Captured Boss {patternIndex + 1}: pos {position.x:0.000}, {position.y:0.000}, {position.z:0.000}  (rotation captured but ignored in boss match)";
            MarkDirty(marker);
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
                _ => sceneReferences.BellFollowAuthoringPath,
            };
        }

        private FinalDemoPoseAuthoringMarker ResolveBossMarker(int patternIndex)
        {
            if (sceneReferences == null)
            {
                return null;
            }

            return patternIndex switch
            {
                1 => sceneReferences.BossPoseTwoMarker,
                2 => sceneReferences.BossPoseThreeMarker,
                _ => sceneReferences.BossPoseOneMarker,
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

            Vector3 position = marker.StoredTargetCameraSpacePosition;
            Vector3 ypr = marker.StoredTargetYawPitchRollDegrees;
            GUILayout.Label($"{label}: pos {position.x:0.00}, {position.y:0.00}, {position.z:0.00}  y/p/r {ypr.x:0}, {ypr.y:0}, {ypr.z:0}");
        }

        private void DrawBossCaptureRow(string label, int patternIndex)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(62f));
            if (GUILayout.Button("Capture Point"))
            {
                CaptureBossPose(patternIndex);
            }
            GUILayout.EndHorizontal();
        }

        private void ResolveReferences()
        {
            director ??= FindFirstObjectByType<FinalDemoDirector>();
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
