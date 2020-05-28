using System;
using System.Text;

namespace StatsdClient.Bufferize
{
    /// <summary>
    /// Append string values to a fixed size bytes buffer.
    /// </summary>
    internal class BufferBuilder
    {
        private static readonly Encoding _encoding = Encoding.UTF8;
        private readonly IBufferBuilderHandler _handler;
        private readonly byte[] _buffer;
        private readonly byte[] _separator;

        public BufferBuilder(
            IBufferBuilderHandler handler,
            int bufferCapacity,
            string separator)
        {
            _buffer = new byte[bufferCapacity];
            _handler = handler;
            _separator = _encoding.GetBytes(separator);
            if (_separator.Length >= _buffer.Length)
            {
                throw new ArgumentException("separator is greater or equal to the bufferCapacity");
            }
        }

        public int Length { get; private set; }

        public int Capacity => _buffer.Length;

        public static byte[] GetBytes(string message)
        {
            return _encoding.GetBytes(message);
        }

        public bool Add(SerializedMetric serializedMetric)
        {
            var value = serializedMetric.ToString();
            var byteCount = _encoding.GetByteCount(value);

            if (byteCount > Capacity)
            {
                return false;
            }

            if (Length != 0)
            {
                byteCount += _separator.Length;
            }

            if (Length + byteCount > Capacity)
            {
                this.HandleBufferAndReset();
            }

            if (Length != 0)
            {
                Array.Copy(_separator, 0, _buffer, Length, _separator.Length);
                Length += _separator.Length;
            }

            // GetBytes requires the buffer to be big enough otherwise it throws, that is why we use GetByteCount.
            Length += _encoding.GetBytes(value, 0, value.Length, _buffer, Length);
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
