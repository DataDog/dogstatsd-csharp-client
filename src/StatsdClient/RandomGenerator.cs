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
            return _random.NextDouble() < sampleRate;
        }
    }
}