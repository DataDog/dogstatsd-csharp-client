using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class DogStatsdServiceReconnectionTests
    {
        [Test]
        public void UDPReconnection()
        {
            var config = new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 1234,
                OriginDetection = false,
            };

            config.ClientSideAggregation = null;
            CheckReconnection(c => new SocketServer(c), config);
        }

        [Test]
        public void UDSReconnection()
        {
            // Skip on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("Test relies on Unix Domain Sockets and does not run on Windows.");
            }

            using (var temporaryPath = new TemporaryPath())
            {
                var config = new StatsdConfig
                {
                    StatsdServerName = StatsdBuilder.UnixDomainSocketPrefix + temporaryPath.Path,
                };

                config.ClientSideAggregation = null;
                CheckReconnection(c => new SocketServer(c), config);
            }
        }

        [Test]
        public void NamedPipeReconnection()
        {
            // Only run on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("Test relies on Named Pipes and only runs on Windows.");
            }

            var config = new StatsdConfig
            {
                PipeName = "TestPipe",
            };

            config.Advanced.TelemetryFlushInterval = null;
            config.ClientSideAggregation = null;
            CheckReconnection(c => new NamedPipeServer(c.PipeName, 1000, TimeSpan.FromSeconds(1)), config);
        }

        private static void CheckReconnection(
            Func<StatsdConfig, AbstractServer> serverFactory,
            StatsdConfig config)
        {
            AbstractServer server = null;

            try
            {
                server = serverFactory(config);

                using (var service = new DogStatsdService())
                {
                    service.Configure(config);
                    service.Increment("test1");
                    Assert.AreEqual("test1:1|c", server.Stop().Single());
                    server.Dispose();

                    // Send a metric when the server is not running.
                    service.Increment("test2");
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

                    // Restart the server
                    server = serverFactory(config);
                    service.Increment("test3");
                    service.Dispose();
                    Assert.AreEqual("test3:1|c", server.Stop().Last());
                }
            }
            finally
            {
                server?.Dispose();
            }
        }
    }
}
