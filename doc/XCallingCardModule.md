# XCallingCardModule Technical Documentation

## Overview

The XCallingCardModule (CallingCardModule) is a shared region module that provides calling card functionality for OpenSimulator. It enables avatars to offer, accept, and decline calling cards, which are inventory items that represent personal connections between users. The module handles calling card creation, inventory management, and integration with the friendship system.

## Module Classification

- **Type**: ISharedRegionModule, ICallingCardModule
- **Namespace**: OpenSim.Region.CoreModules.Avatar.Friends
- **Assembly**: OpenSim.Region.CoreModules
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs with configuration-based loading

## Core Functionality

### Primary Purpose

The XCallingCardModule manages calling card interactions between avatars in OpenSimulator. Calling cards are special inventory items that represent personal connections and can be offered between users. The module handles the complete lifecycle of calling cards, from initial offers through acceptance or decline, and provides special handling for god mode users.

### Key Features

1. **Calling Card Offers**: Send calling card offers between avatars
2. **Acceptance/Decline Handling**: Process user responses to calling card offers
3. **Inventory Integration**: Create and manage calling cards as inventory items
4. **God Mode Support**: Special handling for administrator-level users
5. **Cross-Region Support**: Shared module design for multi-region grids
6. **Message Transfer Integration**: Send calling card offers to offline users
7. **Permission Management**: Configurable permissions for calling card items
8. **Friendship Integration**: Integration with OpenSim's friendship system

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for shared modules
1. Initialise(IConfigSource) - Configuration loading and enablement check
2. AddRegion(Scene) - Register module interface and scene association
3. RegionLoaded(Scene) - Subscribe to client events
4. PostInitialise() - Post-initialization setup (no-op)
5. RemoveRegion(Scene) - Scene cleanup and event unsubscription
6. Close() - Module cleanup (no-op)
```

### Interface Implementation

The module implements two key interfaces:

#### ISharedRegionModule
Provides shared functionality across multiple regions in a grid.

#### ICallingCardModule
Defines the calling card service contract:

```csharp
public interface ICallingCardModule
{
    UUID CreateCallingCard(UUID userID, UUID creatorID, UUID folderID);
}
```

### Configuration Architecture

```csharp
public void Initialise(IConfigSource source)
{
    IConfig ccConfig = source.Configs["XCallingCard"];
    if (ccConfig != null)
        m_Enabled = ccConfig.GetBoolean("Enabled", true);
}
```

## Configuration System

### Module Configuration

#### Optional Configuration ([XCallingCard] section)
- **Enabled**: `boolean` - Enable/disable the module (default: true)

### Configuration Examples

#### Basic Configuration
```ini
[XCallingCard]
Enabled = true

# Module loads automatically when enabled
```

#### Disabled Module
```ini
[XCallingCard]
Enabled = false

# Module will not load or process calling card operations
```

#### Default Behavior (No Configuration)
```ini
# No [XCallingCard] section
# Module defaults to enabled and loads automatically
```

## Calling Card Operations

### Offering Calling Cards

```csharp
private void OnOfferCallingCard(IClientAPI client, UUID destID, UUID transactionID)
{
    ScenePresence sp = GetClientPresence(client.AgentId);
    if (sp != null)
    {
        // God mode special handling - automatic card creation
        if (sp.IsViewerUIGod)
        {
            CreateCallingCard(client.AgentId, destID, UUID.Zero, true);
            return;
        }
    }

    // Check if recipient is online in current scene
    IClientAPI dest = FindClientObject(destID);
    if (dest != null)
    {
        DoCallingCardOffer(dest, client.AgentId);
        return;
    }

    // Send offer to offline user via message transfer
    IMessageTransferModule transferModule = m_Scenes[0].RequestModuleInterface<IMessageTransferModule>();
    if (transferModule != null)
    {
        transferModule.SendInstantMessage(new GridInstantMessage(
            client.Scene, client.AgentId,
            client.FirstName + " " + client.LastName,
            destID, (byte)211, false,
            String.Empty,
            transactionID, false, new Vector3(), Array.Empty<byte>(), true),
            delegate(bool success) {} );
    }
}
```

### Calling Card Creation Process

```csharp
private UUID CreateCallingCard(UUID userID, UUID creatorID, UUID folderID, bool isGod)
{
    // Validate user account service
    IUserAccountService userv = m_Scenes[0].UserAccountService;
    if (userv == null) return UUID.Zero;

    // Get creator account information
    UserAccount info = userv.GetUserAccount(UUID.Zero, creatorID);
    if (info == null) return UUID.Zero;

    // Validate inventory service
    IInventoryService inv = m_Scenes[0].InventoryService;
    if (inv == null) return UUID.Zero;

    // Determine target folder
    if (folderID.IsZero())
    {
        InventoryFolderBase folder = inv.GetFolderForType(userID, FolderType.CallingCard);
        if (folder == null) return UUID.Zero;
        folderID = folder.ID;
    }

    // Create inventory item
    InventoryItemBase item = new InventoryItemBase();
    item.AssetID = UUID.Zero;
    item.AssetType = (int)AssetType.CallingCard;

    // Set permissions based on god mode
    item.BasePermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify);
    if (isGod)
        item.BasePermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify | PermissionMask.Transfer | PermissionMask.Move);

    item.EveryOnePermissions = (uint)PermissionMask.None;
    item.CurrentPermissions = item.BasePermissions;
    item.NextPermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify);

    // Set item metadata
    item.ID = UUID.Random();
    item.CreatorId = creatorID.ToString();
    item.Owner = userID;
    item.GroupID = UUID.Zero;
    item.GroupOwned = false;
    item.Folder = folderID;
    item.CreationDate = Util.UnixTimeSinceEpoch();
    item.InvType = (int)InventoryType.CallingCard;
    item.Flags = 0;
    item.Name = info.Name;
    item.Description = "";
    item.SalePrice = 10;
    item.SaleType = (byte)SaleType.Not;

    // Add to inventory and notify client
    inv.AddItem(item);

    IClientAPI client = FindClientObject(userID);
    if (client != null)
        client.SendBulkUpdateInventory(item);

    return item.ID;
}
```

### Offer Processing Workflow

```csharp
private void DoCallingCardOffer(IClientAPI dest, UUID from)
{
    // Create calling card in recipient's inventory
    UUID itemID = CreateCallingCard(dest.AgentId, from, UUID.Zero, false);

    // Send offer notification to recipient
    dest.SendOfferCallingCard(from, itemID);
}
```

## Event Management and Client Integration

### Scene Integration

```csharp
public void AddRegion(Scene scene)
{
    if (!m_Enabled) return;

    m_Scenes.Add(scene);
    scene.RegisterModuleInterface<ICallingCardModule>(this);
}

public void RegionLoaded(Scene scene)
{
    if (!m_Enabled) return;
    scene.EventManager.OnNewClient += OnNewClient;
}

public void RemoveRegion(Scene scene)
{
    if (!m_Enabled) return;

    m_Scenes.Remove(scene);
    scene.EventManager.OnNewClient -= OnNewClient;
    scene.EventManager.OnIncomingInstantMessage += OnIncomingInstantMessage;
    scene.UnregisterModuleInterface<ICallingCardModule>(this);
}
```

### Client Event Handling

```csharp
private void OnNewClient(IClientAPI client)
{
    client.OnOfferCallingCard += OnOfferCallingCard;
    client.OnAcceptCallingCard += OnAcceptCallingCard;
    client.OnDeclineCallingCard += OnDeclineCallingCard;
}
```

### Response Handling

```csharp
private void OnAcceptCallingCard(IClientAPI client, UUID transactionID, UUID folderID)
{
    // Acceptance handling is managed by the client
    // Card has already been created in recipient's inventory
}

private void OnDeclineCallingCard(IClientAPI client, UUID transactionID)
{
    IInventoryService invService = m_Scenes[0].InventoryService;

    // Move declined calling card to trash
    InventoryFolderBase trashFolder = invService.GetFolderForType(client.AgentId, FolderType.Trash);
    InventoryItemBase item = invService.GetItem(client.AgentId, transactionID);

    if (item != null && trashFolder != null)
    {
        item.Folder = trashFolder.ID;
        List<UUID> uuids = new List<UUID>();
        uuids.Add(item.ID);
        invService.DeleteItems(item.Owner, uuids);
        m_Scenes[0].AddInventoryItem(client, item);
    }
}
```

## Instant Message Integration

### Offline User Handling

```csharp
private void OnIncomingInstantMessage(GridInstantMessage msg)
{
    // Handle calling card offers sent via instant message (dialog type 211)
    if (msg.dialog == (uint)211)
    {
        IClientAPI client = FindClientObject(new UUID(msg.toAgentID));
        if (client == null) return;

        DoCallingCardOffer(client, new UUID(msg.fromAgentID));
    }
}
```

### Message Transfer Integration

The module integrates with IMessageTransferModule to send calling card offers to offline users:

```csharp
IMessageTransferModule transferModule = m_Scenes[0].RequestModuleInterface<IMessageTransferModule>();

if (transferModule != null)
{
    transferModule.SendInstantMessage(new GridInstantMessage(
        client.Scene, client.AgentId,
        client.FirstName + " " + client.LastName,
        destID, (byte)211, false,        // Dialog type 211 for calling card offers
        String.Empty,
        transactionID, false, new Vector3(), Array.Empty<byte>(), true),
        delegate(bool success) {} );
}
```

## User Discovery and Presence Management

### Client Discovery

```csharp
public IClientAPI FindClientObject(UUID agentID)
{
    Scene scene = GetClientScene(agentID);
    if (scene == null) return null;

    ScenePresence presence = scene.GetScenePresence(agentID);
    if (presence == null) return null;

    return presence.ControllingClient;
}
```

### Scene Discovery

```csharp
private Scene GetClientScene(UUID agentId)
{
    foreach (Scene scene in m_Scenes)
    {
        ScenePresence presence = scene.GetScenePresence(agentId);
        if (presence != null)
        {
            if (!presence.IsChildAgent)
                return scene;
        }
    }
    return null;
}
```

### Presence Retrieval

```csharp
private ScenePresence GetClientPresence(UUID agentId)
{
    foreach (Scene scene in m_Scenes)
    {
        ScenePresence presence = scene.GetScenePresence(agentId);
        if (presence != null)
        {
            if (!presence.IsChildAgent)
                return presence;
        }
    }
    return null;
}
```

## Permission System and God Mode

### Standard User Permissions

For regular users, calling cards are created with basic permissions:

```csharp
item.BasePermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify);
item.EveryOnePermissions = (uint)PermissionMask.None;
item.CurrentPermissions = item.BasePermissions;
item.NextPermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify);
```

### God Mode Enhanced Permissions

For god mode users, calling cards have enhanced permissions:

```csharp
if (isGod)
    item.BasePermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify | PermissionMask.Transfer | PermissionMask.Move);
```

### God Mode Behavior

God mode users can bypass the normal offer/accept process:

```csharp
if (sp.IsViewerUIGod)
{
    // Automatic calling card creation without requiring acceptance
    CreateCallingCard(client.AgentId, destID, UUID.Zero, true);
    return;
}
```

## Inventory Integration

### Calling Card Properties

```csharp
InventoryItemBase item = new InventoryItemBase();
item.AssetID = UUID.Zero;                           // No associated asset
item.AssetType = (int)AssetType.CallingCard;        // Asset type 1
item.InvType = (int)InventoryType.CallingCard;      // Inventory type 1
item.Name = info.Name;                              // Creator's display name
item.Description = "";                               // Empty description
item.SalePrice = 10;                                // Default sale price
item.SaleType = (byte)SaleType.Not;                 // Not for sale
```

### Folder Management

```csharp
if (folderID.IsZero())
{
    InventoryFolderBase folder = inv.GetFolderForType(userID, FolderType.CallingCard);
    if (folder == null) return UUID.Zero;  // No calling card folder available
    folderID = folder.ID;
}
```

### Inventory Updates

```csharp
inv.AddItem(item);

IClientAPI client = FindClientObject(userID);
if (client != null)
    client.SendBulkUpdateInventory(item);  // Notify client of new item
```

## Service Dependencies and Integration

### Required Services

- **IUserAccountService**: User account information retrieval
- **IInventoryService**: Inventory management and folder operations
- **IMessageTransferModule**: Offline message delivery

### Service Validation

```csharp
IUserAccountService userv = m_Scenes[0].UserAccountService;
if (userv == null) return UUID.Zero;

IInventoryService inv = m_Scenes[0].InventoryService;
if (inv == null) return UUID.Zero;
```

### Cross-Service Integration

The module integrates with multiple OpenSim services:

1. **User Account Service**: Validates users and retrieves display names
2. **Inventory Service**: Manages calling card inventory items and folders
3. **Message Transfer Service**: Delivers calling card offers to offline users
4. **Scene Management**: Tracks user presence across multiple regions

## Error Handling and Edge Cases

### Service Availability Checks

```csharp
if (userv == null) return UUID.Zero;
if (info == null) return UUID.Zero;
if (inv == null) return UUID.Zero;
if (folder == null) return UUID.Zero;  // No calling card folder
```

### Null Reference Protection

```csharp
ScenePresence sp = GetClientPresence(client.AgentId);
if (sp != null)
{
    // Safe to access scene presence properties
}

IClientAPI dest = FindClientObject(destID);
if (dest != null)
{
    // Safe to send offers to online users
}
```

### Transaction Safety

```csharp
if (item != null && trashFolder != null)
{
    // Safe to move item to trash
    item.Folder = trashFolder.ID;
    invService.DeleteItems(item.Owner, uuids);
    m_Scenes[0].AddInventoryItem(client, item);
}
```

## Performance Considerations

### Efficient User Discovery

The module uses optimized user discovery across multiple scenes:

```csharp
foreach (Scene scene in m_Scenes)
{
    ScenePresence presence = scene.GetScenePresence(agentId);
    if (presence != null && !presence.IsChildAgent)
        return scene;  // Return first valid presence found
}
```

### Memory Management

- Uses thread-safe RwLockedList for scene management
- Minimal object allocation during calling card operations
- Efficient inventory item creation with pre-allocated UUIDs

### Network Optimization

- Bulk inventory updates reduce client communication overhead
- Instant message integration minimizes custom protocol extensions
- Direct client API usage for immediate delivery to online users

## Security Considerations

### Permission Validation

- Calling cards respect OpenSim's permission system
- God mode permissions are appropriately elevated but controlled
- Inventory folder validation prevents unauthorized item placement

### Access Control

- Users can only create calling cards with valid user accounts
- Recipients must exist in the user account service
- Inventory operations require valid inventory service access

### Transaction Integrity

- Calling card creation is atomic within inventory service transactions
- Failed operations return appropriate error codes (UUID.Zero)
- No partial calling card states are possible

## Integration Points

### Friendship System Integration

The module is designed to integrate with OpenSim's friendship system:

```csharp
// Called from friends module when friendship is confirmed
public UUID CreateCallingCard(UUID userID, UUID creatorID, UUID folderID)
{
    return CreateCallingCard(userID, creatorID, folderID, false);
}
```

### Client Protocol Integration

- Uses standard OpenSim client events for calling card operations
- Integrates with viewer calling card UI elements
- Supports both accept and decline user responses

### Scene Management Integration

- Shared module design supports multi-region grids
- Scene-specific event handling with proper cleanup
- Module interface registration for cross-module access

## Use Cases and Applications

### Social Networking

- **Personal Connections**: Users can exchange calling cards to maintain contact lists
- **Business Cards**: Professional networking within virtual environments
- **Community Building**: Facilitate connections between community members

### Administrative Functions

- **God Mode Cards**: Administrators can create calling cards without user consent
- **Enhanced Permissions**: Administrative calling cards have transfer and move permissions
- **Forced Connections**: Bypass normal acceptance workflow for administrative purposes

### Grid Management

- **Cross-Region Consistency**: Calling cards work across multiple regions
- **Offline User Support**: Calling card offers delivered to offline users
- **Service Integration**: Works with grid-wide user and inventory services

## Dependencies

### Core Framework Dependencies

- `OpenSim.Framework` - Core data structures and utilities
- `OpenSim.Region.Framework.Interfaces` - Module interface contracts
- `OpenSim.Region.Framework.Scenes` - Scene management and events
- `OpenSim.Services.Interfaces` - Service interface definitions

### System Dependencies

- `System.Collections.Generic` - Collection management for scene lists
- `log4net` - Logging framework for debug and error messages
- `Nini.Config` - Configuration management
- `ThreadedClasses` - Thread-safe collection implementations

### Service Dependencies

- User Account Service for user validation and name resolution
- Inventory Service for calling card creation and management
- Message Transfer Module for offline user communication

## Troubleshooting

### Common Configuration Issues

1. **Module Not Loading**
   - Check that `[XCallingCard]` section has `Enabled = true`
   - Verify no conflicting calling card modules are loaded
   - Review startup logs for initialization errors

2. **Calling Cards Not Created**
   - Ensure User Account Service is available and functional
   - Verify Inventory Service is properly configured
   - Check that calling card folders exist in user inventories

3. **Offers Not Delivered**
   - Verify Message Transfer Module is loaded for offline users
   - Check instant message system configuration
   - Ensure proper dialog type handling (type 211)

### Common Runtime Issues

1. **Cards Not Appearing in Inventory**
   - Check inventory folder permissions and structure
   - Verify client is receiving bulk inventory updates
   - Ensure calling card folder exists for target user

2. **God Mode Not Working**
   - Verify user has proper god mode permissions
   - Check IsViewerUIGod property in scene presence
   - Ensure god mode calling cards have proper permissions

3. **Cross-Region Issues**
   - Verify shared module is properly loaded across all regions
   - Check scene event subscription and cleanup
   - Ensure proper user discovery across multiple scenes

### Debug Configuration

```ini
[XCallingCard]
Enabled = true

# Enable debug logging
[Logging]
LogLevel = DEBUG

# Check specific module logging
[Log4Net]
logger.OpenSim.Region.CoreModules.Avatar.Friends.CallingCardModule = DEBUG
```

### Log Analysis

Monitor module operation through log messages:
```
[CallingCardModule]: Creating calling card for TestUser in inventory of 12345678-1234-1234-1234-123456789012
[CallingCardModule]: Offering calling card from TestUser to TargetUser
[CallingCardModule]: Processing calling card decline for transaction 12345678-1234-1234-1234-123456789012
```

### Service Validation

Test service dependencies:
```csharp
// Check user account service
IUserAccountService userService = scene.UserAccountService;
UserAccount account = userService?.GetUserAccount(UUID.Zero, userID);

// Check inventory service
IInventoryService invService = scene.InventoryService;
InventoryFolderBase folder = invService?.GetFolderForType(userID, FolderType.CallingCard);
```

## Deployment Considerations

### Grid Architecture Planning

- **Single vs Multi-Region**: Module works in both standalone and grid configurations
- **Service Dependencies**: Ensure user account and inventory services are available
- **Cross-Region Communication**: Plan for message transfer capabilities

### Performance Planning

- **User Volume**: Module scales with user count and calling card usage
- **Inventory Impact**: Consider inventory storage requirements for calling cards
- **Network Traffic**: Factor in instant message traffic for offline offers

### Security Planning

- **Permission Management**: Configure appropriate calling card permissions
- **God Mode Access**: Restrict god mode permissions to trusted administrators
- **Inventory Protection**: Ensure proper inventory folder security

## Future Enhancement Opportunities

### Advanced Features

- **Custom Calling Card Designs**: Support for custom artwork and descriptions
- **Business Card Templates**: Pre-defined templates for professional cards
- **Card Categories**: Organization of calling cards by categories or groups
- **Expiration Dates**: Time-limited calling cards for temporary connections

### Performance Improvements

- **Bulk Operations**: Support for bulk calling card offers and management
- **Caching**: Cache user account information for improved performance
- **Asynchronous Processing**: Non-blocking calling card creation and delivery
- **Database Optimization**: Optimized queries for large-scale deployments

### Integration Enhancements

- **Web Interface**: Web-based calling card management tools
- **External Services**: Integration with external contact management systems
- **Social Features**: Enhanced social networking capabilities
- **Analytics**: Usage tracking and social connection analytics

## Conclusion

The XCallingCardModule provides essential social networking functionality for OpenSimulator grids through calling card management. Its integration with OpenSim's inventory system, support for both online and offline users, and special handling for administrative functions make it a valuable component for community-building in virtual environments. The module's configuration-based loading, comprehensive error handling, and cross-region support ensure reliable operation in both standalone and grid deployments.