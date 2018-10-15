using System;
using System.Net.Sockets;
using NUnit.Framework;
using StatsdClient;

namespace Tests
{
    [TestFixture]
    public class UDPSmokeTests
    {
        [Test]
        public void Sends_a_counter()
        {
            try
            {
                var client = new StatsdUDP(name: "127.0.0.1", port: 8126);
                client.Send("socket2:1|c");
            }
            catch(SocketException ex)
            {
                Assert.Fail("Socket Exception, have you set up your Statsd name and port? Error: {0}", ex.Message);
            }
        }
    }
}
