using System;

namespace BellRinger.Audio
{
    [Serializable]
    public struct BellRingerLedDotFrame
    {
        public int x;
        public int y;
        public float centerX;
        public float centerY;
        public float brightnessNormalized;

        public BellRingerLedDotFrame(int x, int y, float brightnessNormalized)
            : this(x, y, x, y, brightnessNormalized)
        {
        }

        public BellRingerLedDotFrame(int x, int y, float centerX, float centerY, float brightnessNormalized)
        {
            this.x = x;
            this.y = y;
            this.centerX = centerX;
            this.centerY = centerY;
            this.brightnessNormalized = brightnessNormalized;
        }
    }
}
