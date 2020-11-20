namespace StatsdClient
{
#pragma warning disable CS1591
    internal interface IRandomGenerator
    {
        bool ShouldSend(double sampleRate);
    }
}