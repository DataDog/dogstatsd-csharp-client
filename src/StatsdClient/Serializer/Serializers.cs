namespace StatsdClient
{
    internal class Serializers
    {
        public MetricSerializer MetricSerializer { get; set; }

        public ServiceCheckSerializer ServiceCheckSerializer { get; set; }

        public EventSerializer EventSerializer { get; set; }
    }
}