using System;

namespace StatsdClient
{
    public class RandomGenerator : IRandomGenerator
    {
        readonly ThreadSafeRandom _random;
        public RandomGenerator()
        {
            _random = new ThreadSafeRandom();
        }

        public bool ShouldSend(double sampleRate)
        {
            if (sampleRate >= 1) return true;
            return _random.NextDouble() < sampleRate;
        }
    }
}