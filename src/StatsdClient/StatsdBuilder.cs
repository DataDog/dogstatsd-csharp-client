using System;
using System.Collections.Generic;

namespace StatsdClient
{
    class StatsdBuilder : IDisposable
    {
        readonly List<IDisposable> _disposables = new List<IDisposable>();

        public Statsd BuildStats(StatsdConfig config)
        {
            var statsdServerName = !string.IsNullOrEmpty(config.StatsdServerName)
                    ? config.StatsdServerName
                    : Environment.GetEnvironmentVariable(StatsdConfig.DD_AGENT_HOST_ENV_VAR);

            if (string.IsNullOrEmpty(statsdServerName))
            {
                throw new ArgumentNullException(
                    $"{nameof(config)}.{nameof(config.StatsdServerName)} and" 
                    + $" {StatsdConfig.DD_AGENT_HOST_ENV_VAR} environment variable not set");
            }

            var statsdSender = CreateSender(config, statsdServerName);
            var statsD = new Statsd(statsdSender,
                                    new RandomGenerator(),
                                    new StopWatchFactory(),
                                    "",
                                    config.ConstantTags);
            statsD.TruncateIfTooLong = config.StatsdTruncateIfTooLong;
            return statsD;
        }

        IStatsdUDP CreateSender(StatsdConfig config, string statsdServerName)
        {
            if (statsdServerName.StartsWith(StatsdUnixDomainSocket.UnixDomainSocketPrefix))
            {
                var statsdUds = new StatsdUnixDomainSocket(statsdServerName, config.StatsdMaxUnixDomainSocketPacketSize);
                _disposables.Add(statsdUds);
                return statsdUds;
            }

            var statsUdp = new StatsdUDP(config.StatsdServerName, config.StatsdPort, config.StatsdMaxUDPPacketSize);
            _disposables.Add(statsUdp);
            return statsUdp;
        }

        public void Dispose()
        {
            foreach (var d in _disposables)
                d.Dispose();
            _disposables.Clear();
        }
    }
}
