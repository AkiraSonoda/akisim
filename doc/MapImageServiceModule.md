# MapImageServiceModule Technical Documentation

## Overview

The **MapImageServiceModule** is a shared region module that manages map tile upload and distribution services for OpenSim regions. It acts as a bridge between map tile generators and map image storage services, providing automatic refresh capabilities, multi-region support, and seamless integration with both standalone and grid-based deployments. This module handles the conversion of generated map tiles to appropriate formats and coordinates their upload to configured map services.

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`, `IMapImageUploadModule`
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.MapImage`
- **Location**: `src/OpenSim.Region.CoreModules/ServiceConnectorsOut/MapImage/MapImageServiceModule.cs`

### Dependencies
- **Map Service Backend**: IMapImageService implementation for tile storage
- **Map Generators**: IMapImageGenerator implementations for tile creation
- **Timer Framework**: System.Timers for automatic refresh scheduling
- **Image Processing**: System.Drawing for bitmap manipulation and JPEG conversion
- **Configuration System**: Nini configuration framework for module settings

## Functionality

### Core Features

#### 1. Map Tile Upload Management
- **Service Integration**: Interfaces with configurable map image storage services
- **Format Conversion**: Converts bitmap tiles to JPEG format for transmission
- **Error Handling**: Robust error handling for upload failures and service unavailability
- **Multi-Region Support**: Manages map tiles for multiple regions simultaneously

#### 2. Automatic Refresh System
- **Configurable Intervals**: Supports customizable refresh intervals in minutes
- **Timer-Based Scheduling**: Uses System.Timers for reliable periodic updates
- **Startup Coordination**: Waits for region readiness before initial tile generation
- **Performance Monitoring**: Tracks refresh success and failure rates

#### 3. Variable Region Support
- **Legacy Region Compatibility**: Handles standard 256x256 meter regions
- **Variable Region Support**: Automatically segments larger regions into multiple tiles
- **Coordinate Translation**: Correctly maps sub-tiles to grid coordinates
- **Efficient Processing**: Optimized handling of oversized region maps

#### 4. Service Connector Architecture
- **Plugin-Based Loading**: Dynamic loading of map service implementations
- **Configuration-Driven**: Service selection via configuration parameters
- **Graceful Degradation**: Handles service unavailability without crashing
- **Scope-Aware Operations**: Respects region scope for multi-grid scenarios

### Map Upload Process

#### Single Tile Upload (Legacy Regions)
1. **Tile Generation**: Requests map tile from IMapImageGenerator
2. **Validation**: Verifies tile dimensions and content
3. **Format Conversion**: Converts bitmap to JPEG format
4. **Service Upload**: Uploads tile data to configured map service
5. **Result Handling**: Processes success/failure status

#### Multi-Tile Upload (Variable Regions)
1. **Region Analysis**: Determines number of sub-tiles needed
2. **Tile Segmentation**: Splits large region map into standard-sized tiles
3. **Coordinate Calculation**: Computes grid coordinates for each sub-tile
4. **Sequential Upload**: Uploads each sub-tile with proper positioning
5. **Error Recovery**: Aborts remaining uploads on failure

#### Automatic Refresh Cycle
1. **Timer Trigger**: Periodic timer event initiates refresh
2. **Cooldown Check**: Ensures minimum interval between refreshes
3. **Region Iteration**: Processes all registered regions
4. **Statistics Tracking**: Monitors success/failure counts
5. **Error Reporting**: Logs detailed error information

## Configuration

### Section: [Modules]
```ini
[Modules]
    ; Enable MapImageServiceModule for map tile upload services
    ; Must match module name exactly
    ; Default: empty (disabled)
    MapImageService = MapImageServiceModule
```

### Section: [MapImageService]
```ini
[MapImageService]
    ; Refresh interval for automatic map tile updates (in minutes)
    ; Set to 0 to disable automatic refresh
    ; Negative values disable the module
    ; Default: 0 (no automatic refresh)
    RefreshTime = 60

    ; Map service implementation to load
    ; Must be a valid service module implementing IMapImageService
    ; Examples: "OpenSim.Services.MapImageService.dll:MapImageService"
    ; Required for module to function
    LocalServiceModule = OpenSim.Services.MapImageService.dll:MapImageService
```

### Service Implementation Configuration
```ini
[MapImageService]
    ; Example configuration for different service types

    ; Local file-based service
    LocalServiceModule = OpenSim.Services.MapImageService.dll:MapImageService

    ; Remote HTTP-based service
    LocalServiceModule = OpenSim.Services.Connectors.dll:MapImageServicesConnector

    ; Database-backed service
    LocalServiceModule = OpenSim.Services.MapImageService.dll:MapImageService
```

### Factory Integration
The module is loaded through the `CoreModuleFactory` with the following behavior:
- **Configuration-Driven**: Only loaded when `[Modules] MapImageService = "MapImageServiceModule"`
- **Direct Instantiation**: Created directly as a CoreModule service connector
- **Service Dependencies**: Requires valid LocalServiceModule configuration

## Implementation Details

### Initialization Process
1. **Configuration Validation**: Checks for required [Modules] and [MapImageService] sections
2. **Service Loading**: Dynamically loads configured map service implementation
3. **Timer Setup**: Configures automatic refresh timer if interval > 0
4. **Module Activation**: Enables module for region registration

### Region Management
The module maintains region state using thread-safe collections:

#### Region Registration
- **Scene Tracking**: Maintains dictionary of active regions by UUID
- **Interface Registration**: Registers IMapImageUploadModule with each scene
- **Event Subscription**: Subscribes to region ready events for initial uploads
- **Resource Management**: Properly tracks and releases region resources

#### Region Deregistration
- **Clean Removal**: Removes region from tracking collections
- **Event Unsubscription**: Properly unregisters event handlers
- **Resource Cleanup**: Ensures no memory leaks from region tracking

### Timer Management

#### Timer Initialization
```csharp
m_refreshTimer = new System.Timers.Timer();
m_refreshTimer.Enabled = true;
m_refreshTimer.AutoReset = true;
m_refreshTimer.Interval = m_refreshtime;
m_refreshTimer.Elapsed += new ElapsedEventHandler(HandleMaptileRefresh);
```

#### Refresh Logic
- **Cooldown Enforcement**: Prevents overlapping refresh cycles
- **Region Iteration**: Processes all active regions
- **Error Isolation**: Continues processing other regions on individual failures
- **Performance Tracking**: Monitors refresh completion times

### Map Tile Processing

#### JPEG Conversion
```csharp
using (MemoryStream stream = new MemoryStream())
{
    tileImage.Save(stream, ImageFormat.Jpeg);
    jpgData = stream.ToArray();
}
```

#### Variable Region Handling
For regions larger than 256x256 meters:
```csharp
for (uint xx = 0; xx < mapTile.Width; xx += Constants.RegionSize)
{
    for (uint yy = 0; yy < mapTile.Height; yy += Constants.RegionSize)
    {
        Rectangle rect = new Rectangle(
            (int)xx,
            mapTile.Height - (int)yy - (int)Constants.RegionSize,
            (int)Constants.RegionSize, (int)Constants.RegionSize);
        using (Bitmap subMapTile = mapTile.Clone(rect, mapTile.PixelFormat))
        {
            ConvertAndUploadMaptile(scene, subMapTile,
                scene.RegionInfo.RegionLocX + (xx / Constants.RegionSize),
                scene.RegionInfo.RegionLocY + (yy / Constants.RegionSize),
                scene.Name);
        }
    }
}
```

### Error Handling
- **Service Failures**: Graceful handling of map service unavailability
- **Conversion Errors**: Robust JPEG conversion error handling
- **Timer Exceptions**: Prevents timer failures from affecting other regions
- **Configuration Errors**: Clear error messages for configuration issues

## Usage Examples

### Basic Configuration
```ini
[Modules]
MapImageService = MapImageServiceModule

[MapImageService]
RefreshTime = 60
LocalServiceModule = OpenSim.Services.MapImageService.dll:MapImageService
```

### High-Frequency Updates
```ini
[Modules]
MapImageService = MapImageServiceModule

[MapImageService]
RefreshTime = 15
LocalServiceModule = OpenSim.Services.MapImageService.dll:MapImageService
```

### Manual Upload Only
```ini
[Modules]
MapImageService = MapImageServiceModule

[MapImageService]
RefreshTime = 0
LocalServiceModule = OpenSim.Services.MapImageService.dll:MapImageService
```

### Remote Service Integration
```ini
[Modules]
MapImageService = MapImageServiceModule

[MapImageService]
RefreshTime = 30
LocalServiceModule = OpenSim.Services.Connectors.dll:MapImageServicesConnector
```

## Performance Considerations

### Memory Management
- **Bitmap Disposal**: Proper disposal of temporary bitmap objects
- **Stream Management**: Efficient memory stream handling for JPEG conversion
- **Timer Resources**: Appropriate timer disposal on module shutdown
- **Region Tracking**: Thread-safe region collection management

### Upload Performance
- **Sequential Processing**: Regions processed sequentially to avoid overwhelming services
- **Error Isolation**: Individual region failures don't affect others
- **Efficient Conversion**: Optimized JPEG conversion with minimal memory allocation
- **Network Optimization**: Compressed JPEG format minimizes bandwidth usage

### Scalability Factors
- **Region Count**: Performance scales linearly with number of regions
- **Refresh Frequency**: Higher frequencies increase CPU and network load
- **Tile Size**: Variable regions require more processing and upload bandwidth
- **Service Latency**: Remote services may introduce upload delays

### Optimization Strategies
- **Appropriate Intervals**: Balance freshness with performance requirements
- **Service Locality**: Local services provide better performance than remote
- **Error Recovery**: Intelligent retry mechanisms for temporary failures
- **Resource Pooling**: Reuse objects where possible to reduce GC pressure

## Troubleshooting

### Common Issues

#### 1. Module Not Loading
**Symptoms**: No map tile uploads, IMapImageUploadModule interface not available
**Solutions**:
- Verify `[Modules] MapImageService = "MapImageServiceModule"` configuration
- Check that module is properly integrated with CoreModuleFactory
- Ensure configuration section names match exactly (case-sensitive)
- Monitor logs for initialization error messages

#### 2. Service Loading Failures
**Symptoms**: Module loads but tiles never upload, service loading errors
**Solutions**:
- Verify LocalServiceModule path and assembly name are correct
- Check that specified service class exists and implements IMapImageService
- Ensure service assembly is available in bin directory
- Test service configuration independently

#### 3. Automatic Refresh Not Working
**Symptoms**: Manual uploads work but automatic refresh doesn't occur
**Solutions**:
- Check RefreshTime is greater than 0 in configuration
- Verify timer initialization in debug logs
- Monitor for timer exceptions that might disable refresh
- Ensure regions are properly registered and ready

#### 4. Variable Region Upload Issues
**Symptoms**: Large regions only partially upload or fail entirely
**Solutions**:
- Monitor logs for sub-tile processing errors
- Check coordinate calculation for large regions
- Verify map service can handle multiple tiles per region
- Test with smaller regions to isolate the issue

#### 5. JPEG Conversion Failures
**Symptoms**: Tile generation succeeds but upload fails with conversion errors
**Solutions**:
- Check System.Drawing library availability and configuration
- Monitor for memory pressure during conversion
- Verify bitmap format compatibility with JPEG conversion
- Test with different map generators to isolate the issue

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization and service loading details
- Region registration and deregistration events
- Timer setup and refresh cycle execution
- Map tile generation and upload progress
- JPEG conversion success/failure details
- Service communication and error responses

### Performance Monitoring
Monitor these metrics for optimal performance:
- **Upload Success Rate**: Should be near 100% under normal conditions
- **Refresh Cycle Time**: Should complete within reasonable timeframes
- **Memory Usage**: Monitor for memory leaks in bitmap processing
- **Service Response Time**: Track map service performance
- **Error Frequency**: Low error rates indicate healthy operation

### Configuration Validation
Use these steps to validate configuration:

1. **Check Module Loading**:
```bash
# Search for MapImageServiceModule in logs
grep "MapImageServiceModule" OpenSim.log
```

2. **Verify Service Configuration**:
```bash
# Check service loading messages
grep "LocalServiceModule" OpenSim.log
```

3. **Monitor Upload Activity**:
```bash
# Track map tile upload operations
grep "Upload.*tile" OpenSim.log
```

## Integration Notes

### Factory Loading
- Loaded via `CoreModuleFactory.CreateSharedModules()` as a service connector
- Uses direct instantiation based on configuration match
- Requires exact module name match in [Modules] section

### Service Architecture Integration
- Implements standard service connector pattern
- Uses ServerUtils.LoadPlugin for dynamic service loading
- Integrates with OpenSim service framework
- Supports both local and remote service implementations

### Interface Implementation
- Implements `IMapImageUploadModule` for external upload requests
- Provides `UploadMapTile()` methods for manual and automatic uploads
- Supports both bitmap and scene-based upload interfaces
- Integrates with region event system for automatic uploads

### Map Generation Integration
- Uses `IMapImageGenerator` interface to obtain map tiles
- Compatible with all map generation modules (MapImageModule, Warp3DImageModule)
- Handles null tile responses gracefully
- Supports variable region sizes automatically

## Service Implementation Examples

### Local File Service
```csharp
// Example IMapImageService implementation for local file storage
public bool AddMapTile(int x, int y, byte[] imageData, UUID scopeID, out string reason)
{
    string filename = Path.Combine(m_TilesDir, $"map-{x}-{y}.jpg");
    File.WriteAllBytes(filename, imageData);
    reason = string.Empty;
    return true;
}
```

### Database Service
```csharp
// Example IMapImageService implementation for database storage
public bool AddMapTile(int x, int y, byte[] imageData, UUID scopeID, out string reason)
{
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.CommandText = "REPLACE INTO maptiles (locX, locY, data, scopeID) VALUES (?x, ?y, ?data, ?scope)";
        cmd.Parameters.AddWithValue("?x", x);
        cmd.Parameters.AddWithValue("?y", y);
        cmd.Parameters.AddWithValue("?data", imageData);
        cmd.Parameters.AddWithValue("?scope", scopeID.ToString());
        // Execute command...
    }
}
```

### HTTP Service
```csharp
// Example IMapImageService implementation for HTTP upload
public bool AddMapTile(int x, int y, byte[] imageData, UUID scopeID, out string reason)
{
    using (WebClient client = new WebClient())
    {
        client.UploadData($"{m_ServerUrl}/tiles/{x}/{y}", imageData);
    }
    reason = string.Empty;
    return true;
}
```

## Security Considerations

### Service Communication
- **Authentication**: Ensure map services implement proper authentication
- **Authorization**: Verify region permissions before allowing uploads
- **Data Validation**: Validate tile data and coordinates before storage
- **Network Security**: Use HTTPS for remote service communication

### Resource Protection
- **Memory Limits**: Monitor memory usage during tile processing
- **Upload Limits**: Implement reasonable limits on tile size and frequency
- **Error Handling**: Prevent service errors from revealing sensitive information
- **Access Control**: Restrict map service access to authorized regions only

### Data Privacy
- **Scope Isolation**: Ensure tiles are properly scoped to prevent cross-contamination
- **Data Retention**: Consider privacy implications of long-term tile storage
- **Access Logging**: Log map tile access for security auditing
- **Sensitive Content**: Be aware of potentially sensitive information in map tiles

## See Also
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [MapImageModule](./MapImageModule.md) - Legacy map tile generation
- [Warp3DImageModule](./Warp3DImageModule.md) - Advanced map tile generation
- [Service Architecture](../docs/ServiceArchitecture.md) - OpenSim service framework
