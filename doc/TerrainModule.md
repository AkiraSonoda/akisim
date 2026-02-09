# TerrainModule Technical Documentation

## Overview

The **TerrainModule** is a core OpenSimulator module responsible for managing terrain editing, height map manipulation, and terrain file operations within regions. It provides comprehensive terrain functionality including real-time editing tools, file format support, console commands, and viewer integration for terrain modification.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **ICommandableModule**: Console command integration
- **ITerrainModule**: Terrain-specific functionality interface

### Key Components
- **TerrainChannel**: Height map data structure (ITerrainChannel)
- **File Loaders**: Support for multiple terrain file formats
- **Brush Effects**: Paint and flood-based terrain modification tools
- **Modifiers**: Algorithmic terrain transformation operations
- **Console Commands**: Administrative terrain management interface

## Terrain Editing System

### Standard Terrain Effects
The module implements six standard terrain effects recognized by viewers:

1. **Flatten** (0): Levels terrain to a uniform height
2. **Raise** (1): Increases terrain elevation
3. **Lower** (2): Decreases terrain elevation
4. **Smooth** (3): Reduces terrain roughness and noise
5. **Noise** (4): Adds random variation to terrain
6. **Revert** (5): Restores terrain to previously saved state

### Brush Types
The system supports two categories of terrain brushes:

#### Paint Brushes (Sphere-based)
- **FlattenSphere**: Localized terrain flattening
- **RaiseSphere**: Localized terrain elevation increase
- **LowerSphere**: Localized terrain elevation decrease
- **SmoothSphere**: Localized smoothing operations
- **NoiseSphere**: Localized noise generation
- **RevertSphere**: Localized reversion to saved state

#### Flood Brushes (Area-based)
- **FlattenArea**: Large area terrain flattening
- **RaiseArea**: Large area elevation increase
- **LowerArea**: Large area elevation decrease
- **SmoothArea**: Large area smoothing operations
- **NoiseArea**: Large area noise generation
- **RevertArea**: Large area reversion to saved state

## File Format Support

### Supported Input/Output Formats
- **RAW32** (.r32): 32-bit raw height data
- **LLRAW** (.raw): Linden Lab raw format
- **BMP** (.bmp): Windows bitmap height maps
- **JPEG** (.jpg, .jpeg): JPEG image height maps
- **PNG** (.png): Portable Network Graphics height maps
- **TIFF** (.tif, .tiff): Tagged Image File Format height maps
- **GIF** (.gif): Graphics Interchange Format height maps
- **Terragen** (.ter): Terragen terrain format

### File Operations
- **Load**: Import terrain from supported file formats
- **Save**: Export current terrain to supported formats
- **Load-Tile**: Import terrain sections from larger tiled files
- **Save-Tile**: Export terrain sections for tiled operations

## Console Commands

### File Operations
```
terrain load <filename>
terrain save <filename>
terrain load-tile <filename> <file_width> <file_height> <min_x> <min_y>
terrain save-tile <filename> <file_width> <file_height> <min_x> <min_y>
```

### Terrain Modification
```
terrain elevate <amount>        # Raise entire terrain by specified meters
terrain lower <amount>          # Lower entire terrain by specified meters
terrain multiply <amount>       # Multiply all heights by factor
terrain bake                    # Save current state for revert operations
terrain revert                  # Restore to last baked state
terrain newbrushes <true|false> # Enable/disable new brush system
terrain fill <height>          # Set all terrain to specified height
```

### Terrain Analysis and Transformation
```
terrain flip <x|y>             # Flip terrain along X or Y axis
terrain rescale <min> <max>     # Rescale heights to specified range
terrain min <height>            # Set minimum terrain height
terrain max <height>            # Set maximum terrain height
terrain stats                   # Display terrain statistics
```

## Terrain Modifiers

### Available Modifiers
- **FillModifier**: Sets terrain to uniform height
- **RaiseModifier**: Increases terrain elevation globally
- **LowerModifier**: Decreases terrain elevation globally
- **SmoothModifier**: Applies smoothing across entire terrain
- **NoiseModifier**: Adds controlled noise to terrain
- **MinModifier**: Sets minimum height constraint
- **MaxModifier**: Sets maximum height constraint

## Configuration

### Module Configuration
```ini
[Modules]
; Enable/disable TerrainModule (default: true)
TerrainModule = true
```

### Terrain Settings
```ini
[Terrain]
; Initial terrain type when creating new regions
InitialTerrain = "pinhead-island"

; Send terrain updates based on avatar view distance (default: false)
SendTerrainUpdatesByViewDistance = false
```

## Performance Features

### Optimization Systems
- **View Distance Updates**: Optional terrain patch transmission based on avatar position
- **Patch-based Updates**: Efficient transmission of modified terrain sections only
- **Client-specific Tracking**: Per-client terrain patch update management
- **Taint System**: Efficient change detection and propagation

### Memory Management
- **Terrain Channel Caching**: Efficient height map storage and retrieval
- **Baked State Storage**: Compressed storage of revert points
- **Patch Update Queuing**: Optimized client update scheduling

## Integration Points

### Script Integration
- **llModifyLand**: LSL function for script-based terrain modification
- **Terrain Events**: Script notifications for terrain changes
- **Permission System**: Integration with land ownership and permissions

### Viewer Integration
- **Terrain Patch Protocol**: UDP-based terrain data transmission
- **Brush Tool Support**: Real-time terrain editing via viewer tools
- **Texture Integration**: Automatic terrain texture coordinate calculation

### Physics Integration
- **Height Map Updates**: Automatic physics engine terrain updates
- **Collision Detection**: Integration with physics collision systems
- **Performance Optimization**: Efficient physics mesh generation

## Administrative Features

### Region Management
- **Terrain Import/Export**: Bulk terrain data management
- **Backup Operations**: Automated terrain state preservation
- **Cross-Region Consistency**: Terrain edge matching for adjacent regions

### Development Tools
- **Debug Commands**: Comprehensive terrain analysis tools
- **Performance Monitoring**: Terrain update performance tracking
- **Error Handling**: Robust file operation error management

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Configuration-based Loading**: Enabled/disabled via OpenSim.ini configuration
- **Default Behavior**: Loaded by default due to essential terrain functionality
- **Logging Integration**: Comprehensive debug and info logging for operations

### Dependencies
- **Core Framework**: OpenSim.Framework for basic functionality
- **Scene Management**: OpenSim.Region.Framework.Scenes for region integration
- **Console System**: OpenSim.Framework.Console for command interface
- **Image Processing**: System.Drawing.Common for image format support

## Security Considerations

### Command Permissions
- **Hazardous Commands**: Terrain modification marked as hazardous operations
- **Access Control**: Integration with OpenSim permission system
- **Audit Logging**: Comprehensive logging of terrain modifications

### File Operations Security
- **Path Validation**: Secure file path handling and validation
- **Format Validation**: Input validation for terrain file formats
- **Error Handling**: Safe handling of malformed terrain files

This documentation reflects the TerrainModule implementation in `src/OpenSim.Region.CoreModules/World/Terrain/TerrainModule.cs` and its integration with the factory-based module loading system.