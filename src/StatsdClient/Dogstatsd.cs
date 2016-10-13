﻿using System;

namespace StatsdClient
{
    public enum Status
    {
        OK = 0,
        WARNING = 1,
        CRITICAL = 2,
        UNKNOWN = 3
    }

    public static class DogStatsd
    {
        private static readonly DogStatsdService _dogStatsdService = new DogStatsdService();
        
        public static void Configure(StatsdConfig config) => _dogStatsdService.Configure(config);

        public static void Event(string title, string text, string alertType = null, string aggregationKey = null,
            string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null,
            string[] tags = null)
            =>
                _dogStatsdService.Event(title: title, text: text, alertType: alertType, aggregationKey: aggregationKey, sourceType: sourceType, dateHappened: dateHappened, priority: priority,
                    hostname: hostname, tags: tags);

        public static void Counter<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Counter<T>(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Increment(string statName, int value = 1, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Increment(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Decrement(string statName, int value = 1, double sampleRate = 1.0, params string[] tags) =>
            _dogStatsdService.Decrement(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Gauge<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Gauge<T>(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

        public static void Histogram<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null) =>
            _dogStatsdService.Histogram<T>(statName: statName, value: value, sampleRate: sampleRate, tags: tags);

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

        public static void ServiceCheck(string name, Status status, int? timestamp = null, string hostname = null,
            string[] tags = null, string message = null) =>
                _dogStatsdService.ServiceCheck(name, status, timestamp, hostname, tags, message);
    }
}
