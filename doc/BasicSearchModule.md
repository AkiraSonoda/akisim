# BasicSearchModule Technical Documentation

## Overview

The **BasicSearchModule** is a shared region module that provides fundamental search functionality for OpenSim virtual world environments. It handles viewer-based search requests for people (users) and groups, integrating with the user account service and groups service to provide comprehensive directory search capabilities. This module serves as the foundation for avatar and group discovery within virtual worlds, enabling social interaction and community building through search interfaces.

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`
- **Namespace**: `OpenSim.Region.CoreModules.Framework.Search`
- **Location**: `src/OpenSim.Region.CoreModules/Framework/Search/BasicSearchModule.cs`

### Dependencies
- **User Account Service**: IUserAccountService for people search functionality
- **Groups Service**: IGroupsModule for groups search functionality
- **Client Framework**: IClientAPI for handling viewer search requests
- **Caching Framework**: ExpiringCache for search result optimization
- **Event System**: Scene event manager for client connection handling

## Functionality

### Core Features

#### 1. People Search (Avatar Search)
- **Name-Based Search**: Searches for avatars by first name, last name, or partial matches
- **Account Integration**: Queries user account service for comprehensive user data
- **Result Caching**: Implements 30-second caching for improved performance
- **Pagination Support**: Handles result pagination with 100-entry pages
- **Scope Awareness**: Respects region scope for multi-grid environments

#### 2. Groups Search
- **Group Discovery**: Searches for groups by name or partial matches
- **Membership Filtering**: Filters out groups with zero members
- **Service Integration**: Integrates with IGroupsModule for group data
- **Result Optimization**: Caches search results to reduce service load
- **Access Control**: Respects group visibility and access permissions

#### 3. Search Result Management
- **Result Limiting**: Limits results to 101 entries per page (viewer limitation)
- **Caching Strategy**: Implements intelligent caching with 30-second expiration
- **Data Normalization**: Normalizes search queries for consistent matching
- **Error Handling**: Graceful handling of service unavailability and errors

#### 4. Client Integration
- **Viewer Protocol**: Implements standard Second Life directory search protocol
- **Event-Driven Architecture**: Uses scene events for client connection management
- **Multi-Viewer Support**: Compatible with various OpenSim-compatible viewers
- **Flag Processing**: Handles different search type flags and parameters

### Search Process Flow

#### Client Request Processing
1. **Agent Registration**: Registers search handlers when agents become root
2. **Query Reception**: Receives directory search queries from viewer clients
3. **Query Processing**: Normalizes and validates search parameters
4. **Service Integration**: Queries appropriate services based on search type
5. **Result Formatting**: Formats results for viewer consumption
6. **Response Transmission**: Sends formatted results back to requesting client

#### People Search Workflow
1. **Query Validation**: Validates search string and parameters
2. **Cache Check**: Checks local cache for recent results
3. **Service Query**: Queries user account service if cache miss
4. **Result Processing**: Processes user account data into search format
5. **Pagination**: Applies pagination based on query start parameter
6. **Cache Update**: Updates cache with fresh results
7. **Client Response**: Sends results to requesting viewer

#### Groups Search Workflow
1. **Service Validation**: Verifies groups service availability
2. **Query Processing**: Processes group search request
3. **Cache Management**: Checks and updates groups search cache
4. **Filtering**: Filters groups by membership count (> 0 members)
5. **Result Pagination**: Applies pagination for large result sets
6. **Response Generation**: Formats and sends results to client

## Configuration

### Section: [Modules]
```ini
[Modules]
    ; Enable BasicSearchModule for people and groups search
    ; Must match module name exactly for activation
    ; Default: empty (no search functionality)
    SearchModule = BasicSearchModule
```

### Factory Integration
The module is loaded through the `CoreModuleFactory` with the following behavior:
- **Configuration-Driven**: Only loads when `[Modules] SearchModule = "BasicSearchModule"`
- **Direct Instantiation**: Created directly as a CoreModule
- **Service Dependencies**: Requires user account and groups services for functionality

### Service Configuration
The module relies on properly configured underlying services:

#### User Account Service
```ini
[UserAccountService]
    LocalServiceModule = "OpenSim.Services.UserAccountService.dll:UserAccountService"
    StorageProvider = "OpenSim.Data.MySQL.dll"
    ConnectionString = "Data Source=localhost;Database=opensim;User ID=opensim;Password=***;"
```

#### Groups Service
```ini
[Groups]
    Enabled = true
    Module = "Groups Module V2"
    StorageProvider = "OpenSim.Data.MySQL.dll"
    ConnectionString = "Data Source=localhost;Database=opensim;User ID=opensim;Password=***;"
```

## Implementation Details

### Initialization Process
1. **Configuration Check**: Validates [Modules] configuration for SearchModule setting
2. **Module Activation**: Enables module only if properly configured as "BasicSearchModule"
3. **Cache Setup**: Initializes expiring caches for people and groups searches
4. **Scene Registration**: Prepares for region addition and event handling

### Client Event Management
The module uses scene events to manage client connections:

```csharp
scene.EventManager.OnMakeRootAgent += EventManager_OnMakeRootAgent;
scene.EventManager.OnMakeChildAgent += EventManager_OnMakeChildAgent;

void EventManager_OnMakeRootAgent(ScenePresence sp)
{
    sp.ControllingClient.OnDirFindQuery += OnDirFindQuery;
}

void EventManager_OnMakeChildAgent(ScenePresence sp)
{
    sp.ControllingClient.OnDirFindQuery -= OnDirFindQuery;
}
```

### Search Query Processing

#### Query Normalization
```csharp
if (!string.IsNullOrEmpty(queryText))
{
    queryText = queryText.Trim();
    queryText = queryText.ToLowerInvariant();
}
```

#### Flag-Based Routing
```csharp
if (((DirFindFlags)queryFlags & DirFindFlags.People) == DirFindFlags.People)
{
    // Handle people search
}
else if (((DirFindFlags)queryFlags & DirFindFlags.Groups) == DirFindFlags.Groups)
{
    // Handle groups search
}
```

### Caching Implementation

#### Cache Configuration
- **Expiration Time**: 30 seconds for both people and groups searches
- **Cache Type**: ExpiringCache with automatic cleanup
- **Key Strategy**: Uses normalized search text as cache key
- **Thread Safety**: Thread-safe cache operations for concurrent access

#### Cache Usage Pattern
```csharp
List<UserAccount> accounts;
if (!queryPeopleCache.TryGetValue(queryText, out accounts))
{
    accounts = m_Scenes[0].UserAccountService.GetUserAccounts(
        m_Scenes[0].RegionInfo.ScopeID, queryText);
}
queryPeopleCache.AddOrUpdate(queryText, accounts, 30.0);
```

### Result Pagination

#### Viewer Limitation Handling
```csharp
// viewers don't sent sorting, so results they show are a nice mess
if ((queryStart > 0) && (queryStart < count))
{
    int len = count - queryStart;
    if (len > 101) // a viewer page is 100
        len = 101;
    DirPeopleReplyData[] tmp = new DirPeopleReplyData[len];
    Array.Copy(hits, queryStart, tmp, 0, len);
    hits = tmp;
}
else if (count > 101)
{
    DirPeopleReplyData[] tmp = new DirPeopleReplyData[101];
    Array.Copy(hits, 0, tmp, 0, 101);
    hits = tmp;
}
```

### Data Structure Conversion

#### People Search Results
```csharp
DirPeopleReplyData d = new DirPeopleReplyData();
d.agentID = acc.PrincipalID;
d.firstName = acc.FirstName;
d.lastName = acc.LastName;
d.online = false; // Static offline status for basic search
```

#### Groups Search Results
Groups search uses existing `DirGroupsReplyData` from the groups service, with filtering:
```csharp
foreach(DirGroupsReplyData dgrd in answer)
{
    if(dgrd.members > 0)
        result[count++] = dgrd;
}
```

## Usage Examples

### Basic Configuration
```ini
[Modules]
SearchModule = BasicSearchModule

# User account and groups services must be properly configured
```

### Standalone Configuration
```ini
[Modules]
SearchModule = BasicSearchModule

[UserAccountService]
LocalServiceModule = "OpenSim.Services.UserAccountService.dll:UserAccountService"
StorageProvider = "OpenSim.Data.SQLite.dll"

[Groups]
Enabled = true
Module = "Groups Module V2"
StorageProvider = "OpenSim.Data.SQLite.dll"
```

### Grid Mode Configuration
```ini
[Modules]
SearchModule = BasicSearchModule

[UserAccountService]
UserAccountServerURI = "http://mygrid.example.com:8003"

[Groups]
Enabled = true
Module = "Groups Module V2"
GroupsServerURI = "http://mygrid.example.com:8003"
```

## Performance Considerations

### Search Performance
- **Caching Strategy**: 30-second cache reduces service load and improves response times
- **Result Limiting**: Hard limit of 101 results prevents excessive data transfer
- **Query Normalization**: Consistent query processing improves cache hit rates
- **Service Dependencies**: Performance depends on underlying user account and groups services

### Memory Usage
- **Cache Overhead**: Maintains in-memory caches for search results
- **Result Arrays**: Temporary arrays created for result pagination
- **Event Handlers**: Minimal memory overhead for client event subscriptions
- **Garbage Collection**: Regular cache expiration prevents memory accumulation

### Network Impact
- **Compact Results**: Search results contain essential data only
- **Pagination**: Prevents large result sets from overwhelming network
- **Cache Benefits**: Reduces repeated service queries for popular searches
- **Viewer Compatibility**: Optimized for standard viewer pagination

### Scalability Factors
- **Concurrent Searches**: Multiple simultaneous searches handled independently
- **User Base Size**: Performance scales with total number of users and groups
- **Search Complexity**: Simple name matching scales well
- **Service Performance**: Depends on backend database and service performance

## Troubleshooting

### Common Issues

#### 1. Module Not Loading
**Symptoms**: No search functionality available in viewers
**Solutions**:
- Verify `[Modules] SearchModule = "BasicSearchModule"` configuration
- Check that module is properly integrated with CoreModuleFactory
- Monitor logs for module initialization messages
- Ensure configuration section names match exactly

#### 2. People Search Not Working
**Symptoms**: People searches return no results or errors
**Solutions**:
- Verify user account service is properly configured and operational
- Check database connectivity for user account service
- Test user account service functionality independently
- Monitor logs for user account service query failures

#### 3. Groups Search Not Working
**Symptoms**: Groups searches fail or return empty results
**Solutions**:
- Verify groups service (IGroupsModule) is enabled and configured
- Check groups service configuration and database connectivity
- Ensure groups exist in the database with member counts > 0
- Monitor logs for groups service availability warnings

#### 4. Search Results Not Appearing
**Symptoms**: Searches appear to work but no results shown in viewer
**Solutions**:
- Check viewer compatibility with OpenSim search protocols
- Verify search result format and packet transmission
- Test with different viewer clients
- Monitor network connectivity between client and server

#### 5. Performance Issues
**Symptoms**: Slow search responses or timeouts
**Solutions**:
- Monitor cache hit rates and effectiveness
- Check database performance for user account and groups services
- Consider increasing cache expiration time for stable environments
- Monitor concurrent search load and service capacity

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization and configuration validation
- Client connection and event handler registration
- Search request processing and query normalization
- Cache hits/misses and service queries
- Result processing and pagination
- Response transmission and completion

### Performance Monitoring
Monitor these metrics for optimal performance:
- **Search Response Time**: Should complete within reasonable timeframes
- **Cache Hit Rate**: Higher rates indicate effective caching
- **Service Query Frequency**: Lower frequency suggests effective caching
- **Result Set Sizes**: Monitor for appropriate result limiting
- **Concurrent Search Load**: Track simultaneous search requests

### Configuration Validation
Use these steps to validate configuration:

1. **Check Module Loading**:
```bash
# Search for BasicSearchModule in logs
grep "BasicSearchModule" OpenSim.log
```

2. **Verify Service Integration**:
```bash
# Check user account service messages
grep "UserAccountService" OpenSim.log
# Check groups service messages
grep "Groups.*Service" OpenSim.log
```

3. **Monitor Search Activity**:
```bash
# Track search request processing
grep "search.*query" OpenSim.log
```

## Integration Notes

### Factory Loading
- Loaded via `CoreModuleFactory.CreateSharedModules()` as a framework module
- Configuration-driven loading based on SearchModule setting
- Direct instantiation as CoreModule component

### Service Architecture Integration
- **User Account Service**: Uses existing IUserAccountService infrastructure
- **Groups Service**: Integrates with IGroupsModule interface
- **Scene Framework**: Uses standard scene event management
- **Client Protocol**: Implements standard viewer directory search protocol

### Search Protocol Integration
- **Directory Manager**: Uses OpenMetaverse.DirectoryManager.DirFindFlags
- **Packet Handling**: Processes DirFindQuery packets from viewers
- **Response Format**: Generates DirPeopleReply and DirGroupsReply packets
- **Multi-Viewer Support**: Compatible with various OpenSim-compatible viewers

### Caching Framework Integration
- **ExpiringCache**: Uses OpenSim's built-in expiring cache system
- **Thread Safety**: Leverages thread-safe cache operations
- **Memory Management**: Automatic cleanup and expiration handling
- **Performance Optimization**: Cache-first strategy for improved response times

## Search Limitations and TODOs

### Current Limitations
- **No Online Status**: People search always returns offline status
- **Basic Sorting**: No sophisticated sorting options implemented
- **Limited Flags**: Ignores mature content flags and advanced search options
- **Simple Matching**: Uses basic name matching without advanced search operators

### Future Enhancements (TODOs in Code)
- **Advanced Sorting**: Implement proper result sorting based on relevance
- **Mature Content Filtering**: Respect mature content flags in searches
- **Online Status**: Integrate with presence service for real-time status
- **Advanced Search**: Support complex search operators and filters

### Performance Optimizations
- **Database Indexing**: Ensure proper database indexes for search performance
- **Result Caching**: Consider longer cache times for stable environments
- **Service Optimization**: Optimize underlying user account and groups services
- **Batch Operations**: Consider batch processing for large result sets

## Security Considerations

### Privacy Protection
- **Information Disclosure**: Search results reveal user and group names
- **Scope Isolation**: Properly isolates searches by grid scope
- **Access Control**: Respects service-level access restrictions
- **Data Minimization**: Returns only essential information in search results

### Service Protection
- **Result Limiting**: Prevents overwhelming services with unlimited queries
- **Cache Protection**: Reduces load on backend services through caching
- **Input Validation**: Validates and normalizes search input
- **Error Handling**: Prevents service errors from revealing sensitive information

### Search Security
- **Scope Validation**: Ensures searches respect grid boundaries
- **Service Authentication**: Relies on service-level security measures
- **Query Sanitization**: Normalizes queries to prevent injection attempts
- **Rate Limiting**: Cache-based implicit rate limiting for repeated searches

## See Also
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [User Account Services](../docs/UserAccountServices.md) - User account service architecture
- [Groups Services](../docs/GroupsServices.md) - Groups service configuration and integration
- [Search Architecture](../docs/SearchArchitecture.md) - Overall search system design
