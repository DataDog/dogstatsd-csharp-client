using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mono.Unix;
using StatsdClient;

namespace Tests.Utils
{
    internal class SocketServer : IDisposable
    {
        private readonly Socket _server;
        private readonly Task _receiver;
        private readonly ManualResetEventSlim _serverStop = new ManualResetEventSlim(false);
        private readonly List<string> _messagesReceived = new List<string>();

        private volatile bool _shutdown = false;

        public SocketServer(StatsdConfig config, bool removeUDSFileBeforeStarting = false)
        {
            EndPoint endPoint;
            int bufferSize;

            var serverName = config.StatsdServerName;
            if (serverName.StartsWith(StatsdBuilder.UnixDomainSocketPrefix))
            {
                serverName = serverName.Substring(StatsdBuilder.UnixDomainSocketPrefix.Length);
                if (removeUDSFileBeforeStarting)
                {
                    File.Delete(serverName);
                }

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
            _receiver = Task.Run(() => ReadFromServer(bufferSize));
        }

        public void Dispose()
        {
            Stop();
            _server.Dispose();
        }

        public List<string> Stop()
        {
            if (!_shutdown)
            {
                _shutdown = true;
                _serverStop.Wait();
            }

            return _messagesReceived;
        }

        private void ReadFromServer(int bufferSize)
        {
            var buffer = new byte[bufferSize];

            while (true)
            {
                try
                {
                    var count = _server.Receive(buffer);
                    var message = System.Text.Encoding.UTF8.GetString(buffer, 0, count);
                    _messagesReceived.AddRange(message.Split("\n", StringSplitOptions.RemoveEmptyEntries));
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                {
                    if (_shutdown)
                    {
                        _serverStop.Set();
                        return;
                    }
                }
            }
        }
    }
}