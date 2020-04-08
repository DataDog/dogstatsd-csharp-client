using System;
using System.Diagnostics.CodeAnalysis;

namespace StatsdClient
{
    #pragma warning disable CS1591
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "See ObsoleteAttribute.")]
    [ObsoleteAttribute("This class will become private in a future release.")]
    public class Stopwatch : IStopwatch
    {
        private readonly System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

        public void Start()
        {
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public int ElapsedMilliseconds()
        {
            return (int)unchecked(_stopwatch.ElapsedMilliseconds);
        }
    }
}