using System.Diagnostics.CodeAnalysis;

namespace StatsdClient.Statistic
{
    /// <summary>
    /// Stats stores the data for a metric or a service check or an event.
    /// The field `Metric`, `ServiceCheck` and `Event` are structures for performance reasons. These
    /// fields are embeded inside Stats and so do not require extra allocations.
    /// </summary>
    internal class Stats
    {
        // The next 3 fields are not properties because we want to take a reference on them to avoid a copy.
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Take a reference on struct")]
        public StatsMetric Metric;

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Take a reference on struct")]
        public StatsServiceCheck ServiceCheck;

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Take a reference on struct")]
        public StatsEvent Event;

        public StatsKind Kind { get; set; }
    }
}