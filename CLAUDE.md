# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is the DogStatsD C# client library (https://github.com/DataDog/dogstatsd-csharp-client), which provides a C# implementation of the DogStatsD protocol for sending metrics, events, and service checks to Datadog.

## Build and Test Commands

### Building

#### .NET Library (Cross-platform)
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

#### Native Library (Linux only)

The repository includes a small native C library (`libfs`) for Linux inode operations. See `src/StatsdClient.Native/README.md` for details.

**Using build-and-test.sh (recommended):**
```bash
# Build all 4 Linux variants using Docker (linux-x64, linux-musl-x64, linux-arm64, linux-musl-arm64)
./build-and-test.sh --platform linux

# Build and test all Linux variants
./build-and-test.sh --test --platform linux

# Build and test .NET for specific framework (native platform, no Docker)
./build-and-test.sh --test --platform native --framework net8.0

# Build and test .NET for all frameworks sequentially (native platform, no Docker)
./build-and-test.sh --test --platform native
```

**Local build (Linux/WSL only):**
```bash
# Build for specific RID (outputs to runtimes/{rid}/native/)
./src/StatsdClient.Native/build.sh linux-x64
./src/StatsdClient.Native/build.sh linux-musl-x64

# Build for local development (outputs to src/StatsdClient.Native/build/)
./src/StatsdClient.Native/build.sh
```

**Output locations:**
- Docker builds: `runtimes/{rid}/native/libfs.so`
- Local builds with RID: `runtimes/{rid}/native/libfs.so`
- Local builds without RID: `src/StatsdClient.Native/build/libfs.so`

**Supported RIDs** (per [official .NET RID catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#linux-rids)):
- `linux-x64` (standard x64 Linux with glibc)
- `linux-musl-x64` (Alpine x64 Linux with musl)
- `linux-arm64` (standard ARM64 Linux with glibc)
- `linux-musl-arm64` (Alpine ARM64 Linux with musl)

### Testing

**IMPORTANT**: Always specify `--framework` when running tests. Running tests without a framework will run all target frameworks in parallel, which causes conflicts due to shared named pipes.

```bash
# Run tests for a specific framework (REQUIRED)
dotnet test tests/StatsdClient.Tests/ --framework net8.0

# Run only native library tests
dotnet test tests/StatsdClient.Tests/ --framework net8.0 --filter FullyQualifiedName~NativeLibraryTests

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

To build the NuGet package with all native libraries:

```bash
# Step 1: Build all native variants using Docker
./build-and-test.sh --platform linux

# Step 2: Pack the NuGet package
dotnet pack src/StatsdClient/StatsdClient.csproj -c Release

# Output: src/StatsdClient/bin/Release/*.nupkg
```

The package will include all 4 native library variants in the correct RID directories.

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

The library supports:
- .NET Standard 2.0+
- .NET Core 2.1, 3.0, 3.1
- .NET 5.0, 6.0, 7.0, 8.0, 9.0
- .NET Framework 4.8

Tests run on all supported frameworks via GitHub Actions (Linux and Windows).

## Key Design Patterns

1. **Object Pooling**: Uses custom Pool implementation (`src/StatsdClient/Utils/Pool.cs`) to reduce allocations for frequently created objects like buffers and stats.

2. **Struct-based Stats**: Internal `Stats` structs (`src/StatsdClient/Statistic/`) minimize heap allocations when passing metrics through the pipeline.

3. **Thread Safety**: Both DogStatsdService and static DogStatsd are thread-safe. Worker handlers must be thread-safe when `workerThreadCount` > 1.

4. **Non-blocking Operations**: Metric submission methods are non-blocking (enqueue to worker thread). Only `Flush()` and `Dispose()` block.

5. **Telemetry**: Built-in telemetry (`src/StatsdClient/Telemetry.cs`) tracks client metrics like bytes sent, packets sent, dropped metrics, etc.
