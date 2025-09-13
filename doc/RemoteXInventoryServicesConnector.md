# RemoteXInventoryServicesConnector

## Overview

The `RemoteXInventoryServicesConnector` is a region module that provides distributed inventory services functionality for OpenSimulator grid deployments. This module enables centralized inventory management across multiple regions, supporting comprehensive inventory operations including folders, items, and asset permissions with seamless integration to remote inventory service endpoints.

## Purpose

This connector enables grid-wide inventory management by:
- Connecting regions to centralized inventory data services
- Providing distributed inventory operations across multiple regions
- Supporting comprehensive folder and item management operations
- Enabling asset permission queries and inventory skeleton retrieval
- Managing inventory content distribution throughout the grid infrastructure

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`, `IInventoryService`
- **Namespace**: `OpenSim.Region.CoreModules.ServiceConnectorsOut.Inventory`
- **Base Class**: None (direct implementation)
- **Dependencies**: Requires `XInventoryServicesConnector` for remote service communication

### Key Components

#### Core Functionality
- **Remote Service Integration**: Uses `XInventoryServicesConnector` for remote service communication
- **Inventory Operations**: Comprehensive support for folder and item operations
- **Asset Permissions**: Supports asset permission queries for access control
- **User Management Integration**: Integrates with `IUserManagement` for creator data handling
- **Service Interface**: Implements `IInventoryService` for complete inventory operations

#### Configuration Management
- Requires `[Modules]` configuration section with `InventoryServices` setting
- Enables when `InventoryServices = "RemoteXInventoryServicesConnector"`
- Uses `XInventoryServicesConnector` configuration for remote service endpoints
- Validates required configuration sections with comprehensive error handling

## Configuration

### Required Configuration Sections

#### Module Configuration
```ini
[Modules]
InventoryServices = RemoteXInventoryServicesConnector
```

#### Service Configuration
```ini
[XInventoryService]
; XInventory service configuration for remote connectivity
; Additional configuration depends on XInventoryServicesConnector requirements
```

### Complete Grid Mode Configuration Example
```ini
[Modules]
InventoryServices = RemoteXInventoryServicesConnector

[XInventoryService]
; Remote inventory service configuration
; Specific settings depend on your inventory service deployment
```

## Implementation Details

### Module Lifecycle

1. **Initialization** (`Initialise`)
   - Validates `[Modules]` section and `InventoryServices` configuration
   - Calls `Init(source)` to create and configure `XInventoryServicesConnector` instance
   - Provides comprehensive configuration validation and error logging
   - Enables module when proper configuration is detected

2. **Post-Initialization** (`PostInitialise`)
   - Completes post-initialization setup for enabled modules
   - Provides debug logging for initialization completion

3. **Region Integration** (`AddRegion`)
   - Registers `IInventoryService` interface with the scene
   - Sets scene reference for user management integration
   - Provides informational logging for region integration

4. **Region Loading** (`RegionLoaded`)
   - Completes region-specific initialization
   - Logs successful region loading with remote inventory services
   - Provides debug logging for region loading completion

5. **Cleanup** (`RemoveRegion`, `Close`)
   - Handles clean removal from regions
   - Provides debug logging for removal and closure operations

### Service Operations

The module implements comprehensive `IInventoryService` operations:

#### Inventory Skeleton Operations
```csharp
public List<InventoryFolderBase> GetInventorySkeleton(UUID userId)
public InventoryFolderBase GetRootFolder(UUID userID)
public InventoryFolderBase GetFolderForType(UUID userID, FolderType type)
```
- Retrieves inventory structure and root folders
- Supports typed folder lookups for specific inventory categories
- Delegates to remote connector for distributed inventory access

#### Folder Operations
```csharp
public InventoryCollection GetFolderContent(UUID userID, UUID folderID)
public InventoryCollection[] GetMultipleFoldersContent(UUID principalID, UUID[] folderIDs)
public List<InventoryItemBase> GetFolderItems(UUID userID, UUID folderID)
```
- Comprehensive folder content retrieval with batch support
- Individual folder item access for targeted operations
- Includes commented-out user management integration for creator data

#### Folder Management
```csharp
public bool AddFolder(InventoryFolderBase folder)
public bool UpdateFolder(InventoryFolderBase folder)
public bool MoveFolder(InventoryFolderBase folder)
public bool DeleteFolders(UUID ownerID, List<UUID> folderIDs)
public bool PurgeFolder(InventoryFolderBase folder)
```
- Complete folder lifecycle management operations
- Null validation for all folder operations
- Batch deletion support for efficient folder cleanup
- All operations delegate to remote connector

#### Item Operations
```csharp
public bool AddItem(InventoryItemBase item)
public bool UpdateItem(InventoryItemBase item)
public bool MoveItems(UUID ownerID, List<InventoryItemBase> items)
public bool DeleteItems(UUID ownerID, List<UUID> itemIDs)
```
- Comprehensive item management operations with null validation
- Batch operations for efficient item handling
- Move operations support ownership validation
- Empty list handling for delete operations

#### Item Retrieval
```csharp
public InventoryItemBase GetItem(UUID userID, UUID itemID)
public InventoryItemBase[] GetMultipleItems(UUID userID, UUID[] itemIDs)
public InventoryFolderBase GetFolder(UUID userID, UUID folderID)
```
- Individual and batch item retrieval operations
- Enhanced logging with parameter and result tracking
- Remote connector validation with error handling
- Detailed success/failure logging for debugging

#### Special Operations
```csharp
public bool CreateUserInventory(UUID user)
public bool HasInventoryForUser(UUID userID)
public List<InventoryItemBase> GetActiveGestures(UUID userId)
public int GetAssetPermissions(UUID userID, UUID assetID)
```
- **CreateUserInventory**: Returns false (not supported by remote connector)
- **HasInventoryForUser**: Returns false (remote connector limitation)
- **GetActiveGestures**: Returns empty list (remote connector limitation)
- **GetAssetPermissions**: Delegates to remote connector for permission queries

### User Management Integration

#### IUserManagement Integration
- **Scene-Based Access**: Retrieves `IUserManagement` module from scene
- **Creator Data Support**: Commented-out code for handling creator data from items
- **Error Handling**: Comprehensive error logging when user management is unavailable
- **Lazy Loading**: User management module loaded on first access

#### Creator Data Processing
The module includes extensive (currently commented) code for creator data handling:
- Processes creator data from inventory items
- Integrates with user management for creator information
- Designed for asynchronous creator data processing
- Can be enabled for comprehensive creator tracking

### Logging and Diagnostics

The module provides extensive logging for inventory operations:

- **Info Level**: Module enablement and major configuration events
- **Debug Level**: Detailed operation tracking, parameter logging, and result validation
- **Error Level**: Configuration validation failures, connector errors, and service failures
- **Operation Logging**: Comprehensive logging of inventory operations with success/failure tracking

#### Log Examples
```
Remote XInventory connector enabled for distributed inventory services
Using XInventoryServicesConnector for remote service communication
Added to region RegionName and registered IInventoryService interface
Remote XInventory enabled for region RegionName
GetItem requested for user uuid, item itemId
GetItem successful for item itemId (Name: ItemName)
GetFolder requested for user uuid, folder folderId
GetFolder successful for folder folderId (Name: FolderName)
Remote connector is null - cannot retrieve item
```

## Integration with OptionalModulesFactory

This module has been integrated into the `OptionalModulesFactory` pattern, removing dependency on Mono.Addins:

### Factory Integration
- Loaded through `OptionalModulesFactory.CreateOptionalSharedModules()`
- Configuration-based instantiation using `InventoryServices` setting
- Comprehensive logging for factory operations
- Direct implementation without inheritance dependencies

### Migration from Mono.Addins
- Removed from `OpenSim.Region.CoreModules.addin.xml`
- Added to `OptionalModulesFactory` for dynamic loading
- Preserves full compatibility with existing configurations
- Maintains single-configuration section requirement

## Usage Scenarios

### Grid Deployments
- **Distributed Grids**: Central inventory services with multiple region servers
- **Hypergrid Configurations**: Cross-grid inventory synchronization
- **Scalable Architectures**: Centralized inventory data with distributed region processing

### Inventory Operations
- **Asset Management**: Comprehensive folder and item operations
- **User Inventory**: Complete inventory access and management
- **Permission Queries**: Asset permission validation and access control
- **Cross-Region Consistency**: Ensuring consistent inventory data across regions

### Service Dependencies
- Requires functional remote inventory service endpoints
- Depends on proper `XInventoryServicesConnector` configuration
- Integrates with existing OpenSimulator inventory infrastructure

## Troubleshooting

### Common Issues

1. **Module Not Loading**
   - Verify `InventoryServices = RemoteXInventoryServicesConnector` in `[Modules]` section
   - Check log output for configuration validation messages
   - Confirm OptionalModulesFactory integration
   - Verify XInventoryServicesConnector configuration

2. **Configuration Validation Failures**
   - Verify `[Modules]` section exists with correct `InventoryServices` setting
   - Check for typos in configuration keys and values
   - Review XInventory service endpoint configuration
   - Enable debug logging for detailed validation information

3. **Remote Service Connection Issues**
   - Verify remote inventory service endpoint configuration
   - Check network connectivity to grid services
   - Review XInventoryServicesConnector settings and accessibility
   - Monitor service availability and response times

4. **Connector Null Issues**
   - Monitor remote connector initialization in logs
   - Verify XInventoryServicesConnector configuration completeness
   - Check for initialization failures in post-initialization phase
   - Review XInventory service availability during startup

### Debug Configuration
Enable detailed logging by setting log4net configuration:

```xml
<logger name="OpenSim.Region.CoreModules.ServiceConnectorsOut.Inventory">
    <level value="DEBUG" />
    <appender-ref ref="Console" />
    <appender-ref ref="LogFileAppender" />
</logger>
```

## Related Components

- **XInventoryServicesConnector**: Core inventory service connector for remote communication
- **OptionalModulesFactory**: Factory pattern for dynamic module loading
- **IInventoryService**: Service interface for inventory operations
- **IUserManagement**: User management integration for creator data
- **InventoryFolderBase**: Data structure for inventory folders
- **InventoryItemBase**: Data structure for inventory items
- **InventoryCollection**: Container for folder content operations

## Development Notes

### Code Quality
- Follows established OpenSimulator coding patterns
- Direct implementation of required interfaces
- Includes comprehensive error handling and logging
- Implements proper module lifecycle management

### Performance Considerations
- Direct delegation to remote connector for efficiency
- Batch operations optimize multiple inventory operations
- Minimal local processing with remote service delegation
- Efficient null validation and error handling

### Maintenance
- Part of the OptionalModulesFactory modernization effort
- Removed Mono.Addins dependency for improved maintainability
- Follows consistent logging and configuration patterns
- Maintains backward compatibility with existing configurations

## Security Considerations

### Remote-Only Operations
- **Service Delegation**: All operations delegate to remote connector
- **No Local Fallback**: Operations fail if remote service unavailable
- **Validation**: Comprehensive null validation prevents invalid operations
- **Error Logging**: All operation failures are logged for audit purposes

### Access Control
- **Asset Permissions**: Supports asset permission queries for access validation
- **User Validation**: Operations require valid user and item/folder identifiers
- **Remote Authorization**: Access control handled by remote inventory services

## Version History

- **Current**: Integrated with OptionalModulesFactory, enhanced logging, comprehensive operation support, removed Mono.Addins dependency
- **Previous**: Mono.Addins-based loading with basic operation logging

This module represents a modernized approach to inventory service connectivity in OpenSimulator, providing robust distributed inventory functionality with comprehensive operation support, improved maintainability, detailed operational visibility, and efficient inventory management across distributed grid environments.