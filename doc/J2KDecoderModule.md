# J2KDecoderModule

## Overview

The J2KDecoderModule is a critical texture processing system for OpenSimulator/Akisim that provides essential JPEG2000 decoding functionality for the virtual world platform. It serves as the primary interface for decoding JPEG2000 texture assets, analyzing layer boundaries, and converting compressed texture data into usable formats for rendering, texture sending, and asset processing. This module is fundamental to the texture pipeline and supports both the CSJ2K and OpenJPEG decoding engines for maximum compatibility and performance.

## Architecture

The J2KDecoderModule implements multiple interfaces:
- `ISharedRegionModule` - Shared module lifecycle management across all regions
- `IJ2KDecoder` - Primary interface for JPEG2000 decoding operations

### Key Components

1. **Dual Decoder Engine Support**
   - **CSJ2K Integration**: Pure C# JPEG2000 decoder implementation
   - **OpenJPEG Integration**: Native OpenJPEG library wrapper for high performance
   - **Configurable Selection**: Runtime selection between decoder engines
   - **Fallback Mechanisms**: Graceful handling of decoder failures

2. **Layer Boundary Analysis**
   - **Progressive Decoding**: Support for JPEG2000 progressive/layered compression
   - **Layer Detection**: Automatic detection and parsing of quality layers
   - **Boundary Calculation**: Precise calculation of layer start/end positions
   - **Metadata Extraction**: Layer information extraction for texture streaming

3. **Caching System**
   - **Memory Caching**: In-memory cache for decoded layer information
   - **Persistent Caching**: Asset cache integration for long-term storage
   - **Expiring Cache**: Time-based expiration for memory efficiency
   - **Cache Serialization**: Efficient serialization of layer data

4. **Asynchronous Processing**
   - **Non-blocking Decoding**: Asynchronous texture decoding operations
   - **Callback System**: Notification system for decode completion
   - **Request Queuing**: Intelligent queuing of concurrent decode requests
   - **Thread Safety**: Thread-safe operations for concurrent access

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
[Modules]
J2KDecoderModule = true
```

### Decoder Engine Selection

Configure in `[Startup]` section:
```ini
[Startup]
; Use CSJ2K (Pure C#) decoder instead of OpenJPEG (default: true)
UseCSJ2K = true
```

### Engine Characteristics

#### CSJ2K Decoder (Default)
- **Pure C#**: No native library dependencies
- **Cross-platform**: Runs on all .NET-supported platforms
- **Memory Efficient**: Better memory management for large textures
- **Stability**: More stable on some platforms

#### OpenJPEG Decoder
- **High Performance**: Optimized native code for faster decoding
- **Full Feature Set**: Complete JPEG2000 feature support
- **Component Analysis**: Provides component count information
- **Legacy Support**: Traditional OpenSim decoder choice

## Features

### Advanced JPEG2000 Processing

The module provides comprehensive JPEG2000 handling capabilities:

1. **Multi-Engine Support**
   - **CSJ2K Engine**: Pure C# implementation with excellent compatibility
   - **OpenJPEG Engine**: Native code implementation for maximum performance
   - **Runtime Switching**: Configurable engine selection at startup
   - **Fallback Processing**: Automatic fallback to default layers on failure

2. **Layer Boundary Analysis**
   - **Progressive Quality**: Support for JPEG2000 progressive quality layers
   - **Boundary Detection**: Precise detection of layer boundaries in compressed data
   - **Streaming Support**: Enables efficient texture streaming to viewers
   - **Quality Control**: Layer-based quality control for bandwidth management

3. **Image Conversion**
   - **Direct Decoding**: Direct conversion of JPEG2000 data to System.Drawing.Image
   - **Format Support**: Support for various target image formats
   - **Memory Optimization**: Efficient memory handling during conversion
   - **Error Handling**: Robust error handling for corrupted or invalid data

### Caching and Performance

1. **Multi-level Caching**
   - **Memory Cache**: Fast in-memory cache with configurable expiration
   - **Asset Cache**: Integration with OpenSim's asset caching system
   - **Persistence**: Long-term storage of decoded layer information
   - **Automatic Cleanup**: Intelligent cache cleanup and memory management

2. **Asynchronous Operations**
   - **Non-blocking Processing**: Asynchronous decoding to prevent server blocking
   - **Callback Notifications**: Event-driven completion notifications
   - **Request Batching**: Efficient handling of multiple concurrent requests
   - **Thread Safety**: Safe concurrent access from multiple threads

## Technical Implementation

### Decoding Pipeline Architecture

#### Request Processing Flow

1. **Request Reception**: Accept decode request with asset ID and JPEG2000 data
2. **Cache Check**: Check memory and persistent cache for existing results
3. **Queue Management**: Add to notification list or initiate new decode
4. **Engine Selection**: Use configured decoder (CSJ2K or OpenJPEG)
5. **Layer Analysis**: Analyze layer boundaries in compressed data
6. **Cache Storage**: Store results in both memory and persistent cache
7. **Notification**: Notify all waiting callbacks of completion

#### CSJ2K Decoding Process

```csharp
// CSJ2K layer boundary detection
List<int> layerStarts;
using (MemoryStream ms = new MemoryStream(j2kData))
{
    layerStarts = CSJ2K.J2kImage.GetLayerBoundaries(ms);
}

// Convert layer starts to OpenJPEG.J2KLayerInfo format
layers = new OpenJPEG.J2KLayerInfo[layerStarts.Count];
for (int i = 0; i < layerStarts.Count; i++)
{
    OpenJPEG.J2KLayerInfo layer = new OpenJPEG.J2KLayerInfo();
    layer.Start = (i == 0) ? 0 : layerStarts[i];
    layer.End = (i == layerStarts.Count - 1) ? j2kData.Length : layerStarts[i + 1] - 1;
    layers[i] = layer;
}
```

#### OpenJPEG Decoding Process

```csharp
// OpenJPEG layer boundary analysis
if (!OpenJPEG.DecodeLayerBoundaries(j2kData, out layers, out components))
{
    m_log.Warn("OpenJPEG failed to decode texture " + assetID);
    decodedSuccessfully = false;
}
```

### Caching System Implementation

#### Memory Cache Architecture

The module uses a sophisticated expiring cache for immediate access:

```csharp
private readonly ThreadedClasses.ExpiringCache<UUID, OpenJPEG.J2KLayerInfo[]> m_decodedCache =
    new ThreadedClasses.ExpiringCache<UUID,OpenJPEG.J2KLayerInfo[]>(30);

// Cache with 1-minute expiration
m_decodedCache.AddOrUpdate(AssetId, Layers, TimeSpan.FromMinutes(1));
```

#### Persistent Cache Integration

Integration with OpenSim's asset cache for long-term storage:

```csharp
// Create cache asset
AssetBase layerDecodeAsset = new AssetBase(assetID, assetID, (sbyte)AssetType.Notecard, m_CreatorID.ToString());
layerDecodeAsset.Local = true;
layerDecodeAsset.Temporary = true;

// Serialize layer data
StringBuilder stringResult = new StringBuilder();
for (int i = 0; i < Layers.Length; i++)
{
    stringResult.AppendFormat("{0}|{1}|{2}{3}",
        Layers[i].Start, Layers[i].End, Layers[i].End - Layers[i].Start, strEnd);
}
layerDecodeAsset.Data = Util.UTF8.GetBytes(stringResult.ToString());
Cache.Cache(layerDecodeAsset);
```

### Asynchronous Processing System

#### Request Queuing and Notification

The module implements sophisticated request management:

```csharp
// Thread-safe notification management
lock (m_notifyList)
{
    if (m_notifyList.ContainsKey(assetID))
    {
        // Add to existing notification list
        m_notifyList[assetID].Add(callback);
    }
    else
    {
        // Create new notification list and start decode
        List<DecodedCallback> notifylist = new List<DecodedCallback>();
        notifylist.Add(callback);
        m_notifyList.Add(assetID, notifylist);
        decode = true;
    }
}

// Start asynchronous decode
if (decode)
    Util.FireAndForget(delegate { Decode(assetID, j2kData); }, null, "J2KDecoderModule.BeginDecode");
```

#### Callback Notification System

Efficient notification of all waiting clients:

```csharp
// Notify all interested parties
lock (m_notifyList)
{
    if (m_notifyList.ContainsKey(assetID))
    {
        foreach (DecodedCallback d in m_notifyList[assetID])
        {
            if (d != null)
                d.DynamicInvoke(assetID, layers);
        }
        m_notifyList.Remove(assetID);
    }
}
```

### Error Handling and Fallback

#### Default Layer Generation

When decoding fails, the module generates sensible default layers:

```csharp
private OpenJPEG.J2KLayerInfo[] CreateDefaultLayers(int j2kLength)
{
    OpenJPEG.J2KLayerInfo[] layers = new OpenJPEG.J2KLayerInfo[5];

    // Layer boundaries based on empirical texture analysis
    layers[0].Start = 0;
    layers[1].Start = (int)((float)j2kLength * 0.02f);  // 2%
    layers[2].Start = (int)((float)j2kLength * 0.05f);  // 5%
    layers[3].Start = (int)((float)j2kLength * 0.20f);  // 20%
    layers[4].Start = (int)((float)j2kLength * 0.50f);  // 50%

    // Calculate end boundaries
    layers[0].End = layers[1].Start - 1;
    layers[1].End = layers[2].Start - 1;
    layers[2].End = layers[3].Start - 1;
    layers[3].End = layers[4].Start - 1;
    layers[4].End = j2kLength;

    return layers;
}
```

## Performance Characteristics

### Decoding Performance

- **CSJ2K Performance**: Balanced performance with excellent stability
- **OpenJPEG Performance**: Maximum decoding speed for high-throughput scenarios
- **Cache Efficiency**: Eliminates redundant decoding through intelligent caching
- **Asynchronous Operations**: Non-blocking operations maintain server responsiveness

### Memory Management

- **Expiring Cache**: Automatic memory cleanup with configurable expiration
- **Efficient Serialization**: Compact serialization format for cache storage
- **Request Batching**: Efficient handling of multiple requests for same asset
- **Resource Cleanup**: Proper disposal of resources and memory management

### Scalability Features

- **Thread Safety**: Safe concurrent access from multiple regions and threads
- **Cache Sharing**: Shared cache across all regions for efficiency
- **Load Balancing**: Intelligent load balancing of decode operations
- **Resource Monitoring**: Built-in protection against resource exhaustion

## API Methods

### Core Interface Methods

#### IJ2KDecoder Interface

- `BeginDecode(UUID assetID, byte[] j2kData, DecodedCallback callback)` - Asynchronous decode initiation
- `Decode(UUID assetID, byte[] j2kData)` - Synchronous decode operation
- `Decode(UUID assetID, byte[] j2kData, out OpenJPEG.J2KLayerInfo[] layers, out int components)` - Detailed decode with layer info
- `DecodeToImage(byte[] j2kData)` - Direct conversion to System.Drawing.Image

#### Internal Processing Methods

- `DoJ2KDecode(UUID assetID, byte[] j2kData, out OpenJPEG.J2KLayerInfo[] layers, out int components)` - Core decode logic
- `CreateDefaultLayers(int j2kLength)` - Fallback layer generation
- `SaveFileCacheForAsset(UUID AssetId, OpenJPEG.J2KLayerInfo[] Layers)` - Cache storage
- `TryLoadCacheForAsset(UUID AssetId, out OpenJPEG.J2KLayerInfo[] Layers)` - Cache retrieval

### Callback Delegates

```csharp
public delegate void J2KDecodeDelegate(UUID assetID);
public delegate void DecodedCallback(UUID assetID, OpenJPEG.J2KLayerInfo[] layers);
```

## Usage Examples

### Basic Asynchronous Decoding

```csharp
// Request asynchronous decode
j2kDecoder.BeginDecode(textureAssetID, j2kData, (assetID, layers) =>
{
    // Handle decoded layer information
    if (layers != null && layers.Length > 0)
    {
        Console.WriteLine($"Decoded {layers.Length} layers for texture {assetID}");
        foreach (var layer in layers)
        {
            Console.WriteLine($"Layer: {layer.Start}-{layer.End} ({layer.End - layer.Start} bytes)");
        }
    }
});
```

### Synchronous Decoding with Layer Analysis

```csharp
// Synchronous decode with detailed layer information
OpenJPEG.J2KLayerInfo[] layers;
int components;
bool success = j2kDecoder.Decode(textureAssetID, j2kData, out layers, out components);

if (success && layers != null)
{
    // Process layer information for progressive streaming
    for (int i = 0; i < layers.Length; i++)
    {
        int layerSize = layers[i].End - layers[i].Start;
        Console.WriteLine($"Quality layer {i}: {layerSize} bytes");
    }
}
```

### Image Conversion

```csharp
// Convert JPEG2000 data directly to image
using (Image decodedImage = j2kDecoder.DecodeToImage(j2kData))
{
    if (decodedImage != null)
    {
        // Process the decoded image
        Console.WriteLine($"Decoded image: {decodedImage.Width}x{decodedImage.Height}");
        // Save, display, or further process the image
        decodedImage.Save("decoded_texture.png", ImageFormat.Png);
    }
}
```

### Error Handling Example

```csharp
// Robust decoding with error handling
try
{
    j2kDecoder.BeginDecode(assetID, j2kData, (id, layers) =>
    {
        if (layers != null && layers.Length > 0)
        {
            // Successful decode
            ProcessLayers(id, layers);
        }
        else
        {
            // Failed decode - layers will be default fallback layers
            Console.WriteLine($"Decode failed for {id}, using fallback layers");
            ProcessFallbackLayers(id);
        }
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Exception during decode request: {ex.Message}");
}
```

## Integration Points

### With Texture Sending System

- **Layer Streaming**: Provides layer boundaries for progressive texture streaming
- **Quality Control**: Enables quality-based texture delivery
- **Bandwidth Optimization**: Supports adaptive quality based on connection speed
- **Viewer Compatibility**: Compatible with all OpenSim-compatible viewers

### With Asset System

- **Asset Processing**: Direct integration with asset service for texture processing
- **Cache Integration**: Utilizes asset cache for persistent storage
- **Metadata Storage**: Stores decoded layer metadata as cache assets
- **Asset Validation**: Validates texture assets during processing

### With Rendering System

- **Map Tile Generation**: Supports map tile texture processing
- **Mesh Rendering**: Provides texture decoding for mesh and sculpt textures
- **Dynamic Textures**: Enables processing of dynamically generated textures
- **Material System**: Supports material texture processing

### With Region Management

- **Cross-Region Sharing**: Shared decoder across all regions in instance
- **Resource Efficiency**: Prevents duplicate decoding across regions
- **Centralized Caching**: Unified cache for all regions
- **Performance Optimization**: Optimized resource usage across regions

## Security Features

### Input Validation

- **Data Validation**: Comprehensive validation of JPEG2000 input data
- **Size Limits**: Protection against excessively large texture data
- **Format Verification**: Verification of JPEG2000 format compliance
- **Asset Verification**: Integration with asset system security measures

### Error Handling

- **Exception Safety**: Comprehensive exception handling throughout pipeline
- **Graceful Degradation**: Continues operation even with corrupted textures
- **Resource Protection**: Protection against resource exhaustion
- **Logging Integration**: Detailed logging for security monitoring

### Memory Protection

- **Bounded Caching**: Configurable cache limits to prevent memory exhaustion
- **Automatic Cleanup**: Automatic cleanup of expired cache entries
- **Resource Monitoring**: Built-in monitoring of memory usage
- **Leak Prevention**: Proper disposal patterns to prevent memory leaks

## Debugging and Troubleshooting

### Common Issues

1. **Decode Failures**: Check JPEG2000 data validity and decoder configuration
2. **Memory Issues**: Monitor cache size and expiration settings
3. **Performance Problems**: Consider switching between CSJ2K and OpenJPEG
4. **Cache Corruption**: Check asset cache integrity and storage

### Diagnostic Tools

1. **Debug Logging**: Comprehensive debug output for decode operations
2. **Performance Metrics**: Built-in timing and success rate tracking
3. **Cache Analysis**: Tools for analyzing cache hit rates and efficiency
4. **Error Reporting**: Detailed error messages for failed operations

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
J2KDecoderModule = true

[Startup]
UseCSJ2K = true
```

## Use Cases

### Texture Streaming

- **Progressive Loading**: Layer-based progressive texture loading
- **Bandwidth Optimization**: Quality-based delivery for different connection speeds
- **Mobile Support**: Optimized texture delivery for mobile viewers
- **Large Texture Support**: Efficient handling of high-resolution textures

### Content Creation

- **Texture Processing**: Processing of uploaded texture assets
- **Quality Analysis**: Analysis of texture quality and compression
- **Format Conversion**: Conversion between texture formats
- **Asset Validation**: Validation of texture assets during upload

### Rendering Applications

- **Map Tile Generation**: Texture processing for map tile creation
- **Mesh Rendering**: Texture support for mesh and sculpted objects
- **Dynamic Content**: Support for dynamically generated textures
- **Material Processing**: Advanced material texture processing

### Performance Optimization

- **Cache Management**: Efficient caching for high-traffic scenarios
- **Resource Optimization**: Optimized resource usage for large regions
- **Scalability**: Support for high-concurrency texture processing
- **Load Balancing**: Efficient distribution of decode operations

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the CoreModuleFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[Modules]` configuration section includes `J2KDecoderModule = true`
- Test texture decoding functionality after deployment
- Update any custom texture processing code
- Validate both CSJ2K and OpenJPEG decoder paths

### Deployment Considerations

- **Decoder Libraries**: Ensure CSJ2K and OpenJPEG libraries are available
- **Memory Planning**: Plan for texture processing memory requirements
- **Cache Storage**: Configure adequate cache storage for texture metadata
- **Performance Testing**: Test with realistic texture loads

## Configuration Examples

### Basic JPEG2000 Decoding

```ini
[Modules]
J2KDecoderModule = true
```

### CSJ2K Decoder Configuration

```ini
[Modules]
J2KDecoderModule = true

[Startup]
UseCSJ2K = true
```

### OpenJPEG Decoder Configuration

```ini
[Modules]
J2KDecoderModule = true

[Startup]
UseCSJ2K = false
```

### Development Configuration

```ini
[Modules]
J2KDecoderModule = true

[Startup]
UseCSJ2K = true

[Logging]
LogLevel = DEBUG
```

## Best Practices

### Performance Guidelines

1. **Decoder Selection**: Choose CSJ2K for stability, OpenJPEG for performance
2. **Cache Management**: Monitor cache performance and adjust expiration times
3. **Memory Usage**: Monitor memory usage patterns for large texture loads
4. **Concurrent Access**: Design for efficient concurrent texture processing

### Operational Practices

1. **Monitoring**: Monitor decode success rates and performance metrics
2. **Cache Maintenance**: Regular maintenance of cache storage systems
3. **Error Handling**: Implement robust error handling in texture processing code
4. **Resource Planning**: Plan for peak texture processing loads

### Development Guidelines

1. **Asynchronous Usage**: Use asynchronous decode methods for better responsiveness
2. **Error Handling**: Always handle decode failures and fallback scenarios
3. **Resource Cleanup**: Properly dispose of decoded images and resources
4. **Cache Efficiency**: Design applications to leverage cache efficiently

## Future Enhancements

### Potential Improvements

1. **Hardware Acceleration**: GPU-accelerated JPEG2000 decoding
2. **Advanced Caching**: More sophisticated caching strategies
3. **Format Support**: Support for additional texture compression formats
4. **Performance Analytics**: Enhanced performance monitoring and analytics

### Compatibility Considerations

1. **Decoder Updates**: Stay current with CSJ2K and OpenJPEG updates
2. **Format Evolution**: Adapt to evolving JPEG2000 standards
3. **Platform Support**: Maintain compatibility across .NET platforms
4. **Performance Standards**: Optimization for modern hardware capabilities