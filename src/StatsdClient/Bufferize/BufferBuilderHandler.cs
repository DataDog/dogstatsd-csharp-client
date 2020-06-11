namespace StatsdClient.Bufferize
{
    /// <summary>
    /// BufferBuilderHandler forwards metrics to IStatsSender and update telemetry.
    /// </summary>
    internal class BufferBuilderHandler : IBufferBuilderHandler
    {
        private readonly Telemetry _telemetry;
        private readonly IStatsSender _statsSender;

        public BufferBuilderHandler(
            Telemetry telemetry,
            IStatsSender statsSender)
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