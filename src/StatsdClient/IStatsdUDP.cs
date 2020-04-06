using System;
using System.Threading.Tasks;

namespace StatsdClient
{
    [ObsoleteAttribute("This interface will become private in a future release.")]
    public interface IStatsdUDP
    {
        void Send(string command);

        Task SendAsync(string command);
    }
}
