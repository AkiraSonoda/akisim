# BuySellModule Technical Documentation

## Overview

The **BuySellModule** is a core OpenSimulator module that manages object buying and selling transactions within virtual worlds. It provides comprehensive marketplace functionality including in-place sales, copy sales, and content sales. The module handles permission validation, asset creation, inventory management, and ownership transfers for all object transaction types.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **IBuySellModule**: Buy/sell-specific functionality interface for external access

### Key Components
- **Sale Type Management**: Support for three distinct sale types
- **Permission Validation**: Comprehensive permission checking for all transactions
- **Asset Creation**: Dynamic asset generation for copy sales
- **Inventory Integration**: Seamless inventory item creation and transfer
- **Ownership Transfer**: Secure object and content ownership management

## Sale Types and Transaction Modes

### SaleType.Original (In-Place Sale)
Transfers ownership of the existing object without creating copies:

#### Transaction Flow
1. **Permission Check**: Verify transfer permissions and seller authorization
2. **Ownership Transfer**: Change object owner to buyer
3. **Permission Propagation**: Apply next-owner permissions to all parts
4. **Inventory Propagation**: Transfer inventory items within object parts
5. **Sale Reset**: Reset sale settings and click actions
6. **Script Notification**: Trigger Changed.OWNER events in scripts

#### Object State Changes
```csharp
group.SetOwner(remoteClient.AgentId, remoteClient.ActiveGroupId);
rootpart.ObjectSaleType = 0;        // Remove from sale
rootpart.SalePrice = 10;            // Reset to default price
rootpart.ClickAction = 0;           // Reset click action
```

### SaleType.Copy (Copy Sale)
Creates a copy of the object as an inventory item for the buyer:

#### Transaction Flow
1. **Permission Check**: Verify both transfer and copy permissions
2. **State Preservation**: Save original sale settings
3. **Object Serialization**: Convert object to XML format for asset storage
4. **Asset Creation**: Create new asset in asset service
5. **Inventory Item Creation**: Generate inventory item with proper permissions
6. **Permission Calculation**: Apply next-owner and folded permissions
7. **Client Notification**: Send inventory update to buyer

#### Asset and Inventory Management
```csharp
string sceneObjectXml = SceneObjectSerializer.ToOriginalXmlFormat(group);
AssetBase asset = m_scene.CreateAsset(name, desc, (sbyte)AssetType.Object,
                                      Utils.StringToBytes(sceneObjectXml), rootpart.CreatorID);
m_scene.AssetService.Store(asset);
```

### SaleType.Contents (Content Sale)
Transfers all inventory items contained within the object to the buyer:

#### Transaction Flow
1. **Inventory Enumeration**: List all items in object inventory
2. **Permission Validation**: Check transfer permissions for each item
3. **Batch Transfer**: Move all transferable items to buyer's inventory
4. **Content Validation**: Ensure all items are properly transferable

#### Content Transfer Process
```csharp
List<UUID> invList = rootpart.Inventory.GetInventoryList();
m_scene.MoveTaskInventoryItems(remoteClient.AgentId, rootpart.Name, rootpart, invList);
```

## Permission System Integration

### Sale Permission Validation
```csharp
if (!m_scene.Permissions.CanSellObject(client, sog, saleType))
{
    client.SendAgentAlertMessage("You don't have permission to set object on sale", false);
    return;
}
```

### Transfer Permission Requirements
- **SaleType.Original**: Requires Transfer permission
- **SaleType.Copy**: Requires both Transfer and Copy permissions
- **SaleType.Contents**: Requires Transfer permission on all contained items

### Permission Propagation
For original sales, the module handles comprehensive permission updates:
```csharp
if (m_scene.Permissions.PropagatePermissions())
{
    foreach (SceneObjectPart child in group.Parts)
    {
        child.Inventory.ChangeInventoryOwner(remoteClient.AgentId);
        child.TriggerScriptChangedEvent(Changed.OWNER);
        child.ApplyNextOwnerPermissions();
    }
    group.InvalidateDeepEffectivePerms();
}
```

## Client Integration

### Event Registration
The module subscribes to client events for sale information updates:
```csharp
public void SubscribeToClientEvents(IClientAPI client)
{
    client.OnObjectSaleInfo += ObjectSaleInfo;
}
```

### Sale Information Updates
Handles client requests to modify object sale settings:
```csharp
protected void ObjectSaleInfo(IClientAPI client, UUID agentID, UUID sessionID,
                             uint localID, byte saleType, int salePrice)
```

### Client Notifications
- **Sale Success**: Inventory updates and object property changes
- **Sale Failure**: Alert messages explaining failure reasons
- **Permission Errors**: Clear feedback for permission-related failures

## Asset and Inventory Management

### Asset Creation Process
For copy sales, the module creates new assets:
```csharp
AssetBase asset = m_scene.CreateAsset(
    name, desc,                           // Asset name and description
    (sbyte)AssetType.Object,             // Asset type
    Utils.StringToBytes(sceneObjectXml), // Serialized object data
    rootpart.CreatorID);                 // Original creator ID
```

### Inventory Item Configuration
```csharp
InventoryItemBase item = new InventoryItemBase();
item.CreatorId = rootpart.CreatorID.ToString();
item.CreatorData = rootpart.CreatorData;
item.ID = UUID.Random();
item.Owner = remoteClient.AgentId;
item.AssetID = asset.FullID;
item.AssetType = asset.Type;
item.InvType = (int)InventoryType.Object;
```

### Permission Calculation
Complex permission folding for copy sales:
```csharp
perms = group.CurrentAndFoldedNextPermissions();
PermissionsUtil.ApplyNoModFoldedPermissions(perms, ref perms);
perms &= rootpart.NextOwnerMask;
perms = PermissionsUtil.FixAndFoldPermissions(perms);
```

## Error Handling and Validation

### Object Validation
```csharp
SceneObjectGroup group = rootpart.ParentGroup;
if(group == null || group.IsDeleted || group.inTransit)
    return false;
```

### Permission Error Messages
- **"This item doesn't appear to be for sale"**: Transfer permission failure
- **"This sale has been blocked by the permissions system"**: Copy permission failure
- **"This item's inventory doesn't appear to be for sale"**: Content permission failure
- **"Cannot buy now. Your inventory is unavailable"**: Inventory system failure

### Transaction Safety
- **Root Part Validation**: Ensures transactions operate on root parts only
- **Group Consistency**: Validates object group integrity
- **Transit Protection**: Prevents transactions on objects in transit
- **Atomic Operations**: Ensures transaction completeness or rollback

## Security Considerations

### Permission Enforcement
- **Seller Authorization**: Validates seller has permission to list object for sale
- **Transfer Verification**: Confirms object can be legally transferred
- **Copy Validation**: Ensures copy permissions allow duplication
- **Content Screening**: Validates all inventory items are transferable

### Transaction Integrity
- **State Consistency**: Maintains object state consistency throughout transactions
- **Ownership Verification**: Ensures proper ownership chain validation
- **Asset Security**: Secures asset creation and storage processes
- **Client Validation**: Validates client authorization for all operations

## Performance Considerations

### Efficient Operations
- **Direct Object Access**: Uses scene object part lookups for minimal overhead
- **Batch Processing**: Processes inventory transfers in batches
- **State Caching**: Maintains object state during complex transactions
- **Permission Caching**: Leverages permission system caching

### Memory Management
- **Asset Lifecycle**: Proper asset creation and cleanup
- **Object References**: Efficient object part and group referencing
- **Inventory Optimization**: Streamlined inventory item creation
- **Event Cleanup**: Proper client event subscription management

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource source) {}
```
- **No Configuration**: Module requires no specific configuration
- **Always Enabled**: Module loads by default for all regions

### Region Integration
```csharp
public void AddRegion(Scene scene)
public void RegionLoaded(Scene scene)
```
- **Interface Registration**: Register IBuySellModule with scene
- **Event Subscription**: Subscribe to new client events
- **Dialog Module**: Obtain reference to dialog module for user messages

### Cleanup
```csharp
public void RemoveRegion(Scene scene)
public void Close()
```
- **Event Unsubscription**: Clean up client event subscriptions
- **Resource Cleanup**: Proper module resource management

## API Interface

### IBuySellModule.BuyObject Method
```csharp
bool BuyObject(IClientAPI remoteClient, UUID categoryID, uint localID, byte saleType, int salePrice)
```

#### Parameters
- **remoteClient**: Client attempting to purchase object
- **categoryID**: Target inventory category for purchased items
- **localID**: Local identifier of object being purchased
- **saleType**: Type of sale (Original, Copy, or Contents)
- **salePrice**: Expected sale price for validation

#### Return Value
- **true**: Transaction completed successfully
- **false**: Transaction failed (see client alerts for details)

### Internal Sale Info Handler
```csharp
protected void ObjectSaleInfo(IClientAPI client, UUID agentID, UUID sessionID,
                             uint localID, byte saleType, int salePrice)
```
Handles client requests to modify object sale settings.

## Integration Examples

### Programmatic Purchase
```csharp
// Get buy/sell module interface
IBuySellModule buySellModule = scene.RequestModuleInterface<IBuySellModule>();

// Attempt to purchase object
UUID inventoryCategory = // ... target category
uint objectLocalID = // ... object identifier
bool success = buySellModule.BuyObject(client, inventoryCategory, objectLocalID,
                                      (byte)SaleType.Copy, expectedPrice);
```

### Sale Configuration
Objects can be configured for sale through viewer interfaces or scripting:
```csharp
// Set object for sale (handled by ObjectSaleInfo)
part.ObjectSaleType = (byte)SaleType.Copy;
part.SalePrice = 100;  // Sale price in currency units
```

## Economic Integration

### Currency System
- **Price Validation**: Validates expected sale prices
- **Transaction Recording**: Records successful transactions
- **Economic Events**: Triggers economic system notifications
- **Market Integration**: Supports marketplace and commerce systems

### Value Preservation
- **Creator Attribution**: Maintains original creator information
- **Permission Chains**: Preserves permission inheritance
- **Content Integrity**: Ensures content authenticity during transfers
- **Ownership History**: Maintains ownership trail for transactions

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Always Enabled**: Module loaded by default (no configuration required)
- **Essential Functionality**: Object trading is core virtual world functionality
- **Logging Integration**: Comprehensive debug and info logging for operations

### Dependencies
- **Scene Management**: Integration with scene object and inventory systems
- **Asset Service**: Required for copy sale asset creation and storage
- **Permission System**: Critical dependency for transaction authorization
- **Dialog Module**: Optional dependency for user notification (graceful degradation)

## Troubleshooting

### Common Transaction Failures

#### "This item doesn't appear to be for sale"
- **Cause**: Object lacks transfer permissions
- **Solution**: Verify object permissions allow transfer
- **Check**: Ensure effective owner permissions include Transfer

#### "This sale has been blocked by the permissions system"
- **Cause**: Copy sale attempted on no-copy object
- **Solution**: Modify permissions or use different sale type
- **Check**: Verify both Transfer and Copy permissions

#### "Cannot buy now. Your inventory is unavailable"
- **Cause**: Inventory service unavailable or category invalid
- **Solution**: Check inventory service status and category validity
- **Check**: Verify client inventory connectivity

### Debug Information
- **Module Logging**: Comprehensive transaction logging available
- **Permission Tracing**: Detailed permission check logging
- **Asset Tracking**: Asset creation and storage logging
- **Client Feedback**: Clear error messages for all failure modes

## Usage Examples

### Basic Module Setup
No configuration required - module loads automatically:
```csharp
// Module is automatically available
IBuySellModule buySell = scene.RequestModuleInterface<IBuySellModule>();
```

### Transaction Processing
```csharp
// Handle purchase attempt
bool purchased = buySell.BuyObject(
    client,                    // Purchasing client
    inventoryFolderUUID,      // Target inventory folder
    objectLocalID,            // Object to purchase
    (byte)SaleType.Copy,      // Purchase type
    expectedPrice             // Price validation
);

if (purchased)
{
    // Transaction successful - object copied to inventory
    Console.WriteLine("Purchase completed successfully");
}
else
{
    // Transaction failed - check client alerts for reason
    Console.WriteLine("Purchase failed - see client notifications");
}
```

This documentation reflects the BuySellModule implementation in `src/OpenSim.Region.CoreModules/World/Objects/BuySell/BuySellModule.cs` and its integration with the factory-based module loading system.