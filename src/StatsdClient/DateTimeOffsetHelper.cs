using System;

namespace StatsdClient
{
    // Helper to turn a DateTimeOffset to a unix timestamp in seconds
    // ToUnixTimeSeconds() exists today in DateTime and DateTimeOffset but is not
    // available on older versions of .Net.
    // Snippet from: https://github.com/dotnet/dotnet-api-docs/blob/13d97c7/snippets/csharp/System/DateTimeOffset/ToUnixTimeSeconds/tounixtimeseconds1.cs
    // License: https://github.com/dotnet/dotnet-api-docs/blob/13d97c7/LICENSE-CODE
    internal class DateTimeOffsetHelper
    {
        // Number of days in a non-leap year
        private const int DaysPerYear = 365;
        // Number of days in 4 years
        private const int DaysPer4Years = (DaysPerYear * 4) + 1;       // 1461
        // Number of days in 100 years
        private const int DaysPer100Years = (DaysPer4Years * 25) - 1;  // 36524
        // Number of days in 400 years
        private const int DaysPer400Years = (DaysPer100Years * 4) + 1; // 146097
        // Number of days from 1/1/0001 to 12/31/1969
        private const int DaysTo1970 = (DaysPer400Years * 4) + (DaysPer100Years * 3) + (DaysPer4Years * 17) + DaysPerYear; // 719,162
        private const long UnixEpochTicks = TimeSpan.TicksPerDay * DaysTo1970; // 621,355,968,000,000,000
        private const long UnixEpochSeconds = UnixEpochTicks / TimeSpan.TicksPerSecond; // 62,135,596,800
        private const long UnixEpochMilliseconds = UnixEpochTicks / TimeSpan.TicksPerMillisecond; // 62,135,596,800,000

        public static long ToUnixTimeSeconds(DateTimeOffset dto)
        {
            long seconds = dto.UtcDateTime.Ticks / TimeSpan.TicksPerSecond;
            return seconds - UnixEpochSeconds;
        }
    }
}
