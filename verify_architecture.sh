#!/bin/bash

# Verification script for native library architecture
# This script checks that all native libraries have the correct 64-bit architecture

set -e

echo "=========================================="
echo "Native Library Architecture Verification"
echo "=========================================="
echo ""

# Define the paths
BIN_DIR="/home/akira/opensim/grid/akisim/bin"
RUNTIME_DIR="$BIN_DIR/runtimes/linux-x64/native"

# Check if directories exist
if [ ! -d "$BIN_DIR" ]; then
    echo "ERROR: Bin directory not found: $BIN_DIR"
    exit 1
fi

if [ ! -d "$RUNTIME_DIR" ]; then
    echo "ERROR: Runtime directory not found: $RUNTIME_DIR"
    exit 1
fi

echo "Checking libraries in $BIN_DIR:"
echo "--------------------------------"

# Check libraries in bin directory
for lib in libSkiaSharp.so libBulletSim.so libopenjpeg-dotnet.so libubode.so; do
    if [ -f "$BIN_DIR/$lib" ]; then
        arch=$(file "$BIN_DIR/$lib" | grep 'ELF')
        echo "✓ $lib: $arch"
        if [[ "$arch" != *"64-bit"* ]]; then
            echo "  ERROR: Expected 64-bit but got $arch"
            exit 1
        fi
    else
        echo "✗ $lib: NOT FOUND"
    fi
done

echo ""
echo "Checking libraries in $RUNTIME_DIR:"
echo "--------------------------------------"

# Check libraries in runtime directory
for lib in libSkiaSharp.so libBulletSim.so libopenjpeg-dotnet.so libubode.so; do
    if [ -f "$RUNTIME_DIR/$lib" ]; then
        arch=$(file "$RUNTIME_DIR/$lib" | grep 'ELF')
        echo "✓ $lib: $arch"
        if [[ "$arch" != *"64-bit"* ]]; then
            echo "  ERROR: Expected 64-bit but got $arch"
            exit 1
        fi
    else
        echo "✗ $lib: NOT FOUND"
    fi
done

echo ""
echo "=========================================="
echo "✓ All libraries have correct 64-bit architecture!"
echo "=========================================="
