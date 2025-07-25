using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;
using StatsdClient.Utils;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class StatsBufferizeTests
    {
        [Test]
        [Timeout(10000)]
        public void StatsBufferize()
        {
            var handler = new BufferBuilderHandlerMock();
            var bufferBuilder = new BufferBuilder(handler, 30, "\n", Tools.ExceptionHandler);
            var serializers = new Serializers
            {
                EventSerializer = new EventSerializer(new SerializerHelper(null, null)),
            };
            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);
            using (var statsBufferize = new StatsBufferize(statsRouter, 10, null, TimeSpan.Zero, Tools.ExceptionHandler))
            {
                var stats = new Stats { Kind = StatsKind.Event };
                stats.Event.Text = "test";
                stats.Event.Title = "title";

                statsBufferize.Send(stats);
                while (handler.Buffer == null)
                {
                    Task.Delay(TimeSpan.FromMilliseconds(1)).Wait();
                }

                // Sent because buffer is full.
                Assert.AreEqual("_e{5,4}:title|test\n", Encoding.UTF8.GetString(handler.Buffer));
            }
        }
    }
}
