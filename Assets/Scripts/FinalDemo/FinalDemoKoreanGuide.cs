using UnityEngine;

namespace BellRinger.FinalDemo
{
#pragma warning disable 0414
    [DisallowMultipleComponent]
    public sealed class FinalDemoKoreanGuide : MonoBehaviour
    {
        [Header("\uAC1C\uC694")]
        [SerializeField, TextArea(2, 5)]
        private string overview = "FinalDemoRoot \uC548\uB0B4\uC785\uB2C8\uB2E4. \uC8FC\uC694 \uC870\uC815\uC740 FinalDemoTuningProfile, FinalDemoCueLibrary, WorldAudio_FinalDemo, WorldLight_FinalDemo, FinalDemoModelPresenter\uC5D0\uC11C \uD569\uB2C8\uB2E4.";

        [Header("\uC624\uB514\uC624")]
        [SerializeField, TextArea(2, 4)]
        private string audioGuide = "\uC885, \uBE44, \uC774\uBA85, \uC232 \uC18C\uB9AC\uB294 FinalDemoCueLibrary\uC5D0\uC11C \uC5F0\uACB0\uD569\uB2C8\uB2E4. \uC624\uD504\uB2DD \uC570\uBE44\uC5B8\uD2B8\uB294 \uBCF4\uC2A4 \uCC98\uCE58 \uC804\uAE4C\uC9C0 \uC720\uC9C0\uB418\uACE0, \uC232 \uC9C4\uC785 \uB54C ForestBed\uB85C \uC804\uD658\uD569\uB2C8\uB2E4.";

        [Header("LED")]
        [SerializeField, TextArea(2, 4)]
        private string led = "\uC885\uC740 \uCD08\uB85D, \uD328\uB4DC\uB294 \uC8FC\uD669/\uB178\uB791, \uBE44\uB294 \uB525\uBE14\uB8E8, \uC774\uBA85\uC740 \uBCF4\uB77C \uACC4\uC5F4\uC785\uB2C8\uB2E4. \uC2E4\uC81C \uAE30\uD310 \uCD9C\uB825\uACFC \uD654\uBA74 16x8 \uD504\uB9AC\uBDF0\uB294 FinalDemoLightRouter \uAE30\uC900\uC73C\uB85C \uB3D9\uC791\uD569\uB2C8\uB2E4.";

        [Header("\uC885 \uACBD\uB85C")]
        [SerializeField, TextArea(2, 4)]
        private string bellRoute = "\uCD08\uAE30 \uC885 \uD68C\uC804\uC740 Bell Orbit Local Points \uB610\uB294 BellOrbitPath_Authoring\uC744 \uBD05\uB2C8\uB2E4. 1, 2\uBC88\uC9F8 \uC885 \uC774\uB3D9 \uC704\uCE58\uB294 \uAE30\uC874 authoring \uAC12\uACFC tuning profile \uAC12\uC744 \uC720\uC9C0\uD569\uB2C8\uB2E4.";

        [Header("\uD328\uB4DC \uD754\uB4E4\uAE30")]
        [SerializeField, TextArea(2, 4)]
        private string padShake = "\uD328\uB4DC \uD754\uB4E4\uAE30 \uC885\uC18C\uB9AC \uAE30\uC900\uC740 Pad Shake \uC139\uC158\uC758 motion threshold, cooldown, narration cooldown\uC785\uB2C8\uB2E4.";

        [Header("\uBE44/\uC570\uBE44\uC5B8\uD2B8")]
        [SerializeField, TextArea(2, 4)]
        private string rainAmbience = "\uBE44\uB294 BellFollowRain \uC9C4\uC785 \uC2DC \uBC14\uB85C \uC2DC\uC791\uB418\uACE0 3\uCD08 \uD398\uC774\uB4DC\uC778\uD569\uB2C8\uB2E4. \uC2DC\uAC01 \uBE44\uB294 FinalDemoWorldRainEnvironment, \uBC14\uB2E5\uC740 FinalDemoWorldFloorSurface\uAC00 \uB2F4\uB2F9\uD569\uB2C8\uB2E4.";

        [Header("\uC774\uBA85")]
        [SerializeField, TextArea(2, 4)]
        private string tinnitus = "\uC774\uBA85\uACFC \uBCF4\uC2A4 \uC774\uBA85\uC740 \uC808\uCC28 \uACE0\uC8FC\uD30C, \uAE00\uB9AC\uCE58 \uB8E8\uD504, LED \uD328\uD134, \uC624\uBE0C\uC81D\uD2B8 \uAE00\uB9AC\uCE58\uAC00 \uAC19\uC740 \uC6D4\uB4DC \uC704\uCE58\uB97C \uAE30\uC900\uC73C\uB85C \uB3D9\uC791\uD569\uB2C8\uB2E4.";

        [Header("\uBAA8\uB378/\uD658\uACBD")]
        [SerializeField, TextArea(2, 4)]
        private string model = "\uC885/\uD328\uB4DC \uBAA8\uB378\uC740 FinalDemoModelPresenter\uC5D0\uC11C \uAD50\uCCB4\uD569\uB2C8\uB2E4. \uC232\uC740 FinalDemoForestEnvironment\uAC00 \uB2F4\uB2F9\uD558\uBA70, Asset Store \uD328\uD0A4\uC9C0 \uC784\uD3EC\uD2B8 \uC804\uC5D0\uB294 \uC548\uC815\uC801\uC778 \uC808\uCC28\uD615 \uB098\uBB34\uB97C \uAE30\uBCF8\uC73C\uB85C \uC501\uB2C8\uB2E4.";
    }
#pragma warning restore 0414
}
