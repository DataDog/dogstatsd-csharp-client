# Building and Testing

This document describes how to build and test the DogStatsD C# client library.

## Prerequisites

- [.NET SDK 10.0 or above](https://dotnet.microsoft.com/download)

## Quick Start

```bash
# Build the .NET library
dotnet build

# Run tests for a specific framework
dotnet test --framework net8.0

# Pack the NuGet package
dotnet pack src/StatsdClient/StatsdClient.csproj -c Release
# Output: artifacts/package/release/*.nupkg
```

## Building

Build the main .NET library:

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Build specific project
dotnet build src/StatsdClient/StatsdClient.csproj

# Build for specific configuration
dotnet build -c Release

# Build for specific target framework
dotnet build src/StatsdClient/StatsdClient.csproj -f netstandard2.0
```

## Testing

### Important: Framework Selection

**Always specify `--framework` when running tests.** Running tests without a framework specification will run all target frameworks in parallel, causing conflicts due to shared named pipes.

### Using dotnet test

```bash
# Run all tests for a specific framework
dotnet test --framework net8.0

# Run tests for specific project and framework
dotnet test tests/StatsdClient.Tests/ --framework net8.0

# Run specific test class
dotnet test --framework net8.0 --filter FullyQualifiedName~DogStatsdServiceMetricsTests

# Run specific test method
dotnet test --framework net8.0 --filter FullyQualifiedName~DogStatsdServiceMetricsTests.Counter
```

### Testing All Frameworks Sequentially

**Linux/macOS:**
```bash
for tfm in netcoreapp2.1 netcoreapp3.0 netcoreapp3.1 net5.0 net6.0 net7.0 net8.0 net9.0 net10.0; do
    dotnet test --framework $tfm
done
```

**Windows (includes .NET Framework):**
```bash
for tfm in net48 netcoreapp2.1 netcoreapp3.0 netcoreapp3.1 net5.0 net6.0 net7.0 net8.0 net9.0 net10.0; do
    dotnet test --framework $tfm
done
```

### Supported Test Frameworks

The test project runs against:

- .NET Framework 4.8 (Windows only)
- .NET Core 2.1, 3.0, 3.1
- .NET 5, 6, 7, 8, 9, 10

The library itself (`src/StatsdClient/StatsdClient.csproj`) targets `net461`, `netstandard2.0`, `netcoreapp3.1`, and `net6.0`.

## Packaging

To create a NuGet package:

```bash
dotnet pack src/StatsdClient/StatsdClient.csproj -c Release

# Output: artifacts/package/release/*.nupkg
```

The build uses .NET's artifacts output layout (`UseArtifactsOutput` in `Directory.Build.props`), so build and package outputs go to the top-level `artifacts/` directory rather than per-project `bin/` and `obj/` folders.

## Benchmarks

Run performance benchmarks:

```bash
dotnet run -c Release --project benchmarks/StatsdClient.Benchmarks/StatsdClient.Benchmarks.csproj
```

## Troubleshooting

### Tests fail with "address already in use" or named pipe conflicts

Make sure you're specifying `--framework` when running tests. Running multiple frameworks in parallel causes port and named pipe conflicts.

```bash
# ❌ Wrong - runs all frameworks in parallel
dotnet test

# ✅ Correct - runs single framework
dotnet test --framework net8.0
```

## Clean Build

Remove build artifacts:

```bash
# Clean .NET build outputs
dotnet clean

# Remove all build and package outputs
rm -rf artifacts/
```
