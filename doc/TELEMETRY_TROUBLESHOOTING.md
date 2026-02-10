# OpenTelemetry Troubleshooting - No Data Reaching Grafana

## Problem Summary
OpenTelemetry pipeline initializes successfully but no metrics appear in Grafana Cloud.

## Recent Diagnostic Improvements

### 1. Console Commands Added
Two new console commands are available for testing:

```bash
# In OpenSim console:
telemetry export    # Manually trigger metrics export
telemetry test      # Record test metrics and export
```

### 2. Console Exporter (Debug Mode)
Added optional console exporter to see metrics locally before they go to Grafana.

**Configuration:**
```ini
[OpenTelemetry]
    Enabled = true
    EnableConsoleExporter = true  # Add this line for debugging
    OtlpEndpoint = "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
    OtlpProtocol = "HttpProtobuf"
    AuthorizationToken = "1490963:your-api-key"
    ExportIntervalMilliseconds = 60000
```

When enabled, metrics will be printed to the console every 10 seconds, allowing you to verify:
- Metrics are being collected
- Runtime instrumentation is working
- Data format is correct

### 3. Automatic Test Metrics on Startup
The system now records a test metric on startup and triggers an export after 2 seconds. Check your logs for:
```
[OPENTELEMETRY]: Recording test metric to verify pipeline...
[OPENTELEMETRY]: Triggering immediate test export...
[OPENTELEMETRY]: Executing test export...
[OPENTELEMETRY]: ✓ Test export completed
```

### 4. Enhanced Internal Diagnostics
- Enabled HTTP/2 support for GRPC
- Added Activity listeners for OpenTelemetry internal events
- More detailed logging during initialization and export

## Testing Procedure

### Step 1: Enable Console Exporter
Edit your `OpenSim.ini`:
```ini
[OpenTelemetry]
    EnableConsoleExporter = true
```

### Step 2: Rebuild and Deploy
```bash
make build
make deploy
```

### Step 3: Start OpenSim and Monitor Output
Look for:
1. Initialization logs showing successful configuration
2. Test metric being recorded on startup
3. Console exporter output (if enabled) showing metrics data
4. Test export completion

### Step 4: Use Console Commands
```bash
# Test basic export
telemetry export

# Record and export test metrics
telemetry test
```

### Step 5: Check Console Output
If `EnableConsoleExporter = true`, you should see output like:
```
Export opensim.avatar.connections, Meter: OpenSimulator/1.0.0
(2026-01-26T...) test: startup Value: 1
```

This confirms:
- ✅ Metrics are being created
- ✅ Metrics are being recorded
- ✅ Export pipeline is working

If you see console output but nothing in Grafana, the issue is with the OTLP exporter configuration (endpoint, auth, network).

## Common Issues

### Issue 1: Runtime Metrics Not Collected
**Symptom:** No metrics at all, even in console exporter

**Possible causes:**
- `AddRuntimeInstrumentation()` requires running .NET process to generate metrics
- May take 1-2 minutes to collect first runtime metrics
- Some metrics only appear under load

**Solution:** Wait a few minutes, run `telemetry test` to generate custom metrics

### Issue 2: Console Shows Metrics But Grafana Doesn't
**Symptom:** Console exporter works but Grafana shows nothing

**Possible causes:**
- Authentication issue (wrong token format)
- Network connectivity (firewall blocking HTTPS)
- Wrong endpoint URL
- Protocol mismatch (Grpc vs HttpProtobuf)

**Solutions:**
1. Test endpoint connectivity:
```bash
curl -v "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
```

2. Verify authentication token format:
```ini
AuthorizationToken = "instanceId:apiKey"  # Must include colon
```

3. Try different protocol:
```ini
OtlpProtocol = "Grpc"  # or "HttpProtobuf"
```

### Issue 3: No Errors But No Metrics
**Symptom:** Initialization succeeds, no errors, but no metrics anywhere

**Possible causes:**
- Metrics not being recorded (none of the Record* methods are called)
- Export interval too long
- MeterProvider not built correctly

**Solutions:**
1. Enable console exporter to verify export is happening
2. Use `telemetry test` to force metric recording
3. Reduce export interval for testing:
```ini
ExportIntervalMilliseconds = 10000  # 10 seconds instead of 60
```

## What Should Be Working Now

After these changes:

1. **Runtime Metrics** - Should appear automatically:
   - `process.runtime.dotnet.gc.collections.count`
   - `process.runtime.dotnet.gc.heap.size`
   - `process.runtime.dotnet.gc.allocations.size`
   - `process.runtime.dotnet.thread.count`
   - Many others from .NET runtime

2. **Custom Metrics** - Available via console commands:
   - `opensim.avatar.connections`
   - `opensim.script.executions`
   - `opensim.frame.duration`
   - `opensim.sessions.active`

3. **Test Metrics** - Recorded on startup automatically

## Next Steps

Once you verify the export pipeline is working (via console exporter), you can:

1. Disable console exporter:
```ini
EnableConsoleExporter = false
```

2. Integrate metric recording into actual OpenSim code:
   - Hook `RecordAvatarConnection()` into ScenePresence
   - Hook `RecordScriptExecution()` into YEngine
   - Hook `RecordFrameTime()` into Scene heartbeat

3. Add more custom metrics as needed

## Verification Checklist

- [ ] Build succeeds without errors
- [ ] OpenSim starts without OpenTelemetry errors
- [ ] Console exporter shows metrics output (if enabled)
- [ ] `telemetry test` command works
- [ ] `telemetry export` command completes without errors
- [ ] Grafana Cloud receives data (check after 1-2 minutes)

## Files Modified

- `src/OpenSim.Framework/OpenTelemetryMetrics.cs` - Added diagnostics and test features
- `src/OpenSim.Framework/OpenSim.Framework.csproj` - Added console exporter package
- `src/OpenSim.Server.RegionServer/OpenSimBase.cs` - Added console commands
