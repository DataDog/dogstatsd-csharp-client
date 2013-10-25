﻿namespace StatsdClient
{
	public class MetricsConfig
	{
		public string StatsdServerName { get; set; }
        public int StatsdPort { get; set; }
        public int StatsdMaxUDPPacketSize { get; set; }
		public string Prefix { get; set; }
        public string[] Tags { get; set; }

        public const int DefaultStatsdPort = 8125;
        public const int DefaultStatsdMaxUDPPacketSize = 512;

        public MetricsConfig()
        {
            StatsdPort = DefaultStatsdPort;
            StatsdMaxUDPPacketSize = DefaultStatsdMaxUDPPacketSize;
        }
	}
}
