using System;

namespace StatsdClient
{
    /// <summary>
    /// Define the configuration for the client side aggregation.
    /// </summary>
    public class ClientSideAggregationConfig
    {
        /// <summary>
        /// Gets or sets the maximum number of unique stats before flushing.
        /// </summary>
        public int MaxUniqueStatsBeforeFlush { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the maximum interval duration between two flushes.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(10);
    }
}