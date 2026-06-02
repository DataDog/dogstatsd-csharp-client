#!/bin/bash
set -e

# Color output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Default values
RUN_TESTS=false
FRAMEWORK=""
EXTRA_TEST_ARGS=()

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --test)
            RUN_TESTS=true
            shift
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
            echo "Usage: $0 [--test] [--framework <tfm>] [-- <dotnet-test-args>]"
            echo ""
            echo "Options:"
            echo "  --test              Run tests after building (default: build only)"
            echo "  --framework <tfm>   Target framework moniker (e.g., net8.0, net48)"
            echo "                      If not specified, tests all frameworks sequentially"
            echo "                      Prevents parallel test execution conflicts"
            echo "  --                  Everything after -- is passed directly to dotnet test"
            echo ""
            echo "Examples:"
            echo "  $0 --test --framework net8.0    # Build and test net8.0 only"
            echo "  $0 --test                        # Build and test all frameworks sequentially"
            echo "  $0                               # Build only"
            echo "  $0 --test --framework net8.0 -- --filter FullyQualifiedName~OriginDetectionTests"
            echo "                                   # Test only OriginDetectionTests on net8.0"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Run '$0 --help' for usage information"
            exit 1
            ;;
    esac
done

echo -e "${BLUE}Building .NET project...${NC}"
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
                --framework "$tfm" \
                -c Release \
                --no-build \
                "${EXTRA_TEST_ARGS[@]}"
        done
    fi
fi

echo ""
echo -e "${GREEN}✓ Build complete!${NC}"
