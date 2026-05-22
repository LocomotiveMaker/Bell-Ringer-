using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoOperatorControls : MonoBehaviour
    {
        [SerializeField] private FinalDemoDirector director;
        [SerializeField] private FinalDemoInputStatus inputStatus;
        [SerializeField] private bool showOperatorControls = true;
        [SerializeField] private bool showInputStatus = true;
        [SerializeField] private Vector2 operatorPanelPosition = new Vector2(10f, 10f);
        [SerializeField] private Vector2 statusPanelSize = new Vector2(455f, 430f);

        private Rect _operatorRect = new Rect(10f, 10f, 430f, 420f);
        private Rect _statusRect = new Rect(0f, 10f, 455f, 430f);
        private Vector2 _statusScroll;

        public void Initialize(FinalDemoDirector newDirector, FinalDemoInputStatus newInputStatus)
        {
            director = newDirector;
            inputStatus = newInputStatus;
        }

        private void Awake()
        {
            director ??= GetComponent<FinalDemoDirector>() ?? FindFirstObjectByType<FinalDemoDirector>();
            inputStatus ??= GetComponent<FinalDemoInputStatus>() ?? FindFirstObjectByType<FinalDemoInputStatus>();
        }

        private void OnGUI()
        {
            if (director == null)
            {
                return;
            }

            _operatorRect.x = operatorPanelPosition.x;
            _operatorRect.y = operatorPanelPosition.y;
            if (showOperatorControls)
            {
                _operatorRect = GUI.Window(7101, _operatorRect, DrawOperatorWindow, "Final Demo Operator");
            }

            if (showInputStatus)
            {
                _statusRect.width = statusPanelSize.x;
                _statusRect.height = statusPanelSize.y;
                _statusRect.x = Mathf.Max(10f, Screen.width - _statusRect.width - 10f);
                _statusRect.y = 10f;
                _statusRect = GUI.Window(7102, _statusRect, DrawStatusWindow, "Final Demo Input Status");
            }
        }

        private void DrawOperatorWindow(int windowId)
        {
            GUILayout.Label(director.BuildStageSummary());
            GUILayout.Label($"Elapsed {director.StageElapsedSeconds:0.00}s");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Demo"))
            {
                director.StartDemo();
            }

            if (GUILayout.Button("Force Next"))
            {
                director.ForceNextStage();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Complete Current"))
            {
                director.ForceCompleteCurrentObjective();
            }

            if (GUILayout.Button("Reset Stage"))
            {
                director.ResetCurrentStage();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset To Preflight"))
            {
                director.ResetToPreflight();
            }

            if (GUILayout.Button(director.PlayerMovementLocked ? "Unlock Movement" : "Lock Movement"))
            {
                director.ToggleMovementLock();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Recenter Head"))
            {
                director.RecenterHead();
            }

            if (GUILayout.Button("Recenter Pad"))
            {
                director.RecenterPad();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Assist: {director.AssistLevel}"))
            {
                director.CycleAssistLevel();
            }

            if (GUILayout.Button($"HRTF Preview: {(director.HrtfPreviewEnabled ? "On" : "Off")}"))
            {
                director.ToggleHrtfPreview();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Stop All Outputs"))
            {
                director.StopAllOutputs();
            }

            GUILayout.Space(8f);
            GUILayout.Label("Bundle 1 QA:");
            GUILayout.Label("1. Preflight -> Start Demo.");
            GUILayout.Label("2. Force Next until Complete.");
            GUILayout.Label("3. Lock/Unlock movement.");
            GUILayout.Label("4. Recenter head/pad and check status freshness.");

            GUI.DragWindow();
        }

        private void DrawStatusWindow(int windowId)
        {
            inputStatus ??= FindFirstObjectByType<FinalDemoInputStatus>();
            string statusText = inputStatus != null ? inputStatus.BuildStatusText(director) : "No FinalDemoInputStatus found.";
            _statusScroll = GUILayout.BeginScrollView(_statusScroll);
            GUILayout.TextArea(statusText, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
    }
}
