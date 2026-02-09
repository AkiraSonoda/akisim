# RemoteAvatarServicesConnector

## Overview
The RemoteAvatarServicesConnector is a shared region module that provides remote avatar services for OpenSim grid deployments. It implements the IAvatarService interface and acts as a connector to remote avatar services, typically running on a Robust services backend.

## Purpose
This connector enables regions to store and retrieve avatar appearance data from a centralized remote service rather than maintaining avatar data locally. It's essential for distributed grid architectures where multiple regions need to share consistent avatar appearance information across the grid infrastructure.

## Configuration

### Module Configuration
In your `OpenSim.ini` or configuration files, set:

```ini
[Modules]
AvatarServices = "RemoteAvatarServicesConnector"
```

### Avatar Service Configuration
The module requires an `[AvatarService]` section in your configuration:

```ini
[AvatarService]
; Configuration options for connecting to the remote avatar service
AvatarServerURI = "http://robust-server:8003"
; Additional avatar service settings as needed
```

## Architecture

### Class Structure
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.Avatar`
- **Inherits**: `AvatarServicesConnector` (base remote connector)
- **Implements**: `ISharedRegionModule`, `IAvatarService`
- **Base Type**: Shared region module

### Key Components
- **Remote Service Connection**: Connects to Robust avatar service backend
- **Region Integration**: Registers with each region's service interfaces
- **Configuration Management**: Handles connection settings and service discovery
- **Avatar Data Interface**: Provides standardized avatar appearance operations

## Functionality

### Core Avatar Methods
The connector provides the standard IAvatarService interface methods:

```csharp
public AvatarAppearance GetAppearance(UUID userID)
public bool SetAppearance(UUID userID, AvatarAppearance appearance)
public AvatarData GetAvatar(UUID userID)
public bool SetAvatar(UUID userID, AvatarData avatar)
```

### Avatar Operations
- **Appearance Management**: Store and retrieve complete avatar appearance data
- **Avatar Data Handling**: Manage avatar-specific information and metadata
- **Cross-Region Consistency**: Ensure avatar appearance is consistent across all regions
- **Appearance Caching**: Handle avatar data caching and synchronization

## Module Lifecycle

### 1. Initialization
- Checks configuration for `AvatarServices = "RemoteAvatarServicesConnector"`
- Validates `[AvatarService]` configuration section exists
- Initializes base connector with service connection details
- Enables the module if properly configured

### 2. Region Addition
- Registers `IAvatarService` interface with each region
- Provides centralized avatar data access to all regions

### 3. Service Operations
- Routes all avatar operations to the remote Robust service
- Handles network communication and error conditions
- Maintains service connection state and avatar data consistency

## Usage Scenarios

### Distributed Grid Architecture
Essential for grid deployments where multiple region servers connect to centralized Robust services for avatar appearance management.

### Multi-Region Avatar Consistency
Ensures user avatar appearances are consistent regardless of which region the user visits or where they change their appearance.

### Centralized Avatar Management
Enables grids to maintain centralized avatar appearance data rather than duplicating it across regions.

## Integration

### With ModuleFactory
The connector is loaded through the ModuleFactory system with comprehensive logging:
- Debug logging when loading the module
- Info logging when successfully loaded and ready for remote avatar data handling
- Integration with the shared module loading pipeline

### With Robust Services
- Connects to AvatarService running on Robust backend
- Uses HTTP/REST communication for avatar data operations
- Handles service discovery and connection management
- Manages avatar data synchronization and caching

### With Region Services
- Automatically registers with each region's service container
- Provides avatar services to other region components
- Integrates with avatar factory and appearance systems

## Dependencies

### Required Services
- Robust AvatarService backend
- Network connectivity to Robust services
- Base AvatarServicesConnector implementation

### Framework Dependencies
- OpenSim.Region.Framework.Interfaces
- OpenSim.Services.Interfaces
- OpenSim.Services.Connectors
- Nini configuration system
- log4net logging

## Configuration Examples

### Basic Grid Setup
```ini
[Modules]
AvatarServices = "RemoteAvatarServicesConnector"

[AvatarService]
AvatarServerURI = "http://robust-server:8003"
```

### Advanced Configuration
```ini
[Modules]
AvatarServices = "RemoteAvatarServicesConnector"

[AvatarService]
AvatarServerURI = "http://robust-server:8003"
; Connection timeout settings
ConnectionTimeout = 10000
; Caching settings
EnableCaching = true
CacheTimeout = 300
```

## Avatar Data Management

### Appearance Storage
- Complete avatar appearance data including body shape, skin, hair, eyes, clothing
- Attachment information and positioning data
- Visual parameters and morphing data
- Texture references and asset IDs

### Data Synchronization
- Real-time synchronization of appearance changes across regions
- Efficient caching to reduce network overhead
- Consistent data handling for avatar updates and modifications

## Best Practices

### Configuration
- Always ensure Robust AvatarService is running before starting regions
- Use reliable network connectivity between regions and Robust services
- Configure appropriate timeouts for avatar data operations

### Performance
- Avatar operations involve network calls - monitor latency
- Consider caching strategies for frequently accessed avatar data
- Implement proper error handling for service unavailability
- Monitor avatar service load and scaling requirements

### Data Management
- Ensure consistent avatar data formats across the grid
- Implement proper backup and recovery for avatar appearance data
- Monitor avatar data storage and cleanup processes

## Troubleshooting

### Module Not Loading
- Verify `AvatarServices = "RemoteAvatarServicesConnector"` in `[Modules]` section
- Check that the `[AvatarService]` configuration section exists
- Review startup logs for initialization errors

### Avatar Service Failures
- Verify Robust AvatarService is running and accessible
- Check network connectivity between region server and Robust services
- Validate service URL and port configuration
- Review avatar service logs on both region and Robust sides

### Appearance Issues
- Check avatar data integrity and format compatibility
- Verify texture and asset references are valid
- Monitor avatar appearance synchronization across regions
- Review avatar factory and appearance module interactions

### Connection Problems
- Check service connectivity and network configuration
- Monitor Robust service logs for backend errors
- Verify avatar service configuration on Robust side
- Review network timeouts and retry settings

### Performance Issues
- Monitor network latency between regions and avatar service
- Check Robust service performance and database connectivity
- Review avatar data caching effectiveness
- Monitor avatar service load balancing if applicable

## Related Components
- **LocalAvatarServicesConnector**: Local alternative for standalone deployments
- **Robust AvatarService**: Backend service implementation
- **AvatarFactoryModule**: Avatar appearance processing and validation
- **UserAccountServices**: User account management and integration
- **AssetService**: Avatar texture and asset management
- **AppearanceModule**: Avatar appearance handling and updates
- **AttachmentsModule**: Avatar attachment management and positioning