using System;
using System.Net;
using System.Net.Sockets;
using Mono.Unix;

namespace StatsdClient
{
    class StatsSender : IBufferBuilderHandler, IDisposable
    {
        readonly Socket _socket;

        public static StatsSender CreateUDPStatsSender(IPEndPoint endPoint)
        {
            return new StatsSender("UDP", endPoint, AddressFamily.InterNetwork, ProtocolType.Udp);
        }

        public static StatsSender CreateUnixDomainSocketStatsSender(UnixEndPoint endPoint)
        {
            return new StatsSender("Unix domain socket", endPoint, AddressFamily.Unix, ProtocolType.IP);
        }

        StatsSender(
            string kind,
            EndPoint endPoint,
            AddressFamily addressFamily,
            ProtocolType protocolType)
        {
            try
            {
                _socket = new Socket(addressFamily, SocketType.Dgram, protocolType);
            }
            catch (SocketException e)
            {
                throw new NotSupportedException($"{kind} is not supported on your operating system.", e);
            }

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