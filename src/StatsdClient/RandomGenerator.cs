namespace StatsdClient
{
    internal class RandomGenerator : IRandomGenerator
    {
        private readonly ThreadSafeRandom _random;

        public RandomGenerator()
        {
            _random = new ThreadSafeRandom();
        }

        public bool ShouldSend(double sampleRate)
        {
            return sampleRate >= 1 || _random.NextDouble() < sampleRate;
        }
    }
}