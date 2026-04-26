using System;

namespace BellRinger.Audio
{
    [Serializable]
    public struct BellRingerLedDotFrame
    {
        public int x;
        public int y;
        public float brightnessNormalized;

        public BellRingerLedDotFrame(int x, int y, float brightnessNormalized)
        {
            this.x = x;
            this.y = y;
            this.brightnessNormalized = brightnessNormalized;
        }
    }
}
