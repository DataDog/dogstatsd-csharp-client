using System;
using System.Net.Sockets;
using Mono.Unix;

namespace StatsdClient
{
    ///------------------------------------------------------------------------
    // StatsdUnixDomainSocket send messages using Unix domain socket.
    // The maximum message size is 2048.
    // The class is sealed because of the implementation of Dispose.
    ///------------------------------------------------------------------------
    sealed class StatsdUnixDomainSocket : IDisposable, IStatsdUDP
    {
        private readonly int _maxPacketSize;
        private readonly Socket _socket;
        private readonly UnixEndPoint _endPoint;

        public static readonly string UnixDomainSocketPrefix = "unix://";

        public StatsdUnixDomainSocket(string unixSocket, int maxPacketSize)
        {
            if (unixSocket == null || !unixSocket.StartsWith(StatsdUnixDomainSocket.UnixDomainSocketPrefix))
                throw new ArgumentException($"{nameof(unixSocket)} must start with {StatsdUnixDomainSocket.UnixDomainSocketPrefix}");
            unixSocket = unixSocket.Substring(StatsdUnixDomainSocket.UnixDomainSocketPrefix.Length);

            _socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.IP);
            _endPoint = new UnixEndPoint(unixSocket);
            _maxPacketSize = maxPacketSize;
            _socket.Blocking = false;
            _socket.Connect(_endPoint);
        }

        public void Send(string command)
        {            
            SocketSender.Send(_maxPacketSize, command, 
                encodedCommand => _socket.Send(encodedCommand));
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}