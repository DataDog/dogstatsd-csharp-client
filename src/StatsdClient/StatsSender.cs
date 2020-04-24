using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Mono.Unix;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    class StatsSender : IBufferBuilderHandler, IDisposable
    {
        readonly Socket _socket;
        readonly int _noBufferSpaceAvailableRetryCount;

        static readonly TimeSpan NoBufferSpaceAvailableWait = TimeSpan.FromMilliseconds(10);

        public static StatsSender CreateUDPStatsSender(IPEndPoint endPoint)
        {
            return new StatsSender("UDP",
                                   endPoint,
                                   AddressFamily.InterNetwork,
                                   ProtocolType.Udp,
                                   null);
        }

        public static StatsSender CreateUnixDomainSocketStatsSender(UnixEndPoint endPoint,
                                                                    TimeSpan? udsBufferFullBlockDuration)
        {
            return new StatsSender("Unix domain socket",
                                   endPoint,
                                   AddressFamily.Unix,
                                   ProtocolType.IP,
                                   udsBufferFullBlockDuration);
        }

        StatsSender(
            string kind,
            EndPoint endPoint,
            AddressFamily addressFamily,
            ProtocolType protocolType,
            TimeSpan? bufferFullBlockDuration)
        {
            if (bufferFullBlockDuration.HasValue)
            {
                _noBufferSpaceAvailableRetryCount = (int)(bufferFullBlockDuration.Value.TotalMilliseconds
                    / NoBufferSpaceAvailableWait.TotalMilliseconds);
            }

            try
            {
                _socket = new Socket(addressFamily, SocketType.Dgram, protocolType);
            }
            catch (SocketException e)
            {
                throw new NotSupportedException($"{kind} is not supported on your operating system.", e);
            }

            try
            {
                // When closing, wait 2 seconds to send data.
                _socket.LingerState = new LingerOption(true, 2);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ProtocolOption)
            {
                // It is not supported on Windows for Dgram with UDP.
            }
            _socket.Connect(endPoint);
        }

        public void Handle(byte[] buffer, int length)
        {
            for (int i = 0; i < 1 + _noBufferSpaceAvailableRetryCount; ++i)
            {
                try
                {
                    _socket.Send(buffer, 0, length, SocketFlags.None);
                    break;
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                {
                    Task.Delay(NoBufferSpaceAvailableWait).Wait();
                }
            }
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}