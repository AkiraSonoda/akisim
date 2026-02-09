# UserManagement Module

## Overview

The **UserManagement Module** is a core OpenSimulator module responsible for managing user identity, profile information, and name resolution within the virtual world. It provides services for translating between user UUIDs and human-readable names, managing user presence information, and handling user account lookups across both local and remote grids.

## Location

- **File**: `src/OpenSim.Region.CoreModules/Framework/UserManagement/UserManagementModule.cs`
- **Namespace**: `OpenSim.Region.CoreModules.Framework.UserManagement`
- **Assembly**: `OpenSim.Region.CoreModules.dll`

## Module Variants

### 1. UserManagementModule (Basic)
- **Class**: `UserManagementModule`
- **Module Name**: `BasicUserManagementModule`
- **Use Case**: Standard grid operations, single-grid environments
- **Features**: Local user management, basic name resolution, user caching

### 2. HGUserManagementModule (Hypergrid)
- **Class**: `HGUserManagementModule`  
- **Module Name**: `HGUserManagementModule`
- **Use Case**: Hypergrid-enabled environments
- **Features**: Cross-grid user management, foreign user handling, HG profile integration
- **Inherits**: `UserManagementModule` (extends base functionality)

## Module Type

- **Interface**: `ISharedRegionModule`, `IUserManagement`, `IPeople`
- **Scope**: Shared across all regions in an OpenSim instance
- **Loading**: Loaded via ModuleFactory (no Mono.Addins dependency)
- **Dependencies**: IUserAccountService, IGridUserService, IServiceThrottleModule

## Core Functionality

### User Identity Management
- **UUID ↔ Name Resolution** - Bidirectional mapping between user UUIDs and display names
- **Profile Information** - Management of user profile data and metadata
- **User Presence** - Tracking user online/offline status and location information
- **Name Caching** - Efficient caching system with configurable expiration times

### Cross-Grid Support (HG Module)
- **Foreign User Handling** - Management of users from external grids
- **Profile URI Resolution** - Dynamic loading of user profiles from remote grids
- **Hypergrid Integration** - Seamless user experience across multiple connected grids

### Performance Features
- **Intelligent Caching** - Multi-tier caching with different expiration policies
- **Service Throttling** - Protection against service overload
- **Batch Operations** - Efficient bulk user name lookups
- **Background Updates** - Non-blocking profile information updates

## Configuration

### Module Selection

The UserManagement module is automatically loaded by ModuleFactory. You can choose between the basic and hypergrid versions:

```ini
[Modules]
; For basic single-grid environments
; UserManagementModule = ""  ; Uses basic UserManagementModule (default)

; For hypergrid-enabled environments  
UserManagementModule = "HGUserManagementModule"
```

### Configuration Parameters

```ini
[UserManagement]
; Display home URI changes in user profiles
DisplayChangingHomeURI = false

; Cache expiration times (in seconds)
UserCacheTimeout = 3600

; Service throttling settings
MaxConcurrentRequests = 50
```

### Hypergrid Configuration (HG Module Only)

```ini
[HGUserManagementModule]
; Profile service settings
ProfileService = "local"  ; or "remote"
ProfileServerURI = "http://yourgrid.com:8002"

; Foreign user cache settings
ForeignUserCacheTimeout = 3600
BadUserCacheTimeout = 600
```

## API Reference

### Core User Management (IUserManagement)

#### Name Resolution
```csharp
// Get user UUID by first/last name
UUID GetUserIdByName(string firstName, string lastName)

// Get display name by UUID
string GetUserName(UUID uuid)

// Batch name lookup
Dictionary<UUID, string> GetUsersNames(string[] ids, UUID scopeID)
```

#### Profile Management
```csharp
// Get user profile information
UserData GetUser(UUID uuid)

// Update user profile
void UpdateUserProfile(UUID uuid, UserData userData)

// Check if user exists
bool IsUser(UUID uuid)
```

### People Service (IPeople)

#### User Discovery
```csharp
// Search for users by name pattern
UserData[] GetUserData(string query, int start, int count)

// Get detailed user information
UserData GetUserData(UUID uuid)
```

## Caching Strategy

### Cache Types and Expiration

| Cache Type | Default Timeout | Purpose |
|------------|----------------|---------|
| **Local Users** | 3600 seconds | Grid-local user information |
| **HG Users** | 3600 seconds | Foreign/hypergrid user data |
| **Bad URLs** | 600 seconds | Failed profile lookups (prevents retry spam) |
| **No Expire** | Permanent | Confirmed invalid entries |

### Cache Management
- **Memory Efficient** - Uses ExpiringCacheOS with automatic cleanup
- **Hit Rate Optimization** - Prioritizes frequently accessed users
- **Background Refresh** - Updates stale entries without blocking requests

## Performance Characteristics

### Scalability
- **Concurrent Safe** - Thread-safe operations with RwLockedList collections
- **Service Throttling** - Protects against overload with configurable limits
- **Batch Processing** - Efficient bulk operations for large user sets

### Network Efficiency
- **Lazy Loading** - Profile information loaded on-demand
- **Smart Caching** - Reduces redundant network requests
- **Background Updates** - Non-blocking profile synchronization

## Integration Points

### Core Services
- **UserAccountService** - Account management and authentication
- **GridUserService** - User presence and location tracking
- **ServiceThrottleModule** - Request rate limiting and protection

### Client Integration
- **Profile Viewing** - Seamless profile display in viewers
- **Name Display** - Real-time name resolution in chat and UI
- **User Search** - Integrated user discovery functionality

### Module Interactions
- **Friends Module** - Friend list management and presence
- **Groups Module** - Group membership and notifications
- **IM Module** - Instant messaging routing and delivery

## Hypergrid Features (HG Module)

### Cross-Grid Operations
- **Foreign User Support** - Transparent handling of external grid users
- **Profile Federation** - Dynamic loading from remote user services
- **Name Disambiguation** - Handling of name conflicts across grids

### URI Management
- **Home URI Tracking** - User's originating grid information
- **Profile URI Resolution** - Dynamic profile service discovery
- **Grid Detection** - Automatic identification of user's home grid

## Debugging and Monitoring

### Console Commands
```
# Show cached user information
show users

# Clear user cache
clear user cache

# Display module status
show modules UserManagement
```

### Debug Logging
Enable debug logging in OpenSim.exe.config:
```xml
<logger name="OpenSim.Region.CoreModules.Framework.UserManagement">
    <level value="DEBUG" />
</logger>
```

### Performance Metrics
- **Cache Hit Rates** - Monitor cache effectiveness
- **Lookup Times** - Profile resolution performance
- **Service Calls** - External service dependency tracking

## Common Issues

### Name Resolution Problems
1. **Names not displaying**: Check UserAccountService connectivity
2. **Slow profile loading**: Verify cache timeout settings
3. **Foreign user issues**: Ensure HG module is enabled for cross-grid environments

### Performance Issues
1. **High memory usage**: Reduce cache timeout values
2. **Slow user searches**: Enable service throttling
3. **Network timeouts**: Configure appropriate service endpoints

### Configuration Errors
1. **Module not loading**: Verify ModuleFactory integration
2. **Service dependencies**: Check required service availability
3. **HG connectivity**: Validate hypergrid configuration

## Migration Notes

### From Legacy Module System
- **Mono.Addins removed** - Now uses ModuleFactory loading
- **Configuration unchanged** - Existing configs continue to work
- **API compatibility** - No breaking changes to existing functionality

### Upgrade Considerations
- **Cache migration** - Existing cache data automatically invalidated
- **Service dependencies** - Verify all dependent services are available
- **HG environments** - Ensure HGUserManagementModule is selected

## Related Documentation
- [HGUserManagementModule](HGUserManagementModule.md) - Hypergrid-specific features
- [UserAccountService](UserAccountService.md) - Account management backend
- [GridUserService](GridUserService.md) - User presence tracking