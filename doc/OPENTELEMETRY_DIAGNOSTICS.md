# OpenTelemetry Diagnostics Guide

## Problem: Startup Looks Good But No Data is Sent

If you see the OpenTelemetry initialization logs but no data appears in Grafana Cloud, use this guide to diagnose the issue.

## Common Issues and Solutions

### 1. Protocol Mismatch

**Symptom**: Initialization logs show success but no data in Grafana Cloud

**Check**: Verify the protocol matches your endpoint requirements

**Solution**:
- Grafana Cloud supports both GRPC and HTTP/Protobuf
- Check your `OpenSim.ini` configuration:
  ```ini
  [OpenTelemetry]
      OtlpProtocol = "HttpProtobuf"  ; or "Grpc"
  ```
- The code now reads this setting and uses it correctly

### 2. Authentication Issues

**Symptom**: No errors in logs but no data received

**Check**: Verify authentication token format

**Solution**:
- Grafana Cloud requires format: `instanceId:apiKey`
- Example from your config:
  ```ini
  AuthorizationToken = "1490963:your-api-key"
  ```
- If using the old format, it should still work but consider updating

### 3. Network Connectivity

**Symptom**: No errors but data not appearing

**Check**: Test network connectivity to Grafana Cloud

**Solution**:
```bash
# Test HTTP connectivity
curl -v "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"

# Test with authentication
curl -v -H "Authorization: Bearer 1490963:your-api-key" "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
```

### 4. Metrics Not Being Recorded

**Symptom**: Pipeline initialized but no metrics data

**Check**: Verify metrics are being recorded

**Solution**:
- Enable DEBUG logging to see metric recording:
  ```ini
  [log4net]
      <root>
          <level value="DEBUG" />
      </root>
  ```
- Look for logs like:
  ```
  [OPENTELEMETRY]: Recorded avatar connection metric
  [OPENTELEMETRY]: Recorded script execution metric
  ```

### 5. Export Not Triggered

**Symptom**: Metrics recorded but not exported

**Check**: Manually trigger an export

**Solution**:
- Use the console to manually trigger export:
  ```
  # In OpenSim console:
  OpenTelemetryMetrics.Instance.TestExport()
  ```
- Look for log:
  ```
  [OPENTELEMETRY]: Manually triggering metrics export...
  [OPENTELEMETRY]: Export triggered successfully
  ```

## Diagnostic Steps

### 1. Check Configuration Loading

Verify the configuration is being loaded correctly:
```bash
# Check OpenSim.ini
grep -A 20 "\[OpenTelemetry\]" /home/akira/opensim/grid/akisim/bin/OpenSim.ini
```

Expected output should show:
```ini
Enabled = true
OtlpEndpoint = "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
OtlpProtocol = "HttpProtobuf"
AuthorizationToken = "1490963:your-api-key"
```

### 2. Check Startup Logs

Look for these key messages in your logs:
```
[OPENTELEMETRY]: Loading OpenTelemetry configuration...
[OPENTELEMETRY]: Service Name: Akisim.KoPhp, Version: 1.0.0
[OPENTELEMETRY]: OTLP Endpoint: https://otlp-gateway-prod-eu-west-2.grafana.net/otlp
[OPENTELEMETRY]: OTLP Protocol: HttpProtobuf
[OPENTELEMETRY]: Using AuthorizationToken for authentication
[OPENTELEMETRY]: Grafana Cloud authentication configured (Instance ID: 1490963)
[OPENTELEMETRY]: Metrics export interval: 60000 ms
[OPENTELEMETRY]: Starting OpenTelemetry metrics pipeline...
[OPENTELEMETRY]: ✓ OpenTelemetry metrics initialized successfully!
```

### 3. Check for Errors

Search for any OpenTelemetry errors:
```bash
grep "OPENTELEMETRY" /home/akira/opensim/grid/akisim/logs/*.log | grep -i "error\|fail\|exception"
```

### 4. Test Export Manually

Trigger a manual export to test:
```bash
# Connect to OpenSim console
telnet localhost 9020

# Or use the console directly
OpenTelemetryMetrics.Instance.TestExport()
```

### 5. Check Grafana Cloud Status

Verify Grafana Cloud is operational:
- Go to https://grafana.com/status
- Check if there are any outages

## Configuration Reference

### Required Settings

```ini
[OpenTelemetry]
    Enabled = true
    OtlpEndpoint = "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
    OtlpProtocol = "HttpProtobuf"  ; or "Grpc"
    AuthorizationToken = "1490963:your-api-key"
    ExportIntervalMilliseconds = 60000
```

### Protocol Options

- **Grpc**: Binary protocol, port 4317 (default)
- **HttpProtobuf**: HTTP/JSON protocol, port 4318

Grafana Cloud supports both, but HttpProtobuf is often more reliable through firewalls.

### Authentication Format

Grafana Cloud uses Basic Auth with format:
```
instanceId:apiKey
```

Where:
- `instanceId` = Your Grafana Cloud instance ID (found in OTLP configuration)
- `apiKey` = API token (generate under "Generate now" in OTLP settings)

## Troubleshooting Checklist

1. ✅ Configuration file has `Enabled = true`
2. ✅ Endpoint URL is correct for your region
3. ✅ Protocol matches your network/firewall requirements
4. ✅ Authentication token is in correct format (instanceId:apiKey)
5. ✅ Network can reach Grafana Cloud endpoints
6. ✅ No errors in startup logs
7. ✅ Metrics are being recorded (check with DEBUG logging)
8. ✅ Manual export works (TestExport())

## Advanced Diagnostics

### Enable Detailed Logging

Edit your `OpenSim.exe.config` to enable DEBUG logging:
```xml
<log4net>
    <root>
        <level value="DEBUG" />
        <appender-ref ref="Console" />
        <appender-ref ref="File" />
    </root>
</log4net>
```

### Test with Local Collector

Set up a local OpenTelemetry Collector for debugging:
```ini
[OpenTelemetry]
    OtlpEndpoint = "http://localhost:4317"
    OtlpProtocol = "Grpc"
    AuthorizationToken = ""
```

Then run the collector locally to inspect traffic.

### Check OpenTelemetry SDK Version

Ensure you have the correct OpenTelemetry packages:
```bash
ls -lh bin/OpenTelemetry*.dll
```

## Known Issues

### 1. GRPC vs HTTP Protocol

Some firewalls/proxies block GRPC traffic. Try switching to HttpProtobuf:
```ini
OtlpProtocol = "HttpProtobuf"
```

### 2. Large Metric Volumes

If you have many regions, consider increasing the export interval:
```ini
ExportIntervalMilliseconds = 300000  ; 5 minutes
```

### 3. Memory Pressure

Under high load, exports might be delayed. Monitor memory usage.

## Success Criteria

You should see:
1. ✅ Initialization logs in console
2. ✅ Metric recording logs (with DEBUG enabled)
3. ✅ Successful export logs
4. ✅ Data appearing in Grafana Cloud dashboard

## Support

If you've checked all the above and still have issues:
1. Share your OpenTelemetry configuration section
2. Share relevant log excerpts
3. Share the output of `OpenTelemetryMetrics.Instance.TestExport()`
4. Check Grafana Cloud status page
