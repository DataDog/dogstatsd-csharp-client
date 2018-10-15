using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

namespace Tests
{
    namespace Helpers
    {
        // A small UDP server that can be used for testing.
        // Stores a list of the last messages that were received by the server
        // until they are accessed using GetAndClearLastMessages().

        // By design received messages can only be read once. This
        // allows one instance of the listener to be used across
        // multiple tests without risk of the results of previous tests
        // affecting the current one.

        // Intended use:
        // udpListener = new UdpListener(serverName, serverPort);
        // listenThread = new Thread(new ParameterizedThreadStart(udpListener.Listen));
        // listenThread.Start(n);
        // { send n messages to the listener }
        // while(listenThread.IsAlive); // wait for listen thread to receive message or time out
        // List<string> receivedMessage = udpListener.GetAndClearLastMessages()
        // { make sure that the received messages are what was expected }
        public class UdpListener : IDisposable 
        {
            private List<string> _lastReceivedMessages;
            private IPEndPoint _localIpEndPoint;
            private IPEndPoint _senderIpEndPoint;
            private UdpClient _socket;
            private bool _shutdown;

            public UdpListener(string hostname, int port) 
            {
                _lastReceivedMessages = new List<string>();
                _localIpEndPoint = new IPEndPoint(IPAddress.Parse(hostname), port);
                _socket = new UdpClient(_localIpEndPoint);
                _socket.Client.ReceiveTimeout = 1000;
                _senderIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            }

            // Receive messages until it receives count of them or times out. 
            // This call is blocking; you may want to run it in a
            // thread while you send the message.
            public void Listen(object count = null)
            {
                try
                {
                    if (count == null)
                        count = 1;
                    for (int i = 0; i < (int)count; i++)
                    {
                        byte[] lastReceivedBytes = _socket.Receive(ref _senderIpEndPoint);
                        _lastReceivedMessages.Add(Encoding.UTF8.GetString(lastReceivedBytes, 0,
                                                                          lastReceivedBytes.Length));
                    }
                }
                catch (SocketException ex)
                {
                    // If we timeout, stop listening.
                    // If we get another error, propagate it upwards.
                    if (ex.ErrorCode == 10060) // WSAETIMEDOUT; Timeout error
                        return;
                    else
                    throw;
                }
            }

            public void ListenAndWait()
            {                
                while (true)
                {
                    try
                    {
                        byte[] lastReceivedBytes = _socket.Receive(ref _senderIpEndPoint);
                        _lastReceivedMessages.Add(Encoding.UTF8.GetString(lastReceivedBytes, 0,
                            lastReceivedBytes.Length));
                    }
                    catch (SocketException ex)
                    {
                        // If we timeout, check if we are shutting down and exit or listen again
                        if (ex.ErrorCode == 10060) // WSAETIMEDOUT; Timeout error      
                        {
                            if (_shutdown)
                                return;
                        }
                        else
                        {
                            // If we get another error, propagate it upwards.
                            throw;
                        }                    
                    }                
                }                                                   
            }

            public void Shutdown()
            {
                _shutdown = true;
            }

            // Clear and return the message list. Clearing the list allows us to use the 
            // same UdpListener instance for several tests; we never have to worry about a 
            // message received from a previous test giving us a false positive.
            public List<string> GetAndClearLastMessages()
            {
                List<string> messagesToReturn = _lastReceivedMessages;
                _lastReceivedMessages = new List<string>();
                return messagesToReturn;
            }

            public void Dispose() 
            {
                _socket.Close();
            }
        }
    }
}
