# libfs - Native Library for Linux File System Operations

This directory contains a small C library (`libfs`) that provides cross-platform access to file inode numbers on Linux.

## Purpose

The library wraps the `stat()` system call to retrieve inode numbers, avoiding cross-platform P/Invoke compatibility issues when calling libc's `stat()` directly from C#.

## Building

### Using Docker (Recommended)

Build all 4 Linux variants using Docker from the repository root:

```bash
# Build all Linux variants
./build-and-test.sh --platform linux

# Build and test all Linux variants
./build-and-test.sh --test --platform linux
```

**Supported RIDs** (per [.NET RID catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#linux-rids)):
- `linux-x64` - Standard x64 Linux with glibc
- `linux-musl-x64` - Alpine x64 Linux with musl libc
- `linux-arm64` - Standard ARM64 Linux with glibc
- `linux-musl-arm64` - Alpine ARM64 Linux with musl libc

**Output:** `runtimes/{rid}/native/libfs.so`

### Local Build (Linux/WSL only)

Build without Docker:

```bash
# Build for specific RID (outputs to runtimes/{rid}/native/)
./src/StatsdClient.Native/build.sh linux-x64
./src/StatsdClient.Native/build.sh linux-musl-x64
./src/StatsdClient.Native/build.sh linux-arm64
./src/StatsdClient.Native/build.sh linux-musl-arm64

# Build for local development (outputs to src/StatsdClient.Native/build/)
./src/StatsdClient.Native/build.sh
```

**Note:** Local builds compile for the current system only. Use Docker to cross-compile for different architectures or libc variants.

### Testing

**Using build-and-test.sh:**
```bash
# Test all Linux variants in Docker
./build-and-test.sh --test --platform linux

# Test specific framework on native platform
./build-and-test.sh --test --platform native --framework net8.0
```

**Using dotnet test directly:**

After building, run tests using standard .NET tooling. **Always specify `--framework`** to avoid parallel test execution conflicts:

```bash
# Run native library tests for a specific framework
dotnet test tests/StatsdClient.Tests/StatsdClient.Tests.csproj \
    --framework net8.0 \
    --filter 'FullyQualifiedName~NativeLibraryTests'

# Run all tests for a specific framework
dotnet test tests/StatsdClient.Tests/StatsdClient.Tests.csproj \
    --framework net8.0
```

The .NET runtime automatically selects the correct native library based on the current platform:
- On glibc systems: uses `runtimes/linux-{arch}/native/libfs.so`
- On musl systems (Alpine): uses `runtimes/linux-musl-{arch}/native/libfs.so`

## API

### C API

```c
int get_inode(const char* path, unsigned long long* ino);
```

Returns:
- `0` on success, with `ino` populated with the inode number
- `-1` on failure (e.g., file not found, permission denied)

### .NET API

```csharp
using StatsdClient.Native;

if (NativeInode.TryGetInode("/path/to/file", out ulong inode))
{
    Console.WriteLine($"Inode: {inode}");
}
else
{
    Console.WriteLine("Failed to get inode (not on Linux or file not found)");
}
```

The .NET wrapper automatically detects the platform and returns `false` on Windows/macOS.

## CI/CD

The GitHub Actions workflow (`.github/workflows/build-and-test.yml`) automatically builds all variants using Docker containers and includes them in the test runs and NuGet package.

## Files

- `fs.h` - Header file
- `fs.c` - Implementation
- `CMakeLists.txt` - CMake build configuration
- `README.md` - This file
