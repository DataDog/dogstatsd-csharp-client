using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("StatsdClient.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001009558fd81ea0e330858198ae6c860c0c9fd2d9df3e5f2069434649e4ec61c9ceb9744d2a3fd82518d90abb5cbcefb6292e9d227d5854bd07dbd8884d129350c95c7742d499dfc4961223b35326e203c5924e413a2385a7aa7c704432e9101bb946da201977df2b992c25d0fb77645c1ac5bc29cde7bc8e5d054b78bd9c6727497")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2,PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

namespace StatsdClient
{
    public enum Status
    {
        OK = 0,
        WARNING = 1,
        CRITICAL = 2,
        UNKNOWN = 3,
    }

    public static class DogStatsd
    {
        private static readonly DogStatsdService _dogStatsdService = new DogStatsdService();

        public static void Configure(StatsdConfig config) => _dogStatsdService.Configure(config);

        public static void Event(
            string title,
            string text,
            string alertType = null,
            string aggregationKey = null,
            string sourceType = null,
            int? dateHappened = null,
            string priority = null,
            string hostname = null,
            string[] tags = null)
            =>
                _dogStatsdService.Event(
                    title: title,
                    text: text,
                    alertType: alertType,
                    aggregationKey: aggregationKey,
                    sourceType: sourceType,
                    dateHappened: dateHappened,
                    priority: priority,
                    hostname: hostname,
                    tags: tags);

        public static void Counter<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Counter<T>(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Increment(string statName, int value = 1, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Increment(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Decrement(string statName, int value = 1, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Decrement(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Gauge<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Gauge<T>(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Histogram<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Histogram<T>(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Distribution<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Distribution<T>(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Set<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Set<T>(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Timer<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Timer<T>(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static IDisposable StartTimer(string name, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.StartTimer(name: name, sampleRate: sampleRate, tags: tags);

        public static void Time(Action action, string statName, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Time(action: action, statName: statName, sampleRate: sampleRate, tags: tags);

        public static T Time<T>(Func<T> func, string statName, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Time<T>(func: func, statName: statName, sampleRate: sampleRate, tags: tags);

        public static void ServiceCheck(
            string name,
            Status status,
            int? timestamp = null,
            string hostname = null,
            string[] tags = null,
            string message = null) =>
                _dogStatsdService.ServiceCheck(name, status, timestamp, hostname, tags, message);

        public static ITelemetryCounters TelemetryCounters => _dogStatsdService.TelemetryCounters;
    }
}
