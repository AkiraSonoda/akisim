# EstateAccessCapModule

## Overview

The `EstateAccessCapModule` is a region capability module that provides HTTP-based API access for retrieving estate access control information in OpenSim. It implements the `INonSharedRegionModule` interface and serves estate access lists (managers, allowed agents, allowed groups, and banned agents) through the Second Life/OpenSimulator capabilities system.

## Location

**File:** `src/OpenSim.Region.ClientStack.LindenCaps/EstateAccess.cs`  
**Namespace:** `OpenSim.Region.ClientStack.Linden`

## Dependencies

This module does **not** use Mono.Addins and implements the region module interface directly.

### Key Dependencies
- `OpenSim.Region.Framework.Interfaces.INonSharedRegionModule`
- `OpenSim.Region.Framework.Interfaces.IEstateModule`
- `OpenSim.Framework.Capabilities.Caps`
- `OpenMetaverse.StructuredData` (LLSD encoding)

## Configuration

The module is configured through the `[ClientStack.LindenCaps]` section in OpenSim configuration:

```ini
[ClientStack.LindenCaps]
Cap_EstateAccess = localhost
```

The module is only enabled when `Cap_EstateAccess` is set to `"localhost"`.

## Functionality

### Core Features
- Provides HTTP capability endpoint for estate access information retrieval
- Returns complete estate access control lists in LLSD format
- Validates user permissions before serving data
- Integrates with the estate module system
- Supports comprehensive logging and debugging

### Estate Access Information Served
- **Estate Managers** - List of agents with estate management privileges
- **Allowed Agents** - List of individual agents with access permissions
- **Allowed Groups** - List of groups with access permissions  
- **Banned Agents** - List of banned agents with ban details

## API Endpoint

### Request Format
- **Method:** GET (only)
- **Authentication:** Requires estate management permissions
- **Response Format:** LLSD-XML

### Response Structure
```xml
<llsd>
  <map>
    <key>AllowedAgents</key>
    <array>
      <map>
        <key>id</key>
        <uuid>[agent-uuid]</uuid>
      </map>
      <!-- ... more allowed agents -->
    </array>
    
    <key>AllowedGroups</key>
    <array>
      <map>
        <key>id</key>
        <uuid>[group-uuid]</uuid>
      </map>
      <!-- ... more allowed groups -->
    </array>
    
    <key>BannedAgents</key>
    <array>
      <map>
        <key>id</key>
        <uuid>[banned-agent-uuid]</uuid>
        <key>banning_id</key>
        <uuid>[banning-user-uuid]</uuid>
        <key>last_login_date</key>
        <string>na</string>
        <key>ban_date</key>
        <string>YYYY-MM-DD HH:mm</string>
      </map>
      <!-- ... more banned agents -->
    </array>
    
    <key>Managers</key>
    <array>
      <map>
        <key>agent_id</key>
        <uuid>[manager-uuid]</uuid>
      </map>
      <!-- ... more managers -->
    </array>
  </map>
</llsd>
```

### Response Codes
- **200 OK** - Estate access data returned successfully
- **401 Unauthorized** - User lacks estate management permissions
- **404 Not Found** - Non-GET request method
- **410 Gone** - Agent not present or estate settings unavailable

## Security

### Permission Validation
- Validates scene presence via `m_scene.TryGetScenePresence()`
- Requires estate command permissions via `m_scene.Permissions.CanIssueEstateCommand()`
- Only serves data to authorized estate managers

### Access Control
- Only responds to GET requests
- Validates estate settings availability
- Returns appropriate HTTP status codes for different error conditions

## Module Lifecycle

1. **Initialise()** - Reads configuration and enables module if configured
2. **AddRegion()** - Associates module with scene if enabled  
3. **RegionLoaded()** - Validates estate settings and registers capabilities
4. **RemoveRegion()** - Cleans up event handlers when region is removed

## Data Processing

### Estate Settings Integration
The module retrieves access control data directly from `EstateSettings`:
```csharp
EstateSettings regionSettings = m_scene.RegionInfo.EstateSettings;
UUID[] managers = regionSettings.EstateManagers;
UUID[] allowed = regionSettings.EstateAccess;
UUID[] groups = regionSettings.EstateGroups;  
EstateBan[] EstateBans = regionSettings.EstateBans;
```

### LLSD Encoding
Uses `LLSDxmlEncode2` for efficient LLSD-XML generation:
- Handles empty arrays gracefully
- Filters out zero UUIDs automatically
- Formats ban dates in ISO format (YYYY-MM-DD HH:mm)
- Sets unavailable last login dates to "na"

### Ban Information Processing
- Converts Unix timestamps to readable date format
- Handles zero ban times as "0000-00-00 00:00"
- Includes both banned user ID and banning user ID
- Notes that last login date is not available from grid data

## Capabilities System

### Registration
Registers the "EstateAccess" capability with a random UUID path for security:
```csharp
caps.RegisterSimpleHandler("EstateAccess", 
    new SimpleStreamHandler("/" + UUID.Random(), ProcessRequest));
```

### Security Features  
- Random capability URLs prevent unauthorized access
- Per-agent capability registration
- Request method validation (GET only)

## Logging and Debugging

### Comprehensive Logging
- Initialization status logging
- Regional loading success/failure tracking
- Request processing with method validation
- Authorization failure warnings
- Success logging with data summary counts

### Debug Information
- Logs capability registration for each agent
- Tracks request processing with agent details
- Reports final data counts (managers, allowed, groups, banned)

## Usage Notes

- Module only activates when explicitly configured with localhost capability URL
- Requires functional estate module and valid estate settings
- Designed for read-only estate access information retrieval
- Integrates with viewer estate access panels and third-party tools
- Returns real-time estate access control state
- Empty lists are handled gracefully with proper LLSD empty array encoding

## Integration Points

### Estate Module Dependency
- Validates `IEstateModule` availability during region loading
- Relies on estate settings being properly initialized
- Works with any estate module implementation

### Viewer Integration
- Provides data for estate access control panels in viewers
- Compatible with Second Life protocol estate access capabilities
- Supports estate management tools and utilities