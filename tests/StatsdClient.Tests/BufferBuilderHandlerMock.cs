using System;
using StatsdClient;

namespace Tests
{
    class BufferBuilderHandlerMock : IBufferBuilderHandler
    {
        public byte[] Buffer { get; private set; }
        public void Handle(byte[] buffer, int length)
        {
            Buffer = new byte[length];
            Array.Copy(buffer, Buffer, length);
        }
    }
}