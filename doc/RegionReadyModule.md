# RegionReadyModule

## Overview

The **RegionReadyModule** is a non-shared region module that coordinates region startup processes and provides notifications when regions become fully operational. It monitors script compilation completion, manages login access during initialization, and provides external notifications about region readiness status. This module is essential for automated deployment pipelines and load balancers that need to know when regions are ready to accept users.

## Architecture

### Module Type
- **Interface**: `INonSharedRegionModule`, `IRegionReadyModule`
- **Namespace**: `OpenSim.Region.OptionalModules.Scripting.RegionReady`
- **Location**: `src/OpenSim.Region.OptionalModules/Scripting/RegionReadyModule/RegionReadyModule.cs`

### Dependencies
- **HTTP Framework**: HttpClient for external service notifications
- **Event System**: Scene event manager for script compilation and OAR loading events
- **Chat System**: Regional chat for readiness broadcast messages
- **Scene Services**: Grid service integration for neighbor notifications

## Functionality

### Core Features

#### 1. Script Compilation Monitoring
- **Queue Tracking**: Monitors script compilation queue completion
- **Failure Reporting**: Tracks number of failed script compilations
- **First-Run Detection**: Distinguishes between initial startup and subsequent OAR loads
- **Completion Notification**: Triggers region ready state when all scripts are compiled

#### 2. Login Management
- **Login Disable**: Optionally disables logins during region initialization
- **Automatic Enable**: Re-enables logins when region becomes ready
- **Login Lock**: Prevents user access during script compilation
- **Console Messages**: Provides clear status updates to administrators

#### 3. External Service Integration
- **HTTP Notifications**: Sends JSON alerts to external monitoring services
- **Status Updates**: Notifies services of disabled/enabled/shutdown status
- **Authentication**: Provides region identification in alert messages
- **Error Handling**: Robust error handling for network communication

#### 4. OAR Loading Support
- **OAR Detection**: Monitors OAR file loading operations
- **Error Tracking**: Reports OAR loading errors and success status
- **Compilation Coordination**: Coordinates script compilation after OAR loading

#### 5. Region Coordination
- **Neighbor Notification**: Informs neighboring regions when ready
- **Grid Integration**: Integrates with grid service for region status
- **Garbage Collection**: Performs memory cleanup when region becomes ready
- **Chat Broadcasting**: Sends region ready messages via chat system

### Readiness States

#### Initialization Phase
1. **Module Loading**: RegionReadyModule loads and configures itself
2. **Event Registration**: Registers for script compilation and OAR loading events
3. **Login Disable**: Optionally disables logins if configured
4. **External Alert**: Sends "disabled" status to external services

#### Monitoring Phase
1. **Script Compilation**: Waits for script compilation queue to empty
2. **OAR Loading**: Monitors OAR file loading if applicable
3. **Status Tracking**: Tracks compilation failures and errors

#### Ready Phase
1. **Queue Empty**: Script compilation queue becomes empty
2. **Cleanup**: Performs garbage collection and memory optimization
3. **Login Enable**: Re-enables logins if they were disabled
4. **Notifications**: Sends region ready messages and external alerts
5. **Neighbor Update**: Informs neighboring regions of availability

## Configuration

### Section: [RegionReady]
```ini
[RegionReady]
    ; Enable/disable the region ready module
    ; Default: false
    enabled = true

    ; Chat channel for region ready notifications
    ; Default: -1000
    channel_notify = -1000

    ; Disable logins during initialization
    ; Default: false
    login_disable = true

    ; External alert URI for HTTP notifications
    ; Default: empty (no external notifications)
    alert_uri = http://monitor.example.com/region-status
```

### Factory Integration
The module is loaded through the `CoreModuleFactory` with the following behavior:
- **Configuration-Driven**: Only loaded when `[RegionReady] enabled = true`
- **Reflection-Based**: Loaded via reflection to avoid hard dependency on OptionalModules
- **Graceful Fallback**: Warns if configured but dependencies unavailable

## Implementation Details

### Initialization Process
1. **Configuration Reading**: Loads settings from [RegionReady] section
2. **Event Handler Setup**: Registers for script compilation and OAR loading events
3. **Interface Registration**: Registers IRegionReadyModule interface with scene
4. **Initial State**: Sets up tracking variables and initial ready state

### Script Compilation Monitoring
1. **Event Subscription**: Subscribes to OnEmptyScriptCompileQueue event
2. **First Run Detection**: Tracks whether this is initial startup or OAR reload
3. **Failure Counting**: Monitors number of scripts that failed to compile
4. **Completion Handling**: Triggers region ready when queue empties

### Login Management
When `login_disable = true`:
1. **Login Lock**: Sets Scene.LoginLock = true during initialization
2. **Console Output**: Displays clear "LOGINS DISABLED" message
3. **Event Monitoring**: Waits for script compilation completion
4. **Auto-Enable**: Re-enables logins when region becomes ready

### External Service Integration

#### Alert Message Format
```json
{
    "alert": "region_ready",
    "login": "disabled|enabled|shutdown",
    "region_name": "RegionName",
    "region_id": "550e8400-e29b-41d4-a716-446655440000"
}
```

#### Alert States
- **disabled**: Region is initializing, logins disabled
- **enabled**: Region is ready, logins enabled
- **shutdown**: Region is shutting down

#### HTTP Request Details
- **Method**: POST
- **Content-Type**: application/json
- **Headers**: Connection: close, no chunked encoding
- **Body**: JSON alert message

### Memory Management
When region becomes ready:
1. **Heap Compaction**: Enables large object heap compaction
2. **Garbage Collection**: Performs full GC cycle with finalization
3. **Memory Optimization**: Optimizes memory layout for operational use
4. **Restoration**: Restores default GC settings

### Chat Broadcasting

#### Message Format
The module broadcasts structured messages on the configured channel:

**Server Startup**: `server_startup,{failed_count},{message}`
**OAR Load**: `oar_file_load,{success},{failed_count},{message}`

Where:
- `failed_count`: Number of scripts that failed to compile
- `success`: 1 for successful OAR load, 0 for failed
- `message`: Additional status information

## Usage Examples

### Basic Configuration
```ini
[RegionReady]
enabled = true
login_disable = true
channel_notify = -1000
```

### Advanced Configuration with External Monitoring
```ini
[RegionReady]
enabled = true
login_disable = true
channel_notify = -1500
alert_uri = http://loadbalancer.example.com/opensim/status
```

### External Service Implementation
```php
<?php
// Example external service endpoint
$input = file_get_contents('php://input');
$alert = json_decode($input, true);

if ($alert['alert'] === 'region_ready') {
    $region_name = $alert['region_name'];
    $region_id = $alert['region_id'];
    $status = $alert['login'];

    switch ($status) {
        case 'disabled':
            // Remove region from load balancer
            removeRegionFromPool($region_name, $region_id);
            break;

        case 'enabled':
            // Add region to load balancer
            addRegionToPool($region_name, $region_id);
            break;

        case 'shutdown':
            // Remove region and mark as offline
            removeRegionFromPool($region_name, $region_id);
            markRegionOffline($region_name, $region_id);
            break;
    }
}
?>
```

### Script-Based Monitoring
```lsl
// LSL script to monitor region ready status
default
{
    state_entry()
    {
        llListen(-1000, "", "", "");
    }

    listen(integer channel, string name, key id, string message)
    {
        if (name == "RegionReady")
        {
            list parts = llParseString2List(message, [","], []);
            string event_type = llList2String(parts, 0);

            if (event_type == "server_startup")
            {
                integer failed_scripts = llList2Integer(parts, 1);
                string status_msg = llList2String(parts, 2);

                llOwnerSay("Region startup complete. Failed scripts: " + (string)failed_scripts);
            }
            else if (event_type == "oar_file_load")
            {
                integer success = llList2Integer(parts, 1);
                integer failed_scripts = llList2Integer(parts, 2);

                if (success)
                    llOwnerSay("OAR loaded successfully. Failed scripts: " + (string)failed_scripts);
                else
                    llOwnerSay("OAR load failed. Check server logs.");
            }
        }
    }
}
```

## Performance Considerations

### Memory Impact
- **Garbage Collection**: Performs intensive GC when region becomes ready
- **Heap Compaction**: Temporarily increases memory usage during compaction
- **Event Handlers**: Minimal memory overhead for event subscriptions
- **HTTP Clients**: Properly disposed HTTP resources prevent memory leaks

### Network Impact
- **External Alerts**: Lightweight JSON messages to external services
- **Error Handling**: Graceful handling of network failures
- **Connection Management**: Proper connection cleanup and disposal
- **Retry Logic**: No automatic retries - failures logged and reported

### Startup Performance
- **Non-Blocking**: Does not block region startup process
- **Event-Driven**: Responds to events rather than polling
- **Minimal Overhead**: Low CPU usage during monitoring phase
- **Quick Response**: Immediate response to compilation completion

## Troubleshooting

### Common Issues

#### 1. Module Not Loading
**Symptoms**: No region ready coordination, login management not working
**Solutions**:
- Check `[RegionReady] enabled = true` in configuration
- Verify OptionalModules.dll is available
- Check for initialization errors in log files

#### 2. Scripts Not Compiling
**Symptoms**: Region never becomes ready, logins remain disabled
**Solutions**:
- Check for script compilation errors in logs
- Verify script engine is properly configured
- Monitor script compilation queue manually
- Check for stuck or infinite compilation loops

#### 3. External Alerts Not Sent
**Symptoms**: External services not receiving notifications
**Solutions**:
- Verify alert_uri is correctly configured and accessible
- Check network connectivity and firewall rules
- Monitor HTTP error messages in logs
- Test external service endpoint manually

#### 4. Login Management Issues
**Symptoms**: Logins not disabled during startup or not re-enabled
**Solutions**:
- Verify `login_disable = true` is configured
- Check that script compilation is completing
- Monitor console messages for status updates
- Verify no other modules are interfering with login state

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization and configuration
- Script compilation queue monitoring
- External alert preparation and sending
- Region ready state transitions
- OAR loading event handling

### Console Messages
The module provides clear console output for key events:
- `Region {name} - LOGINS DISABLED DURING INITIALIZATION.`
- `INITIALIZATION COMPLETE FOR {name} - LOGINS ENABLED`

### Monitoring External Services
Monitor these aspects of external service integration:
- **HTTP Response Codes**: Ensure external services return 2xx status codes
- **Response Times**: Monitor latency to external services
- **Alert Frequency**: Track frequency of alert messages
- **Service Availability**: Monitor uptime of external alert endpoints

## Integration Notes

### Factory Loading
- Loaded via `CoreModuleFactory.CreateNonSharedModules()` using reflection
- Requires `OpenSim.Region.OptionalModules.dll` assembly
- Graceful degradation if OptionalModules unavailable

### Event System Integration
- Uses Scene.EventManager for script compilation events
- Integrates with OAR loading system for archive events
- Coordinates with chat system for ready broadcasts

### Interface Implementation
- Implements `IRegionReadyModule` for external access
- Provides `TriggerRegionReady()` method for manual triggering
- Supports `OarLoadingAlert()` for OAR loading coordination

### Load Balancer Integration
The module is designed to work with load balancers and automated deployment systems:
- **Health Checks**: External alerts can drive health check systems
- **Automatic Scaling**: Integration with auto-scaling systems
- **Deployment Pipelines**: Coordination with CI/CD deployment processes
- **Monitoring Systems**: Integration with monitoring and alerting platforms

## Security Considerations

### Network Security
- **HTTPS Support**: Use HTTPS URLs for external alert endpoints
- **Authentication**: Consider authentication for external service endpoints
- **Network Isolation**: Restrict external service network access appropriately

### Information Disclosure
- **Region Information**: Alert messages contain region names and UUIDs
- **Status Information**: External services receive detailed status information
- **Error Logging**: Avoid logging sensitive information in error messages

### Service Security
- **Endpoint Validation**: Validate external service endpoints before use
- **Rate Limiting**: Consider implementing rate limiting for alert messages
- **Error Handling**: Graceful handling of service failures and attacks

## See Also
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [Script Engine Documentation](../docs/ScriptEngine.md) - Script compilation system
- [Scene Management](../docs/SceneManagement.md) - Region lifecycle management
- [Load Balancer Integration](../docs/LoadBalancer.md) - External service integration patterns