using System;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient.Bufferize;
using System.Threading;

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
            var timeout = TimeSpan.FromMilliseconds(500);
            using (var statsBufferize = new StatsBufferize(bufferBuilder, 10, null, timeout))
            {
                var stopWatch = System.Diagnostics.Stopwatch.StartNew();
                statsBufferize.Send("123");
                statsBufferize.Send("4"); // Send "123" as "4" cannot be added to the current buffer.
                while (handler.Buffer == null)
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
                byte[] buffer = Interlocked.Exchange(ref handler.Buffer, null);

                // Sent because buffer is full.
                Assert.Less(stopWatch.ElapsedMilliseconds, timeout.TotalMilliseconds);
                Assert.AreEqual("123", Encoding.UTF8.GetString(buffer));

                while (handler.Buffer == null)
                    await Task.Delay(1);
                // Sent because we wait more than the timeout.
                Assert.Greater(stopWatch.ElapsedMilliseconds, timeout.TotalMilliseconds);
                Assert.AreEqual("4", Encoding.UTF8.GetString(handler.Buffer));
            }
        }
    }
}