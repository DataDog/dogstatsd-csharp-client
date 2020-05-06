namespace StatsdClient
{
    internal enum StatsSenderTransportType
    {
        UDS,
        UDP,
    }

    internal interface IStatsSender
    {
        StatsSenderTransportType TransportType { get; }

        bool Send(byte[] buffer, int length);
    }
}