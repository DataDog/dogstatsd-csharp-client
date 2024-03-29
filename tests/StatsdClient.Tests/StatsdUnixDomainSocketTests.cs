using System;
using System.Net.Sockets;
using System.Text;
using Mono.Unix;
using NUnit.Framework;
using StatsdClient;
using Tests.Utils;

#if !OS_WINDOWS
namespace Tests
{
    [TestFixture]
    public class StatsdUnixDomainSocketTests
    {
        private TemporaryPath _temporaryPath;

        public enum HostnameProvider
        {
            Environment,
            Property,
        }

        [SetUp]
        public void Setup()
        {
            _temporaryPath = new TemporaryPath();
        }

        [TearDown]
        public void TearDown()
        {
            _temporaryPath.Dispose();
        }

        [TestCase(HostnameProvider.Property)]
        [TestCase(HostnameProvider.Environment)]
        public void SendSingleMetric(HostnameProvider hostnameProvider)
        {
            using (var socket = CreateSocketServer(_temporaryPath))
            {
                using (var service = CreateService(_temporaryPath, hostnameProvider))
                {
                    var metric = "gas_tank.level";
                    var value = 0.75;
                    service.Gauge(metric, value);
                    Assert.AreEqual($"{metric}:{value}|g\n", ReadFromServer(socket));
                }
            }
        }

        // Use a timeout in case Gauge become blocking
        [Test]
        [Timeout(30000)]
        public void CheckNotBlockWhenServerNotReadMessage()
        {
            var tags = new string[] { new string('A', 100) };

            using (var socket = CreateSocketServer(_temporaryPath))
            {
                using (var service = CreateService(_temporaryPath))
                {
                    // We are sending several Gauge to make sure there is no buffer
                    // that can make service.Gauge blocks after several calls.
                    for (int i = 0; i < 10; ++i)
                    {
                        service.Gauge("metric" + i, 42, 1, tags);
                    }

                    // If the code go here that means we do not block.
                }
            }
        }

        private static DogStatsdService CreateService(
                    TemporaryPath temporaryPath,
                    HostnameProvider hostnameProvider = HostnameProvider.Property)
        {
            var serverName = StatsdBuilder.UnixDomainSocketPrefix + temporaryPath.Path;
            var dogstatsdConfig = new StatsdConfig { StatsdMaxUnixDomainSocketPacketSize = 1000 };

            switch (hostnameProvider)
            {
                case HostnameProvider.Property: dogstatsdConfig.StatsdServerName = serverName; break;
                case HostnameProvider.Environment:
                    {
                        Environment.SetEnvironmentVariable(StatsdConfig.AgentHostEnvVar, serverName);
                        break;
                    }
            }

            var dogStatsdService = new DogStatsdService();
            dogStatsdService.Configure(dogstatsdConfig);

            return dogStatsdService;
        }

        private static Socket CreateSocketServer(TemporaryPath temporaryPath)
        {
            var endPoint = new UnixEndPoint(temporaryPath.Path);
            var server = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified);
            server.Bind(endPoint);

            return server;
        }

        private static string ReadFromServer(Socket socket)
        {
            var builder = new StringBuilder();
            var buffer = new byte[8096];

            while (socket.Available > 0 || builder.Length == 0)
            {
                var count = socket.Receive(buffer);
                var chars = System.Text.Encoding.UTF8.GetChars(buffer, 0, count);
                builder.Append(chars);
            }

            return builder.ToString();
        }
    }
}
#endif