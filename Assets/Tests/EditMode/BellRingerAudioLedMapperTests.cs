using BellRinger.Audio;
using NUnit.Framework;
using UnityEngine;

namespace BellRinger.Tests.EditMode
{
    public sealed class BellRingerAudioLedMapperTests
    {
        [Test]
        public void MapsFrontTargetNearDisplayCenter()
        {
            bool mapped = BellRingerAudioLedMapper.TryMapFromLocalPosition(
                new Vector3(0f, 0f, 5f),
                1f,
                10f,
                1f,
                1f,
                0.22f,
                0.06f,
                90f,
                55f,
                out BellRingerLedDotFrame dotFrame);

            Assert.That(mapped, Is.True);
            Assert.That(dotFrame.x, Is.EqualTo(8));
            Assert.That(dotFrame.y, Is.EqualTo(4));
            Assert.That(dotFrame.brightnessNormalized, Is.GreaterThan(0.06f));
        }

        [Test]
        public void MapsLeftTargetToLeftHalf()
        {
            bool mapped = BellRingerAudioLedMapper.TryMapFromLocalPosition(
                new Vector3(-5f, 0f, 5f),
                1f,
                10f,
                1f,
                1f,
                0.22f,
                0.06f,
                90f,
                55f,
                out BellRingerLedDotFrame dotFrame);

            Assert.That(mapped, Is.True);
            Assert.That(dotFrame.x, Is.LessThan(8));
        }

        [Test]
        public void ReturnsFalseWhenTargetIsOutsideRange()
        {
            bool mapped = BellRingerAudioLedMapper.TryMapFromLocalPosition(
                new Vector3(0f, 0f, 15f),
                1f,
                10f,
                1f,
                1f,
                0.22f,
                0.06f,
                90f,
                55f,
                out _);

            Assert.That(mapped, Is.False);
        }
    }
}
