using System;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using StatsdClient.Bufferize;

namespace Tests
{
    [TestFixture]
    public class StatsBufferizeTests
    {
        [Test]
        public async Task StatsBufferize()
        {
            var handler = new BufferBuilderHandlerMock();
            var bufferBuilder = new BufferBuilder(handler, 3, "\n");
            var timeout = TimeSpan.FromMilliseconds(10);
            using (var statsBufferize = new StatsBufferize(new Telemetry(), bufferBuilder, 10, null, timeout))
            {
                statsBufferize.Send("123");
                while (handler.Buffer == null)
                {
                    await Task.Delay(1);
                }

                // Sent because buffer is full.
                Assert.AreEqual("123", Encoding.UTF8.GetString(handler.Buffer));
                handler.Buffer = null;

                statsBufferize.Send("4");
                while (handler.Buffer == null)
                {
                    await Task.Delay(1);
                }

                // Sent because we wait more than the timeout.
                Assert.AreEqual("4", Encoding.UTF8.GetString(handler.Buffer));
            }
        }
    }
}