using System;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;

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
            var timeout = TimeSpan.FromMilliseconds(300);
            var statsBufferize = new StatsBufferize(bufferBuilder, 10, null, timeout);

            statsBufferize.Send("123");
            statsBufferize.Send("4");
            await Task.Delay(timeout.Multiply(0.5));
            // Sent because buffer is full.
            Assert.AreEqual(Encoding.UTF8.GetBytes("123"), handler.Buffer);

            // Sent because we wait more than the timeout.
            await Task.Delay(timeout.Multiply(2));
            Assert.AreEqual(Encoding.UTF8.GetBytes("4"), handler.Buffer);
        }
    }
}