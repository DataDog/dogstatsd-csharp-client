using System;
using System.Diagnostics.CodeAnalysis;

namespace StatsdClient
{
    #pragma warning disable CS1591
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "See ObsoleteAttribute.")]
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