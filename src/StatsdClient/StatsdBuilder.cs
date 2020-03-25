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

            if (!string.IsNullOrEmpty(statsdServerName))
            {
                IStatsdUDP statsdSender;

                if (statsdServerName.StartsWith(StatsdUnixDomainSocket.UnixDomainSocketPrefix))
                {
                    var statsdUds = new StatsdUnixDomainSocket(statsdServerName, config.StatsdMaxUnixDomainSocketPacketSize);
                    _disposables.Add(statsdUds);
                    statsdSender = statsdUds;
                }
                else
                {
                    var statsUdp = new StatsdUDP(config.StatsdServerName, config.StatsdPort, config.StatsdMaxUDPPacketSize);
                    _disposables.Add(statsUdp);
                    statsdSender = statsUdp;
                }
                var statsD = new Statsd(statsdSender, new RandomGenerator(), new StopWatchFactory(), "", config.ConstantTags);
                statsD.TruncateIfTooLong = config.StatsdTruncateIfTooLong;
                return statsD;
            }
            else
            {
                throw new ArgumentNullException("config.StatsdServername and DD_AGENT_HOST environment variable not set");
            }
        }

        public void Dispose()
        {
            foreach (var d in _disposables)
                d.Dispose();
            _disposables.Clear();
        }
    }
}
