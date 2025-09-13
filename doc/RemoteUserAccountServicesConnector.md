# RemoteUserAccountServicesConnector

## Overview

The `RemoteUserAccountServicesConnector` is a region module that provides distributed user account services functionality for OpenSimulator grid deployments. This module enables centralized user account management across multiple regions, with intelligent caching for improved performance and seamless integration with the grid's user management infrastructure.

## Purpose

This connector enables grid-wide user account management by:
- Connecting regions to centralized user account data services
- Providing distributed user account information across multiple regions
- Supporting cross-region user account queries and lookups
- Implementing intelligent caching to improve performance and reduce network overhead
- Enabling consistent user data management throughout the grid infrastructure

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`, `IUserAccountService`
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.UserAccounts`
- **Base Class**: `UserAccountServicesConnector`
- **Dependencies**: Requires `UserAccountService` configuration section and remote service endpoints

### Key Components

#### Core Functionality
- **Remote Service Integration**: Uses `UserAccountServicesConnector` for remote service communication
- **Intelligent Caching**: Implements `UserAccountCache` for performance optimization
- **Cache Management**: Automatic cache invalidation on user login for data freshness
- **Service Interface**: Implements `IUserAccountService` for user account operations

#### Configuration Management
- Requires both `[Modules]` and `[UserAccountService]` configuration sections
- Enables when `UserAccountServices = "RemoteUserAccountServicesConnector"`
- Validates required configuration sections with comprehensive error handling

## Configuration

### Required Configuration Sections

#### Module Configuration
```ini
[Modules]
UserAccountServices = RemoteUserAccountServicesConnector
```

#### Service Configuration
```ini
[UserAccountService]
UserAccountServerURI = "http://your-grid-server:8003/"
; Additional user account service configuration parameters
```

### Complete Grid Mode Configuration Example
```ini
[Modules]
UserAccountServices = RemoteUserAccountServicesConnector

[UserAccountService]
UserAccountServerURI = "http://your-grid-server:8003/"
StorageProvider = "OpenSim.Data.MySQL.dll"
ConnectionString = "..."
```

## Implementation Details

### Module Lifecycle

1. **Initialization** (`Initialise`)
   - Validates `[Modules]` section and `UserAccountServices` configuration
   - Checks for required `[UserAccountService]` configuration section
   - Calls base class initialization for remote service setup
   - Creates and configures `UserAccountCache` instance
   - Provides comprehensive configuration validation and error logging

2. **Region Integration** (`AddRegion`)
   - Registers both `IUserAccountService` and `IUserAccountCacheModule` interfaces
   - Connects to scene events for cache management
   - Provides informational logging for region integration

3. **Region Loading** (`RegionLoaded`)
   - Completes region-specific initialization
   - Logs successful region loading

4. **Cleanup** (`RemoveRegion`)
   - Handles clean removal from regions
   - Provides debug logging for removal operations

### Service Operations

The module implements comprehensive `IUserAccountService` operations with caching:

#### GetUserAccount by UUID
```csharp
public override UserAccount GetUserAccount(UUID scopeID, UUID userID)
```
- Retrieves user account by UUID with cache optimization
- Checks cache first for performance optimization
- Falls back to remote service for cache misses
- Caches successful results for future requests
- Includes comprehensive debug logging

#### GetUserAccount by Name
```csharp
public override UserAccount GetUserAccount(UUID scopeID, string firstName, string lastName)
```
- Retrieves user account by first and last name
- Uses name-based cache lookup for performance
- Caches results using principal ID for consistency
- Provides detailed parameter and result logging

#### GetUserAccounts Batch Operation
```csharp
public override List<UserAccount> GetUserAccounts(UUID scopeID, List<string> IDs)
```
- Efficient batch retrieval of multiple user accounts
- Separates cached and non-cached requests for optimization
- Performs remote lookup only for missing accounts
- Caches newly retrieved accounts for future requests
- Provides statistics logging for cache hits and misses

#### StoreUserAccount Method
```csharp
public override bool StoreUserAccount(UserAccount data)
```
- **Not Supported**: Remote connector refuses write operations
- Returns false for all store requests
- Logs attempted operations for debugging purposes
- Write operations should be performed through appropriate administrative interfaces

### Caching System

#### UserAccountCache Integration
- **Automatic Cache Management**: Integrated `UserAccountCache` for performance optimization
- **Multi-Key Caching**: Supports caching by UUID and name combinations
- **Cache Invalidation**: Automatic cache clearing on user login for data freshness
- **Performance Optimization**: Reduces network overhead and improves response times

#### Cache Lifecycle
1. **Cache Population**: Successful remote lookups are automatically cached
2. **Cache Retrieval**: Cached data is returned immediately without network calls
3. **Cache Invalidation**: User data is cleared from cache when user logs in
4. **Cache Statistics**: Debug logging provides cache hit/miss statistics

### Event Handling

#### OnNewClient Event
- **Automatic Cache Invalidation**: Clears user data from cache when user connects
- **Data Freshness**: Ensures users see current account information (flags, titles, etc.)
- **Performance Balance**: Balances caching benefits with data accuracy requirements

### Logging and Diagnostics

The module provides extensive logging for user account operations:

- **Info Level**: Module enablement and major configuration events
- **Debug Level**: Detailed operation tracking, cache statistics, and parameter logging
- **Error Level**: Configuration validation failures and service errors
- **Cache Operations**: Comprehensive logging of cache hits, misses, and invalidations

#### Log Examples
```
Remote user account connector enabled for distributed user account services
Using UserAccountServicesConnector for remote service communication
Added to region RegionName and registered IUserAccountService interface
GetUserAccount by UUID for user uuid
GetUserAccount cache hit for user uuid
GetUserAccount successful for user uuid (FirstName LastName)
GetUserAccounts found 3 cached accounts, 2 need remote lookup
GetUserAccounts returning 5 total accounts
Cleared cache for user UserName on new client connection
```

## Integration with OptionalModulesFactory

This module has been integrated into the `OptionalModulesFactory` pattern, removing dependency on Mono.Addins:

### Factory Integration
- Loaded through `OptionalModulesFactory.CreateOptionalSharedModules()`
- Configuration-based instantiation using `UserAccountServices` setting
- Comprehensive logging for factory operations
- Maintains inheritance from `UserAccountServicesConnector`

### Migration from Mono.Addins
- Removed from `OpenSim.Region.CoreModules.addin.xml`
- Added to `OptionalModulesFactory` for dynamic loading
- Preserves full compatibility with existing configurations
- Maintains dual-configuration section requirements

## Usage Scenarios

### Grid Deployments
- **Distributed Grids**: Central user account services with multiple region servers
- **Hypergrid Configurations**: Cross-grid user account synchronization
- **Scalable Architectures**: Centralized user data with distributed region processing

### User Account Operations
- **User Authentication**: Validating user credentials during login
- **Profile Information**: Retrieving user profile data for display
- **Batch Operations**: Efficient retrieval of multiple user accounts
- **Cross-Region Consistency**: Ensuring consistent user data across regions

### Service Dependencies
- Requires functional remote user account service endpoints
- Depends on proper `[UserAccountService]` configuration
- Integrates with existing OpenSimulator authentication infrastructure

## Troubleshooting

### Common Issues

1. **Module Not Loading**
   - Verify `UserAccountServices = RemoteUserAccountServicesConnector` in `[Modules]` section
   - Ensure `[UserAccountService]` configuration section exists
   - Check log output for configuration validation messages
   - Confirm OptionalModulesFactory integration

2. **Configuration Validation Failures**
   - Verify both `[Modules]` and `[UserAccountService]` sections exist
   - Check for typos in configuration keys and values
   - Review user account service endpoint configuration
   - Enable debug logging for detailed validation information

3. **Remote Service Connection Issues**
   - Verify remote user account service endpoint configuration
   - Check network connectivity to grid services
   - Review `UserAccountServerURI` settings and accessibility
   - Monitor service availability and response times

4. **Cache-Related Issues**
   - Monitor cache hit/miss ratios in debug logs
   - Verify cache invalidation on user login events
   - Check for memory usage patterns with large user bases
   - Review cache performance impact on response times

### Debug Configuration
Enable detailed logging by setting log4net configuration:

```xml
<logger name="OpenSim.Region.CoreModules.ServiceConnectorsOut.UserAccounts">
    <level value="DEBUG" />
    <appender-ref ref="Console" />
    <appender-ref ref="LogFileAppender" />
</logger>
```

## Related Components

- **UserAccountServicesConnector**: Base class providing core user account service functionality
- **UserAccountCache**: Caching system for performance optimization
- **OptionalModulesFactory**: Factory pattern for dynamic module loading
- **IUserAccountService**: Service interface for user account operations
- **IUserAccountCacheModule**: Interface for cache management functionality
- **UserAccount**: Data structure for user account information

## Development Notes

### Code Quality
- Follows established OpenSimulator coding patterns
- Uses inheritance from `UserAccountServicesConnector` for code reuse
- Includes comprehensive error handling and logging
- Implements proper module lifecycle management

### Performance Considerations
- Intelligent caching system reduces network overhead
- Batch operations optimize multiple user account lookups
- Efficient cache invalidation balances performance with data freshness
- Minimal memory footprint with appropriate cleanup

### Maintenance
- Part of the OptionalModulesFactory modernization effort
- Removed Mono.Addins dependency for improved maintainability
- Follows consistent logging and configuration patterns
- Maintains backward compatibility with existing configurations

## Security Considerations

### Read-Only Operations
- **Write Protection**: StoreUserAccount operations are not supported
- **Data Integrity**: Prevents accidental data modification through region connectors
- **Administrative Access**: Write operations require proper administrative interfaces
- **Audit Trail**: All attempted write operations are logged

### Cache Security
- **Data Freshness**: Automatic cache invalidation ensures users see current data
- **Memory Management**: Proper cache cleanup prevents memory leaks
- **Access Control**: Cache access is controlled through proper interfaces

## Version History

- **Current**: Integrated with OptionalModulesFactory, enhanced logging, comprehensive caching, removed Mono.Addins dependency
- **Previous**: Mono.Addins-based loading with basic caching functionality

This module represents a modernized approach to user account service connectivity in OpenSimulator, providing robust distributed user account functionality with intelligent caching, improved maintainability, comprehensive operational visibility, and efficient user data management across distributed grid environments.