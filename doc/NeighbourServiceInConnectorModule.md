# NeighbourServiceInConnectorModule Technical Documentation

## Overview

The `NeighbourServiceInConnectorModule` is a shared region module that provides incoming HTTP connectivity for neighbour region services in OpenSimulator. It enables external regions to communicate with local regions through the neighbour protocol, facilitating region-to-region communication and coordination in grid deployments.

## Module Information

- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsIn.Neighbour`
- **Assembly**: `OpenSim.Region.CoreModules.dll`
- **Interfaces**: `ISharedRegionModule`, `INeighbourService`
- **Configuration Key**: `NeighbourServiceInConnector`

## Architecture

### Class Hierarchy
```
ISharedRegionModule
INeighbourService
    └── NeighbourServiceInConnectorModule
```

### Key Components

1. **HTTP Service Connector**: Creates an HTTP endpoint for external neighbour requests
2. **Scene Management**: Maintains a collection of scenes for neighbour protocol handling
3. **Neighbour Service Provider**: Implements `INeighbourService` interface for region communication

## Configuration

### Module Activation
The module is activated through configuration in the `[Modules]` section:

```ini
[Modules]
NeighbourServiceInConnector = true
```

### Common Configuration Locations
- Grid mode: `config-include/Grid.ini` and `config-include/GridHypergrid.ini`
- Standalone mode: `config-include/StandaloneHypergrid.ini` (Hypergrid scenarios)
- Essential for grid deployments and cross-region communication

## Functionality

### Core Features

#### 1. HTTP Endpoint Registration
- Registers HTTP handlers for incoming neighbour service requests
- Uses `OpenSim.Server.Handlers.dll:NeighbourServiceInConnector` plugin
- Endpoint typically available at `/neighbour/` path

#### 2. Region Discovery and Communication
The module implements the neighbour protocol for region-to-region communication:

```csharp
public GridRegion HelloNeighbour(ulong regionHandle, RegionInfo thisRegion)
```

**Functionality:**
- Receives neighbour discovery requests from other regions
- Validates the target region handle against managed scenes
- Delegates to the appropriate scene's `IncomingHelloNeighbour()` method
- Returns region information for successful matches

#### 3. Scene Registration and Management
- Maintains a list of active scenes (`m_Scenes`)
- Adds scenes during `AddRegion()` lifecycle
- Removes scenes during `RemoveRegion()` lifecycle
- Provides efficient region handle lookup

### API Methods

#### HelloNeighbour
```csharp
GridRegion HelloNeighbour(ulong regionHandle, RegionInfo thisRegion)
```

**Parameters:**
- `regionHandle`: Target region handle for the neighbour request
- `thisRegion`: Information about the requesting region

**Returns:**
- `GridRegion`: Region information if found, null otherwise

**Process Flow:**
1. Iterates through all managed scenes
2. Compares region handle with `s.RegionInfo.RegionHandle`
3. Calls `s.IncomingHelloNeighbour(thisRegion)` for matches
4. Returns region grid information or null

## Lifecycle

### Initialization Sequence
1. **Initialise()**: Reads configuration, sets enabled state
2. **PostInitialise()**: No-op for this module
3. **AddRegion()**: Registers HTTP connector on first scene, adds scene to collection
4. **RegionLoaded()**: No additional processing
5. **RemoveRegion()**: Removes scene from collection

### State Management
- Static `m_Enabled`: Module activation state
- Static `m_Registered`: HTTP connector registration state (singleton pattern)
- Instance `m_Scenes`: Collection of managed scenes

## Dependencies

### Required Assemblies
- `OpenSim.Framework.dll`
- `OpenSim.Region.Framework.dll`
- `OpenSim.Server.Base.dll`
- `OpenSim.Server.Handlers.dll`

### Interface Dependencies
- `ISharedRegionModule`: Core module lifecycle
- `INeighbourService`: Neighbour service contract
- `IServiceConnector`: HTTP connector interface

### Service Dependencies
- `MainServer.Instance`: HTTP server for endpoint registration
- `ServerUtils.LoadPlugin<>()`: Plugin loading infrastructure

## Technical Details

### Thread Safety
- Uses static fields for global state (`m_Enabled`, `m_Registered`)
- Scene collection (`m_Scenes`) is accessed from region threads
- HTTP requests processed on separate threads

### Performance Considerations
- Linear search through scenes for region handle matching
- Efficient comparison using native `ulong` region handles
- Single HTTP connector registration regardless of scene count

### Region Handle Processing
- Uses 64-bit region handles for unique region identification
- Handles are compared directly without conversion
- No coordinate processing required (unlike land services)

## Integration Points

### Grid Services Integration
- Critical for grid mode operation and inter-region communication
- Enables region discovery and neighbour relationship establishment
- Supports Hypergrid scenarios for inter-grid region communication

### Scene Integration
- Integrates with each scene's `IncomingHelloNeighbour()` method
- Respects individual scene configuration and capabilities
- Provides region information through `GridRegion` objects

## Neighbour Protocol

### HelloNeighbour Protocol
The neighbour protocol facilitates:
- Region discovery and registration
- Neighbour relationship establishment
- Inter-region communication setup
- Grid topology maintenance

### Message Flow
```
Remote Region → HTTP Request → NeighbourServiceInConnector →
Scene.IncomingHelloNeighbour() → GridRegion Response
```

## Logging

### Log Categories
- **Info**: Module activation and HTTP connector registration
- **Debug**: Neighbour request processing (commented out by default)

### Sample Log Output
```
[NEIGHBOUR IN CONNECTOR]: NeighbourServiceInConnector enabled
[NEIGHBOUR IN CONNECTOR]: HelloNeighbour from RegionA to RegionB
```

## Security Considerations

### Access Control
- No built-in authentication or authorization
- Relies on HTTP server security configuration and network security
- Trust-based communication between regions

### Information Exposure
- Exposes region topology and neighbour relationships
- Provides region capability and connection information
- Should be configured with appropriate network restrictions

## Troubleshooting

### Common Issues

#### Module Not Loading
- Verify `NeighbourServiceInConnector = true` in configuration
- Check that scenes are being added to the module
- Confirm HTTP server is running and accessible

#### Neighbour Requests Failing
- Verify region handle accuracy in requests
- Check that target region is registered with the module
- Confirm network connectivity between regions

#### HTTP Endpoint Issues
- Verify MainServer.Instance is properly configured
- Check for port conflicts or firewall restrictions
- Confirm NeighbourServiceInConnector plugin loads correctly

## Related Components

- **NeighbourServiceInConnector**: HTTP handler in `OpenSim.Server.Handlers`
- **NeighbourServicesOutConnector**: Outgoing neighbour service client
- **Scene.IncomingHelloNeighbour()**: Scene-level neighbour protocol handling
- **GridRegion**: Data structure for region information exchange

## Grid Architecture Context

### Standalone Mode
- Typically disabled in pure standalone deployments
- May be enabled for Hypergrid connectivity

### Grid Mode
- Essential component for grid operation
- Enables region discovery and inter-region communication
- Required for proper grid topology maintenance

### Hypergrid Mode
- Supports cross-grid region communication
- Enables foreign region discovery and access
- Critical for Hypergrid functionality

## Performance Optimization

### Scene Lookup Efficiency
- Consider using region handle-indexed collections for large grids
- Current linear search is acceptable for typical deployments
- Early exit on first match provides reasonable performance

### HTTP Connector Management
- Singleton pattern prevents duplicate connector registration
- Shared connector serves all scenes efficiently
- Minimal overhead for multi-region deployments

## Version Compatibility

This module is part of the core OpenSimulator infrastructure and maintains compatibility with:
- OpenSimulator 0.9.3.x and later
- .NET 8.0+
- Compatible with grid and Hypergrid configurations
- Essential for multi-region deployments

## Configuration Examples

### Grid Configuration
```ini
[Modules]
; Enable incoming neighbour requests
NeighbourServiceInConnector = true

; Other related services
LandServiceInConnector = true
SimulationServiceInConnector = true
```

### Network Considerations
```ini
[Network]
; Ensure neighbour service port is accessible
HttpListenerPort = 9000

; Configure appropriate external hostname
ExternalHostName = "my-region-server.example.com"
```

---

*This documentation covers the NeighbourServiceInConnectorModule as integrated with OptionalModulesFactory, removing dependency on Mono.Addins while maintaining full functionality for region-to-region neighbour communication.*