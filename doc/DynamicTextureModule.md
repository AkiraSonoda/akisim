# DynamicTextureModule

## Overview

The DynamicTextureModule is a powerful texture generation and management system for OpenSimulator/Akisim that enables dynamic creation, modification, and application of textures to in-world objects. It provides a comprehensive framework for registering texture rendering plugins, managing texture updates, and optimizing texture reuse for improved performance.

## Architecture

The DynamicTextureModule implements multiple interfaces:
- `ISharedRegionModule` - Core module lifecycle management
- `IDynamicTextureManager` - Dynamic texture management interface

### Key Components

1. **Rendering Plugin System**
   - **Plugin Registration**: Framework for registering texture rendering plugins
   - **Content Type Routing**: Automatic routing of requests based on content type
   - **Asynchronous Processing**: Non-blocking texture generation and processing
   - **Extensible Architecture**: Support for custom rendering implementations

2. **Texture Management**
   - **Updater System**: Manages texture update operations and lifecycle
   - **Texture Caching**: Intelligent caching of reusable textures
   - **Asset Integration**: Seamless integration with OpenSim's asset system
   - **Alpha Blending**: Advanced alpha blending and texture composition

3. **Performance Optimization**
   - **Texture Reuse**: Configurable texture reuse to reduce redundant generation
   - **Cache Management**: Intelligent cache expiration and cleanup
   - **Data Size Optimization**: Smart handling of texture data sizes
   - **Resource Management**: Efficient memory and asset management

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
DynamicTextureModule = true
```

### Texture Configuration

Configuration is handled through the `[Textures]` section:

```ini
[Textures]
; Enable reuse of dynamic textures with identical parameters
ReuseDynamicTextures = true

; Enable reuse of small data size textures (may cause viewer issues with some legacy viewers)
ReuseDynamicLowDataTextures = false
```

### Configuration Options

- **ReuseDynamicTextures**: Enables caching and reuse of textures with identical generation parameters
- **ReuseDynamicLowDataTextures**: Controls whether small data size textures are reused (disabled by default due to viewer compatibility issues)

## Features

### Dynamic Texture Generation

The module supports multiple texture generation methods:

1. **URL-based Generation**: Generate textures from remote URLs
2. **Data-based Generation**: Generate textures from provided data
3. **Plugin Rendering**: Extensible rendering through registered plugins
4. **Multi-face Support**: Apply textures to individual faces or all faces of objects

### Texture Blending and Effects

1. **Alpha Blending**: Configurable alpha blending with existing textures
2. **Texture Composition**: Blend new textures with existing object textures
3. **Face-specific Application**: Apply textures to specific object faces
4. **Temporary Textures**: Support for temporary texture assets

### Performance Features

1. **Intelligent Caching**: Reuse textures with identical parameters
2. **Asynchronous Processing**: Non-blocking texture generation
3. **Resource Optimization**: Efficient memory and asset management
4. **Automatic Cleanup**: Configurable texture expiration and cleanup

## Technical Implementation

### Texture Generation Process

#### URL-based Texture Generation

1. **Request Reception**: Receive texture generation request with URL
2. **Plugin Routing**: Route request to appropriate rendering plugin
3. **Asynchronous Download**: Download content from specified URL
4. **Texture Processing**: Process downloaded content into texture format
5. **Asset Creation**: Create asset and apply to target object

#### Data-based Texture Generation

1. **Data Processing**: Process provided data through rendering plugin
2. **Cache Check**: Check for existing cached texture with same parameters
3. **Texture Generation**: Generate new texture if not cached
4. **Blending Operations**: Perform alpha blending if requested
5. **Asset Management**: Create and manage texture assets

### Rendering Plugin Architecture

The module supports a plugin-based rendering system:

- **Plugin Registration**: Plugins register for specific content types
- **Asynchronous Processing**: All rendering operations are asynchronous
- **Callback System**: Plugins return results through callback mechanisms
- **Resource Management**: Plugins handle their own resource requirements

### Texture Updater System

The DynamicTextureUpdater class manages texture update operations:

- **Update Tracking**: Tracks individual texture update operations
- **Asset Creation**: Creates new texture assets in the asset system
- **Object Application**: Applies textures to scene objects
- **Cleanup Management**: Handles cleanup of old textures

## API Methods

### Core Texture Operations

- `RegisterRender(string handleType, IDynamicTextureRender render)` - Register rendering plugin
- `ReturnData(UUID updaterId, IDynamicTexture texture)` - Handle completed texture generation
- `GetDrawStringSize(string contentType, string text, string fontName, int fontSize, out double xSize, out double ySize)` - Calculate text dimensions

### URL-based Generation

- `AddDynamicTextureURL(UUID simID, UUID primID, string contentType, string url, string extraParams)` - Basic URL texture generation
- `AddDynamicTextureURL(UUID simID, UUID primID, string contentType, string url, string extraParams, bool SetBlending, byte AlphaValue)` - URL generation with blending
- `AddDynamicTextureURL(UUID simID, UUID primID, string contentType, string url, string extraParams, bool SetBlending, int disp, byte AlphaValue, int face)` - Full URL generation control

### Data-based Generation

- `AddDynamicTextureData(UUID simID, UUID primID, string contentType, string data, string extraParams)` - Basic data texture generation
- `AddDynamicTextureData(UUID simID, UUID primID, string contentType, string data, string extraParams, bool SetBlending, byte AlphaValue)` - Data generation with blending
- `AddDynamicTextureData(UUID simID, UUID primID, string contentType, string data, string extraParams, bool SetBlending, int disp, byte AlphaValue, int face)` - Full data generation control

## Usage Examples

### Basic Texture Generation

```csharp
// Generate texture from URL
UUID textureID = dynamicTextureManager.AddDynamicTextureURL(
    regionID,
    objectID,
    "vector",
    "http://example.com/image.svg",
    "width:512,height:512"
);

// Generate texture from data
UUID textureID = dynamicTextureManager.AddDynamicTextureData(
    regionID,
    objectID,
    "vector",
    "MoveTo 100,100 LineTo 200,200",
    "width:256,height:256"
);
```

### Advanced Texture Effects

```csharp
// Generate texture with alpha blending
UUID textureID = dynamicTextureManager.AddDynamicTextureData(
    regionID,
    objectID,
    "vector",
    vectorData,
    "width:512,height:512",
    true,           // Enable blending
    128,            // Alpha value
    0               // Face number
);
```

## Performance Characteristics

### Caching Strategy

- **Parameter-based Caching**: Textures cached based on generation parameters
- **Automatic Expiration**: 24-hour default cache expiration
- **Memory Optimization**: Conservative cache strategy to minimize memory usage
- **Asset Integration**: Leverages OpenSim's asset caching system

### Resource Management

- **Asynchronous Processing**: Non-blocking operations for better performance
- **Memory Efficiency**: Proper disposal of temporary resources
- **Asset Cleanup**: Automatic cleanup of expired temporary assets
- **Connection Pooling**: Efficient handling of remote URL requests

### Optimization Features

- **Texture Reuse**: Avoid regenerating identical textures
- **Size-based Optimization**: Special handling for different texture sizes
- **Viewer Compatibility**: Optimizations for various viewer versions
- **Background Processing**: Texture generation in background threads

## Security Features

### Input Validation

- **URL Validation**: Proper validation of remote URLs
- **Data Validation**: Validation of input data before processing
- **Parameter Sanitization**: Sanitization of generation parameters
- **Resource Limits**: Protection against resource exhaustion

### Asset Security

- **Local Asset Creation**: Generated textures are local to the grid
- **Temporary Asset Management**: Proper handling of temporary assets
- **Permission Integration**: Integration with OpenSim's permission system
- **Audit Logging**: Comprehensive logging of texture operations

## Integration Points

### With Asset Services

- **Asset Creation**: Creates texture assets in the asset service
- **Cache Integration**: Integrates with asset caching mechanisms
- **Temporary Assets**: Proper handling of temporary asset lifecycle
- **Asset Cleanup**: Automatic cleanup of expired assets

### With Scene Objects

- **Object Integration**: Direct application of textures to scene objects
- **Face Management**: Support for multi-face texture application
- **Texture Entry Updates**: Proper updating of object texture entries
- **Permission Checking**: Respects object ownership and permissions

### With Rendering Plugins

- **Plugin Discovery**: Automatic discovery of available rendering plugins
- **Content Type Routing**: Automatic routing based on content types
- **Asynchronous Communication**: Non-blocking communication with plugins
- **Error Handling**: Robust error handling for plugin failures

## Rendering Plugins

### Supported Content Types

The module supports various content types through rendering plugins:

- **vector**: Vector graphics rendering (SVG-like commands)
- **image**: Image processing and manipulation
- **text**: Text rendering with font support
- **custom**: Custom rendering implementations

### Plugin Development

Rendering plugins must implement the `IDynamicTextureRender` interface:

```csharp
public interface IDynamicTextureRender
{
    string GetContentType();
    string GetName();
    bool SupportsAsynchronous();
    byte[] ConvertUrl(string url, string extraParams);
    byte[] ConvertData(string bodyData, string extraParams);
    bool AsyncConvertUrl(UUID id, string url, string extraParams);
    bool AsyncConvertData(UUID id, string bodyData, string extraParams);
    void GetDrawStringSize(string text, string fontName, int fontSize, out double xSize, out double ySize);
}
```

## Debugging and Troubleshooting

### Common Issues

1. **Plugin Not Found**: Verify rendering plugin is loaded and registered
2. **Texture Not Applied**: Check object permissions and asset service connectivity
3. **Performance Issues**: Monitor cache hit rates and texture reuse effectiveness
4. **Memory Usage**: Monitor texture cache size and cleanup frequency

### Diagnostic Tools

1. **Debug Logging**: Enable detailed logging for texture operations
2. **Cache Analysis**: Monitor cache performance and hit rates
3. **Asset Monitoring**: Track asset creation and cleanup operations
4. **Performance Metrics**: Analyze texture generation timing

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
DynamicTextureModule = true

[Textures]
ReuseDynamicTextures = true
ReuseDynamicLowDataTextures = false
```

## Use Cases

### Script-based Texture Generation

- **LSL Integration**: Support for LSL scripts generating dynamic textures
- **Interactive Displays**: Create dynamic information displays
- **User-generated Content**: Enable users to create custom textures
- **Real-time Updates**: Update textures based on real-time data

### Advanced Visual Effects

- **Animated Textures**: Create pseudo-animated effects through texture updates
- **Data Visualization**: Render data as visual representations
- **Custom UI Elements**: Create custom user interface elements
- **Artistic Applications**: Support for artistic and creative applications

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Deployment Considerations

- **Asset Service**: Ensure asset service is properly configured
- **Cache Configuration**: Configure appropriate cache settings
- **Rendering Plugins**: Ensure required rendering plugins are available
- **Performance Monitoring**: Monitor texture generation performance

## Configuration Examples

### Basic Configuration

```ini
[Modules]
DynamicTextureModule = true

[Textures]
ReuseDynamicTextures = true
ReuseDynamicLowDataTextures = false
```

### Performance-optimized Configuration

```ini
[Modules]
DynamicTextureModule = true

[Textures]
ReuseDynamicTextures = true
ReuseDynamicLowDataTextures = false

[AssetCache]
MemoryCacheEnabled = true
FileCacheEnabled = true
```

### Development Configuration

```ini
[Modules]
DynamicTextureModule = true

[Textures]
ReuseDynamicTextures = false
ReuseDynamicLowDataTextures = true

[Logging]
LogLevel = DEBUG
```

## Best Practices

### Performance Optimization

1. **Enable Texture Reuse**: Use `ReuseDynamicTextures = true` for better performance
2. **Cache Monitoring**: Monitor cache hit rates and effectiveness
3. **Resource Management**: Properly manage temporary assets and cleanup
4. **Plugin Efficiency**: Ensure rendering plugins are optimized

### Development Guidelines

1. **Error Handling**: Implement robust error handling in rendering plugins
2. **Resource Cleanup**: Ensure proper cleanup of temporary resources
3. **Asynchronous Design**: Use asynchronous patterns for better responsiveness
4. **Testing**: Thoroughly test texture generation under various conditions

### Operational Practices

1. **Monitoring**: Monitor texture generation performance and errors
2. **Cache Management**: Regular monitoring of cache performance
3. **Resource Usage**: Monitor memory and asset usage patterns
4. **Plugin Management**: Keep rendering plugins updated and tested

## Future Enhancements

### Potential Improvements

1. **Enhanced Caching**: More sophisticated caching algorithms
2. **Compression**: Texture compression for better performance
3. **Distributed Generation**: Support for distributed texture generation
4. **Advanced Effects**: Additional blending and effect options

### Compatibility Considerations

1. **Viewer Compatibility**: Continued support for various viewer versions
2. **Plugin API Evolution**: Maintain backward compatibility for plugins
3. **Asset Format Support**: Support for new asset formats
4. **Performance Scaling**: Enhanced scalability for larger deployments