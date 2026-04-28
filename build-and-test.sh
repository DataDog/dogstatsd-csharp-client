#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Color output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Default values
RUN_TESTS=false
PLATFORM=""
FRAMEWORK=""
EXTRA_TEST_ARGS=()

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --test)
            RUN_TESTS=true
            shift
            ;;
        --platform)
            PLATFORM="$2"
            shift 2
            ;;
        --framework)
            FRAMEWORK="$2"
            shift 2
            ;;
        --)
            # Everything after -- is passed to dotnet test
            shift
            EXTRA_TEST_ARGS=("$@")
            break
            ;;
        --help)
            echo "Usage: $0 [--test] [--platform <native|linux>] [--framework <tfm>] [-- <dotnet-test-args>]"
            echo ""
            echo "Options:"
            echo "  --test              Run tests after building (default: build only)"
            echo "  --platform <type>   Target platform:"
            echo "                        native - Use native dotnet build/test (Windows/macOS)"
            echo "                        linux  - Use Docker to build all 4 Linux variants"
            echo "                        (default: auto-detect based on OS)"
            echo "  --framework <tfm>   Target framework moniker (e.g., net8.0, net48)"
            echo "                      If not specified, tests all frameworks sequentially"
            echo "                      Prevents parallel test execution conflicts"
            echo "  --                  Everything after -- is passed directly to dotnet test"
            echo ""
            echo "Examples:"
            echo "  $0 --test --platform native --framework net8.0    # Build and test .NET for net8.0 only"
            echo "  $0 --test --platform native                        # Build and test all frameworks sequentially"
            echo "  $0 --platform linux                                # Build Linux native libs in Docker"
            echo "  $0 --test --platform linux                         # Build and test all Linux variants in Docker"
            echo "  $0 --test --platform linux -- --filter FullyQualifiedName~NativeLibraryTests"
            echo "                                                     # Test only NativeLibraryTests in Docker"
            echo "  $0                                                 # Auto-detect platform, build only"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Run '$0 --help' for usage information"
            exit 1
            ;;
    esac
done

# Auto-detect platform if not specified
if [ -z "$PLATFORM" ]; then
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        PLATFORM="linux"
        echo -e "${YELLOW}Auto-detected platform: linux${NC}"
    elif [[ "$OSTYPE" == "darwin"* ]] || [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]]; then
        PLATFORM="native"
        echo -e "${YELLOW}Auto-detected platform: native${NC}"
    else
        echo -e "${RED}Could not auto-detect platform. Please specify --platform <native|linux>${NC}"
        exit 1
    fi
fi

# Validate platform
if [[ "$PLATFORM" != "native" && "$PLATFORM" != "linux" ]]; then
    echo -e "${RED}Invalid platform: $PLATFORM${NC}"
    echo "Must be 'native' or 'linux'"
    exit 1
fi

echo -e "${BLUE}Starting build process...${NC}"
echo ""

# Native platform (Windows/macOS - .NET only, no native libs)
if [ "$PLATFORM" = "native" ]; then
    echo -e "${BLUE}Building .NET project (native platform)...${NC}"
    dotnet build src/StatsdClient/StatsdClient.csproj -c Release

    if [ "$RUN_TESTS" = true ]; then
        if [ -n "$FRAMEWORK" ]; then
            # Test a single framework
            echo ""
            echo -e "${BLUE}Running tests for framework: $FRAMEWORK${NC}"
            dotnet test tests/StatsdClient.Tests/StatsdClient.Tests.csproj \
                --framework "$FRAMEWORK" \
                -c Release \
                --no-build \
                "${EXTRA_TEST_ARGS[@]}"
        else
            # Test all frameworks sequentially
            echo ""
            echo -e "${BLUE}Running tests for all frameworks sequentially (to avoid named pipe conflicts)...${NC}"

            # Determine which frameworks to test based on OS
            FRAMEWORKS="netcoreapp2.1 netcoreapp3.0 netcoreapp3.1 net5.0 net6.0 net7.0 net8.0 net9.0 net10.0"

            # Add .NET Framework on Windows
            if [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]]; then
                FRAMEWORKS="net48 $FRAMEWORKS"
            fi

            # Run tests for each framework sequentially
            for tfm in $FRAMEWORKS; do
                echo ""
                echo -e "${BLUE}Testing framework: $tfm${NC}"
                dotnet test tests/StatsdClient.Tests/StatsdClient.Tests.csproj \
                    --framework $tfm \
                    -c Release \
                    --no-build \
                    "${EXTRA_TEST_ARGS[@]}"
            done
        fi
    fi

    echo ""
    echo -e "${GREEN}✓ Native build complete!${NC}"
    exit 0
fi

# Linux platform (Docker - build native libs for all variants)
if [ "$PLATFORM" = "linux" ]; then
    # Check if Docker is available
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}Error: Docker is not installed or not in PATH${NC}"
        exit 1
    fi

    export DOCKER_BUILDKIT=1
    echo "Docker version:"
    docker --version
    echo ""

    build_and_test_variant() {
        local arch=$1
        local libc=$2
        local base_image=$3

        # Construct proper RID based on official RID catalog
        # https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#linux-rids
        local rid="linux-${arch}"
        if [ "$libc" = "musl" ]; then
            rid="linux-musl-${arch}"
        fi

        echo -e "${BLUE}Building $rid...${NC}"

        # Determine Docker platform
        local platform=""
        if [ "$arch" = "x64" ]; then
            platform="linux/amd64"
        elif [ "$arch" = "arm64" ]; then
            platform="linux/arm64"
        fi

        # Determine build target
        # For ARM64: only build native lib (skip tests due to QEMU emulation limitations)
        local target="builder"
        if [ "$RUN_TESTS" = true ] && [ "$arch" = "x64" ]; then
            target="tester"
        fi

        if [ "$RUN_TESTS" = true ] && [ "$arch" = "arm64" ]; then
            echo -e "${YELLOW}Note: Skipping .NET tests for ARM64 due to QEMU emulation limitations${NC}"
        fi

        # Build the Docker image
        # Convert array to string for passing to Docker
        local test_args_string="${EXTRA_TEST_ARGS[*]}"

        docker build \
            --platform "$platform" \
            --build-arg BASE_IMAGE="$base_image" \
            --build-arg RID="$rid" \
            --build-arg DOTNET_TEST_ARGS="$test_args_string" \
            --target "$target" \
            -t "libfs-${target}:${rid}" \
            -f Dockerfile.linux \
            .

        # Create a temporary container to extract the built library
        local container_id=$(docker create --platform "$platform" "libfs-${target}:${rid}")

        # Extract the built library
        mkdir -p "$SCRIPT_DIR/runtimes/$rid/native"
        docker cp "$container_id:/build/runtimes/$rid/native/libfs.so" "$SCRIPT_DIR/runtimes/$rid/native/libfs.so"

        # Clean up
        docker rm "$container_id" > /dev/null

        if [ "$RUN_TESTS" = true ] && [ "$arch" = "x64" ]; then
            echo -e "${GREEN}✓ Built and tested $rid${NC}"
        else
            echo -e "${GREEN}✓ Built $rid${NC}"
        fi
        echo
    }

    # Build all combinations
    build_and_test_variant "x64" "glibc" "mcr.microsoft.com/dotnet/sdk:10.0"
    build_and_test_variant "x64" "musl" "mcr.microsoft.com/dotnet/sdk:10.0-alpine"
    build_and_test_variant "arm64" "glibc" "mcr.microsoft.com/dotnet/sdk:10.0"
    build_and_test_variant "arm64" "musl" "mcr.microsoft.com/dotnet/sdk:10.0-alpine"

    echo ""
    echo -e "${GREEN}All Linux builds complete!${NC}"
    echo "Outputs in $SCRIPT_DIR/runtimes/"
    echo ""
    tree "$SCRIPT_DIR/runtimes/"
fi
