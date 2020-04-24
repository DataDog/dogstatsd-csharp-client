namespace StatsdClient
{
    enum StatsSenderTransportType
    {
        UDS,
        UDP
    }

    internal interface IStatsSender
    {
        bool Send(byte[] buffer, int length);
        StatsSenderTransportType TransportType { get; }
    }
}