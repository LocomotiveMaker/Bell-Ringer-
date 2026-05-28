using UnityEngine;

namespace BellRinger.FinalDemo
{
#pragma warning disable 0414
    [DisallowMultipleComponent]
    public sealed class FinalDemoKoreanGuide : MonoBehaviour
    {
        [Header("개요")]
        [SerializeField, TextArea(2, 5)]
        private string overview = "FinalDemoRoot 안내입니다. 실제 조정 수치는 FinalDemoTuningProfile, WorldAudio_FinalDemo, WorldLight_FinalDemo, FinalDemoModelPresenter에서 섹션별로 조절합니다.";

        [Header("오디오")]
        [SerializeField, TextArea(2, 4)]
        private string audioGuide = "종, 비, 이명 소리는 FinalDemoCueLibrary에서 연결합니다. 볼륨, 거리감, HRTF는 WorldAudio_FinalDemo의 FinalDemoAudioRouter와 FinalDemoTuningProfile을 함께 봅니다.";

        [Header("LED")]
        [SerializeField, TextArea(2, 4)]
        private string led = "종, 비, 이명 LED 밝기와 하드웨어 출력은 WorldLight_FinalDemo의 FinalDemoLightRouter에서 조절합니다. 종 광량은 bell 계열 boost와 maximum brightness를 우선 확인합니다.";

        [Header("종 경로")]
        [SerializeField, TextArea(2, 4)]
        private string bellRoute = "초기 종 회전 경로는 FinalDemoTuningProfile의 Bell Orbit Local Points를 봅니다. 실제 추적 구간은 BellFollowPath_Authoring 또는 Bell Follow Target 값을 봅니다.";

        [Header("패드 흔들기")]
        [SerializeField, TextArea(2, 4)]
        private string padShake = "패드 흔들기 종소리 재생 기준은 Pad Shake 섹션의 motion threshold, cooldown, narration cooldown입니다.";

        [Header("비/앰비언트")]
        [SerializeField, TextArea(2, 4)]
        private string rainAmbience = "오프닝 앰비언트는 OpeningAmbienceBed, 비는 RainLightBed/RainStrongBed를 사용합니다. 레인존 진입 후 Rain Intensity Ramp Seconds 동안 페이드인됩니다.";

        [Header("이명")]
        [SerializeField, TextArea(2, 4)]
        private string tinnitus = "이명은 절차음과 글리치 클립이 같은 월드 위치에서 재생됩니다. HRTF/software binaural은 FinalDemoDirector에서 기본 ON입니다.";

        [Header("모델")]
        [SerializeField, TextArea(2, 4)]
        private string model = "종/패드 모델 교체와 크기 보정은 FinalDemoModelPresenter에서 합니다. 파이널데모 씬에만 적용됩니다.";
    }
#pragma warning restore 0414
}
