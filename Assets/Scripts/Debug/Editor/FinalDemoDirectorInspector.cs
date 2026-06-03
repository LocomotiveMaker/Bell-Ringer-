using BellRinger.FinalDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BellRinger.Debug.Editor
{
    [CustomEditor(typeof(FinalDemoDirector))]
    public sealed class FinalDemoDirectorInspector : UnityEditor.Editor
    {
        private bool _showCueLibrary = true;
        private bool _showBellTiming = true;
        private bool _showPadShake = true;
        private bool _showRangeAuthoring = true;
        private bool _showRangeCurve = true;
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
                "This section exposes the cue library and the tuning profile values most often changed during final-demo QA.",
                MessageType.Info);

            DrawCueLibrarySection(director.CueLibrary);
            DrawRangeAuthoringSection(director);
            DrawTuningSections(director.TuningProfile);
        }

        private void DrawCueLibrarySection(FinalDemoCueLibrary cueLibrary)
        {
            _showCueLibrary = EditorGUILayout.BeginFoldoutHeaderGroup(_showCueLibrary, "Audio Cue List / Clips / Loop Settings");
            if (_showCueLibrary)
            {
                if (cueLibrary == null)
                {
                    EditorGUILayout.HelpBox("FinalDemoCueLibrary is not assigned.", MessageType.Warning);
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
                            DrawCueEntry(cues.GetArrayElementAtIndex(i), i);
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

        private void DrawRangeAuthoringSection(FinalDemoDirector director)
        {
            _showRangeAuthoring = EditorGUILayout.BeginFoldoutHeaderGroup(_showRangeAuthoring, "Scene Range Circles: Sound + LED");
            if (_showRangeAuthoring)
            {
                EditorGUILayout.HelpBox(
                    "Create or refresh five circular authoring objects. Their radius controls both audio loudness and LED brightness for the same source.",
                    MessageType.None);
                if (GUILayout.Button("Create / Refresh Range Circles In Scene", GUILayout.Height(28f)))
                {
                    CreateOrRefreshRangeObjects(director);
                }

                SerializedObject directorObject = serializedObject;
                DrawSerializedProperty(directorObject, "bellFollowOneRange", "Bell follow 1 range");
                DrawSerializedProperty(directorObject, "bellFollowTwoRange", "Bell follow 2 range");
                DrawSerializedProperty(directorObject, "tinnitusOneRange", "Tinnitus 1 range");
                DrawSerializedProperty(directorObject, "tinnitusTwoRange", "Tinnitus 2 range");
                DrawSerializedProperty(directorObject, "bossTinnitusRange", "Boss tinnitus range");
                if (directorObject.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(director);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTuningSections(FinalDemoTuningProfile profile)
        {
            if (profile == null)
            {
                EditorGUILayout.HelpBox("FinalDemoTuningProfile is not assigned.", MessageType.Warning);
                return;
            }

            SerializedObject tuningObject = new SerializedObject(profile);
            tuningObject.Update();
            EditorGUILayout.ObjectField("Tuning Profile Asset", profile, typeof(FinalDemoTuningProfile), false);

            _showBellTiming = EditorGUILayout.BeginFoldoutHeaderGroup(_showBellTiming, "Bell Timing / Follow / Gaze");
            if (_showBellTiming)
            {
                DrawTuningProperty(tuningObject, "bellFollowInitialCallIntervalSeconds", "Follow first bell interval");
                DrawTuningProperty(tuningObject, "bellFollowMinimumCallIntervalSeconds", "Follow minimum interval");
                DrawTuningProperty(tuningObject, "bellFollowMissIntervalReductionSeconds", "Follow interval reduction per missed call");
                DrawTuningProperty(tuningObject, "bellFollowNearIntervalReduction", "Follow near interval reduction");
                DrawTuningProperty(tuningObject, "bellCallPostClipGapSeconds", "Minimum post-clip gap");
                DrawTuningProperty(tuningObject, "bellAssistTimeoutSeconds", "Automatic assist start seconds");
                DrawTuningProperty(tuningObject, "bellAssistRepeatSeconds", "Automatic assist repeat seconds");
                DrawTuningProperty(tuningObject, "bellFollowAutomaticStrongAssistSound", "Enable automatic BellStrongAssist");
                DrawTuningProperty(tuningObject, "bellOrbitCallIntervalSeconds", "Opening orbit bell interval");
                DrawTuningProperty(tuningObject, "bellGazeCallIntervalSeconds", "Bell gaze call interval");
                DrawTuningProperty(tuningObject, "forestBellCallIntervalSeconds", "Forest bell interval");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showPadShake = EditorGUILayout.BeginFoldoutHeaderGroup(_showPadShake, "Pad Shake Bell Assist");
            if (_showPadShake)
            {
                EditorGUILayout.HelpBox("Pad shake bell assist is enabled only during BellFollowOne and BellFollowRain.", MessageType.None);
                DrawTuningProperty(tuningObject, "padShakeAssistMotionThreshold", "Shake detection threshold");
                DrawTuningProperty(tuningObject, "padShakeAssistCooldownSeconds", "Shake bell cooldown");
                DrawTuningProperty(tuningObject, "padShakeAssistNarrationCooldownSeconds", "Shake narration cooldown");
                DrawTuningProperty(tuningObject, "bellAssistGainMultiplier", "Shake/assist volume and LED gain");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showRangeCurve = EditorGUILayout.BeginFoldoutHeaderGroup(_showRangeCurve, "Shared Sound + LED Range Curve");
            if (_showRangeCurve)
            {
                DrawTuningProperty(tuningObject, "soundLightRangeResponseCurve", "Shared response curve");
                DrawTuningProperty(tuningObject, "defaultBellFollowSoundLightRadius", "Default bell follow radius");
                DrawTuningProperty(tuningObject, "defaultTinnitusSoundLightRadius", "Default tinnitus radius");
                DrawTuningProperty(tuningObject, "defaultBossSoundLightRadius", "Default boss radius");
                DrawTuningProperty(tuningObject, "bellRangeMinimumLedScale", "Bell LED floor inside range");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showLed = EditorGUILayout.BeginFoldoutHeaderGroup(_showLed, "LED / Sound Reactive");
            if (_showLed)
            {
                DrawTuningProperty(tuningObject, "soundReactiveLedEnabled", "Sound-reactive LED");
                DrawTuningProperty(tuningObject, "soundReactiveLedUpdateIntervalSeconds", "LED update interval");
                DrawTuningProperty(tuningObject, "bellSoundReactiveSensitivity", "Bell waveform sensitivity");
                DrawTuningProperty(tuningObject, "tinnitusSoundReactiveSensitivity", "Tinnitus waveform sensitivity");
                DrawTuningProperty(tuningObject, "bellOrbitSilentLightMultiplier", "Opening silent bell light multiplier");
                DrawTuningProperty(tuningObject, "bellOrbitWaveLightMultiplier", "Opening waveform bell light multiplier");
                DrawTuningProperty(tuningObject, "bellFollowLedIntensity", "Follow bell LED");
                DrawTuningProperty(tuningObject, "bellGazeLedIntensity", "Bell gaze LED");
                DrawTuningProperty(tuningObject, "generalTinnitusLedIntensity", "General tinnitus LED");
                DrawTuningProperty(tuningObject, "bossMassLedIntensity", "Boss tinnitus LED");
                DrawTuningProperty(tuningObject, "rainMaxIntensity", "Rain max LED/audio scale");
                DrawTuningProperty(tuningObject, "rainIntensityRampSeconds", "Rain ramp seconds");
                DrawTuningProperty(tuningObject, "rainAudioGainMultiplier", "Rain audio gain");
                DrawTuningProperty(tuningObject, "rainArrivalAudioFadeSeconds", "Rain arrival audio fade");
                DrawTuningProperty(tuningObject, "rainArrivalLightFadeSeconds", "Rain arrival LED fade");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showWallsProgress = EditorGUILayout.BeginFoldoutHeaderGroup(_showWallsProgress, "Walls / Progress Conditions");
            if (_showWallsProgress)
            {
                DrawTuningProperty(tuningObject, "bellArrivalRadius", "Bell arrival radius");
                DrawTuningProperty(tuningObject, "bellFollowProgressBlockerEnabled", "Prevent walking past bell");
                DrawTuningProperty(tuningObject, "bellFollowBlockerMarginMeters", "Bell blocker margin");
                DrawTuningProperty(tuningObject, "generalTinnitusApproachRadius", "Fallback general tinnitus approach radius");
                DrawTuningProperty(tuningObject, "bossApproachRadius", "Fallback boss approach radius");
                DrawTuningProperty(tuningObject, "forestBellArrivalRadius", "Forest bell arrival radius");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showTinnitus = EditorGUILayout.BeginFoldoutHeaderGroup(_showTinnitus, "Tinnitus / Boss Tinnitus");
            if (_showTinnitus)
            {
                DrawTuningProperty(tuningObject, "generalTinnitusTreatmentSeconds", "General tinnitus treatment seconds");
                DrawTuningProperty(tuningObject, "generalTinnitusPositionToleranceMeters", "General tinnitus position tolerance");
                DrawTuningProperty(tuningObject, "generalTinnitusRotationToleranceDegrees", "General tinnitus rotation tolerance");
                DrawTuningProperty(tuningObject, "generalTinnitusLightIntervalSeconds", "General tinnitus LED interval");
                DrawTuningProperty(tuningObject, "finalDemoProceduralTinnitusEnabled", "Enable generated tinnitus tone in FinalDemo");
                DrawTuningProperty(tuningObject, "tinnitusMatchToneEnabled", "Enable match feedback tone");
                DrawTuningProperty(tuningObject, "tinnitusMatchToneVolume", "Match feedback tone volume");
                DrawTuningProperty(tuningObject, "tinnitusPositionToneFrequencyRange", "Position sine frequency range");
                DrawTuningProperty(tuningObject, "tinnitusRotationToneFrequencyRange", "Rotation square frequency range");
                DrawTuningProperty(tuningObject, "tinnitusRotationToneVolumeMultiplier", "Rotation square volume multiplier");
                DrawTuningProperty(tuningObject, "tinnitusPadBellFeedbackEnabled", "Enable pad bell while cleansing");
                DrawTuningProperty(tuningObject, "tinnitusPadBellBaseIntervalSeconds", "Pad bell base interval");
                DrawTuningProperty(tuningObject, "tinnitusPadBellVolume", "Pad bell volume");
                DrawTuningProperty(tuningObject, "tinnitusPadBellFarSpeed", "Pad bell far speed");
                DrawTuningProperty(tuningObject, "tinnitusPadBellNearSpeed", "Pad bell near speed");
                DrawTuningProperty(tuningObject, "bossPatternOneSeconds", "Boss pattern 1 seconds");
                DrawTuningProperty(tuningObject, "bossPatternTwoSeconds", "Boss pattern 2 seconds");
                DrawTuningProperty(tuningObject, "bossPatternThreeSeconds", "Boss pattern 3 seconds");
                DrawTuningProperty(tuningObject, "bossOpeningHoldSeconds", "Boss opening hold seconds");
                DrawTuningProperty(tuningObject, "bossMovingToleranceMeters", "Boss moving position tolerance");
                DrawTuningProperty(tuningObject, "bossLightIntervalSeconds", "Boss LED interval");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (tuningObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(profile);
            }
        }

        private static void CreateOrRefreshRangeObjects(FinalDemoDirector director)
        {
            if (director == null)
            {
                return;
            }

            FinalDemoTuningProfile profile = director.TuningProfile;
            FinalDemoSceneReferences references = director.SceneReferences;
            Transform parent = references != null && references.WorldRoot != null
                ? references.WorldRoot
                : director.transform;

            Vector3 bellOne = profile != null ? profile.BellFollowTargetOnePosition : new Vector3(-1.4f, 1.5f, 3.2f);
            Vector3 bellTwo = profile != null ? profile.BellFollowTargetTwoPosition : new Vector3(1.4f, 1.5f, 3.6f);
            if (references != null && references.BellFollowAuthoringPath != null)
            {
                if (references.BellFollowAuthoringPath.TryGetWorldPoint(0, out Vector3 authoredOne))
                {
                    bellOne = authoredOne;
                }

                if (references.BellFollowAuthoringPath.TryGetWorldPoint(1, out Vector3 authoredTwo))
                {
                    bellTwo = authoredTwo;
                }
            }

            Vector3 tinnitusOne = references != null && references.TinnitusOneVisual != null
                ? references.TinnitusOneVisual.position
                : profile != null ? profile.GeneralTinnitusOneWorldPosition : new Vector3(-1.2f, 1.45f, 2.8f);
            Vector3 tinnitusTwo = references != null && references.TinnitusTwoVisual != null
                ? references.TinnitusTwoVisual.position
                : profile != null ? profile.GeneralTinnitusTwoWorldPosition : new Vector3(1.25f, 1.35f, 3.1f);
            Vector3 boss = references != null && references.BossVisual != null
                ? references.BossVisual.position
                : profile != null ? profile.BossWorldPosition : new Vector3(0f, 1.6f, 4.2f);

            SerializedObject directorObject = new SerializedObject(director);
            directorObject.Update();
            AssignRange(directorObject, "bellFollowOneRange", "Range_BellFollow_01", "Bell follow 1 sound+LED", bellOne, profile != null ? profile.DefaultBellFollowSoundLightRadius : 4.2f, new Color(0.1f, 1f, 0.2f, 0.9f), parent);
            AssignRange(directorObject, "bellFollowTwoRange", "Range_BellFollow_02", "Bell follow 2 sound+LED", bellTwo, profile != null ? profile.DefaultBellFollowSoundLightRadius : 4.2f, new Color(0.1f, 1f, 0.2f, 0.9f), parent);
            AssignRange(directorObject, "tinnitusOneRange", "Range_Tinnitus_01", "Tinnitus 1 sound+LED", tinnitusOne, profile != null ? profile.DefaultTinnitusSoundLightRadius : 2.2f, new Color(0.55f, 0.1f, 1f, 0.9f), parent);
            AssignRange(directorObject, "tinnitusTwoRange", "Range_Tinnitus_02", "Tinnitus 2 sound+LED", tinnitusTwo, profile != null ? profile.DefaultTinnitusSoundLightRadius : 2.2f, new Color(0.55f, 0.1f, 1f, 0.9f), parent);
            AssignRange(directorObject, "bossTinnitusRange", "Range_BossTinnitus", "Boss tinnitus sound+LED", boss, profile != null ? profile.DefaultBossSoundLightRadius : 2.4f, new Color(1f, 0.1f, 0.25f, 0.9f), parent);
            directorObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        }

        private static void AssignRange(
            SerializedObject directorObject,
            string propertyName,
            string objectName,
            string label,
            Vector3 position,
            float radius,
            Color color,
            Transform parent)
        {
            FinalDemoRangeAuthoring range = FindOrCreateRange(objectName, parent);
            Undo.RecordObject(range.gameObject, $"Refresh {objectName}");
            range.transform.position = position;
            range.RadiusMeters = radius;
            range.Label = label;
            range.GizmoColor = color;
            EditorUtility.SetDirty(range);
            SerializedProperty property = directorObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = range;
            }
        }

        private static FinalDemoRangeAuthoring FindOrCreateRange(string objectName, Transform parent)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing == null)
            {
                existing = new GameObject(objectName);
                Undo.RegisterCreatedObjectUndo(existing, $"Create {objectName}");
            }

            if (parent != null && existing.transform.parent == null)
            {
                Undo.SetTransformParent(existing.transform, parent, $"Parent {objectName}");
            }

            FinalDemoRangeAuthoring range = existing.GetComponent<FinalDemoRangeAuthoring>();
            if (range == null)
            {
                range = Undo.AddComponent<FinalDemoRangeAuthoring>(existing);
            }

            return range;
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
            DrawSerializedProperty(tuningObject, propertyName, label);
        }

        private static void DrawSerializedProperty(SerializedObject serializedObject, string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
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
