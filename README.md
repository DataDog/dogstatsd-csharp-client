# DogStatsD for C#

[![Build status](https://ci.appveyor.com/api/projects/status/bg8e39b5f9iiavvj/branch/master?svg=true)](https://ci.appveyor.com/project/Datadog/dogstatsd-csharp-client/branch/master)

A C# [DogStatsD](https://docs.datadoghq.com/developers/dogstatsd/?tab=net) client. DogStatsD is an extension of the [StatsD](http://codeascraft.com/2011/02/15/measure-anything-measure-everything/) metric server for [Datadog](http://datadoghq.com).

See [CHANGELOG](CHANGELOG.md) for details.

## Installation

Grab the [package from NuGet](https://nuget.org/packages/DogStatsD-CSharp-Client/), or get the source from here and build it yourself.

### Platforms

DogStatsD-CSharp-Client supports the following platforms:

* .NET Standard 1.3
* .NET Standard 1.6
* .NET Core Application 1.1
* .NET Core Application 2.0
* .NET Framework 4.5.1
* .NET Framework 4.6.1

## Configuration

At start of your app, configure the `DogStatsd` class like this:

```csharp
// The code is located under the StatsdClient namespace
using StatsdClient;

// ...

var dogstatsdConfig = new StatsdConfig
{
    StatsdServerName = "127.0.0.1",
    StatsdPort = 8125,
};

StatsdClient.DogStatsd.Configure(dogstatsdConfig);
```

See the full list of available [DogStatsD Client instantiation parameters](https://docs.datadoghq.com/developers/dogstatsd/?tab=net#client-instantiation-parameters).

Supported environment variables:

* The client can use the `DD_AGENT_HOST` and (optionally) the `DD_DOGSTATSD_PORT` environment variables to build the target address if the `addr` parameter is empty.
* If the `DD_ENTITY_ID` enviroment variable is found, its value will be injected as a global `dd.internal.entity_id` tag. This tag will be used by the Datadog Agent to insert container tags to the metrics.

Where `StatsdServerName` is the hostname or address of the StatsD server, `StatsdPort` is the optional DogStatsD port number, and `Prefix` is an optional string that is prepended to all metrics.

## Usage via the static DogStatsd class.

For usage of DogStatsD metrics, events, and Service Checks the Agent must be [running and available](https://docs.datadoghq.com/developers/dogstatsd/?tab=net#setup).

### Metrics

After the client is created, you can start sending custom metrics to Datadog. See the dedicated [Metric Submission: DogStatsD documentation](https://docs.datadoghq.com/developers/metrics/dogstatsd_metrics_submission/?tab=net) to see how to submit all supported metric types to Datadog with working code examples:

* [Submit a COUNT metric](https://docs.datadoghq.com/developers/metrics/dogstatsd_metrics_submission/?tab=net#count).
* [Submit a GAUGE metric](https://docs.datadoghq.com/developers/metrics/dogstatsd_metrics_submission/?tab=net#gauge).
* [Submit a SET metric](https://docs.datadoghq.com/developers/metrics/dogstatsd_metrics_submission/?tab=net#set)
* [Submit a HISTOGRAM metric](https://docs.datadoghq.com/developers/metrics/dogstatsd_metrics_submission/?tab=net#histogram)
* [Submit a DISTRIBUTION metric](https://docs.datadoghq.com/developers/metrics/dogstatsd_metrics_submission/?tab=net#distribution)

Some options are suppported when submitting metrics, like [applying a Sample Rate to your metrics](https://docs.datadoghq.com/developers/metrics/dogstatsd_metrics_submission/?tab=net#metric-submission-options) or [Tagging your metrics with your custom Tags](https://docs.datadoghq.com/developers/metrics/dogstatsd_metrics_submission/?tab=net#metric-tagging).

### Events

After the client is created, you can start sending events to your Datadog Event Stream. See the dedicated [Event Submission: DogStatsD documentation](https://docs.datadoghq.com/developers/events/dogstatsd/?tab=net) to see how to submit an event to Datadog Event Stream.

### Service Checks

After the client is created, you can start sending Service Checks to Datadog. See the dedicated [Service Check Submission: DogStatsD documentation](https://docs.datadoghq.com/developers/service_checks/dogstatsd_service_checks_submission/?tab=net) to see how to submit a Service Check to Datadog.


## Usage via the Statsd class:

In most cases, the static DogStatsd class is probably better to use. However, the Statsd is useful when you want to queue up a number of metrics/events to be sent in one UDP message (via the `Add` method).

``` C#
// The code is located under the StatsdClient namespace
using StatsdClient;

// ...

// NB: StatsdUDP is IDisposable and if not disposed, will leak resources
StatsdUDP udp = new StatsdUDP(HOSTNAME, PORT);
using (udp)
{
  Statsd s = new Statsd(udp);

  // Incrementing a counter by 1
  s.Send<Statsd.Counting,int>("stat-name", 1);

  // Recording a gauge
  s.Send<Statsd.Gauge,double>("stat-name", 5.5);

  // Sampling a histogram
  s.Send<Statsd.Histogram,int>("stat-name", 1);

  // Measure a distribution of values
  s.Send<Statsd.Distribution,int>("stat-name", 1);

  // Send elements to a set
  s.Send<Statsd.Set,int>("stat-name", 1);
  s.Send<Statsd.Set,string>("stat-name", "stat-value");

  // Send a timer
  s.Send<Statsd.Timing,double>("stat-name", 3.1337);

  // Time a method
  s.Send(() => MethodToTime(), "stat-name");

  // See note below on how exceptions in timed methods are handled

  // All types have optional sample rates and tags:
  s.Send<Statsd.Counting,int>("stat-name", 1, sampleRate: 1/10, tags: new[] {"tag1:true", "tag2"});

  // Send an event
  s.Send("title", "content");

  // You can add combinations of messages which will be sent in one go:
  s.Add<Statsd.Counting,int>("stat-name", 1);
  s.Add<Statsd.Timing,int>("stat-name", 5, sampleRate: 1/10);
  s.Add("event title", "content", priority: "low");
  s.Send(); // message will contain counter and will contain timer 10% of the time
  // All previous commands will be flushed after any Send
  // Any Adds will be ignored if using a Send directly
  s.Add<Statsd.Counting,int>("stat-name", 1);
  s.Send<Statsd.Timing,double>("stat-name", 4.4); // message will only contain Timer
  s.Send(); // the counter will not be sent by the command
}

// By default, Statsd will split messages containing multiple metrics/events into
// UDP messages that are 512 bytes long. To change this limit, create a new
// instance of StatsUDP
int maxUDPPacketSize = 4096;
StatsUDP udpNew = new StatsdUDP(HOSTNAME, PORT, maxUDPPacketSize);
using (udP)
{
  // ...
}
// To disable the splitting of UDP messages, set this limit to 0
```

A note about timing: Statsd will not attempt to handle any exceptions that occur in a timed method. If an unhandled exception is thrown while timing, a timer metric containing the time elapsed before the exception occurred will be sent or added to the send queue (depending on whether Send or Add is being called).

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

To suggest a feature, report a bug, or general discussion, head over
[here](https://github.com/DataDog/statsd-csharp-client/issues).

## Credits

dogstatsd-csharp-client is forked from Goncalo Pereira's [original Statsd
client](https://github.com/goncalopereira/statsd-csharp-client).

Copyright (c) 2012 Goncalo Pereira and all contributors. See MIT-LICENCE.md for
further details.

Thanks to Goncalo Pereira, Anthony Steele, Darrell Mozingo, Antony Denyer, and Tim Skauge for their contributions to the original client.
