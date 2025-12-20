# LocalSimulationConnectorModule

## Overview

The `LocalSimulationConnectorModule` is a shared region module that provides local simulation services functionality for OpenSimulator standalone deployments or multi-region single-simulator configurations. This module enables efficient in-process communication between multiple regions running within the same simulator instance, eliminating network overhead for local region-to-region operations.

## Purpose

This connector enables high-performance local simulation coordination by:
- Facilitating agent transfers between local regions (teleports, crossings)
- Enabling cross-region object operations without network overhead
- Supporting local agent state synchronization through direct method calls
- Providing local access control and permission queries
- Delivering seamless cross-region experiences with minimal latency
- Optimizing memory usage through shared scene references

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`, `ISimulationService`
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation`
- **Assembly**: `OpenSim.Region.CoreModules.dll`
- **Loading**: Factory-based loading via `CoreModuleFactory.CreateSharedModules()`

### Key Components

#### Scene Management
- **Thread-Safe Scene Dictionary**: Uses `RwLockedDictionary<UUID, Scene>` for concurrent access
- **Dynamic Region Registration**: Regions are added/removed dynamically during runtime
- **Scene Lookup by UUID**: Fast region lookup using region ID as key
- **Region Handle Queries**: Supports lookup by region handle for compatibility

#### Core Functionality
- **Local-Only Operation**: Operates purely within the same process, no network I/O
- **Agent Management**: Handles agent creation, updates, position updates, and cleanup
- **Object Operations**: Supports cross-region object transfers with cloning
- **Query Operations**: Provides fast local access queries and region validation
- **Service Interface**: Implements full `ISimulationService` interface for simulation operations

#### Configuration Management
- Reads configuration from the `[Modules]` section
- Enables when `SimulationServices = "LocalSimulationConnectorModule"`
- Provides initialization logging for debugging
- Falls back gracefully when not configured

## Configuration

### Module Configuration
Add to your `OpenSim.ini` or appropriate configuration file:

```ini
[Modules]
SimulationServices = LocalSimulationConnectorModule
```

### Standalone Mode Configuration
This module is typically used in standalone deployments where all regions run in a single process:

```ini
[Modules]
SimulationServices = LocalSimulationConnectorModule

[Architecture]
Include-Architecture = "config-include/Standalone.ini"
```

### Multi-Region Standalone
For running multiple regions in the same simulator process:

```ini
[Modules]
SimulationServices = LocalSimulationConnectorModule

[Startup]
; Multiple region support
region_info_source = "filesystem"

[RegionConfig]
; Regions are defined in Regions/Regions.ini
```

## Implementation Details

### Scene Registration

When a region starts, it is registered in the module's scene dictionary:

```csharp
public void Init(Scene scene)
{
    if (!m_scenes.ContainsKey(scene.RegionInfo.RegionID))
        m_scenes[scene.RegionInfo.RegionID] = scene;
    else
        m_log.WarnFormat(
            "Tried to add region {0} but it is already present",
            scene.RegionInfo.RegionName);
}
```

#### Key Features:
- Thread-safe concurrent access using `RwLockedDictionary`
- Duplicate detection with warning logging
- Direct scene reference storage for zero-copy access

### Scene Removal

When a region shuts down, it is unregistered from the module:

```csharp
public void RemoveScene(Scene scene)
{
    if (m_scenes.ContainsKey(scene.RegionInfo.RegionID))
        m_scenes.Remove(scene.RegionInfo.RegionID);
    else
        m_log.WarnFormat(
            "Tried to remove region {0} but it was not present",
            scene.RegionInfo.RegionName);
}
```

#### Key Features:
- Safe removal with existence checking
- Warning on attempted removal of non-existent regions
- Clean resource cleanup

### Agent Operations

#### CreateAgent
Creates a new agent in a destination region for teleports or crossings:

```csharp
public bool CreateAgent(GridRegion source, GridRegion destination,
    AgentCircuitData aCircuit, uint teleportFlags,
    EntityTransferContext ctx, out string reason)
```

**Features:**
- Debug logging for agent creation tracking
- Null destination validation
- Direct scene method invocation for `NewUserConnection()`
- Returns detailed failure reasons
- Supports both root and child agent creation

**Performance Characteristics:**
- Zero network latency (in-process call)
- Direct memory access to scene objects
- No serialization/deserialization overhead

#### UpdateAgent (AgentData)
Updates agent state data for child agents:

```csharp
public bool UpdateAgent(GridRegion destination, AgentData cAgentData,
    EntityTransferContext ctx)
```

**Features:**
- Null destination check
- Direct scene update call
- Returns success/failure status

#### UpdateAgent (AgentPosition)
Broadcasts agent position updates to all local regions:

```csharp
public bool UpdateAgent(GridRegion destination, AgentPosition agentPosition)
```

**Features:**
- Broadcast to all local scenes (destination parameter ignored for local updates)
- Each scene checks for presence before updating
- Optimized for position synchronization across borders

#### QueryAccess
Performs access control checks before agent arrival:

```csharp
public bool QueryAccess(GridRegion destination, UUID agentID,
    string agentHomeURI, bool viaTeleport, Vector3 position,
    List<UUID> features, EntityTransferContext ctx, out string reason)
```

**Features:**
- Variable-sized region compatibility checking
- Older client detection and rejection for var regions
- Direct access to region size information
- Detailed failure reasons

**Special Handling:**
- Rejects connections from older simulators (outbound version < 0.3) to var regions
- Prevents viewer crashes from region size mismatches

#### ReleaseAgent
Notifies entity transfer module that agent has reached destination:

```csharp
public bool ReleaseAgent(UUID originId, UUID agentId, string uri)
```

**Features:**
- Direct callback to EntityTransferModule
- Confirms successful agent arrival
- Enables cleanup of transfer state

#### CloseAgent
Closes agent connection in a region:

```csharp
public bool CloseAgent(GridRegion destination, UUID id, string auth_token)
```

**Features:**
- Authentication token validation
- Clean agent removal
- Region presence cleanup

### Object Operations

#### CreateObject
Transfers objects between local regions:

```csharp
public bool CreateObject(GridRegion destination, Vector3 newPosition,
    ISceneObject sog, bool isLocalCall)
```

**Features:**
- **Local Call Mode**: Creates clone of object for same-simulator transfers
  - Uses `CloneForNewScene()` for proper object duplication
  - Transfers state snapshot with `GetStateSnapshot()` and `SetState()`
  - Preserves scripts, inventory, and object properties
- **Remote Call Mode**: Uses object as-is (when called from remote connector)
- Position adjustment to target region
- Direct scene object creation

**Cloning Strategy:**
- Local calls always clone to avoid reference sharing issues
- Clones preserve all object attributes and child prims
- State transfer ensures script continuity

### Region Query Operations

#### GetScene
Retrieves a scene by region ID:

```csharp
public IScene GetScene(UUID regionId)
```

**Features:**
- Fast dictionary lookup
- Fallback to first available scene with error logging
- Pre-existing behavior for compatibility

**Note:** The fallback behavior may hide errors but ensures continued operation.

#### IsLocalRegion (UUID)
Checks if a region UUID is managed locally:

```csharp
public bool IsLocalRegion(UUID id)
{
    return m_scenes.ContainsKey(id);
}
```

#### IsLocalRegion (Handle)
Checks if a region handle is managed locally:

```csharp
public bool IsLocalRegion(ulong regionhandle)
{
    foreach (Scene s in m_scenes.Values)
        if (s.RegionInfo.RegionHandle == regionhandle)
            return true;
    return false;
}
```

**Performance Note:** Linear search through all scenes - acceptable for small scene counts.

## Module Lifecycle

### 1. Initialization Phase
```csharp
public void Initialise(IConfigSource configSource)
```

**Actions:**
- Reads `[Modules]` configuration
- Checks if `SimulationServices = "LocalSimulationConnectorModule"`
- Calls `InitialiseService()` (currently empty, available for future use)
- Sets `m_ModuleEnabled` flag
- Logs initialization status

### 2. Post-Initialization Phase
```csharp
public void PostInitialise()
```

Currently empty - available for cross-module initialization.

### 3. Region Addition
```csharp
public void AddRegion(Scene scene)
```

**Actions:**
- Checks if module is enabled
- Calls `Init(scene)` to register scene
- Registers module as `ISimulationService` interface provider

### 4. Region Loaded
```csharp
public void RegionLoaded(Scene scene)
```

Currently empty - available for post-region-load operations.

### 5. Region Removal
```csharp
public void RemoveRegion(Scene scene)
```

**Actions:**
- Checks if module is enabled
- Calls `RemoveScene(scene)` to unregister scene
- Unregisters `ISimulationService` interface

### 6. Module Closure
```csharp
public void Close()
```

Currently empty - cleanup handled by scene removal.

## Thread Safety

The module implements thread-safe operations using `RwLockedDictionary<UUID, Scene>`:

- **Read Operations**: Multiple concurrent readers allowed
- **Write Operations**: Exclusive access during add/remove
- **Scene Access**: Thread-safe dictionary operations
- **Scene Methods**: Individual scene methods handle their own thread safety

## Performance Characteristics

### Advantages
- **Zero Network Latency**: All calls are in-process method invocations
- **No Serialization**: Direct object references, no marshalling overhead
- **Memory Efficient**: Shared scene references across operations
- **Fast Lookups**: O(1) dictionary access by UUID
- **Reduced GC Pressure**: No allocation for network buffers or serialized data

### Limitations
- **Single Process Only**: Cannot communicate across process boundaries
- **No Grid Scalability**: All regions must run in the same simulator
- **Handle Lookup Cost**: O(n) linear search for region handle queries
- **Memory Constraints**: All regions share the same memory space

## Comparison with RemoteSimulationConnectorModule

| Feature | LocalSimulationConnectorModule | RemoteSimulationConnectorModule |
|---------|-------------------------------|--------------------------------|
| **Scope** | Single simulator process | Grid-wide across network |
| **Performance** | In-process method calls (fast) | Network HTTP/HTTPS calls (slower) |
| **Latency** | Near-zero (microseconds) | Network-dependent (milliseconds) |
| **Scalability** | Limited to single process | Unlimited grid scaling |
| **Network I/O** | None | HTTP requests/responses |
| **Serialization** | None | Full object serialization |
| **Deployment** | Standalone, multi-region | Grid mode, distributed |
| **Memory** | Shared memory space | Separate processes |
| **Fault Isolation** | Single point of failure | Region failures isolated |

## Usage Scenarios

### Ideal Use Cases
1. **Standalone Deployments**: Single user or small group testing
2. **Development Environments**: Rapid testing with multiple local regions
3. **Small Private Grids**: Limited regions on powerful single server
4. **Multi-Region Events**: Large events spanning adjacent regions on same host
5. **Performance Testing**: Benchmark maximum throughput without network overhead

### Not Recommended For
1. **Large Public Grids**: Cannot scale across multiple servers
2. **High Availability**: Single process is single point of failure
3. **Geographic Distribution**: All regions must be co-located
4. **Resource Isolation**: Regions share memory and CPU resources

## Migration from Mono.Addins

This module was previously loaded using the Mono.Addins plugin system. It has been migrated to factory-based loading for .NET 8 compatibility:

**Old Loading Method (Mono.Addins):**
```csharp
[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule",
    Id = "LocalSimulationConnectorModule")]
public class LocalSimulationConnectorModule : ISharedRegionModule, ISimulationService
```

**New Loading Method (Factory):**
```csharp
// In CoreModuleFactory.cs
string simulationServicesModule = modulesConfig?.GetString("SimulationServices", "");
if (simulationServicesModule == "LocalSimulationConnectorModule")
{
    if(m_log.IsDebugEnabled)
        m_log.Debug("Loading LocalSimulationConnectorModule for local region-to-region communication");
    yield return new LocalSimulationConnectorModule();
    if(m_log.IsInfoEnabled)
        m_log.Info("LocalSimulationConnectorModule loaded for local simulation services");
}
```

**Benefits of Migration:**
- .NET 8 compatibility without Mono.Addins dependency
- Explicit module loading with configuration-based control
- Better logging for module initialization
- Clearer dependency management

## Debugging and Logging

### Log Messages

**Initialization:**
```
[LOCAL SIMULATION CONNECTOR MODULE]: Local simulation enabled.
```

**Scene Registration:**
```
[LOCAL SIMULATION CONNECTOR MODULE]: Tried to add region {RegionName} but it is already present
```

**Scene Removal:**
```
[LOCAL SIMULATION CONNECTOR MODULE]: Tried to remove region {RegionName} but it was not present
```

**Agent Creation:**
```
[LOCAL SIMULATION CONNECTOR MODULE]: CreateAgent was given source: {Source} and destination: {Destination} aCircuit: {AgentID}
[LOCAL SIMULATION CONNECTOR MODULE]: CreateAgent was given source: null and destination: {Destination} aCircuit: {AgentID}
[LOCAL SIMULATION CONNECTOR MODULE]: Found region {RegionName} to send SendCreateChildAgent
```

**Variable Region Protection:**
```
[LOCAL SIMULATION CONNECTOR MODULE]: Request to access this variable-sized region from older simulator was denied
```

**Scene Lookup Fallback:**
```
[LOCAL SIMULATION CONNECTOR MODULE]: Region with id {RegionID} not found. Returning {RegionName} {RegionID} instead
```

### Debug Configuration

Enable debug logging in `OpenSim.exe.config`:

```xml
<logger name="OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation">
    <level value="DEBUG" />
</logger>
```

## Dependencies

### Required Interfaces
- `ISharedRegionModule` - Region module lifecycle
- `ISimulationService` - Simulation service operations

### Required Types
- `OpenSim.Framework.Scene` - Region scene management
- `OpenSim.Services.Interfaces.GridRegion` - Region metadata
- `OpenSim.Framework.AgentCircuitData` - Agent connection data
- `OpenSim.Services.Interfaces.AgentData` - Agent state data
- `OpenSim.Services.Interfaces.AgentPosition` - Agent position updates
- `OpenSim.Services.Interfaces.EntityTransferContext` - Transfer context
- `ThreadedClasses.RwLockedDictionary` - Thread-safe dictionary

### External Dependencies
- `Nini.Config` - Configuration management
- `log4net` - Logging framework
- `OpenMetaverse` - UUID and vector types

## Future Enhancements

### Potential Improvements
1. **Metrics Collection**: Track transfer counts, latencies, and failures
2. **Scene Cache**: Optimize handle lookups with reverse lookup table
3. **Batch Operations**: Support bulk agent updates for performance
4. **Event Notifications**: Emit events for agent/object transfers
5. **Configuration Validation**: Enhanced config checking with warnings
6. **Performance Profiling**: Built-in performance counters

### Possible Optimizations
1. **Handle Index**: Maintain O(1) lookup for region handles
2. **Scene Pooling**: Reuse scene references for frequently accessed regions
3. **Async Operations**: Convert synchronous calls to async where beneficial
4. **Parallel Updates**: Batch position updates with parallel processing

## Related Modules

- **RemoteSimulationConnectorModule**: Grid-wide remote simulation connector
- **SimulationServiceInConnectorModule**: Incoming simulation service handler
- **EntityTransferModule**: High-level agent transfer coordination
- **NeighbourServicesOutConnector**: Neighbour region communication

## Code Location

**Source File:**
```
src/OpenSim.Region.CoreModules/ServiceConnectorsOut/Simulation/LocalSimulationConnector.cs
```

**Factory Registration:**
```
src/OpenSim.Region.CoreModules/ModuleFactory.cs
```
Line ~1184-1203

**Configuration:**
```
bin/OpenSim.ini
bin/config-include/StandaloneCommon.ini
```

## License

This module is part of OpenSimulator and is licensed under the BSD 3-Clause License. See the file header for full license text.

## Contributors

This module is maintained by the OpenSimulator development community. Contributions are welcome through the OpenSimulator project channels.

## See Also

- [RemoteSimulationConnectorModule](RemoteSimulationConnectorModule.md) - Remote simulation connector
- [EntityTransferModule Documentation](http://opensimulator.org/wiki/EntityTransfer) - Agent transfer system
- [OpenSimulator Architecture](http://opensimulator.org/wiki/Architecture) - Overall system architecture
- [Standalone Mode Configuration](http://opensimulator.org/wiki/Configuration) - Standalone setup guide