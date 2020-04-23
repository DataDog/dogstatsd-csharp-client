using System;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient.Bufferize;

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
                statsBufferize.Send("1");
                while (handler.Buffer == null)
                    Task.Delay(TimeSpan.FromMilliseconds(1)).Wait();

                // Sent because buffer is full.                
                Assert.AreEqual("1", Encoding.UTF8.GetString(handler.Buffer));
            }
        }
    }
}