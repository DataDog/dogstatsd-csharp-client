using System;
using StatsdClient.Bufferize;

namespace Tests
{
    class BufferBuilderHandlerMock : IBufferBuilderHandler
    {
        public byte[] Buffer { get; private set; }

        public void Handle(byte[] buffer, int length)
        {
            var newBuffer = new byte[length];
            Array.Copy(buffer, newBuffer, length);

            Buffer = newBuffer;
        }
    }
}