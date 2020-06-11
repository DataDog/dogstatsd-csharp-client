using System;

namespace StatsdClient
{
    internal enum StatsSenderTransportType
    {
        UDS,
        UDP,
    }

    internal interface IStatsSender : IDisposable
    {
        StatsSenderTransportType TransportType { get; }

        bool Send(byte[] buffer, int length);
    }
}