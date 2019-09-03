using System;

namespace StatsdClient
{
    public interface IStatsdUDP : IDisposable
    {
        void Send(string command);
    }
}