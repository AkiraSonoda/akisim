# AvatarPickerSearchModule

## Overview

The AvatarPickerSearchModule is a shared region module that provides avatar search functionality for Second Life-compatible viewers in OpenSimulator. It implements the AvatarPickerSearch capability, enabling users to search for other avatars by name through the viewer's avatar picker interface (used in features like friend requests, group invitations, and direct messaging).

## Architecture

- **Type**: `ISharedRegionModule` - instantiated once per OpenSim instance and shared across all regions
- **Namespace**: `OpenSim.Region.ClientStack.Linden`
- **Location**: `src/OpenSim.Region.ClientStack.LindenCaps/AvatarPickerSearchModule.cs`

## Key Features

### Avatar Search Capability
- **HTTP Capability**: Registers "AvatarPickerSearch" capability for connected clients
- **Name-based Search**: Searches for avatars by first and last name patterns
- **Pagination Support**: Handles paged search results with configurable page sizes
- **LLSD Response Format**: Returns results in Second Life-compatible LLSD format

### Flexible Deployment Modes
- **Local Mode**: When `Cap_AvatarPickerSearch = "localhost"`, processes requests locally
- **External Mode**: When configured with external URL, delegates to external search service
- **IPeople Integration**: Uses IPeople service interface for user data retrieval

### Search Result Processing
- Converts internal UserData format to LLSD avatar picker format
- Handles legacy name format and modern username generation
- Supports display names and username conventions
- Returns paginated results with next page URLs

## Configuration

### Enabling the Module
The module is configured through the `[ClientStack.LindenCaps]` configuration section:

```ini
[ClientStack.LindenCaps]
; Avatar picker search capability
; Set to "localhost" for local processing, or external URL for remote service
; Leave empty or comment out to disable
Cap_AvatarPickerSearch = "localhost"

; Alternative: Use external service
; Cap_AvatarPickerSearch = "http://search-service.example.com/avatarpicker"
```

### Configuration Options

#### Local Processing (`localhost`)
```ini
Cap_AvatarPickerSearch = "localhost"
```
- Processes searches using local IPeople service
- Requires IPeople implementation to be available
- Searches against local user database/service

#### External Service
```ini
Cap_AvatarPickerSearch = "http://external-search.example.com/search"
```
- Delegates search requests to external HTTP service
- Useful for grid-wide search across multiple simulators
- External service must implement compatible API

#### Disabled (Default)
```ini
; Cap_AvatarPickerSearch = 
```
- Module remains inactive when no URL is configured
- Avatar search functionality unavailable in viewers

## HTTP API Reference

### Request Format
**Method**: `GET`
**URL**: Capability URL provided to viewer
**Query Parameters**:
- `names` (required): Search query string (minimum 3 characters)
- `page_size` (optional): Number of results per page (default: 500)
- `page` (optional): Page number (default: 1)

**Example Request**:
```
GET /capability-uuid?names=john%20doe&page_size=20&page=1
```

### Response Format
**Content-Type**: `application/llsd+xml`
**Response Structure** (LLSD):
```xml
<llsd>
  <map>
    <key>agents</key>
    <array>
      <map>
        <key>id</key>
        <uuid>user-uuid</uuid>
        <key>legacy_first_name</key>
        <string>John</string>
        <key>legacy_last_name</key>
        <string>Doe</string>
        <key>display_name</key>
        <string>John Doe</string>
        <key>username</key>
        <string>john.doe</string>
        <key>is_display_name_default</key>
        <boolean>0</boolean>
      </map>
    </array>
    <key>next_page_url</key>
    <string>capability-url-for-next-page</string>
  </map>
</llsd>
```

### Error Responses
- **400 Bad Request**: Invalid or missing search parameters
- **404 Not Found**: Invalid HTTP method (only GET supported)

## Module Lifecycle

### Initialization
1. **Initialise()** - Reads configuration and determines enable state
2. **AddRegion()** - Basic region setup (if enabled)
3. **RegionLoaded()** - Obtains IPeople service and registers capability events
4. **PostInitialise()** - No-op
5. **Close()** - Cleanup (currently no-op)

### Dynamic Behavior
- Module automatically enables/disables based on configuration
- Capability registration occurs per agent connection
- IPeople service obtained from first loaded region

## Technical Implementation

### Search Processing Workflow
1. **Request Validation**: Validates HTTP method and query parameters
2. **Parameter Extraction**: Extracts search terms and pagination parameters
3. **Service Delegation**: Calls IPeople.GetUserData() with search criteria
4. **Data Conversion**: Converts UserData objects to LLSD format
5. **Response Generation**: Serializes LLSD response with pagination info

### Username Generation Logic
The module implements Second Life username conventions:
```csharp
// For users with @ in last name (modern usernames)
if (user.LastName.StartsWith("@"))
    p.username = user.FirstName.ToLower() + user.LastName.ToLower();
else
    // Legacy format with dot separator
    p.username = user.FirstName.ToLower() + "." + user.LastName.ToLower();
```

### Data Structures
- **UserData**: Input format from IPeople service
- **LLSDPerson**: Output format for avatar picker
- **LLSDAvatarPicker**: Container for search results

### Service Dependencies
- **IPeople**: Core service for user data retrieval and search
- **Scene**: Region context and service access
- **Caps**: Capability registration and HTTP handling

## Search Behavior

### Search Query Processing
- **Minimum Length**: Requires at least 3 characters
- **Search Scope**: Depends on IPeople implementation (local users, grid-wide, etc.)
- **Result Ordering**: Determined by IPeople service implementation
- **Case Sensitivity**: Handled by underlying IPeople service

### Pagination Support
- **Default Page Size**: 500 results
- **Configurable**: Clients can request different page sizes
- **Next Page URLs**: Automatically generated for continued browsing
- **Page Numbering**: 1-based page indexing

### Performance Considerations
- Search processing scales with IPeople implementation
- LLSD serialization overhead for large result sets
- HTTP capability per-agent overhead
- Database/service query performance dependency

## Integration Points

### IPeople Service Interface
```csharp
List<UserData> users = m_People.GetUserData(names, page_size, page_number);
```
The module depends on IPeople implementations such as:
- Local user service for standalone mode
- Grid user service for grid mode
- Custom people modules for specialized search

### Viewer Integration
- **Second Life Viewer**: Primary target client
- **OpenMetaverse-based Viewers**: Compatible with LLSD format
- **Avatar Picker Dialog**: Triggered by friend requests, groups, IM, etc.
- **Search Results Display**: Shows avatar names and allows selection

### Capability System
- Registered per-agent on login
- Unique capability URL per user session
- Integrated with OpenSim capability framework
- Supports both local and external URL delegation

## Logging

The module provides comprehensive debug logging for troubleshooting:

### Initialization Logging
- Module enable/disable status with configuration details
- IPeople service availability detection
- Region addition and loading events

### Runtime Logging
- Capability registration per agent with mode (local/external)
- Search request processing with query details
- Search result counts and performance information

**Log Category**: `OpenSim.Region.ClientStack.Linden.AvatarPickerSearchModule`

**Example Log Output**:
```
[DEBUG] AvatarPickerSearchModule initializing
[DEBUG] AvatarPickerSearchModule enabled with URL: localhost
[DEBUG] AvatarPickerSearchModule adding region MainRegion
[DEBUG] AvatarPickerSearchModule region loaded MainRegion
[DEBUG] AvatarPickerSearchModule found IPeople interface: True
[DEBUG] AvatarPickerSearchModule registering local capability for agent xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
[DEBUG] AvatarPickerSearchModule processing search request for: john doe
[DEBUG] AvatarPickerSearchModule found 3 users for query 'john doe'
```

## Security Considerations

### Access Control
- **Authenticated Access**: Capability URLs provide session-based security
- **Per-Agent URLs**: Unique capability per connected user
- **Query Length Validation**: Prevents very short queries that might be expensive

### Privacy Implications
- **Search Visibility**: Users searchable through this interface
- **Information Exposure**: Exposes usernames and UUIDs in search results
- **Grid-wide Scope**: May expose users across entire grid depending on IPeople implementation

### Rate Limiting
- No built-in rate limiting (depends on HTTP server and IPeople service)
- Consider implementing rate limiting for production deployments
- Monitor search query patterns for abuse

## Troubleshooting

### Common Issues

#### Module Not Working
**Symptoms**: Avatar search returns no results or fails
**Check**:
1. `Cap_AvatarPickerSearch` configuration present and valid
2. IPeople service available and functional
3. Module initialization logs show successful setup

#### Search Returns Empty Results
**Symptoms**: Searches complete but return no avatars
**Check**:
1. IPeople service implementation and data availability
2. Search query length (minimum 3 characters)
3. Database connectivity and user data presence

#### External Service Configuration
**Symptoms**: External search service not responding
**Check**:
1. External URL accessibility from simulator
2. External service API compatibility
3. Network connectivity and firewall settings

### Diagnostic Commands
Use standard OpenSim logging and module debugging:
```ini
; Enable debug logging
[Logging]
LogLevel = DEBUG
```

## Module Loading

The AvatarPickerSearchModule is located in the ClientStack.LindenCaps project and is loaded through the standard OpenSimulator module discovery mechanism. Since it implements `ISharedRegionModule`, it will be automatically discovered and loaded by the module loading system without requiring factory registration.

The module is part of the core client protocol implementation and is essential for proper avatar search functionality in Second Life-compatible viewers.

## Performance Optimization

### Caching Considerations
- No built-in result caching (stateless per request)
- Consider caching in IPeople implementation for better performance
- Capability URLs can be cached by clients between sessions

### Database Optimization
- Ensure proper indexing on user name fields in IPeople service
- Consider search performance impact of large user databases
- Monitor query performance and optimize IPeople implementation

### Network Optimization
- LLSD serialization is compact but not the most efficient
- Consider result set size limits for very large user bases
- Monitor capability request patterns and response sizes

## Future Enhancements

### Potential Improvements
- Result caching for improved performance
- Advanced search operators (exact match, wildcards)
- Search result ranking and relevance scoring
- Integration with presence information

### Extended Features
- Group-based search filtering
- Region-specific search scoping
- Search analytics and logging
- Custom search field support

## Maintenance Notes

### Module Dependencies
- No Mono.Addins dependencies (modernized architecture)
- Depends on OpenSim core capability system and IPeople service
- Compatible with both Standalone and Grid deployment modes
- Requires proper IPeople service implementation for functionality

### Testing Considerations
- Test with various IPeople implementations
- Verify search functionality across different viewer clients
- Test pagination with large result sets
- Validate LLSD format compatibility
- Test both local and external service configurations

This module is essential for avatar search functionality in OpenSimulator deployments, providing the bridge between Second Life viewers and the underlying user search services.