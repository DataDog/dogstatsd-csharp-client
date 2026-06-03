# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is the DogStatsD C# client library (https://github.com/DataDog/dogstatsd-csharp-client), a C# implementation of the DogStatsD protocol for sending metrics, events, and service checks to Datadog.

## Building and Testing

See [BUILD.md](BUILD.md) for build, test, packaging, and benchmark commands, and the list of target frameworks.

Gotcha: always pass `--framework` to `dotnet test` (e.g. `dotnet test --framework net8.0`). Running all target frameworks in parallel causes named-pipe conflicts.

## Architecture

Metrics flow through: submission API -> router -> client-side aggregation or buffer -> background worker -> transport, with telemetry tracked throughout.

- **Public API**: `DogStatsdService` is the thread-safe, instance-based entry point; it must be `Configure()`d before use and disposed to flush. The static `DogStatsd` class wraps a single global instance over the same implementation.
- **Routing and aggregation**: stats are routed to client-side aggregators (Count, Gauge, Set) or sent directly to the buffer (Histogram, Distribution, Timing). Aggregation flushes on an interval (default 2s, configurable; set `StatsdConfig.ClientSideAggregation` to null to disable).
- **Buffering and transport**: metrics are batched into datagrams up to a max packet size, then sent over UDP, a Unix domain socket, or a Windows named pipe. Submission is non-blocking; only `Flush()` and `Dispose()` block.
- **Configuration**: `StatsdConfig` holds the server name/port and aggregation settings and reads environment variables such as `DD_AGENT_HOST`, `DD_DOGSTATSD_PORT`, `DD_ENTITY_ID`, `DD_SERVICE`, `DD_ENV`, and `DD_VERSION`.

## Design Notes

- Both `DogStatsdService` and the static `DogStatsd` are thread-safe. Custom worker handlers must be thread-safe when `workerThreadCount > 1`.
- Hot paths use object pooling and struct-based stats to minimize allocations; keep this in mind before adding heap allocations to the submission path.
