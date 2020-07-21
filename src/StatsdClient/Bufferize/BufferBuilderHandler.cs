using StatsdClient.Transport;

namespace StatsdClient.Bufferize
{
    /// <summary>
    /// BufferBuilderHandler forwards metrics to ITransport and update telemetry.
    /// </summary>
    internal class BufferBuilderHandler : IBufferBuilderHandler
    {
        private readonly Telemetry _telemetry;
        private readonly ITransport _transport;

        public BufferBuilderHandler(
            Telemetry telemetry,
            ITransport transport)
        {
            _telemetry = telemetry;
            _transport = transport;
        }

        public void Handle(byte[] buffer, int length)
        {
            if (_transport.Send(buffer, length))
            {
                _telemetry.OnPacketSent(length);
            }
            else
            {
                _telemetry.OnPacketDropped(length);
            }
        }
    }
}