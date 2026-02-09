# RemoteAgentPreferencesServicesConnector

## Overview
The RemoteAgentPreferencesServicesConnector is a shared region module that provides remote agent preferences services for OpenSim grid deployments. It implements the IAgentPreferencesService interface and acts as a connector to remote agent preferences services, typically running on a Robust services backend.

## Purpose
This connector enables regions to store and retrieve user preference data from a centralized remote service rather than maintaining preferences locally. It's essential for distributed grid architectures where multiple regions need to share consistent user preference information across the grid infrastructure.

## Configuration

### Module Configuration
In your `OpenSim.ini` or configuration files, set:

```ini
[Modules]
AgentPreferencesServices = "RemoteAgentPreferencesServicesConnector"
```

### Agent Preferences Service Configuration
The module requires an `[AgentPreferencesService]` section in your configuration:

```ini
[AgentPreferencesService]
; Configuration options for connecting to the remote agent preferences service
; Typically includes service URL and authentication details
AgentPreferencesServerURI = "http://robust-server:8003"
```

## Architecture

### Class Structure
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.AgentPreferences`
- **Inherits**: `AgentPreferencesServicesConnector` (base remote connector)
- **Implements**: `ISharedRegionModule`, `IAgentPreferencesService`
- **Base Type**: Shared region module

### Key Components
- **Remote Service Connection**: Connects to Robust services backend
- **Region Integration**: Registers with each region's service interfaces
- **Configuration Management**: Handles connection settings and service discovery
- **Service Interface**: Provides standardized agent preferences operations

## Functionality

### Core Methods
The connector provides the standard IAgentPreferencesService interface methods:

```csharp
public AgentPrefs GetAgentPreferences(UUID principalID)
public bool StoreAgentPreferences(AgentPrefs data)  
public string GetLang(UUID principalID)
```

### Agent Preferences Operations
- **Preference Retrieval**: Get user preferences from remote service
- **Preference Storage**: Save user preferences to remote service
- **Language Settings**: Retrieve user's language preference
- **Cross-Region Consistency**: Ensures preferences are consistent across all regions

## Module Lifecycle

### 1. Initialization
- Checks configuration for `AgentPreferencesServices = "RemoteAgentPreferencesServicesConnector"`
- Validates `[AgentPreferencesService]` configuration section exists
- Initializes base connector with service connection details
- Enables the module if properly configured

### 2. Region Addition
- Registers `IAgentPreferencesService` interface with each region
- Provides centralized preferences access to all regions

### 3. Service Operations
- Routes all preference operations to the remote Robust service
- Handles network communication and error conditions
- Maintains service connection state

## Usage Scenarios

### Distributed Grid Architecture
Essential for grid deployments where multiple region servers connect to centralized Robust services for user data management.

### Multi-Region Consistency
Ensures user preferences (language, UI settings, etc.) are consistent regardless of which region the user visits.

### Scalable User Management
Enables grids to scale by centralizing user preference data rather than duplicating it across regions.

## Integration

### With ModuleFactory
The connector is loaded through the ModuleFactory system with comprehensive logging:
- Debug logging when loading the module
- Info logging when successfully loaded and ready for remote preferences handling
- Integration with the shared module loading pipeline

### With Robust Services
- Connects to AgentPreferencesService running on Robust backend
- Uses HTTP/REST communication for preference operations
- Handles service discovery and connection management

### With Region Services
- Automatically registers with each region's service container
- Provides agent preferences services to other region components
- Integrates with user management and avatar systems

## Dependencies

### Required Services
- Robust AgentPreferencesService backend
- Network connectivity to Robust services
- Base AgentPreferencesServicesConnector implementation

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
AgentPreferencesServices = "RemoteAgentPreferencesServicesConnector"

[AgentPreferencesService]
AgentPreferencesServerURI = "http://robust-server:8003"
```

### Advanced Configuration
```ini
[Modules]
AgentPreferencesServices = "RemoteAgentPreferencesServicesConnector"

[AgentPreferencesService]
AgentPreferencesServerURI = "http://robust-server:8003"
; Additional connection settings
ConnectionTimeout = 10000
MaxRetries = 3
```

## Best Practices

### Configuration
- Always ensure Robust AgentPreferencesService is running before starting regions
- Use reliable network connectivity between regions and Robust services
- Configure appropriate timeouts for network operations

### Security Considerations
- Secure communication channels between regions and Robust services
- Implement proper authentication for service access
- Monitor access logs for unusual preference access patterns

### Performance
- Remote operations have network latency - consider caching for frequently accessed preferences
- Monitor network connectivity and service response times
- Implement proper error handling for service unavailability

## Troubleshooting

### Module Not Loading
- Verify `AgentPreferencesServices = "RemoteAgentPreferencesServicesConnector"` in `[Modules]` section
- Check that the `[AgentPreferencesService]` configuration section exists
- Review startup logs for initialization errors

### Connection Failures
- Verify Robust AgentPreferencesService is running and accessible
- Check network connectivity between region server and Robust services
- Validate service URL configuration
- Review service logs on Robust backend

### Preference Operation Failures
- Check service connectivity and authentication
- Monitor Robust service logs for backend errors
- Verify user account exists in the grid's user service
- Review network timeouts and retry settings

### Performance Issues
- Monitor network latency between regions and services
- Check Robust service performance and database connectivity
- Consider implementing preference caching if appropriate
- Review service load and scaling requirements

## Related Components
- **LocalAgentPreferencesServicesConnector**: Local alternative for standalone deployments
- **Robust AgentPreferencesService**: Backend service implementation
- **AgentPreferencesModule**: Region-side preference handling
- **User Management**: Integration with user account and profile systems
- **Avatar Services**: User appearance and preference coordination