﻿using System;
using System.Collections.Generic;

namespace StatsdClient
{
    public class MetricsTimer : IDisposable
    {
        private readonly string _name;
        private readonly DogStatsdService _dogStatsd;
        private readonly Stopwatch _stopWatch;
        private bool _disposed;
        private readonly double _sampleRate;

        public MetricsTimer(string name, double sampleRate = 1.0, string[] tags = null) : this(null, name, sampleRate,
            tags)
        {            
        }

        public MetricsTimer(DogStatsdService dogStatsd, string name, double sampleRate = 1.0, string[] tags = null)
        {
            _name = name;
            _dogStatsd = dogStatsd;
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            _sampleRate = sampleRate;
            Tags = new List<string>();
            if(tags != null)
                Tags.AddRange(tags);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _stopWatch.Stop();

                if(_dogStatsd == null)
                    DogStatsd.Timer(_name, _stopWatch.ElapsedMilliseconds(), _sampleRate, Tags.ToArray());
                else                
                    _dogStatsd.Timer(_name, _stopWatch.ElapsedMilliseconds(), _sampleRate, Tags.ToArray());                
            }
        }

        public List<string> Tags { get; set; }
    }
}