using System.Text;

namespace StatsdClient
{
    /// <summary>
    /// Append string values to a fixed size bytes buffer.
    /// </summary>
    class BufferBuilder
    {
        readonly IBufferBuilderHandler _handler;
        readonly byte[] _buffer;

        public BufferBuilder(IBufferBuilderHandler handler, int bufferCapacity)
        {
            _buffer = new byte[bufferCapacity];
            _handler = handler;
        }

        public int Length { get; private set; }
        public int Capacity { get { return _buffer.Length; } }

        public bool Add(string value)
        {
            var byteCount = Encoding.UTF8.GetByteCount(value);
            if (byteCount > _buffer.Length)
                return false;

            if (Length + byteCount > _buffer.Length)
                this.HandleBufferAndReset();

            // GetBytes requires the buffer to be big enough, that is why we use GetByteCount.
            Length += Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, Length);
            return true;
        }

        public void HandleBufferAndReset()
        {
            if (Length > 0)
            {
                _handler.Handle(_buffer, Length);
                Length = 0;
            }
        }
    }
}
