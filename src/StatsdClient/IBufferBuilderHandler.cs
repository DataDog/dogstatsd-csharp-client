namespace StatsdClient
{
    interface IBufferBuilderHandler
    {
        void Handle(byte[] buffer, int length);
    }
}