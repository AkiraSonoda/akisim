# CallingCardModule (XCallingCard) Technical Documentation

## Overview

The CallingCardModule (also known as XCallingCard) is a shared region module that provides calling card functionality for OpenSimulator. It enables users to create, offer, accept, and manage calling cards as a form of friendship and contact management system within the virtual world environment.

## Module Classification

- **Type**: ISharedRegionModule, ICallingCardModule
- **Namespace**: OpenSim.Region.CoreModules.Avatar.Friends
- **Assembly**: OpenSim.Region.CoreModules
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs with configuration-based loading
- **Module ID**: XCallingCard

## Core Functionality

### Primary Purpose

The CallingCardModule provides a comprehensive calling card system that allows users to share contact information and establish social connections within OpenSimulator. It handles the complete lifecycle of calling cards from offering to acceptance/decline, including inventory management and cross-region messaging.

### Key Features

1. **Calling Card Creation**: Dynamic creation of calling card inventory items
2. **Offer System**: Interactive calling card offering between users
3. **Accept/Decline Workflow**: Complete response handling for calling card offers
4. **God Mode Support**: Administrative calling card creation capabilities
5. **Cross-Region Messaging**: Grid-wide instant message support for calling card offers
6. **Inventory Integration**: Seamless integration with user inventory system
7. **Permission Management**: Flexible permission settings for calling cards
8. **Friendship Integration**: Connection with friends system for relationship management

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for shared modules
1. Initialise(IConfigSource) - Configuration loading and feature enablement
2. PostInitialise() - Post-initialization setup (no-op)
3. AddRegion(Scene) - Register module interface and event handlers
4. RegionLoaded(Scene) - Client event registration and scene setup
5. RemoveRegion(Scene) - Event cleanup and module interface removal
6. Close() - Module cleanup (no-op)
```

### Interface Implementation

The module implements two key interfaces:

#### ISharedRegionModule
Provides shared functionality across multiple regions within the same OpenSim instance.

#### ICallingCardModule
Defines the calling card management contract:

```csharp
public interface ICallingCardModule
{
    UUID CreateCallingCard(UUID userID, UUID creatorID, UUID folderID);
}
```

### Event-Driven Architecture

The module uses comprehensive event handling for user interactions:

```csharp
// Client event registration
private void OnNewClient(IClientAPI client)
{
    client.OnOfferCallingCard += OnOfferCallingCard;
    client.OnAcceptCallingCard += OnAcceptCallingCard;
    client.OnDeclineCallingCard += OnDeclineCallingCard;
}

// Scene event registration
scene.EventManager.OnNewClient += OnNewClient;
scene.EventManager.OnIncomingInstantMessage += OnIncomingInstantMessage;
```

## Configuration System

### Module Configuration

#### Basic Configuration ([XCallingCard] section)
- **Enabled**: `boolean` - Enable/disable the calling card module (default: true)

#### Module Loading
- **Default Behavior**: Module loads by default with `Enabled = true`
- **Explicit Disabling**: Set `Enabled = false` to disable calling card functionality

### Configuration Examples

#### Default Configuration
```ini
[XCallingCard]
Enabled = true
```

#### Disabled Configuration
```ini
[XCallingCard]
Enabled = false
```

### Minimal Configuration
```ini
# Module loads with default settings if no configuration section is present
```

## Calling Card Operations

### Calling Card Offering

#### OnOfferCallingCard Method
```csharp
private void OnOfferCallingCard(IClientAPI client, UUID destID, UUID transactionID)
{
    ScenePresence sp = GetClientPresence(client.AgentId);
    if (sp != null)
    {
        // God mode handling - instant creation
        if (sp.IsViewerUIGod)
        {
            CreateCallingCard(client.AgentId, destID, UUID.Zero, true);
            return;
        }
    }

    // Standard user workflow
    IClientAPI dest = FindClientObject(destID);
    if (dest != null)
    {
        DoCallingCardOffer(dest, client.AgentId);
        return;
    }

    // Cross-region instant message for offline users
    SendInstantMessageOffer(client, destID, transactionID);
}
```

#### Offer Workflow

1. **God Mode Check**: Administrators can instantly create calling cards
2. **Direct Offer**: If target user is online in the same region
3. **Cross-Region Messaging**: Grid-wide instant message for offline/remote users

### Calling Card Creation

#### CreateCallingCard Method
```csharp
public UUID CreateCallingCard(UUID userID, UUID creatorID, UUID folderID)
{
    return CreateCallingCard(userID, creatorID, folderID, false);
}

private UUID CreateCallingCard(UUID userID, UUID creatorID, UUID folderID, bool isGod)
{
    // Service validation
    IUserAccountService userv = m_Scenes[0].UserAccountService;
    if (userv == null) return UUID.Zero;

    UserAccount info = userv.GetUserAccount(UUID.Zero, creatorID);
    if (info == null) return UUID.Zero;

    IInventoryService inv = m_Scenes[0].InventoryService;
    if (inv == null) return UUID.Zero;

    // Folder resolution
    if (folderID.IsZero())
    {
        InventoryFolderBase folder = inv.GetFolderForType(userID, FolderType.CallingCard);
        if (folder == null) return UUID.Zero;
        folderID = folder.ID;
    }

    // Inventory item creation
    InventoryItemBase item = CreateInventoryItem(userID, creatorID, folderID, info.Name, isGod);
    inv.AddItem(item);

    // Client notification
    IClientAPI client = FindClientObject(userID);
    if (client != null)
        client.SendBulkUpdateInventory(item);

    return item.ID;
}
```

#### Creation Workflow

1. **Service Validation**: Verify user account and inventory services
2. **User Information Lookup**: Retrieve creator's account information
3. **Folder Resolution**: Find or use calling card folder in inventory
4. **Item Creation**: Generate calling card inventory item with appropriate permissions
5. **Inventory Addition**: Add item to user's inventory
6. **Client Notification**: Update client inventory display

### Inventory Item Properties

#### Standard Calling Card Properties
```csharp
InventoryItemBase item = new InventoryItemBase();
item.AssetID = UUID.Zero;                                    // No associated asset
item.AssetType = (int)AssetType.CallingCard;                // Calling card asset type
item.BasePermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify);
item.EveryOnePermissions = (uint)PermissionMask.None;       // Private by default
item.CurrentPermissions = item.BasePermissions;
item.NextPermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify);
item.InvType = (int)InventoryType.CallingCard;              // Calling card inventory type
item.Name = info.Name;                                       // Creator's display name
item.Description = "";                                       // Empty description
item.SalePrice = 10;                                        // Default sale price
item.SaleType = (byte)SaleType.Not;                        // Not for sale
```

#### God Mode Calling Card Properties
```csharp
if (isGod)
    item.BasePermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify |
                                 PermissionMask.Transfer | PermissionMask.Move);
```

**Enhanced Permissions**: God mode calling cards have full permissions including transfer and move.

## Response Handling

### Calling Card Acceptance

#### OnAcceptCallingCard Method
```csharp
private void OnAcceptCallingCard(IClientAPI client, UUID transactionID, UUID folderID)
{
    // Currently no-op implementation
    // Calling card is already created during offer phase
}
```

**Note**: The current implementation creates the calling card during the offer phase, so acceptance doesn't require additional processing.

### Calling Card Decline

#### OnDeclineCallingCard Method
```csharp
private void OnDeclineCallingCard(IClientAPI client, UUID transactionID)
{
    IInventoryService invService = m_Scenes[0].InventoryService;

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

#### Decline Workflow

1. **Inventory Service Access**: Get inventory service interface
2. **Trash Folder Lookup**: Find user's trash folder
3. **Item Retrieval**: Get the calling card item by transaction ID
4. **Item Deletion**: Move item to trash and mark for deletion
5. **Inventory Update**: Refresh client inventory display

## Cross-Region Messaging

### Grid-Wide Instant Messages

#### Instant Message Integration
```csharp
private void OnIncomingInstantMessage(GridInstantMessage msg)
{
    if (msg.dialog == (uint)211)  // Calling card offer message type
    {
        IClientAPI client = FindClientObject(new UUID(msg.toAgentID));
        if (client == null)
            return;

        DoCallingCardOffer(client, new UUID(msg.fromAgentID));
    }
}
```

#### Message Transfer Module Integration
```csharp
IMessageTransferModule transferModule = m_Scenes[0].RequestModuleInterface<IMessageTransferModule>();

if (transferModule != null)
{
    transferModule.SendInstantMessage(new GridInstantMessage(
        client.Scene, client.AgentId,
        client.FirstName+" "+client.LastName,
        destID, (byte)211, false,  // Dialog type 211 for calling card offers
        String.Empty,
        transactionID, false, new Vector3(), Array.Empty<byte>(), true),
        delegate(bool success) {} );
}
```

### Message Types

- **Dialog Type 211**: Calling card offer messages for cross-region communication
- **Grid Instant Message**: Standard grid-wide messaging infrastructure
- **Message Transfer Module**: Handles delivery across region boundaries

## User Discovery and Scene Management

### Client Discovery

#### FindClientObject Method
```csharp
public IClientAPI FindClientObject(UUID agentID)
{
    Scene scene = GetClientScene(agentID);
    if (scene == null)
        return null;

    ScenePresence presence = scene.GetScenePresence(agentID);
    if (presence == null)
        return null;

    return presence.ControllingClient;
}
```

#### Scene Resolution
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

### Presence Management

#### GetClientPresence Method
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

**Key Features**:
- **Multi-Scene Search**: Searches across all registered scenes
- **Child Agent Filtering**: Only returns root agents, not child agents
- **Presence Validation**: Ensures user is actually present in scene

## God Mode Functionality

### Administrative Features

#### God Mode Detection
```csharp
ScenePresence sp = GetClientPresence(client.AgentId);
if (sp != null)
{
    if (sp.IsViewerUIGod)
    {
        CreateCallingCard(client.AgentId, destID, UUID.Zero, true);
        return;
    }
}
```

#### Enhanced Permissions
God mode calling cards receive enhanced permissions:
- **Copy**: Standard copying permissions
- **Modify**: Standard modification permissions
- **Transfer**: Ability to transfer to other users
- **Move**: Ability to move between inventory folders

#### Administrative Use Cases
- **User Support**: Help desk staff can create calling cards for users
- **Relationship Management**: Administrative tools for managing user connections
- **Social Network Administration**: Grid-wide social connection management

## Performance Considerations

### Thread Safety

#### Scene Collection Management
```csharp
protected RwLockedList<Scene> m_Scenes = new RwLockedList<Scene>();
```

**Features**:
- **Read-Write Locking**: Efficient concurrent access to scene collection
- **Thread Safety**: Safe multi-threaded access to scene list
- **Performance Optimization**: Optimized for frequent read operations

### Resource Management

#### Service Caching
- **First Scene Strategy**: Uses `m_Scenes[0]` for service access
- **Service Interface Caching**: Efficient service lookup and reuse
- **Minimal Overhead**: Lightweight operations for frequent calling card activities

### Network Optimization

#### Instant Message Efficiency
- **Direct Client Communication**: Local users communicate directly
- **Grid Messages**: Only uses grid messaging for remote/offline users
- **Minimal Bandwidth**: Compact message format for cross-region communication

## Integration Points

### Inventory System Integration
- **Folder Type Resolution**: Automatic calling card folder detection
- **Item Creation**: Standard inventory item creation workflow
- **Bulk Updates**: Efficient inventory update notifications
- **Trash Management**: Proper handling of declined calling cards

### Friends System Integration
- **ICallingCardModule Interface**: Provides calling card creation for friends system
- **Relationship Management**: Supports friendship establishment workflows
- **Social Connections**: Facilitates social network building

### Scene Management Integration
- **Multi-Scene Support**: Operates across multiple regions simultaneously
- **Event System**: Integrates with scene event management
- **Presence Tracking**: Efficient user presence monitoring

### Client Communication Integration
- **IClientAPI Events**: Comprehensive client interaction handling
- **UI Integration**: Supports viewer calling card interfaces
- **Notification System**: Real-time client notifications

## Security Considerations

### Permission Management
- **Default Privacy**: Calling cards are private by default (EveryOnePermissions = None)
- **Controlled Transfer**: Standard calling cards cannot be transferred
- **God Mode Controls**: Enhanced permissions only for administrative users

### Input Validation
- **UUID Validation**: Proper validation of user and transaction UUIDs
- **Service Availability**: Graceful handling of unavailable services
- **Null Checks**: Comprehensive null checking for robustness

### Access Control
- **Module Enablement**: Can be completely disabled via configuration
- **Scene-Specific Control**: Per-scene registration allows granular control
- **Event Handler Management**: Proper event subscription/unsubscription

## Use Cases and Applications

### Social Networking
- **Contact Exchange**: Easy sharing of contact information between users
- **Friendship Building**: Foundation for establishing friendships
- **Network Growth**: Facilitates social network expansion

### Business Applications
- **Professional Networking**: Business card equivalent for virtual worlds
- **Customer Relations**: Customer service and support contact management
- **Event Management**: Conference and event attendee networking

### Educational Use
- **Classroom Management**: Teacher-student contact facilitation
- **Study Groups**: Student collaboration and contact sharing
- **Academic Networking**: Research collaboration and contact management

### Administrative Functions
- **User Support**: Help desk and customer service contact management
- **Community Building**: Social connection facilitation tools
- **Relationship Mediation**: Administrative tools for user relationship management

## Error Handling and Resilience

### Service Availability
```csharp
IUserAccountService userv = m_Scenes[0].UserAccountService;
if (userv == null)
    return UUID.Zero;

IInventoryService inv = m_Scenes[0].InventoryService;
if (inv == null)
    return UUID.Zero;
```

### User Validation
```csharp
UserAccount info = userv.GetUserAccount(UUID.Zero, creatorID);
if (info == null)
    return UUID.Zero;
```

### Folder Validation
```csharp
InventoryFolderBase folder = inv.GetFolderForType(userID, FolderType.CallingCard);
if (folder == null) // Nowhere to put it
    return UUID.Zero;
```

### Graceful Degradation
- **Service Unavailable**: Returns UUID.Zero for failed operations
- **User Not Found**: Handles missing user accounts gracefully
- **Inventory Issues**: Manages inventory service problems efficiently
- **Network Failures**: Robust handling of cross-region communication failures

## Dependencies

### Core Framework Dependencies
- `OpenSim.Framework` - Core data structures and utilities
- `OpenSim.Region.Framework.Interfaces` - Module interface contracts
- `OpenSim.Region.Framework.Scenes` - Scene and presence management
- `OpenSim.Services.Interfaces` - Service interface definitions

### Service Dependencies
- `IUserAccountService` - User account information lookup
- `IInventoryService` - Inventory management and folder operations
- `IMessageTransferModule` - Cross-region instant messaging

### System Dependencies
- `ThreadedClasses` - Thread-safe collections (RwLockedList)
- `System.Collections.Generic` - Collection types
- `System.Reflection` - Logging infrastructure support

## Troubleshooting

### Common Configuration Issues

1. **Module Not Loading**
   - Verify `[XCallingCard]` section with `Enabled = true`
   - Check that Friends namespace is available in ModuleFactory
   - Review startup logs for module loading messages

2. **Calling Cards Not Created**
   - Verify user account service is available
   - Check inventory service is functioning
   - Ensure calling card folder exists in user inventory

3. **Cross-Region Offers Failing**
   - Verify message transfer module is loaded
   - Check grid messaging configuration
   - Review instant message delivery logs

### Common Runtime Issues

1. **Offers Not Received**
   - Check target user is online and available
   - Verify instant message system is functioning
   - Review client event handler registration

2. **Inventory Not Updated**
   - Check inventory service availability
   - Verify client bulk inventory update calls
   - Review inventory folder permissions

3. **God Mode Not Working**
   - Verify user has god privileges in the scene
   - Check IsViewerUIGod property
   - Review god mode permission configuration

### Debug Configuration

```ini
[XCallingCard]
Enabled = true

# Enable detailed logging if needed
[Logging]
LogLevel = DEBUG
```

### Log Analysis

Monitor module operation through log messages:
```
[CallingCardModule]: Creating calling card for UserName in inventory of 12345678-1234-1234-1234-123456789012
```

Debug logging provides detailed calling card creation information.

## Deployment Considerations

### Multi-Region Environments
- **Shared Module**: Single instance serves multiple regions
- **Cross-Region Messaging**: Requires proper grid messaging configuration
- **Service Coordination**: Shared access to user account and inventory services

### Grid-Wide Deployments
- **Message Transfer**: Requires functional message transfer module
- **Service Backend**: Shared user and inventory services across grid
- **Network Reliability**: Stable network connectivity for cross-region operations

### Performance Scaling
- **Service Load**: Consider user account and inventory service capacity
- **Message Volume**: Plan for instant message traffic during peak usage
- **Memory Usage**: Thread-safe collections provide efficient memory usage

## Future Enhancement Opportunities

### Advanced Features
- **Calling Card Categories**: Organize calling cards by relationship type
- **Enhanced Metadata**: Additional information storage in calling cards
- **Batch Operations**: Bulk calling card creation and management
- **Integration APIs**: Extended API support for external systems

### User Experience Improvements
- **Custom Messages**: Personalized messages with calling card offers
- **Preview System**: Preview calling card appearance before creation
- **Search Integration**: Find users by calling card information
- **Social Features**: Calling card sharing and recommendation systems

### Administrative Enhancements
- **Usage Statistics**: Calling card creation and usage metrics
- **Moderation Tools**: Administrative controls for calling card content
- **Audit Logging**: Detailed logging for security and compliance
- **Batch Management**: Administrative tools for bulk calling card operations

## Conclusion

The CallingCardModule (XCallingCard) provides essential social networking functionality for OpenSimulator environments. Its comprehensive calling card management, cross-region messaging support, and robust inventory integration make it valuable for both social and professional virtual world applications. The module's event-driven architecture, god mode support, and thread-safe design ensure reliable operation in multi-user, multi-region environments while maintaining performance and security standards.