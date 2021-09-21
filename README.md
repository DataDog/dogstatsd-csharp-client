# DogStatsD for C#

[![Build status](https://ci.appveyor.com/api/projects/status/bg8e39b5f9iiavvj/branch/master?svg=true)](https://ci.appveyor.com/project/Datadog/dogstatsd-csharp-client/branch/master)

A C# [DogStatsD](https://docs.datadoghq.com/developers/dogstatsd/?code-lang=.NET) client. DogStatsD is an extension of the [StatsD](http://codeascraft.com/2011/02/15/measure-anything-measure-everything/) metric server for [Datadog](http://datadoghq.com).

See [CHANGELOG](CHANGELOG.md) for details.

## Installation

Grab the [package from NuGet](https://nuget.org/packages/DogStatsD-CSharp-Client/), or get the source from here and build it yourself.

### Platforms

DogStatsD-CSharp-Client supports the following platforms:
* .NET Standard 1.3 or greater
* .NET Core 1.0 or greater
* .NET Framework 4.5.1 or greater

## Configuration

At start of your application, configure an instance of `DogStatsdService` class like this:

```csharp
// The code is located under the StatsdClient namespace
using StatsdClient;

// ...

var dogstatsdConfig = new StatsdConfig
{
    StatsdServerName = "127.0.0.1",
    StatsdPort = 8125,
};

using (var service = new DogStatsdService())
{
    service.Configure(dogstatsdConfig);
}
```

See the full list of available [DogStatsD Client instantiation parameters](https://docs.datadoghq.com/developers/dogstatsd/?code-lang=.NET#client-instantiation-parameters).

Supported environment variables:

* The client can use the `DD_AGENT_HOST` and (optionally) the `DD_DOGSTATSD_PORT` environment variables to build the target address if the `StatsdServerName` and/or `StatsdPort` parameters are empty.
* If the `DD_ENTITY_ID` enviroment variable is found, its value will be injected as a global `dd.internal.entity_id` tag. This tag will be used by the Datadog Agent to insert container tags to the metrics.

Where `StatsdServerName` is the hostname or address of the StatsD server, `StatsdPort` is the optional DogStatsD port number, and `Prefix` is an optional string that is prepended to all metrics.

## Usage via the DogStatsdService class or the static DogStatsd class.

For usage of DogStatsD metrics, events, and Service Checks the Agent must be [running and available](https://docs.datadoghq.com/developers/dogstatsd/?code-lang=.NET#setup).

Here is an example to submit different kinds of metrics with `DogStatsdService`.
```csharp
// The code is located under the StatsdClient namespace
using StatsdClient;

// ...

var dogstatsdConfig = new StatsdConfig
{
    StatsdServerName = "127.0.0.1",
    StatsdPort = 8125,
};

using (var service = new DogStatsdService())
{
    service.Configure(dogstatsdConfig);
    service.Increment("example_metric.increment", tags: new[] { "environment:dev" });
    service.Decrement("example_metric.decrement", tags: new[] { "environment:dev" });
    service.Counter("example_metric.count", 2, tags: new[] { "environment:dev" });

    var random = new Random(0);

    for (int i = 0; i < 10; i++)
    {
        service.Gauge("example_metric.gauge", i, tags: new[] { "environment:dev" });
        service.Set("example_metric.set", i, tags: new[] { "environment:dev" });
        service.Histogram("example_metric.histogram", random.Next(20), tags: new[] { "environment:dev" });
        System.Threading.Thread.Sleep(random.Next(10000));
    }
}  
```

Here is another example to submit different kinds of metrics with `DogStatsd`.
```csharp
// The code is located under the StatsdClient namespace
using StatsdClient;

// ...

var dogstatsdConfig = new StatsdConfig
{
    StatsdServerName = "127.0.0.1",
    StatsdPort = 8125,
};

DogStatsd.Configure(dogstatsdConfig);
DogStatsd.Increment("example_metric.increment", tags: new[] { "environment:dev" });
DogStatsd.Decrement("example_metric.decrement", tags: new[] { "environment:dev" });
DogStatsd.Counter("example_metric.count", 2, tags: new[] { "environment:dev" });

var random = new Random(0);

for (int i = 0; i < 10; i++)
{
    DogStatsd.Gauge("example_metric.gauge", i, tags: new[] { "environment:dev" });
    DogStatsd.Set("example_metric.set", i, tags: new[] { "environment:dev" });
    DogStatsd.Histogram("example_metric.histogram", random.Next(20), tags: new[] { "environment:dev" });
    System.Threading.Thread.Sleep(random.Next(10000));
}
  
DogStatsd.Dispose(); // Flush all metrics not yet sent
```

### Metrics

After the client is created, you can start sending custom metrics to Datadog. See the dedicated [Metric Submission: DogStatsD documentation](https://docs.datadoghq.com/metrics/dogstatsd_metrics_submission/?code-lang=.NET) to see how to submit all supported metric types to Datadog with working code examples:

* [Submit a COUNT metric](https://docs.datadoghq.com/metrics/dogstatsd_metrics_submission/?code-lang=.NET#count).
* [Submit a GAUGE metric](https://docs.datadoghq.com/metrics/dogstatsd_metrics_submission/?code-lang=.NET#gauge).
* [Submit a SET metric](https://docs.datadoghq.com/metrics/dogstatsd_metrics_submission/?code-lang=.NET#set)
* [Submit a HISTOGRAM metric](https://docs.datadoghq.com/metrics/dogstatsd_metrics_submission/?code-lang=.NET#histogram)
* [Submit a DISTRIBUTION metric](https://docs.datadoghq.com/metrics/dogstatsd_metrics_submission/?code-lang=.NET#distribution)

Some options are suppported when submitting metrics, like [applying a Sample Rate to your metrics](https://docs.datadoghq.com/metrics/dogstatsd_metrics_submission/?code-lang=.NET#metric-submission-options) or [Tagging your metrics with your custom Tags](https://docs.datadoghq.com/metrics/dogstatsd_metrics_submission/?code-lang=.NET#metric-tagging).

### Events

After the client is created, you can start sending events to your Datadog Event Stream. See the dedicated [Event Submission: DogStatsD documentation](https://docs.datadoghq.com/developers/events/dogstatsd/?code-lang=.NET) to see how to submit an event to Datadog Event Stream.

### Service Checks

After the client is created, you can start sending Service Checks to Datadog. See the dedicated [Service Check Submission: DogStatsD documentation](https://docs.datadoghq.com/developers/service_checks/dogstatsd_service_checks_submission/?code-lang=.NET) to see how to submit a Service Check to Datadog.


## Usage via the Statsd class

`Statsd` has been removed in v`6.0.0` because it is not thread safe and not efficient. Use `DogStatsdService` or `DogStatsd` instead:
* Methods from `DogStatsdService` and `DogStatsd` do not block when called except for `Flush` and `Dispose`.
* `DogStatsdService` and `DogStatsd` batch automatically several metrics in one datagram.

## Unix domain socket support

The version 6 (and above) of the Agent accepts packets through a Unix Socket datagram connection. Details about the advantages of using UDS over UDP are available in the [Datadog DogStatsD Unix Socket documentation](https://docs.datadoghq.com/developers/dogstatsd/unix_socket/).

You can use unix domain socket protocol by setting `StatsdServerName` property to `unix://YOUR_FULL_PATH`, for example `unix:///tmp/dsd.socket`. Note that there are three `/` as the path of the socket is `/tmp/dsd.socket`.

``` C#
var dogstatsdConfig = new StatsdConfig
{    
    StatsdServerName = "unix:///tmp/dsd.socket"  
};
```

The property `StatsdMaxUnixDomainSocketPacketSize` of `StatsdConfig` defines the maximum size of the payload. Values higher than 8196 bytes are ignored.

**The feature is not supported on Windows platform**.
Windows has support for [unix domain socket](https://devblogs.microsoft.com/commandline/af_unix-comes-to-windows/), but not for unix domain socket of type Dgram (`SocketType.Dgram`). 

On MacOS Mojave, setting more than `2048` bytes for `StatsdMaxUnixDomainSocketPacketSize` is experimental.

## Testing

1. Restore packages
  ```
  dotnet restore
  ```
2. Run the tests
  ```
  dotnet test tests/StatsdClient.Tests/
  ```

## Feedback

To suggest a feature, report a bug, or general discussion, [create a new issue](https://github.com/DataDog/statsd-csharp-client/issues) in the Github repo.

## Credits

`dogstatsd-csharp-client` is forked from Goncalo Pereira's [original StatsD client](https://github.com/goncalopereira/statsd-csharp-client).

Copyright (c) 2012 Goncalo Pereira and all contributors. See MIT-LICENCE.md for further details.

Thanks to Goncalo Pereira, Anthony Steele, Darrell Mozingo, Antony Denyer, and Tim Skauge for their contributions to the original client.
