using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Tests.Utils
{
    internal class NamedPipeServer : AbstractServer
    {
        private readonly NamedPipeServerStream _pipeServer;
        private readonly object _lock = new object();
        private readonly TimeSpan _timeout;

        public NamedPipeServer(string pipeName, int bufferSize, TimeSpan timeout)
        {
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _timeout = timeout;
            Start(bufferSize);
        }

        public override void Dispose()
        {
            base.Dispose();
            lock (_lock)
            {
                if (_pipeServer.IsConnected)
                {
                    _pipeServer.Disconnect();
                }

                _pipeServer.Dispose();
            }
        }

        protected override int? Read(byte[] buffer)
        {
            lock (_lock)
            {
                if (!_pipeServer.IsConnected)
                {
                    try
                    {
                        _pipeServer.WaitForConnection();
                    }
                    catch (IOException)
                    {
                        return null;
                    }
                }

                var cts = new CancellationTokenSource();
                cts.CancelAfter(_timeout);
                try
                {
                    var task = _pipeServer.ReadAsync(buffer, 0, buffer.Length);
                    // Overload of ReadAsync with CancellationToken does not seem to work.
                    task.Wait(cts.Token);
                    return task.Result;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }
        }
    }
}