using System.Threading.Tasks;

namespace StatsdClient
{
    public interface IStatsdUDP
    {
        void Send(string command);
        Task SendAsync(string command);
    }
}
