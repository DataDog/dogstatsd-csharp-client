using System;

namespace StatsdClient
{
    internal enum TransportType
    {
        UDS,
        UDP,
    }

    internal interface ITransport : IDisposable
    {
        TransportType TransportType { get; }

        bool Send(byte[] buffer, int length);
    }
}