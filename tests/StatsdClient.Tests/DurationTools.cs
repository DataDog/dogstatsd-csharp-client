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
    }
}