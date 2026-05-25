using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoStageVisibility : MonoBehaviour
    {
        [SerializeField] private FinalDemoDirector director;
        [SerializeField] private GameObject padRoot;
        [SerializeField] private GameObject bellRoot;
        [SerializeField] private GameObject rainRoot;
        [SerializeField] private GameObject tinnitusRoot;
        [SerializeField] private GameObject bossRoot;
        [SerializeField] private GameObject forestRoot;
        [SerializeField] private GameObject darkSkyRoot;
        [SerializeField] private GameObject clearSkyRoot;
        [SerializeField] private bool keepAllVisibleInEditMode = true;

        private void OnValidate()
        {
            if (!Application.isPlaying && keepAllVisibleInEditMode)
            {
                SetActiveIfAssigned(padRoot, true);
                SetActiveIfAssigned(bellRoot, true);
                SetActiveIfAssigned(rainRoot, true);
                SetActiveIfAssigned(tinnitusRoot, true);
                SetActiveIfAssigned(bossRoot, true);
                SetActiveIfAssigned(forestRoot, true);
                SetActiveIfAssigned(darkSkyRoot, true);
                SetActiveIfAssigned(clearSkyRoot, true);
            }
        }

        private void Awake()
        {
            director ??= FindFirstObjectByType<FinalDemoDirector>();
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            FinalDemoStage stage = director != null ? director.CurrentStage : FinalDemoStage.Preflight;
            bool bellVisible = stage == FinalDemoStage.OpeningCloseBell ||
                               stage == FinalDemoStage.BellOrbit ||
                               stage == FinalDemoStage.BellFollowOne ||
                               stage == FinalDemoStage.BellFollowRain ||
                               stage == FinalDemoStage.BellGaze ||
                               stage == FinalDemoStage.BellAcquisition ||
                               stage == FinalDemoStage.ForestEnding ||
                               stage == FinalDemoStage.Complete;
            bool rainVisible = stage == FinalDemoStage.BellFollowRain || stage == FinalDemoStage.BellGaze;
            bool tinnitusVisible = stage == FinalDemoStage.GeneralTinnitusOne || stage == FinalDemoStage.GeneralTinnitusTwo;
            bool bossVisible = stage == FinalDemoStage.BossApproach ||
                               stage == FinalDemoStage.BossPatternOne ||
                               stage == FinalDemoStage.BossPatternTwo ||
                               stage == FinalDemoStage.BossPatternThree ||
                               stage == FinalDemoStage.BossDefeat;
            bool forestVisible = stage == FinalDemoStage.ForestEnding || stage == FinalDemoStage.Complete;

            SetActiveIfAssigned(padRoot, true);
            SetActiveIfAssigned(bellRoot, bellVisible || !Application.isPlaying);
            SetActiveIfAssigned(rainRoot, rainVisible || !Application.isPlaying);
            SetActiveIfAssigned(tinnitusRoot, tinnitusVisible || !Application.isPlaying);
            SetActiveIfAssigned(bossRoot, bossVisible || !Application.isPlaying);
            SetActiveIfAssigned(forestRoot, forestVisible || !Application.isPlaying);
            SetActiveIfAssigned(darkSkyRoot, !forestVisible || !Application.isPlaying);
            SetActiveIfAssigned(clearSkyRoot, forestVisible || !Application.isPlaying);
        }

        private static void SetActiveIfAssigned(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
