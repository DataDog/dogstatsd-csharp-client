namespace StatsdClient.Bufferize
{
    interface IBufferBuilderHandler
    {
        void Handle(byte[] buffer, int length);
    }
}