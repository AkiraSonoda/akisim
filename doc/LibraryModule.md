# LibraryModule

## Overview

The LibraryModule is a shared region module that provides library services for OpenSimulator. It manages the loading and initialization of library content from inventory archive (.iar) files, making predefined assets and inventory items available to all users in the virtual world.

## Technical Specification

### Class Information
- **Namespace**: `OpenSim.Region.CoreModules.Framework.Library`
- **Type**: `ISharedRegionModule` implementation
- **Assembly**: `OpenSim.Region.CoreModules`

### Key Features

1. **Library Service Integration**: Interfaces with `ILibraryService` to provide library functionality
2. **Archive Loading**: Automatically loads .iar files from the Library directory
3. **Permission Management**: Sets appropriate permissions on library items
4. **Shared Module**: Single instance serves all regions in the simulator

### Configuration

The module is controlled by the following configuration options in the `[Modules]` section:

```ini
[Modules]
LibraryModule = true  ; Enable the library module (default: false)
```

Additional configuration in the `[LibraryService]` section:

```ini
[LibraryService]
LocalServiceModule = "OpenSim.Services.InventoryService.dll:LibraryService"
```

### Architecture

#### Dependencies
- **ILibraryService**: Core library service interface
- **IInventoryService**: For inventory operations during archive loading
- **IAssetService**: For asset management during archive loading
- **InventoryArchiveReadRequest**: For processing .iar files

#### Key Components

1. **Library Service Loading**:
   - Loads the library service implementation via reflection
   - Configurable via `LocalServiceModule` setting

2. **Archive Processing**:
   - Scans the `Library/` directory for .iar files
   - Creates mock scene for archive loading operations
   - Processes each archive using `InventoryArchiveReadRequest`

3. **Permission Handling**:
   - Sets full permissions for base permissions
   - Restricts modify permissions for everyone
   - Ensures proper permission inheritance

### Lifecycle

#### Initialization Phase
1. **Initialise()**: Reads configuration and loads library service
2. **AddRegion()**: Registers library service interface with scene
3. **RegionLoaded()**: Loads library archives (once per simulator instance)

#### Runtime Phase
- Provides library service interface to other modules
- Serves library content to inventory requests

#### Shutdown Phase
- **RemoveRegion()**: Unregisters library service interface
- **Close()**: Cleanup operations

### Archive Loading Process

1. **Directory Scan**: Scans `Library/` directory for .iar files
2. **Mock Scene Creation**: Creates temporary scene for loading operations
3. **Service Registration**: Registers required services (inventory, assets)
4. **Archive Processing**:
   - Attempts to load into specified subfolder
   - Falls back to root level if subfolder not found
5. **Permission Fixup**: Sets appropriate permissions on all loaded items

### File Structure

```
Library/
├── archive1.iar    # Library archive files
├── archive2.iar
└── ...
```

### Permission Model

Library items use the following permission structure:
- **Base Permissions**: Full permissions (All)
- **Everyone Permissions**: All except Modify
- **Current Permissions**: Full permissions (All)
- **Next Permissions**: Full permissions (All)

### Integration Points

#### Service Registration
The module registers `ILibraryService` with each scene, making library functionality available to:
- Inventory modules
- Asset services
- Client inventory requests
- Other modules requiring library access

#### Archive Integration
Works closely with the inventory archiver system:
- Uses `InventoryArchiveReadRequest` for .iar processing
- Integrates with asset and inventory services
- Handles archive format compatibility

### Error Handling

- **Missing Library Service**: Module disables itself if service cannot be loaded
- **Archive Processing Errors**: Logs errors but continues with remaining archives
- **Permission Failures**: Handles gracefully with debug logging

### Performance Considerations

- **Single Instance**: Shared module reduces memory overhead
- **One-Time Loading**: Archives loaded only once per simulator startup
- **Lazy Initialization**: Service loading deferred until needed
- **Mock Scene Usage**: Minimal overhead for archive processing

### Logging

The module provides comprehensive logging at different levels:
- **Debug**: Detailed operation information
- **Info**: Archive loading progress
- **Warn**: Configuration or service issues
- **Error**: Critical failures (via exception handling)

### Migration Notes

This module has been migrated from Mono.Addins to the OptionalModulesFactory system:
- Removed `[Extension]` attribute
- Added factory registration in `OptionalModulesFactory`
- Maintained full backward compatibility
- Enhanced logging for better diagnostics

### Usage Examples

#### Basic Configuration
```ini
[Modules]
LibraryModule = true

[LibraryService]
LocalServiceModule = "OpenSim.Services.InventoryService.dll:LibraryService"
```

#### Archive Preparation
1. Place .iar files in the `Library/` directory
2. Name files descriptively (name becomes folder path)
3. Ensure proper permissions in source inventory

### Related Modules

- **InventoryArchiverModule**: For creating/managing .iar files
- **InventoryService**: Core inventory functionality
- **AssetService**: Asset storage and retrieval
- **LibraryService**: Backend library implementation

### See Also

- [InventoryArchiverModule.md](InventoryArchiverModule.md)
- [ArchiverModule.md](ArchiverModule.md)
- OpenSim Library Documentation
- Inventory Archive Format Specification