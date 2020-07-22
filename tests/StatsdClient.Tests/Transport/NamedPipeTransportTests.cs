#if OS_WINDOWS
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using StatsdClient.Transport;

namespace Tests
{
    [TestFixture]
    public class NamedPipeTransportTests
    {
        private static int _serverBufferSize = 10;
        private static string _pipeName = "pipeNameTest";
        private static byte[] _buffToSend = new byte[] { 1, 2, 3 };

        [Test]
        public void Send()
        {
            var task = StartServerSingleRead(_buffToSend.Length);

            using (var transport = new NamedPipeTransport(_pipeName))
            {
                Assert.True(transport.Send(_buffToSend, _buffToSend.Length));
            }

            CollectionAssert.AreEqual(task.Result, _buffToSend);
        }

        [Test]
        public void NoTimeout()
        {
            var task = StartServerMultipleReads(4, _serverBufferSize, TimeSpan.FromSeconds(1));

            using (var transport = new NamedPipeTransport(_pipeName, TimeSpan.FromSeconds(2)))
            {
                var buff = new byte[_serverBufferSize];
                for (int i = 0; i < 4; ++i)
                {
                    Assert.True(transport.Send(buff, buff.Length));
                }
            }

            task.Wait();
        }

        [Test]
        public void Timeout()
        {
            var task = StartServerMultipleReads(4, _serverBufferSize, TimeSpan.FromSeconds(1));

            using (var transport = new NamedPipeTransport(_pipeName, TimeSpan.FromMilliseconds(100)))
            {
                var buff = new byte[_serverBufferSize];
                bool bufferSent = true;
                for (int i = 0; i < 4; ++i)
                {
                    bufferSent = bufferSent && transport.Send(buff, buff.Length);
                }

                Assert.False(bufferSent);
            }

            task.Wait();
        }

        [Test]
        public void Reconnection()
        {
            using (var transport = new NamedPipeTransport(_pipeName))
            {
                for (int i = 0; i < 3; i++)
                {
                    var task = StartServerSingleRead(_buffToSend.Length);
                    Assert.True(transport.Send(_buffToSend, _buffToSend.Length));
                    CollectionAssert.AreEqual(task.Result, _buffToSend);
                }
            }
        }

        private Task<byte[]> StartServerSingleRead(int bufferSize)
        {
            return StartServer(server =>
            {
                var readBuffer = new byte[bufferSize];
                server.Read(readBuffer, 0, readBuffer.Length);
                return readBuffer;
            });
        }

        private Task StartServerMultipleReads(int readBufferCount, int bufferSize, TimeSpan durationBetweenRead)
        {
            return StartServer(server =>
            {
                for (int i = 0; i < readBufferCount; ++i)
                {
                    var readBuffer = new byte[bufferSize];
                    server.Read(readBuffer, 0, readBuffer.Length);
                    Thread.Sleep(durationBetweenRead);
                }
                return Task.CompletedTask;
            });
        }

        private Task<T> StartServer<T>(Func<NamedPipeServerStream, T> serverCallback)
        {
            return Task.Run(() =>
            {
                using (var serverStream = new NamedPipeServerStream(
                            _pipeName,
                            PipeDirection.In,
                            1,
                            PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous,
                            _serverBufferSize,
                            0))
                {
                    serverStream.WaitForConnection();
                    var res = serverCallback(serverStream);
                    serverStream.Disconnect();
                    return res;
                }
            });
        }
    }
}
#endif