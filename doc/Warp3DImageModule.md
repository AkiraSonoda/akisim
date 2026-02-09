# Warp3DImageModule

## Overview

The Warp3DImageModule is a sophisticated 3D map tile generation system for OpenSimulator/Akisim that provides high-quality, photorealistic map tiles and view images of virtual regions. It utilizes the Warp3D rendering engine to create detailed 3D visualizations that include terrain with texture mapping, water surfaces, and rendered primitives (prims) with full geometry and texturing support. This module serves as the primary map image generator for creating both region overview tiles and custom camera view images.

## Architecture

The Warp3DImageModule implements multiple interfaces:
- `INonSharedRegionModule` - Per-region module instance management
- `IMapImageGenerator` - Map tile and view image generation interface

### Key Components

1. **3D Rendering Engine**
   - **Warp3D Integration**: High-performance software-based 3D rendering
   - **Scene Management**: Complete 3D scene construction and rendering
   - **Camera System**: Configurable camera positioning and projection modes
   - **Lighting Model**: Ambient and directional lighting for realistic rendering

2. **Terrain Rendering System**
   - **Height Map Processing**: Accurate terrain geometry from region height maps
   - **Texture Splatting**: Multi-layer terrain texture blending
   - **LOD Optimization**: Level-of-detail optimization for large regions
   - **Color Averaging**: Optional texture color averaging for performance

3. **Primitive Rendering Pipeline**
   - **Mesh Generation**: Full primitive mesh generation with sculpts and meshes
   - **Texture Mapping**: Complete texture coordinate mapping and UV processing
   - **Material Processing**: Advanced material and texture handling
   - **LOD Selection**: Dynamic level-of-detail based on screen projection

4. **Asset Management**
   - **Texture Caching**: Efficient texture loading and caching system
   - **Color Analysis**: Automatic texture color analysis and metadata caching
   - **Asset Retrieval**: Integration with OpenSim asset service
   - **Memory Management**: Aggressive memory management for large scenes

## Configuration

### Module Activation

Set in `[Map]` or `[Startup]` section:
```ini
[Map]
MapImageModule = Warp3DImageModule
```

### Rendering Configuration Options

Configure in `[Map]` or `[Startup]` sections:

#### Basic Rendering Control
```ini
[Map]
MapImageModule = Warp3DImageModule

; Enable/disable primitive rendering on map tiles (default: true)
DrawPrimOnMapTile = true

; Enable/disable terrain texture mapping (default: true)
TextureOnMapTile = true

; Use average texture colors instead of full textures (default: false)
AverageTextureColorOnMapTile = false

; Enable/disable primitive texture mapping (default: true)
TexturePrims = true

; Minimum prim size for texturing in region units (default: 48.0)
TexturePrimSize = 48.0

; Enable mesh rendering instead of bounding boxes (default: false)
RenderMeshes = false
```

#### Height and Visibility Control
```ini
[Map]
; Maximum height for rendered objects in meters (default: 4096.0)
RenderMaxHeight = 4096.0

; Minimum height for rendered objects in meters (default: -100.0)
RenderMinHeight = -100.0
```

### Performance and Quality Settings

The module automatically adjusts rendering quality based on region size and available resources:
- **Terrain Resolution**: Automatically calculated based on region size (max 256x256 vertices)
- **Texture Resolution**: Textures reduced to 256x256 for efficiency
- **LOD Selection**: Dynamic level-of-detail for primitives based on screen size

## Features

### Advanced 3D Rendering

The module provides sophisticated 3D rendering capabilities:

1. **Realistic Terrain Rendering**
   - **Multi-layer Texturing**: Support for up to 4 terrain texture layers
   - **Height-based Blending**: Automatic texture blending based on elevation
   - **Smooth Interpolation**: Bilinear interpolation for smooth terrain surfaces
   - **Water Surface**: Realistic water plane rendering with transparency

2. **Complete Primitive Support**
   - **All Primitive Types**: Support for all OpenSim primitive shapes
   - **Sculpted Primitives**: Full sculpted prim rendering with image-based sculpting
   - **Mesh Objects**: Complete mesh object rendering with imported meshes
   - **Texture Mapping**: Full UV coordinate processing and texture application

3. **Advanced Material System**
   - **Color Blending**: Advanced color and texture blending
   - **Transparency Support**: Full alpha channel and transparency handling
   - **Texture Caching**: Intelligent texture caching with color analysis
   - **Material Properties**: Support for all primitive material properties

### Camera and Projection System

1. **Map Tile Mode**
   - **Orthographic Projection**: Top-down orthographic view for map tiles
   - **Auto-positioning**: Automatic camera positioning for full region coverage
   - **Fixed Height**: Camera positioned at optimal height for region overview

2. **Custom View Mode**
   - **Perspective Projection**: Full perspective camera with configurable field of view
   - **Free Positioning**: Arbitrary camera position and orientation
   - **Flexible Resolution**: Support for any image resolution and aspect ratio

## Technical Implementation

### 3D Scene Construction

#### Scene Setup Process

1. **Renderer Initialization**: Create Warp3D renderer with specified dimensions
2. **Camera Configuration**: Set up camera position, orientation, and projection
3. **Lighting Setup**: Configure ambient and directional lighting
4. **Scene Population**: Add terrain, water, and primitive objects
5. **Rendering Execution**: Perform 3D rendering to bitmap
6. **Memory Cleanup**: Aggressive cleanup of graphics resources

#### Terrain Generation Algorithm

```csharp
// Optimized terrain mesh generation
int twidth = 1 << bitWidth;  // Power-of-2 terrain resolution
float diff = regionsx / twidth;  // Vertex spacing

// Create terrain vertices with texture coordinates
for (y = 0; y < regionsy; y += diff)
{
    tv = y * invsy;
    for (x = 0; x < regionsx; x += diff)
        obj.addVertex(x, terrain[(int)x, (int)y], y, x * invsx, tv);
}

// Generate triangle mesh
for (int j = 0; j < limy; j++)
{
    for (int i = 0; i < limx; i++)
    {
        int v = j * npointsx + i;
        obj.addTriangle(v, v + 1, v + npointsx);
        obj.addTriangle(v + npointsx + 1, v + npointsx, v + 1);
    }
}
```

### Primitive Rendering Pipeline

#### Level-of-Detail Selection

The module implements dynamic LOD selection based on screen projection:

```csharp
// Calculate screen space projection for LOD selection
float screenFactor = renderer.Scene.EstimateBoxProjectedArea(primPos, primScale, rotationMatrix);
int p2 = (int)(MathF.Log2(screenFactor) * 0.25 - 1);
DetailLevel lod = (DetailLevel)(3 - Math.Clamp(p2, 0, 3));
```

#### Mesh Generation Process

1. **Shape Analysis**: Determine primitive type (basic, sculpted, or mesh)
2. **Asset Retrieval**: Load sculpt textures or mesh assets as needed
3. **Mesh Generation**: Generate appropriate mesh using selected renderer
4. **UV Mapping**: Process texture coordinates for each face
5. **Material Application**: Apply textures and colors to mesh faces

### Texture Processing System

#### Texture Caching Strategy

The module implements sophisticated texture caching:

```csharp
// Texture caching with color analysis metadata
string cacheName = "MAPCLR" + textureID.ToString();
AssetBase metadata = m_scene.AssetService.GetCached(cacheName);

// Calculate and cache average color
Color4 avgColor = GetAverageColor(textureAsset.FullID, textureAsset.Data, out width, out height);
OSDMap data = new OSDMap { { "X-RGBA", OSD.FromColor4(avgColor) } };
```

#### UV Coordinate Processing

Support for different texture mapping modes:

```csharp
// Planar mapping for complex surfaces
if (teFace.TexMapType == MappingType.Planar)
{
    UVPlanarMap(ref vertex, ref primScale, out tu, out tv);
}
else
{
    // Standard UV mapping
    tu = face.Vertices[j].TexCoord.X - 0.5f;
    tv = 0.5f - face.Vertices[j].TexCoord.Y;
}

// Apply texture transformations
if (rotation != 0)
{
    float tur = tu * rc - tv * rs;
    float tvr = tu * rs + tv * rc;
    faceObj.addVertex(new warp_Vertex(pos, tur * scaleu + offsetu, tvr * scalev + offsetv));
}
```

### Memory Management

The module implements aggressive memory management for handling large scenes:

```csharp
// Force garbage collection after rendering
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.Default;
```

## Performance Characteristics

### Rendering Optimization

- **Terrain LOD**: Automatic terrain resolution scaling (max 256x256 vertices)
- **Texture Reduction**: All textures scaled to 256x256 for memory efficiency
- **Culling System**: Height-based culling for objects outside render range
- **Screen Space LOD**: Dynamic primitive detail based on projected screen size

### Memory Efficiency

- **Resource Pooling**: Efficient caching of textures and materials
- **Automatic Cleanup**: Aggressive cleanup of graphics resources
- **Streaming Processing**: Processes primitives individually to minimize memory usage
- **Garbage Collection**: Strategic garbage collection after rendering

### Scalability Features

- **Region Size Support**: Automatic handling of variable region sizes
- **Large Scene Handling**: Optimized for regions with thousands of primitives
- **Concurrent Safety**: Thread-safe implementation for multi-region servers
- **Resource Monitoring**: Built-in protection against excessive memory usage

## API Methods

### Core Interface Methods

#### IMapImageGenerator Interface

- `CreateMapTile()` - Generate standard 256x256 (or region size) map tile
- `CreateViewImage(Vector3 camPos, Vector3 camDir, float fov, int width, int height, bool useTextures)` - Create custom camera view
- `WriteJpeg2000Image()` - Generate JPEG2000 encoded map tile

#### Internal Rendering Methods

- `GenImage()` - Core image generation with 3D scene setup
- `CreateTerrain(WarpRenderer renderer)` - Generate terrain mesh with texturing
- `CreateWater(WarpRenderer renderer)` - Add water plane to scene
- `CreateAllPrims(WarpRenderer renderer)` - Render all scene primitives
- `CreatePrim(WarpRenderer renderer, SceneObjectPart prim)` - Render individual primitive

### Utility Methods

- `GetTexture(UUID id, SceneObjectPart sop)` - Retrieve and cache texture
- `GetOrCreateMaterial(WarpRenderer renderer, Color4 faceColor, UUID textureID, bool useAverageTextureColor, SceneObjectPart sop)` - Material management
- `GetAverageColor(UUID textureID, byte[] j2kData, out int width, out int height)` - Texture color analysis

## Usage Examples

### Basic Map Tile Generation

```csharp
// Module automatically generates map tiles when configured
// Configuration in OpenSim.ini:
[Map]
MapImageModule = Warp3DImageModule
DrawPrimOnMapTile = true
TextureOnMapTile = true
```

### Custom View Generation

```csharp
// Generate custom camera view (example from external code)
Vector3 cameraPosition = new Vector3(128, 128, 100);  // Center of 256x256 region, 100m high
Vector3 cameraDirection = -Vector3.UnitZ;              // Looking down
float fieldOfView = 45.0f;                             // 45 degree FOV
int imageWidth = 512;
int imageHeight = 512;
bool useTextures = true;

Bitmap viewImage = warp3DModule.CreateViewImage(
    cameraPosition, cameraDirection, fieldOfView,
    imageWidth, imageHeight, useTextures);
```

### Performance-Optimized Configuration

```ini
[Map]
MapImageModule = Warp3DImageModule
; Enable basic rendering
DrawPrimOnMapTile = true
TextureOnMapTile = true
; Use average colors for better performance
AverageTextureColorOnMapTile = true
; Disable mesh rendering for speed
RenderMeshes = false
; Set texture threshold higher
TexturePrimSize = 64.0
```

### High-Quality Configuration

```ini
[Map]
MapImageModule = Warp3DImageModule
; Enable all features for maximum quality
DrawPrimOnMapTile = true
TextureOnMapTile = true
AverageTextureColorOnMapTile = false
TexturePrims = true
RenderMeshes = true
TexturePrimSize = 16.0
; Extend render range
RenderMaxHeight = 6000.0
RenderMinHeight = -200.0
```

## Integration Points

### With Asset System

- **Texture Loading**: Direct integration with OpenSim asset service
- **Mesh Assets**: Support for mesh asset loading and processing
- **Sculpt Textures**: Image-based sculpt map processing
- **Caching Integration**: Metadata caching for texture color analysis

### With Terrain System

- **Height Maps**: Direct access to region terrain height data
- **Texture Mapping**: Integration with region terrain texture settings
- **Water Level**: Automatic water plane positioning based on region settings
- **Multi-layer Texturing**: Support for region texture splatting configuration

### With Primitive System

- **Shape Processing**: Complete integration with primitive shape system
- **Texture Entries**: Full support for primitive texture entry processing
- **Material Properties**: Integration with primitive material and color properties
- **Transform System**: Proper handling of primitive positioning and rotation

### With Mesh System

- **Mesh Decoding**: Integration with mesh asset decoding system
- **LOD Processing**: Support for mesh level-of-detail processing
- **Sculpt Processing**: Image-based sculpted primitive processing
- **Rendering Pipeline**: Integration with OpenMetaverse rendering pipeline

## Security Features

### Resource Protection

- **Memory Limits**: Automatic protection against excessive memory allocation
- **Render Bounds**: Configurable height limits for rendering
- **Texture Validation**: Safe texture loading with error handling
- **Asset Verification**: Validation of asset data before processing

### Error Handling

- **Graceful Degradation**: Continues rendering when individual assets fail
- **Exception Safety**: Comprehensive exception handling throughout pipeline
- **Resource Cleanup**: Guaranteed cleanup even on rendering failures
- **Logging Integration**: Detailed logging for troubleshooting and monitoring

### Performance Safeguards

- **LOD Enforcement**: Automatic level-of-detail to prevent performance issues
- **Size Constraints**: Texture size limitations for memory management
- **Timeout Protection**: Implicit timeout through memory management
- **Resource Monitoring**: Built-in monitoring of resource usage

## Debugging and Troubleshooting

### Common Issues

1. **Map Tiles Not Generated**: Check MapImageModule configuration in [Map] section
2. **Missing Textures**: Verify asset service connectivity and texture availability
3. **Poor Performance**: Adjust LOD settings and enable texture color averaging
4. **Memory Issues**: Monitor texture caching and consider reducing render ranges

### Diagnostic Tools

1. **Debug Logging**: Comprehensive debug output for rendering process
2. **Asset Monitoring**: Tracking of texture and mesh asset loading
3. **Performance Metrics**: Built-in timing and memory usage tracking
4. **Error Reporting**: Detailed error messages for failed operations

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Map]
MapImageModule = Warp3DImageModule
```

## Use Cases

### Virtual World Mapping

- **Region Overviews**: High-quality overview maps for virtual regions
- **Terrain Visualization**: Detailed terrain maps with texture blending
- **Land Management**: Visual tools for land parcel management
- **Navigation Aids**: Map tiles for viewer-based navigation systems

### Content Creation

- **Build Visualization**: 3D visualization of constructed content
- **Design Review**: Quality review of region designs and layouts
- **Screenshot Generation**: High-quality screenshots from arbitrary viewpoints
- **Marketing Materials**: Professional images for virtual world marketing

### Administrative Tools

- **Region Monitoring**: Visual monitoring of region content and activity
- **Asset Verification**: Visual verification of mesh and texture assets
- **Performance Analysis**: Identification of rendering performance bottlenecks
- **Content Auditing**: Visual auditing of region content for compliance

### Educational Applications

- **Virtual Architecture**: Architectural visualization and walkthroughs
- **Geographic Education**: Terrain and geographic feature visualization
- **Historical Recreation**: Visual documentation of historical recreations
- **Scientific Visualization**: 3D visualization of scientific data and models

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the CoreModuleFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via MapImageModule configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[Map]` configuration section includes `MapImageModule = Warp3DImageModule`
- Test map tile generation after deployment
- Update any custom map generation scripts
- Validate texture and mesh rendering functionality

### Deployment Considerations

- **Warp3D Library**: Ensure Warp3D rendering library is available
- **Graphics Dependencies**: Verify System.Drawing.Common and related graphics libraries
- **Memory Planning**: Plan for graphics processing memory requirements
- **Asset Storage**: Ensure adequate asset service performance for texture loading

## Configuration Examples

### Basic Map Generation

```ini
[Map]
MapImageModule = Warp3DImageModule
```

### Performance-Optimized Configuration

```ini
[Map]
MapImageModule = Warp3DImageModule
DrawPrimOnMapTile = true
TextureOnMapTile = true
AverageTextureColorOnMapTile = true
TexturePrims = false
RenderMeshes = false
TexturePrimSize = 100.0
```

### High-Quality Configuration

```ini
[Map]
MapImageModule = Warp3DImageModule
DrawPrimOnMapTile = true
TextureOnMapTile = true
AverageTextureColorOnMapTile = false
TexturePrims = true
RenderMeshes = true
TexturePrimSize = 8.0
RenderMaxHeight = 8192.0
RenderMinHeight = -500.0
```

### Development Configuration

```ini
[Map]
MapImageModule = Warp3DImageModule
DrawPrimOnMapTile = true
TextureOnMapTile = true
AverageTextureColorOnMapTile = false
TexturePrims = true
RenderMeshes = true

[Logging]
LogLevel = DEBUG
```

## Best Practices

### Performance Guidelines

1. **LOD Management**: Use appropriate TexturePrimSize settings for region complexity
2. **Texture Strategy**: Enable AverageTextureColorOnMapTile for better performance on complex regions
3. **Height Limits**: Set appropriate RenderMinHeight and RenderMaxHeight for content
4. **Mesh Rendering**: Enable RenderMeshes only when necessary for quality

### Quality Optimization

1. **Texture Quality**: Disable AverageTextureColorOnMapTile for highest visual quality
2. **Mesh Support**: Enable RenderMeshes for accurate representation of mesh objects
3. **Detail Threshold**: Lower TexturePrimSize for more detailed small object texturing
4. **Range Extension**: Extend render ranges for tall buildings or underground content

### Operational Practices

1. **Monitoring**: Monitor map generation performance and memory usage
2. **Asset Management**: Ensure texture and mesh assets are optimally sized
3. **Testing**: Test map generation with various region content types
4. **Backup**: Maintain backup map generation methods for reliability

## Future Enhancements

### Potential Improvements

1. **Hardware Acceleration**: GPU-accelerated rendering for improved performance
2. **Advanced Materials**: Support for PBR materials and advanced shading
3. **Dynamic Lighting**: Real-time lighting based on region sun position
4. **Enhanced LOD**: More sophisticated level-of-detail algorithms

### Compatibility Considerations

1. **Rendering Engine**: Stay current with Warp3D rendering engine updates
2. **Graphics APIs**: Adapt to evolving .NET graphics APIs
3. **Asset Formats**: Support for new mesh and texture asset formats
4. **Performance Standards**: Optimization for modern hardware capabilities