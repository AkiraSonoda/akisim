# RemoteSimulationConnectorModule

## Overview

The `RemoteSimulationConnectorModule` is a region module that provides distributed simulation services functionality for OpenSimulator grid deployments. This module enables cross-region agent and object operations by facilitating communication between different simulators and regions in a distributed grid environment.

## Purpose

This connector enables grid-wide simulation coordination by:
- Facilitating agent transfers between regions (teleports, crossings)
- Enabling cross-region object operations and communications
- Supporting distributed agent state synchronization
- Providing remote access control and permission queries
- Enabling seamless cross-region experiences for users

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`, `ISimulationService`
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation`
- **Dependencies**: Requires `SimulationServiceConnector` for remote communication

### Key Components

#### Core Functionality
- **Remote-Only Operation**: Operates purely as a remote connector without local simulation fallbacks
- **Agent Management**: Handles agent creation, updates, queries, and cleanup across regions
- **Object Operations**: Supports cross-region object transfers and operations
- **Service Interface**: Implements `ISimulationService` for simulation operations

#### Configuration Management
- Reads configuration from the `[Modules]` section
- Enables when `SimulationServices = "RemoteSimulationConnectorModule"`
- Provides comprehensive configuration validation and logging

## Configuration

### Module Configuration
Add to your `OpenSim.ini` or appropriate configuration file:

```ini
[Modules]
SimulationServices = RemoteSimulationConnectorModule
```

### Grid Mode Configuration
This module is typically used in grid deployments where simulation services are distributed:

```ini
[Modules]
SimulationServices = RemoteSimulationConnectorModule

[SimulationService]
; Configuration for remote simulation service communication
; Service connectors are configured automatically
```

## Implementation Details

### Module Lifecycle

1. **Initialization** (`Initialise`)
   - Reads `[Modules]` configuration section
   - Creates `SimulationServiceConnector` instance for remote communication
   - Enables module if `SimulationServices` matches connector name
   - Logs initialization status with detailed debugging information

2. **Region Integration** (`AddRegion`)
   - Performs one-time initialization on first region addition
   - Registers the `ISimulationService` interface with each scene
   - Provides comprehensive logging for region integration

3. **Region Loading** (`RegionLoaded`)
   - Completes region-specific initialization
   - Logs successful region loading

4. **Cleanup** (`RemoveRegion`)
   - Unregisters `ISimulationService` interface from regions
   - Handles clean removal from regions with logging

### Service Operations

The module implements comprehensive `ISimulationService` operations:

#### Agent-Related Communications

##### CreateAgent Method
```csharp
public bool CreateAgent(GridRegion source, GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags, EntityTransferContext ctx, out string reason)
```
- Creates agent presence in destination region during teleports
- Handles agent circuit data transfer and initialization
- Returns success/failure status with detailed reason information
- Critical for cross-region agent transfers and teleportation

##### UpdateAgent Methods
```csharp
public bool UpdateAgent(GridRegion destination, AgentData cAgentData, EntityTransferContext ctx)
public bool UpdateAgent(GridRegion destination, AgentPosition cAgentData)
```
- Updates agent state information in remote regions
- Handles agent data synchronization during region transitions
- Supports both full agent data and position-only updates
- Essential for maintaining agent state consistency

##### QueryAccess Method
```csharp
public bool QueryAccess(GridRegion destination, UUID agentID, string agentHomeURI, bool viaTeleport, Vector3 position, List<UUID> features, EntityTransferContext ctx, out string reason)
```
- Queries destination region for agent access permissions
- Pre-validates teleport and crossing attempts
- Supports feature negotiation and compatibility checking
- Returns access status with detailed reason information

##### Agent Cleanup Methods
```csharp
public bool ReleaseAgent(UUID origin, UUID id, string uri)
public bool CloseAgent(GridRegion destination, UUID id, string auth_token)
```
- Handles agent cleanup and resource release operations
- Manages agent session termination across regions
- Ensures proper cleanup during failed transfers or logouts

#### Object-Related Communications

##### CreateObject Method
```csharp
public bool CreateObject(GridRegion destination, Vector3 newPosition, ISceneObject sog, bool isLocalCall)
```
- Handles cross-region object transfers and creation
- Supports object rezzing and inter-region object movement
- Manages object state preservation during transfers

#### Internal Service Methods

##### GetScene Method
```csharp
public IScene GetScene(UUID regionId)
```
- Returns null for remote-only operation
- Local scene queries not supported in distributed mode

##### GetInnerService Method
```csharp
public ISimulationService GetInnerService()
```
- Returns the underlying `SimulationServiceConnector`
- Provides access to the actual remote communication layer

### Logging and Diagnostics

The module provides extensive logging for simulation operations:

- **Info Level**: Module enablement and major operations
- **Debug Level**: Detailed operation tracking, parameter logging, and result validation
- **Agent Operations**: Comprehensive logging of agent transfers, updates, and access queries
- **Object Operations**: Detailed logging of cross-region object operations

#### Log Examples
```
Remote simulation connector enabled for distributed simulation services
Using SimulationServiceConnector for remote service communication
Added to region RegionName and registered ISimulationService interface
CreateAgent for agentUuid from SourceRegion to DestinationRegion
CreateAgent result: True, reason: none
UpdateAgent for agentUuid to DestinationRegion
QueryAccess for agent agentUuid to DestinationRegion, viaTeleport: True
QueryAccess result: True, reason: Access granted
```

## Integration with OptionalModulesFactory

This module has been integrated into the `OptionalModulesFactory` pattern, removing dependency on Mono.Addins:

### Factory Integration
- Loaded through `OptionalModulesFactory.CreateOptionalSharedModules()`
- Configuration-based instantiation using `SimulationServices` setting
- Comprehensive logging for factory operations

### Migration from Mono.Addins
- Removed from `OpenSim.Region.CoreModules.addin.xml`
- Added to `OptionalModulesFactory` for dynamic loading
- Maintains full compatibility with existing configurations
- Preserves remote-only operational architecture

## Usage Scenarios

### Grid Deployments
- **Distributed Grids**: Multiple region servers with centralized or peer-to-peer simulation communication
- **Hypergrid Configurations**: Cross-grid agent and object transfers
- **Scalable Architectures**: Load-balanced region distribution with seamless user experience

### Simulation Operations
- **Agent Teleportation**: Coordinated agent transfers between regions
- **Region Crossings**: Seamless movement across region boundaries  
- **Object Transfers**: Cross-region object rezzing and movement
- **Access Control**: Permission validation for cross-region operations

### Service Dependencies
- Requires functional remote simulation service endpoints
- Depends on proper grid infrastructure and region discovery
- Integrates with existing OpenSimulator transport and communication layers

## Troubleshooting

### Common Issues

1. **Module Not Loading**
   - Verify `SimulationServices = RemoteSimulationConnectorModule` in `[Modules]` section
   - Check log output for configuration validation messages
   - Ensure OptionalModulesFactory is properly integrated

2. **Agent Transfer Failures**
   - Enable debug logging to see detailed transfer operations
   - Verify region connectivity and network accessibility
   - Check destination region capacity and access permissions
   - Monitor CreateAgent and QueryAccess operations

3. **Cross-Region Communication Issues**
   - Verify network connectivity between simulators
   - Check firewall and port configurations
   - Monitor simulation service endpoints and availability
   - Review grid service configuration and region registration

4. **Object Transfer Problems**
   - Enable debug logging for CreateObject operations
   - Verify object serialization and deserialization processes
   - Check destination region object limits and permissions
   - Monitor network capacity for large object transfers

### Debug Configuration
Enable detailed logging by setting log4net configuration:

```xml
<logger name="OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation">
    <level value="DEBUG" />
    <appender-ref ref="Console" />
    <appender-ref ref="LogFileAppender" />
</logger>
```

## Related Components

- **SimulationServiceConnector**: Remote service connector for actual network communication
- **OptionalModulesFactory**: Factory pattern for dynamic module loading
- **ISimulationService**: Service interface for simulation operations
- **EntityTransferContext**: Context information for agent and object transfers
- **AgentCircuitData**: Agent initialization and authentication data
- **GridRegion**: Region identification and location information

## Development Notes

### Code Quality
- Follows established OpenSimulator coding patterns
- Includes comprehensive error handling and logging
- Implements proper module lifecycle management
- Uses defensive programming with null checks and validation

### Performance Considerations
- Efficient remote communication with proper error handling
- Minimal memory footprint with appropriate cleanup
- Optimized for high-frequency cross-region operations
- Network-efficient data serialization and transfer protocols

### Maintenance
- Part of the OptionalModulesFactory modernization effort
- Removed Mono.Addins dependency for improved maintainability
- Follows consistent logging and configuration patterns
- Maintains backward compatibility with existing grid configurations

## Operational Considerations

### Remote-Only Architecture
- **No Local Fallback**: Operates purely in distributed mode without local simulation backends
- **Network Dependency**: Requires reliable network connectivity between regions
- **Service Discovery**: Depends on proper grid service configuration for region location
- **Error Handling**: Comprehensive error handling for network and service failures

### Security and Authentication
- **Agent Authentication**: Validates agent credentials during transfers
- **Access Control**: Implements permission checks for cross-region operations
- **Secure Communication**: Uses authenticated channels for sensitive operations
- **Token Management**: Handles authentication tokens for agent operations

## Version History

- **Current**: Integrated with OptionalModulesFactory, enhanced logging, removed Mono.Addins dependency
- **Previous**: Mono.Addins-based loading with basic logging functionality

This module represents a modernized approach to simulation service connectivity in OpenSimulator, providing robust distributed simulation functionality with improved maintainability, comprehensive operational visibility, and efficient cross-region communication capabilities.