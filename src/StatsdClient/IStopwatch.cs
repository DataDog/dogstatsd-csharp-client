using System;
using System.Diagnostics.CodeAnalysis;

namespace StatsdClient
{
    #pragma warning disable CS1591
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "See ObsoleteAttribute.")]
    [ObsoleteAttribute("This interface will become private in a future release.")]
    public interface IStopwatch
    {
        void Start();

        void Stop();

        int ElapsedMilliseconds();
    }
}