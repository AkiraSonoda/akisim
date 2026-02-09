# MapImageModule Technical Documentation

## Overview

The **MapImageModule** is a non-shared region module that provides legacy 2D map tile generation functionality for OpenSim regions. It implements the `IMapImageGenerator` interface to create bitmap-based map tiles that represent the terrain and objects within a region. This module serves as the fallback map tile generator when the more advanced Warp3D system is not available or configured, providing basic terrain rendering with optional object volume visualization.

## Architecture

### Module Type
- **Interface**: `INonSharedRegionModule`, `IMapImageGenerator`
- **Namespace**: `OpenSim.Region.CoreModules.World.LegacyMap`
- **Location**: `src/OpenSim.Region.CoreModules/World/LegacyMap/MapImageModule.cs`

### Dependencies
- **Graphics Framework**: System.Drawing for bitmap manipulation and graphics rendering
- **Image Processing**: OpenJPEG for JPEG2000 encoding of map tiles
- **Terrain System**: Scene heightmap for terrain elevation data
- **Asset System**: Asset service for fetching texture data
- **Configuration System**: Nini configuration framework for module settings

## Functionality

### Core Features

#### 1. Legacy Map Tile Generation
- **Dynamic Rendering**: Creates map tiles from real-time scene data
- **Terrain Visualization**: Renders terrain heightmaps with shading or texturing
- **Object Volume Rendering**: Optionally draws 3D object volumes on the map
- **Static Image Support**: Supports pre-created static map images as alternatives

#### 2. Terrain Rendering Options
- **Shaded Rendering**: Uses ShadedMapTileRenderer for basic terrain visualization
- **Textured Rendering**: Uses TexturedMapTileRenderer for texture-based terrain display
- **Configurable Selection**: Runtime configuration determines rendering method
- **Heightmap Integration**: Direct integration with scene terrain data

#### 3. Object Volume Visualization
- **3D Object Projection**: Projects 3D scene objects onto 2D map representation
- **Color Mapping**: Uses object texture colors for map representation
- **Z-Order Sorting**: Renders objects in proper depth order
- **Selective Rendering**: Filters objects by size and position criteria

#### 4. Image Format Support
- **Bitmap Generation**: Creates standard System.Drawing.Bitmap objects
- **JPEG2000 Encoding**: Encodes map tiles to JPEG2000 format for transmission
- **Static File Loading**: Supports loading pre-created map images
- **Texture Asset Integration**: Fetches and processes texture assets

### Map Tile Generation Process

#### Dynamic Generation Flow
1. **Configuration Check**: Validates module configuration and rendering options
2. **Terrain Rendering**: Creates base terrain representation using selected renderer
3. **Object Processing**: Iterates through scene objects and calculates projections
4. **Volume Calculation**: Computes 3D object volumes and their 2D projections
5. **Z-Order Sorting**: Sorts objects by height for proper rendering order
6. **Graphics Rendering**: Uses System.Drawing.Graphics to composite final image
7. **Format Encoding**: Encodes result to requested format (bitmap or JPEG2000)

#### Static Image Handling
1. **File Validation**: Checks if static map file is configured and accessible
2. **Image Loading**: Loads pre-created bitmap from file system
3. **Error Handling**: Falls back to dynamic generation on file errors
4. **Format Support**: Supports standard image formats readable by System.Drawing

## Configuration

### Section: [Startup] / [Map]
```ini
[Startup]
    ; Select map image module - must be "MapImageModule" to enable this module
    ; Alternative: "Warp3DImageModule" for advanced rendering
    ; Default: "MapImageModule"
    MapImageModule = MapImageModule

[Map]
    ; Alternative location for MapImageModule configuration
    ; Takes precedence over [Startup] section
    MapImageModule = MapImageModule

    ; Enable/disable map tile generation entirely
    ; Default: true
    GenerateMaptiles = true

    ; Draw 3D object volumes on map tiles
    ; Default: true
    DrawPrimOnMapTile = true

    ; Use texture-based terrain rendering instead of shading
    ; Default: false
    TextureOnMapTile = false
```

### Static Map Image Configuration
```ini
[Region Configuration]
    ; Path to static map image file (optional)
    ; If specified, replaces dynamic generation
    ; Must be valid image file readable by System.Drawing
    MaptileStaticFile = /path/to/static/map.png
```

### Factory Integration
The module is loaded through the `CoreModuleFactory` with the following behavior:
- **Configuration-Driven**: Only loaded when `MapImageModule = "MapImageModule"`
- **Direct Instantiation**: Created directly as a CoreModule (not via reflection)
- **Alternative to Warp3D**: Provides fallback when Warp3DImageModule not available

## Implementation Details

### Initialization Process
1. **Configuration Validation**: Checks for correct MapImageModule configuration
2. **Module Activation**: Enables module only if properly configured
3. **Interface Registration**: Registers IMapImageGenerator interface with scene
4. **Renderer Setup**: Defers renderer creation until tile generation time

### Terrain Rendering
The module supports two terrain rendering approaches:

#### Shaded Terrain Rendering
- **ShadedMapTileRenderer**: Provides basic height-based shading
- **Performance**: Fast rendering suitable for most scenarios
- **Visual Style**: Grayscale heightmap with lighting simulation
- **Configuration**: Default when `TextureOnMapTile = false`

#### Textured Terrain Rendering
- **TexturedMapTileRenderer**: Applies terrain textures to heightmap
- **Performance**: More CPU-intensive but visually richer
- **Visual Style**: Realistic terrain texture representation
- **Configuration**: Enabled when `TextureOnMapTile = true`

### Object Volume Rendering

#### Object Selection Criteria
The module applies several filters to determine which objects to render:
1. **Type Filter**: Only renders SceneObjectGroup entities
2. **Size Filter**: Objects must be at least 1 meter in any dimension
3. **Position Filter**: Objects must be within region boundaries
4. **Height Filter**: Objects must be within 256m above terrain
5. **Validation Filter**: Objects must have valid positions (no NaN/Infinity)

#### 3D to 2D Projection
1. **Bounding Box Calculation**: Computes 3D bounding box for each object
2. **Rotation Handling**: Applies object rotation to bounding box
3. **Face Generation**: Creates 6 faces representing object volume
4. **Projection Algorithm**: Projects 3D faces to 2D map coordinates
5. **Polygon Rendering**: Fills projected polygons with object color

#### Color Mapping
- **Texture Color Extraction**: Uses default texture entry RGBA values
- **Color Inversion**: Applies color inversion for visual contrast
- **Fallback Color**: Uses gray when object has white or invalid texture
- **Error Handling**: Graceful handling of invalid color values

### Z-Order Processing
1. **Height Collection**: Collects Z-coordinate of all valid objects
2. **Sorting Algorithm**: Sorts objects by height using Array.Sort
3. **Rendering Order**: Renders objects from lowest to highest Z-position
4. **Overlap Handling**: Ensures proper visibility of overlapping objects

### Graphics Rendering
```csharp
using (Graphics g = Graphics.FromImage(mapbmp))
{
    // Render objects in Z-order from bottom to top
    for (int s = 0; s < sortedZHeights.Length; s++)
    {
        DrawStruct rectDrawStruct = z_sort[sortedlocalIds[s]];
        for (int r = 0; r < rectDrawStruct.trns.Length; r++)
        {
            g.FillPolygon(rectDrawStruct.brush, rectDrawStruct.trns[r].pts);
        }
    }
}
```

## Usage Examples

### Basic Legacy Map Generation
```ini
[Startup]
MapImageModule = MapImageModule

[Map]
GenerateMaptiles = true
DrawPrimOnMapTile = true
TextureOnMapTile = false
```

### Textured Terrain Rendering
```ini
[Startup]
MapImageModule = MapImageModule

[Map]
GenerateMaptiles = true
DrawPrimOnMapTile = true
TextureOnMapTile = true
```

### Static Map Image Usage
```ini
[Startup]
MapImageModule = MapImageModule

; Region configuration file
[Region Configuration]
MaptileStaticFile = /var/opensim/maps/region1_map.png
```

### Terrain-Only Rendering
```ini
[Startup]
MapImageModule = MapImageModule

[Map]
GenerateMaptiles = true
DrawPrimOnMapTile = false
TextureOnMapTile = true
```

## Performance Considerations

### Memory Usage
- **Bitmap Allocation**: Creates full-resolution bitmaps (256x256 or region size)
- **Object Sorting**: Temporary arrays for Z-order sorting of scene objects
- **Graphics Context**: System.Drawing.Graphics context for rendering operations
- **Brush Management**: Proper disposal of graphics brushes to prevent memory leaks

### CPU Performance
- **Terrain Rendering**: CPU-intensive heightmap to bitmap conversion
- **Object Iteration**: Processes all scene objects for volume calculation
- **3D Projection**: Complex 3D to 2D mathematical transformations
- **Graphics Operations**: Multiple polygon fill operations for object rendering

### Optimization Strategies
- **Early Filtering**: Objects filtered by size and position before processing
- **Efficient Sorting**: Single sort operation for all objects by Z-height
- **Resource Disposal**: Proper disposal pattern for graphics resources
- **Batch Processing**: Groups object rendering operations for efficiency

### Rendering Performance
```csharp
// Performance timing for object volume rendering
int tc = Environment.TickCount;
m_log.Debug("Generating Maptile Step 2: Object Volume Profile");
// ... rendering operations ...
m_log.Debug("Generating Maptile Step 2: Done in " + (Environment.TickCount - tc) + " ms");
```

## Troubleshooting

### Common Issues

#### 1. Module Not Loading
**Symptoms**: No map tiles generated, IMapImageGenerator interface not available
**Solutions**:
- Verify `MapImageModule = "MapImageModule"` in [Startup] or [Map] section
- Check for case-sensitive configuration matching
- Ensure module properly integrated with CoreModuleFactory
- Verify no competing map image modules are configured

#### 2. Static Map Image Errors
**Symptoms**: Error messages about static file loading, fallback to dynamic generation
**Solutions**:
- Verify static image file path is correct and accessible
- Check file permissions for OpenSim process
- Ensure image file is in supported format (PNG, BMP, JPG)
- Test file loading with System.Drawing.Image manually

#### 3. Terrain Rendering Issues
**Symptoms**: Blank or corrupted terrain on map tiles
**Solutions**:
- Check terrain renderer initialization in logs
- Verify scene heightmap data is valid
- Test both shaded and textured rendering modes
- Monitor for OpenJPEG encoding errors

#### 4. Object Rendering Problems
**Symptoms**: Objects not appearing on map tiles, incorrect positioning
**Solutions**:
- Enable debug logging to see object filtering details
- Check object size requirements (minimum 1 meter)
- Verify objects are within region boundaries
- Monitor for invalid object positions (NaN/Infinity)

#### 5. JPEG2000 Encoding Failures
**Symptoms**: Null results from WriteJpeg2000Image, encoding errors
**Solutions**:
- Verify OpenJPEG libraries are properly installed
- Check for sufficient memory for encoding operations
- Monitor for P/Invoke exceptions in logs
- Test bitmap creation before encoding

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization and configuration validation
- Terrain renderer selection and setup
- Object filtering and processing statistics
- Map tile generation timing and performance
- JPEG2000 encoding success/failure details

### Performance Monitoring
Monitor these metrics for optimal performance:
- **Map Tile Generation Time**: Should complete within reasonable timeframes
- **Memory Usage**: Monitor for memory leaks in graphics operations
- **Object Count**: High object counts may impact rendering performance
- **Terrain Complexity**: Complex terrain affects rendering time

## Integration Notes

### Factory Loading
- Loaded via `CoreModuleFactory.CreateSharedModules()` as part of map image modules
- Uses direct instantiation rather than reflection-based loading
- Requires configuration match for "MapImageModule" to activate

### Interface Implementation
- Implements `IMapImageGenerator` for map tile generation services
- Provides `CreateMapTile()` method for bitmap generation
- Provides `WriteJpeg2000Image()` method for encoded output
- Supports `CreateViewImage()` interface (not implemented)

### Scene Integration
- Registers with scene as IMapImageGenerator service provider
- Uses scene heightmap for terrain data
- Accesses scene entities for object volume rendering
- Integrates with scene asset service for texture fetching

### Graphics Framework Integration
- Uses System.Drawing namespace for all graphics operations
- Relies on System.Drawing.Graphics for polygon rendering
- Depends on System.Drawing.Bitmap for image manipulation
- Requires proper graphics resource disposal

## Alternative Solutions

### Warp3DImageModule
For advanced map tile generation with better visual quality:
```ini
[Startup]
MapImageModule = Warp3DImageModule
```

Benefits of Warp3D over legacy MapImageModule:
- **Advanced Rendering**: 3D-aware rendering with proper lighting
- **Better Performance**: Optimized rendering algorithms
- **Enhanced Visuals**: More realistic object and terrain representation
- **Modern Architecture**: More maintainable and extensible codebase

### Custom Map Tile Solutions
- **External Generation**: Generate map tiles outside OpenSim and use static files
- **Hybrid Approach**: Combine static base with dynamic object overlays
- **Specialized Renderers**: Custom IMapImageGenerator implementations
- **Post-Processing**: Additional processing of generated map tiles

## Security Considerations

### File System Access
- **Static Image Files**: Module reads image files from file system
- **Path Validation**: Ensure static image paths are properly validated
- **File Permissions**: Restrict access to map image directories
- **Input Validation**: Validate image file formats and content

### Resource Management
- **Memory Limits**: Large bitmaps can consume significant memory
- **CPU Usage**: Map generation can be CPU-intensive
- **Graphics Resources**: Proper disposal prevents resource leaks
- **Error Handling**: Graceful handling of graphics operations failures

### Content Security
- **Object Visibility**: Map tiles reveal object positions and layouts
- **Terrain Information**: Heightmaps expose terrain topology
- **Access Control**: Consider who should access generated map tiles
- **Data Exposure**: Be aware of information revealed in map tiles

## See Also
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [Warp3DImageModule](./Warp3DImageModule.md) - Advanced map tile generation
- [Terrain System](../docs/TerrainSystem.md) - Heightmap and terrain rendering
- [Scene Management](../docs/SceneManagement.md) - Region and object management
