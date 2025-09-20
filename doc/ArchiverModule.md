# ArchiverModule Technical Documentation

## Overview

The `ArchiverModule` is a non-shared region module that provides comprehensive functionality for saving and loading OpenSimulator region archives (OAR files). It implements the `IRegionArchiverModule` interface and operates at the region level, enabling users to backup entire regions including terrain, objects, assets, and metadata.

## Architecture

### Module Classification
- **Type**: Non-shared Region Module (`INonSharedRegionModule`)
- **Interface**: `IRegionArchiverModule`
- **Namespace**: `OpenSim.Region.CoreModules.World.Archiver`
- **Assembly**: `OpenSim.Region.CoreModules`

### Key Dependencies
- **Framework**: OpenSim.Framework, OpenSim.Region.Framework
- **Logging**: log4net for comprehensive operation logging
- **Configuration**: Nini.Config for configuration management
- **Command Line**: NDesk.Options for parameter parsing
- **Scene Management**: OpenSim.Region.Framework.Scenes

## Functionality

### Core Operations

#### 1. Region Archiving (Save)
Saves a complete region to an OAR (OpenSimulator Archive) file format.

**Key Features:**
- Complete scene serialization including objects, terrain, and parcels
- Asset inclusion with optional filtering
- Metadata preservation (ownership, permissions, scripts)
- Configurable export options

**Console Command:**
```
save oar [options] [filename]
```

**Available Options:**
- `--home=<url>`: Set home URL for assets
- `--noassets`: Exclude assets from archive
- `--publish`: Remove ownership information (anonymize)
- `--perm=<value>`: Set permission checking level
- `--all`: Include all region data

#### 2. Region Dearchiving (Load)
Loads a region from an OAR file, restoring objects, terrain, and assets.

**Key Features:**
- Selective loading with merge capabilities
- Object placement with displacement and rotation
- Asset restoration and validation
- Conflict resolution for existing content

**Console Command:**
```
load oar [options] [filename]
```

**Available Options:**
- `--merge`: Merge with existing region content
- `--mergeReplaceObjects`: Replace existing objects during merge
- `--skip-assets`: Skip asset loading
- `--merge-terrain`: Merge terrain data
- `--merge-parcels`: Merge parcel information
- `--no-objects`: Skip object loading
- `--default-user=<name>`: Set default user for orphaned objects
- `--displacement=<vector>`: Offset loaded objects by specified vector
- `--rotation=<degrees>`: Rotate loaded content
- `--bounding-origin=<vector>`: Set bounding box origin for selective loading
- `--bounding-size=<vector>`: Set bounding box size for selective loading
- `--debug`: Enable debug output

### Implementation Details

#### Module Lifecycle
1. **Initialization**: Configuration parsing and setup
2. **Region Addition**: Registration with scene and interface exposure
3. **Region Loading**: Module activation and service registration
4. **Operation**: Archive/dearchive operations as requested
5. **Cleanup**: Resource disposal and deregistration

#### Archive Operations

**ArchiveWriteRequest**
- Handles the serialization process
- Manages asset collection and validation
- Provides progress reporting
- Supports both file and stream output

**ArchiveReadRequest**
- Manages deserialization and restoration
- Handles merge operations and conflict resolution
- Supports selective loading based on bounding boxes
- Manages asset restoration and validation

### Configuration

#### Module Configuration
The module is configured through the `[Modules]` section in OpenSim configuration:

```ini
[Modules]
ArchiverModule = true  ; Enable/disable the module (default: true)
```

#### Operation Parameters
All archive operations support extensive parameterization through command-line options, allowing fine-grained control over the archiving process.

### Console Commands

#### Save Operations
```bash
# Basic save
save oar myregion.oar

# Save without assets
save oar --noassets myregion.oar

# Anonymized save for distribution
save oar --publish public_region.oar
```

#### Load Operations
```bash
# Basic load (replaces existing content)
load oar myregion.oar

# Merge with existing content
load oar --merge --merge-terrain additional_content.oar

# Load with displacement
load oar --displacement "<100,100,0>" shifted_content.oar

# Selective loading with bounding box
load oar --bounding-origin "<0,0,0>" --bounding-size "<128,128,100>" partial_load.oar
```

### Error Handling

The module implements comprehensive error handling:
- **Parameter Validation**: Command-line arguments are validated before processing
- **File Operations**: File access errors are caught and reported
- **Asset Management**: Missing or corrupted assets are logged and handled gracefully
- **Scene Consistency**: Operations maintain scene integrity during all phases

### Logging

Extensive logging is provided at multiple levels:
- **Info**: Major operation start/completion
- **Debug**: Detailed parameter parsing and validation
- **Error**: Operation failures and invalid parameters
- **Warning**: Non-fatal issues and deprecated options

### Performance Considerations

#### Memory Management
- Streaming operations for large files
- Progressive asset loading to manage memory usage
- Efficient serialization formats

#### Scalability
- Supports regions of varying sizes
- Handles large asset collections
- Optimized for both small and large-scale operations

### Security and Permissions

#### Asset Security
- Validates asset permissions during operations
- Supports permission-based filtering
- Maintains ownership information unless anonymized

#### User Security
- Validates user permissions for archive operations
- Supports default user assignment for orphaned content
- Maintains audit trails through logging

### Integration Points

#### Scene Integration
- Registers as `IRegionArchiverModule` service
- Integrates with scene object management
- Coordinates with asset services

#### Command Integration
- Exposes console commands for administrative access
- Supports both interactive and automated operations
- Provides comprehensive parameter validation

### Migration from Mono.Addins

As part of the modernization effort, this module has been migrated from the Mono.Addins plugin system to the OptionalModulesFactory pattern:

#### Previous Implementation
- Used `[Extension]` attribute for automatic discovery
- Loaded through Mono.Addins reflection mechanisms

#### Current Implementation
- Instantiated through `OptionalModulesFactory.CreateOptionalRegionModules()`
- Configuration-driven loading with explicit logging
- Improved error handling and diagnostics

#### Benefits
- Reduced dependency on external plugin frameworks
- Better control over module lifecycle
- Enhanced logging and diagnostics
- Improved configuration flexibility

### Usage Examples

#### Complete Region Backup
```bash
# Create a complete backup including all assets
save oar --all complete_backup.oar
```

#### Region Migration
```bash
# Export from source region
save oar --home="http://newgrid.com/assets" migration_export.oar

# Import to destination region
load oar migration_export.oar
```

#### Content Merging
```bash
# Add content to existing region
load oar --merge --displacement "<200,200,0>" additional_buildings.oar
```

#### Development and Testing
```bash
# Quick save without assets for testing
save oar --noassets test_region.oar

# Debug load with detailed logging
load oar --debug --merge test_content.oar
```

## Best Practices

1. **Regular Backups**: Schedule regular region saves for disaster recovery
2. **Asset Management**: Use `--noassets` for quick structural backups
3. **Merge Operations**: Test merge operations in development environments first
4. **Parameter Validation**: Always validate displacement and rotation parameters
5. **Monitoring**: Monitor logs during large archive operations
6. **Security**: Use `--publish` option when distributing regions publicly

## Troubleshooting

### Common Issues
- **File Access**: Ensure proper file system permissions for archive files
- **Memory Usage**: Monitor memory consumption during large archive operations
- **Asset Conflicts**: Use asset validation options when loading archives
- **Parameter Errors**: Validate vector parameters format before operations

### Diagnostic Tools
- Enable debug logging for detailed operation traces
- Use parameter validation before complex operations
- Monitor system resources during large archive operations

This module provides essential functionality for OpenSimulator region management, enabling comprehensive backup, restore, and migration capabilities while maintaining flexibility and performance.