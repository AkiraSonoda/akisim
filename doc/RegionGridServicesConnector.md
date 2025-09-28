# RegionGridServicesConnector Technical Documentation

## Overview

The **RegionGridServicesConnector** is a critical shared region module that serves as the primary interface between OpenSimulator regions and grid services. It acts as a bridge that coordinates local and remote grid operations, providing essential functionality for region registration, neighbor discovery, and cross-grid communication in Hypergrid environments.

## Purpose

The RegionGridServicesConnector provides a unified grid service interface that:

- **Region Management**: Handles region registration and deregistration with grid services
- **Neighbor Discovery**: Manages discovery and communication with neighboring regions
- **Grid Service Bridge**: Coordinates between local and remote grid service providers
- **Performance Optimization**: Implements intelligent caching for grid data to reduce network overhead
- **Hypergrid Support**: Enables cross-grid region discovery and hyperlink management
- **Dual Mode Operation**: Supports both standalone and distributed grid architectures

## Architecture

### Interface Implementation

The module implements both:
- **ISharedRegionModule** - Shared across all regions in the simulator instance
- **IGridService** - Complete grid service interface for region operations

### Service Coordination Model

```
┌─────────────────────────────────────────┐
│        RegionGridServicesConnector      │
├─────────────────────────────────────────┤
│  Local Grid Service  │  Remote Grid     │
│  (SQLite/MySQL)      │  Service         │
│                      │  (HTTP/REST)     │
├─────────────────────────────────────────┤
│          RegionInfoCache                │
│     (Performance Layer)                 │
└─────────────────────────────────────────┘
```

The connector maintains two service channels:
1. **Local Grid Service** (`m_LocalGridService`) - Direct database access for local regions
2. **Remote Grid Service** (`m_RemoteGridService`) - Network communication with grid services

## Configuration

### Module Activation

Configure in OpenSim.ini [Modules] section:

```ini
[Modules]
GridServices = "RegionGridServicesConnector"
```

### Grid Service Configuration

#### Local Service Configuration

```ini
[GridService]
LocalServiceModule = "OpenSim.Services.GridService.dll:GridService"
```

#### Network Connector (Grid Mode)

```ini
[GridService]
LocalServiceModule = "OpenSim.Services.GridService.dll:GridService"
NetworkConnector = "OpenSim.Services.Connectors.dll:GridServicesConnector"
```

### Deployment Modes

#### Standalone Mode
- Only `LocalServiceModule` configured
- All grid operations use local database
- No network connectivity to external grid services

#### Grid Mode
- Both `LocalServiceModule` and `NetworkConnector` configured
- Local service handles region data storage
- Remote service provides grid-wide communication

## Core Functionality

### Region Lifecycle Management

#### Region Registration
```csharp
public string RegisterRegion(UUID scopeID, GridRegion regionInfo)
```
- Registers region with local grid service first
- If successful and remote service available, registers with grid network
- Returns error message on failure, empty string on success

#### Region Deregistration
```csharp
public bool DeregisterRegion(UUID regionID)
```
- Removes region from both local and remote services
- Ensures cleanup on region shutdown

### Region Discovery Services

#### By UUID
```csharp
public GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
```
- Checks cache first for performance
- Falls back to local service, then remote service
- Caches successful results

#### By Position
```csharp
public GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
```
- World coordinate-based region lookup
- Critical for avatar movement and teleportation
- Implements sophisticated caching strategy

#### By Name/URI
```csharp
public GridRegion GetRegionByName(UUID scopeID, string name)
public GridRegion GetRegionByURI(UUID scopeID, RegionURI uri)
```
- Supports both simple names and complex URIs
- Handles Hypergrid region references
- Integrates with default region fallback logic

### Specialized Discovery Operations

#### Neighbor Discovery
```csharp
public List<GridRegion> GetNeighbours(UUID scopeID, UUID regionID)
```
- Essential for seamless avatar movement
- Preloads adjacent regions for border crossing
- Uses remote service in grid mode for complete neighbor data

#### Hypergrid Operations
```csharp
public List<GridRegion> GetHyperlinks(UUID scopeID)
public List<GridRegion> GetDefaultHypergridRegions(UUID scopeID)
```
- Manages cross-grid connectivity
- Maintains hyperlink registry
- Supports Hypergrid teleportation

#### Fallback and Load Balancing
```csharp
public List<GridRegion> GetFallbackRegions(UUID scopeID, int x, int y)
public List<GridRegion> GetOnlineRegions(UUID scopeID, int x, int y, int maxCount)
```
- Provides alternative destinations when primary regions unavailable
- Supports load distribution across multiple regions
- Critical for grid reliability

## Performance Features

### RegionInfoCache System

The module includes a sophisticated caching layer that:

- **Reduces Network Calls**: Caches successful lookups to minimize remote service requests
- **Multiple Index Types**: Supports lookups by UUID, position, handle, and name
- **Automatic Cache Population**: Populates cache during normal operations
- **Neighbor Pre-loading**: Caches neighbor information proactively

### Cache Management

- **Cache Population**: Successful lookups automatically cached
- **Scope Isolation**: Cache entries isolated by scope ID
- **Efficient Lookups**: O(1) access for cached entries
- **Memory Management**: Automatic cleanup of expired entries

## Integration Points

### Scene Integration

```csharp
public void AddRegion(Scene scene)
{
    scene.RegisterModuleInterface<IGridService>(this);
    // Cache local region information
    GridRegion r = new GridRegion(scene.RegionInfo);
    m_RegionInfoCache.CacheLocal(r);
    // Subscribe to region events
    scene.EventManager.OnRegionUp += OnRegionUp;
}
```

### Event Handling

The module responds to region events:
- **OnRegionUp**: Automatically caches newly discovered regions
- **Region Removal**: Cleans up cache entries for removed regions

## Factory Integration

### Configuration-Driven Loading

The module is integrated with the CoreModuleFactory system:

```csharp
string gridServicesModule = modulesConfig?.GetString("GridServices", "");
if (gridServicesModule == "RegionGridServicesConnector")
{
    if(m_log.IsDebugEnabled) m_log.Debug("Loading RegionGridServicesConnector for grid service communication and region discovery");
    yield return new RegionGridServicesConnector();
    if(m_log.IsInfoEnabled) m_log.Info("RegionGridServicesConnector loaded for grid service interface, region lookups, neighbor discovery, and Hypergrid support");
}
```

### Dependency Management

- **No Mono.Addins Dependency**: Removed for factory-based loading
- **Service Dependencies**: Requires configured LocalServiceModule
- **Optional Remote Service**: NetworkConnector optional for standalone mode

## Error Handling and Resilience

### Service Fallback Logic

1. **Cache First**: Always check cache before service calls
2. **Local Service**: Try local service for all operations
3. **Remote Service**: Fall back to remote service in grid mode
4. **Graceful Degradation**: Continue operation with reduced functionality if remote service unavailable

### Logging and Monitoring

The module provides comprehensive logging:

```csharp
// Debug level - detailed operation tracking
if(m_log.IsDebugEnabled) m_log.DebugFormat("Found region {0} on local. Pos=<{1},{2}>",
    rinfo.RegionName, rinfo.RegionCoordX, rinfo.RegionCoordY);

// Warning level - operational issues
m_log.WarnFormat("Requested region {0}-{1} not found", regionX, regionY);

// Info level - significant events
m_log.Info("enabled in Grid mode");
```

## Security Considerations

### Scope Isolation

All operations require scope ID parameter:
- Prevents cross-scope data leakage
- Ensures proper multi-tenant operation
- Maintains grid boundary enforcement

### Data Validation

- **Input Sanitization**: Validates region coordinates and identifiers
- **Service Authentication**: Relies on underlying service authentication
- **Cache Integrity**: Ensures cached data consistency

## Troubleshooting

### Common Configuration Issues

#### Module Not Loading
- Verify `GridServices = "RegionGridServicesConnector"` in [Modules] section
- Check that module appears in factory logging output

#### Local Service Failure
```
Error: "No LocalServiceModule named in section GridService"
```
**Solution**: Configure LocalServiceModule in [GridService] section

#### Network Service Issues
```
Error: "failed to load NetworkConnector"
```
**Solution**:
- Verify NetworkConnector DLL availability
- Check network connectivity to grid services
- Review authentication configuration

### Performance Issues

#### High Network Traffic
- Monitor cache hit ratios in debug logs
- Consider increasing cache sizes
- Review region neighbor configurations

#### Slow Region Discovery
- Check database performance for local service
- Monitor network latency to remote services
- Review region index configurations

## Monitoring and Metrics

### Key Performance Indicators

- **Cache Hit Ratio**: Percentage of lookups served from cache
- **Service Response Times**: Local vs remote service performance
- **Failed Lookup Rate**: Percentage of unsuccessful region lookups
- **Neighbor Discovery Latency**: Time to discover adjacent regions

### Log Analysis

Monitor logs for:
- `"Found region X on local"` - Local service success
- `"Added region X to the cache"` - Remote service integration
- `"Requested region X-Y not found"` - Missing region warnings

## Advanced Configuration

### Multi-Grid Environments

For complex grid topologies:

```ini
[GridService]
LocalServiceModule = "OpenSim.Services.GridService.dll:GridService"
NetworkConnector = "OpenSim.Services.Connectors.dll:GridServicesConnector"

# Additional grid-specific settings
ScopeID = "00000000-0000-0000-0000-000000000000"
DefaultRegion = "MyRegion"
```

### Performance Tuning

```ini
[GridService]
# Optimize for high-traffic grids
MaxRegions = 10000
CacheTimeout = 300
```

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Driven**: Activation controlled by configuration settings
- **Enhanced Logging**: Improved debugging and monitoring capabilities
- **Backward Compatibility**: Maintains full API compatibility

### Upgrade Considerations

- Update configuration files to use factory loading
- Review logging configuration for new message formats
- Monitor performance during initial migration
- Verify Hypergrid connectivity after upgrade

## Related Components

### Dependencies
- **OpenSim.Services.GridService**: Local grid service implementation
- **OpenSim.Services.Connectors**: Remote grid service connectors
- **RegionInfoCache**: Performance optimization layer

### Integration Points
- **Scene Management**: Region lifecycle integration
- **Avatar Movement**: Teleportation and border crossing
- **World Map**: Region discovery for map functionality
- **Hypergrid**: Cross-grid communication support

---

*This documentation covers RegionGridServicesConnector as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full grid service functionality and performance optimization.*