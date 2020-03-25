using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StatsdClient
{
    public class StatsdUDP : IDisposable, IStatsdUDP
    {
        private int MaxUDPPacketSize { get; set; } // In bytes; default is MetricsConfig.DefaultStatsdMaxUDPPacketSize.
        // Set to zero for no limit.
        public IPEndPoint IPEndpoint { get; private set; }
        private Socket UDPSocket { get; set; }
        private string Name { get; set; }
        private int Port { get; set; }

        public StatsdUDP(int maxUDPPacketSize = StatsdConfig.DefaultStatsdMaxUDPPacketSize)
        : this(GetHostNameFromEnvVar(),GetPortFromEnvVar(StatsdConfig.DefaultStatsdPort),maxUDPPacketSize)
        {
        }
        public StatsdUDP(string name = null, int port = 0, int maxUDPPacketSize = StatsdConfig.DefaultStatsdMaxUDPPacketSize)
        {
            Port = port;
            if (Port == 0)
            {
                Port = GetPortFromEnvVar(StatsdConfig.DefaultStatsdPort);
            }
            Name = name;
            if (string.IsNullOrEmpty(Name))
            {
                Name = GetHostNameFromEnvVar();
            }

            MaxUDPPacketSize = maxUDPPacketSize;

            UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var ipAddress = GetIpv4Address(Name);

            IPEndpoint = new IPEndPoint(ipAddress, Port);
        }

        private static string GetHostNameFromEnvVar()
        {
            return Environment.GetEnvironmentVariable(StatsdConfig.DD_AGENT_HOST_ENV_VAR);
        }

        private static int GetPortFromEnvVar(int defaultValue)
        {
            int port = defaultValue;
            string portString = Environment.GetEnvironmentVariable(StatsdConfig.DD_DOGSTATSD_PORT_ENV_VAR);
            if (portString != null)
            {
                try
                {
                    port = Int32.Parse(portString);
                }
                catch (FormatException)
                {
                    throw new ArgumentException("Environment Variable 'DD_DOGSTATSD_PORT' bad format");
                }
            }
            return port;
        }
        internal static IPAddress GetIpv4Address(string name)
        {
            IPAddress ipAddress;
            bool isValidIPAddress = IPAddress.TryParse(name, out ipAddress);

            if (!isValidIPAddress)
            {
                ipAddress = null;
#if NET451
                IPAddress[] addressList = Dns.GetHostEntry(name).AddressList;
#else
                IPAddress[] addressList = Dns.GetHostEntryAsync(name).Result.AddressList;
#endif
                //The IPv4 address is usually the last one, but not always
                for(int positionToTest = addressList.Length - 1; positionToTest >= 0; --positionToTest)
                {
                    if(addressList[positionToTest].AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = addressList[positionToTest];
                        break;
                    }
                }

                //If no IPV4 address is found, throw an exception here, rather than letting it get squashed when encountered at sendtime
                if(ipAddress == null)
                    throw new SocketException((int)SocketError.AddressFamilyNotSupported);
            }
            return ipAddress;
        }

        public void Send(string command)
        {
            SocketSender.Send(MaxUDPPacketSize, command, 
                encodedCommand => UDPSocket.SendTo(encodedCommand, encodedCommand.Length, SocketFlags.None, IPEndpoint));

        }

        public Task SendAsync(string command)
        {
            return SocketSender.SendAsync(
                IPEndpoint,
                UDPSocket,
                MaxUDPPacketSize,
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(command)));
        }

        public void Dispose()
        {
            UDPSocket.Dispose();
        }
    }
}
