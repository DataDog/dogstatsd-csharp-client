using System;
using System.Net;
using System.Net.Sockets;

namespace StatsdClient
{
    class StatsSender : IBufferBuilderHandler, IDisposable
    {
        readonly Socket _socket;

        public StatsSender(IPEndPoint endPoint)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // When closing, wait 2 seconds to send data.
            _socket.LingerState = new LingerOption(true, 2);
            _socket.Connect(endPoint);
        }

        public void Handle(byte[] buffer, int length)
        {
            _socket.Send(buffer, 0, length, SocketFlags.None);
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}