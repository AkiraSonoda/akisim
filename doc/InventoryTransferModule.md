# InventoryTransferModule

## Overview

The InventoryTransferModule is a shared region module that handles inventory transfers between avatars in OpenSimulator. It manages the exchange of inventory items and folders through instant messaging, supporting both local and remote (grid-wide) transfers. The module handles user interactions for giving, accepting, and declining inventory offers.

## Technical Specification

### Class Information
- **Namespace**: `OpenSim.Region.CoreModules.Avatar.Inventory.Transfer`
- **Type**: `ISharedRegionModule` implementation
- **Assembly**: `OpenSim.Region.CoreModules`

### Key Features

1. **Inventory Offering**: Allows users to offer items and folders to other users
2. **Cross-Region Support**: Works across multiple regions through message transfer
3. **Accept/Decline Handling**: Manages user responses to inventory offers
4. **Task Inventory Support**: Handles inventory transfers from objects/tasks
5. **Automatic Trash Management**: Moves declined items to trash folder

### Configuration

The module is controlled by configuration in two sections:

#### Primary Configuration ([Modules] section)
```ini
[Modules]
InventoryTransferModule = true  ; Enable inventory transfers (default: true)
```

#### Legacy Configuration ([Messaging] section)
```ini
[Messaging]
InventoryTransferModule = InventoryTransferModule  ; Legacy configuration method
```

### Architecture

#### Dependencies
- **IMessageTransferModule**: For cross-region message delivery
- **IInventoryService**: For inventory operations
- **Scene**: For local user presence and inventory management
- **ThreadedClasses.RwLockedList**: For thread-safe scene list management

#### Key Components

1. **Scene Management**:
   - Maintains list of scenes for cross-region user lookup
   - Handles scene registration and unregistration

2. **Message Transfer Integration**:
   - Uses IMessageTransferModule for remote instant messages
   - Falls back to local delivery when message transfer unavailable

3. **Instant Message Processing**:
   - Handles multiple instant message dialog types
   - Processes both incoming and outgoing inventory offers

### Supported Instant Message Types

#### Outgoing (Client to Grid)
1. **InventoryOffered**: User offering item/folder to another user
2. **InventoryAccepted**: User accepting offered inventory
3. **InventoryDeclined**: User declining offered inventory
4. **TaskInventoryAccepted**: User accepting inventory from objects
5. **TaskInventoryDeclined**: User declining inventory from objects

#### Incoming (Grid to Client)
1. **InventoryOffered**: Remote inventory offer delivery
2. **TaskInventoryOffered**: Object inventory offer delivery
3. **InventoryAccepted/Declined**: Delivery of accept/decline responses

### Lifecycle

#### Initialization Phase
1. **Initialise()**: Reads configuration and determines enablement
2. **AddRegion()**: Registers event handlers and adds scene to list
3. **RegionLoaded()**: Attempts to get message transfer module reference

#### Runtime Phase
- Processes instant messages related to inventory transfers
- Manages inventory operations (move, copy, delete)
- Handles cross-region message delivery

#### Shutdown Phase
- **RemoveRegion()**: Unregisters event handlers and removes scene
- **Close()**: Cleanup operations

### Inventory Transfer Process

#### Offering Process
1. **Validation**: Checks item/folder existence and permissions
2. **Inventory Copy**: Creates copy in recipient's inventory using Scene.GiveInventory*
3. **Message Preparation**: Encodes transfer data in instant message binary bucket
4. **Delivery**: Sends instant message locally or via message transfer module

#### Acceptance Process
1. **Item Location**: Moves accepted item to specified folder
2. **Notification**: Sends acceptance confirmation to original sender
3. **Inventory Update**: Updates recipient's inventory display

#### Decline Process
1. **Trash Movement**: Moves declined item to user's trash folder
2. **Notification**: Sends decline notification to original sender (for user offers)
3. **Cleanup**: Removes temporary inventory references

### Binary Bucket Format

#### For Items/Folders
- **Byte 0**: AssetType enum value
- **Bytes 1-16**: UUID of the inventory item/folder
- **Additional UUIDs**: For folders with multiple items (17 bytes per additional item)

#### For Task Inventory
- **Byte 0**: AssetType enum value (single byte for task inventory)

### Error Handling

#### Client-Side Errors
- Invalid item/folder UUIDs
- Missing inventory items
- Permission violations
- Trash folder not found

#### Network Errors
- Message transfer module unavailable (local-only operation)
- Remote delivery failures (user offline notifications)
- Cross-region communication issues

### Performance Considerations

- **Thread Safety**: Uses RwLockedList for scene management
- **Efficient Lookup**: Caches message transfer module reference
- **Lazy Evaluation**: Only processes relevant instant message types
- **Memory Management**: Reuses binary bucket arrays where possible

### Security Features

#### Asset Type Validation
- Prevents transfer of link items/folders (security risk)
- Validates asset types in binary buckets

#### Permission Checking
- Relies on Scene.GiveInventory* methods for permission validation
- Respects inventory folder permissions

#### Ownership Verification
- Validates sender ownership before transfers
- Prevents unauthorized inventory manipulation

### Integration Points

#### Scene Integration
- Uses Scene.GiveInventoryItem and Scene.GiveInventoryFolder
- Integrates with scene event system
- Leverages scene's inventory and asset services

#### Message Transfer Integration
- Seamlessly works with or without message transfer module
- Provides graceful degradation for local-only operation
- Handles cross-region instant message delivery

#### Client Protocol
- Implements Second Life protocol instant message handling
- Supports standard viewer inventory transfer UI
- Maintains protocol compatibility

### Logging

The module provides detailed logging at different levels:
- **Debug**: Detailed transfer operations and message processing
- **Info**: Transfer completion status
- **Error**: Transfer failures and configuration issues

### Migration Notes

This module has been migrated from Mono.Addins to the OptionalModulesFactory system:
- Removed `[Extension]` attribute and Mono.Addins dependency
- Added factory registration in `OptionalModulesFactory`
- Maintained full backward compatibility
- Enhanced logging for better diagnostics
- Preserved both [Modules] and [Messaging] configuration support

### Usage Examples

#### Basic Configuration
```ini
[Modules]
InventoryTransferModule = true
```

#### Legacy Configuration (still supported)
```ini
[Messaging]
InventoryTransferModule = InventoryTransferModule
```

#### Disable Inventory Transfers
```ini
[Modules]
InventoryTransferModule = false
```

### Common Issues

#### "User not online. Inventory has been saved"
- Occurs when recipient is offline during transfer
- Inventory is still transferred and will be available when user logs in

#### "No Message transfer module found, transfers will be local only"
- Message transfer module not configured
- Transfers will only work within the same region

#### Permission Errors
- Check folder/item permissions in source inventory
- Ensure proper ownership before transfer

### Related Modules

- **MessageTransferModule**: For cross-region message delivery
- **InventoryService**: Backend inventory storage
- **InventoryArchiverModule**: For bulk inventory operations
- **HGInventoryService**: For hypergrid inventory transfers

### See Also

- [InventoryArchiverModule.md](InventoryArchiverModule.md)
- [LibraryModule.md](LibraryModule.md)
- OpenSim Instant Message Documentation
- Second Life Protocol Specification