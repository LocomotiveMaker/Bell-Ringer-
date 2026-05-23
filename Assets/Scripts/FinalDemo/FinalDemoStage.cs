namespace BellRinger.FinalDemo
{
    public enum FinalDemoStage
    {
        Preflight = 0,
        OpeningAmbience = 1,
        OpeningSilence = 2,
        OpeningCloseBell = 3,
        BellOrbit = 4,
        BellFollowOne = 5,
        BellFollowRain = 6,
        BellGaze = 7,
        BellAcquisition = 8,
        GeneralTinnitusOne = 9,
        GeneralTinnitusTwo = 10,
        BossApproach = 11,
        BossPatternOne = 12,
        BossPatternTwo = 13,
        BossPatternThree = 14,
        BossDefeat = 15,
        ForestEnding = 16,
        Complete = 17,
    }

    public enum FinalDemoAssistLevel
    {
        Normal = 0,
        Gentle = 1,
        MaxSafety = 2,
    }

    public enum FinalDemoCueId
    {
        None = 0,
        BellOpeningOrbit = 1,
        BellMovementTexture = 2,
        BellDistantCall = 3,
        BellStrongAssist = 4,
        BellPadShakeResponse = 5,
        BellGazeSuccess = 6,
        BellAcquisition = 7,
        RainLightBed = 8,
        RainCloseDrops = 9,
        RainStrongBed = 10,
        NarrFollowBell = 11,
        NarrLookBell = 12,
        TinnitusLongGlitch = 13,
        TinnitusBurst = 14,
        TinnitusPoseLock = 15,
        TinnitusPoseLost = 16,
        TinnitusHealingLoop = 17,
        TinnitusResolve = 18,
        BossBasePulse = 19,
        BossGlitchBurst = 20,
        BossWeakpointMove = 21,
        BossHit = 22,
        BossDefeatRise = 23,
        BossDefeatAir = 24,
        TransitionSoftCut = 25,
        ForestBed = 26,
        ForestBell = 27,
        NarrPadShakeAssist = 28,
        NarrFindTinnitusPose = 29,
        NarrHoldPose = 30,
        NarrBossTrack = 31,
    }

    public enum FinalDemoAudioBus
    {
        Master = 0,
        Bell = 1,
        RainWind = 2,
        Tinnitus = 3,
        BossTinnitus = 4,
        Ambience = 5,
        Interaction = 6,
        Narration = 7,
    }

    public enum FinalDemoLightBinding
    {
        None = 0,
        Bell = 1,
        Rain = 2,
        Tinnitus = 3,
        BossTinnitus = 4,
        WallNoise = 5,
        PadFeedback = 6,
    }

    public enum FinalDemoHapticBinding
    {
        None = 0,
        BellAssist = 1,
        BellAcquisition = 2,
        TinnitusApproach = 3,
        TinnitusLock = 4,
        TinnitusCleanse = 5,
        BossTracking = 6,
        BossFailure = 7,
        BossHit = 8,
        EnvironmentalDrop = 9,
    }

    public enum FinalDemoFeedbackPriority
    {
        Rain = 10,
        WallNoise = 20,
        Tinnitus = 30,
        Bell = 40,
        CriticalPad = 50,
    }
}
