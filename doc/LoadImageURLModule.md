# LoadImageURLModule

## Overview

The LoadImageURLModule is a dynamic texture rendering system for OpenSimulator/Akisim that enables real-time loading and processing of images from remote URLs. It serves as a crucial component for script-based texture generation, allowing LSL scripts to dynamically load images from the internet and apply them as textures to in-world objects, creating dynamic displays, signage, and interactive visual content.

## Architecture

The LoadImageURLModule implements multiple interfaces:
- `ISharedRegionModule` - Shared module lifecycle management across all regions
- `IDynamicTextureRender` - Dynamic texture rendering interface for integration with the texture system

### Key Components

1. **HTTP Image Retrieval**
   - **Asynchronous Downloads**: Non-blocking image downloads from remote URLs
   - **Redirect Handling**: Automatic following of HTTP redirects (301, 302, 303, 307)
   - **Proxy Support**: Integration with HTTP proxy configuration
   - **URL Filtering**: Security filtering through OutboundUrlFilter system

2. **Image Processing Pipeline**
   - **Format Support**: Automatic handling of various image formats (JPEG, PNG, GIF, BMP, etc.)
   - **Size Normalization**: Automatic scaling to power-of-two texture dimensions
   - **JPEG2000 Encoding**: Conversion to OpenSim's native texture format
   - **Quality Optimization**: Optimized encoding for virtual world display

3. **Dynamic Texture Integration**
   - **DynamicTextureManager Integration**: Seamless integration with OpenSim's texture system
   - **Callback System**: Asynchronous completion callbacks for texture generation
   - **Request Tracking**: UUID-based request tracking and management
   - **Error Handling**: Robust error handling and failure reporting

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
[Modules]
LoadImageURLModule = true
```

### HTTP Proxy Configuration

Configure in `[Startup]` section:
```ini
[Startup]
; HTTP proxy for outbound requests
HttpProxy = http://proxy.example.com:8080

; Proxy exceptions (semicolon-separated list)
HttpProxyExceptions = localhost;127.0.0.1;internal.domain.com
```

### URL Filtering Configuration

Configure through OutboundUrlFilter settings:
```ini
[XEngine]
; Allowed HTTP request schemes
HttpRequestUrlSchemes = http,https

; Allowed hosts (wildcards supported)
AllowedOutboundHttpHosts = *
```

## Features

### Image Loading Capabilities

The module supports comprehensive image loading features:

1. **Multiple Formats**: Support for all standard web image formats
   - JPEG, PNG, GIF, BMP, TIFF
   - WebP (where system support is available)
   - ICO and other bitmap formats

2. **Size Processing**: Intelligent image sizing
   - Automatic scaling to appropriate texture dimensions
   - Power-of-two sizing (32x32, 64x64, 128x128, 256x256, 512x512, 1024x1024)
   - Aspect ratio preservation during scaling
   - Memory-efficient processing for large images

3. **Quality Control**: Optimized texture generation
   - JPEG2000 encoding for optimal quality and compression
   - Lossless mode support for critical images
   - Progressive loading for large textures
   - Memory usage optimization

### Network Features

1. **Asynchronous Processing**: Non-blocking operations for server performance
2. **Redirect Support**: Automatic handling of HTTP redirects
3. **Timeout Management**: Configurable timeouts for network operations
4. **Error Recovery**: Graceful handling of network failures
5. **Proxy Integration**: Support for corporate proxy environments

## Technical Implementation

### Image Loading Process

#### Request Initiation

1. **URL Validation**: Verify URL format and security filtering
2. **Request Creation**: Create HTTP web request with appropriate headers
3. **Proxy Configuration**: Apply proxy settings if configured
4. **Asynchronous Execution**: Begin asynchronous download operation
5. **Request Tracking**: Track request with unique identifier

#### Image Processing Workflow

1. **Download Completion**: Receive HTTP response and image data
2. **Format Detection**: Automatic detection of image format
3. **Size Analysis**: Analyze image dimensions for scaling requirements
4. **Size Calculation**: Determine appropriate texture dimensions
5. **Image Scaling**: Scale image to power-of-two dimensions if needed
6. **JPEG2000 Encoding**: Convert to OpenSim's native texture format
7. **Callback Execution**: Return processed texture to requesting system

### Size Normalization Logic

The module implements intelligent size normalization:

```csharp
// Size determination logic
if (image.Height < 64 && image.Width < 64)
    newSize = 32x32;
else if (image.Height < 128 && image.Width < 128)
    newSize = 64x64;
else if (image.Height < 256 && image.Width < 256)
    newSize = 128x128;
else if (image.Height < 512 && image.Width < 512)
    newSize = 256x256;
else if (image.Height < 1024 && image.Width < 1024)
    newSize = 512x512;
else
    newSize = 1024x1024;
```

### HTTP Redirect Handling

The module automatically handles common HTTP redirect scenarios:

- **301 Moved Permanently**: Permanent redirects
- **302 Found**: Temporary redirects
- **303 See Other**: See other redirects
- **307 Temporary Redirect**: Temporary redirects with method preservation

### Error Handling

Comprehensive error handling covers:

- **Network Failures**: Connection timeouts, DNS failures
- **HTTP Errors**: 404, 403, 500, and other HTTP error codes
- **Image Format Errors**: Corrupted or unsupported image formats
- **Memory Limitations**: Out-of-memory conditions during processing
- **Processing Failures**: JPEG2000 encoding failures

## API Integration

### IDynamicTextureRender Interface

The module implements the standard dynamic texture interface:

#### Core Methods

- `GetName()` - Returns "LoadImageURL" identifier
- `GetContentType()` - Returns "image" content type
- `SupportsAsynchronous()` - Returns true for async support
- `AsyncConvertUrl(UUID id, string url, string extraParams)` - Main image loading method

#### Texture Generation

- **Input**: URL string and optional parameters
- **Processing**: Asynchronous image download and conversion
- **Output**: JPEG2000 texture data with appropriate dimensions
- **Callback**: Completion notification through DynamicTextureManager

### Integration with DynamicTextureManager

The module registers with the DynamicTextureManager as an "image" renderer:

1. **Registration**: Automatic registration during region loading
2. **Request Routing**: DynamicTextureManager routes image requests to this module
3. **Callback Handling**: Completion callbacks through ReturnData method
4. **Error Reporting**: Failure notification and error handling

## Usage Examples

### Basic LSL Script Usage

```lsl
// Load image from URL and apply to object face
key texture_request = llHTTPRequest("http://example.com/image.jpg", [], "");

// In the http_response event:
http_response(key request_id, integer status, list metadata, string body)
{
    if (status == 200)
    {
        // Apply loaded texture to face 0
        llSetTexture(body, 0);
    }
}
```

### Advanced LSL Usage with Parameters

```lsl
// Load image with specific parameters
string url = "http://example.com/dynamic-image.png";
string params = "width:256,height:256";
vector size = <2.0, 2.0, 0.0>;

// Request dynamic texture
llSetText("Loading image...", <1,1,1>, 1.0);
key request = llHTTPRequest(url, [], params);
```

### Script Integration Example

```lsl
// Dynamic signage system
list image_urls = [
    "http://content.example.com/sign1.jpg",
    "http://content.example.com/sign2.jpg",
    "http://content.example.com/sign3.jpg"
];

integer current_image = 0;

default
{
    state_entry()
    {
        llSetTimerEvent(30.0); // Change image every 30 seconds
        load_next_image();
    }

    timer()
    {
        current_image = (current_image + 1) % llGetListLength(image_urls);
        load_next_image();
    }

    load_next_image()
    {
        string url = llList2String(image_urls, current_image);
        // Request to load image from URL
        // Implementation depends on specific LSL texture loading functions
    }
}
```

## Performance Characteristics

### Memory Management

- **Streaming Processing**: Images processed in streaming fashion to minimize memory usage
- **Automatic Cleanup**: Automatic disposal of image resources after processing
- **Size Limitations**: Built-in protection against excessively large images
- **Memory Pool**: Efficient memory allocation for image processing operations

### Network Optimization

- **Asynchronous Operations**: Non-blocking network operations
- **Connection Reuse**: Efficient HTTP connection management
- **Timeout Control**: Configurable timeouts to prevent hanging requests
- **Proxy Support**: Efficient proxy utilization for corporate environments

### Processing Efficiency

- **Format Detection**: Fast image format detection and validation
- **Optimized Scaling**: Efficient image scaling algorithms
- **JPEG2000 Encoding**: High-performance texture encoding
- **Parallel Processing**: Support for concurrent image processing requests

## Security Features

### URL Filtering

- **OutboundUrlFilter Integration**: Comprehensive URL filtering system
- **Protocol Validation**: Restriction to safe protocols (HTTP/HTTPS)
- **Domain Filtering**: Configurable domain whitelist/blacklist
- **Path Validation**: URL path filtering for additional security

### Content Validation

- **Image Format Validation**: Verification of image file formats
- **Size Limitations**: Protection against extremely large images
- **Content Type Verification**: HTTP content-type header validation
- **Malformed Data Protection**: Protection against corrupted image data

### Network Security

- **Proxy Support**: Integration with corporate security infrastructure
- **SSL/TLS Support**: Secure HTTPS connections for encrypted image transfer
- **Timeout Protection**: Prevention of resource exhaustion through timeouts
- **Request Rate Limiting**: Protection against abuse through request tracking

## Integration Points

### With DynamicTextureModule

- **Renderer Registration**: Automatic registration as image content renderer
- **Request Delegation**: DynamicTextureModule delegates image requests to this module
- **Callback Integration**: Seamless integration with texture completion callbacks
- **Parameter Passing**: Support for texture generation parameters

### With LSL Scripting System

- **Script Integration**: Scripts can request image loading through LSL functions
- **Event Generation**: Completion events for script notification
- **Error Reporting**: Script-accessible error reporting
- **Texture Application**: Integration with LSL texture application functions

### With Asset System

- **Texture Asset Creation**: Generated textures become standard texture assets
- **Asset Storage**: Integration with OpenSim's asset storage system
- **Cache Integration**: Leverage asset caching for performance
- **UUID Management**: Proper UUID generation and management for textures

## Debugging and Troubleshooting

### Common Issues

1. **Images Not Loading**: Check URL accessibility and network connectivity
2. **Size Issues**: Verify image dimensions are within reasonable limits
3. **Format Problems**: Ensure image format is supported by system
4. **Network Timeouts**: Check network configuration and proxy settings

### Diagnostic Tools

1. **Debug Logging**: Comprehensive debug output for troubleshooting
2. **Network Monitoring**: Monitor HTTP requests and responses
3. **Image Analysis**: Tools for analyzing processed image properties
4. **Performance Metrics**: Monitoring of processing times and success rates

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
LoadImageURLModule = true
```

## Use Cases

### Digital Signage

- **Dynamic Advertising**: Real-time advertising content from web sources
- **Information Displays**: Live information feeds and announcements
- **Event Promotion**: Dynamic event posters and promotional materials
- **Menu Systems**: Restaurant menus and pricing displays

### Educational Applications

- **Live Content**: Real-time educational content from web sources
- **Interactive Displays**: Dynamic educational materials and presentations
- **Research Visualization**: Live data visualization and research results
- **Student Projects**: Student-generated content displayed dynamically

### Entertainment Systems

- **Game Content**: Dynamic game assets and promotional materials
- **Media Walls**: Large-scale media displays and entertainment content
- **User-Generated Content**: Community-generated images and artwork
- **Interactive Art**: Dynamic art installations and creative displays

### Business Applications

- **Corporate Communications**: Company announcements and communications
- **Product Displays**: Dynamic product catalogs and promotional materials
- **Data Visualization**: Real-time business metrics and dashboard displays
- **Customer Information**: Dynamic customer information and support content

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the CoreModuleFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify HTTP proxy configuration if used
- Test URL filtering settings
- Validate image loading functionality
- Update any custom scripts that depend on the module

### Deployment Considerations

- **Network Access**: Ensure outbound HTTP/HTTPS access is available
- **Proxy Configuration**: Configure proxy settings if required
- **Security Policies**: Establish URL filtering policies
- **Performance Planning**: Plan for image processing resource requirements

## Configuration Examples

### Basic Configuration

```ini
[Modules]
LoadImageURLModule = true
```

### With Proxy Support

```ini
[Modules]
LoadImageURLModule = true

[Startup]
HttpProxy = http://proxy.company.com:8080
HttpProxyExceptions = localhost;127.0.0.1;internal.company.com
```

### With Security Filtering

```ini
[Modules]
LoadImageURLModule = true

[XEngine]
HttpRequestUrlSchemes = https
AllowedOutboundHttpHosts = trusted-cdn.com;secure-images.com
```

## Best Practices

### Security Guidelines

1. **URL Filtering**: Implement appropriate URL filtering policies
2. **Protocol Security**: Use HTTPS when possible for secure image transfer
3. **Content Validation**: Validate image sources and content
4. **Access Control**: Control which scripts can load images from URLs

### Performance Optimization

1. **Image Sizing**: Use appropriately sized source images
2. **Format Selection**: Choose efficient image formats for web delivery
3. **Caching Strategy**: Implement caching for frequently used images
4. **Network Optimization**: Optimize network configuration for image loading

### Operational Practices

1. **Monitoring**: Monitor image loading success rates and performance
2. **Error Handling**: Implement robust error handling in scripts
3. **Resource Management**: Monitor server resources during image processing
4. **Content Management**: Maintain image source URLs and content quality

## Future Enhancements

### Potential Improvements

1. **Enhanced Formats**: Support for additional image formats and protocols
2. **Caching System**: Built-in caching for frequently accessed images
3. **Batch Processing**: Support for batch image loading operations
4. **Advanced Filtering**: More sophisticated content filtering and validation

### Compatibility Considerations

1. **Web Standards**: Stay current with evolving web image standards
2. **Protocol Updates**: Adapt to new HTTP protocol versions
3. **Security Standards**: Implement new security best practices
4. **Performance Scaling**: Enhanced scalability for larger deployments