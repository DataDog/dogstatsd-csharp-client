using System.Text;
using Moq;
using StatsdClient.Bufferize;

namespace StatsdClient.Tests.Aggregator
{
    /// <summary>
    /// Mock for `IBufferBuilderHandler`.
    /// </summary>
    internal class BufferBuilderHandlerMock
    {
        private readonly Mock<IBufferBuilderHandler> _handler = new Mock<IBufferBuilderHandler>();

        public BufferBuilderHandlerMock()
        {
            _handler.Setup(h => h.Handle(It.IsAny<byte[]>(), It.IsAny<int>())).Callback<byte[], int>((buffer, len) =>
            {
                Value = Encoding.UTF8.GetString(buffer, 0, len);
            });
        }

        public string Value { get; private set; }

        public IBufferBuilderHandler Object => _handler.Object;
    }
}