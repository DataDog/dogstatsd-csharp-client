using System;
using System.Text;
using StatsdClient.Bufferize;

namespace Tests
{
    internal class BufferBuilderHandlerMock : IBufferBuilderHandler
    {
        public byte[] Buffer { get; private set; }

        public void Handle(byte[] buffer, int length)
        {
            var newBuffer = new byte[length];
            Array.Copy(buffer, newBuffer, length);

            Buffer = newBuffer;
        }

        public string BufferToString()
        {
            if (Buffer == null)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(Buffer, 0, Buffer.Length);
        }

        public void Reset()
        {
            Buffer = null;
        }
    }
}