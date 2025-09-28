# WorldViewModule Technical Documentation

## Overview

The **WorldViewModule** is a non-shared region module that provides HTTP-based world view image generation capabilities within OpenSimulator. It serves as a RESTful web service that generates real-time rendered images of the virtual world from specified viewpoints, enabling external applications to access visual representations of the 3D environment through standard HTTP requests.

## Purpose

The WorldViewModule serves as a visual bridge between the 3D virtual world and external applications that:

- **HTTP Image Service**: Provides RESTful endpoints for world view image generation
- **Real-time Rendering**: Generates live images from specified camera positions and orientations
- **External Integration**: Enables web applications, monitoring systems, and third-party tools to access world visuals
- **Configurable Rendering**: Supports customizable camera parameters including position, rotation, field of view, and image dimensions
- **Texture Control**: Offers optional texture rendering for performance optimization
- **Per-Region Service**: Operates independently per region for distributed world coverage

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│          WorldViewModule            │
├─────────────────────────────────────┤
│       INonSharedRegionModule        │
│    - Per-region instantiation      │
│    - Independent configuration     │
│    - Isolated state management     │
├─────────────────────────────────────┤
│      HTTP Request Handler           │
│   WorldViewRequestHandler          │
│    - RESTful endpoint management    │
│    - Parameter validation          │
│    - Response formatting           │
├─────────────────────────────────────┤
│      Image Generation Engine        │
│     IMapImageGenerator             │
│    - 3D scene rendering            │
│    - Camera positioning            │
│    - Texture processing            │
├─────────────────────────────────────┤
│       Response Processing           │
│    - JPEG encoding                 │
│    - Memory stream management      │
│    - Error handling                │
└─────────────────────────────────────┘
```

### Request Flow Architecture

```
HTTP Request
     ↓
WorldViewRequestHandler
     ↓
Parameter Validation
     ↓
WorldViewModule.GenerateWorldView()
     ↓
IMapImageGenerator.CreateViewImage()
     ↓
3D Scene Rendering
     ↓
JPEG Encoding
     ↓
HTTP Response (image/jpeg)
```

### Module Lifecycle

```
  Initialise()
      ↓
  AddRegion()
      ↓
RegionLoaded()
      ↓
HTTP Handler Registration
      ↓
Service Ready
      ↓
RemoveRegion()
      ↓
   Close()
```

## Interface Implementation

The module implements:
- **INonSharedRegionModule**: Each region has its own module instance

### Module Lifecycle Methods

```csharp
public void Initialise(IConfigSource config)
public void AddRegion(Scene scene)
public void RegionLoaded(Scene scene)
public void RemoveRegion(Scene scene)
public void Close()
```

## Configuration

### Module Activation

Configure in OpenSim.ini [Modules] section:

```ini
[Modules]
WorldViewModule = WorldViewModule
```

The module requires explicit configuration to enable, ensuring it only runs when specifically requested.

### Configuration Validation

```csharp
public void Initialise(IConfigSource config)
{
    IConfig moduleConfig = config.Configs["Modules"];
    if (moduleConfig == null)
        return;

    if (moduleConfig.GetString("WorldViewModule", String.Empty) != Name)
        return;

    m_Enabled = true;
}
```

The module enables only when the configuration explicitly matches the module name.

### Factory Integration

The module is loaded via factory with configuration-based activation:

```csharp
if (modulesConfig?.GetString("WorldViewModule", String.Empty) == "WorldViewModule")
{
    if(m_log.IsDebugEnabled) m_log.Debug("Loading WorldViewModule for HTTP-based world view image generation");
    var worldViewModuleInstance = LoadWorldViewModule();
    if (worldViewModuleInstance != null)
    {
        yield return worldViewModuleInstance;
        if(m_log.IsInfoEnabled) m_log.Info("WorldViewModule loaded for HTTP world view endpoints and image generation");
    }
    else
    {
        m_log.Warn("WorldViewModule was configured ([Modules] WorldViewModule = WorldViewModule) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
    }
}
```

## Core Functionality

### HTTP Endpoint Registration

#### Service Initialization

```csharp
public void RegionLoaded(Scene scene)
{
    if (!m_Enabled)
        return;

    m_Generator = scene.RequestModuleInterface<IMapImageGenerator>();
    if (m_Generator == null)
    {
        m_Enabled = false;
        return;
    }

    IHttpServer server = MainServer.GetHttpServer(0);
    server.AddStreamHandler(new WorldViewRequestHandler(this,
            scene.RegionInfo.RegionID.ToString()));
}
```

The module registers a unique HTTP endpoint for each region using the region UUID.

#### Endpoint Pattern

```
GET /worldview/{region-uuid}?parameters
```

Example:
```
GET /worldview/550e8400-e29b-41d4-a716-446655440000?posX=128&posY=128&posZ=50&rotX=0&rotY=0&rotZ=0&fov=60&width=800&height=600&usetex=true
```

### Image Generation Pipeline

#### Primary Generation Method

```csharp
public byte[] GenerateWorldView(Vector3 pos, Vector3 rot, float fov,
        int width, int height, bool usetex)
{
    if (!m_Enabled)
        return Array.Empty<byte>();

    using (Bitmap bmp = m_Generator.CreateViewImage(pos, rot, fov, width, height, usetex))
    {
        using (MemoryStream str = new MemoryStream())
        {
            bmp.Save(str, ImageFormat.Jpeg);
            return str.ToArray();
        }
    }
}
```

The method integrates with the scene's map image generator to create rendered views.

### HTTP Request Processing

#### Request Handler Implementation

```csharp
public class WorldViewRequestHandler : BaseStreamHandler
{
    protected WorldViewModule m_WorldViewModule;

    public WorldViewRequestHandler(WorldViewModule fmodule, string rid)
            : base("GET", "/worldview/" + rid)
    {
        m_WorldViewModule = fmodule;
    }
}
```

Each region gets its own request handler with a unique endpoint.

#### Parameter Processing

```csharp
protected override byte[] ProcessRequest(string path, Stream requestData,
        IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
{
    httpResponse.ContentType = "image/jpeg";

    Dictionary<string, object> request = new Dictionary<string, object>();
    foreach (string name in httpRequest.QueryString)
        request[name] = httpRequest.QueryString[name];

    return SendWorldView(request);
}
```

The handler extracts query parameters and forwards them for processing.

### Parameter Validation and Processing

#### Required Parameters

```csharp
public Byte[] SendWorldView(Dictionary<string, object> request)
{
    // Required parameters validation
    if (!request.ContainsKey("posX")) return Array.Empty<byte>();
    if (!request.ContainsKey("posY")) return Array.Empty<byte>();
    if (!request.ContainsKey("posZ")) return Array.Empty<byte>();
    if (!request.ContainsKey("rotX")) return Array.Empty<byte>();
    if (!request.ContainsKey("rotY")) return Array.Empty<byte>();
    if (!request.ContainsKey("rotZ")) return Array.Empty<byte>();
    if (!request.ContainsKey("fov")) return Array.Empty<byte>();
    if (!request.ContainsKey("width")) return Array.Empty<byte>();
    if (!request.ContainsKey("height")) return Array.Empty<byte>();
    if (!request.ContainsKey("usetex")) return Array.Empty<byte>();

    // Parameter conversion with error handling
    try
    {
        posX = Convert.ToSingle(request["posX"]);
        posY = Convert.ToSingle(request["posY"]);
        posZ = Convert.ToSingle(request["posZ"]);
        rotX = Convert.ToSingle(request["rotX"]);
        rotY = Convert.ToSingle(request["rotY"]);
        rotZ = Convert.ToSingle(request["rotZ"]);
        fov = Convert.ToSingle(request["fov"]);
        width = Convert.ToInt32(request["width"]);
        height = Convert.ToInt32(request["height"]);
        usetex = Convert.ToBoolean(request["usetex"]);
    }
    catch
    {
        return Array.Empty<byte>();
    }
}
```

### Camera Parameter System

#### Position Parameters
- **posX**: X coordinate in region space (0.0 to 256.0)
- **posY**: Y coordinate in region space (0.0 to 256.0)
- **posZ**: Z coordinate (height) in meters (any positive value)

#### Rotation Parameters
- **rotX**: Pitch rotation in radians
- **rotY**: Yaw rotation in radians
- **rotZ**: Roll rotation in radians

#### View Parameters
- **fov**: Field of view in degrees (typically 45-90)
- **width**: Image width in pixels
- **height**: Image height in pixels
- **usetex**: Boolean flag to enable/disable texture rendering

### Image Format and Quality

#### Output Specifications

- **Format**: JPEG (image/jpeg MIME type)
- **Compression**: Standard JPEG compression for optimal web delivery
- **Color Depth**: 24-bit RGB
- **Dimensions**: User-specified width and height
- **Memory Management**: Automatic disposal of bitmap and stream objects

#### Performance Optimization

```csharp
using (Bitmap bmp = m_Generator.CreateViewImage(pos, rot, fov, width, height, usetex))
{
    using (MemoryStream str = new MemoryStream())
    {
        bmp.Save(str, ImageFormat.Jpeg);
        return str.ToArray();
    }
}
```

Proper resource disposal prevents memory leaks during high-frequency usage.

## Advanced Features

### Dynamic Image Generation

The module provides real-time rendering capabilities:
- **Live Scene State**: Images reflect current object positions, avatars, and scene state
- **Dynamic Lighting**: Rendered lighting matches current environment settings
- **Object Visibility**: All visible objects, terrain, and effects are included
- **Avatar Rendering**: Live avatar positions and animations are captured

### Texture Control System

```csharp
bool usetex = Convert.ToBoolean(request["usetex"]);
```

The texture flag allows performance optimization:
- **usetex=true**: Full texture rendering with materials and lighting
- **usetex=false**: Wireframe or simplified rendering for faster generation

### Multi-Region Support

Each region instance operates independently:
- **Unique Endpoints**: `/worldview/{region-uuid}` per region
- **Isolated State**: No shared state between region instances
- **Independent Configuration**: Per-region enable/disable capability
- **Concurrent Operation**: Multiple regions can serve requests simultaneously

## Performance Characteristics

### Rendering Performance

- **Generation Time**: Varies based on scene complexity (50ms to 2 seconds)
- **Memory Usage**: Temporary bitmap allocation during rendering
- **CPU Usage**: Intensive 3D rendering operations
- **Network Efficiency**: JPEG compression optimizes bandwidth usage

### Scalability Features

- **Non-blocking Operation**: Each request handled independently
- **Resource Cleanup**: Automatic memory management prevents leaks
- **Error Isolation**: Failed requests don't affect other operations
- **Concurrent Support**: Multiple simultaneous requests supported

### Performance Metrics

| Parameter | Typical Range | Performance Impact |
|-----------|---------------|-------------------|
| Image Size | 400x300 to 1920x1080 | Linear with pixel count |
| Field of View | 45° to 90° | Minimal impact |
| Texture Rendering | On/Off | 2-5x performance difference |
| Scene Complexity | Varies | Major impact on generation time |

## Error Handling and Resilience

### Configuration Validation

```csharp
public void RegionLoaded(Scene scene)
{
    if (!m_Enabled)
        return;

    m_Generator = scene.RequestModuleInterface<IMapImageGenerator>();
    if (m_Generator == null)
    {
        m_Enabled = false;
        return;
    }
}
```

The module gracefully disables if the required image generator is unavailable.

### Request Error Handling

```csharp
try
{
    // Parameter conversion and processing
}
catch (Exception e)
{
    m_log.Debug("Exception: " + e.ToString());
}

return Array.Empty<byte>();
```

Invalid requests return empty responses rather than causing server errors.

### Resource Management

```csharp
using (Bitmap bmp = m_Generator.CreateViewImage(...))
{
    using (MemoryStream str = new MemoryStream())
    {
        // Processing with automatic disposal
    }
}
```

Proper `using` statements ensure resources are released even if exceptions occur.

### Graceful Degradation

- **Module Unavailable**: Returns empty responses when disabled
- **Parameter Errors**: Returns empty responses for invalid parameters
- **Generator Failure**: Module disables automatically if image generator unavailable
- **Memory Pressure**: Garbage collection handles temporary allocations

## Security Considerations

### Access Control

- **No Authentication**: Currently provides open access to configured endpoints
- **Parameter Validation**: Strict validation prevents injection attacks
- **Resource Limits**: Image generation bounded by available system resources
- **Error Information**: Minimal error information exposed in responses

### Resource Protection

- **Memory Management**: Automatic cleanup prevents memory exhaustion
- **Processing Limits**: Bounded by scene complexity and system capabilities
- **Request Isolation**: Individual request failures don't affect others
- **Configuration Control**: Module only enables with explicit configuration

### Network Security

- **HTTP Only**: Currently operates over HTTP (consider HTTPS for sensitive deployments)
- **Region Isolation**: Each region serves only its own content
- **Parameter Sanitization**: All parameters validated before processing
- **Response Headers**: Proper MIME type headers for security

## Integration Points

### Map Image Generator Integration

```csharp
m_Generator = scene.RequestModuleInterface<IMapImageGenerator>();
```

The module depends on the scene's map image generator for rendering capabilities.

### HTTP Server Integration

```csharp
IHttpServer server = MainServer.GetHttpServer(0);
server.AddStreamHandler(new WorldViewRequestHandler(this, scene.RegionInfo.RegionID.ToString()));
```

Seamless integration with OpenSimulator's HTTP infrastructure.

### Scene System Integration

- **Scene Events**: Rendered images reflect current scene state
- **Object System**: All scene objects included in rendering
- **Terrain System**: Terrain height and textures rendered
- **Avatar System**: Live avatar positions and animations captured

## API Reference

### HTTP Endpoint

```
GET /worldview/{region-uuid}
```

### Query Parameters

| Parameter | Type | Required | Description | Range/Values |
|-----------|------|----------|-------------|--------------|
| posX | float | Yes | Camera X position | 0.0 - 256.0 |
| posY | float | Yes | Camera Y position | 0.0 - 256.0 |
| posZ | float | Yes | Camera Z position (height) | Any positive value |
| rotX | float | Yes | Camera pitch rotation | Radians |
| rotY | float | Yes | Camera yaw rotation | Radians |
| rotZ | float | Yes | Camera roll rotation | Radians |
| fov | float | Yes | Field of view | Degrees (typically 45-90) |
| width | int | Yes | Image width | Pixels |
| height | int | Yes | Image height | Pixels |
| usetex | bool | Yes | Enable texture rendering | true/false |

### Response Format

- **Content-Type**: `image/jpeg`
- **Body**: JPEG-encoded image data
- **Success**: 200 OK with image data
- **Error**: 200 OK with empty body

### Example Usage

#### Basic Request
```bash
curl "http://simulator:9000/worldview/550e8400-e29b-41d4-a716-446655440000?posX=128&posY=128&posZ=50&rotX=0&rotY=0&rotZ=0&fov=60&width=800&height=600&usetex=true" > worldview.jpg
```

#### High-Quality Screenshot
```bash
curl "http://simulator:9000/worldview/550e8400-e29b-41d4-a716-446655440000?posX=128&posY=128&posZ=100&rotX=-0.5&rotY=0&rotZ=0&fov=45&width=1920&height=1080&usetex=true" > screenshot.jpg
```

#### Performance Mode (No Textures)
```bash
curl "http://simulator:9000/worldview/550e8400-e29b-41d4-a716-446655440000?posX=128&posY=128&posZ=25&rotX=0&rotY=0&rotZ=0&fov=90&width=400&height=300&usetex=false" > wireframe.jpg
```

## Use Cases

### Web Integration

- **Region Previews**: Display live region thumbnails on websites
- **Virtual Tours**: Create guided tours with scripted camera movements
- **Monitoring Dashboards**: Real-time visual monitoring of virtual spaces
- **Social Media**: Generate shareable images of virtual world activities

### Development and Debugging

- **Scene Validation**: Visual verification of object placement and terrain
- **Performance Testing**: Monitor scene complexity and rendering performance
- **Documentation**: Generate documentation images for builds and layouts
- **Quality Assurance**: Automated visual testing of scene changes

### Third-Party Applications

- **Mobile Apps**: Provide world views for mobile applications
- **External Tools**: Integrate with content management systems
- **Analytics**: Visual analytics of space utilization and activity
- **Archive Systems**: Create visual archives of virtual world history

### Educational Applications

- **Virtual Campus Tours**: Provide visual previews of educational spaces
- **Research Documentation**: Document virtual experiments and environments
- **Student Projects**: Enable students to capture and share their work
- **Remote Learning**: Provide visual context for virtual classrooms

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: WorldViewModule not appearing in logs
Cause: [Modules] WorldViewModule != "WorldViewModule"
Solution: Set WorldViewModule = WorldViewModule in [Modules] section
```

#### HTTP Endpoint Not Available
```
Symptom: 404 Not Found for /worldview/ URLs
Causes:
- Module not enabled for region
- IMapImageGenerator not available
- HTTP server configuration issues

Solutions:
- Verify module configuration per region
- Check map image generator availability
- Verify HTTP server port and accessibility
```

#### Empty Image Responses
```
Symptom: HTTP 200 but zero-byte responses
Causes:
- Missing required parameters
- Invalid parameter values
- Image generation failure

Solutions:
- Verify all required parameters are present
- Check parameter value ranges and types
- Review server logs for generation errors
```

#### Performance Issues
```
Symptom: Slow image generation or timeouts
Causes:
- Complex scenes with many objects
- High-resolution image requests
- Texture rendering enabled with large textures

Solutions:
- Reduce image dimensions for faster generation
- Disable texture rendering (usetex=false)
- Optimize scene complexity
- Consider caching for frequently requested views
```

### Debug Information

Enable debug logging for detailed troubleshooting:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

// Add debug statements:
m_log.DebugFormat("[WORLDVIEW]: Processing request for position {0}", pos);
m_log.DebugFormat("[WORLDVIEW]: Generated image size: {0} bytes", imageData.Length);
```

### Performance Monitoring

Key metrics to monitor:
- **Request Rate**: Number of image generation requests per minute
- **Generation Time**: Average time to generate images
- **Memory Usage**: Peak memory usage during image generation
- **Error Rate**: Percentage of failed requests

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [Modules] WorldViewModule setting
- **Enhanced Logging**: Improved operational visibility and debugging capabilities
- **Backward Compatibility**: Maintains full API and configuration compatibility

### Upgrade Considerations

- Update configuration files to use factory loading system
- Review HTTP endpoint accessibility after upgrade
- Test image generation functionality after migration
- Verify proper integration with map image generators

## Related Components

### Dependencies
- **IMapImageGenerator**: Core rendering engine for image generation
- **INonSharedRegionModule**: Module interface contract
- **BaseStreamHandler**: HTTP request handling infrastructure
- **MainServer**: HTTP server management

### Integration Points
- **Scene System**: Live scene state and object rendering
- **HTTP Infrastructure**: RESTful endpoint management
- **Image Processing**: Bitmap manipulation and JPEG encoding
- **Module Interface**: Per-region module lifecycle management

## Future Enhancements

### Potential Improvements

- **Authentication System**: Token-based or API key authentication
- **Caching Layer**: Redis or memory-based caching for frequently requested views
- **Format Options**: Support for PNG, WebP, and other image formats
- **Streaming Capability**: WebRTC or WebSocket-based live streaming
- **Batch Processing**: Multiple view generation in single requests

### Advanced Features

- **Animation Support**: Capture animated GIFs or video sequences
- **Effects Processing**: Post-processing filters and effects
- **Template System**: Predefined camera positions and compositions
- **Quality Presets**: Predefined quality/performance profiles
- **Analytics Integration**: Usage statistics and performance metrics

### Security Enhancements

- **Rate Limiting**: Request throttling to prevent abuse
- **Access Control Lists**: IP-based or user-based access restrictions
- **HTTPS Support**: Encrypted communication for sensitive deployments
- **Audit Logging**: Detailed logging of access patterns and usage

---

*This documentation covers WorldViewModule as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full HTTP-based world view image generation and RESTful endpoint capabilities.*