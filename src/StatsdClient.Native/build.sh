#!/bin/bash
set -e

# Simple build script for libfs
# Usage: ./build.sh [rid]
# Example: ./build.sh linux-glibc-x64
# If no RID is provided, builds to src/StatsdClient.Native/build/ (for local development)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$SCRIPT_DIR/../.."
RID="$1"

echo "Building libfs..."

if [ -n "$RID" ]; then
    # Build to runtimes/{rid}/native/ for consistency with Docker builds
    OUTPUT_DIR="$REPO_ROOT/runtimes/$RID/native"
    BUILD_DIR="$OUTPUT_DIR/build"
    mkdir -p "$BUILD_DIR"
    cd "$BUILD_DIR"

    # Configure and build
    cmake "$SCRIPT_DIR"
    cmake --build . --config Release

    # Move the library to the native directory (one level up from build)
    mv libfs.so "$OUTPUT_DIR/libfs.so"

    # Clean up build artifacts
    cd "$OUTPUT_DIR"
    rm -rf build

    echo "Build complete. Library at: $OUTPUT_DIR/libfs.so"
else
    # No RID specified - build to local build directory for development
    OUTPUT_DIR="$SCRIPT_DIR"
    mkdir -p "$OUTPUT_DIR/build"
    cd "$OUTPUT_DIR/build"

    # Configure and build
    cmake "$SCRIPT_DIR"
    cmake --build . --config Release

    echo "Build complete. Library at: $OUTPUT_DIR/build/libfs.so"
    echo "Note: To build for testing, specify a RID: ./build.sh linux-glibc-x64"
fi
