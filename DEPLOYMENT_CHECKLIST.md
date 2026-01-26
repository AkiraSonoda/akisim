# OpenTelemetry Fix - Deployment Checklist

## ✅ Pre-Deployment Checklist

### 1. Configuration Verification

- [ ] Check `OpenSim.ini` has correct OpenTelemetry settings:
  ```ini
  [OpenTelemetry]
      Enabled = true
      OtlpEndpoint = "https://otlp-gateway-prod-eu-west-2.grafana.net/otlp"
      OtlpProtocol = "HttpProtobuf"
      AuthorizationToken = "1490963:your-api-key"
      ServiceName = "Akisim.KoPhp"
      ServiceVersion = "1.0.0"
      ExportIntervalMilliseconds = 60000
  ```

- [ ] Verify `GridCommon.ini` has no OpenTelemetry include:
  ```ini
  ;; Include-OpenTelemetry = "config-include/OpenTelemetry.ini"
  ```

### 2. Build Verification

- [ ] Build completed successfully: `make build`
- [ ] 0 errors reported
- [ ] Binaries exist in `bin/` directory

### 3. Architecture Verification

- [ ] Run verification script: `bash verify_architecture.sh`
- [ ] All libraries show "ELF 64-bit" in output

## 🚀 Deployment Steps

### 1. Deploy the Build

```bash
make deploy
```

### 2. Verify Deployment

```bash
# Check deployed configuration
ls -lh /home/akira/opensim/grid/akisim/bin/OpenSim.ini

# Verify architecture
bash verify_architecture.sh
```

### 3. Start OpenSimulator

```bash
cd /home/akira/opensim/grid/akisim/bin
dotnet ./OpenSim.dll
```

## 🔍 Post-Deployment Verification

### 1. Check Startup Logs

Look for these messages in console logs:
```
[OPENTELEMETRY]: Loading OpenTelemetry configuration...
[OPENTELEMETRY]: Service Name: Akisim.KoPhp, Version: 1.0.0
[OPENTELEMETRY]: OTLP Endpoint: https://otlp-gateway-prod-eu-west-2.grafana.net/otlp
[OPENTELEMETRY]: OTLP Protocol: HttpProtobuf
[OPENTELEMETRY]: Using AuthorizationToken for authentication
[OPENTELEMETRY]: Grafana Cloud authentication configured (Instance ID: 1490963)
[OPENTELEMETRY]: ✓ OpenTelemetry metrics initialized successfully!
```

### 2. Test Metrics Export

```
# In OpenSim console:
OpenTelemetryMetrics.Instance.TestExport()

# Look for:
[OPENTELEMETRY]: Manually triggering metrics export...
[OPENTELEMETRY]: Export triggered successfully
```

### 3. Enable DEBUG Logging (Optional)

Edit `OpenSim.exe.config`:
```xml
<log4net>
    <root>
        <level value="DEBUG" />
    </root>
</log4net>
```

Restart and look for metric recording logs:
```
[OPENTELEMETRY]: Recorded avatar connection metric
[OPENTELEMETRY]: Recorded script execution metric
```

## 📊 Verify Data in Grafana Cloud

1. Log in to Grafana Cloud
2. Navigate to Explore section
3. Select your OpenTelemetry data source
4. Look for metrics with names like:
   - `opensim.avatar.connections`
   - `opensim.script.executions`
   - `opensim.frame.duration`
   - `opensim.sessions.active`

## ⚠️ Troubleshooting

### No Data in Grafana Cloud

1. Check startup logs for errors
2. Test export manually: `OpenTelemetryMetrics.Instance.TestExport()`
3. Verify network connectivity to Grafana Cloud
4. Check Grafana Cloud status page: https://grafana.com/status

### Authentication Errors

- Verify `AuthorizationToken` format: `instanceId:apiKey`
- Check that both instance ID and API key are correct
- Regenerate API key if needed

### Protocol Issues

- Try switching protocol:
  ```ini
  OtlpProtocol = "Grpc"  ; or "HttpProtobuf"
  ```
- HttpProtobuf often works better through firewalls

### Metrics Not Recording

- Enable DEBUG logging
- Check if metrics are being recorded
- Verify that avatars/scripts are actually being used

## 📝 Documentation References

- `OPENTELEMETRY_DIAGNOSTICS.md` - Comprehensive troubleshooting guide
- `OPENTELEMETRY_CONFIG_FIX.md` - Configuration details
- `FINAL_SUMMARY.md` - Complete overview of changes

## ✅ Success Criteria

- [ ] OpenSimulator starts without errors
- [ ] OpenTelemetry initialization logs appear
- [ ] Manual export works: `TestExport()`
- [ ] Metrics appear in Grafana Cloud dashboard
- [ ] No architecture errors (ELF class issues)

## 🎯 Expected Behavior

After successful deployment, you should see:

1. **Startup**: Detailed OpenTelemetry initialization logs
2. **Runtime**: Metric recording logs (with DEBUG enabled)
3. **Export**: Automatic exports every 60 seconds
4. **Dashboard**: Metrics appearing in Grafana Cloud

## 📞 Support

If you encounter issues:

1. Check all boxes in this checklist
2. Review `OPENTELEMETRY_DIAGNOSTICS.md`
3. Share relevant log excerpts
4. Verify Grafana Cloud status

## 🎉 Completion

Once all success criteria are met, deployment is complete! 🎉

**Estimated Time**: 15-30 minutes
**Complexity**: Medium
**Risk Level**: Low (backward compatible)
