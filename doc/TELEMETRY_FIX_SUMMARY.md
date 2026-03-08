# OpenTelemetry Fix Summary - Why Metrics Weren't Reaching Grafana

## Root Causes Found

### 1. **Authentication Format Issue** ✅ FIXED
**Problem:** Your config has the token base64-encoded (correct!), but the code wasn't decoding it properly.
- Config had: `AuthorizationToken = "MTQ5MDk2MzpnbGNf..."` (base64)
- Code was treating it as a single token instead of decoding to get `instanceId:apiKey`

**Fix:** Added base64 detection and decoding logic to properly parse the token.

### 2. **Wrong Authentication Scheme** ✅ FIXED
**Problem:** Code was using `Authorization: Bearer` but Grafana Cloud requires `Authorization: Basic`
- Old: `Authorization=Bearer <token>`
- New: `Authorization=Basic <base64(instanceId:apiKey)>`

**Fix:** Changed to use `Basic` authentication with properly base64-encoded credentials.

### 3. **Wrong Endpoint for HttpProtobuf** ✅ FIXED
**Problem:** Grafana Cloud needs different paths for different protocols:
- Grpc: `https://otlp-gateway-prod-eu-west-2.grafana.net/otlp`
- HttpProtobuf: `https://otlp-gateway-prod-eu-west-2.grafana.net/otlp/v1/metrics`

**Fix:** Code now automatically appends `/v1/metrics` when using HttpProtobuf protocol.

## Changes Made

### File: `src/OpenSim.Framework/OpenTelemetryMetrics.cs`

1. **Added base64 token detection and decoding:**
   - Detects if token is base64-encoded
   - Decodes to extract `instanceId:apiKey`
   - Handles both encoded and plain-text formats

2. **Fixed authentication header format:**
   - Changed from `Bearer` to `Basic`
   - Properly re-encodes credentials as base64
   - Logs instance ID for verification

3. **Automatic endpoint path adjustment:**
   - Adds `/v1/metrics` suffix for HttpProtobuf protocol
   - Leaves Grpc endpoints unchanged

4. **Enhanced logging:**
   - Shows decoded instance ID
   - Indicates authentication method being used
   - Shows adjusted endpoint URLs

## Testing Instructions

### Step 1: Enable Console Exporter (Recommended)

Edit `/home/akira/opensim/grid/akisim/bin/OpenSim.ini` and add this line to the `[OpenTelemetry]` section:

```ini
[OpenTelemetry]
    Enabled = true
    EnableConsoleExporter = true    # ADD THIS LINE
    ServiceName = "Akisim.KoPhp"
    OtlpEndpoint = "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
    OtlpProtocol = "Grpc"  # or "HttpProtobuf"
    AuthorizationToken = "MTQ5MDk2MzpnbGNf..."
```

### Step 2: Restart OpenSim

```bash
cd /home/akira/opensim/grid/akisim/bin
./opensim.sh
```

### Step 3: Check Startup Logs

You should now see:
```
[OPENTELEMETRY]: Decoded base64 token - Instance ID: 1490963
[OPENTELEMETRY]: Basic authentication configured for instance 1490963
```

Instead of the old:
```
[OPENTELEMETRY]: Using single token authentication
```

### Step 4: Wait for Metrics Export

- With console exporter enabled: Metrics will print to console every 10 seconds
- Check Grafana Cloud after 1-2 minutes for data

### Step 5: Use Test Commands

In the OpenSim console:
```
telemetry test     # Record test metrics and export immediately
telemetry export   # Manually trigger export
```

## What You Should See Now

### In Console (if EnableConsoleExporter = true):
```
Export opensim.avatar.connections, Meter: OpenSimulator/1.0.0
(2026-01-26T...) test: startup Value: 1

Export process.runtime.dotnet.gc.collections.count, Meter: OpenTelemetry.Instrumentation.Runtime
...
```

### In Grafana Cloud:
Within 1-2 minutes, you should see:
- **Runtime Metrics:**
  - `process.runtime.dotnet.gc.collections.count`
  - `process.runtime.dotnet.gc.heap.size`
  - `process.runtime.dotnet.thread.count`
  - Many others from .NET runtime instrumentation

- **Custom Metrics (when test command is used):**
  - `opensim.avatar.connections`
  - `opensim.script.executions`
  - `opensim.frame.duration`

## Protocol Recommendations

### Try Both Protocols:

**Option 1: Grpc (Current)**
```ini
OtlpProtocol = "Grpc"
OtlpEndpoint = "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
```

**Option 2: HttpProtobuf (May work better through firewalls)**
```ini
OtlpProtocol = "HttpProtobuf"
OtlpEndpoint = "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
# Code will automatically append /v1/metrics
```

## Troubleshooting

### If still no metrics:

1. **Check authentication worked:**
   Look for: `[OPENTELEMETRY]: Decoded base64 token - Instance ID: 1490963`

2. **Enable console exporter to verify collection:**
   ```ini
   EnableConsoleExporter = true
   ```

3. **Test network connectivity:**
   ```bash
   curl -v https://otlp-gateway-prod-eu-west-2.grafana.net/otlp
   ```

4. **Try different protocol:**
   Switch between Grpc and HttpProtobuf

5. **Check export interval:**
   Default is 60 seconds. For testing, reduce to 10 seconds:
   ```ini
   ExportIntervalMilliseconds = 10000
   ```

## Next Steps

Once metrics are flowing:

1. Disable console exporter (for production):
   ```ini
   EnableConsoleExporter = false
   ```

2. Integrate metric recording into actual OpenSim events:
   - Avatar connections → `RecordAvatarConnection()`
   - Script executions → `RecordScriptExecution()`
   - Frame time → `RecordFrameTime(ms)`

3. Add more custom metrics as needed

## Verification Checklist

- [ ] Startup shows "Decoded base64 token - Instance ID: 1490963"
- [ ] Startup shows "Basic authentication configured"
- [ ] Console exporter shows metrics output (if enabled)
- [ ] No errors in OpenTelemetry initialization
- [ ] `telemetry test` command works without errors
- [ ] Grafana Cloud shows data within 1-2 minutes

## What Changed in Code

```diff
- authToken.Split(':')  // Didn't work with base64
+ Convert.FromBase64String(authToken)  // Decode first

- Authorization=Bearer instanceId:apiKey  // Wrong format
+ Authorization=Basic <base64(instanceId:apiKey)>  // Correct format

- Endpoint = ".../otlp"  // Missing path for HttpProtobuf
+ Endpoint = ".../otlp/v1/metrics"  // Correct for HttpProtobuf
```

## Files Modified

- ✅ `src/OpenSim.Framework/OpenTelemetryMetrics.cs` - Fixed auth and endpoint handling
- ✅ Built successfully with 0 errors
- ✅ Deployed to `/home/akira/opensim/grid/akisim/bin`

## Success Criteria

The fix is successful when you see:
1. ✅ Correct instance ID in logs (1490963)
2. ✅ "Basic authentication configured" message
3. ✅ Metrics in console (if enabled) or Grafana Cloud
4. ✅ No authentication or export errors
