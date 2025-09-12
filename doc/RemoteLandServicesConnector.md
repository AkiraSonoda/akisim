# RemoteLandServicesConnector

## Overview

The `RemoteLandServicesConnector` is a region module that provides distributed land services functionality for OpenSimulator grid deployments. This module operates in remote-only mode, connecting to centralized land services without requiring local service fallbacks.

## Purpose

This connector enables grid-wide land parcel management by:
- Connecting regions to centralized land data services
- Providing distributed land parcel information across multiple regions
- Supporting cross-region land queries and management
- Operating without local land service dependencies for simplified grid deployments

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`, `ILandService`
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.Land`
- **Base Class**: `LandServicesConnector`

### Key Components

#### Core Functionality
- **Remote-Only Operation**: Operates purely as a remote connector without local service fallbacks
- **Grid Integration**: Integrates with the grid service infrastructure for cross-region functionality
- **Service Interface**: Implements `ILandService` for land data operations

#### Configuration Management
- Reads configuration from the `[Modules]` section
- Enables when `LandServices = "RemoteLandServicesConnector"`
- Provides comprehensive configuration validation and logging

## Configuration

### Module Configuration
Add to your `OpenSim.ini` or appropriate configuration file:

```ini
[Modules]
LandServices = RemoteLandServicesConnector
```

### Grid Mode Configuration
This module is typically used in grid deployments where land services are centralized:

```ini
[Modules]
LandServices = RemoteLandServicesConnector

[LandService]
; Configuration for remote land service connection
LandServerURI = "http://your-grid-server:8003/"
```

## Implementation Details

### Module Lifecycle

1. **Initialization** (`Initialise`)
   - Reads `[Modules]` configuration
   - Enables module if `LandServices` matches connector name
   - Logs initialization status with detailed debugging information

2. **Region Integration** (`AddRegion`)
   - Registers the `ILandService` interface with the scene
   - Configures region-specific land service functionality
   - Provides debug logging for region integration

3. **Region Loading** (`RegionLoaded`)
   - Configures GridService dependency
   - Establishes connection to grid infrastructure
   - Logs successful configuration

4. **Cleanup** (`RemoveRegion`)
   - Handles clean removal from regions
   - Provides logging for removal operations

### Service Operations

#### GetLandData Method
The primary service method that:
- Retrieves land parcel data from remote services
- Accepts scope ID, region handle, and coordinates
- Returns `LandData` object with parcel information
- Includes comprehensive debug logging for requests and responses

```csharp
public override LandData GetLandData(UUID scopeID, ulong regionHandle, uint x, uint y, out byte regionAccess)
```

### Logging and Diagnostics

The module provides extensive logging with the prefix `[REMOTE LAND CONNECTOR]`:

- **Info Level**: Module enablement and major operations
- **Debug Level**: Detailed operation tracking, configuration status, and request/response logging
- **Error Conditions**: Configuration issues and service failures

#### Log Examples
```
[REMOTE LAND CONNECTOR]: Remote Land connector enabled for grid-wide land services
[REMOTE LAND CONNECTOR]: Added to region RegionName and registered ILandService interface
[REMOTE LAND CONNECTOR]: GetLandData request - scopeID: uuid, regionHandle: handle, position: (x,y)
[REMOTE LAND CONNECTOR]: GetLandData successful - parcel: ParcelName, owner: uuid
```

## Integration with OptionalModulesFactory

This module has been integrated into the `OptionalModulesFactory` pattern, removing dependency on Mono.Addins:

### Factory Integration
- Loaded through `OptionalModulesFactory.CreateOptionalSharedModules()`
- Configuration-based instantiation using `LandServices` setting
- Comprehensive logging for factory operations

### Migration from Mono.Addins
- Removed from `OpenSim.Region.CoreModules.addin.xml`
- Added to `OptionalModulesFactory` for dynamic loading
- Maintains full compatibility with existing configurations

## Usage Scenarios

### Grid Deployments
- **Distributed Grids**: Central land services with multiple region servers
- **Hypergrid Configurations**: Cross-grid land parcel access and management
- **Scalable Architectures**: Centralized land data with distributed region processing

### Service Dependencies
- Requires functional GridService for cross-region operations
- Depends on properly configured remote land service endpoints
- Integrates with existing OpenSimulator service infrastructure

## Troubleshooting

### Common Issues

1. **Module Not Loading**
   - Verify `LandServices = RemoteLandServicesConnector` in `[Modules]` section
   - Check log output for configuration validation messages
   - Ensure OptionalModulesFactory is properly integrated

2. **Remote Service Connection Issues**
   - Verify remote land service endpoint configuration
   - Check network connectivity to grid services
   - Review GridService configuration and connectivity

3. **Land Data Retrieval Failures**
   - Enable debug logging to see detailed request/response information
   - Verify region handles and coordinate parameters
   - Check remote service availability and configuration

### Debug Configuration
Enable detailed logging by setting log4net configuration:

```xml
<logger name="OpenSim.Region.CoreModules.ServiceConnectorsOut.Land">
    <level value="DEBUG" />
    <appender-ref ref="Console" />
    <appender-ref ref="LogFileAppender" />
</logger>
```

## Related Components

- **LandServicesConnector**: Base class providing core connectivity functionality
- **OptionalModulesFactory**: Factory pattern for dynamic module loading
- **ILandService**: Service interface for land operations
- **GridService**: Grid infrastructure dependency

## Development Notes

### Code Quality
- Follows established OpenSimulator coding patterns
- Includes comprehensive error handling and logging
- Implements proper module lifecycle management
- Uses defensive programming practices

### Performance Considerations
- Remote-only operation eliminates local service overhead
- Efficient parameter validation and logging
- Minimal memory footprint with proper cleanup

### Maintenance
- Part of the OptionalModulesFactory modernization effort
- Removed Mono.Addins dependency for improved maintainability
- Follows consistent logging and configuration patterns

## Version History

- **Current**: Integrated with OptionalModulesFactory, enhanced logging, removed Mono.Addins dependency
- **Previous**: Mono.Addins-based loading with basic logging functionality

This module represents a modernized approach to land service connectivity in OpenSimulator, providing robust distributed land services functionality with improved maintainability and comprehensive operational visibility.