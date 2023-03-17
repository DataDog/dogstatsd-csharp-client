using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace StatsdClient.Transport
{
    internal class NamedPipeTransport : ITransport
    {
        private readonly NamedPipeClientStream _namedPipe;
        private readonly TimeSpan _timeout;
        private readonly object _lock = new object();

        private byte[] _internalbuffer = Array.Empty<byte>();

        public NamedPipeTransport(string pipeName, TimeSpan? timeout = null)
        {
            _namedPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            _timeout = timeout ?? TimeSpan.FromSeconds(2);
        }

        public TransportType TransportType => TransportType.NamedPipe;

        public string TelemetryClientTransport => "named_pipe";

        public bool Send(byte[] buffer, int length)
        {
            lock (_lock)
            {
                if (_internalbuffer.Length < length + 1)
                {
                    _internalbuffer = new byte[length + 1];
                }

                // Server expects messages to end with '\n'
                Array.Copy(buffer, 0, _internalbuffer, 0, length);
                _internalbuffer[length] = (byte)'\n';

                return SendBuffer(_internalbuffer, length + 1, allowRetry: true);
            }
        }

        public void Dispose()
        {
            _namedPipe.Dispose();
        }

        private bool SendBuffer(byte[] buffer, int length, bool allowRetry)
        {
            try
            {
                if (!_namedPipe.IsConnected)
                {
                    _namedPipe.Connect((int)_timeout.TotalMilliseconds);
                }
            }
            catch (TimeoutException)
            {
                return false;
            }

            bool ioException = false;
            try
            {
                // WriteAsync overload with a CancellationToken instance seems to not work.
                _namedPipe.WriteAsync(buffer, 0, length).Wait(_timeout);
                return true;
            }
            catch (OperationCanceledException) { }
            catch (IOException)
            {
                ioException = true;
            }
            catch (AggregateException e)
            {
                // dotnet6.0 raises AggregateException when an IOException occurs.
                e.Handle(ex =>
                {
                    if (ex is IOException)
                    {
                        ioException = true;
                        return true;
                    }
                    return ex is OperationCanceledException;
                });
            }

            // When the server disconnects, IOException is raised with the message "Pipe is broken".
            // In this case, we try to reconnect once.
            if (ioException && allowRetry)
            {
                return SendBuffer(buffer, length, allowRetry: false);
            }

            return false;
        }
    }
}
