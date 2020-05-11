CHANGELOG
=========
# 5.0.0 / XX-XX-2020
Improve significantly the performance of `DogStatsdService` and `DogStatsd`.

## Breaking changes
**You must call `DogStatsdService.Dispose()` or `DogStatsd.Dispose()` before your program termination in order to flush metrics not yet sent.** 
`Statsd` is marked as obsolete.

## Changes 
* [IMPROVEMENT] Both `DogStatsdService` and `DogStatsd` methods do not block anymore and batch several metrics automatically in one UDP or UDS message. See [#108][] and [#109][].
* [IMPROVEMENT] Send telemetry metrics. See [#110][] and [#114][].
* [IMPROVEMENT] Enable StyleCop. See [#111][], [#112][] and [#113][].

# 4.0.1 / 02-11-2020
* [BUGFIX] Fix `System.ArgumentException: unixSocket must start with unix://` when using the `DD_AGENT_HOST` environment variable with UDS support. See [this comment](https://github.com/DataDog/dogstatsd-csharp-client/issues/85#issuecomment-581371860) (Thanks [@danopia][])

# 4.0.0 / 01-03-2020
## Breaking changes
Version `3.4.0` uses a strong-named assembly that may introduce a [breaking change](https://github.com/DataDog/dogstatsd-csharp-client/pull/96#issuecomment-561379859).
This major version change makes this breaking change explicit. No other breaking changes are expected.

## Changes 
* [IMPROVEMENT] Add Async methods to Statsd. See [#59][] (Thanks [@alistair][])
* [IMPROVEMENT] Add Unix domain socket support. See [#92][]

# 3.4.0 / 11-15-2019

* [IMPROVEMENT] Use a strong-named assembly. See [#96][] (Thanks [@carlreid][])


# 3.3.0 / 04-05-2019

* [FEATURE] Option to set global tags that are added to every statsd call. See [#3][], [#78][] (Thanks [@chriskinsman][])
* [IMPROVEMENT] Configure the client with environment variables. See [#78][]


# 3.2.0 / 10-18-2018

* [BUGFIX] Fix an issue causing the `StartTimer` method to ignore non static `DogStatsdService` instance configurations. See [#62][], [#63][] (Thanks [@jpasichnyk][])
* [BUGFIX] Prevent the static API from being configured more than once to avoid race conditions. See [#66][] (Thanks [@nrjohnstone][])
* [BUGFIX] Set a default value for `tags` in the `Decrement` method similar to `Increment`. See [#60][], [#61][] (Thanks [@sqdk][])
* [FEATURE] Add support for DogStatsD distribution. See [#65][]

# 3.1.0 / 11-16-2017

## Supported target framework versions

DogStatsD-CSharp-Client `3.1.0` supports the following platforms:
* .NET Standard 1.3
* .NET Standard 1.6
* .NET Core Application 1.1
* .NET Core Application 2.0
* .NET Framework 4.5.1
* .NET Framework 4.6.1

## Changes

* [BUGFIX] `DogStatsdService` implements `IDogStatsd`. See [#43][], [#54][]
* [BUGFIX] Fix IP host name resolution when IPv6 addresses are available. See [#50][] (Thanks [@DanielVukelich][])
* [IMPROVEMENT] Add `IDisposable` interface to `DogStatsdService` to manage the release of resources. See [#44][] (Thanks [@bcuff][])
* [IMPROVEMENT] New `StatsdConfig.StatsdTruncateIfTooLong` option to truncate Events and Service checks larger than 8 kB (default to True). See [#48][], [#55][]
* [IMPROVEMENT] New supported targeted frameworks: .NET Standard 1.6, .NET Core Application 1.1, .NET Core Application 2.0, .NET Framework 4.6.1. See [#52][] (Thanks [@pdpurcell][])

# 3.0.0 / 10-31-2016

## .NET Core support, end of .NET Framework 3.5 compatibility

DogStatsD-CSharp-Client `2.2.1` is the last version to support .NET Framework 3.5. As of `3.0.0`, DogStatsD-CSharp-Client supports the following platforms:
* .NET Framework 4.5.1
* .NET Standard 1.3

## Changes

* [IMPROVEMENT] Move to .NET Core, and drop .NET Framework 3.5 compatibility. See [#28][], [#39][] (Thanks [@wjdavis5][])
* [IMPROVEMENT] Abstract DogStatsD service. See [#30][], [#40][] (Thanks [@nrjohnstone][])

# 2.2.1 / 10-13-2016
* [BUGFIX] Remove the `TRACE` directive from release builds. See [#33][], [#34][] (Thanks [@albertofem][])
* [FEATURE] Service check support. See [#29][] (Thanks [@nathanrobb][])

# 2.2.0 / 08-08-2016
* [BUGFIX] Fix `Random` generator thread safety. See [#26][] (Thanks [@windsnow98][])

#  2.1.1 / 12-04-2015
* [BUGFIX] Optional automatic truncation of events that exceed the message length limit. See [#22][] (Thanks [@daniel-chambers][])

#  2.1.0 / 09-01-2015
* [BUGFIX][IMPROVEMENT] Fix `DogStatsd` unsafe thread operations. See [#18][] (Thanks [@yori-s][])

#  2.0.3 / 08-17-2015
* [BUGFIX] Fix event's text escape when it contains windows carriage returns. See [#15][] (Thanks [@anthonychu][]

# 2.0.2 / 03-09-2015
* [IMPROVEMENT] Strong-name-assembly. See [#11][]

# 2.0.1 / 02-10-2015
* [BUGFIX] Remove NUnit dependency from StatsdClient project. See [#8][] (Thanks [@michaellockwood][])

# 2.0.0 / 01-21-2015
* [FEATURE] Event support
* [FEATURE] Increment/decrement by value.
* [IMPROVEMENT] UDP packets UTF-8 encoding (was ASCII).

# 1.1.0 / 07-17-2013
* [IMPROVEMENT] UDP packets containing multiple metrics that are over the UDP packet size limit will now be split into multiple appropriately-sized packets if possible.

# 1.0.0 / 07-02-2013
* Initial release

<!--- The following link definition list is generated by PimpMyChangelog --->
[#3]: https://github.com/DataDog/dogstatsd-csharp-client/issues/3
[#8]: https://github.com/DataDog/dogstatsd-csharp-client/issues/8
[#11]: https://github.com/DataDog/dogstatsd-csharp-client/issues/11
[#15]: https://github.com/DataDog/dogstatsd-csharp-client/issues/15
[#18]: https://github.com/DataDog/dogstatsd-csharp-client/issues/18
[#22]: https://github.com/DataDog/dogstatsd-csharp-client/issues/22
[#26]: https://github.com/DataDog/dogstatsd-csharp-client/issues/26
[#28]: https://github.com/DataDog/dogstatsd-csharp-client/issues/28
[#29]: https://github.com/DataDog/dogstatsd-csharp-client/issues/29
[#30]: https://github.com/DataDog/dogstatsd-csharp-client/issues/30
[#33]: https://github.com/DataDog/dogstatsd-csharp-client/issues/33
[#34]: https://github.com/DataDog/dogstatsd-csharp-client/issues/34
[#39]: https://github.com/DataDog/dogstatsd-csharp-client/issues/39
[#40]: https://github.com/DataDog/dogstatsd-csharp-client/issues/40
[#43]: https://github.com/DataDog/dogstatsd-csharp-client/issues/43
[#44]: https://github.com/DataDog/dogstatsd-csharp-client/issues/44
[#48]: https://github.com/DataDog/dogstatsd-csharp-client/issues/48
[#50]: https://github.com/DataDog/dogstatsd-csharp-client/issues/50
[#52]: https://github.com/DataDog/dogstatsd-csharp-client/issues/52
[#54]: https://github.com/DataDog/dogstatsd-csharp-client/issues/54
[#55]: https://github.com/DataDog/dogstatsd-csharp-client/issues/55
[#59]: https://github.com/DataDog/dogstatsd-csharp-client/issues/59
[#60]: https://github.com/DataDog/dogstatsd-csharp-client/issues/60
[#61]: https://github.com/DataDog/dogstatsd-csharp-client/issues/61
[#62]: https://github.com/DataDog/dogstatsd-csharp-client/issues/62
[#63]: https://github.com/DataDog/dogstatsd-csharp-client/issues/63
[#65]: https://github.com/DataDog/dogstatsd-csharp-client/issues/65
[#66]: https://github.com/DataDog/dogstatsd-csharp-client/issues/66
[#78]: https://github.com/DataDog/dogstatsd-csharp-client/issues/78
[#92]: https://github.com/DataDog/dogstatsd-csharp-client/issues/92
[#96]: https://github.com/DataDog/dogstatsd-csharp-client/issues/96
[#108]: https://github.com/DataDog/dogstatsd-csharp-client/issues/108
[#109]: https://github.com/DataDog/dogstatsd-csharp-client/issues/109
[#110]: https://github.com/DataDog/dogstatsd-csharp-client/issues/110
[#111]: https://github.com/DataDog/dogstatsd-csharp-client/issues/111
[#112]: https://github.com/DataDog/dogstatsd-csharp-client/issues/112
[#113]: https://github.com/DataDog/dogstatsd-csharp-client/issues/113
[#114]: https://github.com/DataDog/dogstatsd-csharp-client/issues/114
[@DanielVukelich]: https://github.com/DanielVukelich
[@albertofem]: https://github.com/albertofem
[@alistair]: https://github.com/alistair
[@anthonychu]: https://github.com/anthonychu
[@bcuff]: https://github.com/bcuff
[@carlreid]: https://github.com/carlreid
[@chriskinsman]: https://github.com/chriskinsman
[@daniel-chambers]: https://github.com/daniel-chambers
[@danopia]: https://github.com/danopia
[@jpasichnyk]: https://github.com/jpasichnyk
[@michaellockwood]: https://github.com/michaellockwood
[@nathanrobb]: https://github.com/nathanrobb
[@nrjohnstone]: https://github.com/nrjohnstone
[@pdpurcell]: https://github.com/pdpurcell
[@sqdk]: https://github.com/sqdk
[@windsnow98]: https://github.com/windsnow98
[@wjdavis5]: https://github.com/wjdavis5
[@yori-s]: https://github.com/yori-s