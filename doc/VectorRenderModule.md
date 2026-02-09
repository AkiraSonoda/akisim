# VectorRenderModule

## Overview

The VectorRenderModule is a sophisticated dynamic texture rendering system for OpenSimulator/Akisim that enables real-time generation of vector-based graphics as textures. It serves as a powerful component for script-based texture creation, allowing LSL scripts to dynamically generate complex vector graphics, diagrams, text, and geometric shapes that can be applied as textures to in-world objects, creating dynamic displays, signage, technical diagrams, and interactive visual content.

## Architecture

The VectorRenderModule implements multiple interfaces:
- `ISharedRegionModule` - Shared module lifecycle management across all regions
- `IDynamicTextureRender` - Dynamic texture rendering interface for integration with the texture system

### Key Components

1. **Vector Graphics Engine**
   - **GDI+ Integration**: High-performance graphics rendering using .NET Graphics class
   - **Command-based Drawing**: Text-based command parsing for vector operations
   - **Multi-format Support**: Support for various geometric shapes, text, and images
   - **Transform Operations**: Matrix transformations for scaling, rotation, and translation

2. **Dynamic Texture Pipeline**
   - **Parameter Processing**: Flexible parameter parsing for texture dimensions and properties
   - **Image Processing**: Bitmap creation with configurable dimensions and color formats
   - **JPEG2000 Encoding**: Conversion to OpenSim's native texture format
   - **Memory Management**: Efficient resource cleanup and memory optimization

3. **Drawing Command System**
   - **Text Rendering**: Advanced text rendering with font control and formatting
   - **Geometric Shapes**: Lines, rectangles, ellipses, polygons with fill options
   - **Image Integration**: Remote image loading and embedding in vector graphics
   - **Color Management**: Comprehensive color control with hex and named color support

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
[Modules]
VectorRenderModule = true
```

### Vector Render Configuration

Configure in `[VectorRender]` section:
```ini
[VectorRender]
; Font name for text rendering (default: Arial)
font_name = Arial
```

### Integration with Dynamic Texture Manager

The module automatically registers with the DynamicTextureManager when the region loads, making it available for script-based texture generation through the "vector" content type.

## Features

### Advanced Drawing Capabilities

The module supports comprehensive vector graphics features:

1. **Text Rendering**
   - **Multi-font Support**: Configurable font families and sizes
   - **Font Styling**: Bold, italic, underline, strikeout formatting
   - **Dynamic Font Changes**: Runtime font property modification
   - **Text Positioning**: Precise text placement and alignment

2. **Geometric Primitives**
   - **Lines**: Vector line drawing with configurable pen properties
   - **Rectangles**: Filled and outlined rectangle drawing
   - **Ellipses**: Circular and elliptical shapes with fill options
   - **Polygons**: Complex multi-point polygon shapes

3. **Advanced Graphics Features**
   - **Pen Customization**: Line width, color, and cap style control
   - **Brush Properties**: Fill colors and patterns
   - **Matrix Transformations**: Translation, scaling, rotation operations
   - **Image Embedding**: HTTP-based remote image loading and integration

### Command-Based Drawing System

The module uses a sophisticated command parsing system:

#### Basic Drawing Commands

- **MoveTo x,y** - Move drawing cursor to coordinates
- **LineTo x,y** - Draw line from current position to coordinates
- **Rectangle width,height** - Draw rectangle at current position
- **FillRectangle width,height** - Draw filled rectangle
- **Ellipse width,height** - Draw ellipse at current position
- **FillEllipse width,height** - Draw filled ellipse
- **Polygon x1,y1,x2,y2,x3,y3...** - Draw polygon with multiple points
- **FillPolygon x1,y1,x2,y2,x3,y3...** - Draw filled polygon

#### Text Commands

- **Text message** - Render text at current position
- **FontSize size** - Set font size for subsequent text
- **FontName fontname** - Change font family
- **FontProp B,I,U,S,R** - Set font properties (Bold, Italic, Underline, Strikeout, Regular)

#### Style Commands

- **PenSize width** - Set line width for drawing operations
- **PenColour/PenColor hex|name** - Set pen color (hex format or color name)
- **PenCap start|end|both arrow|round|diamond|flat** - Set line cap styles

#### Transform Commands

- **ResetTransf** - Reset transformation matrix to identity
- **TransTransf x,y** - Apply translation transformation
- **ScaleTransf x,y** - Apply scaling transformation
- **RotTransf angle** - Apply rotation transformation

#### Image Commands

- **Image width,height,url** - Load and embed remote image

## Technical Implementation

### Drawing Engine Architecture

#### Command Processing Pipeline

1. **Command Parsing**: Split input data into individual drawing commands
2. **Parameter Extraction**: Parse command parameters using delimiter splitting
3. **Graphics Context**: Maintain drawing state (position, pen, brush, font)
4. **Execution Engine**: Execute commands sequentially with state management
5. **Resource Cleanup**: Proper disposal of graphics resources

#### Memory Management

The module implements sophisticated memory management:

```csharp
// Thread-safe resource disposal
lock (this)
{
    if (alpha == 256)
    {
        bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
        graph = Graphics.FromImage(bitmap);
        graph.Clear(bgColor);
    }
    else
    {
        Color newbg = Color.FromArgb(alpha, bgColor);
        bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        graph = Graphics.FromImage(bitmap);
        graph.Clear(newbg);
    }
    GDIDraw(data, graph, altDataDelim, out reuseable);
}
```

### Parameter Processing System

The module supports extensive parameter customization:

#### Dimension Parameters
- **width**: Texture width (1-2048 pixels, default: 256)
- **height**: Texture height (1-2048 pixels, default: 256)

#### Transparency Parameters
- **alpha**: Alpha transparency (0-255, default: 255)
- **bgcolor/bgcolour**: Background color (hex or named color)

#### Format Parameters
- **lossless**: Enable lossless JPEG2000 encoding
- **altdatadelim**: Custom command delimiter character

#### Legacy Parameters
- **setalpha**: Legacy transparent background mode
- **Single Integer**: Legacy width/height parameter (128-1024)

### Text Measurement System

The module provides accurate text measurement capabilities:

```csharp
public void GetDrawStringSize(string text, string fontName, int fontSize,
                              out double xSize, out double ySize)
{
    lock (thisLock)
    {
        using (Font myFont = new Font(fontName, fontSize))
        {
            SizeF stringSize = m_graph.MeasureString(text, myFont);
            xSize = stringSize.Width;
            ySize = stringSize.Height;
        }
    }
}
```

### Remote Image Integration

The module supports loading images from remote URLs:

#### HTTP Request Processing
- **Asynchronous Loading**: Non-blocking image retrieval
- **Error Handling**: Graceful handling of network failures
- **Format Support**: Automatic format detection and conversion
- **Caching Strategy**: Texture reusability assessment

#### Security Considerations
- **URL Validation**: Basic URL format checking
- **Network Timeouts**: Request timeout management
- **Error Fallback**: Visual error indication for failed loads

## Performance Characteristics

### Graphics Optimization

- **Resource Pooling**: Static graphics context for text measurement
- **Lock-based Synchronization**: Thread-safe graphics operations
- **Memory Efficient**: Proper disposal patterns for graphics objects
- **Streaming Processing**: Efficient command processing pipeline

### Scalability Features

- **Dimension Limits**: Configurable size constraints (1-2048 pixels)
- **Command Complexity**: Support for complex multi-command sequences
- **Concurrent Processing**: Thread-safe execution for multiple requests
- **Resource Monitoring**: Efficient memory usage patterns

### Caching Strategy

- **Reusability Assessment**: Automatic detection of cacheable content
- **Image-based Invalidation**: Dynamic content marking for cache bypass
- **Static Content Optimization**: Efficient caching for static vector graphics

## API Integration

### IDynamicTextureRender Interface

The module implements the standard dynamic texture interface:

#### Core Methods

- `GetName()` - Returns "VectorRenderModule" identifier
- `GetContentType()` - Returns "vector" content type
- `SupportsAsynchronous()` - Returns true for async support
- `ConvertData(string bodyData, string extraParams)` - Main vector rendering method
- `AsyncConvertData(UUID id, string bodyData, string extraParams)` - Asynchronous rendering

#### Texture Generation Process

- **Input**: Vector command string and rendering parameters
- **Processing**: Command parsing and graphics generation
- **Output**: JPEG2000 texture data with specified dimensions
- **Metadata**: Size information and reusability flags

### Integration with DynamicTextureManager

The module registers with the DynamicTextureManager as a "vector" renderer:

1. **Registration**: Automatic registration during region loading
2. **Request Routing**: DynamicTextureManager routes vector requests to this module
3. **Completion Handling**: Texture data return through ReturnData method
4. **Error Management**: Comprehensive error handling and reporting

## Usage Examples

### Basic Vector Graphics

```lsl
// Simple rectangle with text
string commands = "Rectangle 100,50;MoveTo 10,20;Text Hello World";
string params = "width:256,height:256";

// Request dynamic texture generation
key texture_id = llList2Key(llGetLinkData([LINK_THIS], [PRIM_TEXTURE, 0]), 0);
llSetText("Generating vector texture...", <1,1,1>, 1.0);
```

### Advanced Graphics with Styling

```lsl
// Complex diagram with multiple elements
string commands =
    "PenSize 3;" +
    "PenColour FF0000;" +  // Red pen
    "Rectangle 200,100;" +
    "MoveTo 50,25;" +
    "FontSize 16;" +
    "FontProp B;" +        // Bold text
    "Text DIAGRAM TITLE;" +
    "ResetTransf;" +
    "TransTransf 20,60;" +
    "PenColour 0000FF;" +  // Blue pen
    "Ellipse 30,30;" +
    "MoveTo 40,15;" +
    "FontSize 12;" +
    "FontProp R;" +        // Regular text
    "Text Detail";

string params = "width:256,height:128,bgcolor:FFFFFF";
```

### Dynamic Charts and Graphs

```lsl
// Bar chart generation
string commands =
    "PenSize 2;" +
    "PenColour 000000;" +
    // Chart frame
    "MoveTo 20,20;" +
    "LineTo 20,180;" +
    "LineTo 220,180;" +
    // Data bars
    "PenColour 00FF00;" +
    "FillRectangle 30,60;" +
    "MoveTo 50,120;" +
    "FillRectangle 30,100;" +
    "MoveTo 90,140;" +
    "FillRectangle 30,80;" +
    // Labels
    "MoveTo 10,10;" +
    "FontSize 14;" +
    "Text Sales Data";

string params = "width:256,height:200";
```

### Geometric Patterns

```lsl
// Complex polygon pattern
string commands =
    "PenSize 1;" +
    "PenColour 8B4513;" +   // Brown outline
    "Polygon 50,50,100,20,150,50,150,100,100,130,50,100;" +
    "PenColour FFD700;" +   // Gold fill
    "FillPolygon 60,60,90,40,120,60,120,90,90,110,60,90;" +
    "MoveTo 85,75;" +
    "FontSize 10;" +
    "FontProp B;" +
    "Text STAR";

string params = "width:200,height:150,alpha:200"; // Semi-transparent
```

### Image Integration

```lsl
// Vector graphics with embedded image
string commands =
    "PenSize 5;" +
    "PenColour 000080;" +
    "Rectangle 250,180;" +
    "MoveTo 10,10;" +
    "Image 100,80,http://example.com/logo.png;" +
    "MoveTo 120,20;" +
    "FontSize 18;" +
    "FontProp B;" +
    "Text Company Report;" +
    "MoveTo 120,50;" +
    "FontSize 12;" +
    "FontProp R;" +
    "Text Generated: " + llGetDate();

string params = "width:256,height:200,bgcolor:F0F0F0";
```

## Command Reference

### Complete Command Set

#### Movement Commands
- `MoveTo x,y` - Set drawing cursor position

#### Line Drawing
- `LineTo x,y` - Draw line to coordinates
- `PenSize width` - Set line thickness
- `PenColour hex` - Set line color
- `PenCap start|end|both type` - Set line cap style

#### Shape Drawing
- `Rectangle width,height` - Draw rectangle outline
- `FillRectangle width,height` - Draw filled rectangle
- `Ellipse width,height` - Draw ellipse outline
- `FillEllipse width,height` - Draw filled ellipse
- `Polygon x1,y1,x2,y2,...` - Draw polygon outline
- `FillPolygon x1,y1,x2,y2,...` - Draw filled polygon

#### Text Rendering
- `Text message` - Render text string
- `FontSize size` - Set font size
- `FontName fontname` - Set font family
- `FontProp properties` - Set font style (B=Bold, I=Italic, U=Underline, S=Strikeout, R=Regular)

#### Transformations
- `ResetTransf` - Reset transformation matrix
- `TransTransf x,y` - Apply translation
- `ScaleTransf x,y` - Apply scaling
- `RotTransf angle` - Apply rotation

#### Image Operations
- `Image width,height,url` - Embed remote image

## Security Features

### Input Validation

- **Parameter Bounds**: Strict validation of dimension and numeric parameters
- **Command Parsing**: Safe parsing of drawing commands with error handling
- **Memory Limits**: Protection against excessive memory allocation
- **Resource Constraints**: Automatic cleanup of graphics resources

### Network Security

- **URL Validation**: Basic URL format checking for image commands
- **Timeout Protection**: HTTP request timeouts for image loading
- **Error Handling**: Graceful handling of network failures
- **Resource Limits**: Protection against large image downloads

### Content Protection

- **Format Validation**: Verification of image formats during loading
- **Error Visualization**: Clear error indication for failed operations
- **Fallback Handling**: Graceful degradation for unsupported operations

## Debugging and Troubleshooting

### Common Issues

1. **Graphics Not Rendering**: Check command syntax and parameter format
2. **Font Issues**: Verify font availability on the system
3. **Image Loading Failures**: Check URL accessibility and format support
4. **Memory Problems**: Monitor texture dimensions and complexity

### Diagnostic Tools

1. **Debug Logging**: Comprehensive debug output for troubleshooting
2. **Command Validation**: Error reporting for invalid commands
3. **Parameter Analysis**: Validation of rendering parameters
4. **Performance Monitoring**: Resource usage tracking

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
VectorRenderModule = true

[VectorRender]
font_name = Arial
```

## Use Cases

### Technical Documentation

- **Architectural Diagrams**: Building layouts and technical schematics
- **Flow Charts**: Process diagrams and workflow visualization
- **Network Diagrams**: System architecture and connectivity maps
- **Engineering Drawings**: Technical specifications and blueprints

### Educational Applications

- **Mathematical Diagrams**: Geometric shapes and mathematical visualizations
- **Scientific Illustrations**: Labeled diagrams and experimental setups
- **Interactive Tutorials**: Step-by-step visual guides
- **Assessment Materials**: Quiz graphics and educational content

### Business Applications

- **Data Visualization**: Charts, graphs, and statistical displays
- **Organizational Charts**: Company structure and hierarchy diagrams
- **Process Maps**: Business process documentation
- **Presentation Graphics**: Meeting slides and promotional materials

### Creative Applications

- **Digital Art**: Vector-based artwork and designs
- **Logo Design**: Brand identity and corporate graphics
- **Signage Systems**: Dynamic signs and information displays
- **Game Graphics**: UI elements and game asset generation

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the CoreModuleFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[VectorRender]` configuration section if custom fonts are used
- Test vector rendering functionality after deployment
- Update any custom scripts that depend on the module
- Validate command syntax compatibility

### Deployment Considerations

- **Font Availability**: Ensure required fonts are installed on the server
- **Graphics Libraries**: Verify GDI+ and System.Drawing.Common availability
- **Memory Planning**: Plan for graphics processing resource requirements
- **Network Access**: Configure access for remote image loading if needed

## Configuration Examples

### Basic Vector Rendering

```ini
[Modules]
VectorRenderModule = true
```

### Custom Font Configuration

```ini
[Modules]
VectorRenderModule = true

[VectorRender]
font_name = Times New Roman
```

### Production Configuration

```ini
[Modules]
VectorRenderModule = true

[VectorRender]
font_name = Arial

[Logging]
LogLevel = INFO
```

### Development Configuration

```ini
[Modules]
VectorRenderModule = true

[VectorRender]
font_name = Arial

[Logging]
LogLevel = DEBUG
```

## Best Practices

### Performance Guidelines

1. **Optimize Dimensions**: Use appropriate texture sizes for content complexity
2. **Command Efficiency**: Minimize redundant drawing operations
3. **Resource Management**: Ensure proper cleanup in scripts
4. **Caching Strategy**: Design for texture reusability when possible

### Graphics Design

1. **Vector Optimization**: Use vector commands instead of complex bitmap operations
2. **Color Management**: Use efficient color specifications (hex vs named)
3. **Font Selection**: Choose standard fonts for cross-platform compatibility
4. **Layout Planning**: Design for target texture dimensions

### Script Integration

1. **Error Handling**: Implement error checking for texture generation
2. **Parameter Validation**: Validate input parameters before rendering
3. **Progress Indication**: Provide user feedback during generation
4. **Version Compatibility**: Design for backward compatibility

## Future Enhancements

### Potential Improvements

1. **Advanced Graphics**: Support for gradients, patterns, and advanced effects
2. **Performance Optimization**: Hardware acceleration and optimized rendering
3. **Extended Commands**: Additional drawing primitives and operations
4. **Format Support**: Support for additional image formats and protocols

### Compatibility Considerations

1. **Graphics Standards**: Stay current with graphics API developments
2. **Font Technologies**: Adapt to new font rendering technologies
3. **Platform Updates**: Maintain compatibility with evolving .NET graphics stack
4. **Performance Standards**: Implement performance best practices for large-scale deployments