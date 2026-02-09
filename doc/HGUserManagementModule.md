# HGUserManagementModule

## Overview

The **HGUserManagementModule** is the hypergrid-enabled version of OpenSimulator's user management system. It extends the base UserManagementModule to provide seamless cross-grid user identity management, enabling users from different grids to interact naturally within the same virtual environment.

## Location

- **File**: `src/OpenSim.Region.CoreModules/Framework/UserManagement/HGUserManagementModule.cs`
- **Namespace**: `OpenSim.Region.CoreModules.Framework.UserManagement`
- **Assembly**: `OpenSim.Region.CoreModules.dll`

## Module Type

- **Interface**: `ISharedRegionModule`, `IUserManagement`
- **Scope**: Shared across all regions in an OpenSim instance
- **Loading**: Loaded via ModuleFactory (no Mono.Addins dependency)
- **Dependencies**: UserManagementModule (base class), UserAgentService, GatekeeperService
- **Inherits**: All functionality from `UserManagementModule`

## Key Features

### Cross-Grid User Management
- **Foreign User Integration** - Seamless handling of users from external grids
- **Profile Federation** - Dynamic loading of user profiles from their home grids
- **Name Resolution** - Consistent user naming across different grid environments
- **Presence Synchronization** - Cross-grid user presence and status tracking

### Hypergrid Protocol Support
- **HG Profile URIs** - Support for hypergrid user profile addressing
- **Grid Detection** - Automatic identification of user's home grid
- **Service Discovery** - Dynamic discovery of remote grid services
- **Authentication Bridge** - Secure cross-grid user authentication

### Performance Optimization
- **Enhanced Caching** - Specialized caching for foreign user data
- **Lazy Loading** - Profile information loaded only when needed
- **Background Updates** - Non-blocking profile synchronization
- **Error Resilience** - Graceful handling of remote grid unavailability

## Configuration

### Module Selection

Enable the HG UserManagement module in your configuration:

```ini
[Modules]
UserManagementModule = "HGUserManagementModule"
```

### Required Hypergrid Configuration

```ini
[Hypergrid]
; Enable hypergrid functionality
Hypergrid = true

; Your grid's public URI
HomeURI = "http://yourgrid.com:8002/"
GatekeeperURI = "http://yourgrid.com:8002/"

[HGUserManagementModule]
; Profile service configuration
ProfileService = "remote"
ProfileServerURI = "http://yourgrid.com:8002"

; Foreign user cache settings
ForeignUserCacheTimeout = 3600
BadForeignUserCacheTimeout = 600

; Enable/disable home URI display changes
DisplayChangingHomeURI = true
```

### Service Dependencies

```ini
[UserAgentService]
; Required for HG user verification
LocalServiceModule = "OpenSim.Services.HypergridService.dll:UserAgentService"

[GatekeeperService] 
; Required for HG authentication
LocalServiceModule = "OpenSim.Services.HypergridService.dll:GatekeeperService"
```

## Enhanced Functionality

### Foreign User Handling

#### User Profile Federation
- **Dynamic Profile Loading** - Fetches user profiles from their home grid on-demand
- **Profile Caching** - Intelligent caching with separate timeouts for foreign users
- **Fallback Mechanisms** - Graceful degradation when remote services are unavailable
- **Profile Validation** - Verification of foreign user profile authenticity

#### Cross-Grid Name Resolution
```csharp
// Examples of cross-grid user identification
UUID foreignUser = GetUserIdByName("John", "Doe@foreigngrid.com");
string userName = GetUserName(foreignUserUUID); // Returns "John Doe @foreigngrid.com"
```

### Enhanced Caching Strategy

| Cache Type | HG Timeout | Purpose |
|------------|------------|---------|
| **Local Users** | 3600s | Same as base module |
| **HG Users (Valid)** | 3600s | Confirmed foreign users |
| **HG Users (Bad)** | 600s | Failed foreign lookups |
| **Profile URIs** | 7200s | Foreign grid service endpoints |
| **Bad URIs** | 120s | Failed service connections |

### Service Integration

#### UserAgent Service Integration
- **User Verification** - Validates foreign user credentials
- **Presence Tracking** - Cross-grid user location and status
- **Session Management** - Handles foreign user session lifecycle

#### Gatekeeper Service Integration  
- **Authentication** - Secure cross-grid user authentication
- **Authorization** - Foreign user access permission checks
- **Grid Handshakes** - Establishes trust relationships between grids

## API Extensions

### HG-Specific Methods

#### Foreign User Management
```csharp
// Check if user is from foreign grid
bool IsForeignUser(UUID userID)

// Get user's home grid URI
string GetUserHomeURI(UUID userID)

// Validate foreign user profile
bool ValidateForeignUser(UUID userID, string homeURI)
```

#### Profile Federation
```csharp
// Load foreign user profile
UserData LoadForeignUserProfile(UUID userID, string homeURI)

// Update foreign user cache
void UpdateForeignUserCache(UUID userID, UserData userData)

// Clear stale foreign user data
void ClearStaleForeignUsers()
```

## Cross-Grid User Experience

### User Identification
- **Grid Suffix Display** - Foreign users shown as "Name @grid.com"
- **Profile Integration** - Seamless profile viewing across grids
- **Consistent Identity** - Maintains user identity across grid boundaries

### Name Resolution
- **Automatic Detection** - Identifies foreign vs local users automatically
- **URI Parsing** - Extracts grid information from user URIs
- **Name Caching** - Efficient storage of cross-grid name mappings

### Profile Display
- **Dynamic Loading** - Profiles loaded from user's home grid
- **Fallback Display** - Shows cached information if home grid unavailable  
- **Update Notifications** - Alerts when foreign profiles are refreshed

## Performance Considerations

### Network Optimization
- **Connection Pooling** - Reuses connections to foreign grids
- **Batch Operations** - Combines multiple foreign user lookups
- **Timeout Management** - Prevents hanging on unresponsive grids
- **Circuit Breakers** - Temporarily disables failed foreign services

### Memory Management
- **Separate Cache Pools** - Different cache strategies for local vs foreign data
- **Garbage Collection** - Automatic cleanup of expired foreign user data
- **Memory Limits** - Configurable bounds on foreign user cache size

### Scaling Considerations
- **Foreign Grid Limits** - Manages load on remote services
- **Local Resource Protection** - Prevents foreign operations from overwhelming local services
- **Concurrent Access** - Thread-safe operations for multiple foreign grid connections

## Troubleshooting

### Common Issues

#### Foreign User Problems
1. **Names not resolving**: Check hypergrid connectivity and HomeURI configuration
2. **Profile loading fails**: Verify remote grid availability and ProfileServerURI
3. **Authentication errors**: Validate UserAgent and Gatekeeper service configuration

#### Performance Issues
1. **Slow foreign lookups**: Adjust cache timeouts and connection limits
2. **Memory growth**: Monitor foreign user cache size and cleanup frequency
3. **Grid connectivity**: Check network connectivity to foreign grids

#### Configuration Errors
1. **HG module not loading**: Verify `UserManagementModule = "HGUserManagementModule"`
2. **Service dependencies**: Ensure UserAgent and Gatekeeper services are available
3. **URI configuration**: Validate HomeURI and GatekeeperURI settings

### Debug Commands
```
# Show HG user cache status
show hg users

# Test foreign grid connectivity
test hg connection <grid-uri>

# Clear foreign user cache
clear hg cache

# Display HG module configuration
show hg config
```

### Logging Configuration
```xml
<logger name="OpenSim.Region.CoreModules.Framework.UserManagement.HGUserManagementModule">
    <level value="DEBUG" />
</logger>
```

## Security Considerations

### Trust Management
- **Grid Verification** - Validates foreign grid authenticity
- **User Authentication** - Verifies foreign user credentials
- **Permission Checking** - Enforces access controls for foreign users

### Privacy Protection
- **Profile Filtering** - Controls what information is shared with foreign grids
- **Data Minimization** - Only shares necessary user information
- **Audit Logging** - Tracks cross-grid user interactions

## Migration and Upgrade

### From Basic UserManagement
1. **Update Configuration** - Change `UserManagementModule = "HGUserManagementModule"`
2. **Add HG Services** - Configure UserAgent and Gatekeeper services
3. **Set URIs** - Configure HomeURI and GatekeeperURI
4. **Test Connectivity** - Verify hypergrid functionality

### Compatibility Notes
- **Backward Compatible** - All base UserManagement functionality preserved
- **Configuration Additive** - New HG settings can be added incrementally
- **API Compatible** - No breaking changes to existing user management APIs

## Related Documentation
- [UserManagementModule](UserManagementModule.md) - Base user management functionality
- [Hypergrid Configuration](HypergridConfiguration.md) - Complete hypergrid setup guide
- [UserAgentService](UserAgentService.md) - HG user authentication service
- [GatekeeperService](GatekeeperService.md) - HG grid authentication service