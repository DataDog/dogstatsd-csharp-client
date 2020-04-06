namespace StatsdClient.Bufferize
{
    /// <summary>
    /// BufferBuilderHandler forwards metrics to StatsSender and update telemetry.
    /// </summary>
    internal class BufferBuilderHandler : IBufferBuilderHandler
    {
        private readonly Telemetry _telemetry;
        private readonly StatsSender _statsSender;

        public BufferBuilderHandler(
            Telemetry telemetry,
            StatsSender statsSender)
        {
            _telemetry = telemetry;
            _statsSender = statsSender;
        }

        public void Handle(byte[] buffer, int length)
        {
            if (_statsSender.Send(buffer, length))
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