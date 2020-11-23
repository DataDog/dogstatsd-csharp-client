using NUnit.Framework;
using StatsdClient;

namespace Tests
{
    [TestFixture]
    public class IPV4ParsingTests
    {
        [Test]
        public void Ipv4_parsing_works_with_hostname()
        {
            var ipAddress = StatsdUDP.GetIpv4Address("localhost");
            Assert.That(ipAddress.ToString(), Does.Contain("127.0.0.1"));
        }

        [Test]
        public void Ipv4_parsing_works_with_ip()
        {
            var ipAddress = StatsdUDP.GetIpv4Address("127.0.0.1");
            Assert.That(ipAddress.ToString(), Does.Contain("127.0.0.1"));
        }
    }
}