# RemotePresenceServicesConnector

## Overview

The `RemotePresenceServicesConnector` is a region module that provides distributed presence services functionality for OpenSimulator grid deployments. This module enables centralized presence tracking and management across multiple regions, allowing for coordinated user presence information throughout the grid infrastructure.

## Purpose

This connector enables grid-wide presence management by:
- Connecting regions to centralized presence data services
- Providing distributed presence tracking across multiple regions
- Supporting cross-region presence queries and agent status monitoring
- Enabling coordinated login/logout operations across the grid
- Facilitating automatic presence detection and reporting

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule` (inherited from `BasePresenceServiceConnector`)
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.Presence`
- **Base Class**: `BasePresenceServiceConnector` (implements `IPresenceService`)
- **Dependencies**: Requires `PresenceServicesConnector` for remote communication

### Key Components

#### Core Functionality
- **Remote Service Integration**: Uses `PresenceServicesConnector` for remote service communication
- **Automatic Presence Detection**: Integrates `PresenceDetector` for automatic presence tracking
- **Service Interface**: Implements `IPresenceService` through inheritance from `BasePresenceServiceConnector`
- **Centralized Architecture**: Operates through centralized presence services without local fallbacks

#### Configuration Management
- Reads configuration from the `[Modules]` section
- Enables when `PresenceServices = "RemotePresenceServicesConnector"`
- Provides comprehensive configuration validation and logging

## Configuration

### Module Configuration
Add to your `OpenSim.ini` or appropriate configuration file:

```ini
[Modules]
PresenceServices = RemotePresenceServicesConnector
```

### Grid Mode Configuration
This module is typically used in grid deployments where presence services are centralized:

```ini
[Modules]
PresenceServices = RemotePresenceServicesConnector

[PresenceService]
; Configuration for remote presence service connection
PresenceServerURI = "http://your-grid-server:8003/"
```

## Implementation Details

### Module Lifecycle

1. **Initialization** (`Initialise`)
   - Reads `[Modules]` configuration section
   - Creates `PresenceServicesConnector` instance for remote communication
   - Initializes `PresenceDetector` for automatic presence tracking
   - Enables module if `PresenceServices` matches connector name
   - Logs initialization status with detailed debugging information

2. **Region Integration** (`AddRegion` - inherited from BasePresenceServiceConnector)
   - Registers the `IPresenceService` interface with the scene
   - Adds `PresenceDetector` to the region for automatic tracking
   - Provides informational logging for region integration

3. **Region Loading** (`RegionLoaded` - inherited from BasePresenceServiceConnector)
   - Completes region-specific initialization
   - Logs successful region loading

4. **Cleanup** (`RemoveRegion` - inherited from BasePresenceServiceConnector)
   - Removes `PresenceDetector` from region
   - Handles clean removal from regions with logging

### Service Operations

The module implements comprehensive `IPresenceService` operations through its base class:

#### LoginAgent Method
```csharp
public bool LoginAgent(string userID, UUID sessionID, UUID secureSessionID)
```
- **Not Implemented at Simulator Level**: Returns false with warning log
- Login operations are handled by grid services, not individual simulators

#### LogoutAgent Method
```csharp
public bool LogoutAgent(UUID sessionID)
```
- Logs out a specific agent session from the presence system
- Returns success/failure status
- Includes comprehensive debug logging

#### LogoutRegionAgents Method
```csharp
public bool LogoutRegionAgents(UUID regionID)
```
- Logs out all agents from a specific region
- Useful for region shutdown or emergency operations
- Returns success/failure status with detailed logging

#### ReportAgent Method
```csharp
public bool ReportAgent(UUID sessionID, UUID regionID)
```
- Reports agent presence in a specific region
- Updates agent's current region location
- Critical for cross-region teleports and presence tracking
- Includes detailed parameter and result logging

#### GetAgent Method
```csharp
public PresenceInfo GetAgent(UUID sessionID)
```
- Retrieves presence information for a specific agent session
- Returns `PresenceInfo` object with agent location and status
- Includes comprehensive debug logging for requests and responses

#### GetAgents Method
```csharp
public PresenceInfo[] GetAgents(string[] userIDs)
```
- Retrieves presence information for multiple agents
- Optimized to avoid network calls for empty requests
- Returns array of `PresenceInfo` objects
- Includes batch operation logging

### Logging and Diagnostics

The module provides extensive logging with two main prefixes:

#### RemotePresenceServicesConnector Logging
- **Prefix**: `[REMOTE PRESENCE CONNECTOR]`
- **Scope**: Initialization, configuration, and connector-specific operations

#### BasePresenceServiceConnector Logging
- **Prefix**: `[BASE PRESENCE SERVICE CONNECTOR]`
- **Scope**: Region lifecycle, service operations, and presence tracking

#### Log Examples
```
[REMOTE PRESENCE CONNECTOR]: Remote presence connector enabled for distributed presence services
[REMOTE PRESENCE CONNECTOR]: Using PresenceServicesConnector for remote service communication
[BASE PRESENCE SERVICE CONNECTOR]: Added to region RegionName and registered IPresenceService interface
[BASE PRESENCE SERVICE CONNECTOR]: ReportAgent for session uuid in region regionUuid
[BASE PRESENCE SERVICE CONNECTOR]: GetAgent successful - user: userUuid, region: regionUuid
[BASE PRESENCE SERVICE CONNECTOR]: GetAgents for 5 users
[BASE PRESENCE SERVICE CONNECTOR]: GetAgents returned 3 presence records
```

## Integration with OptionalModulesFactory

This module has been integrated into the `OptionalModulesFactory` pattern, removing dependency on Mono.Addins:

### Factory Integration
- Loaded through `OptionalModulesFactory.CreateOptionalSharedModules()`
- Configuration-based instantiation using `PresenceServices` setting
- Comprehensive logging for factory operations

### Migration from Mono.Addins
- Removed from `OpenSim.Region.CoreModules.addin.xml`
- Added to `OptionalModulesFactory` for dynamic loading
- Maintains full compatibility with existing configurations
- Preserves inheritance-based architecture with `BasePresenceServiceConnector`

## Usage Scenarios

### Grid Deployments
- **Distributed Grids**: Central presence services with multiple region servers
- **Hypergrid Configurations**: Cross-grid presence tracking and coordination
- **Scalable Architectures**: Centralized user presence with distributed region processing

### Presence Tracking Use Cases
- **Cross-Region Teleports**: Coordinating agent movement between regions
- **Friend Online Status**: Tracking friend presence across the grid
- **Administrative Operations**: Monitoring user activity and locations
- **Load Balancing**: Understanding user distribution across regions

### Service Dependencies
- Requires functional remote presence service endpoints
- Depends on proper `PresenceServicesConnector` configuration
- Integrates with existing OpenSimulator presence infrastructure
- Requires grid-wide presence service coordination

## Troubleshooting

### Common Issues

1. **Module Not Loading**
   - Verify `PresenceServices = RemotePresenceServicesConnector` in `[Modules]` section
   - Check log output for configuration validation messages
   - Ensure OptionalModulesFactory is properly integrated

2. **Remote Service Connection Issues**
   - Verify remote presence service endpoint configuration
   - Check network connectivity to grid services
   - Review `PresenceServicesConnector` configuration and connectivity

3. **Presence Tracking Failures**
   - Enable debug logging to see detailed operation information
   - Verify session IDs and region IDs in requests
   - Check PresenceDetector functionality and configuration
   - Monitor network connectivity and service response times

4. **Cross-Region Presence Issues**
   - Verify `ReportAgent` calls are working correctly during teleports
   - Check presence synchronization across multiple regions
   - Monitor presence service database for consistency

### Debug Configuration
Enable detailed logging by setting log4net configuration:

```xml
<logger name="OpenSim.Region.CoreModules.ServiceConnectorsOut.Presence">
    <level value="DEBUG" />
    <appender-ref ref="Console" />
    <appender-ref ref="LogFileAppender" />
</logger>
```

## Related Components

- **BasePresenceServiceConnector**: Base class providing core presence service functionality
- **PresenceServicesConnector**: Remote service connector for network communication
- **PresenceDetector**: Automatic presence detection and tracking component
- **OptionalModulesFactory**: Factory pattern for dynamic module loading
- **IPresenceService**: Service interface for presence operations
- **PresenceInfo**: Data structure for presence information

## Development Notes

### Code Quality
- Follows established OpenSimulator coding patterns
- Uses inheritance for code reuse and maintainability
- Includes comprehensive error handling and logging
- Implements proper module lifecycle management

### Performance Considerations
- Optimized batch operations for multiple agent queries
- Efficient network call management with empty request checks
- Proper delegation to specialized remote connector
- Minimal memory footprint with appropriate cleanup

### Maintenance
- Part of the OptionalModulesFactory modernization effort
- Removed Mono.Addins dependency for improved maintainability
- Follows consistent logging and configuration patterns
- Maintains backward compatibility with existing configurations

## Architecture Benefits

### Inheritance-Based Design
- **Code Reuse**: `BasePresenceServiceConnector` provides common functionality
- **Modularity**: Specific connectors focus on configuration and initialization
- **Maintainability**: Changes to base functionality benefit all connectors
- **Consistency**: Uniform interface and behavior across presence connectors

### Automatic Presence Detection
- **PresenceDetector Integration**: Automatic tracking without manual intervention
- **Event-Driven Updates**: Real-time presence updates based on region events
- **Minimal Overhead**: Efficient presence tracking with minimal performance impact

## Version History

- **Current**: Integrated with OptionalModulesFactory, enhanced logging, removed Mono.Addins dependency
- **Previous**: Mono.Addins-based loading with basic logging functionality

This module represents a modernized approach to presence service connectivity in OpenSimulator, providing robust distributed presence functionality with improved maintainability, comprehensive operational visibility, and efficient inheritance-based architecture.