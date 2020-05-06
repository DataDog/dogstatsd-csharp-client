using System.Diagnostics.CodeAnalysis;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    public class StatsdConfig
    {
        public const int DefaultStatsdPort = 8125;
        public const int DefaultStatsdMaxUDPPacketSize = 512;

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Avoid breaking changes.")]
        public const string DD_ENTITY_ID_ENV_VAR = "DD_ENTITY_ID";

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Avoid breaking changes.")]
        public const string DD_DOGSTATSD_PORT_ENV_VAR = "DD_DOGSTATSD_PORT";

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Avoid breaking changes.")]
        public const string DD_AGENT_HOST_ENV_VAR = "DD_AGENT_HOST";

        public StatsdConfig()
        {
            StatsdPort = 0;
            StatsdMaxUDPPacketSize = DefaultStatsdMaxUDPPacketSize;
            Advanced = new AdvancedStatsConfig();
        }

        public string StatsdServerName { get; set; }

        public int StatsdPort { get; set; }

        public int StatsdMaxUDPPacketSize { get; set; }

        public int StatsdMaxUnixDomainSocketPacketSize { get; set; } = 2048;

        public bool StatsdTruncateIfTooLong { get; set; } = true;

        public string Prefix { get; set; }

        public AdvancedStatsConfig Advanced { get; }

        public string[] ConstantTags { get; set; }
    }
}
