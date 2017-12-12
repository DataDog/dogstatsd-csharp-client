using System;

namespace StatsdClient
{
    public interface IMetricsTimer : IDisposable
    {
        void AddTag(params string[] additionalTags);
    }
}