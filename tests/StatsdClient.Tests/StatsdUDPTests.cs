using System;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using StatsdClient;
using Tests.Helpers;
using System.Threading.Tasks;

namespace Tests
{
    // Most of StatsUDP is tested in StatsdUnitTests. This is mainly to test the splitting of oversized
    // UDP packets
    [TestFixture]
    public class StatsUDPTests
    {
        private UdpListener _udpListener;
        private Thread _listenThread;
        private readonly int _serverPort = Convert.ToInt32("8126");
        private readonly string _serverName = "127.0.0.1";
        private StatsdUDP _udp;
        private Statsd _statsd;
        private List<string> _lastPulledMessages;
        private DogStatsdService _dogStatsdService;

        [OneTimeSetUp]
        public void SetUpUdpListenerAndStatsd()
        {
            _udpListener = new UdpListener(_serverName, _serverPort);
            var metricsConfig = new StatsdConfig { StatsdServerName = _serverName };
            _dogStatsdService = new DogStatsdService();
            _dogStatsdService.Configure(metricsConfig);
            _udp = new StatsdUDP(_serverName, _serverPort);
            _statsd = new Statsd(_udp);
        }

        [OneTimeTearDown]
        public void TearDownUdpListener()
        {
            _udpListener.Dispose();
            _udp.Dispose();
        }

        [SetUp]
        public void UdpListenerThread()
        {
            _lastPulledMessages = new List<string>();
            _listenThread = new Thread(new ParameterizedThreadStart(_udpListener.Listen));
        }

        [TearDown]
        public void ClearUdpListenerMessages()
        {
            _udpListener.GetAndClearLastMessages(); // just to be sure that nothing is left over
        }

        // Test helper. Waits until the listener is done receiving a message,
        // then asserts that the passed string is equal to the message received.
        private void AssertWasReceived(string shouldBe, int index = 0)
        {
            if (_lastPulledMessages.Count == 0)
            {
                // Stall until the the listener receives a message or times out
                while (_listenThread.IsAlive);
                _lastPulledMessages = _udpListener.GetAndClearLastMessages();
            }

            string actual = null;

            if (index < _lastPulledMessages.Count)
                actual = _lastPulledMessages[index];
            Assert.AreEqual(shouldBe, actual);
        }

        [Test]
        public void send()
        {
            // (Sanity test)
            _listenThread.Start();
            _udp.Send("test-metric");
            AssertWasReceived("test-metric");
        }

        [Test]
        public void send_equal_to_udp_packet_limit_is_still_sent()
        {
            var msg = new String('f', StatsdConfig.DefaultStatsdMaxUDPPacketSize);
            _listenThread.Start();
            _udp.Send(msg);
            // As long as we're at or below the limit, the packet should still be sent
            AssertWasReceived(msg);
        }

        [Test]
        public void send_unsplittable_oversized_udp_packets_are_not_split_or_sent_and_no_exception_is_raised()
        {
            // This message will be one byte longer than the theoretical limit of a UDP packet
            var msg = new String('f', 65508);
            _listenThread.Start();
            _statsd.Add<Statsd.Counting, int>(msg, 1);
            _statsd.Send();
            // It shouldn't be split or sent, and no exceptions should be raised.
            AssertWasReceived(null);
        }

        [Test]
        public void send_oversized_udp_packets_are_split_if_possible()
        {
            var msg = new String('f', (StatsdConfig.DefaultStatsdMaxUDPPacketSize - 15));
            _listenThread.Start(3); // Listen for 3 messages
            _statsd.Add<Statsd.Counting, int>(msg, 1);
            _statsd.Add<Statsd.Gauge, int>(msg, 2);
            _statsd.Send();
            // These two metrics should be split as their combined lengths exceed the maximum packet size
            AssertWasReceived(String.Format("{0}:1|c", msg), 0);
            AssertWasReceived(String.Format("{0}:2|g", msg), 1);
            // No extra metric should be sent at the end
            AssertWasReceived(null, 2);
        }

        [Test]
        public void send_oversized_udp_packets_are_split_if_possible_with_multiple_messages_in_one_packet()
        {
            var msg = new String('f', StatsdConfig.DefaultStatsdMaxUDPPacketSize / 2);
            _listenThread.Start(3); // Listen for 3 messages
            _statsd.Add<Statsd.Counting, int>("counter", 1);
            _statsd.Add<Statsd.Counting, int>(msg, 2);
            _statsd.Add<Statsd.Counting, int>(msg, 3);
            _statsd.Send();
            AssertWasReceived(String.Format("counter:1|c\n{0}:2|c", msg), 0);
            AssertWasReceived(String.Format("{0}:3|c", msg), 1);
            AssertWasReceived(null, 2);
        }

        [Test]
        public void set_max_udp_packet_size()
        {
            // Make sure that we can set the max UDP packet size
            _udp = new StatsdUDP(_serverName, _serverPort, 10);
            _statsd = new Statsd(_udp);
            var msg = new String('f', 5);
            _listenThread.Start(2);
            _statsd.Add<Statsd.Counting, int>(msg, 1);
            _statsd.Add<Statsd.Gauge, int>(msg, 2);
            _statsd.Send();
            // Since our packet size limit is now 10, this (short) message should still be split
            AssertWasReceived(String.Format("{0}:1|c", msg), 0);
            AssertWasReceived(String.Format("{0}:2|g", msg), 1);
        }
    }
}
