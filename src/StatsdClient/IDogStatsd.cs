﻿using System;

namespace StatsdClient
{
    public interface IDogStatsd
    {
        void Configure(StatsdConfig config);
        void Counter<T>(string statName, T value, double sampleRate = 1, string[] tags = null);
        void Decrement(string statName, int value = 1, double sampleRate = 1, params string[] tags);

        void Event(string title, string text, string alertType = null, string aggregationKey = null,
            string sourceType = null, int? dateHappened = null, string priority = null,
            string hostname = null, string[] tags = null);

        void Gauge<T>(string statName, T value, double sampleRate = 1, string[] tags = null);
        void Histogram<T>(string statName, T value, double sampleRate = 1, string[] tags = null);
        void Increment(string statName, int value = 1, double sampleRate = 1, string[] tags = null);
        void Set<T>(string statName, T value, double sampleRate = 1, string[] tags = null);
        IDisposable StartTimer(string name, double sampleRate = 1, string[] tags = null);
        void Time(Action action, string statName, double sampleRate = 1, string[] tags = null);
        T Time<T>(Func<T> func, string statName, double sampleRate = 1, string[] tags = null);
        void Timer<T>(string statName, T value, double sampleRate = 1, string[] tags = null);

        void ServiceCheck(string name, Status status, int? timestamp = null, string hostname = null, string[] tags = null,
            string message = null);
    }
}