# OpenTelemetry Configuration Fix

## Problem

OpenTelemetry data was not being sent because of configuration mismatches:

1. **Configuration Key Mismatch**: The code expected configuration keys like `Endpoint`, `GrafanaInstanceId`, and `GrafanaApiKey`, but the configuration files used different keys like `OtlpEndpoint` and `AuthorizationToken`.

2. **Duplicate Configuration**: OpenTelemetry settings were spread across multiple files:
   - `OpenSim.ini` - Main configuration
   - `config-include/OpenTelemetry.ini` - Duplicate configuration
   - `config-include/GridCommon.ini` - Include statement

3. **Confusing Structure**: The configuration was inconsistent and hard to maintain.

## Solution

### 1. Updated OpenTelemetryMetrics.cs

Modified the `Configure` method in `src/OpenSim.Framework/OpenTelemetryMetrics.cs` to:

- Support both old and new configuration key names for backward compatibility
- Parse the `AuthorizationToken` in Grafana Cloud format (instanceId:apiKey)
- Fall back to old format if new keys are not present

**Key Changes:**
```csharp
// Support both OtlpEndpoint and Endpoint
Endpoint = config.GetString("OtlpEndpoint", config.GetString("Endpoint", Endpoint));

// Parse AuthorizationToken in instanceId:apiKey format
string authToken = config.GetString("AuthorizationToken", "");
if (!string.IsNullOrEmpty(authToken))
{
    string[] parts = authToken.Split(':');
    if (parts.Length == 2)
    {
        GrafanaInstanceId = parts[0];
        GrafanaApiKey = parts[1];
    }
}
```

### 2. Consolidated Configuration

**Moved all OpenTelemetry configuration to OpenSim.ini:**

- Removed active OpenTelemetry configuration from `config-include/OpenTelemetry.ini`
- Updated `config-include/OpenTelemetry.ini` to indicate it's deprecated
- Removed the include statement from `config-include/GridCommon.ini`
- Kept the detailed configuration in `OpenSim.ini` for easy access

### 3. Updated Configuration Files

**OpenSim.ini** - Now contains the complete OpenTelemetry configuration:
```ini
[OpenTelemetry]
    Enabled = true
    ServiceName = "Akisim.KoPhp"
    OtlpEndpoint = "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
    OtlpProtocol = "HttpProtobuf"
    AuthorizationToken = "MTQ5MDk2MzpnbGNfZXlKdklqb2lNVFl6TnpNM01pSXNJbTRpT2lKemRHRmpheTB4TkRrd09UWXpMVzkwYkhBdGQzSnBkR1V0WVd0cExYUnZhMlZ1SWl3aWF5STZJako1YUc4ek5UQnJXRFoyTUVWak5UUlhja0p5TmxjMWN5SXNJbTBpT25zaWNpSTZJbkJ5YjJRdFpYVXRkMlZ6ZEMweUluMTk="
    MetricsEnabled = true
    LogsEnabled = true
    MetricsExportIntervalMs = 60000
    LogLevel = "Information"
```

**config-include/OpenTelemetry.ini** - Now deprecated:
```ini
;; This file is now deprecated. OpenTelemetry configuration has been moved to OpenSim.ini
;; in the [OpenTelemetry] section.
```

## Benefits

1. **Single Source of Truth**: All OpenTelemetry configuration is now in one place
2. **Backward Compatible**: Supports both old and new configuration formats
3. **Clearer Structure**: No more confusion about which file to edit
4. **Better Maintainability**: Easier to update and manage configuration
5. **Proper Authentication**: Correctly handles Grafana Cloud authentication tokens

## Usage

To configure OpenTelemetry:

1. Edit the `[OpenTelemetry]` section in `OpenSim.ini`
2. Set `Enabled = true` to enable telemetry
3. Configure the endpoint, protocol, and authentication
4. Set `MetricsEnabled` and `LogsEnabled` as needed
5. Restart OpenSimulator

## Verification

Check the logs for OpenTelemetry initialization:
```
[OPENTELEMETRY]: OpenTelemetry metrics initialized. Exporting to: https://otlp-gateway-prod-eu-west-2.grafana.net/otlp
```

## Files Modified

1. `src/OpenSim.Framework/OpenTelemetryMetrics.cs` - Updated configuration parsing
2. `doc/bin_delta/akisim_phpgrid_lin/config-include/OpenTelemetry.ini` - Marked as deprecated
3. `doc/bin_delta/akisim_phpgrid_lin/config-include/GridCommon.ini` - Removed include statement
4. `doc/bin_delta/akisim_phpgrid_lin/OpenSim.ini` - Kept as primary configuration (no changes needed)
5. `src/OpenSim.Server.RegionServer/Data/config-include/OpenTelemetry.ini.example` - Updated example

## Migration Guide

If you have an existing `config-include/OpenTelemetry.ini` file:

1. Copy your settings from `config-include/OpenTelemetry.ini` to the `[OpenTelemetry]` section in `OpenSim.ini`
2. Update the key names:
   - `Endpoint` → `OtlpEndpoint`
   - `GrafanaInstanceId` + `GrafanaApiKey` → `AuthorizationToken` (format: "instanceId:apiKey")
3. Remove or comment out the include statement in `GridCommon.ini`
4. Restart OpenSimulator

## Notes

- The configuration is now consistent across all deployment scenarios
- Both old and new configuration formats are supported for backward compatibility
- The deprecated `config-include/OpenTelemetry.ini` is kept for reference but not used
- All future OpenTelemetry configuration should be done in `OpenSim.ini`
