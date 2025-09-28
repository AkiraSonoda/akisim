# MonitorModule

## Overview

The **MonitorModule** is a non-shared region module that provides comprehensive performance monitoring, health tracking, and statistical reporting capabilities for OpenSim regions. It collects real-time metrics about region performance, agent activity, physics simulation, and system resources.

## Architecture

### Module Type
- **Interface**: `INonSharedRegionModule`
- **Namespace**: `OpenSim.Region.CoreModules.Framework.Monitoring`
- **Location**: `src/OpenSim.Region.CoreModules/Framework/Monitoring/MonitorModule.cs`

### Dependencies
- `OpenSim.Framework.Monitoring` - Core monitoring framework
- `OpenSim.Framework.Servers.HttpServer` - Web stats endpoints
- Various monitor implementations in the `Monitors` subdirectory
- Alert system implementations in the `Alerts` subdirectory

## Functionality

### Core Features

#### 1. Performance Monitoring
- **Agent Metrics**: Root agents, child agents, NPCs
- **Object Metrics**: Total objects, active objects, active scripts
- **Physics Metrics**: Physics FPS, frame times, simulation performance
- **Network Metrics**: Packet rates, bandwidth usage, pending transfers
- **System Metrics**: Memory usage, garbage collection, thread counts

#### 2. Real-time Statistics
- **Frame Performance**: Total frame time, net time, physics time, agent time
- **Script Performance**: Script events per second, script execution time
- **Simulation Health**: Time dilation, spare frame time, deadlock detection

#### 3. Web Statistics Interface
Provides HTTP endpoints for accessing monitoring data:
- `/monitorstats/{RegionUUID}` - Statistics by region UUID
- `/monitorstats/{RegionName}` - Statistics by region name (URL-encoded)

#### 4. Console Commands
- `monitor report` - Displays comprehensive monitoring report for the region

#### 5. Alert System
- **Deadlock Detection**: Monitors frame time for potential deadlocks
- **Configurable Alerts**: Extensible alert system for various conditions

### Monitor Types

#### Static Monitors
Pre-defined monitors that track specific metrics:

- **AgentCountMonitor** - Root agent count
- **ChildAgentCountMonitor** - Child agent count
- **GCMemoryMonitor** - Garbage collection memory usage
- **ObjectCountMonitor** - Total object count
- **PhysicsFrameMonitor** - Physics simulation frame count
- **PhysicsUpdateFrameMonitor** - Physics update frame count
- **PWSMemoryMonitor** - Process working set memory
- **ThreadCountMonitor** - Active thread count
- **TotalFrameMonitor** - Total simulation frame count
- **EventFrameMonitor** - Event processing frame count
- **LandFrameMonitor** - Land management frame count
- **LastFrameTimeMonitor** - Last frame execution time

#### Generic Monitors
Configurable monitors using lambda expressions:

- **TimeDilationMonitor** - Time dilation factor
- **SimFPSMonitor** - Simulation frames per second
- **PhysicsFPSMonitor** - Physics frames per second
- **AgentUpdatesPerSecondMonitor** - Agent update rate
- **ActiveObjectCountMonitor** - Active object count
- **ActiveScriptsMonitor** - Active script count
- **ScriptEventsPerSecondMonitor** - Script event rate
- **InPacketsPerSecondMonitor** - Incoming packet rate
- **OutPacketsPerSecondMonitor** - Outgoing packet rate
- **UnackedBytesMonitor** - Unacknowledged bytes
- **PendingDownloadsMonitor** - Pending download count
- **PendingUploadsMonitor** - Pending upload count
- Various frame time monitors for different subsystems

## Configuration

### Section: [Monitoring]
```ini
[Monitoring]
    ; Enable/disable the monitoring module
    ; Default: true
    Enabled = true
```

### Factory Integration
The module is loaded through the `CoreModuleFactory` with the following behavior:
- **Default**: Enabled if no configuration is provided
- **Configurable**: Can be disabled via `[Monitoring] Enabled = false`
- **Essential**: Considered essential for region health monitoring

## Implementation Details

### Initialization Process
1. **Configuration Check**: Reads `[Monitoring]` section for enable/disable setting
2. **Default Behavior**: Defaults to enabled if no configuration exists
3. **Debug Logging**: Logs initialization status and configuration decisions

### Region Integration
1. **Command Registration**: Adds `monitor report` console command
2. **HTTP Handlers**: Registers web statistics endpoints
3. **Monitor Setup**: Initializes all static and generic monitors
4. **Alert Configuration**: Sets up deadlock detection and other alerts
5. **Stats Registration**: Registers with StatsManager for centralized statistics

### Cleanup Process
1. **HTTP Handler Removal**: Unregisters web endpoints
2. **Stats Unregistration**: Removes statistics from StatsManager
3. **Resource Cleanup**: Disposes of monitoring resources

### Web Interface
The module provides XML-formatted statistics through HTTP GET requests:

```xml
<data>
    <AgentCountMonitor>5</AgentCountMonitor>
    <ObjectCountMonitor>1248</ObjectCountMonitor>
    <SimFPSMonitor>44.7</SimFPSMonitor>
    <!-- Additional metrics -->
</data>
```

Supports querying specific monitors via URL parameter:
- `GET /monitorstats/{region}?monitor=SimFPSMonitor`

## Usage Examples

### Console Monitoring
```
# Get comprehensive monitoring report
monitor report
```

### Web Statistics Access
```bash
# Get all statistics for a region
curl http://localhost:9000/monitorstats/MyRegion

# Get specific monitor data
curl http://localhost:9000/monitorstats/MyRegion?monitor=SimFPSMonitor
```

### Configuration Examples
```ini
# Enable monitoring (default)
[Monitoring]
Enabled = true

# Disable monitoring
[Monitoring]
Enabled = false
```

## Integration Notes

### Factory Loading
- Loaded via `CoreModuleFactory.CreateNonSharedModules()`
- Includes comprehensive debug and info logging
- Configuration-aware loading with sensible defaults

### Dependencies
- Requires `OpenSim.Framework.Monitoring` assembly
- Uses `MainServer.Instance` for HTTP endpoints
- Integrates with `StatsManager` for centralized statistics

### Performance Impact
- Minimal overhead during normal operation
- Statistics collected during regular simulation cycles
- HTTP endpoints only active when accessed
- Memory usage scales with number of active monitors

## Troubleshooting

### Common Issues
1. **Module Not Loading**: Check `[Monitoring] Enabled` setting
2. **Missing Statistics**: Verify region is properly initialized
3. **Web Access Issues**: Confirm HTTP server is running and ports are accessible
4. **Performance Impact**: Monitor CPU usage if experiencing frame rate issues

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization status
- Monitor setup progress
- HTTP handler registration
- Configuration decisions

## See Also
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [StatsManager Documentation](../docs/StatsManager.md) - Centralized statistics
- [HTTP Server Configuration](../docs/HttpServer.md) - Web endpoint setup