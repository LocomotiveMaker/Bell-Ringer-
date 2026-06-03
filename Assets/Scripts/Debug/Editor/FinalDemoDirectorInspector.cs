using BellRinger.FinalDemo;
using UnityEditor;
using UnityEngine;

namespace BellRinger.Debug.Editor
{
    [CustomEditor(typeof(FinalDemoDirector))]
    public sealed class FinalDemoDirectorInspector : UnityEditor.Editor
    {
        private bool _showCueLibrary = true;
        private bool _showBellTiming = true;
        private bool _showPadShake = true;
        private bool _showLed = true;
        private bool _showWallsProgress = true;
        private bool _showTinnitus = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FinalDemoDirector director = (FinalDemoDirector)target;
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("FinalDemoRoot Quick Tuning", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "아래 항목은 FinalDemoRoot에서 FinalDemoCueLibrary와 FinalDemoTuningProfile의 핵심 값을 바로 확인/수정하기 위한 요약입니다.",
                MessageType.Info);

            DrawCueLibrarySection(director.CueLibrary);
            DrawTuningSections(director.TuningProfile);
        }

        private void DrawCueLibrarySection(FinalDemoCueLibrary cueLibrary)
        {
            _showCueLibrary = EditorGUILayout.BeginFoldoutHeaderGroup(_showCueLibrary, "오디오 Cue 목록 / Clip / Loop");
            if (_showCueLibrary)
            {
                if (cueLibrary == null)
                {
                    EditorGUILayout.HelpBox("FinalDemoCueLibrary가 연결되어 있지 않습니다.", MessageType.Warning);
                }
                else
                {
                    SerializedObject cueObject = new SerializedObject(cueLibrary);
                    cueObject.Update();
                    SerializedProperty cues = cueObject.FindProperty("cues");
                    int missingClipCount = CountMissingClips(cues);
                    EditorGUILayout.ObjectField("Cue Library Asset", cueLibrary, typeof(FinalDemoCueLibrary), false);
                    EditorGUILayout.LabelField("Cue Count", cues != null ? cues.arraySize.ToString() : "0");
                    EditorGUILayout.LabelField("Missing Main Clips", missingClipCount.ToString());

                    if (cues != null)
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < cues.arraySize; i++)
                        {
                            SerializedProperty entry = cues.GetArrayElementAtIndex(i);
                            DrawCueEntry(entry, i);
                        }
                        EditorGUI.indentLevel--;
                    }

                    if (cueObject.ApplyModifiedProperties())
                    {
                        EditorUtility.SetDirty(cueLibrary);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTuningSections(FinalDemoTuningProfile profile)
        {
            if (profile == null)
            {
                EditorGUILayout.HelpBox("FinalDemoTuningProfile이 연결되어 있지 않습니다.", MessageType.Warning);
                return;
            }

            SerializedObject tuningObject = new SerializedObject(profile);
            tuningObject.Update();
            EditorGUILayout.ObjectField("Tuning Profile Asset", profile, typeof(FinalDemoTuningProfile), false);

            _showBellTiming = EditorGUILayout.BeginFoldoutHeaderGroup(_showBellTiming, "종 주기 / Follow / Gaze");
            if (_showBellTiming)
            {
                EditorGUILayout.HelpBox("Follow 종소리 주기는 현재 고정 주기가 아니라 첫 주기 -> 최소 주기까지 감소하는 규칙을 사용합니다.", MessageType.None);
                DrawTuningProperty(tuningObject, "bellFollowCallIntervalSeconds", "Follow 고정 주기 (현재 미사용/호환)");
                DrawTuningProperty(tuningObject, "bellOrbitCallIntervalSeconds", "초기 회전 종소리 주기");
                DrawTuningProperty(tuningObject, "bellFollowInitialCallIntervalSeconds", "Follow 첫 종소리 주기");
                DrawTuningProperty(tuningObject, "bellFollowMinimumCallIntervalSeconds", "Follow 최소 주기");
                DrawTuningProperty(tuningObject, "bellFollowMissIntervalReductionSeconds", "못 찾을 때 주기 감소");
                DrawTuningProperty(tuningObject, "bellCallPostClipGapSeconds", "종 Clip 이후 최소 여백");
                DrawTuningProperty(tuningObject, "bellAssistTimeoutSeconds", "자동 Assist 시작 시간");
                DrawTuningProperty(tuningObject, "bellAssistRepeatSeconds", "자동 Assist 반복 주기");
                DrawTuningProperty(tuningObject, "bellFollowAutomaticStrongAssistSound", "BellStrongAssist 자동 반복음");
                DrawTuningProperty(tuningObject, "bellGazeCallIntervalSeconds", "종 바라보기 종소리 주기");
                DrawTuningProperty(tuningObject, "forestBellCallIntervalSeconds", "숲 종소리 주기");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showPadShake = EditorGUILayout.BeginFoldoutHeaderGroup(_showPadShake, "패드 흔들기 / Bell Assist");
            if (_showPadShake)
            {
                EditorGUILayout.HelpBox("Follow 1/2에서는 이동한 종 위치에서 울리고, BellAcquisition 이후에는 사용자 중앙의 손 안 종 위치에서 울립니다.", MessageType.None);
                DrawTuningProperty(tuningObject, "padShakeAssistMotionThreshold", "흔들림 감지 기준");
                DrawTuningProperty(tuningObject, "padShakeAssistCooldownSeconds", "흔들기 종소리 쿨다운");
                DrawTuningProperty(tuningObject, "padShakeAssistNarrationCooldownSeconds", "흔들기 안내 나레이션 쿨다운");
                DrawTuningProperty(tuningObject, "bellAssistGainMultiplier", "흔들기/Assist 볼륨·LED 증폭");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showLed = EditorGUILayout.BeginFoldoutHeaderGroup(_showLed, "LED / 소리 반응");
            if (_showLed)
            {
                DrawTuningProperty(tuningObject, "soundReactiveLedEnabled", "소리 파형 LED 반응");
                DrawTuningProperty(tuningObject, "soundReactiveLedUpdateIntervalSeconds", "LED 반응 업데이트 간격");
                DrawTuningProperty(tuningObject, "bellSoundReactiveSensitivity", "종 파형 민감도");
                DrawTuningProperty(tuningObject, "tinnitusSoundReactiveSensitivity", "이명 파형 민감도");
                DrawTuningProperty(tuningObject, "bellFollowLedIntensity", "Follow 종 LED");
                DrawTuningProperty(tuningObject, "bellGazeLedIntensity", "종 바라보기 LED");
                DrawTuningProperty(tuningObject, "generalTinnitusLedIntensity", "일반 이명 LED");
                DrawTuningProperty(tuningObject, "bossMassLedIntensity", "보스 이명 LED");
                DrawTuningProperty(tuningObject, "rainMaxIntensity", "비 최대 광량");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showWallsProgress = EditorGUILayout.BeginFoldoutHeaderGroup(_showWallsProgress, "벽 / 진행 조건");
            if (_showWallsProgress)
            {
                DrawTuningProperty(tuningObject, "bellArrivalRadius", "종 도착 반경");
                DrawTuningProperty(tuningObject, "bellFollowProgressBlockerEnabled", "종 너머 진행 방지");
                DrawTuningProperty(tuningObject, "bellFollowBlockerMarginMeters", "종 너머 진행 여유");
                DrawTuningProperty(tuningObject, "generalTinnitusApproachRadius", "일반 이명 접근 반경");
                DrawTuningProperty(tuningObject, "bossApproachRadius", "보스 이명 접근 반경");
                DrawTuningProperty(tuningObject, "forestBellArrivalRadius", "숲 종 도착 반경");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showTinnitus = EditorGUILayout.BeginFoldoutHeaderGroup(_showTinnitus, "이명 / 보스 이명");
            if (_showTinnitus)
            {
                DrawTuningProperty(tuningObject, "generalTinnitusTreatmentSeconds", "일반 이명 치료 시간");
                DrawTuningProperty(tuningObject, "generalTinnitusPositionToleranceMeters", "일반 이명 위치 허용");
                DrawTuningProperty(tuningObject, "generalTinnitusRotationToleranceDegrees", "일반 이명 회전 허용");
                DrawTuningProperty(tuningObject, "generalTinnitusLightIntervalSeconds", "일반 이명 LED 주기");
                DrawTuningProperty(tuningObject, "bossPatternOneSeconds", "보스 1단계 시간");
                DrawTuningProperty(tuningObject, "bossPatternTwoSeconds", "보스 2단계 시간");
                DrawTuningProperty(tuningObject, "bossPatternThreeSeconds", "보스 3단계 시간");
                DrawTuningProperty(tuningObject, "bossOpeningHoldSeconds", "보스 초기 고정 시간");
                DrawTuningProperty(tuningObject, "bossMovingToleranceMeters", "보스 이동 위치 허용");
                DrawTuningProperty(tuningObject, "bossLightIntervalSeconds", "보스 LED 주기");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (tuningObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(profile);
            }
        }

        private static void DrawCueEntry(SerializedProperty entry, int index)
        {
            SerializedProperty id = entry.FindPropertyRelative("id");
            SerializedProperty operatorLabel = entry.FindPropertyRelative("operatorLabel");
            SerializedProperty audioClip = entry.FindPropertyRelative("audioClip");
            SerializedProperty bus = entry.FindPropertyRelative("bus");
            SerializedProperty defaultVolume = entry.FindPropertyRelative("defaultVolume");
            SerializedProperty loop = entry.FindPropertyRelative("loop");
            SerializedProperty spatialized = entry.FindPropertyRelative("spatialized");
            SerializedProperty minDistance = entry.FindPropertyRelative("minDistance");
            SerializedProperty maxDistance = entry.FindPropertyRelative("maxDistance");

            string idLabel = id != null && id.enumValueIndex >= 0 && id.enumValueIndex < id.enumDisplayNames.Length
                ? id.enumDisplayNames[id.enumValueIndex]
                : $"Cue {index + 1}";
            string label = operatorLabel != null && !string.IsNullOrWhiteSpace(operatorLabel.stringValue)
                ? $"{idLabel} / {operatorLabel.stringValue}"
                : idLabel;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{index + 1:00}. {label}", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(audioClip, new GUIContent("Clip"));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(bus, new GUIContent("Bus"));
            EditorGUILayout.PropertyField(defaultVolume, new GUIContent("Volume"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(loop, new GUIContent("Loop"));
            EditorGUILayout.PropertyField(spatialized, new GUIContent("Spatialized"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(minDistance, new GUIContent("Min Distance"));
            EditorGUILayout.PropertyField(maxDistance, new GUIContent("Max Distance"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void DrawTuningProperty(SerializedObject tuningObject, string propertyName, string label)
        {
            SerializedProperty property = tuningObject.FindProperty(propertyName);
            if (property == null)
            {
                EditorGUILayout.LabelField(label, $"Missing property: {propertyName}");
                return;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }

        private static int CountMissingClips(SerializedProperty cues)
        {
            if (cues == null)
            {
                return 0;
            }

            int missing = 0;
            for (int i = 0; i < cues.arraySize; i++)
            {
                SerializedProperty entry = cues.GetArrayElementAtIndex(i);
                SerializedProperty clip = entry.FindPropertyRelative("audioClip");
                if (clip == null || clip.objectReferenceValue == null)
                {
                    missing++;
                }
            }

            return missing;
        }
    }
}
