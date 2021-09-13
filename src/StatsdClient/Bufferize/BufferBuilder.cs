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
        private readonly byte _separator;
        private readonly char[] _charsBuffers;

        public BufferBuilder(
            IBufferBuilderHandler handler,
            int bufferCapacity,
            string separator)
        {
            _buffer = new byte[bufferCapacity];
            _charsBuffers = new char[bufferCapacity];
            _handler = handler;
            var separatorBytes = _encoding.GetBytes(separator);

            if (separatorBytes.Length != 1)
            {
                throw new ArgumentException($"{nameof(separator)} must be converted to a single byte.");
            }

            _separator = separatorBytes[0];
        }

        public int Length { get; private set; }

        public int Capacity => _buffer.Length;

        public static byte[] GetBytes(string message)
        {
            return _encoding.GetBytes(message);
        }

        public void Add(SerializedMetric serializedMetric)
        {
            var length = serializedMetric.CopyToChars(_charsBuffers);

            if (length < 0)
            {
                throw new InvalidOperationException($"The metric size exceeds the internal buffer capacity {_charsBuffers.Length}: {serializedMetric.ToString()}");
            }

            // Heuristic to know if there is enough space without calling `GetByteCount`.
            // Note: GetMaxByteCount(length) >= GetByteCount(length)
            // `+ 1` is for _separator.
            if (Length + 1 + _encoding.GetMaxByteCount(length) > Capacity)
            {
                var byteCount = _encoding.GetByteCount(_charsBuffers, 0, length);

                if (byteCount > Capacity)
                {
                    throw new InvalidOperationException($"The metric size exceeds the buffer capacity {Capacity}: {serializedMetric.ToString()}");
                }

                // For separator
                byteCount++;

                if (Length + byteCount > Capacity)
                {
                    this.HandleBufferAndReset();
                }
            }

            // GetBytes requires the buffer to be big enough otherwise it throws.
            Length += _encoding.GetBytes(_charsBuffers, 0, length, _buffer, Length);

            _buffer[Length] = _separator;
            Length++;
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
