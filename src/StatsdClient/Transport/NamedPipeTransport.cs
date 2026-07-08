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
        private readonly TimeSpan _connectionCooldown;
        private readonly System.Diagnostics.Stopwatch _sendFailureTimer = new System.Diagnostics.Stopwatch();
        private readonly object _lock = new object();

        private byte[] _internalbuffer = Array.Empty<byte>();

        public NamedPipeTransport(string pipeName, TimeSpan? timeout = null, TimeSpan? connectionCooldown = null)
        {
            _namedPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            _timeout = timeout ?? TimeSpan.FromSeconds(2);
            _connectionCooldown = connectionCooldown ?? TimeSpan.FromSeconds(5);
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
            // After a failed send, fail fast until the cooldown elapses instead of blocking
            // again on Connect or Write. Otherwise every send re-blocks for the full timeout
            // while the pipe is unavailable, starving the worker and (through the telemetry
            // timer) inflating the thread count. The gate sits ahead of the connection check
            // so a connected-but-stalled pipe (writes timing out) is throttled too.
            if (_sendFailureTimer.IsRunning && _sendFailureTimer.Elapsed < _connectionCooldown)
            {
                return false;
            }

            try
            {
                if (!_namedPipe.IsConnected)
                {
                    _namedPipe.Connect((int)_timeout.TotalMilliseconds);
                }
            }
            catch (TimeoutException)
            {
                _sendFailureTimer.Restart();
                return false;
            }

            try
            {
                // WriteAsync overload with a CancellationToken instance seems to not work.
                if (_namedPipe.WriteAsync(buffer, 0, length).Wait(_timeout))
                {
                    _sendFailureTimer.Reset();
                    return true;
                }

                // The write timed out. The pipe still reports connected, so cool down to
                // avoid re-blocking for the full timeout on every subsequent send.
                _sendFailureTimer.Restart();
                return false;
            }
            catch (IOException)
            {
            }
            catch (AggregateException e) when (e.InnerException is IOException)
            {
                // dotnet6.0 raises AggregateException when an IOException occurs.
            }

            // When the server disconnects, IOException is raised with the message "Pipe is broken".
            // In this case, we try to reconnect once.
            if (allowRetry)
            {
                return SendBuffer(buffer, length, allowRetry: false);
            }

            _sendFailureTimer.Restart();
            return false;
        }
    }
}
