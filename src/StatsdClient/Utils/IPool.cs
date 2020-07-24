namespace StatsdClient.Utils
{
    internal interface IPool
    {
        void Enqueue(object obj);
    }
}