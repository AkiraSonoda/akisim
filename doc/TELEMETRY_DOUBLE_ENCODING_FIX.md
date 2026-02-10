# OpenTelemetry Double-Encoding Fix

## Problem Found

Metrics were being **collected successfully** (console exporter showed them), but **not reaching Grafana Cloud**. The OTLP exporter was failing silently due to **double-encoding** of the authentication credentials.

## Root Cause

### The Authentication Flow (WRONG):

1. **Config has:** `MTQ5MDk2MzpnbGNf...` (base64-encoded `1490963:glc_...`)
2. **Code decodes it to:** `1490963` + `glc_...`
3. **Code re-encodes it:** `MTQ5MDk2MzpnbGNf...` (double-encoded!)
4. **Grafana receives:** Invalid authentication → Rejects silently

### Why This Happened:

The code was designed to handle both:
- Plain text: `1490963:glc_...` → Encode to base64
- Base64: `MTQ5MDk2MzpnbGNf...` → Decode, extract parts, **then re-encode**

The re-encoding step was **unnecessary** - we should use the original base64 token directly!

## The Fix

### Before:
```csharp
// Decode the token
GrafanaInstanceId = "1490963";
GrafanaApiKey = "glc_...";

// Then re-encode it (WRONG!)
string credentials = GrafanaInstanceId + ":" + GrafanaApiKey;
string base64Credentials = Convert.ToBase64String(...);
exporterOptions.Headers = "Authorization=Basic " + base64Credentials;
```

### After:
```csharp
// Decode the token for logging
GrafanaInstanceId = "1490963";
GrafanaApiKey = "glc_...";

// But store the ORIGINAL base64 token
OriginalAuthToken = "MTQ5MDk2MzpnbGNf...";

// Use the original token directly (NO re-encoding!)
exporterOptions.Headers = "Authorization=Basic " + OriginalAuthToken;
```

## Changes Made

### 1. Added Property to Store Original Token
```csharp
public string OriginalAuthToken { get; set; } = "";
```

### 2. Updated Configuration Loading
- When base64 token is detected, **store the original** in `OriginalAuthToken`
- Still decode it to extract `instanceId` for logging
- Don't re-encode it later

### 3. Updated Authentication Header Logic
- **Priority 1:** Use `OriginalAuthToken` if available (no encoding)
- **Priority 2:** Encode plain text credentials (if no original token)
- **Priority 3:** Use single token as-is

## What You'll See Now

### In Startup Logs:
```
[OPENTELEMETRY]: Decoded base64 token - Instance ID: 1490963
[OPENTELEMETRY]: Will use original base64 token for authentication (no double-encoding)
[OPENTELEMETRY]: Basic authentication configured for instance 1490963 (using original token)
```

Key phrase: **"using original token"** instead of just "configured"

## Testing

### Step 1: Restart OpenSim
```bash
cd /home/akira/opensim/grid/akisim/bin
./opensim.sh
```

### Step 2: Check Startup Logs
Look for:
```
[OPENTELEMETRY]: Will use original base64 token for authentication (no double-encoding)
```

### Step 3: Wait 1-2 Minutes
Grafana Cloud should start receiving metrics

### Step 4: Verify in Grafana Cloud
Go to your Grafana Cloud dashboard and look for:
- `process.runtime.dotnet.gc.collections.count`
- `process.runtime.dotnet.timer.count`
- `process.runtime.dotnet.assemblies.count`
- `process.runtime.dotnet.exceptions.count`

With labels:
- `service.name: Akisim.KoPhp`
- `deployment.environment: production`
- `host.name: cachyos`

## Why This Was Hard to Debug

1. **Silent Failure:** OTLP exporter doesn't log authentication errors
2. **Console Exporter Worked:** This proved metrics were being collected
3. **Logs Looked Good:** "Authentication configured" message appeared
4. **The Logic Seemed Correct:** Decoding → Re-encoding makes sense... except when the input is already encoded!

## Verification Checklist

After restarting OpenSim:

- [ ] Startup shows "Will use original base64 token for authentication"
- [ ] Startup shows "(using original token)" in authentication message
- [ ] Console exporter still shows metrics (optional, can disable)
- [ ] Wait 2 minutes
- [ ] Grafana Cloud shows runtime metrics
- [ ] Service name appears as "Akisim.KoPhp"

## Next Steps

Once metrics are flowing:

1. **Disable Console Exporter** (optional):
   Edit `OpenSim.ini`:
   ```ini
   EnableConsoleExporter = false
   ```

2. **Add Custom Metrics:**
   - Integrate `RecordAvatarConnection()` into avatar login code
   - Integrate `RecordScriptExecution()` into script engine
   - Integrate `RecordFrameTime()` into scene heartbeat

3. **Create Grafana Dashboards:**
   - Runtime performance dashboard
   - Avatar activity dashboard
   - Script execution monitoring

## Files Modified

- ✅ `src/OpenSim.Framework/OpenTelemetryMetrics.cs`
  - Added `OriginalAuthToken` property
  - Store original base64 token during configuration
  - Use original token directly (no re-encoding)
- ✅ Built and deployed successfully

## Success Criteria

✅ **The fix is working when:**
1. Grafana Cloud receives metrics within 2 minutes
2. Metrics show correct service name and labels
3. No authentication errors (check Grafana Cloud logs if available)

If metrics still don't appear after this fix, the issue is likely:
- Network/firewall blocking OTLP/gRPC traffic (try HttpProtobuf protocol)
- Invalid API token (regenerate in Grafana Cloud)
- Wrong Grafana Cloud endpoint URL
