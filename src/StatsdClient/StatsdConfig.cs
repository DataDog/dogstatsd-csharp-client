﻿namespace StatsdClient
{
    /// <summary>
    /// The configuration options for DogStatsdService.
    /// </summary>
    public class StatsdConfig
    {
        /// <summary>
        /// The default port for UDP.
        /// </summary>
        public const int DefaultStatsdPort = 8125;

        /// <summary>
        /// The default UDP maximum packet size.
        /// </summary>
        public const int DefaultStatsdMaxUDPPacketSize = 1432;

        /// <summary>
        /// The name of the environment variable defining the global tags to be applied to every metric, event, and service check.
        /// </summary>
        public const string EntityIdEnvVar = "DD_ENTITY_ID";

        /// <summary>
        /// The name of the environment variable defining the port of the targeted StatsD server.
        /// </summary>
        public const string DogStatsdPortEnvVar = "DD_DOGSTATSD_PORT";

        /// <summary>
        /// The name of the environment variable defining the host name of the targeted StatsD server.
        /// </summary>
        public const string AgentHostEnvVar = "DD_AGENT_HOST";

        /// <summary>
        /// The name of the environment variable defining the name of the pipe. INTERNAL USAGE ONLY.
        /// </summary>
        public const string AgentPipeNameEnvVar = "DD_DOGSTATSD_PIPE_NAME";

        /// <summary>
        /// The name of the environment variable defining the service name
        /// </summary>
        public const string ServiceEnvVar = "DD_SERVICE";

        /// <summary>
        /// The name of the environment variable defining the environment name
        /// </summary>
        public const string EnvironmentEnvVar = "DD_ENV";

        /// <summary>
        /// The name of the environment variable defining the version of the service
        /// </summary>
        public const string VersionEnvVar = "DD_VERSION";

        internal const string ServiceTagKey = "service";

        internal const string EnvironmentTagKey = "env";

        internal const string VersionTagKey = "version";

        /// <summary>
        /// Initializes a new instance of the <see cref="StatsdConfig"/> class.
        /// </summary>
        public StatsdConfig()
        {
            StatsdPort = 0;
            StatsdMaxUDPPacketSize = DefaultStatsdMaxUDPPacketSize;
            Advanced = new AdvancedStatsConfig();
        }

        /// <summary>
        /// Gets or sets the host name of the targeted StatsD server.
        /// </summary>
        /// <value>The host name of the targeted StatsD server.</value>
        public string StatsdServerName { get; set; }

        /// <summary>
        /// Gets or sets the name of the pipe. INTERNAL USAGE ONLY.
        /// </summary>
        public string PipeName { get; set; }

        /// <summary>
        /// Gets or sets the port of the targeted StatsD server.
        /// </summary>
        /// <value>The port of the targeted StatsD server.</value>
        public int StatsdPort { get; set; }

        /// <summary>
        /// Gets or sets the maximum UDP packet size.
        /// </summary>
        /// <value>The maximum UDP packet size.</value>
        public int StatsdMaxUDPPacketSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum Unix domain socket packet size.
        /// </summary>
        /// <value>The maximum Unix domain socket packet size.</value>
        public int StatsdMaxUnixDomainSocketPacketSize { get; set; } = 8192;

        /// <summary>
        /// Gets or sets a value indicating whether we truncate the metric if it is too long.
        /// </summary>
        /// <value>The value indicating whether we truncate the metric if it is too long.</value>
        public bool StatsdTruncateIfTooLong { get; set; } = true;

        /// <summary>
        /// Gets or sets the prefix to apply to every metric, event, and service check.
        /// </summary>
        /// <value>The prefix to apply to every metric, event, and service check.</value>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets the advanced configuration.
        /// </summary>
        /// <value>The advanced configuration</value>
        public AdvancedStatsConfig Advanced { get; }

        /// <summary>
        /// Gets or sets the environment tag
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the service version tag
        /// </summary>
        public string ServiceVersion { get; set; }

        /// <summary>
        /// Gets or sets the service name tag
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the global tags to be applied to every metric, event, and service check.
        /// </summary>
        /// <value>The global tags to be applied to every metric, event, and service check.</value>
        public string[] ConstantTags { get; set; }

        /// <summary>
        /// Gets or sets a value defining the client side aggregation config.
        /// If the value is null, the client side aggregation is not enabled.
        /// </summary>
        public ClientSideAggregationConfig ClientSideAggregation { get; set; } = new ClientSideAggregationConfig();

        /// <summary>
        /// Gets or sets a value indicating whether or not origin detection is enabled.
        /// </summary>
        public bool OriginDetection { get; set; } = true;
    }
}
