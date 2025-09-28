# SerialiserModule Technical Documentation

## Overview

The SerialiserModule is a shared region module that provides comprehensive serialization capabilities for OpenSimulator regions. It enables export of region data including terrain heightmaps and scene objects to XML and binary formats, serving as an alternative to the OAR (OpenSimulator Archive) system for region backup and migration purposes.

## Module Classification

- **Type**: ISharedRegionModule, IRegionSerialiserModule
- **Namespace**: OpenSim.Region.CoreModules.World.Serialiser
- **Assembly**: OpenSim.Region.CoreModules
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs with configuration-based loading

## Core Functionality

### Primary Purpose

The SerialiserModule provides XML-based serialization and export functionality for OpenSimulator regions, allowing administrators and developers to export region data in human-readable and machine-parseable formats. It serves as a complementary system to OAR archives with different use cases and capabilities.

### Key Features

1. **Terrain Export**: RAW32 heightmap file generation for terrain data
2. **Object Serialization**: XML-based scene object export with full property preservation
3. **Compressed Archives**: Automatic GZip compression for reduced file sizes
4. **Manifest Generation**: Comprehensive export catalogs with metadata
5. **Modular Architecture**: Plugin-based serializer system for extensibility
6. **Multiple Format Support**: Both XML and binary format outputs
7. **Region Documentation**: Automatic README generation with export metadata

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for shared modules
1. Initialise(IConfigSource) - Configuration loading and directory setup
2. PostInitialise() - Register file serializers (terrain and objects)
3. AddRegion(Scene) - Register module interface and add to region tracking
4. RegionLoaded(Scene) - Final region-specific setup (currently no-op)
5. RemoveRegion(Scene) - Clean up region tracking
6. Close() - Clear all regions and resources
```

### Interface Implementation

The module implements two key interfaces:

#### ISharedRegionModule
Provides standard shared module functionality across multiple regions.

#### IRegionSerialiserModule
Defines the serialization contract with the following key methods:

```csharp
public interface IRegionSerialiserModule
{
    // XML Version 1 methods (legacy)
    void LoadPrimsFromXml(Scene scene, string fileName, bool newIDS, Vector3 loadOffset);
    void SavePrimsToXml(Scene scene, string fileName);

    // XML Version 2 methods (current)
    void LoadPrimsFromXml2(Scene scene, string fileName);
    void SavePrimsToXml2(Scene scene, string fileName);
    void SaveNamedPrimsToXml2(Scene scene, string primName, string fileName);

    // Advanced serialization methods
    SceneObjectGroup DeserializeGroupFromXml2(string xmlString);
    string SerializeGroupToXml2(SceneObjectGroup grp, Dictionary<string, object> options);

    // Region-wide export
    List<string> SerialiseRegion(Scene scene, string saveDir);
}
```

### Serialization Architecture

The module uses a plugin-based architecture with the `IFileSerialiser` interface:

```csharp
internal interface IFileSerialiser
{
    string WriteToFile(Scene scene, string dir);
}
```

#### Registered Serializers

1. **SerialiseTerrain**: Exports terrain heightmaps in RAW32 format
2. **SerialiseObjects**: Exports scene objects in XML format with compression

## Configuration System

### Module Configuration

#### Basic Configuration ([Serialiser] section)
- **save_dir**: `string` - Directory for export files (default: "exports")

#### Module Loading ([Modules] section)
- **SerialiserModule**: `boolean` - Enable/disable the module (default: true)

### Configuration Example

```ini
[Serialiser]
save_dir = /opt/opensim/exports

[Modules]
SerialiserModule = true
```

## File Serializers

### SerialiseTerrain

#### Purpose
Exports terrain heightmap data in RAW32 format for external terrain editing tools.

#### Implementation
```csharp
public string WriteToFile(Scene scene, string dir)
{
    ITerrainLoader fileSystemExporter = new RAW32();
    string targetFileName = Path.Combine(dir, "heightmap.r32");

    lock (scene.Heightmap)
    {
        fileSystemExporter.SaveFile(targetFileName, scene.Heightmap);
    }

    return "heightmap.r32";
}
```

#### Output
- **File**: `heightmap.r32`
- **Format**: RAW32 binary heightmap data
- **Thread Safety**: Uses heightmap locking for concurrent access protection

### SerialiseObjects

#### Purpose
Exports all scene objects to XML format with optional compression for backup and analysis.

#### Implementation Workflow

1. **Object Collection**: Retrieves all entities from the scene
2. **XML Serialization**: Converts SceneObjectGroups to XML2 format
3. **Sorting**: Alphabetical ordering for consistent output
4. **Formatting**: Pretty-printed XML with proper indentation
5. **Compression**: GZip compression for space efficiency

#### Core Serialization Process
```csharp
private static string GetObjectXml(Scene scene)
{
    string xmlstream = "<scene>";
    EntityBase[] EntityList = scene.GetEntities();
    List<string> EntityXml = new List<string>();

    foreach (EntityBase ent in EntityList)
    {
        if (ent is SceneObjectGroup)
        {
            EntityXml.Add(SceneObjectSerializer.ToXml2Format((SceneObjectGroup)ent));
        }
    }

    EntityXml.Sort(); // Consistent ordering
    foreach (string xml in EntityXml)
        xmlstream += xml;

    xmlstream += "</scene>";
    return xmlstream;
}
```

#### Output Files
- **objects.xml**: Human-readable XML format with formatted structure
- **objects.xml.gzs**: GZip compressed version for space efficiency

#### XML Structure
```xml
<scene>
  <SceneObjectGroup>
    <SceneObjectPart>
      <!-- Complete object properties -->
      <Name>ObjectName</Name>
      <UUID>object-uuid</UUID>
      <Position>vector3</Position>
      <Rotation>quaternion</Rotation>
      <!-- Additional properties -->
    </SceneObjectPart>
  </SceneObjectGroup>
  <!-- Additional objects -->
</scene>
```

## Region Export Workflow

### SerialiseRegion Method

The comprehensive region export process creates a complete snapshot of region data:

```csharp
public List<string> SerialiseRegion(Scene scene, string saveDir)
{
    List<string> results = new List<string>();

    // Create export directory
    if (!Directory.Exists(saveDir))
        Directory.CreateDirectory(saveDir);

    // Execute all registered serializers
    foreach (IFileSerialiser serialiser in m_serialisers)
    {
        results.Add(serialiser.WriteToFile(scene, saveDir));
    }

    // Generate README with metadata
    TextWriter regionInfoWriter = new StreamWriter(Path.Combine(saveDir, "README.TXT"));
    regionInfoWriter.WriteLine("Region Name: " + scene.RegionInfo.RegionName);
    regionInfoWriter.WriteLine("Region ID: " + scene.RegionInfo.RegionID.ToString());
    regionInfoWriter.WriteLine("Backup Time: UTC " + DateTime.UtcNow.ToString());
    regionInfoWriter.WriteLine("Serialise Version: 0.1");
    regionInfoWriter.Close();

    // Create manifest file
    TextWriter manifestWriter = new StreamWriter(Path.Combine(saveDir, "region.manifest"));
    foreach (string line in results)
    {
        manifestWriter.WriteLine(line);
    }
    manifestWriter.Close();

    return results;
}
```

### Export Directory Structure

A complete region export creates the following structure:

```
exports/
└── region-uuid/
    ├── README.TXT           # Region metadata
    ├── region.manifest      # File listing
    ├── heightmap.r32        # Terrain data
    ├── objects.xml          # Scene objects (formatted)
    └── objects.xml.gzs      # Scene objects (compressed)
```

### Metadata Files

#### README.TXT
Contains human-readable export information:
```
Region Name: MyRegion
Region ID: 12345678-1234-5678-9abc-123456789012
Backup Time: UTC 2024-03-15 14:30:45
Serialise Version: 0.1
```

#### region.manifest
Lists all exported files:
```
heightmap.r32
objects.xml
```

## XML Serialization Methods

### Legacy XML (Version 1)

#### LoadPrimsFromXml / SavePrimsToXml
Provides compatibility with older XML formats.

### Current XML (Version 2)

#### LoadPrimsFromXml2 / SavePrimsToXml2
```csharp
public void SavePrimsToXml2(Scene scene, string fileName)
{
    SceneXmlLoader.SavePrimsToXml2(scene, fileName);
}

public void LoadPrimsFromXml2(Scene scene, string fileName)
{
    SceneXmlLoader.LoadPrimsFromXml2(scene, fileName);
}
```

#### Advanced Methods

**SaveNamedPrimsToXml2**: Selective export by object name
```csharp
public void SaveNamedPrimsToXml2(Scene scene, string primName, string fileName)
{
    SceneXmlLoader.SaveNamedPrimsToXml2(scene, primName, fileName);
}
```

**Selective Region Export**: Export with bounding box constraints
```csharp
public void SavePrimsToXml2(Scene scene, TextWriter stream, Vector3 min, Vector3 max)
{
    SceneXmlLoader.SavePrimsToXml2(scene, stream, min, max);
}
```

## Object-Level Serialization

### Individual Object Serialization
```csharp
public string SerializeGroupToXml2(SceneObjectGroup grp, Dictionary<string, object> options)
{
    return SceneXmlLoader.SaveGroupToXml2(grp, options);
}

public SceneObjectGroup DeserializeGroupFromXml2(string xmlString)
{
    return SceneXmlLoader.DeserializeGroupFromXml2(xmlString);
}
```

### Batch Object Operations
```csharp
public void SavePrimListToXml2(EntityBase[] entityList, string fileName)
{
    SceneXmlLoader.SavePrimListToXml2(entityList, fileName);
}
```

## Thread Safety and Concurrency

### Scene Access Protection
- Uses scene entity enumeration for consistent snapshots
- Implements heightmap locking during terrain export
- Thread-safe region tracking with RwLockedList

### File System Operations
- Creates directories as needed with proper error handling
- Uses proper stream disposal patterns
- Atomic file operations where possible

## Error Handling and Logging

### Configuration Validation
```csharp
public void Initialise(IConfigSource source)
{
    IConfig config = source.Configs["Serialiser"];
    if (config != null)
    {
        m_savedir = config.GetString("save_dir", m_savedir);
    }

    m_log.InfoFormat("Enabled, using save dir \"{0}\"", m_savedir);
}
```

### File System Error Handling
- Directory creation with exception handling
- Stream disposal in using statements
- Comprehensive logging for troubleshooting

## Performance Considerations

### Memory Management
- Efficient XML processing with streaming where possible
- Proper disposal of file streams and memory streams
- Sorted object processing for consistent output

### Large Region Handling
- Streaming XML generation for memory efficiency
- Compression to reduce disk space usage
- Incremental processing of scene entities

### Concurrency
- Minimal locking with scene snapshot approaches
- Thread-safe collections for region tracking
- Non-blocking export operations

## Use Cases and Applications

### Development and Testing
- Human-readable XML for debugging object properties
- Terrain export for external editing tools
- Region comparison and analysis

### Backup and Migration
- Alternative backup format to OAR archives
- Cross-platform region data exchange
- Selective object export for migration

### Content Management
- Object inventory and cataloging
- Content analysis and reporting
- Automated content processing pipelines

### Integration and Tooling
- External tool integration via XML APIs
- Automated region processing scripts
- Content validation and verification tools

## Comparison with OAR System

### SerialiserModule Advantages
- **Human Readable**: XML format enables manual inspection and editing
- **Selective Export**: Individual components (terrain, objects) can be exported separately
- **Compression Options**: Both uncompressed and compressed formats available
- **External Tool Integration**: Standard XML format works with external tools
- **Debugging**: Clear structure for troubleshooting region issues

### OAR System Advantages
- **Complete Archives**: Includes assets, textures, and all dependencies
- **Standardized Format**: Widely supported across OpenSimulator installations
- **Efficient Storage**: Optimized binary format for production use
- **Asset Management**: Handles texture and asset dependencies automatically

### When to Use SerialiserModule
- Development and debugging scenarios
- Content analysis and reporting needs
- Integration with external tools and workflows
- Selective backup requirements
- Cross-platform data exchange needs

## Dependencies

### Core Framework Dependencies
- `OpenSim.Framework` - Core data structures and utilities
- `OpenSim.Region.Framework` - Scene and region management
- `OpenSim.Region.Framework.Scenes.Serialization` - Scene XML processing

### Terrain System Dependencies
- `OpenSim.Region.CoreModules.World.Terrain` - Terrain management interfaces
- `OpenSim.Region.CoreModules.World.Terrain.FileLoaders` - RAW32 format support

### Threading Dependencies
- `ThreadedClasses` - Thread-safe collections (RwLockedList)

### System Dependencies
- `System.IO` - File system operations
- `System.IO.Compression` - GZip compression support
- `System.Xml` - XML processing and formatting

## Integration Points

### Scene Manager Integration
- Registers as module interface for serialization services
- Tracks multiple regions for cross-region operations
- Integrates with scene entity management

### Terrain System Integration
- Uses terrain loader infrastructure for heightmap export
- Leverages existing RAW32 format support
- Maintains terrain thread safety requirements

### Object Serialization Integration
- Uses SceneXmlLoader for standardized XML processing
- Integrates with SceneObjectSerializer for object conversion
- Maintains compatibility with existing XML formats

## Troubleshooting

### Common Configuration Issues

1. **Module Not Loading**
   - Verify `SerialiserModule = true` in `[Modules]` section
   - Check module factory integration and loading logs
   - Ensure OpenSim.Region.CoreModules.dll is available

2. **Export Directory Problems**
   - Verify save_dir is writable by OpenSimulator process
   - Check disk space availability for large exports
   - Ensure proper file system permissions

3. **Export Failures**
   - Check scene readiness and entity availability
   - Verify terrain module is loaded for heightmap export
   - Review logs for specific serialization errors

### Common Runtime Issues

1. **Empty Exports**
   - Verify scene contains objects to export
   - Check entity enumeration returns valid objects
   - Ensure proper module interface registration

2. **Corrupted XML**
   - Verify object properties are valid for serialization
   - Check for special characters in object names/descriptions
   - Review XML formatting and encoding issues

3. **File Access Errors**
   - Ensure export directory exists and is writable
   - Check for file locking by other processes
   - Verify sufficient disk space for export operations

### Debug Configuration

```ini
[Serialiser]
save_dir = ./debug-exports

[Modules]
SerialiserModule = true

# Enable detailed logging if needed
```

### Log Analysis

Monitor module loading and operation:
```
[SERIALISER]: Enabled, using save dir "exports"
[SERIALISER]: Region [RegionName] added
[SERIALISER]: Exporting region [RegionName] to directory [path]
```

## Future Enhancement Opportunities

### Advanced Serialization
- Support for additional terrain formats (PNG, GeoTIFF)
- Asset reference tracking and export
- Incremental export capabilities for large regions

### Enhanced Metadata
- Detailed object statistics and analysis
- Dependency tracking between objects
- Export validation and integrity checking

### Integration Improvements
- REST API endpoints for remote export operations
- Scheduled export capabilities
- Integration with external storage systems (S3, FTP)

### Performance Optimizations
- Parallel processing for large object collections
- Streaming compression for memory efficiency
- Background export processing

## Conclusion

The SerialiserModule provides essential XML-based serialization capabilities for OpenSimulator regions. Its modular architecture, comprehensive export functionality, and human-readable output formats make it valuable for development, debugging, content management, and integration scenarios. The module's implementation of both ISharedRegionModule and IRegionSerialiserModule interfaces ensures compatibility with existing OpenSimulator infrastructure while providing extensible serialization capabilities for diverse use cases.