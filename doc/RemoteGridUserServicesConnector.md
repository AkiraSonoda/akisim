# RemoteGridUserServicesConnector

## Overview
The RemoteGridUserServicesConnector is a shared region module that provides remote grid user services for OpenSim grid deployments. It implements the IGridUserService interface and acts as a connector to remote grid user services, typically running on a Robust services backend, with intelligent caching and activity detection capabilities.

## Purpose
This connector enables regions to store and retrieve grid user information from a centralized remote service rather than maintaining user data locally. It tracks user positions, home locations, login/logout times, and other grid-specific user data across the distributed grid infrastructure with optimized caching for performance.

## Configuration

### Module Configuration
In your `OpenSim.ini` or configuration files, set:

```ini
[Modules]
GridUserServices = "RemoteGridUserServicesConnector"
```

### Grid User Service Configuration
The module requires a `[GridUserService]` section in your configuration:

```ini
[GridUserService]
; Configuration options for connecting to the remote grid user service
GridUserServerURI = "http://robust-server:8003"
; Additional grid user service settings as needed
```

## Architecture

### Class Structure
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.GridUser`
- **Implements**: `ISharedRegionModule`, `IGridUserService`, `IDisposable`
- **Base Type**: Shared region module with caching capabilities

### Key Components
- **Remote Service Connection**: Connects to Robust grid user service backend
- **Intelligent Caching**: Local cache with 30-second expiration for performance optimization
- **Activity Detection**: Monitors and reports user activity across regions
- **Region Integration**: Registers with each region's service interfaces
- **Resource Management**: Proper disposal and cleanup of cache resources

## Functionality

### Core Grid User Methods
The connector provides the standard IGridUserService interface methods:

```csharp
public GridUserInfo LoggedIn(string userID)
public bool LoggedOut(string userID, UUID sessionID, UUID region, Vector3 position, Vector3 lookat)
public bool SetHome(string userID, UUID regionID, Vector3 position, Vector3 lookAt)
public bool SetLastPosition(string userID, UUID sessionID, UUID regionID, Vector3 position, Vector3 lookAt)
public GridUserInfo GetGridUserInfo(string userID)
public GridUserInfo[] GetGridUserInfo(string[] userID)
```

### Grid User Operations
- **Position Tracking**: Monitor user locations and movements across regions
- **Home Management**: Set and retrieve user home locations
- **Session Tracking**: Track login/logout times and session data
- **Activity Detection**: Report user activity and region presence
- **Batch Operations**: Handle multiple user queries efficiently

## Caching System

### Cache Implementation
- **ExpiringCacheOS**: High-performance expiring cache with 10,000 entry limit
- **30-Second Expiration**: Balances performance with data freshness
- **Smart Updates**: Cache is updated on successful remote operations
- **Memory Management**: Automatic cleanup and disposal of cache resources

### Cache Benefits
- **Reduced Network Traffic**: Frequently accessed data served from local cache
- **Improved Performance**: Sub-millisecond access for cached grid user data
- **Load Reduction**: Minimizes load on Robust services backend
- **Consistency**: Cache invalidation ensures data accuracy

## Activity Detection

### ActivityDetector Component
- **Region Activity Monitoring**: Tracks user presence and activity in regions
- **Event-Driven Updates**: Responds to user movement and state changes
- **Position Updates**: Automatically reports position changes to remote service
- **Session Management**: Coordinates session state across region boundaries

## Module Lifecycle

### 1. Initialization
- Checks configuration for `GridUserServices = "RemoteGridUserServicesConnector"`
- Creates remote connector to Robust grid user service
- Initializes activity detector and cache systems
- Enables the module if properly configured

### 2. Region Addition
- Registers `IGridUserService` interface with each region
- Adds region to activity detector monitoring
- Provides centralized grid user data access

### 3. Service Operations
- Routes operations to remote service with caching layer
- Handles network communication and error conditions
- Maintains cache consistency and activity tracking

### 4. Cleanup
- Proper disposal of cache resources
- Activity detector cleanup for removed regions
- Resource management and garbage collection optimization

## Usage Scenarios

### Distributed Grid Architecture
Essential for grid deployments where multiple region servers need to share consistent user location and activity data through centralized Robust services.

### User Position Tracking
Enables accurate tracking of user movements across regions, supporting features like teleportation, friend location, and grid-wide user management.

### Home Location Management
Provides centralized storage and retrieval of user home locations, ensuring consistency regardless of which region the user accesses.

## Integration

### With ModuleFactory
The connector is loaded through the ModuleFactory system with comprehensive logging:
- Debug logging when loading the module
- Info logging when successfully loaded and ready for remote grid user data handling
- Integration with the shared module loading pipeline

### With Robust Services
- Connects to GridUserService running on Robust backend
- Uses HTTP/REST communication for grid user data operations
- Handles service discovery and connection management
- Manages data synchronization with intelligent caching

### With Region Services
- Automatically registers with each region's service container
- Provides grid user services to other region components
- Integrates with teleportation, user management, and presence systems

## Dependencies

### Required Services
- Robust GridUserService backend
- Network connectivity to Robust services
- Base GridUserServicesConnector implementation

### Framework Dependencies
- OpenSim.Region.Framework.Interfaces
- OpenSim.Services.Interfaces
- OpenSim.Services.Connectors
- OpenSim.Framework (ExpiringCacheOS)
- Nini configuration system
- log4net logging

## Configuration Examples

### Basic Grid Setup
```ini
[Modules]
GridUserServices = "RemoteGridUserServicesConnector"

[GridUserService]
GridUserServerURI = "http://robust-server:8003"
```

### Advanced Configuration
```ini
[Modules]
GridUserServices = "RemoteGridUserServicesConnector"

[GridUserService]
GridUserServerURI = "http://robust-server:8003"
; Connection timeout settings
ConnectionTimeout = 10000
; Service retry settings
MaxRetryAttempts = 3
```

## Performance Optimization

### Caching Strategy
- **30-Second Cache Expiration**: Balances data freshness with performance
- **10,000 Entry Limit**: Prevents memory overflow while supporting large user bases
- **Smart Cache Updates**: Updates cache on successful remote operations
- **Batch Query Support**: Efficient handling of multiple user queries

### Network Optimization
- **Connection Pooling**: Reuses HTTP connections to Robust services
- **Lazy Loading**: Data is fetched only when needed
- **Activity-Based Updates**: Position updates triggered by actual user activity
- **Error Handling**: Graceful degradation during network issues

## Best Practices

### Configuration
- Always ensure Robust GridUserService is running before starting regions
- Use reliable network connectivity between regions and Robust services
- Monitor cache hit rates and adjust expiration times if needed

### Performance
- Grid user operations involve network calls - monitor latency
- Cache provides significant performance benefits - avoid disabling
- Monitor memory usage of cache in high-traffic scenarios
- Consider regional clustering for optimal performance

### Data Management
- Ensure consistent time synchronization across grid infrastructure
- Monitor grid user data accuracy and cleanup processes
- Implement proper backup strategies for user location data

## Troubleshooting

### Module Not Loading
- Verify `GridUserServices = "RemoteGridUserServicesConnector"` in `[Modules]` section
- Check that the `[GridUserService]` configuration section exists
- Review startup logs for initialization errors

### Grid User Service Failures
- Verify Robust GridUserService is running and accessible
- Check network connectivity between region server and Robust services
- Validate service URL and port configuration
- Review grid user service logs on both region and Robust sides

### Cache Issues
- Monitor cache hit rates and performance metrics
- Check memory usage and cache entry limits
- Verify cache expiration and cleanup processes
- Review cache consistency with remote data

### Position Tracking Problems
- Verify activity detector is functioning correctly
- Check position update frequency and accuracy
- Monitor network latency for position updates
- Review session tracking and user state management

### Performance Issues
- Monitor network latency between regions and grid user service
- Check cache effectiveness and hit rates
- Review batch query performance for multiple users
- Monitor Robust service performance and database connectivity

## Related Components
- **LocalGridUserServicesConnector**: Local alternative for standalone deployments
- **Robust GridUserService**: Backend service implementation
- **ActivityDetector**: User activity monitoring component
- **PresenceService**: User presence and session management
- **UserAccountServices**: User account management and integration
- **TeleportModule**: User movement and position tracking
- **HomeModule**: Home location management and teleportation