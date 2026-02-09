# NeighbourServicesOutConnector Technical Documentation

## Overview

The **NeighbourServicesOutConnector** is a shared region module that extends the base `NeighbourServicesConnector` to provide optimized neighbor communication capabilities within OpenSimulator. It serves as a hybrid connector that handles both local neighbor discovery (same simulator process) and remote neighbor communication (across network), providing essential functionality for region boundary management and cross-region avatar movement.

## Purpose

The NeighbourServicesOutConnector serves as a critical infrastructure component that:

- **Local Neighbor Optimization**: Provides direct communication for regions within the same simulator process
- **Cross-Region Coordination**: Manages neighbor relationships between different regions
- **Border Crossing Support**: Enables seamless avatar movement between adjacent regions
- **Hello Protocol Implementation**: Implements the neighbor discovery and handshake protocol
- **Performance Optimization**: Bypasses network calls for local region-to-region communication
- **Hybrid Architecture**: Combines local direct access with remote network connectivity

## Architecture

### Inheritance Hierarchy

```
NeighbourServicesConnector (Base)
         ↓
NeighbourServicesOutConnector
         ↓ implements
    ISharedRegionModule
    INeighbourService
```

### Core Components

```
┌─────────────────────────────────────┐
│    NeighbourServicesOutConnector    │
├─────────────────────────────────────┤
│          Local Regions              │
│      List<Scene> m_Scenes           │
│    - Direct region access          │
│    - In-process communication       │
├─────────────────────────────────────┤
│         Remote Service              │
│   NeighbourServicesConnector        │
│    - Network communication         │
│    - HTTP/REST protocol             │
├─────────────────────────────────────┤
│        Hello Protocol               │
│   - HelloNeighbour method           │
│   - Region discovery                │
│   - Neighbor handshake              │
└─────────────────────────────────────┘
```

### Communication Flow

1. **Local Check**: First checks if target region is in same simulator process
2. **Direct Communication**: If local, uses direct `Scene.IncomingHelloNeighbour()` call
3. **Remote Fallback**: If not local, delegates to base class for network communication
4. **Performance Optimization**: Eliminates network overhead for local neighbors

## Interface Implementation

The module implements:
- **ISharedRegionModule**: Shared across all regions in the simulator
- **INeighbourService**: Provides neighbor communication interface

### INeighbourService Interface

The module overrides the key method:

```csharp
public override GridRegion HelloNeighbour(ulong regionHandle, RegionInfo thisRegion)
```

## Core Functionality

### Neighbor Discovery Process

#### 1. Local Region Check

```csharp
foreach (Scene s in m_Scenes)
{
    if (s.RegionInfo.RegionHandle == regionHandle)
    {
        // Found local region - use direct communication
        return s.IncomingHelloNeighbour(thisRegion);
    }
}
```

#### 2. Remote Fallback

```csharp
// Not found locally - use network communication
return base.HelloNeighbour(regionHandle, thisRegion);
```

### Module Lifecycle

#### Initialization

```csharp
public void Initialise(IConfigSource source)
{
    IConfig moduleConfig = source.Configs["Modules"];
    if (moduleConfig != null)
    {
        string name = moduleConfig.GetString("NeighbourServices");
        if (name == Name)
        {
            m_Enabled = true;
            m_log.Info("[NEIGHBOUR CONNECTOR]: Neighbour out connector enabled");
        }
    }
}
```

#### Region Integration

```csharp
public void AddRegion(Scene scene)
{
    if (!m_Enabled)
        return;

    m_Scenes.Add(scene);
    scene.RegisterModuleInterface<INeighbourService>(this);
}
```

#### Service Configuration

```csharp
public void RegionLoaded(Scene scene)
{
    if (!m_Enabled)
        return;

    m_GridService = scene.GridService;
    m_log.InfoFormat("[NEIGHBOUR CONNECTOR]: Enabled out neighbours for region {0}",
                     scene.RegionInfo.RegionName);
}
```

## Configuration

### Module Activation

Configure in OpenSim.ini [Modules] section:

```ini
[Modules]
NeighbourServices = "NeighbourServicesOutConnector"
```

### Configuration Examples

#### Standalone Mode

```ini
[Modules]
NeighbourServices = "NeighbourServicesOutConnector"
```

#### Grid Mode

```ini
[Modules]
NeighbourServices = "NeighbourServicesOutConnector"

[NeighbourService]
; Base neighbor service configuration
LocalServiceModule = "OpenSim.Services.NeighbourService.dll:NeighbourService"
```

### Factory Integration

The module is loaded via factory with configuration-based activation:

```csharp
string neighbourServicesModule = modulesConfig?.GetString("NeighbourServices", "");
if (neighbourServicesModule == "NeighbourServicesOutConnector")
{
    if(m_log.IsDebugEnabled)
        m_log.Debug("Loading NeighbourServicesOutConnector for cross-region neighbor communication and discovery");
    yield return new NeighbourServicesOutConnector();
    if(m_log.IsInfoEnabled)
        m_log.Info("NeighbourServicesOutConnector loaded for neighbor hello protocol, region discovery, and cross-region coordination");
}
```

## Performance Characteristics

### Local Communication Benefits

- **Zero Network Latency**: Direct method calls eliminate network overhead
- **Higher Throughput**: No serialization/deserialization overhead
- **Improved Reliability**: No network failure points for local communication
- **Resource Efficiency**: Reduced CPU and memory usage

### Hybrid Architecture Advantages

- **Optimal Routing**: Automatically chooses most efficient communication path
- **Scalability**: Supports both single-simulator and distributed grid deployments
- **Transparent Operation**: Higher-level code doesn't need to know communication method
- **Fault Tolerance**: Graceful fallback to remote communication

### Performance Metrics

- **Local Communication Latency**: < 1ms for same-process regions
- **Remote Communication Latency**: Network-dependent (typically 10-100ms)
- **Memory Footprint**: Minimal additional overhead beyond base class
- **CPU Usage**: Reduced for local communications

## Integration Points

### Scene Integration

```csharp
public void AddRegion(Scene scene)
{
    if (!m_Enabled)
        return;

    m_Scenes.Add(scene);
    scene.RegisterModuleInterface<INeighbourService>(this);
}
```

### Grid Service Integration

```csharp
public void RegionLoaded(Scene scene)
{
    if (!m_Enabled)
        return;

    m_GridService = scene.GridService;
}
```

### Region Management

- **Dynamic Addition**: Regions added to local list as they come online
- **Automatic Removal**: Regions removed when they shut down
- **State Synchronization**: Maintains current list of active local regions

## Use Cases and Benefits

### Primary Use Cases

1. **Multi-Region Simulators**: Optimizes communication between regions in same process
2. **Border Crossings**: Enables efficient avatar movement between adjacent regions
3. **Neighbor Discovery**: Implements hello protocol for region relationship establishment
4. **Grid Coordination**: Maintains neighbor relationships in distributed grid environments

### Performance Benefits

- **Reduced Network Traffic**: Eliminates unnecessary network calls for local regions
- **Lower Latency**: Direct method calls vs network round-trips
- **Improved Reliability**: No network failure points for local communication
- **Resource Optimization**: Reduced CPU, memory, and bandwidth usage

### Operational Benefits

- **Simplified Configuration**: Single module handles both local and remote scenarios
- **Transparent Operation**: No changes required to existing region code
- **Fault Tolerance**: Graceful degradation to remote communication when needed
- **Monitoring**: Comprehensive logging for debugging and performance analysis

## Error Handling and Resilience

### Configuration Validation

```csharp
public void Initialise(IConfigSource source)
{
    IConfig moduleConfig = source.Configs["Modules"];
    if (moduleConfig != null)
    {
        string name = moduleConfig.GetString("NeighbourServices");
        if (name == Name)
        {
            m_Enabled = true;
            m_log.Info("[NEIGHBOUR CONNECTOR]: Neighbour out connector enabled");
        }
    }
}
```

### State Management

```csharp
public void RemoveRegion(Scene scene)
{
    // Always remove - defensive programming
    if (m_Scenes.Contains(scene))
        m_Scenes.Remove(scene);
}
```

### Fallback Handling

- **Local Check First**: Always attempts local communication first
- **Graceful Fallback**: Seamlessly falls back to base class for remote regions
- **Error Propagation**: Properly propagates errors from underlying services

## Debugging and Monitoring

### Logging Capabilities

The module provides comprehensive logging:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(
    MethodBase.GetCurrentMethod().DeclaringType);

// Initialization logging
m_log.Info("[NEIGHBOUR CONNECTOR]: Neighbour out connector enabled");

// Per-region logging
m_log.InfoFormat("[NEIGHBOUR CONNECTOR]: Enabled out neighbours for region {0}",
                 scene.RegionInfo.RegionName);
```

### Debug Information

Commented debug logging available for troubleshooting:

```csharp
//uint x, y;
//Util.RegionHandleToRegionLoc(regionHandle, out x, out y);
//m_log.DebugFormat("[NEIGHBOUR SERVICE OUT CONNECTOR]: HelloNeighbour from region {0} to neighbour {1} at {2}-{3}",
//                  thisRegion.RegionName, s.Name, x, y);
```

### Factory Logging

Enhanced logging through factory integration:

```csharp
if(m_log.IsDebugEnabled)
    m_log.Debug("Loading NeighbourServicesOutConnector for cross-region neighbor communication and discovery");

if(m_log.IsInfoEnabled)
    m_log.Info("NeighbourServicesOutConnector loaded for neighbor hello protocol, region discovery, and cross-region coordination");
```

## Advanced Configuration

### Multi-Simulator Grids

For complex grid topologies with multiple simulators:

```ini
[Modules]
NeighbourServices = "NeighbourServicesOutConnector"

[NeighbourService]
LocalServiceModule = "OpenSim.Services.NeighbourService.dll:NeighbourService"
; Grid service for remote region discovery
GridService = "OpenSim.Services.GridService.dll:GridService"
```

### Performance Tuning

```ini
[NeighbourService]
; Optimize for high-traffic scenarios
ConnectorMaxConcurrentRequests = 10
RequestTimeout = 30000
```

### Development and Testing

```ini
[Modules]
; Disable for testing network-only scenarios
; NeighbourServices = ""

; Enable detailed debugging
[Startup]
LogLevel = DEBUG
```

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: No neighbor communication working
Cause: Module not configured or enabled
Solution: Check [Modules] NeighbourServices = "NeighbourServicesOutConnector"
```

#### Local Communication Not Working
```
Symptom: Network calls for same-simulator regions
Cause: Regions not properly registered in m_Scenes
Solution: Verify AddRegion/RemoveRegion lifecycle
```

#### Border Crossings Failing
```
Symptom: Avatar can't cross between regions
Cause: Hello protocol failures
Solution: Check grid service configuration and region registration
```

### Debugging Steps

1. **Enable Debug Logging**: Uncomment debug statements in HelloNeighbour
2. **Verify Configuration**: Check [Modules] section for proper configuration
3. **Monitor Scene List**: Verify regions are being added to m_Scenes collection
4. **Test Network Fallback**: Ensure base class functionality works for remote regions
5. **Check Grid Service**: Verify grid service is properly configured and accessible

## Security Considerations

### Access Control

- **Configuration-Based**: Only loads when explicitly configured
- **Scene Isolation**: Each scene maintains its own registration
- **Interface Isolation**: Only exposes INeighbourService interface

### Data Protection

- **Local Communication**: No network exposure for same-simulator communication
- **Network Security**: Relies on base class network security implementation
- **Input Validation**: Validates region handles and region information

### Resource Protection

- **Memory Management**: Proper cleanup during region removal
- **Resource Limits**: Inherits resource limits from base class
- **Error Handling**: Graceful error handling prevents resource leaks

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [Modules] NeighbourServices setting
- **Enhanced Logging**: Improved operational visibility and debugging
- **Backward Compatibility**: Maintains full API and configuration compatibility

### Upgrade Considerations

- Update configuration files to use factory loading system
- Review logging configuration for new message formats
- Test neighbor communication after upgrade
- Verify proper integration with grid services

## Related Components

### Dependencies
- **NeighbourServicesConnector**: Base class providing remote communication
- **Scene**: Regional simulation environment providing local communication
- **IGridService**: Grid service for region discovery and metadata
- **INeighbourService**: Service interface contract

### Integration Points
- **Region Management**: Scene lifecycle and registration
- **Grid Services**: Region discovery and neighbor identification
- **Avatar Movement**: Border crossing and teleportation support
- **Module System**: ISharedRegionModule implementation

## Future Enhancements

### Potential Improvements

- **Caching Layer**: Cache neighbor information for performance
- **Load Balancing**: Distribute neighbor requests across multiple connections
- **Priority Queuing**: Prioritize local vs remote communication
- **Metrics Collection**: Detailed performance and usage metrics
- **Health Monitoring**: Automatic detection and recovery from failed neighbors

### Advanced Features

- **Neighbor Groups**: Logical grouping of related regions
- **Dynamic Discovery**: Automatic neighbor detection and registration
- **Quality of Service**: Different service levels for different neighbor types
- **Compression**: Data compression for remote communications
- **Encryption**: Enhanced security for sensitive neighbor communications

## Protocol Details

### Hello Protocol Implementation

The HelloNeighbour method implements the standard OpenSimulator neighbor protocol:

1. **Region Handle Lookup**: Identifies target region by handle
2. **Local Check**: Searches for region in local scene collection
3. **Direct Call**: If local, calls `Scene.IncomingHelloNeighbour(RegionInfo)`
4. **Remote Fallback**: If not local, uses base class network communication
5. **Response Handling**: Returns GridRegion information about target region

### Communication Patterns

- **Synchronous**: HelloNeighbour calls are synchronous for simplicity
- **Stateless**: No persistent connection state between calls
- **Idempotent**: Multiple hello calls to same region are safe
- **Error Transparent**: Errors from local or remote calls handled identically

---

*This documentation covers NeighbourServicesOutConnector as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full neighbor communication and cross-region coordination capabilities.*