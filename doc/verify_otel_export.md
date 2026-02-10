# OpenTelemetry Export Verification

## Summary
OpenTelemetry is properly initialized and configured to send data to Grafana Cloud.

## Configuration
- **Service Name**: Akisim.KoPhp
- **Endpoint**: https://otlp-gateway-prod-eu-west-2.grafana.net/otlp
- **Protocol**: HTTP/Protobuf
- **Authorization**: Configured with Grafana Cloud token
- **Export Interval**: 60 seconds

## What's Being Exported

### Metrics
1. **Runtime Metrics** (.NET performance metrics)
   - GC metrics
   - Thread pool metrics
   - Exception counters
   - Memory usage

2. **Heartbeat Gauge** (`otel_heartbeat_seconds`)
   - Shows time in seconds since the last export
   - Useful for monitoring export frequency and verifying exports are working

3. **StatsManager Metrics** (3 registered)
   - OpenSim-specific performance metrics

### Logs
- Configured to export logs at Information level
- Sent to Grafana Cloud Loki

## Verification Steps

### 1. Check Grafana Cloud Dashboard
Login to your Grafana Cloud account and check:
- **Metrics Explorer**: Look for `otel_heartbeat_seconds` metric
- **Service Name**: Filter by "Akisim.KoPhp"
- **Data should appear**: Within 60-120 seconds of OpenSim startup

### 2. Expected Metrics in Grafana
```
Service: Akisim.KoPhp
Metrics:
  - otel_heartbeat_seconds (gauge, shows seconds since last export)
  - process.runtime.dotnet.* (various .NET runtime metrics)
  - opensim.* (StatsManager metrics)
```

### 3. Check Logs in Grafana
Navigate to Loki/Logs and filter by:
- Service: Akisim.KoPhp
- Look for OpenTelemetry initialization messages

## Troubleshooting

If metrics don't appear in Grafana:
1. Check authorization token is valid
2. Verify network connectivity to otlp-gateway-prod-eu-west-2.grafana.net
3. Check OpenSim logs for export errors
4. Verify Grafana Cloud instance has OTLP endpoint enabled

## Test Commands

Start OpenSim and wait 60-120 seconds for first export:
```bash
cd /home/akira/opensim/grid/working/bin
dotnet OpenSim.dll
```

Check logs for export activity:
```bash
grep -i "opentelemetry" OpenSim.log
```
