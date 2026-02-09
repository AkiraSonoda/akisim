# RemoteMuteListServicesConnector

## Overview

The `RemoteMuteListServicesConnector` is a region module that provides distributed mute list services functionality for OpenSimulator grid deployments. This module enables centralized mute list management across multiple regions and serves as a bridge between local region operations and remote mute list services.

## Purpose

This connector enables grid-wide mute list management by:
- Connecting regions to centralized mute list data services
- Providing distributed mute list functionality across multiple regions
- Supporting cross-region mute list synchronization and management
- Enabling users to maintain consistent mute lists regardless of their current region

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`, `IMuteListService`
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.MuteList`
- **Dependencies**: Requires `MuteListServicesConnector` for remote communication

### Key Components

#### Core Functionality
- **Remote Service Integration**: Uses `MuteListServicesConnector` for remote service communication
- **Multi-Configuration Dependency**: Requires both `[Messaging]` and `[Modules]` configuration sections
- **Service Interface**: Implements `IMuteListService` for mute list operations

#### Configuration Dependencies
- **MuteListModule Requirement**: Only activates when `MuteListModule` is enabled in `[Messaging]` section
- **Service Selection**: Enables when `MuteListService` matches connector name in `[Modules]` section
- **Comprehensive Validation**: Provides detailed configuration validation and logging

## Configuration

### Required Configuration Sections

#### Messaging Configuration
```ini
[Messaging]
MuteListModule = MuteListModule
```

#### Module Service Configuration
```ini
[Modules]
MuteListService = RemoteMuteListServicesConnector
```

### Complete Grid Mode Configuration Example
```ini
[Messaging]
MuteListModule = MuteListModule

[Modules]
MuteListService = RemoteMuteListServicesConnector

[MuteListService]
; Configuration for remote mute list service connection
MuteListServerURI = "http://your-grid-server:8003/"
```

## Implementation Details

### Module Lifecycle

1. **Initialization** (`Initialise`)
   - Validates `[Messaging]` section exists and `MuteListModule` is properly configured
   - Checks `[Modules]` section for `MuteListService` configuration
   - Creates `MuteListServicesConnector` instance for remote communication
   - Provides comprehensive configuration validation and debug logging

2. **Region Integration** (`AddRegion`)
   - Registers the `IMuteListService` interface with the scene
   - Configures region-specific mute list service functionality
   - Provides informational logging for region integration

3. **Region Loading** (`RegionLoaded`)
   - Completes region-specific initialization
   - Logs successful region loading

4. **Cleanup** (`RemoveRegion`)
   - Handles clean removal from regions
   - Provides debug logging for removal operations

### Service Operations

The module implements three core `IMuteListService` operations:

#### MuteListRequest Method
```csharp
public Byte[] MuteListRequest(UUID agentID, uint crc)
```
- Retrieves complete mute list data for a specific agent
- Uses CRC (Cyclic Redundancy Check) for data integrity verification
- Returns serialized mute list data or null if unavailable
- Includes comprehensive debug logging for requests and responses

#### UpdateMute Method
```csharp
public bool UpdateMute(MuteData mute)
```
- Adds or updates a mute entry in the remote service
- Accepts `MuteData` object containing mute information
- Returns success/failure status
- Logs detailed mute operation information

#### RemoveMute Method
```csharp
public bool RemoveMute(UUID agentID, UUID muteID, string muteName)
```
- Removes a specific mute entry from the remote service
- Requires agent ID, mute target ID, and mute target name
- Returns success/failure status
- Provides detailed removal operation logging

### Logging and Diagnostics

The module provides extensive logging with the prefix `[REMOTE MUTE LIST CONNECTOR]`:

- **Info Level**: Module enablement, region integration, and major operations
- **Debug Level**: Configuration validation, detailed operation tracking, request/response logging
- **Parameter Logging**: Comprehensive logging of service operation parameters and results

#### Log Examples
```
[REMOTE MUTE LIST CONNECTOR]: Remote mute list connector enabled for distributed mute list services
[REMOTE MUTE LIST CONNECTOR]: Added to region RegionName and registered IMuteListService interface
[REMOTE MUTE LIST CONNECTOR]: MuteListRequest for agent uuid, CRC: 12345
[REMOTE MUTE LIST CONNECTOR]: MuteListRequest successful - returned 1024 bytes
[REMOTE MUTE LIST CONNECTOR]: UpdateMute for agent uuid, mute: targetUuid (TargetName)
[REMOTE MUTE LIST CONNECTOR]: UpdateMute result: True
```

## Integration with OptionalModulesFactory

This module has been integrated into the `OptionalModulesFactory` pattern, removing dependency on Mono.Addins:

### Factory Integration
- Loaded through `OptionalModulesFactory.CreateOptionalSharedModules()`
- Configuration-based instantiation using `MuteListService` setting
- Comprehensive logging for factory operations
- Maintains existing dual-configuration dependency pattern

### Migration from Mono.Addins
- Removed from `OpenSim.Region.CoreModules.addin.xml`
- Added to `OptionalModulesFactory` for dynamic loading
- Preserves full compatibility with existing configuration patterns
- Maintains dependency on both `[Messaging]` and `[Modules]` sections

## Usage Scenarios

### Grid Deployments
- **Distributed Grids**: Central mute list services with multiple region servers
- **Hypergrid Configurations**: Cross-grid mute list synchronization
- **Scalable Architectures**: Centralized user data with distributed region processing

### Service Dependencies
- Requires functional remote mute list service endpoints
- Depends on proper `MuteListModule` configuration in `[Messaging]` section
- Integrates with existing OpenSimulator messaging infrastructure

## Troubleshooting

### Common Issues

1. **Module Not Loading**
   - Verify `MuteListModule = MuteListModule` in `[Messaging]` section
   - Ensure `MuteListService = RemoteMuteListServicesConnector` in `[Modules]` section
   - Check log output for configuration validation messages
   - Confirm OptionalModulesFactory integration

2. **Configuration Validation Failures**
   - Enable debug logging to see detailed configuration validation
   - Verify both `[Messaging]` and `[Modules]` sections exist
   - Check for typos in configuration keys and values

3. **Remote Service Connection Issues**
   - Verify remote mute list service endpoint configuration
   - Check network connectivity to grid services
   - Review `MuteListServicesConnector` configuration and connectivity

4. **Service Operation Failures**
   - Enable debug logging to see detailed operation information
   - Verify agent UUIDs and mute target information
   - Check remote service availability and configuration
   - Monitor network connectivity and service response times

### Debug Configuration
Enable detailed logging by setting log4net configuration:

```xml
<logger name="OpenSim.Region.CoreModules.ServiceConnectorsOut.MuteList">
    <level value="DEBUG" />
    <appender-ref ref="Console" />
    <appender-ref ref="LogFileAppender" />
</logger>
```

## Related Components

- **MuteListServicesConnector**: Remote service connector for actual network communication
- **OptionalModulesFactory**: Factory pattern for dynamic module loading
- **IMuteListService**: Service interface for mute list operations
- **MuteListModule**: Core mute list module in `[Messaging]` section
- **MuteData**: Data structure for mute list entries

## Development Notes

### Code Quality
- Follows established OpenSimulator coding patterns
- Includes comprehensive error handling and logging
- Implements proper module lifecycle management
- Uses defensive programming practices with null checks and validation

### Performance Considerations
- Efficient parameter validation and logging
- Proper delegation to specialized remote connector
- Minimal memory footprint with appropriate cleanup
- CRC-based data integrity checking for mute list requests

### Maintenance
- Part of the OptionalModulesFactory modernization effort
- Removed Mono.Addins dependency for improved maintainability
- Follows consistent logging and configuration patterns
- Maintains backward compatibility with existing configurations

## Configuration Dependencies

### Critical Configuration Requirements
1. **Messaging Section**: Must have `MuteListModule = MuteListModule`
2. **Modules Section**: Must have `MuteListService = RemoteMuteListServicesConnector`
3. **Service Configuration**: Remote service endpoint must be properly configured

### Configuration Validation Flow
```
[Messaging] section exists? → MuteListModule enabled? → [Modules] section exists? → MuteListService matches? → Enable module
```

## Version History

- **Current**: Integrated with OptionalModulesFactory, enhanced logging, removed Mono.Addins dependency
- **Previous**: Mono.Addins-based loading with basic logging functionality

This module represents a modernized approach to mute list service connectivity in OpenSimulator, providing robust distributed mute list functionality with improved maintainability, comprehensive operational visibility, and dual-configuration validation patterns.