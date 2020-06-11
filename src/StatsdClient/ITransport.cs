using System;

namespace StatsdClient
{
    internal enum StatsSenderTransportType
    {
        UDS,
        UDP,
    }

    internal interface ITransport : IDisposable
    {
        StatsSenderTransportType TransportType { get; }

        bool Send(byte[] buffer, int length);
    }
}