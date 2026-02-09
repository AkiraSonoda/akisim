# UploadBakedTextureModule Technical Documentation

## Overview

The **UploadBakedTextureModule** is a shared region module that provides avatar baked texture upload capabilities for OpenSimulator viewers. It implements the `UploadBakedTexture` capability, allowing viewers to upload pre-rendered avatar textures (baked textures) that combine multiple avatar appearance elements like skin, clothing, and attachments into optimized composite textures for efficient rendering and network transmission.

## Purpose

The UploadBakedTextureModule serves as a critical avatar appearance infrastructure component that:

- **Baked Texture Upload**: Provides the `UploadBakedTexture` capability for viewers to upload composite avatar textures
- **Avatar Optimization**: Enables efficient avatar rendering by combining multiple texture layers into single textures
- **Network Efficiency**: Reduces bandwidth by transmitting pre-composed textures instead of individual layers
- **Asset Management**: Handles temporary baked texture asset storage and caching
- **Security Implementation**: Validates uploads and enforces IP address restrictions
- **Protocol Compatibility**: Maintains compatibility with Second Life viewer baking protocols

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│     UploadBakedTextureModule        │
├─────────────────────────────────────┤
│      ISharedRegionModule            │
│    - Cross-region capability       │
│    - Shared asset cache access      │
├─────────────────────────────────────┤
│     Capabilities Integration        │
│    - UploadBakedTexture cap         │
│    - HTTP/HTTPS endpoint creation   │
│    - Configurable URL handling      │
├─────────────────────────────────────┤
│     BakedTextureUploader            │
│    - Asynchronous upload handling   │
│    - Security validation           │
│    - Asset creation and caching    │
├─────────────────────────────────────┤
│      Asset Cache Integration        │
│    - IAssetCache interface         │
│    - Temporary asset storage       │
│    - Local caching optimization    │
├─────────────────────────────────────┤
│       Security and Validation       │
│    - IP address verification       │
│    - Upload timeout management     │
│    - Data size limitations         │
└─────────────────────────────────────┘
```

### Data Flow Architecture

```
Viewer requests UploadBakedTexture capability
     ↓
UploadBakedTextureModule.RegisterCaps()
     ↓
Check configuration (localhost vs external URL)
     ↓
Create unique upload endpoint
     ↓
Return upload URL to viewer
     ↓
Viewer uploads baked texture data
     ↓
BakedTextureUploader.process()
     ↓
Validate IP address and data
     ↓
Create AssetBase with baked texture
     ↓
Store in asset cache (temporary, local)
     ↓
Return asset UUID to viewer
     ↓
Automatic cleanup after timeout
```

### Class Structure

#### UploadBakedTextureModule Class
```csharp
public class UploadBakedTextureModule : ISharedRegionModule
{
    private int m_nscenes;              // Scene counter
    IAssetCache m_assetCache = null;    // Asset cache interface
    private string m_URL;               // Configuration URL setting
}
```

#### BakedTextureUploader Class
```csharp
class BakedTextureUploader
{
    private string m_uploaderPath;      // Upload endpoint path
    private IHttpServer m_httpListener; // HTTP server reference
    private UUID m_agentID;             // Uploading agent UUID
    private IPAddress m_remoteAddress;  // Client IP for validation
    private IAssetCache m_assetCache;   // Asset storage interface
    private Timer m_timeout;            // Upload timeout timer
}
```

## Configuration

### Capabilities Configuration

Configure in OpenSim.ini [ClientStack.LindenCaps] section:

```ini
[ClientStack.LindenCaps]
Cap_UploadBakedTexture = "localhost"
```

### Configuration Options

- **"localhost"**: Handle uploads locally using internal capabilities system
- **""** (empty): Disable the UploadBakedTexture capability entirely
- **Custom URL**: Redirect uploads to external service (e.g., "http://external.service/upload")

### Configuration Implementation

```csharp
public void Initialise(IConfigSource source)
{
    IConfig config = source.Configs["ClientStack.LindenCaps"];
    if (config == null)
        return;

    m_URL = config.GetString("Cap_UploadBakedTexture", string.Empty);
}
```

### Factory Integration

The module is loaded via factory through the CreateCapsModules reflection method:

```csharp
private static IEnumerable<ISharedRegionModule> CreateCapsModules()
{
    var capsModuleTypes = new[]
    {
        "OpenSim.Region.ClientStack.Linden.FetchInventory2Module",
        "OpenSim.Region.ClientStack.Linden.UploadBakedTextureModule",
        "OpenSim.Region.ClientStack.LindenCaps.ServerReleaseNotesModule",
        "OpenSim.Region.ClientStack.Linden.AvatarPickerSearchModule"
    };

    foreach (var typeName in capsModuleTypes)
    {
        Type moduleType = assembly.GetType(typeName);
        if (moduleType != null)
        {
            var moduleInstance = Activator.CreateInstance(moduleType) as ISharedRegionModule;
            if (moduleInstance != null)
                modules.Add(moduleInstance);
        }
    }
}
```

## Core Functionality

### Capability Registration

#### RegisterCaps Method

```csharp
public void RegisterCaps(UUID agentID, Caps caps)
{
    if (m_URL == "localhost")
    {
        caps.RegisterSimpleHandler("UploadBakedTexture",
            new SimpleStreamHandler("/" + UUID.Random(), delegate (IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
            {
                UploadBakedTexture(httpRequest, httpResponse, agentID, caps, m_assetCache);
            }));
    }
    else if(!string.IsNullOrWhiteSpace(m_URL))
    {
        caps.RegisterHandler("UploadBakedTexture", m_URL);
    }
}
```

This method:
- **Local Handling**: Creates internal endpoint when configured as "localhost"
- **External Redirection**: Registers external URL for third-party services
- **Capability Disabling**: Ignores registration when URL is empty
- **Per-Agent Setup**: Registers capability for each connecting agent

### Upload Request Processing

#### UploadBakedTexture Method

```csharp
public void UploadBakedTexture(IOSHttpRequest httpRequest, IOSHttpResponse httpResponse, UUID agentID, Caps caps, IAssetCache cache)
{
    if(httpRequest.HttpMethod != "POST")
    {
        httpResponse.StatusCode = (int)HttpStatusCode.NotFound;
        return;
    }

    try
    {
        string capsBase = "/" + UUID.Random()+"-BK";
        string protocol = caps.SSLCaps ? "https://" : "http://";
        string uploaderURL = protocol + caps.HostName + ":" + caps.Port.ToString() + capsBase;

        LLSDAssetUploadResponse uploadResponse = new LLSDAssetUploadResponse();
        uploadResponse.uploader = uploaderURL;
        uploadResponse.state = "upload";

        BakedTextureUploader uploader =
            new BakedTextureUploader(capsBase, caps.HttpListener, agentID, cache, httpRequest.RemoteIPEndPoint.Address);

        var uploaderHandler = new SimpleBinaryHandler("POST", capsBase, uploader.process);
        uploaderHandler.MaxDataSize = 6000000; // 6MB limit

        caps.HttpListener.AddSimpleStreamHandler(uploaderHandler);

        httpResponse.RawBuffer = Util.UTF8NBGetbytes(LLSDHelpers.SerialiseLLSDReply(uploadResponse));
        httpResponse.StatusCode = (int)HttpStatusCode.OK;
    }
    catch (Exception e)
    {
        m_log.ErrorFormat("[UPLOAD BAKED TEXTURE HANDLER]: Error: {0}", e.Message);
        httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
    }
}
```

This method:
- **HTTP Method Validation**: Only accepts POST requests
- **Unique Endpoint Creation**: Generates random upload URL with "-BK" suffix
- **Protocol Selection**: Uses HTTP or HTTPS based on caps configuration
- **Size Limits**: Enforces 6MB maximum upload size
- **Security Tracking**: Records client IP address for validation
- **LLSD Response**: Returns properly formatted upload response

### Baked Texture Processing

#### BakedTextureUploader.process Method

```csharp
public void process(IOSHttpRequest httpRequest, IOSHttpResponse httpResponse, byte[] data)
{
    m_timeout.Stop();
    m_httpListener.RemoveSimpleStreamHandler(m_uploaderPath);
    m_timeout.Dispose();

    if (!httpRequest.RemoteIPEndPoint.Address.Equals(m_remoteAddress))
    {
        httpResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
        return;
    }

    try
    {
        UUID newAssetID = UUID.Random();
        AssetBase asset = new AssetBase(newAssetID, "Baked Texture", (sbyte)AssetType.Texture, m_agentID.ToString());
        asset.Data = data;
        asset.Temporary = true;
        asset.Local = true;
        m_assetCache.Cache(asset);

        LLSDAssetUploadComplete uploadComplete = new LLSDAssetUploadComplete();
        uploadComplete.new_asset = newAssetID.ToString();
        uploadComplete.new_inventory_item = UUID.Zero;
        uploadComplete.state = "complete";

        httpResponse.RawBuffer = Util.UTF8NBGetbytes(LLSDHelpers.SerialiseLLSDReply(uploadComplete));
        httpResponse.StatusCode = (int)HttpStatusCode.OK;
    }
    catch
    {
        httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
    }
}
```

This method:
- **Cleanup Management**: Stops timeout timer and removes handler
- **IP Validation**: Ensures upload comes from same IP that requested upload
- **Asset Creation**: Creates new temporary texture asset with unique UUID
- **Cache Storage**: Stores baked texture in asset cache with temporary and local flags
- **Completion Response**: Returns asset UUID and completion status to viewer
- **Error Handling**: Returns BadRequest on any processing errors

### Resource Management

#### Scene Integration

```csharp
public void RegionLoaded(Scene s)
{
    if (m_assetCache == null)
        m_assetCache = s.RequestModuleInterface <IAssetCache>();
    if (m_assetCache != null)
    {
        ++m_nscenes;
        s.EventManager.OnRegisterCaps += RegisterCaps;
    }
}

public void RemoveRegion(Scene s)
{
    s.EventManager.OnRegisterCaps -= RegisterCaps;
    --m_nscenes;
    if(m_nscenes <= 0)
        m_assetCache = null;
}
```

- **Asset Cache Access**: Requests IAssetCache interface from scene
- **Scene Counting**: Tracks number of active scenes using the module
- **Event Registration**: Registers for capability registration events
- **Cleanup**: Removes event handlers and clears cache reference when no scenes remain

#### Timeout Management

```csharp
public BakedTextureUploader(string path, IHttpServer httpServer, UUID agentID, IAssetCache cache, IPAddress remoteAddress)
{
    // ... initialization ...
    m_timeout = new Timer();
    m_timeout.Elapsed += Timeout;
    m_timeout.AutoReset = false;
    m_timeout.Interval = 30000; // 30 seconds
    m_timeout.Start();
}

private void Timeout(Object source, ElapsedEventArgs e)
{
    m_httpListener.RemoveSimpleStreamHandler(m_uploaderPath);
    m_timeout.Dispose();
}
```

- **30-Second Timeout**: Automatic cleanup of upload endpoints after 30 seconds
- **Automatic Cleanup**: Removes HTTP handlers and disposes timers
- **Resource Protection**: Prevents accumulation of stale upload endpoints

## Baked Texture System

### What are Baked Textures?

Baked textures are composite images that combine multiple avatar appearance elements:

1. **Skin Texture**: Base avatar skin
2. **Clothing Layers**: Shirts, pants, jackets, etc.
3. **Tattoos and Makeup**: Applied cosmetic layers
4. **System Layers**: Built-in avatar appearance elements

### Baking Process

```
Individual Texture Layers
     ↓
Viewer Composition Engine
     ↓
Composite Baked Texture
     ↓
Upload to OpenSimulator
     ↓
Asset Cache Storage
     ↓
Distribution to Other Viewers
```

### Baked Texture Types

OpenSimulator supports these standard baked texture types:

- **BAKED_HEAD**: Face, hair, and head accessories
- **BAKED_UPPER**: Torso, arms, and upper body clothing
- **BAKED_LOWER**: Legs, feet, and lower body clothing
- **BAKED_EYES**: Eye textures and makeup
- **BAKED_SKIRT**: Skirt attachments (if applicable)
- **BAKED_HAIR**: Hair textures and styling

### Asset Properties

Baked textures are stored with specific asset characteristics:

```csharp
AssetBase asset = new AssetBase(newAssetID, "Baked Texture", (sbyte)AssetType.Texture, m_agentID.ToString());
asset.Data = data;           // Binary texture data (JPEG2000)
asset.Temporary = true;      // Not permanently stored
asset.Local = true;          // Region-local caching
```

## LLSD Protocol Integration

### Upload Request Response

```xml
<?xml version="1.0" encoding="UTF-8"?>
<llsd>
    <map>
        <key>uploader</key>
        <string>http://simulator:9000/CAPS/UUID-BK</string>
        <key>state</key>
        <string>upload</string>
    </map>
</llsd>
```

### Upload Completion Response

```xml
<?xml version="1.0" encoding="UTF-8"?>
<llsd>
    <map>
        <key>new_asset</key>
        <string>asset-uuid-here</string>
        <key>new_inventory_item</key>
        <string>00000000-0000-0000-0000-000000000000</string>
        <key>state</key>
        <string>complete</string>
    </map>
</llsd>
```

## Security Features

### IP Address Validation

```csharp
if (!httpRequest.RemoteIPEndPoint.Address.Equals(m_remoteAddress))
{
    httpResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
    return;
}
```

- **Single-IP Restriction**: Upload must come from same IP that requested upload URL
- **Session Validation**: Prevents unauthorized uploads using leaked URLs
- **Security Logging**: Failed attempts result in HTTP 401 Unauthorized

### Upload Size Limits

```csharp
uploaderHandler.MaxDataSize = 6000000; // 6MB limit
```

- **6MB Maximum**: Prevents excessive memory usage and abuse
- **Resource Protection**: Protects against large file uploads
- **Performance Optimization**: Ensures reasonable processing times

### Timeout Protection

- **30-Second Limit**: Upload windows automatically close after 30 seconds
- **Resource Cleanup**: Prevents accumulation of stale upload endpoints
- **Memory Management**: Automatic disposal of timeout timers and handlers

## Performance Characteristics

### Memory Management

- **Temporary Assets**: Baked textures marked as temporary to prevent permanent storage
- **Local Caching**: Assets cached locally for regional distribution efficiency
- **Automatic Cleanup**: Upload handlers and timers automatically disposed
- **Shared Asset Cache**: Single asset cache instance shared across all scenes

### Network Efficiency

- **Composite Textures**: Reduces multiple texture fetches to single baked texture
- **Local Processing**: Upload handling done locally without external dependencies
- **Optimized Protocols**: Uses LLSD for efficient data serialization
- **Connection Reuse**: Leverages existing capability HTTP connections

### Processing Optimization

- **Asynchronous Operations**: Upload processing doesn't block capability registration
- **Timer-Based Cleanup**: Efficient resource management using system timers
- **Stream Handling**: Direct binary data processing without intermediate storage
- **Exception Containment**: Errors handled locally without affecting other uploads

## Integration Patterns

### Viewer Client Integration

```javascript
// Viewer perspective (conceptual)
// 1. Request UploadBakedTexture capability
var cap_url = getCapability("UploadBakedTexture");

// 2. POST to capability to get upload URL
var upload_request = {
    // Usually empty for baked textures
};
var upload_response = POST(cap_url, upload_request);

// 3. Upload baked texture data to returned URL
var texture_data = bakingEngine.compositeTexture(layers);
var completion = POST(upload_response.uploader, texture_data);

// 4. Use returned asset UUID for avatar appearance
setAvatarTexture(completion.new_asset);
```

### Asset Service Integration

```csharp
// Asset retrieval for other viewers
IAssetService assetService = scene.AssetService;
AssetBase bakedTexture = assetService.Get(assetUUID);

if (bakedTexture != null && bakedTexture.Type == (int)AssetType.Texture)
{
    // Send baked texture to requesting viewer
    sendTextureToViewer(agentID, bakedTexture);
}
```

### Scene Event Integration

```csharp
// Capability registration for new agents
scene.EventManager.OnRegisterCaps += (agentID, caps) =>
{
    if (m_assetCache != null)
    {
        uploadBakedTextureModule.RegisterCaps(agentID, caps);
    }
};
```

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: UploadBakedTexture capability not available
Causes:
- Module not in CreateCapsModules list
- Asset cache interface not available
- Configuration disabled capability

Solutions:
- Verify module in caps module types array
- Check IAssetCache module is loaded
- Set Cap_UploadBakedTexture = "localhost"
```

#### Upload Failures
```
Symptom: Baked texture uploads return HTTP 400/401
Causes:
- IP address mismatch (401)
- Data corruption or invalid format (400)
- Upload timeout exceeded
- Size limit exceeded (6MB)

Solutions:
- Ensure consistent network routing
- Verify texture data format (JPEG2000)
- Reduce upload processing time
- Check texture file sizes
```

#### Asset Storage Issues
```
Symptom: Baked textures not appearing on other viewers
Causes:
- Asset cache not functioning
- Temporary asset cleanup too aggressive
- Network issues preventing asset distribution

Solutions:
- Verify IAssetCache implementation
- Check asset service configuration
- Monitor network connectivity
```

#### Memory/Resource Issues
```
Symptom: High memory usage or handler accumulation
Causes:
- Timeout cleanup not working
- Exception preventing disposal
- High upload volume

Solutions:
- Monitor timeout timer functionality
- Check exception handling in upload process
- Implement rate limiting if needed
```

### Debug Information

Enable detailed logging for troubleshooting:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

// Enable debug logging in upload process:
m_log.DebugFormat("[UPLOAD BAKED TEXTURE]: Registering capability for agent {0}", agentID);
m_log.DebugFormat("[UPLOAD BAKED TEXTURE]: Created upload endpoint {0}", uploaderURL);
m_log.DebugFormat("[UPLOAD BAKED TEXTURE]: Processing upload from {0}", httpRequest.RemoteIPEndPoint.Address);
m_log.DebugFormat("[UPLOAD BAKED TEXTURE]: Created asset {0} for agent {1}", newAssetID, m_agentID);
```

### Testing Procedures

1. **Capability Registration**: Verify capability appears in viewer capability list
2. **Upload Request**: Test upload URL generation and response format
3. **Texture Upload**: Upload sample baked texture data
4. **Asset Verification**: Confirm asset storage in cache
5. **Distribution Test**: Verify other viewers can retrieve baked texture
6. **Cleanup Test**: Confirm timeout cleanup works correctly

## Related Components

### Dependencies
- **ISharedRegionModule**: Module interface contract
- **IAssetCache**: Asset storage and caching interface
- **Caps**: Capabilities system for HTTP endpoint management
- **LLSD**: Structured data serialization for viewer communication
- **Timer**: Resource cleanup and timeout management

### Integration Points
- **Avatar Appearance**: Baked textures used for avatar rendering
- **Asset Services**: Storage and distribution of texture assets
- **Capabilities System**: HTTP endpoint registration and management
- **Viewer Protocol**: LLSD communication and baking workflows
- **Scene Management**: Per-region capability registration

## Future Enhancements

### Potential Improvements

- **Compression Optimization**: Advanced texture compression for better performance
- **Caching Strategies**: Intelligent caching based on usage patterns
- **Validation Enhancement**: Content validation for uploaded textures
- **Progress Tracking**: Upload progress reporting for large textures
- **Batch Processing**: Multiple texture upload support

### Protocol Extensions

- **Partial Updates**: Support for partial baked texture updates
- **Format Flexibility**: Support for additional texture formats beyond JPEG2000
- **Metadata Support**: Extended texture metadata and properties
- **Version Control**: Baked texture versioning and rollback capabilities
- **Quality Settings**: Configurable texture quality and compression levels

### Performance Enhancements

- **Streaming Uploads**: Chunked upload support for large textures
- **Parallel Processing**: Concurrent upload handling
- **CDN Integration**: Content delivery network support for texture distribution
- **Bandwidth Optimization**: Adaptive quality based on connection speed
- **Regional Caching**: Cross-region baked texture sharing

---

*This documentation covers UploadBakedTextureModule as integrated with the factory-based loading system through reflection-based caps module loading, maintaining full baked texture upload functionality, asset management, and viewer compatibility without dependency on Mono.Addins.*