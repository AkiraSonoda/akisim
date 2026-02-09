# LocalAuthorizationServicesConnector

## Overview
The LocalAuthorizationServicesConnector is a region module that provides local authorization services for controlling access to regions within an OpenSim grid. It implements the IAuthorizationService interface and operates as a non-shared region module, meaning each region has its own instance.

## Purpose
This connector handles authorization decisions for users attempting to access regions, allowing grid administrators to implement custom access control policies at the region level. It works locally within each region simulator rather than connecting to a remote authorization service.

## Configuration

### Module Configuration
In your `OpenSim.ini` or configuration files, set:

```ini
[Modules]
AuthorizationServices = "LocalAuthorizationServicesConnector"
```

### Authorization Service Configuration
The module requires an `[AuthorizationService]` section in your configuration:

```ini
[AuthorizationService]
; Configuration options for the authorization service
; (specific options depend on the underlying AuthorizationService implementation)
```

## Architecture

### Class Structure
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.Authorization`
- **Implements**: `INonSharedRegionModule`, `IAuthorizationService`
- **Base Type**: Non-shared region module

### Key Components
- **Authorization Service**: Handles the actual authorization logic
- **Region Integration**: Registers with each region's service interfaces
- **Configuration Management**: Reads authorization settings from config files

## Functionality

### Core Method
```csharp
public bool IsAuthorizedForRegion(
    string userID, 
    string firstName, 
    string lastName, 
    string regionID, 
    out string message)
```

This method determines whether a user is authorized to access a specific region.

### Parameters
- `userID`: UUID of the user requesting access
- `firstName`: User's first name
- `lastName`: User's last name  
- `regionID`: UUID of the region being accessed
- `message`: Output parameter for authorization messages/errors

### Return Value
- `true`: User is authorized to access the region
- `false`: User is denied access to the region

## Module Lifecycle

### 1. Initialization
- Checks configuration for `AuthorizationServices = "LocalAuthorizationServicesConnector"`
- Loads `[AuthorizationService]` configuration section
- Enables the module if properly configured

### 2. Region Addition
- Registers `IAuthorizationService` interface with the region
- Stores reference to the region scene

### 3. Region Loading
- Creates underlying `AuthorizationService` instance
- Passes configuration and scene to the service
- Logs successful initialization

## Usage Scenarios

### Local Grid Authorization
Ideal for standalone or small grid setups where authorization decisions should be made locally within each region simulator.

### Custom Access Control
Enables implementation of region-specific access control policies without requiring external authorization services.

### Development and Testing
Useful for development environments where local authorization is preferred over remote service dependencies.

## Integration

### With ModuleFactory
The connector is loaded through the ModuleFactory system with appropriate logging:
- Debug logging when loading the module
- Info logging when successfully loaded
- Integration with the shared module loading pipeline

### With Region Services
- Automatically registers with each region's service container
- Provides authorization services to other region components
- Integrates with the region's user access control systems

## Dependencies

### Required Services
- Base AuthorizationService implementation
- Region scene management
- Configuration system

### Framework Dependencies
- OpenSim.Region.Framework.Interfaces
- OpenSim.Services.Interfaces
- Nini configuration system
- log4net logging

## Best Practices

### Configuration
- Always configure the `[AuthorizationService]` section when using this connector
- Test authorization rules in development before deploying to production
- Monitor authorization logs for security-related events

### Security Considerations
- Authorization decisions are made locally within each region
- Ensure proper configuration to prevent unauthorized access
- Consider audit logging for authorization decisions

### Performance
- Local authorization provides low-latency access decisions
- Each region maintains its own authorization context
- Suitable for high-performance scenarios requiring fast authorization

## Troubleshooting

### Module Not Loading
- Verify `AuthorizationServices = "LocalAuthorizationServicesConnector"` in `[Modules]` section
- Check that the `[AuthorizationService]` configuration section exists
- Review startup logs for initialization errors

### Authorization Failures
- Check authorization service configuration
- Verify user credentials and region IDs
- Review authorization logs for detailed error messages

### Performance Issues
- Monitor region startup times if authorization rules are complex
- Consider caching strategies for frequently accessed authorization data
- Review log verbosity settings to avoid excessive logging overhead

## Related Components
- **RemoteAuthorizationServicesConnector**: Remote alternative for distributed grids
- **AuthorizationService**: Underlying service implementation
- **Region Scene Management**: Integration point for region-level services
- **User Management**: Works with user account services for access control