# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is the DogStatsD C# client library (https://github.com/DataDog/dogstatsd-csharp-client), which provides a C# implementation of the DogStatsD protocol for sending metrics, events, and service checks to Datadog.

## Build and Test Commands

### Building
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Build specific project
dotnet build src/StatsdClient/StatsdClient.csproj

# Build for specific target framework
dotnet build src/StatsdClient/StatsdClient.csproj -f netstandard2.0
```

### Testing

**IMPORTANT**: Always specify `--framework` when running tests. Running tests without a framework will run all target frameworks in parallel, which causes conflicts due to shared named pipes.

```bash
# Run tests for a specific framework (REQUIRED)
dotnet test tests/StatsdClient.Tests/ --framework net8.0

# Run a single test class
dotnet test tests/StatsdClient.Tests/ --framework net8.0 --filter FullyQualifiedName~DogStatsdServiceMetricsTests

# Run a single test method
dotnet test tests/StatsdClient.Tests/ --framework net8.0 --filter FullyQualifiedName~DogStatsdServiceMetricsTests.Counter

# Run all tests sequentially (one framework at a time)
# On Linux/macOS:
for tfm in netcoreapp2.1 netcoreapp3.0 netcoreapp3.1 net5.0 net6.0 net7.0 net8.0 net9.0 net10.0; do
    dotnet test tests/StatsdClient.Tests/ --framework $tfm
done

# On Windows (also includes net48):
for tfm in net48 netcoreapp2.1 netcoreapp3.0 netcoreapp3.1 net5.0 net6.0 net7.0 net8.0 net9.0 net10.0; do
    dotnet test tests/StatsdClient.Tests/ --framework $tfm
done
```

### Packaging

```bash
dotnet pack src/StatsdClient/StatsdClient.csproj -c Release

# Output: artifacts/package/release/*.nupkg (UseArtifactsOutput is enabled in Directory.Build.props)
```

### Benchmarks
```bash
# Run benchmarks
dotnet run -c Release --project benchmarks/StatsdClient.Benchmarks/StatsdClient.Benchmarks.csproj
```

## Architecture

### Core Components

**DogStatsdService** (`src/StatsdClient/DogStatsdService.cs`): Thread-safe instance-based API for sending metrics. Requires explicit `Configure()` call before use. Must be disposed to flush metrics.

**DogStatsd** (static class): Static wrapper around DogStatsdService for applications that prefer a single global instance. Shares the same underlying implementation.

**StatsRouter** (`src/StatsdClient/StatsRouter.cs`): Routes incoming stats to either client-side aggregators (for Count, Gauge, Set) or directly to BufferBuilder (for Histogram, Distribution, Timing).

**MetricsSender** (`src/StatsdClient/MetricsSender.cs`): Handles serialization and sending of metrics through the StatsRouter.

### Client-Side Aggregation

By default, basic metric types (Count, Gauge, Set) are aggregated client-side before sending to reduce network usage and agent load:
- **CountAggregator** (`src/StatsdClient/Aggregator/CountAggregator.cs`): Aggregates counter values
- **GaugeAggregator** (`src/StatsdClient/Aggregator/GaugeAggregator.cs`): Keeps last gauge value
- **SetAggregator** (`src/StatsdClient/Aggregator/SetAggregator.cs`): Tracks unique set values
- **AggregatorFlusher** (`src/StatsdClient/Aggregator/AggregatorFlusher.cs`): Periodically flushes aggregated metrics

Aggregation window defaults to 2 seconds (configurable via `ClientSideAggregationConfig.FlushInterval`). Disable by setting `StatsdConfig.ClientSideAggregation` to null.

### Buffering and Transport

**BufferBuilder** (`src/StatsdClient/Bufferize/BufferBuilder.cs`): Batches multiple metrics into single datagrams up to max packet size (default 1432 bytes for UDP).

**AsynchronousWorker** (`src/StatsdClient/Worker/AsynchronousWorker.cs`): Manages background worker threads that process the metrics queue asynchronously. Non-blocking except for `Flush()` and `Dispose()`.

**Transport Layer** (`src/StatsdClient/Transport/`):
- **UDPTransport**: Standard UDP transport to agent
- **UnixDomainSocketTransport**: Unix domain socket transport (not supported on Windows for Dgram sockets)
- **NamedPipeTransport**: Windows named pipe transport (internal)

### Configuration

**StatsdConfig** (`src/StatsdClient/StatsdConfig.cs`): Main configuration class with properties:
- `StatsdServerName`: Agent hostname or unix socket path (e.g., "unix:///tmp/dsd.socket")
- `StatsdPort`: Agent port (defaults to 8125)
- `ClientSideAggregation`: Client-side aggregation settings (null to disable)
- Environment variable support: `DD_AGENT_HOST`, `DD_DOGSTATSD_PORT`, `DD_ENTITY_ID`, `DD_SERVICE`, `DD_ENV`, `DD_VERSION`

## Target Frameworks

The library (`src/StatsdClient/StatsdClient.csproj`) targets: `net461`, `netstandard2.0`, `netcoreapp3.1`, `net6.0`.

The test project (`tests/StatsdClient.Tests/StatsdClient.Tests.csproj`) targets a wider range to validate the library on all supported runtimes: netcoreapp2.1, 3.0, 3.1; net5.0 through net10.0; plus net48 on Windows.

Tests run on all test frameworks via GitHub Actions (Linux, Windows, and macOS). See `.github/workflows/build-and-test.yml`.

## Key Design Patterns

1. **Object Pooling**: Uses custom Pool implementation (`src/StatsdClient/Utils/Pool.cs`) to reduce allocations for frequently created objects like buffers and stats.

2. **Struct-based Stats**: Internal `Stats` structs (`src/StatsdClient/Statistic/`) minimize heap allocations when passing metrics through the pipeline.

3. **Thread Safety**: Both DogStatsdService and static DogStatsd are thread-safe. Worker handlers must be thread-safe when `workerThreadCount` > 1.

4. **Non-blocking Operations**: Metric submission methods are non-blocking (enqueue to worker thread). Only `Flush()` and `Dispose()` block.

5. **Telemetry**: Built-in telemetry (`src/StatsdClient/Telemetry.cs`) tracks client metrics like bytes sent, packets sent, dropped metrics, etc.
