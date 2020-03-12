using System;
using NUnit.Framework;

namespace Tests
{
    static class DurationTools
    {
        static readonly double InaccuracyTimeFactor = 0.2;

        public static void AssertLess(long millisecondsDuration, TimeSpan duration)
        {
            Assert.Less(
                millisecondsDuration,
                duration.Multiply(1 + InaccuracyTimeFactor).TotalMilliseconds);
        }

        public static void AssertGreater(long millisecondsDuration, TimeSpan duration)
        {
            Assert.Greater(
                millisecondsDuration,
                duration.Multiply(1 - InaccuracyTimeFactor).TotalMilliseconds);
        }

        public static void AssertClose(long millisecondsDuration, TimeSpan duration)
        {
            Assert.That(millisecondsDuration,
                Is.EqualTo(duration.TotalMilliseconds).Within(InaccuracyTimeFactor * 100).Percent);
        }

        public static bool AreClose(long millisecondsDuration, TimeSpan duration)
        {
            return millisecondsDuration > duration.Multiply(1 - InaccuracyTimeFactor).TotalMilliseconds
                && millisecondsDuration < duration.Multiply(1 + InaccuracyTimeFactor).TotalMilliseconds;
        }
    }
}