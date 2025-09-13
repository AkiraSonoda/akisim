# SimulationServiceInConnectorModule Technical Documentation

## Overview

The `SimulationServiceInConnectorModule` is a shared region module that provides incoming HTTP connectivity for simulation services in OpenSimulator. It enables external regions to transfer agents (avatars) and objects to local regions through the simulation protocol, facilitating seamless movement and interaction across grid deployments.

## Module Information

- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsIn.Simulation`
- **Assembly**: `OpenSim.Region.CoreModules.dll`
- **Interfaces**: `ISharedRegionModule`
- **Configuration Key**: `SimulationServiceInConnector`

## Architecture

### Class Hierarchy
```
ISharedRegionModule
    └── SimulationServiceInConnectorModule
```

### Key Components

1. **HTTP Service Connector**: Creates an HTTP endpoint for external simulation requests
2. **Agent Transfer Handling**: Processes incoming agent transfers from other regions
3. **Object Transfer Handling**: Manages object transfers and attachments
4. **Scene Integration**: Integrates with local scenes for transfer processing

## Configuration

### Module Activation
The module is activated through configuration in the `[Modules]` section:

```ini
[Modules]
SimulationServiceInConnector = true
```

### Common Configuration Locations
- Grid mode: `config-include/Grid.ini` and `config-include/GridHypergrid.ini`
- Standalone mode: `config-include/StandaloneHypergrid.ini` (Hypergrid scenarios)
- Critical for grid deployments and cross-region movement

## Functionality

### Core Features

#### 1. HTTP Endpoint Registration
- Registers HTTP handlers for incoming simulation service requests
- Uses `OpenSim.Server.Handlers.dll:SimulationServiceInConnector` plugin
- Endpoint typically available at `/agent/`, `/object/`, and related simulation paths

#### 2. Agent Transfer Processing
The module handles various types of agent transfers:

**Agent Movement Types:**
- **Teleportation**: Direct agent transfer between regions
- **Border Crossing**: Continuous movement across region boundaries
- **Child Agent Creation**: Establishing agent presence in neighboring regions
- **Agent Updates**: Position and state synchronization

#### 3. Object Transfer Management
Handles object-related transfers:

**Object Operations:**
- **Attachment Transfers**: Moving attachments with agents
- **Prim Crossing**: Object movement across region boundaries
- **Temporary Objects**: Short-lived objects for effects and animations
- **Script State Transfer**: Preserving script execution state

#### 4. Scene Integration
- Registers HTTP connector once during initialization
- Provides scene reference to the simulation connector
- Enables proper routing of incoming requests to appropriate scenes

## Lifecycle

### Initialization Sequence
1. **Initialise()**: Reads configuration, sets enabled state
2. **PostInitialise()**: No-op for this module
3. **AddRegion()**: No specific region-level setup
4. **RegionLoaded()**: Registers HTTP connector on first scene load (singleton pattern)
5. **RemoveRegion()**: No specific cleanup needed

### State Management
- Static `m_Enabled`: Module activation state
- Instance `m_Registered`: HTTP connector registration state (singleton pattern)
- Single connector serves all regions in the server

## Dependencies

### Required Assemblies
- `OpenSim.Framework.dll`
- `OpenSim.Region.Framework.dll`
- `OpenSim.Server.Base.dll`
- `OpenSim.Server.Handlers.dll`

### Interface Dependencies
- `ISharedRegionModule`: Core module lifecycle
- `IServiceConnector`: HTTP connector interface

### Service Dependencies
- `MainServer.Instance`: HTTP server for endpoint registration
- `ServerUtils.LoadPlugin<>()`: Plugin loading infrastructure
- Scene services for transfer processing

## Technical Details

### Thread Safety
- Uses static field for global enabled state
- HTTP requests processed on separate threads from simulation
- Scene integration handles concurrent access appropriately

### Performance Considerations
- Single HTTP connector registration regardless of scene count
- Efficient routing through SimulationServiceInConnector handler
- Minimal overhead for multi-region deployments

### Agent Transfer Protocol
The module integrates with OpenSimulator's agent transfer protocol:

**Transfer Phases:**
1. **Pre-flight**: Destination region validation and preparation
2. **Transfer**: Agent data and state transmission
3. **Post-transfer**: Cleanup and finalization
4. **Verification**: Transfer success confirmation

## Integration Points

### Grid Services Integration
- Essential for grid mode operation and inter-region movement
- Enables seamless agent transfers between regions
- Supports Hypergrid scenarios for inter-grid travel

### Scene Integration
- Each scene can process incoming transfers independently
- Maintains region-specific transfer policies and restrictions
- Integrates with scene-level security and access controls

### Physics Integration
- Coordinates with physics engines for smooth transitions
- Handles velocity and momentum preservation
- Manages collision detection during transfers

## Simulation Protocol

### Agent Transfer Messages
The simulation protocol handles various message types:

**Core Messages:**
- **CreateAgent**: Initial agent creation in destination region
- **UpdateAgent**: Agent state and position updates
- **CloseAgent**: Agent removal and cleanup
- **QueryAccess**: Access permission verification

**Extended Messages:**
- **ReleaseAgent**: Source region agent release
- **MakeChildAgent**: Converting to child agent status
- **MakeRootAgent**: Promoting to root agent status

### Object Transfer Messages
**Object Operations:**
- **CreateObject**: Object creation in destination region
- **UpdateObject**: Object state and property updates
- **DeleteObject**: Object removal and cleanup
- **ObjectProperty**: Specific property updates

## Logging

### Log Categories
- **Info**: Module activation and HTTP connector registration
- **Debug**: Transfer request processing and routing

### Sample Log Output
```
[SIM SERVICE]: SimulationService IN connector enabled
[SIM SERVICE]: Starting...
```

## Security Considerations

### Access Control
- No built-in authentication at module level
- Relies on HTTP server security configuration
- Transfer validation handled by scene and region policies

### Data Validation
- Agent data validation during transfer processing
- Object integrity checks for transferred items
- Script state verification for secure execution

### Rate Limiting
- Potential for implementing transfer rate limiting
- DoS protection through HTTP server configuration
- Resource usage monitoring for large transfers

## Troubleshooting

### Common Issues

#### Module Not Loading
- Verify `SimulationServiceInConnector = true` in configuration
- Check that HTTP server is running and accessible
- Confirm scene is being loaded properly

#### Agent Transfers Failing
- Verify network connectivity between regions
- Check destination region capacity and restrictions
- Confirm simulation service endpoints are accessible

#### Performance Issues
- Monitor HTTP server performance under load
- Check for network latency between regions
- Verify adequate server resources for concurrent transfers

### Diagnostic Tools
- HTTP endpoint testing with curl or similar tools
- OpenSimulator console commands for transfer status
- Log analysis for transfer success/failure patterns

## Related Components

- **SimulationServiceInConnector**: HTTP handler in `OpenSim.Server.Handlers`
- **SimulationServiceOutConnector**: Outgoing simulation service client
- **Scene Transfer Methods**: Scene-level transfer processing
- **EntityTransferModule**: High-level agent transfer coordination

## Grid Architecture Context

### Standalone Mode
- Typically disabled unless Hypergrid connectivity is required
- May be enabled for testing or development scenarios

### Grid Mode
- Critical component for grid operation
- Enables region-to-region agent and object movement
- Required for proper multi-region user experience

### Hypergrid Mode
- Supports cross-grid agent transfers
- Enables foreign grid access and interaction
- Critical for Hypergrid functionality

## Configuration Examples

### Basic Grid Configuration
```ini
[Modules]
; Enable incoming simulation requests
SimulationServiceInConnector = true

; Related transfer services
EntityTransferModule = "BasicEntityTransferModule"
```

### Advanced Configuration
```ini
[Modules]
SimulationServiceInConnector = true

[SimulationService]
; Optional simulation service configuration
MaxTransferTime = 60
TransferVersion = "SIMULATION/0.2"
```

### Network Configuration
```ini
[Network]
; Ensure simulation service ports are accessible
HttpListenerPort = 9000
PublicPort = 9000

; Configure appropriate external hostname
ExternalHostName = "my-region-server.example.com"
```

## Performance Optimization

### Transfer Efficiency
- Optimize agent data serialization for faster transfers
- Consider compression for large state transfers
- Implement connection pooling for frequent transfers

### Resource Management
- Monitor memory usage during large transfers
- Implement cleanup for failed or incomplete transfers
- Consider transfer queuing for high-load scenarios

### Network Optimization
- Ensure adequate bandwidth between regions
- Configure appropriate timeout values
- Implement retry logic for failed transfers

## Version Compatibility

This module is part of the core OpenSimulator infrastructure and maintains compatibility with:
- OpenSimulator 0.9.3.x and later
- .NET 8.0+
- Compatible with grid, standalone, and Hypergrid configurations
- Essential for multi-region and cross-grid deployments

## Agent Transfer Flow

### Typical Transfer Sequence
1. **Source Region**: Initiates transfer request
2. **Destination Region**: Receives and validates request
3. **SimulationServiceInConnectorModule**: Routes request to appropriate scene
4. **Scene Processing**: Handles agent creation and setup
5. **Confirmation**: Returns success/failure status
6. **Cleanup**: Source region completes transfer or rollback

### Error Handling
- Transfer timeout handling
- Network failure recovery
- Invalid destination handling
- Resource exhaustion management

---

*This documentation covers the SimulationServiceInConnectorModule as integrated with OptionalModulesFactory, removing dependency on Mono.Addins while maintaining full functionality for incoming simulation service requests including agent and object transfers.*