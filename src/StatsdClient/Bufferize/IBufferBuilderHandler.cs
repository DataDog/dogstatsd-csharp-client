namespace StatsdClient.Bufferize
{
    internal interface IBufferBuilderHandler
    {
        void Handle(byte[] buffer, int length);
    }
}