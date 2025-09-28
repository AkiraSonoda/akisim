# XBakesModule Technical Documentation

## Overview

The XBakesModule is a non-shared region module that provides external baked texture storage and caching capabilities for OpenSimulator. It enables offloading of avatar baked textures to external storage services, reducing simulator memory usage and providing centralized avatar appearance management across multiple regions and grid instances.

## Module Classification

- **Type**: INonSharedRegionModule, IBakedTextureModule
- **Namespace**: OpenSim.Region.CoreModules.Avatar.BakedTextures
- **Assembly**: OpenSim.Region.CoreModules
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs with configuration-based loading

## Core Functionality

### Primary Purpose

The XBakesModule provides a bridge between OpenSimulator and external baked texture storage services. It implements the IBakedTextureModule interface to handle avatar appearance caching, allowing for persistent storage of baked textures that can be shared across multiple simulator instances and survive restarts.

### Key Features

1. **External Storage Integration**: REST API communication with external baked texture services
2. **XML Serialization**: Structured data format for texture metadata and asset storage
3. **WearableCacheItem Management**: Comprehensive handling of avatar appearance cache data
4. **Service Authentication**: Configurable authentication for secure external service communication
5. **Extended Texture Support**: Support for both standard and extended texture indices
6. **Asynchronous Operations**: Non-blocking texture storage using fire-and-forget patterns
7. **Compression and Optimization**: Efficient data transfer and storage mechanisms

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for non-shared modules
1. Initialise(IConfigSource) - Configuration loading and service setup
2. AddRegion(Scene) - Register module interface and scene association
3. RegionLoaded(Scene) - Final region-specific setup (no-op)
4. RemoveRegion(Scene) - Region cleanup (no-op)
5. Close() - Module cleanup (no-op)
```

### Interface Implementation

The module implements two key interfaces:

#### INonSharedRegionModule
Provides per-region module instances for localized functionality.

#### IBakedTextureModule
Defines the baked texture management contract:

```csharp
public interface IBakedTextureModule
{
    WearableCacheItem[] Get(UUID id);                                    // Retrieve cached textures
    void Store(UUID agentId);                                           // Store current textures (legacy)
    void Store(UUID agentId, WearableCacheItem[] data);                 // Store texture cache data
    void UpdateMeshAvatar(UUID agentId);                                // Update mesh avatar data
}
```

### Configuration Architecture

```csharp
public void Initialise(IConfigSource configSource)
{
    IConfig config = configSource.Configs["XBakes"];
    if (config == null) return;

    m_URL = config.GetString("URL", String.Empty);
    if (m_URL.Length == 0) return;

    m_enabled = true;
    m_Auth = ServiceAuth.Create(configSource, "XBakes");
}
```

## Configuration System

### Module Configuration

#### Required Configuration ([XBakes] section)
- **URL**: `string` - Base URL of the external baked texture service (required)

#### Optional Authentication Configuration
- **ServiceAuth Configuration**: Various authentication methods supported through ServiceAuth framework

### Configuration Examples

#### Basic Configuration
```ini
[XBakes]
URL = http://xbakes-server:8080/

# Module loads automatically when URL is specified
```

#### Configuration with Authentication
```ini
[XBakes]
URL = https://xbakes-server:8080/

# ServiceAuth configuration (various methods supported)
AuthType = BasicAuth
Username = opensim
Password = secretkey

# OR Token-based authentication
AuthType = Bearer
Token = your-api-token-here
```

### Service Integration

The module integrates with external REST services using a standardized API:

- **GET** `/bakes/{agentId}` - Retrieve baked textures for an agent
- **POST** `/bakes/{agentId}` - Store baked textures for an agent

## Data Structures and Serialization

### WearableCacheItem Structure

```csharp
public class WearableCacheItem
{
    public UUID CacheId { get; set; }        // Unique cache identifier
    public uint TextureIndex { get; set; }   // Texture slot index (0-26 standard, >26 extended)
    public AssetBase TextureAsset { get; set; } // Actual texture asset data
    public UUID TextureID { get; set; }      // Texture asset UUID
}
```

### XML Schema Structure

#### Retrieval Response Format
```xml
<BakedAppearance>
  <BakedTexture TextureIndex="0" CacheId="uuid">
    <AssetBase>
      <!-- Serialized AssetBase data -->
    </AssetBase>
  </BakedTexture>
  <!-- Additional BakedTexture elements -->

  <!-- Extended textures (TextureIndex > 26) -->
  <BESetA TextureIndex="27" CacheId="uuid">
    <AssetBase>
      <!-- Serialized AssetBase data -->
    </AssetBase>
  </BESetA>
  <!-- Additional BESetA elements -->
</BakedAppearance>
```

#### Storage Request Format
```xml
<BakedAppearance>
  <BakedTexture TextureIndex="0" CacheId="uuid">
    <AssetBase>
      <ID>asset-uuid</ID>
      <Name>texture-name</Name>
      <Description>texture-description</Description>
      <Type>texture-type</Type>
      <Data>base64-encoded-texture-data</Data>
      <!-- Additional AssetBase properties -->
    </AssetBase>
  </BakedTexture>
  <!-- Extended textures use BESetA element name -->
</BakedAppearance>
```

## Texture Retrieval Operations

### Get Method Implementation

```csharp
public WearableCacheItem[] Get(UUID id)
{
    if (m_URL.Length == 0) return null;

    using (RestClient rc = new RestClient(m_URL))
    {
        List<WearableCacheItem> ret = new List<WearableCacheItem>();
        rc.AddResourcePath("bakes/" + id.ToString());
        rc.RequestMethod = "GET";

        try
        {
            using(MemoryStream s = rc.Request(m_Auth))
            using(XmlTextReader sr = new XmlTextReader(s))
            {
                sr.DtdProcessing = DtdProcessing.Ignore;
                sr.ReadStartElement("BakedAppearance");

                // Process standard textures (BakedTexture elements)
                while (sr.LocalName == "BakedTexture")
                {
                    ProcessTextureElement(sr, ret);
                }

                // Process extended textures (BESetA elements)
                while (sr.LocalName == "BESetA")
                {
                    ProcessTextureElement(sr, ret);
                }

                return ret.ToArray();
            }
        }
        catch (XmlException)
        {
            return null;
        }
    }
}
```

### Texture Processing Workflow

1. **HTTP Request**: GET request to `/bakes/{agentId}` endpoint
2. **XML Parsing**: Parse response using XmlTextReader with DTD processing disabled
3. **Element Processing**: Handle both standard (BakedTexture) and extended (BESetA) texture elements
4. **Deserialization**: Convert XML AssetBase elements to WearableCacheItem objects
5. **Return Assembly**: Compile results into array for scene processing

## Texture Storage Operations

### Store Method Implementation

```csharp
public void Store(UUID agentId, WearableCacheItem[] data)
{
    if (m_URL.Length == 0) return;

    int numberWears = 0;
    byte[] uploadData;

    using (MemoryStream bakeStream = new MemoryStream())
    using (XmlTextWriter bakeWriter = new XmlTextWriter(bakeStream, null))
    {
        bakeWriter.WriteStartElement(String.Empty, "BakedAppearance", String.Empty);
        List<int> extended = new List<int>();

        // Process textures and separate standard vs extended
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] != null && data[i].TextureAsset != null)
            {
                if(data[i].TextureIndex > 26)
                {
                    extended.Add(i);
                    continue;
                }

                // Write standard texture
                WriteTextureElement(bakeWriter, data[i], "BakedTexture");
                numberWears++;
            }
        }

        // Write extended textures
        foreach(int i in extended)
        {
            WriteTextureElement(bakeWriter, data[i], "BESetA");
            numberWears++;
        }

        bakeWriter.WriteEndElement();
        bakeWriter.Flush();
        uploadData = bakeStream.ToArray();
    }

    // Asynchronous upload
    Util.FireAndForget(delegate
    {
        using(RestClient rc = new RestClient(m_URL))
        {
            rc.AddResourcePath("bakes/" + agentId.ToString());
            rc.POSTRequest(uploadData, m_Auth);
        }
    }, null, "XBakesModule.Store");
}
```

### Storage Workflow

1. **Data Validation**: Check for valid URL configuration and non-null texture data
2. **XML Generation**: Create structured XML document with texture metadata
3. **Texture Classification**: Separate standard (≤26) and extended (>26) texture indices
4. **Serialization**: Convert AssetBase objects to XML using XmlSerializer
5. **Asynchronous Upload**: Use fire-and-forget pattern for non-blocking HTTP POST
6. **Resource Cleanup**: Proper disposal of memory streams and upload data

## Texture Index Management

### Standard Texture Indices (0-26)
Standard Second Life/OpenSimulator texture slots:
- **0-5**: Body textures (head, upper body, lower body, etc.)
- **6-10**: Clothing layers (shirt, pants, shoes, etc.)
- **11-26**: Additional clothing and attachment points

### Extended Texture Indices (>26)
Extended texture slots for newer avatar features:
- **27+**: Mesh body parts, additional clothing layers, custom attachments

### Processing Logic
```csharp
// Classification during storage
if(data[i].TextureIndex > 26)
{
    extended.Add(i);  // Process as BESetA element
    continue;
}
// Process as BakedTexture element
```

## Performance Considerations

### Asynchronous Operations
```csharp
Util.FireAndForget(delegate
{
    // Storage operations run in background
    using(RestClient rc = new RestClient(m_URL))
    {
        rc.AddResourcePath("bakes/" + agentId.ToString());
        rc.POSTRequest(uploadData, m_Auth);
    }
}, null, "XBakesModule.Store");
```

### Memory Management
- **Streaming XML Processing**: Uses XmlTextReader for memory-efficient parsing
- **Proper Disposal**: Using statements ensure resource cleanup
- **Data Upload Cleanup**: Upload data is nullified after transmission

### Network Optimization
- **RESTful API**: Standard HTTP methods for efficient communication
- **XML Compression**: Structured data format with minimal overhead
- **Authentication Caching**: ServiceAuth framework provides efficient auth handling

## Security Considerations

### Service Authentication
- **Configurable Auth**: Support for multiple authentication methods
- **ServiceAuth Integration**: Leverages OpenSim's authentication framework
- **Secure Communication**: HTTPS support for encrypted data transmission

### Data Protection
- **UUID-based Access**: Agent-specific texture storage with UUID keys
- **Input Validation**: XML parsing with DTD processing disabled for security
- **Error Handling**: Graceful handling of malformed or missing data

### Network Security
- **External Service Trust**: Requires trust relationship with external storage service
- **Authentication Requirements**: Configurable authentication prevents unauthorized access
- **Transport Security**: HTTPS recommended for production deployments

## Error Handling and Resilience

### XML Processing Errors
```csharp
try
{
    using(XmlTextReader sr = new XmlTextReader(s))
    {
        // XML processing
    }
}
catch (XmlException)
{
    return null;  // Graceful degradation
}
```

### Network Error Handling
- **Connection Failures**: RestClient handles network timeouts and connectivity issues
- **Service Unavailability**: Module gracefully disables when service is unreachable
- **Authentication Failures**: ServiceAuth framework provides error handling

### Graceful Degradation
- **Service Unavailable**: Module disables functionality when external service is unreachable
- **Configuration Missing**: Module remains inactive without valid configuration
- **Data Corruption**: Returns null for corrupted or invalid texture data

## Integration Points

### Avatar Factory Integration
- Registers as IBakedTextureModule for avatar appearance management
- Integrates with scene's avatar appearance pipeline
- Provides texture caching for avatar factory operations

### Scene Management Integration
- Per-region module instances for localized functionality
- Integrates with scene lifecycle and avatar management
- Supports multiple concurrent regions with independent configurations

### Asset System Integration
- Works with AssetBase objects for texture data management
- Integrates with OpenSim's asset storage and retrieval systems
- Provides external caching layer for texture assets

## Use Cases and Applications

### Grid-Wide Avatar Consistency
- **Multi-Region Appearance**: Consistent avatar appearance across regions
- **Server Restart Persistence**: Avatar textures survive simulator restarts
- **Cross-Grid Compatibility**: Shared texture storage for grid interconnection

### Performance Optimization
- **Memory Reduction**: Offload texture storage from simulator memory
- **Bandwidth Optimization**: Cached textures reduce repeated downloads
- **Storage Efficiency**: Centralized storage eliminates duplicate textures

### Administrative Benefits
- **Centralized Management**: Single point for avatar texture administration
- **Backup and Recovery**: External storage provides backup capabilities
- **Monitoring and Analytics**: External service can provide usage analytics

## Dependencies

### Core Framework Dependencies
- `OpenSim.Framework` - Core data structures and utilities
- `OpenSim.Framework.ServiceAuth` - Authentication framework
- `OpenSim.Region.Framework.Interfaces` - Module interface contracts
- `OpenSim.Services.Interfaces` - Service interface definitions

### System Dependencies
- `System.Xml` - XML processing and serialization
- `System.Xml.Serialization` - AssetBase serialization
- `System.IO` - Stream processing for network operations
- `System.Text` - UTF8 encoding for text processing

### Network Dependencies
- `RestClient` - HTTP communication with external services
- Network connectivity to configured external storage service

## Troubleshooting

### Common Configuration Issues

1. **Module Not Loading**
   - Verify `[XBakes]` section exists with valid URL
   - Check that external service is accessible
   - Review startup logs for configuration errors

2. **Authentication Failures**
   - Verify ServiceAuth configuration is correct
   - Check external service authentication requirements
   - Test authentication credentials independently

3. **Service Connectivity Issues**
   - Verify external service URL is correct and accessible
   - Check network connectivity and firewall settings
   - Test service endpoints with external tools

### Common Runtime Issues

1. **Texture Retrieval Failures**
   - Check external service availability and response format
   - Verify agent UUID exists in external storage
   - Review XML parsing for format compatibility

2. **Storage Operation Failures**
   - Monitor fire-and-forget operations for exceptions
   - Check external service write permissions and capacity
   - Verify texture data integrity before storage

3. **Performance Issues**
   - Monitor external service response times
   - Check network latency to storage service
   - Review concurrent operation limits

### Debug Configuration

```ini
[XBakes]
URL = http://localhost:8080/
# Use local service for testing

# Enable debug logging if needed
[Logging]
LogLevel = DEBUG
```

### Log Analysis

Monitor module operation through log messages:
```
[XBakes]: read 5 textures for user 12345678-1234-1234-1234-123456789012
[XBakes]: stored 3 textures for user 12345678-1234-1234-1234-123456789012
```

### External Service Testing

Test service endpoints independently:
```bash
# Test retrieval
curl -X GET http://xbakes-server:8080/bakes/agent-uuid

# Test storage (with XML data)
curl -X POST http://xbakes-server:8080/bakes/agent-uuid \
     -H "Content-Type: application/xml" \
     -d @texture-data.xml
```

## Deployment Considerations

### External Service Requirements
- **REST API Compatibility**: Service must support GET/POST operations
- **XML Processing**: Must handle structured XML texture data
- **Authentication Support**: Compatible authentication mechanisms
- **Storage Capacity**: Adequate storage for texture assets

### Performance Requirements
- **Response Time**: Service should respond within acceptable timeouts
- **Concurrency**: Must handle multiple simultaneous requests
- **Availability**: High availability for production grid operations

### Security Requirements
- **Authentication**: Secure authentication between module and service
- **Data Encryption**: HTTPS transport encryption recommended
- **Access Control**: Service-level access controls for agent data

## Future Enhancement Opportunities

### Advanced Features
- **Texture Compression**: Automatic compression for reduced storage
- **Versioning Support**: Texture version management and history
- **Caching Strategies**: Local caching with external storage synchronization
- **Batch Operations**: Bulk texture upload and retrieval operations

### Performance Improvements
- **Connection Pooling**: Reuse HTTP connections for efficiency
- **Parallel Processing**: Concurrent texture operations
- **Compression Support**: HTTP compression for data transfer
- **Retry Logic**: Automatic retry for failed operations

### Monitoring and Management
- **Health Checks**: Service availability monitoring
- **Usage Metrics**: Texture storage and retrieval statistics
- **Administrative Tools**: Management interfaces for texture data
- **Integration APIs**: Extended API support for advanced features

## Conclusion

The XBakesModule provides essential external baked texture storage capabilities for OpenSimulator grids requiring centralized avatar appearance management. Its REST API integration, comprehensive XML serialization, and robust error handling make it suitable for production grid deployments. The module's configuration-based loading and authentication support ensure secure and flexible integration with external storage services while maintaining optimal performance and reliability.