﻿namespace StatsdClient
{
    public class StatsdConfig
    {
        public string StatsdServerName { get; set; }
        public int StatsdPort { get; set; }
        public int StatsdMaxUDPPacketSize { get; set; }
        public int StatsdMaxUnixDomainSocketPacketSize {get; set;} = 2048;
        public bool StatsdTruncateIfTooLong { get; set; } = true;
        public string Prefix { get; set; }

        public AdvancedStatsConfig Advanced { get; }

        public string[] ConstantTags { get; set; }
        public const int DefaultStatsdPort = 8125;
        public const int DefaultStatsdMaxUDPPacketSize = 512;

        public const string DD_ENTITY_ID_ENV_VAR = "DD_ENTITY_ID";
        public const string DD_DOGSTATSD_PORT_ENV_VAR = "DD_DOGSTATSD_PORT";
        public const string DD_AGENT_HOST_ENV_VAR = "DD_AGENT_HOST";

        public StatsdConfig()
        {
            StatsdPort = 0;
            StatsdMaxUDPPacketSize = DefaultStatsdMaxUDPPacketSize;
            Advanced = new AdvancedStatsConfig();
        }
    }
}
