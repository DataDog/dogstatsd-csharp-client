using System;
using System.Linq;
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
            CheckReconnection(new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 1234,
            });
        }

#if !OS_WINDOWS
        [Test]
        public void UDSReconnection()
        {
            using (var temporaryPath = new TemporaryPath())
            {
                CheckReconnection(new StatsdConfig
                {
                    StatsdServerName = StatsdBuilder.UnixDomainSocketPrefix + temporaryPath.Path,
                });
            }
        }
#endif

        private static void CheckReconnection(StatsdConfig config)
        {
            SocketServer server = null;

            try
            {
                server = new SocketServer(config);
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
                    server = new SocketServer(config, removeUDSFileBeforeStarting: true);
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