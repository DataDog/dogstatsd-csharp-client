using System;

namespace StatsdClient
{
    [ObsoleteAttribute("This class will become private in a future release.")]
    public class RandomGenerator : IRandomGenerator
    {
        private readonly ThreadSafeRandom _random;

        public RandomGenerator()
        {
            _random = new ThreadSafeRandom();
        }

        public bool ShouldSend(double sampleRate)
        {
            return _random.NextDouble() < sampleRate;
        }
    }
}