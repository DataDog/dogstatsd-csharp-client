using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using StatsdClient.Bufferize;
using StatsdClient.Utils;

namespace Tests
{
    [TestFixture]
    public class StatsBufferizeTests
    {
        [Test]
        public void StatsBufferize()
        {
            var handler = new BufferBuilderHandlerMock();
            var bufferBuilder = new BufferBuilder(handler, 3, "\n");
            using (var statsBufferize = new StatsBufferize(bufferBuilder, 10, null, TimeSpan.Zero))
            {
                var serializedMetric = new SerializedMetric(new Pool<SerializedMetric>(p => new SerializedMetric(p), 10));
                serializedMetric.Builder.Append("1");

                statsBufferize.Send(serializedMetric);
                while (handler.Buffer == null)
                {
                    Task.Delay(TimeSpan.FromMilliseconds(1)).Wait();
                }

                // Sent because buffer is full.
                Assert.AreEqual("1", Encoding.UTF8.GetString(handler.Buffer));
            }
        }
    }
}