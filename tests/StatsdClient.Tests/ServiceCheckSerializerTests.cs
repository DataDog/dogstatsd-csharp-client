using System;
using System.Text;
using Moq;
using NUnit.Framework;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class ServiceCheckSerializerTests
    {
        private IStatsdUDP _udp;
        private IRandomGenerator _randomGenerator;
        private IStopWatchFactory _stopwatch;

        [SetUp]
        public void Setup()
        {
            _udp = Mock.Of<IStatsdUDP>();
            _randomGenerator = Mock.Of<IRandomGenerator>();
            Mock.Get(_randomGenerator).Setup(x => x.ShouldSend(It.IsAny<double>())).Returns(true);
            _stopwatch = Mock.Of<IStopWatchFactory>();
        }

        // =-=-=-=- ServiceCheck -=-=-=-=
        [Test]
        public void Send_service_check()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0);
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0"));
        }

        [Test]
        public void Send_service_check_with_timestamp()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, timestamp: 1);
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|d:1"));
        }

        [Test]
        public void Send_service_check_with_hostname()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, hostname: "hostname");
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|h:hostname"));
        }

        [Test]
        public void Send_service_check_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, tags: new[] { "tag1:value1", "tag2", "tag3:value3" });
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|#tag1:value1,tag2,tag3:value3"));
        }

        [Test]
        public void Send_service_check_with_message()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, serviceCheckMessage: "message");
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|m:message"));
        }

        [Test]
        public void Send_service_check_with_pipe_in_name()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            Assert.Throws<ArgumentException>(() => s.Send("name|", 0));
        }

        [Test]
        [TestCase("\r\n")]
        [TestCase("\n")]
        public void Send_service_check_with_new_line_in_name(string newline)
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name" + newline, 0);
            Mock.Get(_udp).Verify(x => x.Send("_sc|name\\n|0"));
        }

        [Test]
        public void Send_service_check_with_suffix_in_message()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, serviceCheckMessage: "m:message");
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|m:m\\:message"));
        }

        [Test]
        public void Send_service_check_with_all_optional()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, 1, "hostname", new[] { "tag1:value1", "tag2", "tag3:value3" }, "message");
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|d:1|h:hostname|#tag1:value1,tag2,tag3:value3|m:message"));
        }

        [Test]
        public void Send_service_check_with_message_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = (8 * 1024) - 13;
            var builder = BuildLongString(length);
            var message = builder;

            var exception = Assert.Throws<Exception>(() => s.Send("name", 0, serviceCheckMessage: message + "x"));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void Send_service_check_with_message_that_is_too_long_truncate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = (8 * 1024) - 13;
            var builder = BuildLongString(length);
            var message = builder;

            s.Send("name", 0, serviceCheckMessage: message + "x", truncateIfTooLong: true);

            var expected = "_sc|name|0|m:" + message;
            Mock.Get(_udp).Verify(x => x.Send(expected));
        }

        [Test]
        public void Send_service_check_with_name_that_is_too_long_truncate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = (8 * 1024) - 6;
            var builder = BuildLongString(length);
            var name = builder;

            var exception = Assert.Throws<ArgumentException>(() => s.Send(name + "x", 0, truncateIfTooLong: true));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void Add_service_check()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0"));
        }

        [Test]
        public void Add_service_check_with_timestamp()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, timestamp: 1);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|d:1"));
        }

        [Test]
        public void Add_service_check_with_hostname()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, hostname: "hostname");

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|h:hostname"));
        }

        [Test]
        public void Add_service_check_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, tags: new[] { "tag1:value1", "tag2", "tag3:value3" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|#tag1:value1,tag2,tag3:value3"));
        }

        [Test]
        public void Add_service_check_with_message()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, serviceCheckMessage: "message");

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|m:message"));
        }

        [Test]
        public void Add_service_check_with_pipe_in_name()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            Assert.Throws<ArgumentException>(() => s.Add("name|", 0));
        }

        [Test]
        [TestCase("\r\n")]
        [TestCase("\n")]
        public void Add_service_check_with_new_line_in_name(string newline)
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name" + newline, 0);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name\\n|0"));
        }

        [Test]
        public void Add_service_check_with_suffix_in_message()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, serviceCheckMessage: "m:message");

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|m:m\\:message"));
        }

        [Test]
        public void Add_service_check_with_all_optional()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, 1, "hostname", new[] { "tag1:value1", "tag2", "tag3:value3" }, "message");

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|d:1|h:hostname|#tag1:value1,tag2,tag3:value3|m:message"));
        }

        [Test]
        public void Add_service_check_with_message_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = (8 * 1024) - 13;
            var builder = BuildLongString(length);
            var message = builder;

            var exception = Assert.Throws<Exception>(() => s.Add("name", 0, serviceCheckMessage: message + "x"));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void Add_service_check_with_name_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = (8 * 1024) - 6;
            var builder = BuildLongString(length);
            var name = builder;

            var exception = Assert.Throws<Exception>(() => s.Add(name + "x", 0));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        private static string BuildLongString(int length)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                builder.Append(i % 10);
            }

            return builder.ToString();
        }
    }
}
