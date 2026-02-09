# InventoryArchiverModule Technical Documentation

## Overview

The `InventoryArchiverModule` is a shared region module that provides comprehensive functionality for saving and loading OpenSimulator inventory archives (IAR files). It implements the `IInventoryArchiverModule` interface and operates across multiple regions, enabling users to backup and restore individual user inventories including items, folders, assets, and metadata.

## Architecture

### Module Classification
- **Type**: Shared Region Module (`ISharedRegionModule`)
- **Interface**: `IInventoryArchiverModule`
- **Namespace**: `OpenSim.Region.CoreModules.Avatar.Inventory.Archiver`
- **Assembly**: `OpenSim.Region.CoreModules`

### Key Dependencies
- **Framework**: OpenSim.Framework, OpenSim.Region.Framework
- **Services**: OpenSim.Services.Interfaces for user account and authentication
- **Logging**: log4net for comprehensive operation logging
- **Configuration**: Nini.Config for configuration management
- **Command Line**: NDesk.Options for parameter parsing
- **Scene Management**: OpenSim.Region.Framework.Scenes

## Functionality

### Core Operations

#### 1. Inventory Archiving (Save)
Saves a user's complete inventory or specific inventory paths to an IAR (Inventory Archive) file format.

**Key Features:**
- Complete inventory serialization including items, folders, and assets
- Selective saving by inventory path
- Asset inclusion with optional filtering
- Creator information preservation
- Permission-based filtering
- Folder and item exclusion capabilities

**Console Command:**
```
save iar [options] <first> <last> <inventory path> <password> [<IAR path>]
```

**Available Options:**
- `--home=<url>`: Add profile service URL to saved user information
- `--noassets`: Exclude assets from archive
- `--creators`: Preserve foreign creator information
- `--exclude=<name/uuid>`: Exclude specific inventory items
- `--excludefolder=<folder/uuid>`: Exclude folder contents
- `--verbose`: Enable detailed debug output
- `--perm=<permissions>`: Filter items by permissions (C=Copy, T=Transfer, M=Modify)

#### 2. Inventory Dearchiving (Load)
Loads inventory from an IAR file, restoring items, folders, and assets to a user's inventory.

**Key Features:**
- Complete inventory restoration
- Merge capabilities with existing inventory
- Asset validation and restoration
- Inventory path targeting
- User authentication and validation

**Console Command:**
```
load iar [options] <first> <last> <inventory path> <password> [<IAR path>]
```

**Available Options:**
- `--merge`: Merge with existing inventory folders where possible

### Implementation Details

#### Module Lifecycle
1. **Initialization**: Configuration parsing and setup
2. **Region Addition**: Registration with first scene and interface exposure
3. **Multi-Region Support**: Tracking of all scenes in the simulator
4. **Service Access**: Dynamic service interface resolution
5. **Operation**: Archive/dearchive operations as requested
6. **Cleanup**: Resource disposal and deregistration

#### Archive Operations

**InventoryArchiveWriteRequest**
- Handles the serialization process for inventory items and folders
- Manages asset collection and validation
- Provides progress reporting and completion callbacks
- Supports both file and stream output
- Implements filtering based on permissions and exclusion lists

**InventoryArchiveReadRequest**
- Manages deserialization and restoration of inventory data
- Handles merge operations with existing inventory
- Supports selective loading to specific inventory paths
- Manages asset restoration and validation
- Updates connected clients with loaded inventory nodes

#### User Authentication
The module implements robust user authentication:
- Username/password validation through authentication services
- MD5 password hashing for security
- Comprehensive error handling for authentication failures
- Support for multi-user operations across regions

### Configuration

#### Module Configuration
The module is configured through the `[Modules]` section in OpenSim configuration:

```ini
[Modules]
InventoryArchiverModule = true  ; Enable/disable the module (default: true)
```

#### Operation Parameters
All archive operations support extensive parameterization through command-line options, allowing fine-grained control over the archiving process.

### Console Commands

#### Save Operations
```bash
# Basic inventory save
save iar John Doe "/Objects" mypassword johndoe_objects.iar

# Save without assets
save iar --noassets John Doe "/Clothing" mypassword clothing_backup.iar

# Save with creator information preserved
save iar --creators --home="http://mygrid.com" John Doe "/" mypassword complete_backup.iar

# Save with exclusions
save iar --exclude="Trash" --excludefolder="Temporary" John Doe "/Objects" mypassword filtered_backup.iar

# Save with permission filtering
save iar --perm="CT" John Doe "/Assets" mypassword transferable_items.iar
```

#### Load Operations
```bash
# Basic inventory load
load iar John Doe "/Restored" mypassword backup.iar

# Load with merge capability
load iar --merge John Doe "/Objects" mypassword additional_objects.iar

# Load to root inventory
load iar John Doe "/" mypassword complete_restore.iar
```

### Events and Callbacks

#### Event System
The module provides comprehensive event notifications:

**InventoryArchiveSaved Event**
- Triggered upon completion of save operations
- Provides success status, user information, and item counts
- Includes error reporting for failed operations

**InventoryArchiveLoaded Event**
- Triggered upon completion of load operations
- Provides success status and loaded item counts
- Includes error reporting for failed operations

#### Console Integration
- Automatic tracking of console-initiated operations
- Progress reporting for long-running operations
- Comprehensive success/failure reporting with statistics

### Error Handling

The module implements comprehensive error handling:
- **Authentication**: User validation and password verification
- **File Operations**: File access errors are caught and reported
- **Asset Management**: Missing or corrupted assets are logged and handled gracefully
- **Inventory Operations**: Inventory service failures are properly managed
- **Compression**: Special handling for compression library mismatches

### Logging

Extensive logging is provided at multiple levels:
- **Info**: Major operation start/completion with statistics
- **Debug**: Detailed parameter parsing and validation
- **Error**: Operation failures, authentication errors, and invalid parameters
- **Warning**: Compatibility warnings and deprecated features

### Performance Considerations

#### Memory Management
- Streaming operations for large inventory archives
- Progressive asset loading to manage memory usage
- Efficient serialization formats
- Automatic cleanup of temporary resources

#### Scalability
- Supports inventories of varying sizes
- Handles large asset collections efficiently
- Optimized for both small and large-scale operations
- Multi-region support for distributed inventories

#### Network Efficiency
- Compressed archive formats
- Efficient asset bundling
- Progress reporting for long operations

### Security and Permissions

#### User Security
- Strong password authentication using MD5 hashing
- User presence validation across regions
- Comprehensive audit trails through logging
- Protection against unauthorized inventory access

#### Asset Security
- Validates asset permissions during operations
- Supports permission-based filtering during saves
- Maintains asset integrity during transfers
- Prevents unauthorized asset access

#### Inventory Protection
- Validates inventory folder permissions
- Supports selective folder exclusion
- Maintains inventory hierarchy integrity
- Prevents inventory corruption during operations

### Integration Points

#### Service Integration
- Integrates with `IUserAccountService` for user management
- Uses `IAuthenticationService` for password validation
- Coordinates with `IInventoryService` for inventory operations
- Interacts with `IAssetService` for asset management

#### Client Integration
- Updates connected clients with loaded inventory changes
- Sends bulk inventory updates for efficiency
- Handles client disconnection scenarios gracefully

#### Command Integration
- Exposes comprehensive console commands for administrative access
- Supports both interactive and automated operations
- Provides detailed parameter validation and help text

### Migration from Mono.Addins

As part of the modernization effort, this module has been migrated from the Mono.Addins plugin system to the OptionalModulesFactory pattern:

#### Previous Implementation
- Used `[Extension]` attribute for automatic discovery
- Loaded through Mono.Addins reflection mechanisms

#### Current Implementation
- Instantiated through `OptionalModulesFactory.CreateOptionalSharedModules()`
- Configuration-driven loading with explicit logging
- Improved error handling and diagnostics

#### Benefits
- Reduced dependency on external plugin frameworks
- Better control over module lifecycle
- Enhanced logging and diagnostics
- Improved configuration flexibility
- Consistent loading patterns across modules

### Advanced Features

#### Filtering Capabilities
- **Permission-based filtering**: Filter items based on Copy, Transfer, and Modify permissions
- **Item exclusion**: Exclude specific items by name or UUID
- **Folder exclusion**: Exclude entire folders and their contents
- **Asset filtering**: Optional exclusion of assets for structure-only backups

#### Creator Information
- Preservation of foreign creator information when enabled
- Compatibility warnings for older OpenSim versions
- Home URL integration for distributed grid operations

#### Merge Operations
- Intelligent merging with existing inventory structures
- Conflict resolution for duplicate folder names
- Preservation of existing inventory organization

### Usage Examples

#### Complete User Backup
```bash
# Create a complete backup with all assets and creator info
save iar --creators --home="http://mygrid.com/profiles" John Doe "/" password complete_backup.iar
```

#### Selective Content Export
```bash
# Export only transferable items from specific folders
save iar --perm="T" --excludefolder="Trash" John Doe "/Objects" password export_items.iar
```

#### Content Migration
```bash
# Export from source grid
save iar --creators --home="http://newgrid.com/profiles" Jane Smith "/" password migration_export.iar

# Import to destination user
load iar --merge John Doe "/Imported" password migration_export.iar
```

#### Development and Testing
```bash
# Quick structural backup without assets
save iar --noassets --verbose John Doe "/Scripts" password test_structure.iar

# Test load with detailed logging
load iar --merge --verbose John Doe "/Test" password test_content.iar
```

### Best Practices

1. **Regular Backups**: Schedule regular inventory saves for important users
2. **Asset Management**: Use `--noassets` for quick structural backups
3. **Permission Filtering**: Use permission filters when creating distribution packages
4. **Merge Operations**: Always test merge operations in development environments
5. **Creator Information**: Preserve creator info for grid migrations
6. **Security**: Always validate user credentials before operations
7. **Monitoring**: Monitor logs during large inventory operations

### Troubleshooting

#### Common Issues
- **Authentication Failures**: Verify user credentials and account status
- **Compression Errors**: Check zlib1g library compatibility with Mono
- **Large Operations**: Monitor memory usage during extensive inventory operations
- **Asset Conflicts**: Use asset validation options when loading archives
- **Permission Errors**: Validate user permissions for inventory access

#### Diagnostic Tools
- Enable verbose logging for detailed operation traces
- Use console commands for testing specific scenarios
- Monitor system resources during large operations
- Check authentication service availability

#### Known Limitations
- Presence checking is currently disabled (commented out)
- Large inventories may require significant memory
- Cross-grid compatibility depends on asset service availability

This module provides essential functionality for OpenSimulator inventory management, enabling comprehensive backup, restore, and migration capabilities while maintaining security, performance, and flexibility.