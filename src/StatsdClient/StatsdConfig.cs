namespace StatsdClient
{
    public class StatsdConfig
    {
        public string StatsdServerName { get; set; }
        public int StatsdPort { get; set; }
        public int StatsdMaxUDPPacketSize { get; set; }
        public bool StatsdTruncateIfTooLong { get; set; } = true;
        public string Prefix { get; set; }

        public const int DefaultStatsdPort = 8125;
        public const int DefaultStatsdMaxUDPPacketSize = 512;

        public StatsdConfig()
        {
            StatsdPort = DefaultStatsdPort;
            StatsdMaxUDPPacketSize = DefaultStatsdMaxUDPPacketSize;
        }
    }
}
