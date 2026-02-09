# MapSearchModule Technical Documentation

## Overview

The **MapSearchModule** is a shared region module that provides map search functionality for OpenSim viewer clients. It handles region search requests from viewers, queries the grid service for matching regions, and returns formatted map block data to enable users to locate and teleport to regions by name. This module works in conjunction with WorldMap modules to provide a complete map navigation experience for virtual world users.

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`
- **Namespace**: `OpenSim.Region.CoreModules.World.WorldMap`
- **Location**: `src/OpenSim.Region.CoreModules/World/WorldMap/MapSearchModule.cs`

### Dependencies
- **Grid Service**: IGridService for querying region information across the grid
- **Client Framework**: IClientAPI for handling viewer communication
- **World Map Modules**: Works with WorldMapModule or HGWorldMapModule for complete functionality
- **Event System**: Scene event manager for client connection handling

## Functionality

### Core Features

#### 1. Region Search Processing
- **Name-Based Search**: Searches for regions by partial or complete name matching
- **Grid Integration**: Queries grid service for region information across the virtual world
- **Multi-Result Handling**: Supports returning multiple regions matching search criteria
- **Search Validation**: Enforces minimum search string length requirements

#### 2. Client Request Handling
- **Viewer Protocol**: Handles map name request packets from various viewer clients
- **Flag Processing**: Supports different viewer-specific flag values
- **Response Formatting**: Converts grid region data to viewer-compatible map blocks
- **Error Messaging**: Provides user feedback for invalid searches

#### 3. Map Block Generation
- **Data Conversion**: Converts GridRegion objects to MapBlockData format
- **Image Selection**: Chooses appropriate map images based on request flags
- **Coordinate Translation**: Converts region coordinates to viewer map coordinates
- **Size Information**: Includes region size data for variable-sized regions

#### 4. Search Result Management
- **Result Limiting**: Caps search results to prevent overwhelming viewers
- **Result Ordering**: Maintains consistent ordering of search results
- **Final Block**: Adds closing block to properly terminate search results
- **Empty Handling**: Gracefully handles searches with no results

### Search Process Flow

#### Client Request Processing
1. **Request Reception**: Receives map name search request from viewer client
2. **Validation**: Validates search string length and format
3. **Grid Query**: Queries grid service for matching regions
4. **Result Processing**: Converts grid results to map block format
5. **Response Transmission**: Sends formatted results back to viewer

#### Search String Processing
1. **Length Validation**: Ensures minimum 3-character search string
2. **Special Character Handling**: Processes special characters in search terms
3. **Original Name Preservation**: Maintains original search terms when needed
4. **Grid Service Query**: Passes processed search to grid service

#### Result Formatting
1. **Region Data Extraction**: Extracts relevant information from GridRegion objects
2. **Map Block Creation**: Creates MapBlockData objects for each result
3. **Image Assignment**: Assigns terrain or parcel images based on flags
4. **Coordinate Conversion**: Converts world coordinates to map coordinates
5. **Final Block Addition**: Adds terminating block to complete result set

## Configuration

### Factory Integration
The module is automatically loaded by the `CoreModuleFactory` when WorldMap functionality is enabled:

```ini
[Map]
    ; Enable WorldMap functionality - required for MapSearchModule
    ; Options: "WorldMap", "HGWorldMap"
    WorldMapModule = WorldMap

[Startup]
    ; Alternative location for WorldMapModule configuration
    WorldMapModule = WorldMap
```

### Automatic Loading Behavior
- **Conditional Loading**: Only loads when WorldMapModule is configured
- **Dependency-Based**: Loads with both "WorldMap" and "HGWorldMap" configurations
- **No Configuration Required**: No separate configuration section needed
- **Service Integration**: Uses existing grid service configuration

### Grid Service Configuration
The module relies on the configured grid service for region queries:

```ini
[GridService]
    ; Grid service configuration for region lookups
    LocalServiceModule = "OpenSim.Services.GridService.dll:GridService"
    StorageProvider = "OpenSim.Data.MySQL.dll"
    ConnectionString = "Data Source=localhost;Database=opensim;User ID=opensim;Password=***;"
```

## Implementation Details

### Initialization Process
1. **Module Registration**: Registers with scene event manager for client connections
2. **Service Discovery**: Discovers grid service from first loaded region
3. **Scope Configuration**: Configures search scope based on region settings
4. **Event Subscription**: Subscribes to new client events for each region

### Client Event Handling
The module handles client connections through the event system:

```csharp
scene.EventManager.OnNewClient += OnNewClient;

private void OnNewClient(IClientAPI client)
{
    client.OnMapNameRequest += OnMapNameRequestHandler;
}
```

### Search Request Processing

#### Request Validation
- **Minimum Length**: Requires at least 3 characters for search
- **Special Characters**: Handles special characters that affect search behavior
- **Active Client Check**: Verifies client is still connected before processing

#### Grid Service Integration
```csharp
List<GridRegion> regionInfos = m_gridservice.GetRegionsByName(m_stupidScope, mapName, 20);
```

The module queries the grid service with:
- **Scope ID**: Limits search to appropriate grid scope
- **Search String**: User-provided region name or partial name
- **Result Limit**: Maximum 20 results to prevent overwhelming responses

#### Map Block Conversion
```csharp
private static void MapBlockFromGridRegion(MapBlockData block, GridRegion r, uint flag)
{
    block.Access = r.Access;
    switch (flag)
    {
        case 0:
            block.MapImageId = r.TerrainImage;
            break;
        case 2:
            block.MapImageId = r.ParcelImage;
            break;
        default:
            block.MapImageId = UUID.Zero;
            break;
    }
    block.Name = r.RegionName;
    block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
    block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
    block.SizeX = (ushort)r.RegionSizeX;
    block.SizeY = (ushort)r.RegionSizeY;
}
```

### Error Handling
- **Service Unavailability**: Gracefully handles grid service failures
- **Client Disconnection**: Checks client status before sending responses
- **Exception Handling**: Comprehensive exception handling with detailed logging
- **Invalid Searches**: Provides user feedback for invalid search criteria

### Resource Management
- **Event Cleanup**: Properly unregisters event handlers on region removal
- **Service References**: Manages grid service references appropriately
- **Memory Management**: Efficient handling of search results and map blocks

## Usage Examples

### Basic WorldMap Configuration
```ini
[Map]
WorldMapModule = WorldMap

# MapSearchModule loads automatically with WorldMapModule
```

### HyperGrid WorldMap Configuration
```ini
[Map]
WorldMapModule = HGWorldMap

# MapSearchModule provides search functionality for HyperGrid environments
```

### Standalone Configuration
```ini
[Map]
WorldMapModule = WorldMap

[GridService]
LocalServiceModule = "OpenSim.Services.GridService.dll:GridService"
StorageProvider = "OpenSim.Data.SQLite.dll"
```

### Grid Mode Configuration
```ini
[Map]
WorldMapModule = WorldMap

[GridService]
GridServerURI = "http://mygrid.example.com:8003"
```

## Performance Considerations

### Search Performance
- **Result Limiting**: Hard limit of 20 results prevents excessive data transfer
- **Grid Service Efficiency**: Performance depends on underlying grid service implementation
- **Client Validation**: Early validation reduces unnecessary grid service queries
- **Caching Potential**: Grid service may implement caching for improved performance

### Memory Usage
- **Temporary Objects**: Creates temporary MapBlockData objects during processing
- **Event Handlers**: Minimal memory overhead for event subscriptions
- **Grid Service References**: Single shared grid service reference across regions
- **Result Processing**: Processes results sequentially to minimize memory peaks

### Network Impact
- **Compressed Data**: Map block data is relatively compact
- **Batched Results**: All results sent in single response to minimize round trips
- **Grid Queries**: Single grid service query per search request
- **Alert Messages**: Minimal additional data for user feedback

### Scalability Factors
- **Concurrent Searches**: Multiple simultaneous searches handled independently
- **Region Count**: Performance scales with total number of regions in grid
- **Search Complexity**: Complex searches may require more grid service processing
- **Client Load**: Multiple clients can search simultaneously without interference

## Troubleshooting

### Common Issues

#### 1. Module Not Loading
**Symptoms**: No search functionality available in viewer map
**Solutions**:
- Verify WorldMapModule is configured in [Map] or [Startup] section
- Check that MapSearchModule loads automatically with WorldMap modules
- Monitor logs for factory loading messages
- Ensure CoreModuleFactory integration is working

#### 2. No Search Results
**Symptoms**: All searches return empty results even for known regions
**Solutions**:
- Verify grid service is properly configured and operational
- Check region scope ID configuration
- Test grid service connectivity independently
- Monitor logs for grid service query failures

#### 3. Search Validation Errors
**Symptoms**: Users get "minimum 3 characters" message for valid searches
**Solutions**:
- Check for hidden characters in search strings
- Verify search string processing logic
- Test with simple alphabetic region names
- Monitor logs for search validation details

#### 4. Grid Service Connection Failures
**Symptoms**: Search requests fail with service errors
**Solutions**:
- Verify grid service configuration and connectivity
- Check database connectivity for grid service
- Monitor network connectivity to remote grid services
- Test grid service independently

#### 5. Client Response Issues
**Symptoms**: Search results don't appear in viewer or appear incorrectly
**Solutions**:
- Check viewer compatibility with OpenSim map protocols
- Verify map block data format and content
- Test with different viewer clients
- Monitor client packet transmission

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization and region registration
- Client connection and event handler registration
- Search request processing and validation
- Grid service queries and responses
- Map block generation and transmission
- Error conditions and exception details

### Performance Monitoring
Monitor these metrics for optimal performance:
- **Search Response Time**: Should complete within reasonable timeframes
- **Grid Service Latency**: Monitor grid service query response times
- **Search Success Rate**: Track percentage of successful vs. failed searches
- **Result Accuracy**: Verify search results match expected regions
- **Client Satisfaction**: Monitor user feedback on search functionality

### Configuration Validation
Use these steps to validate configuration:

1. **Check Module Loading**:
```bash
# Search for MapSearchModule in logs
grep "MapSearchModule" OpenSim.log
```

2. **Verify Grid Service**:
```bash
# Check grid service initialization
grep "Grid.*Service" OpenSim.log
```

3. **Monitor Search Activity**:
```bash
# Track search request processing
grep "map search" OpenSim.log
```

## Integration Notes

### Factory Loading
- Loaded automatically by `CoreModuleFactory.CreateSharedModules()`
- Conditional loading based on WorldMapModule configuration
- No separate configuration section required
- Direct instantiation as CoreModule

### WorldMap Module Integration
- **Complementary Functionality**: Works alongside WorldMapModule/HGWorldMapModule
- **Shared Resources**: Uses same grid service and scene infrastructure
- **Event Coordination**: Coordinates with map modules through scene events
- **Protocol Compatibility**: Compatible with all WorldMap implementations

### Viewer Client Integration
- **Protocol Support**: Supports standard Second Life map search protocol
- **Multi-Viewer Compatibility**: Works with various OpenSim-compatible viewers
- **Flag Handling**: Processes viewer-specific flag values correctly
- **Response Format**: Generates viewer-compatible MapBlockData

### Grid Service Integration
- **Service Discovery**: Automatically discovers grid service from scenes
- **Scope Awareness**: Respects region scope for multi-grid scenarios
- **Query Optimization**: Uses efficient grid service query methods
- **Error Resilience**: Handles grid service failures gracefully

## Search Protocol Details

### Map Name Request Packet
The module handles the MapNameRequest packet with these fields:
- **AgentID**: Requesting agent's UUID
- **MapName**: Search string provided by user
- **Flags**: Viewer-specific flags (0 = terrain, 2 = parcel images)

### Map Block Response
The module responds with MapBlock packets containing:
- **RegionName**: Name of matching region
- **RegionHandle**: Encoded region coordinates
- **MapImageID**: UUID of terrain or parcel image
- **Access**: Region access level (PG, Mature, Adult)
- **RegionFlags**: Additional region flags
- **WaterHeight**: Region water height (legacy)

### Special Handling
- **Minimum Length**: Enforces 3-character minimum for searches
- **Special Characters**: Handles characters that affect search behavior
- **Result Termination**: Adds final block to properly close result set
- **V3 Compatibility**: Provides additional user feedback for V3 viewers

## Security Considerations

### Search Privacy
- **Information Disclosure**: Search results reveal region names and locations
- **Access Control**: Respects region access levels in search results
- **Scope Isolation**: Properly isolates searches by grid scope
- **User Privacy**: No personal information exposed in search results

### Service Protection
- **Result Limiting**: Prevents overwhelming grid service with unlimited queries
- **Input Validation**: Validates search strings to prevent injection attacks
- **Error Handling**: Prevents service errors from revealing sensitive information
- **Resource Limits**: Reasonable limits on search frequency and scope

### Grid Security
- **Scope Validation**: Ensures searches respect grid boundaries
- **Service Authentication**: Relies on grid service security measures
- **Data Validation**: Validates grid service responses before forwarding
- **Access Verification**: Respects region access restrictions

## See Also
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [WorldMapModule](./WorldMapModule.md) - Primary world map functionality
- [HGWorldMapModule](./HGWorldMapModule.md) - HyperGrid world map functionality
- [Grid Services](../docs/GridServices.md) - Grid service architecture and configuration
