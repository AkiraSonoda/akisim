#!/bin/bash

# Quick test script to verify OpenTelemetry is working
# This will start OpenSim briefly and check if OTEL initializes

cd /home/akira/opensim/grid/working/bin

echo "=== Testing OpenTelemetry Initialization ==="
echo ""
echo "Starting OpenSim and monitoring for OpenTelemetry messages..."
echo "Will auto-terminate after 15 seconds"
echo ""

# Start OpenSim in background and capture output
timeout 15s dotnet OpenSim.dll 2>&1 | tee /tmp/otel_test.log &
PID=$!

# Wait for it to finish or timeout
wait $PID

echo ""
echo "=== Checking for OpenTelemetry initialization ==="
grep -i "opentelemetry" /tmp/otel_test.log

echo ""
echo "=== Checking for configuration loaded ==="
grep -A 10 "Configuration loaded" /tmp/otel_test.log

echo ""
echo "=== Checking for MeterProvider/LoggerProvider initialization ==="
grep -E "(MeterProvider|LoggerProvider)" /tmp/otel_test.log

echo ""
echo "=== Checking for any errors ==="
grep -i "error.*opentelemetry\|failed.*opentelemetry" /tmp/otel_test.log

echo ""
echo "Full log saved to: /tmp/otel_test.log"
