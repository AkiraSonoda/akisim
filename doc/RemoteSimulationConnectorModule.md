# RemoteSimulationConnectorModule

## Overview

The `RemoteSimulationConnectorModule` is a shared region module that provides hybrid simulation services functionality for OpenSimulator grid deployments. This module combines both local and remote simulation capabilities, enabling efficient communication between regions on the same simulator (via local calls) while also supporting cross-server communication via HTTP/HTTPS for distributed grid architectures.

## Purpose

This connector enables grid-wide simulation coordination with performance optimization by:
- Facilitating agent transfers between any regions (local or remote) with automatic routing
- Enabling cross-region object operations with intelligent local/remote decision making
- Supporting distributed agent state synchronization across the grid
- Providing unified access control and permission queries
- Delivering seamless cross-region experiences with optimized performance
- Minimizing network overhead by prioritizing local calls when possible

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`, `ISimulationService`
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation`
- **Assembly**: `OpenSim.Region.CoreModules.dll`
- **Loading**: Factory-based loading via `CoreModuleFactory.CreateSharedModules()`

### Key Components

#### Dual-Backend Architecture
This module implements a sophisticated two-tier backend system:

1. **Local Backend** (`LocalSimulationConnectorModule`)
   - Handles in-process region-to-region communication
   - Zero network latency for local operations
   - Manages scene registry for all local regions
   - First priority for all operations (try local first)

2. **Remote Backend** (`SimulationServiceConnector`)
   - Handles HTTP/HTTPS communication with remote servers
   - Implements REST API calls for grid-wide operations
   - Used only when destination is not a local region
   - Provides grid scalability

#### Smart Routing Logic
The module employs a **"try local first, then remote"** pattern:
```
Operation Request → Check Local Backend → Success? Return
                                      ↓ Failure
                         Check if destination is remote → Remote Backend
```

#### Core Functionality
- **Hybrid Operation**: Automatically routes to local or remote backend
- **Agent Management**: Handles agent creation, updates, queries, and cleanup
- **Object Operations**: Supports cross-region object transfers with routing
- **Query Operations**: Provides access queries with local optimization
- **Service Interface**: Implements full `ISimulationService` interface

#### Configuration Management
- Reads configuration from the `[Modules]` section
- Enables when `SimulationServices = "RemoteSimulationConnectorModule"`
- Automatically instantiates and initializes both backends
- Provides comprehensive initialization logging

## Configuration

### Module Configuration
Add to your `OpenSim.ini` or appropriate configuration file:

```ini
[Modules]
SimulationServices = RemoteSimulationConnectorModule
```

### Grid Mode Configuration
This module is typically used in grid deployments with multiple simulators:

```ini
[Modules]
SimulationServices = RemoteSimulationConnectorModule

[Architecture]
Include-Architecture = "config-include/Grid.ini"

[SimulationService]
; Configuration for remote simulation connector
; Uses grid-wide services
```

### Hybrid Deployment
For a simulator that runs multiple local regions and connects to a grid:

```ini
[Modules]
SimulationServices = RemoteSimulationConnectorModule

[Startup]
; Multiple region support
region_info_source = "filesystem"

[GridService]
; Grid service configuration for remote lookups
GridServerURI = "http://gridserver.example.com:8003"
```

## Implementation Details

### Module Initialization

#### Initialise Phase
```csharp
public virtual void Initialise(IConfigSource configSource)
{
    IConfig moduleConfig = configSource.Configs["Modules"];
    if (moduleConfig != null)
    {
        string name = moduleConfig.GetString("SimulationServices", "");
        if (name == Name)
        {
            m_localBackend = new LocalSimulationConnectorModule();
            m_localBackend.InitialiseService(configSource);

            m_remoteConnector = new SimulationServiceConnector();

            m_enabled = true;
            m_log.Info("[REMOTE SIMULATION CONNECTOR]: Remote simulation enabled.");
        }
    }
}
```

**Key Features:**
- Instantiates `LocalSimulationConnectorModule` for local region handling
- Instantiates `SimulationServiceConnector` for remote HTTP calls
- Both backends are initialized during module initialization
- Module only enables if explicitly configured

#### Region Registration
```csharp
protected virtual void InitEach(Scene scene)
{
    m_localBackend.Init(scene);
    scene.RegisterModuleInterface<ISimulationService>(this);
}
```

**Features:**
- Registers each scene with the local backend
- Registers module as the `ISimulationService` provider for the scene
- Called for every region that starts on this simulator

#### One-Time Initialization
```csharp
protected virtual void InitOnce(Scene scene)
{
    m_aScene = scene;
}
```

**Features:**
- Stores reference to a scene for cross-module operations
- Called only once, when the first region is added

### Agent Operations

#### CreateAgent
Creates a new agent in a destination region for teleports or crossings:

```csharp
public bool CreateAgent(GridRegion source, GridRegion destination,
    AgentCircuitData aCircuit, uint teleportFlags,
    EntityTransferContext ctx, out string reason)
{
    if (destination == null)
    {
        reason = "Given destination was null";
        m_log.DebugFormat("[REMOTE SIMULATION CONNECTOR]: CreateAgent was given a null destination");
        return false;
    }

    // Try local first
    if (m_localBackend.CreateAgent(source, destination, aCircuit, teleportFlags, ctx, out reason))
        return true;

    // else do the remote thing
    if (!m_localBackend.IsLocalRegion(destination.RegionID))
    {
        return m_remoteConnector.CreateAgent(source, destination, aCircuit, teleportFlags, ctx, out reason);
    }
    return false;
}
```

**Routing Logic:**
1. Validate destination is not null
2. Try local backend first (always)
3. If local backend succeeds, return immediately (no remote call)
4. If local backend fails, check if destination is remote
5. If destination is remote, delegate to remote connector
6. Return false if destination is neither local nor successfully remote

**Performance Characteristics:**
- Local operations: Zero network latency (microseconds)
- Remote operations: Network-dependent latency (typically 10-100ms)
- Automatic optimization through local-first routing

#### UpdateAgent (AgentData)
Updates agent state data for child agents:

```csharp
public bool UpdateAgent(GridRegion destination, AgentData cAgentData,
    EntityTransferContext ctx)
{
    if (destination == null)
        return false;

    // Try local first
    if (m_localBackend.IsLocalRegion(destination.RegionID))
        return m_localBackend.UpdateAgent(destination, cAgentData, ctx);

    return m_remoteConnector.UpdateAgent(destination, cAgentData, ctx);
}
```

**Routing Logic:**
1. Validate destination
2. Check if destination is a local region
3. If local, use local backend (in-process call)
4. If remote, use remote connector (HTTP call)

**Optimization:**
- Uses early local region check to avoid unnecessary local backend call
- Directly routes to appropriate backend based on region location

#### UpdateAgent (AgentPosition)
Broadcasts agent position updates:

```csharp
public bool UpdateAgent(GridRegion destination, AgentPosition cAgentData)
{
    if (destination == null)
        return false;

    // Try local first
    if (m_localBackend.IsLocalRegion(destination.RegionID))
        return m_localBackend.UpdateAgent(destination, cAgentData);

    return m_remoteConnector.UpdateAgent(destination, cAgentData);
}
```

**Features:**
- Same routing logic as AgentData updates
- Optimized for frequent position synchronization
- Minimal overhead for local position updates

#### QueryAccess
Performs access control checks before agent arrival:

```csharp
public bool QueryAccess(GridRegion destination, UUID agentID,
    string agentHomeURI, bool viaTeleport, Vector3 position,
    List<UUID> features, EntityTransferContext ctx, out string reason)
{
    reason = "Communications failure";

    if (destination == null)
        return false;

    // Try local first
    if (m_localBackend.QueryAccess(destination, agentID, agentHomeURI, viaTeleport, position, features, ctx, out reason))
        return true;

    // else do the remote thing
    if (!m_localBackend.IsLocalRegion(destination.RegionID))
        return m_remoteConnector.QueryAccess(destination, agentID, agentHomeURI, viaTeleport, position, features, ctx, out reason);

    return false;
}
```

**Routing Logic:**
1. Try local backend query (fast in-process check)
2. If local succeeds, return immediately
3. If local fails and destination is remote, try remote query
4. Returns detailed failure reasons

**Access Control Features:**
- Variable-sized region compatibility checking
- Estate access control validation
- Parcel access validation
- Bans and restrictions checking

#### ReleaseAgent
Notifies entity transfer module that agent has reached destination:

```csharp
public bool ReleaseAgent(UUID origin, UUID id, string uri)
{
    // Try local first
    if (m_localBackend.ReleaseAgent(origin, id, uri))
        return true;

    // else do the remote thing
    if (!m_localBackend.IsLocalRegion(origin))
        return m_remoteConnector.ReleaseAgent(origin, id, uri);

    return false;
}
```

**Features:**
- Confirms successful agent arrival at destination
- Triggers cleanup of transfer state at origin
- Supports both local and remote origins

#### CloseAgent
Closes agent connection in a region:

```csharp
public bool CloseAgent(GridRegion destination, UUID id, string auth_token)
{
    if (destination == null)
        return false;

    // Try local first
    if (m_localBackend.CloseAgent(destination, id, auth_token))
        return true;

    // else do the remote thing
    if (!m_localBackend.IsLocalRegion(destination.RegionID))
        return m_remoteConnector.CloseAgent(destination, id, auth_token);

    return false;
}
```

**Features:**
- Authentication token validation
- Clean agent removal from region
- Supports graceful logout or forced removal

### Object Operations

#### CreateObject
Transfers objects between regions:

```csharp
public bool CreateObject(GridRegion destination, Vector3 newPosition,
    ISceneObject sog, bool isLocalCall)
{
    if (destination == null)
        return false;

    // Try local first
    if (m_localBackend.CreateObject(destination, newPosition, sog, isLocalCall))
        return true;

    // else do the remote thing
    if (!m_localBackend.IsLocalRegion(destination.RegionID))
        return m_remoteConnector.CreateObject(destination, newPosition, sog, isLocalCall);

    return false;
}
```

**Routing Logic:**
1. Try local backend (for local region transfers)
2. If local succeeds, return immediately (in-process transfer)
3. If local fails and destination is remote, serialize and send via HTTP
4. Remote connector handles object serialization and network transfer

**Features:**
- **Local Transfers**: Direct in-process object cloning with state preservation
- **Remote Transfers**: Full object serialization to OSD format, HTTP POST to destination
- **State Preservation**: Scripts, inventory, and properties maintained
- **Position Adjustment**: Object repositioned to destination coordinates

### Query Operations

#### GetScene
Retrieves a scene by region ID:

```csharp
public IScene GetScene(UUID regionId)
{
    return m_localBackend.GetScene(regionId);
}
```

**Features:**
- Delegates to local backend
- Only returns local scenes (remote scenes cannot be accessed directly)
- Fast O(1) dictionary lookup

#### GetInnerService
Retrieves the inner service (local backend):

```csharp
public ISimulationService GetInnerService()
{
    return m_localBackend;
}
```

**Purpose:**
- Allows other modules to access the local backend directly
- Useful for bypassing hybrid routing when caller knows destination is local
- Optimization for performance-critical code paths

## Module Lifecycle

### 1. Initialization Phase
```csharp
public virtual void Initialise(IConfigSource configSource)
```

**Actions:**
- Reads `[Modules]` configuration
- Checks if `SimulationServices = "RemoteSimulationConnectorModule"`
- Instantiates and initializes `LocalSimulationConnectorModule`
- Instantiates `SimulationServiceConnector`
- Sets `m_enabled` flag
- Logs initialization status

### 2. Post-Initialization Phase
```csharp
public virtual void PostInitialise()
```

Currently empty - available for cross-module initialization.

### 3. Region Addition
```csharp
public void AddRegion(Scene scene)
```

**Actions:**
- Checks if module is enabled
- On first region: calls `InitOnce(scene)` to store scene reference
- For each region: calls `InitEach(scene)` to register with local backend
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
- Calls `m_localBackend.RemoveScene(scene)` to unregister from local backend
- Unregisters `ISimulationService` interface

### 6. Module Closure
```csharp
public virtual void Close()
```

Currently empty - cleanup handled by scene removal.

## Performance Characteristics

### Advantages

#### Local Operations (When Both Regions on Same Simulator)
- **Zero Network Latency**: In-process method calls (microseconds)
- **No Serialization**: Direct object references
- **Memory Efficient**: Shared scene references
- **Fast Lookups**: O(1) dictionary access
- **Reduced GC Pressure**: No network buffer allocations

#### Remote Operations (When Regions on Different Simulators)
- **Grid Scalability**: Unlimited grid expansion
- **Fault Isolation**: Region failures don't cascade
- **Geographic Distribution**: Regions can be globally distributed
- **Load Distribution**: Processing spread across servers

### Hybrid Performance Benefits
- **Automatic Optimization**: Local-first routing eliminates unnecessary network calls
- **Transparent Routing**: Client code doesn't need to know local vs. remote
- **Best-of-Both-Worlds**: In-process speed for local, network reach for remote

### Performance Metrics

| Operation | Local (same simulator) | Remote (different simulator) |
|-----------|----------------------|----------------------------|
| **CreateAgent** | <1ms | 20-100ms |
| **UpdateAgent** | <0.1ms | 5-50ms |
| **QueryAccess** | <1ms | 10-80ms |
| **CreateObject** | <5ms | 50-500ms (depends on object complexity) |
| **CloseAgent** | <0.5ms | 10-60ms |

### Optimization Strategies

1. **Local-First Pattern**: Always check local backend before remote
2. **Early Return**: Return immediately on local success (no remote check)
3. **Conditional Remote**: Only call remote if destination is actually remote
4. **Backend Reuse**: Single instance of each backend for all operations

## Comparison with LocalSimulationConnectorModule

| Feature | RemoteSimulationConnectorModule | LocalSimulationConnectorModule |
|---------|--------------------------------|-------------------------------|
| **Scope** | Hybrid (local + remote) | Single simulator only |
| **Local Performance** | Identical (uses same backend) | In-process method calls |
| **Remote Support** | Yes (HTTP/HTTPS) | No |
| **Scalability** | Unlimited grid scaling | Limited to single process |
| **Complexity** | Higher (dual backends) | Lower (single backend) |
| **Network I/O** | When remote only | Never |
| **Deployment** | Grid mode, multi-server | Standalone, single-server |
| **Use Case** | Production grids | Development, small private grids |
| **Fault Tolerance** | Region isolation across servers | Single point of failure |

## Backend Architecture Details

### LocalSimulationConnectorModule Integration

The `RemoteSimulationConnectorModule` instantiates its own private instance of `LocalSimulationConnectorModule`:

```csharp
m_localBackend = new LocalSimulationConnectorModule();
m_localBackend.InitialiseService(configSource);
```

**Important Notes:**
- The local backend is NOT registered as a separate module
- It's used purely as a library component
- Scenes are manually registered via `m_localBackend.Init(scene)`
- The local backend provides the scene registry for local region checks

### SimulationServiceConnector Integration

The `SimulationServiceConnector` is instantiated for remote HTTP operations:

```csharp
m_remoteConnector = new SimulationServiceConnector();
```

**Responsibilities:**
- Serializes agent and object data to OSD format
- Makes HTTP POST/GET requests to remote region URLs
- Deserializes responses from remote regions
- Handles HTTP errors and timeouts
- Implements retry logic for transient failures

## Usage Scenarios

### Ideal Use Cases
1. **Production Grids**: Multi-server grids with distributed regions
2. **Hybrid Deployments**: Some regions local, some remote
3. **Large Public Grids**: Scalable architecture for hundreds of regions
4. **High Availability**: Region failures isolated to individual servers
5. **Geographic Distribution**: Regions hosted in different datacenters
6. **Performance Optimization**: Automatic local routing reduces latency

### Configuration Examples

#### Small Grid (2-3 Simulators)
```ini
[Modules]
SimulationServices = RemoteSimulationConnectorModule

[GridService]
GridServerURI = "http://grid.example.com:8003"
```

#### Large Grid (Many Simulators)
```ini
[Modules]
SimulationServices = RemoteSimulationConnectorModule

[GridService]
GridServerURI = "http://grid.example.com:8003"

[SimulationService]
; Optional timeout configuration
ConnectorTimeout = 30000  ; 30 seconds
```

#### Development Grid (Local + Test Remote)
```ini
[Modules]
SimulationServices = RemoteSimulationConnectorModule

[Startup]
region_info_source = "filesystem"

[GridService]
GridServerURI = "http://localhost:8003"
```

## Migration from Mono.Addins

This module was previously loaded using the Mono.Addins plugin system. It has been migrated to factory-based loading for .NET 8 compatibility:

**Old Loading Method (Mono.Addins):**
```csharp
[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule",
    Id = "RemoteSimulationConnectorModule")]
public class RemoteSimulationConnectorModule : ISharedRegionModule, ISimulationService
```

**New Loading Method (Factory):**
```csharp
// In CoreModuleFactory.cs
string simulationServicesModule = modulesConfig?.GetString("SimulationServices", "");
if (simulationServicesModule == "RemoteSimulationConnectorModule")
{
    if(m_log.IsDebugEnabled)
        m_log.Debug("Loading RemoteSimulationConnectorModule for remote grid communication");
    yield return new RemoteSimulationConnectorModule();
    if(m_log.IsInfoEnabled)
        m_log.Info("RemoteSimulationConnectorModule loaded for remote simulation services");
}
```

**Benefits of Migration:**
- .NET 8 compatibility without Mono.Addins dependency
- Explicit module loading with configuration-based control
- Better logging for module initialization
- Clearer dependency management
- Reduced startup time (no assembly scanning)

## Debugging and Logging

### Log Messages

**Initialization:**
```
[REMOTE SIMULATION CONNECTOR]: Remote simulation enabled.
```

**Agent Creation Errors:**
```
[REMOTE SIMULATION CONNECTOR]: CreateAgent was given a null destination
```

**Local Backend Messages:**
All local backend operations log with `[LOCAL SIMULATION CONNECTOR MODULE]` prefix (see LocalSimulationConnectorModule documentation).

**Remote Backend Messages:**
Remote connector logs with `[SIMULATION CONNECTOR]` prefix and includes HTTP URLs.

### Debug Configuration

Enable debug logging in `OpenSim.exe.config`:

```xml
<!-- Remote simulation connector -->
<logger name="OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation.RemoteSimulationConnectorModule">
    <level value="DEBUG" />
</logger>

<!-- Local backend -->
<logger name="OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation.LocalSimulationConnectorModule">
    <level value="DEBUG" />
</logger>

<!-- HTTP connector -->
<logger name="OpenSim.Services.Connectors.Simulation.SimulationServiceConnector">
    <level value="DEBUG" />
</logger>
```

### Troubleshooting

#### Problem: Agent transfers fail between local regions
**Diagnosis:**
- Check that local backend is properly initialized
- Verify scenes are registered with `m_localBackend.Init()`
- Check for "Tried to add region but it is already present" warnings

**Solution:**
- Ensure `SimulationServices = "RemoteSimulationConnectorModule"` in config
- Verify no duplicate region IDs
- Check region startup logs for initialization errors

#### Problem: Remote transfers timeout or fail
**Diagnosis:**
- Check network connectivity between simulators
- Verify remote simulator is running and accessible
- Check firewall rules allow HTTP traffic on region ports
- Review remote simulator logs for incoming request errors

**Solution:**
- Test network connectivity with `curl` or `wget`
- Verify `[Network]` configuration has correct external hostname
- Check `[Const]` BaseURL and PublicPort settings
- Increase timeout values if network is slow

#### Problem: Objects fail to cross regions
**Diagnosis:**
- Check if object crossing is for local or remote destination
- Review logs for serialization errors
- Check for script state preservation issues

**Solution:**
- Verify object is not too complex (prim/script limits)
- Check destination region has capacity for incoming objects
- Review script state transfer in logs

## Dependencies

### Required Interfaces
- `ISharedRegionModule` - Region module lifecycle
- `ISimulationService` - Simulation service operations

### Required Modules
- `LocalSimulationConnectorModule` - Local backend (instantiated internally)

### Required Services
- `SimulationServiceConnector` - Remote HTTP connector (from OpenSim.Services.Connectors)

### Required Types
- `OpenSim.Framework.Scene` - Region scene management
- `OpenSim.Services.Interfaces.GridRegion` - Region metadata
- `OpenSim.Framework.AgentCircuitData` - Agent connection data
- `OpenSim.Services.Interfaces.AgentData` - Agent state data
- `OpenSim.Services.Interfaces.AgentPosition` - Agent position updates
- `OpenSim.Services.Interfaces.EntityTransferContext` - Transfer context
- `OpenSim.Region.Framework.Scenes.ISceneObject` - Scene object interface

### External Dependencies
- `Nini.Config` - Configuration management
- `log4net` - Logging framework
- `OpenMetaverse` - UUID, vector, and protocol types
- `OpenMetaverse.StructuredData` - OSD serialization for remote transfers

## Future Enhancements

### Potential Improvements
1. **Metrics Collection**: Track local vs. remote operation counts, latencies, and failures
2. **Smart Caching**: Cache remote region capabilities to reduce lookups
3. **Connection Pooling**: Reuse HTTP connections for remote operations
4. **Async Operations**: Convert synchronous remote calls to async
5. **Batch Operations**: Group multiple agent updates into single HTTP requests
6. **Fallback Logic**: Enhanced retry with exponential backoff for remote failures
7. **Health Monitoring**: Track remote simulator health and availability

### Possible Optimizations
1. **Predictive Routing**: Learn common transfer patterns and pre-warm connections
2. **Parallel Queries**: Query local and remote simultaneously for access checks
3. **Response Caching**: Cache QueryAccess results for repeated checks
4. **Compression**: Enable HTTP compression for large object transfers
5. **Connection Reuse**: HTTP keep-alive for reduced connection overhead

## Security Considerations

### Authentication
- Remote connections should use authentication tokens
- Auth tokens validated before agent/object operations
- Tokens should expire and be refreshed periodically

### Authorization
- Estate access controls enforced before agent entry
- Parcel permissions checked for object creation
- Admin privileges required for certain operations

### Network Security
- HTTPS recommended for production grids
- Consider VPN or private network for inter-simulator communication
- Firewall rules should restrict region ports to known simulators

### Data Validation
- Validate all incoming data from remote connectors
- Sanitize object and script data before deserialization
- Enforce size limits on transferred objects

## Related Modules

- **LocalSimulationConnectorModule**: Local-only simulation connector (used internally)
- **SimulationServiceConnector**: HTTP connector for remote operations (used internally)
- **SimulationServiceInConnectorModule**: Incoming simulation service handler
- **EntityTransferModule**: High-level agent transfer coordination
- **HGEntityTransferModule**: Hypergrid-enhanced entity transfer
- **GridService**: Provides region lookup for routing decisions

## Code Location

**Source File:**
```
src/OpenSim.Region.CoreModules/ServiceConnectorsOut/Simulation/RemoteSimulationConnector.cs
```

**Factory Registration:**
```
src/OpenSim.Region.CoreModules/ModuleFactory.cs
```
Line ~1190-1194

**Configuration:**
```
bin/OpenSim.ini
bin/config-include/GridCommon.ini
```

**Remote Connector:**
```
src/OpenSim.Services.Connectors/Simulation/SimulationServiceConnector.cs
```

## License

This module is part of OpenSimulator and is licensed under the BSD 3-Clause License. See the file header for full license text.

## Contributors

This module is maintained by the OpenSimulator development community. Contributions are welcome through the OpenSimulator project channels.

## See Also

- [LocalSimulationConnectorModule](LocalSimulationConnectorModule.md) - Local simulation connector
- [SimulationServiceInConnectorModule](SimulationServiceInConnectorModule.md) - Incoming simulation handler
- [EntityTransferModule Documentation](http://opensimulator.org/wiki/EntityTransfer) - Agent transfer system
- [Grid Architecture](http://opensimulator.org/wiki/Grid) - Grid mode architecture
- [Hypergrid](http://opensimulator.org/wiki/Hypergrid) - Inter-grid communication