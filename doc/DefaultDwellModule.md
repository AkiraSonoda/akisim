# DefaultDwellModule Technical Documentation

## Overview

The **DefaultDwellModule** is a core OpenSimulator module that manages parcel dwell tracking and land popularity metrics. It provides functionality for tracking how much time avatars spend on land parcels and handles client requests for dwell information. The module is essential for land economics and popularity tracking in virtual worlds.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **IDwellModule**: Dwell-specific functionality interface for external access

### Key Components
- **Parcel Dwell Tracking**: Monitor and store land parcel popularity
- **Client Request Handling**: Process viewer requests for dwell information
- **Land Data Integration**: Access dwell values from land management system
- **Configurable Loading**: Optional module loading based on configuration

## Dwell System

### Dwell Concept
Dwell represents the popularity or traffic measure of a land parcel:
- **Time-based Metric**: Measures avatar time spent on parcels
- **Popularity Indicator**: Higher dwell values indicate more popular areas
- **Economic Factor**: Often used in land valuation and search rankings
- **Viewer Display**: Shown in land information dialogs and search results

### Dwell Data Storage
Dwell values are stored in the land management system:
- **LandData.Dwell**: Floating-point dwell value stored with parcel data
- **Persistence**: Dwell values persist across server restarts
- **FakeID**: Uses land parcel's FakeID for client communication
- **Integer Conversion**: Dwell values converted to integers for client transmission

## Configuration System

### Module Enablement
```ini
[Dwell]
; Enable DefaultDwellModule (default: disabled)
DwellModule = DefaultDwellModule
```

### Configuration Behavior
- **Explicit Enablement**: Module only loads when properly configured
- **Default Disabled**: Module disabled by default (requires [Dwell] section)
- **Module Selection**: Allows for different dwell module implementations
- **Graceful Degradation**: System functions without dwell tracking when disabled

## Client Integration

### Client Event Handling
The module registers for client events to handle dwell requests:

```csharp
public void OnNewClient(IClientAPI client)
{
    client.OnParcelDwellRequest += ClientOnParcelDwellRequest;
}
```

### Dwell Request Processing
When clients request parcel dwell information:

```csharp
private void ClientOnParcelDwellRequest(int localID, IClientAPI client)
{
    ILandObject parcel = m_scene.LandChannel.GetLandObject(localID);
    if (parcel == null) return;

    LandData land = parcel.LandData;
    if(land != null)
        client.SendParcelDwellReply(localID, land.FakeID, land.Dwell);
}
```

### Client Protocol
- **Request**: Client sends parcel dwell request with local parcel ID
- **Response**: Server sends dwell reply with parcel FakeID and dwell value
- **Format**: Dwell value sent as integer (converted from float)
- **Validation**: Null parcel and land data validation before response

## API Interface

### IDwellModule Methods

#### GetDwell(UUID parcelID)
```csharp
public int GetDwell(UUID parcelID)
```
- **Purpose**: Retrieve dwell value for specific parcel UUID
- **Parameters**: `parcelID` - UUID of the land parcel
- **Returns**: Integer dwell value (0 if parcel not found)
- **Usage**: Programmatic access to parcel dwell data

#### GetDwell(LandData land)
```csharp
public int GetDwell(LandData land)
```
- **Purpose**: Retrieve dwell value from existing LandData object
- **Parameters**: `land` - LandData object containing dwell information
- **Returns**: Integer dwell value (0 if land data is null)
- **Usage**: Direct dwell extraction from land objects

### Return Value Handling
- **Integer Conversion**: Float dwell values cast to integers
- **Null Safety**: Returns 0 for null parcel or land data
- **Range**: Dwell values typically range from 0 to high positive integers
- **Precision**: Loss of decimal precision in integer conversion

## Land Management Integration

### LandChannel Integration
```csharp
ILandObject parcel = m_scene.LandChannel.GetLandObject(localID);
ILandObject parcel = m_scene.LandChannel.GetLandObject(parcelID);
```

### Land Object Access
- **By Local ID**: Retrieve parcels using local parcel identifier
- **By UUID**: Retrieve parcels using global parcel UUID
- **LandData Access**: Extract land data containing dwell information
- **Validation**: Check for null parcel and land data objects

### Dwell Data Structure
```csharp
public class LandData
{
    public float Dwell { get; set; }    // Parcel popularity metric
    public UUID FakeID { get; set; }    // Client-visible parcel ID
    // ... other land properties
}
```

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource source)
```
- **Configuration Loading**: Read [Dwell] section from configuration
- **Module Selection**: Check if DefaultDwellModule is specified
- **Enable/Disable**: Set module enabled state based on configuration

### Region Integration
```csharp
public void AddRegion(Scene scene)
public void RegionLoaded(Scene scene)
```
- **Scene Registration**: Register IDwellModule interface with scene
- **Event Registration**: Subscribe to OnNewClient events
- **Service Availability**: Make dwell services available to other modules

### Cleanup
```csharp
public void RemoveRegion(Scene scene)
```
- **Event Unregistration**: Unsubscribe from client events
- **Resource Cleanup**: Clean up module resources
- **Interface Removal**: Remove module interface from scene

## Performance Considerations

### Lightweight Operations
- **Simple Lookups**: Direct access to stored dwell values
- **No Calculations**: Module doesn't calculate dwell, only retrieves stored values
- **Minimal Memory**: Small memory footprint with basic data access
- **Fast Response**: Quick client response for dwell requests

### Scalability
- **Per-Region**: Each region maintains independent dwell tracking
- **Client Isolation**: Client requests handled independently
- **No Cross-Region**: No inter-region dwell coordination required
- **Configurable**: Can be disabled for performance-critical deployments

## Security and Validation

### Input Validation
- **Null Checks**: Comprehensive null checking for parcel and land data
- **Client Validation**: Proper client API validation
- **Boundary Checking**: Safe handling of invalid parcel IDs
- **Error Handling**: Graceful degradation on lookup failures

### Data Integrity
- **Read-Only Access**: Module only reads dwell data, doesn't modify
- **Safe Conversion**: Safe casting from float to integer values
- **Consistent Response**: Guaranteed response format for clients
- **Fallback Values**: Returns 0 for invalid or missing data

## Administrative Features

### Module Status
- **Replaceability**: Module can be replaced with alternative implementations
- **Interface Availability**: Other modules can query dwell information
- **Debug Information**: Module name and status available for diagnostics
- **Configuration Feedback**: Clear logging of enable/disable status

### Alternative Implementations
The module architecture supports different dwell implementations:
- **Plugin Architecture**: IDwellModule interface allows alternative modules
- **Configuration Selection**: DwellModule setting selects implementation
- **Compatibility**: Standard interface ensures consistent behavior
- **Extensibility**: Custom dwell tracking algorithms can be implemented

## Integration Examples

### Programmatic Dwell Access
```csharp
// Get dwell module interface
IDwellModule dwellModule = scene.RequestModuleInterface<IDwellModule>();

// Get dwell by parcel UUID
UUID parcelID = // ... parcel identifier
int dwellValue = dwellModule.GetDwell(parcelID);

// Get dwell from land data
LandData landData = // ... existing land data
int dwellValue = dwellModule.GetDwell(landData);
```

### Client Request Flow
1. **Client Request**: Viewer requests dwell information for visible parcel
2. **Module Processing**: DefaultDwellModule receives parcel dwell request
3. **Land Lookup**: Module queries land channel for parcel data
4. **Dwell Extraction**: Extract dwell value from land data
5. **Client Response**: Send dwell reply to requesting client

### Search Integration
Dwell values are commonly used in:
- **Land Search**: Filter and sort parcels by popularity
- **Economic Systems**: Land valuation based on traffic
- **Analytics**: Region traffic and usage analysis
- **Marketing**: Promote high-traffic areas

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Configuration-based Loading**: Controlled via [Dwell] section configuration
- **Default Behavior**: Disabled by default, requires explicit configuration
- **Logging Integration**: Comprehensive debug and info logging for operations

### Configuration Migration
Previous versions may have used different configuration:
```ini
# Old style (automatic loading)
# Module loaded automatically if present

# New style (explicit configuration)
[Dwell]
DwellModule = DefaultDwellModule
```

### Dependencies
- **Land Channel**: Requires functional land management system
- **Scene Management**: Integration with scene and region lifecycle
- **Client API**: Client event handling and response mechanisms
- **Land Data**: Access to persistent land data storage

## Troubleshooting

### Common Issues

#### Module Not Loading
- **Check Configuration**: Ensure [Dwell] section exists
- **Module Name**: Verify DwellModule = "DefaultDwellModule"
- **Log Messages**: Check for loading debug messages
- **Case Sensitivity**: Configuration values are case-sensitive

#### Dwell Values Not Updating
- **Module Responsibility**: DefaultDwellModule only retrieves stored values
- **Dwell Calculation**: Actual dwell calculation handled by other components
- **Data Storage**: Check land data persistence and storage
- **Regional Differences**: Each region maintains independent dwell tracking

#### Client Display Issues
- **Protocol Compatibility**: Ensure viewer supports dwell display
- **Integer Conversion**: Dwell values converted to integers for transmission
- **Network Issues**: Check client-server communication
- **Parcel Validation**: Verify parcel data integrity

## Usage Examples

### Basic Configuration
```ini
[Dwell]
DwellModule = DefaultDwellModule
```

### Alternative Implementation
```ini
[Dwell]
# Use different dwell module
DwellModule = CustomDwellModule
```

### Module Interface Usage
```csharp
// Check if dwell module is available
IDwellModule dwellModule = scene.RequestModuleInterface<IDwellModule>();
if (dwellModule != null)
{
    // Get dwell for specific parcel
    int dwell = dwellModule.GetDwell(parcelUUID);

    // Use dwell value for land ranking, economics, etc.
    Console.WriteLine($"Parcel dwell value: {dwell}");
}
```

This documentation reflects the DefaultDwellModule implementation in `src/OpenSim.Region.CoreModules/World/Land/DwellModule.cs` and its integration with the factory-based module loading system.