# LandServiceInConnectorModule Technical Documentation

## Overview

The `LandServiceInConnectorModule` is a shared region module that provides incoming HTTP connectivity for land data services in OpenSimulator. It enables external systems to query land information from regions over HTTP by implementing both `ISharedRegionModule` and `ILandService` interfaces.

## Module Information

- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsIn.Land`
- **Assembly**: `OpenSim.Region.CoreModules.dll`
- **Interfaces**: `ISharedRegionModule`, `ILandService`
- **Configuration Key**: `LandServiceInConnector`

## Architecture

### Class Hierarchy
```
ISharedRegionModule
ILandService
    └── LandServiceInConnectorModule
```

### Key Components

1. **HTTP Service Connector**: Creates an HTTP endpoint for external land data queries
2. **Scene Management**: Maintains a collection of scenes for land data lookup
3. **Land Data Provider**: Implements `ILandService` interface to respond to queries

## Configuration

### Module Activation
The module is activated through configuration in the `[Modules]` section:

```ini
[Modules]
LandServiceInConnector = true
```

### Common Configuration Locations
- Grid mode: `config-include/Grid.ini` and `config-include/GridHypergrid.ini`
- Typically enabled in grid configurations for inter-region communication

## Functionality

### Core Features

#### 1. HTTP Endpoint Registration
- Registers HTTP handlers for incoming land service requests
- Uses `OpenSim.Server.Handlers.dll:LandServiceInConnector` plugin
- Endpoint typically available at `/land/` path

#### 2. Multi-Scene Land Data Lookup
The module implements sophisticated land data lookup across multiple scenes:

```csharp
public LandData GetLandData(UUID scopeID, ulong regionHandle, uint x, uint y, out byte regionAccess)
```

**Lookup Algorithm:**
1. Converts region handle to world coordinates
2. Adjusts coordinates by provided x,y offset
3. Iterates through all managed scenes
4. Checks if coordinates fall within scene boundaries
5. Returns land data from matching scene
6. Includes dwell information if available

#### 3. Scene Registration
- Maintains a list of active scenes (`m_Scenes`)
- Adds scenes during `AddRegion()` lifecycle
- Removes scenes during `RemoveRegion()` lifecycle

### API Methods

#### GetLandData
```csharp
LandData GetLandData(UUID scopeID, ulong regionHandle, uint x, uint y, out byte regionAccess)
```

**Parameters:**
- `scopeID`: Scope identifier (typically unused)
- `regionHandle`: Target region handle
- `x, y`: Coordinates within the region
- `regionAccess`: Output parameter for region access level

**Returns:**
- `LandData`: Land parcel information or null if not found
- Includes dwell data when `IDwellModule` is available

**Coordinate Handling:**
- Supports variable-sized regions
- Performs boundary checking against `RegionSizeX` and `RegionSizeY`
- Handles world coordinate to local coordinate conversion

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
- `ILandService`: Land data service contract
- `IDwellModule`: Optional dwell information provider

### Service Dependencies
- `MainServer.Instance`: HTTP server for endpoint registration
- `ServerUtils.LoadPlugin<>()`: Plugin loading infrastructure

## Technical Details

### Thread Safety
- Uses static fields for global state (`m_Enabled`, `m_Registered`)
- Scene collection (`m_Scenes`) is accessed from region threads
- HTTP requests processed on separate threads

### Performance Considerations
- Linear search through scenes for coordinate matching
- Efficient boundary checking with early exit conditions
- Single HTTP connector registration regardless of scene count

### Error Handling
- Graceful handling of missing regions
- Debug logging for unresolved region handles
- Fallback `regionAccess = 42` for missing regions

## Integration Points

### Grid Services Integration
- Enables cross-region land data queries in grid mode
- Supports Hypergrid scenarios for inter-grid land information
- Used by external grid services for land verification

### Region Module Integration
- Integrates with `IDwellModule` for popularity metrics
- Uses scene's `GetLandData()` method for actual land lookup
- Respects region access levels and boundaries

## Logging

### Log Categories
- **Info**: Module activation and HTTP connector registration
- **Debug**: Land data lookup operations (commented out by default)
- **Debug**: Region handle resolution failures

### Sample Log Output
```
[LAND IN CONNECTOR]: LandServiceInConnector enabled
[LAND IN CONNECTOR]: region handle 1152921504875283456 not found
```

## Security Considerations

### Access Control
- No built-in authentication or authorization
- Relies on HTTP server security configuration
- Region access levels returned to caller for decision-making

### Data Exposure
- Exposes land parcel information over HTTP
- Includes ownership and property details
- Should be configured carefully in public environments

## Troubleshooting

### Common Issues

#### Module Not Loading
- Verify `LandServiceInConnector = true` in configuration
- Check that scenes are being added to the module
- Confirm HTTP server is running

#### Land Data Not Found
- Verify region handle calculation is correct
- Check that target region is registered with the module
- Confirm coordinate boundaries are within region limits

#### Missing Dwell Information
- Ensure `IDwellModule` is loaded and configured
- Check that dwell calculation is enabled in the target scene

## Related Components

- **LandServiceInConnector**: HTTP handler in `OpenSim.Server.Handlers`
- **LandManagementModule**: Core land management functionality
- **RemoteLandServicesConnector**: Outgoing land service client
- **IDwellModule**: Land popularity tracking

## Version Compatibility

This module is part of the core OpenSimulator infrastructure and maintains compatibility with:
- OpenSimulator 0.9.3.x and later
- .NET 8.0+
- Compatible with both standalone and grid configurations

---

*This documentation covers the LandServiceInConnectorModule as integrated with OptionalModulesFactory, removing dependency on Mono.Addins while maintaining full functionality.*