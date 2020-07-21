using System.Net;
using System.Net.Sockets;
using Mono.Unix;
using StatsdClient;

namespace Tests.Utils
{
    internal class SocketServer : AbstractServer
    {
        private readonly Socket _server;

        public SocketServer(StatsdConfig config)
        {
            EndPoint endPoint;
            int bufferSize;

            var serverName = config.StatsdServerName;
            if (serverName.StartsWith(StatsdBuilder.UnixDomainSocketPrefix))
            {
                serverName = serverName.Substring(StatsdBuilder.UnixDomainSocketPrefix.Length);
                _server = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified);
                endPoint = new UnixEndPoint(serverName);
                bufferSize = config.StatsdMaxUnixDomainSocketPacketSize;
            }
            else
            {
                endPoint = new IPEndPoint(IPAddress.Parse(config.StatsdServerName), config.StatsdPort);
                _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                bufferSize = config.StatsdMaxUDPPacketSize;
            }

            _server.ReceiveTimeout = 1000;
            _server.Bind(endPoint);
            Start(bufferSize);
        }

        public override void Dispose()
        {
            base.Dispose();
            _server.Dispose();
        }

        protected override int? Read(byte[] buffer)
        {
            try
            {
                return _server.Receive(buffer);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
            {
                return null;
            }
        }
    }
}