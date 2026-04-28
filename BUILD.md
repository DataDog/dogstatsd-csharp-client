# Building and Testing

This document describes how to build and test the DogStatsD C# client library.

## Prerequisites

- [.NET SDK 10.0 or above](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started) (required for building Linux native libraries)

## Quick Start

```bash
# Build the .NET library
dotnet build

# Run tests for a specific framework
dotnet test --framework net8.0

# Build all Linux native libraries using Docker
./build-and-test.sh --platform linux

# Pack the NuGet package with all native libraries
dotnet pack src/StatsdClient/StatsdClient.csproj -c Release
```

## Building

### .NET Library

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

### Native Library (Linux only)

The repository includes a native C library (`libfs`) for Linux file system operations. This library must be built for multiple Linux variants to support different distributions.

#### Using Docker (Recommended)

Build all 4 Linux variants using Docker:

```bash
# Build all variants (linux-x64, linux-musl-x64, linux-arm64, linux-musl-arm64)
./build-and-test.sh --platform linux

# Build and test all variants
./build-and-test.sh --test --platform linux
```

**Output:** `runtimes/{rid}/native/libfs.so`

**Supported RIDs:**
- `linux-x64` - Standard x64 Linux (glibc)
- `linux-musl-x64` - Alpine x64 Linux (musl libc)
- `linux-arm64` - Standard ARM64 Linux (glibc)
- `linux-musl-arm64` - Alpine ARM64 Linux (musl libc)

#### Local Build (Linux/WSL only)

Build without Docker on Linux or WSL:

```bash
# Build for specific RID (outputs to runtimes/{rid}/native/)
./src/StatsdClient.Native/build.sh linux-x64

# Build for local development only (outputs to src/StatsdClient.Native/build/)
./src/StatsdClient.Native/build.sh
```

**Note:** Local builds compile for the current system architecture only. Docker is required to cross-compile for different architectures (x64/ARM64) or libc variants (glibc/musl).

## Testing

### Important: Framework Selection

**Always specify `--framework` when running tests.** Running tests without a framework specification will run all target frameworks in parallel, causing conflicts due to shared named pipes.

### Using build-and-test.sh

```bash
# Test specific framework on native platform (Windows/macOS/Linux)
./build-and-test.sh --test --platform native --framework net8.0

# Test all frameworks sequentially on native platform
./build-and-test.sh --test --platform native

# Test all Linux variants in Docker
./build-and-test.sh --test --platform linux
```

### Using dotnet test

```bash
# Run all tests for a specific framework
dotnet test --framework net8.0

# Run tests for specific project and framework
dotnet test tests/StatsdClient.Tests/ --framework net8.0

# Run only native library tests
dotnet test --framework net8.0 --filter FullyQualifiedName~NativeLibraryTests

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

Or use the build script:
```bash
./build-and-test.sh --test --platform native
```

### Supported Target Frameworks

- .NET Framework 4.8 (Windows only)
- .NET Core 2.1, 3.0, 3.1
- .NET 5, 6, 7, 8, 9, 10

## Packaging

To create a NuGet package with all native libraries:

```bash
# Step 1: Build all native library variants
./build-and-test.sh --platform linux

# Step 2: Pack the NuGet package
dotnet pack src/StatsdClient/StatsdClient.csproj -c Release

# Output: src/StatsdClient/bin/Release/*.nupkg
```

The package will include all 4 Linux native library variants in the correct RID directories. The .NET runtime automatically selects the appropriate variant based on the deployment environment.

### Verify Package Contents

**Linux/WSL:**
```bash
unzip -l src/StatsdClient/bin/Release/DogStatsD-CSharp-Client.*.nupkg | grep libfs
```

**Windows (PowerShell):**
```powershell
Expand-Archive src/StatsdClient/bin/Release/DogStatsD-CSharp-Client.*.nupkg -DestinationPath temp
Get-ChildItem temp/runtimes/*/native/
```

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

### Native library not found during tests

Ensure you've built the native libraries before running native library tests:

```bash
./build-and-test.sh --platform linux
dotnet test --framework net8.0 --filter FullyQualifiedName~NativeLibraryTests
```

### Docker build fails on Windows

Make sure Docker Desktop is running and configured for Linux containers (not Windows containers).

### Local native build fails

Native library builds require:
- CMake 3.10+
- GCC or Clang
- Standard build tools (make, etc.)

On Ubuntu/Debian:
```bash
sudo apt-get update
sudo apt-get install -y cmake build-essential
```

On Alpine:
```bash
apk add --no-cache cmake make gcc g++ musl-dev
```

## Clean Build

Remove build artifacts:

```bash
# Clean .NET build outputs
dotnet clean

# Remove native library outputs
rm -rf runtimes/*/native/libfs.so
rm -rf src/StatsdClient.Native/build/

# Remove NuGet packages
rm -rf src/StatsdClient/bin/ src/StatsdClient/obj/
```

## Additional Resources

- [src/StatsdClient.Native/README.md](src/StatsdClient.Native/README.md) - Native library details
- [.NET RID Catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog) - Runtime identifier documentation
