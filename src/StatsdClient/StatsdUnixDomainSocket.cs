using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Mono.Unix;
using System.Text;

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
        private readonly object _socketLock;

        public static readonly string UnixDomainSocketPrefix = "unix://";

        public StatsdUnixDomainSocket(string unixSocket, int maxPacketSize)
        {
#if OS_WINDOWS
#pragma warning disable CS0162 // Unreachable code detected
            throw new NotSupportedException("Unix domain socket is not supported on Windows.");
#endif
            if (unixSocket == null || !unixSocket.StartsWith(StatsdUnixDomainSocket.UnixDomainSocketPrefix))
                throw new ArgumentException($"{nameof(unixSocket)} must start with {StatsdUnixDomainSocket.UnixDomainSocketPrefix}");
            unixSocket = unixSocket.Substring(StatsdUnixDomainSocket.UnixDomainSocketPrefix.Length);

            _socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.IP);
            _endPoint = new UnixEndPoint(unixSocket);
            _maxPacketSize = maxPacketSize;
            _socket.Blocking = false;

            // When closing, wait 2 seconds to send data.
            _socket.LingerState = new LingerOption(true, 2);
            _socketLock = new object();
        }

        public void Send(string command)
        {
            if (!_socket.Connected)        
            {
                lock (_socketLock)
                {
                    if (!_socket.Connected)        
                        _socket.Connect(_endPoint);
                }                
            }

            SocketSender.Send(_maxPacketSize, command, 
                encodedCommand => _socket.Send(encodedCommand));
        }

        public Task SendAsync(string command)
        {
            return SocketSender.SendAsync(_endPoint, _socket, _maxPacketSize, 
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(command)));
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}