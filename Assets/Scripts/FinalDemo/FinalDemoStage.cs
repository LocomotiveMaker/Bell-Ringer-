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
        OpeningAmbience = 1,
        OpeningCloseBell = 2,
        BellOrbit = 3,
        BellFollowBell = 4,
        RainWindMasking = 5,
        BellGazeLoop = 6,
        BellAcquisition = 7,
        GeneralTinnitus = 8,
        TinnitusResolve = 9,
        BossTinnitus = 10,
        BossResolve = 11,
        ForestEnding = 12,
    }
}
