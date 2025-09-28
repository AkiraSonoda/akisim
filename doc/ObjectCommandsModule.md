# ObjectCommandsModule Technical Documentation

## Overview

The **ObjectCommandsModule** is a core OpenSimulator module that provides comprehensive console commands for manipulating, inspecting, and managing scene objects. It offers administrators powerful tools for object debugging, cleanup, analysis, and maintenance operations. The module is essential for grid administration, region management, and troubleshooting object-related issues.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **Console Command Interface**: Extensive console command registration and handling

### Key Components
- **Object Inspection**: Detailed viewing of scene objects and parts
- **Object Deletion**: Flexible object removal with multiple criteria
- **Object Serialization**: Export objects to XML format for backup/analysis
- **Search and Filtering**: Advanced object finding capabilities
- **Safety Mechanisms**: Confirmation prompts and attachment protection

## Console Command Categories

### Object Display Commands

#### show object id
```bash
show object id [--full] <UUID-or-localID>
```
- **Purpose**: Display detailed information about a specific object
- **Parameters**: UUID or local ID, optional --full flag for complete details
- **Output**: Object properties, location, flags, and part information
- **Usage**: Debugging specific object issues or getting object details

#### show object name
```bash
show object name [--full] [--regex] <name>
```
- **Purpose**: Find and display objects by name
- **Parameters**: Object name, optional --full and --regex flags
- **Output**: All matching objects with their properties
- **Usage**: Locating objects when you know their names

#### show object owner
```bash
show object owner [--full] <OwnerID>
```
- **Purpose**: Display all objects owned by a specific user
- **Parameters**: Owner UUID, optional --full flag
- **Output**: Complete list of objects owned by the specified user
- **Usage**: User management, ownership verification, cleanup operations

#### show object pos
```bash
show object pos [--full] <start x,y,z> <end x,y,z>
```
- **Purpose**: Display objects within a specified volume
- **Parameters**: Start and end coordinates, optional --full flag
- **Output**: All objects within the defined spatial boundaries
- **Usage**: Regional analysis, spatial debugging, area management

### Object Part Commands

#### show part id
```bash
show part id <UUID-or-localID>
```
- **Purpose**: Display detailed information about a specific object part
- **Parameters**: Part UUID or local ID
- **Output**: Complete part properties including textures, physics, and inventory
- **Usage**: Detailed debugging of individual object parts

#### show part name
```bash
show part name [--regex] <name>
```
- **Purpose**: Find and display object parts by name
- **Parameters**: Part name, optional --regex flag
- **Output**: All matching parts with detailed information
- **Usage**: Finding specific parts across multiple objects

#### show part pos
```bash
show part pos <start x,y,z> <end x,y,z>
```
- **Purpose**: Display object parts within a specified volume
- **Parameters**: Start and end coordinates
- **Output**: All parts within the defined spatial boundaries
- **Usage**: Detailed spatial analysis of object components

### Object Deletion Commands

#### delete object owner
```bash
delete object owner <UUID>
```
- **Purpose**: Delete all objects owned by a specific user
- **Parameters**: Owner UUID
- **Confirmation**: Requires user confirmation before deletion
- **Safety**: Excludes attachments from deletion
- **Usage**: User cleanup, ownership transfers, account deletion

#### delete object creator
```bash
delete object creator <UUID>
```
- **Purpose**: Delete all objects created by a specific user
- **Parameters**: Creator UUID
- **Confirmation**: Requires user confirmation before deletion
- **Safety**: Excludes attachments from deletion
- **Usage**: Content cleanup, creator-based removal

#### delete object id
```bash
delete object id <UUID-or-localID>
```
- **Purpose**: Delete a specific object by identifier
- **Parameters**: Object UUID or local ID
- **Confirmation**: Only required for attachments
- **Safety**: Warns when deleting attachments
- **Usage**: Precise object removal

#### delete object name
```bash
delete object name [--regex] <name>
```
- **Purpose**: Delete objects by name pattern
- **Parameters**: Object name, optional --regex flag
- **Confirmation**: Requires user confirmation before deletion
- **Safety**: Excludes attachments from deletion
- **Usage**: Bulk cleanup by name patterns

#### delete object outside
```bash
delete object outside
```
- **Purpose**: Delete objects outside region boundaries
- **Parameters**: None
- **Confirmation**: Requires user confirmation before deletion
- **Criteria**: Objects below Z=0, above Z=10000, or on invalid land
- **Usage**: Region cleanup, boundary enforcement

#### delete object pos
```bash
delete object pos <start x,y,z> <end x,y,z>
```
- **Purpose**: Delete objects within a specified volume
- **Parameters**: Start and end coordinates
- **Confirmation**: Requires user confirmation before deletion
- **Safety**: Excludes attachments from deletion
- **Usage**: Area cleanup, spatial management

### Object Export Commands

#### dump object id
```bash
dump object id <UUID-or-localID>
```
- **Purpose**: Export object to XML file for backup or analysis
- **Parameters**: Object UUID or local ID
- **Output**: Creates <UUID>.xml file with complete object serialization
- **Safety**: Checks for existing files to prevent overwriting
- **Usage**: Object backup, debugging, content migration

## Advanced Features

### Regular Expression Support
```csharp
private void HandleShowObjectByName(string module, string[] cmdparams)
{
    bool useRegex = false;
    OptionSet options = new OptionSet();
    options.Add("regex", v => useRegex = v != null);

    if (useRegex)
    {
        Regex nameRegex = new Regex(name);
        searchPredicate = so => nameRegex.IsMatch(so.Name);
    }
    else
    {
        searchPredicate = so => so.Name == name;
    }
}
```

### Spatial Filtering
```csharp
private bool TryParseVectorRange(IEnumerable<string> rawComponents, out Vector3 startVector, out Vector3 endVector)
{
    // Parse start and end coordinates for spatial queries
    // Validate coordinate format and ranges
    // Return bounding box for object filtering
}
```

### Safety Mechanisms
```csharp
private void HandleDeleteObject(string module, string[] cmd)
{
    if (requireConfirmation)
    {
        string response = MainConsole.Instance.Prompt(
            string.Format("Are you sure that you want to delete {0} objects from {1}",
                         deletes.Count, m_scene.RegionInfo.RegionName), "y/N");

        if (response.ToLower() != "y")
        {
            MainConsole.Instance.Output("Aborting delete of {0} objects", deletes.Count);
            return;
        }
    }
}
```

## Object Information Display

### Summary Object Report
```csharp
private StringBuilder AddSummarySceneObjectReport(StringBuilder sb, SceneObjectGroup so)
{
    ConsoleDisplayList cdl = new ConsoleDisplayList();
    cdl.AddRow("Name", so.Name);
    cdl.AddRow("Description", so.Description);
    cdl.AddRow("Local ID", so.LocalId);
    cdl.AddRow("UUID", so.UUID);
    cdl.AddRow("Location", string.Format("{0} @ {1}", so.AbsolutePosition, so.Scene.Name));
    cdl.AddRow("Parts", so.PrimCount);
    cdl.AddRow("Flags", so.RootPart.Flags);

    return sb.Append(cdl.ToString());
}
```

### Detailed Part Information
When using the --full flag, the module displays extensive part details:
- **Basic Properties**: Name, description, UUID, location, parent information
- **Link Information**: Link number, root/child status
- **Shape Properties**: Flexi, light, projection, sculpt settings
- **Texture Information**: Texture UUIDs for all faces
- **Physics Properties**: Scale, rotation, position data
- **Inventory Contents**: Complete inventory listing with script status

### Inventory Item Display
```csharp
private StringBuilder AddScenePartItemsReport(StringBuilder sb, IEntityInventory inv)
{
    ConsoleDisplayTable cdt = new ConsoleDisplayTable();
    cdt.AddColumn("Name", 50);
    cdt.AddColumn("Type", 12);
    cdt.AddColumn("Running", 7);
    cdt.AddColumn("Item UUID", 36);
    cdt.AddColumn("Asset UUID", 36);

    foreach (TaskInventoryItem item in inv.GetInventoryItems())
    {
        bool foundScriptInstance, scriptRunning;
        foundScriptInstance = SceneObjectPartInventory.TryGetScriptInstanceRunning(m_scene, item, out scriptRunning);

        cdt.AddRow(item.Name, ((InventoryType)item.InvType).ToString(),
                  foundScriptInstance ? scriptRunning.ToString() : "n/a",
                  item.ItemID.ToString(), item.AssetID.ToString());
    }
}
```

## Administrative Use Cases

### Region Maintenance
- **Cleanup Operations**: Remove abandoned objects, clear specific areas
- **Performance Analysis**: Identify high-polygon objects, complex shapes
- **Content Auditing**: Review object ownership, creator attribution
- **Spatial Management**: Clear objects outside boundaries, manage density

### Debugging and Support
- **Object Inspection**: Detailed analysis of problematic objects
- **User Support**: Help users find lost objects, verify ownership
- **Content Issues**: Investigate texture problems, script issues
- **Performance Troubleshooting**: Identify objects causing lag

### Content Management
- **Backup Operations**: Export important objects for safekeeping
- **Migration Support**: Prepare objects for region transfers
- **Inventory Analysis**: Review object contents and script status
- **Quality Control**: Verify object properties and settings

## Security and Safety Features

### Attachment Protection
```csharp
if (!so.IsAttachment && !deletes.Contains(so))
    deletes.Add(so);

if (so.IsAttachment)
{
    requireConfirmation = true;
    m_console.Output("Warning: object with uuid {0} is an attachment", uuid);
}
```

### Confirmation Requirements
- **Mass Deletions**: All bulk delete operations require confirmation
- **Attachment Deletions**: Special warnings for attachment operations
- **File Overwrites**: Export operations check for existing files
- **Abort Capability**: Users can cancel operations during confirmation

### Input Validation
```csharp
UUID uuid;
uint localId;
if (!ConsoleUtil.TryParseConsoleId(m_console, mainParams[3], out uuid, out localId))
    return;

Vector3 startVector, endVector;
if (!TryParseVectorRange(cmdparams.Skip(3).Take(3), out startVector, out endVector))
    return;
```

## Performance Considerations

### Efficient Object Lookup
- **Direct Scene Access**: Uses optimized scene object retrieval methods
- **Predicate Filtering**: Efficient lambda-based object filtering
- **Lazy Evaluation**: Commands only process objects when needed
- **Memory Management**: Minimal object instantiation for large datasets

### Large Dataset Handling
- **Incremental Processing**: Commands handle large object sets efficiently
- **Memory Optimization**: Uses StringBuilder for large output formatting
- **Batched Operations**: Deletion operations processed in batches
- **Progress Feedback**: User feedback during long-running operations

### Search Optimization
```csharp
// Efficient object enumeration
m_scene.ForEachSOG(delegate (SceneObjectGroup g)
{
    if (searchPredicate(g))
        results.Add(g);
});

// Optimized part searching
sceneObjects.ForEach(so => parts.AddRange(Array.FindAll<SceneObjectPart>(so.Parts, searchPredicate)));
```

## Error Handling and Validation

### Command Validation
```csharp
if (mainParams.Count < 4)
{
    m_console.Output("Usage: show object name [--full] [--regex] <name>");
    return;
}

if (!(m_console.ConsoleScene == null || m_console.ConsoleScene == m_scene))
    return;
```

### Safe File Operations
```csharp
string fileName = string.Format("{0}.xml", objectUuid);

if (!ConsoleUtil.CheckFileDoesNotExist(m_console, fileName))
    return;

using (XmlTextWriter xtw = new XmlTextWriter(fileName, Encoding.UTF8))
{
    xtw.Formatting = Formatting.Indented;
    SceneObjectSerializer.ToOriginalXmlFormat(so, xtw, true);
}
```

### Graceful Degradation
- **Missing Objects**: Silent handling of non-existent objects
- **Invalid Parameters**: Clear error messages for malformed input
- **Permission Issues**: Appropriate handling of access restrictions
- **Resource Constraints**: Graceful handling of memory/disk limitations

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource source)
{
    // No specific configuration required
}
```
- **No Configuration**: Module requires no external configuration
- **Always Active**: Module initializes automatically as core functionality

### Region Integration
```csharp
public void AddRegion(Scene scene)
{
    m_scene = scene;
    m_console = MainConsole.Instance;

    // Register all console commands
    m_console.Commands.AddCommand("Objects", false, "delete object owner", ...);
    m_console.Commands.AddCommand("Objects", false, "show object id", ...);
    // ... additional command registrations
}
```

### Command Registration
The module registers comprehensive command sets:
- **12 Total Commands**: Complete object manipulation suite
- **Category "Objects"**: Organized under Objects command category
- **Admin Level**: Commands require administrative console access
- **Help Integration**: All commands include detailed help text

### Cleanup
```csharp
public void RemoveRegion(Scene scene)
{
    // Commands automatically cleaned up by console system
}
```

## Integration Examples

### Basic Object Inspection
```bash
# Find object by name
show object name "My Object"

# Get detailed info including all parts
show object name --full "My Object"

# Search with regex pattern
show object name --regex "Test.*"

# Show specific object by UUID
show object id 12345678-1234-1234-1234-123456789abc
```

### Object Cleanup Operations
```bash
# Delete all objects by specific owner
delete object owner 12345678-1234-1234-1234-123456789abc

# Clean up objects outside region boundaries
delete object outside

# Remove objects in specific area
delete object pos <100,100,20> <150,150,50>

# Delete objects matching name pattern
delete object name --regex "Temp.*"
```

### Backup and Export
```bash
# Export specific object to XML
dump object id 12345678-1234-1234-1234-123456789abc

# First find objects, then export them
show object owner 12345678-1234-1234-1234-123456789abc
dump object id <identified-uuid>
```

### Advanced Inspection
```bash
# Show all objects in specific area with full details
show object pos --full <0,0,0> <100,100,100>

# Find parts by name across all objects
show part name --regex "Door.*"

# Get detailed part information
show part id 12345678-1234-1234-1234-123456789abc
```

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Always Enabled**: Module loaded by default as essential functionality
- **No Configuration**: Module requires no configuration settings
- **Logging Integration**: Comprehensive debug and info logging for operations

### Backward Compatibility
- **Command Compatibility**: All existing console commands remain unchanged
- **Output Format**: Console output format and structure unchanged
- **Parameter Syntax**: All command parameters and options unchanged
- **Help System**: Command help and documentation remain identical

### Dependencies
- **Console System**: Requires MainConsole.Instance for command registration
- **Scene Management**: Integration with scene and region lifecycle
- **Serialization**: Depends on SceneObjectSerializer for XML export
- **Utility Libraries**: Uses ConsoleUtil, NDesk.Options for command parsing

## Troubleshooting

### Common Issues

#### Commands Not Available
- **Module Loading**: Verify ObjectCommandsModule is loaded in factory
- **Console Access**: Ensure administrative console access
- **Command Registration**: Check for command registration errors in logs
- **Scene Context**: Verify commands are run in correct scene context

#### Object Not Found
- **UUID Validation**: Verify object UUIDs are correct and exist
- **Local ID Changes**: Local IDs may change across region restarts
- **Object Deletion**: Object may have been deleted by another process
- **Scene Synchronization**: Ensure scene is fully loaded and synchronized

#### Export/Dump Failures
- **File Permissions**: Check write permissions for export directory
- **Disk Space**: Verify sufficient disk space for export files
- **File Conflicts**: Existing files prevent overwriting (by design)
- **Object Complexity**: Very complex objects may cause serialization issues

#### Performance Issues
- **Large Object Sets**: Mass operations on thousands of objects may be slow
- **Complex Objects**: High-detail objects take longer to process
- **Regex Performance**: Complex regular expressions may impact search speed
- **Memory Usage**: Large result sets may consume significant memory

## Usage Examples

### Daily Administration
```bash
# Check for objects outside boundaries
delete object outside

# Review objects by specific troublesome user
show object owner 12345678-1234-1234-1234-123456789abc

# Clean up test objects
delete object name --regex "Test.*"
```

### Content Management
```bash
# Backup important objects
show object name "Important Sculpture"
dump object id <found-uuid>

# Find all objects in central plaza
show object pos <90,90,20> <110,110,40>

# Review object ownership in area
show object pos --full <0,0,0> <256,256,100>
```

### Debugging Support
```bash
# Investigate problematic object
show object id --full 12345678-1234-1234-1234-123456789abc

# Check object parts individually
show part name "Problematic Part"

# Review object inventory and scripts
show part id --full 12345678-1234-1234-1234-123456789abc
```

This documentation reflects the ObjectCommandsModule implementation in `src/OpenSim.Region.CoreModules/World/Objects/Commands/ObjectCommandsModule.cs` and its integration with the factory-based module loading system.