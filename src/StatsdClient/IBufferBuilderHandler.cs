namespace StatsdClient
{
    public interface IBufferBuilderHandler
    {
        void Handle(byte[] buffer, int length);
    }
}