#if OS_WINDOWS
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
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

        [Test]
        public void ConnectionCooldownAvoidsRepeatedBlocking()
        {
            var connectTimeout = TimeSpan.FromSeconds(1);
            var cooldown = TimeSpan.FromSeconds(10);

            // No server listens on this pipe, so Connect blocks for the full timeout and fails.
            using (var transport = new NamedPipeTransport("cooldownPipeNameTest-" + Guid.NewGuid(), connectTimeout, cooldown))
            {
                var stopwatch = Stopwatch.StartNew();
                Assert.False(transport.Send(_buffToSend, _buffToSend.Length));

                stopwatch.Restart();
                for (int i = 0; i < 5; i++)
                {
                    Assert.False(transport.Send(_buffToSend, _buffToSend.Length));
                }

                var cooldownSendsDuration = stopwatch.Elapsed;

                // Subsequent sends within the cooldown must fail fast instead of each blocking
                // on Connect again. Assert the whole batch completes in a small fraction of a
                // single connect timeout so the test fails if sends keep blocking, without
                // relying on Connect actually blocking the full timeout (some Windows/.NET
                // combinations fail a missing pipe quickly).
                Assert.That(cooldownSendsDuration, Is.LessThan(TimeSpan.FromMilliseconds(connectTimeout.TotalMilliseconds * 0.5)));
            }
        }

        [Test]
        public void WriteTimeoutTriggersCooldown()
        {
            var timeout = TimeSpan.FromSeconds(1);
            var cooldown = TimeSpan.FromSeconds(10);
            var pipeName = "writeCooldownPipeNameTest-" + Guid.NewGuid();
            var releaseServer = new ManualResetEventSlim(false);

            // The server connects but never reads, so its buffer fills and the client's
            // write stalls until it times out.
            var serverTask = Task.Run(() =>
            {
                using (var serverStream = new NamedPipeServerStream(
                            pipeName,
                            PipeDirection.In,
                            1,
                            PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous,
                            _serverBufferSize,
                            0))
                {
                    serverStream.WaitForConnection();
                    releaseServer.Wait();
                }
            });

            try
            {
                using (var transport = new NamedPipeTransport(pipeName, timeout, cooldown))
                {
                    // The OS may round the server's requested buffer size up, so use a payload
                    // far larger than the requested buffer to make the stalled write reliable.
                    var buff = new byte[_serverBufferSize * 1000];

                    var stopwatch = Stopwatch.StartNew();
                    Assert.False(transport.Send(buff, buff.Length));

                    stopwatch.Restart();
                    for (int i = 0; i < 5; i++)
                    {
                        Assert.False(transport.Send(buff, buff.Length));
                    }

                    var cooldownSendsDuration = stopwatch.Elapsed;

                    // Subsequent sends within the cooldown must fail fast instead of each blocking
                    // on the stalled write again. Assert the whole batch completes in a small
                    // fraction of a single write timeout so the test fails if sends keep blocking.
                    Assert.That(cooldownSendsDuration, Is.LessThan(TimeSpan.FromMilliseconds(timeout.TotalMilliseconds * 0.5)));
                }
            }
            finally
            {
                releaseServer.Set();
                Assert.True(serverTask.Wait(TimeSpan.FromSeconds(5)), "server task did not complete");
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