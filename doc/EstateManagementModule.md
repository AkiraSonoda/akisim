# EstateManagementModule Technical Documentation

## Overview

The EstateManagementModule is a non-shared region module that provides comprehensive estate management functionality for OpenSimulator. It implements the core estate system that controls region access, user permissions, terrain management, and administrative controls, serving as the primary interface for estate owners to manage their virtual properties.

## Module Classification

- **Type**: INonSharedRegionModule, IEstateModule
- **Namespace**: OpenSim.Region.CoreModules.World.Estate
- **Assembly**: OpenSim.Region.CoreModules
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs (always enabled for essential functionality)

## Core Functionality

### Primary Purpose

The EstateManagementModule provides the foundational estate management system for OpenSimulator regions. It handles estate settings, access controls, ban management, terrain modification controls, and administrative functions that estate owners need to manage their virtual properties effectively.

### Key Features

1. **Estate Access Control**: Comprehensive user access management and restrictions
2. **Ban Management**: Estate-wide ban lists and access controls
3. **Terrain Management**: Terrain upload, download, and modification controls
4. **Region Flags**: Detailed region behavior configuration (damage, scripts, physics, etc.)
5. **Telehub Management**: Teleport hub configuration and landing point management
6. **Estate Messaging**: Estate-wide communication and notification systems
7. **Administrative Controls**: Region restart, user teleportation, and management functions
8. **Console Commands**: Extensive command-line interface for estate administration
9. **Payment Access Control**: Economic restrictions and payment verification
10. **Minor Access Control**: Age verification and minor access restrictions

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for non-shared modules
1. Initialise(IConfigSource) - Configuration loading and feature setup
2. AddRegion(Scene) - Register module interface, event handlers, and components
3. RegionLoaded(Scene) - User management integration and estate settings application
4. RemoveRegion(Scene) - Cleanup (no-op)
5. Close() - Command system cleanup and resource disposal
```

### Interface Implementation

The module implements two key interfaces:

#### INonSharedRegionModule
Provides per-region module instances for localized estate management.

#### IEstateModule
Defines the estate management contract with extensive functionality:

```csharp
public interface IEstateModule
{
    uint GetRegionFlags();                              // Region behavior flags
    bool IsManager(UUID avatarID);                      // Estate manager check
    bool HasAccess(UUID user);                          // Estate access validation
    void sendRegionHandshakeToAll();                   // Region info broadcast
    void TriggerEstateInfoChange();                     // Estate change notification
    void TriggerRegionInfoChange();                     // Region change notification
    // Additional estate management methods...
}
```

### Event System Architecture

```csharp
// Estate management events
public event ChangeDelegate OnRegionInfoChange;
public event ChangeDelegate OnEstateInfoChange;
public event MessageDelegate OnEstateMessage;
public event EstateTeleportOneUserHomeRequest OnEstateTeleportOneUserHomeRequest;
public event EstateTeleportAllUsersHomeRequest OnEstateTeleportAllUsersHomeRequest;

// Scene event integration
Scene.EventManager.OnNewClient += EventManager_OnNewClient;
Scene.EventManager.OnRequestChangeWaterHeight += ChangeWaterHeight;
```

## Configuration System

### Module Configuration

#### Basic Configuration ([EstateManagement] section)
- **AllowRegionRestartFromClient**: `boolean` - Allow client-initiated region restarts (default: true)
- **IgnoreEstateMinorAccessControl**: `boolean` - Ignore minor access restrictions (default: true)
- **IgnoreEstatePaymentAccessControl**: `boolean` - Ignore payment info requirements (default: true)

### Configuration Examples

#### Default Configuration
```ini
[EstateManagement]
AllowRegionRestartFromClient = true
IgnoreEstateMinorAccessControl = true
IgnoreEstatePaymentAccessControl = true
```

#### Restricted Configuration
```ini
[EstateManagement]
AllowRegionRestartFromClient = false
IgnoreEstateMinorAccessControl = false
IgnoreEstatePaymentAccessControl = false
```

### Access Control Configuration

```csharp
// Applied during RegionLoaded
scene.RegionInfo.EstateSettings.DoDenyMinors = !m_ignoreEstateMinorAccessControl;
scene.RegionInfo.EstateSettings.DoDenyAnonymous = !m_ignoreEstatePaymentAccessControl;
```

## Region Flags Management

### GetRegionFlags Implementation

```csharp
public uint GetRegionFlags()
{
    RegionFlags flags = RegionFlags.None;

    // Core region behaviors
    if (Scene.RegionInfo.RegionSettings.AllowDamage)
        flags |= RegionFlags.AllowDamage;
    if (Scene.RegionInfo.RegionSettings.BlockTerraform)
        flags |= RegionFlags.BlockTerraform;
    if (!Scene.RegionInfo.RegionSettings.AllowLandResell)
        flags |= RegionFlags.BlockLandResell;
    if (Scene.RegionInfo.RegionSettings.DisableCollisions)
        flags |= RegionFlags.SkipCollisions;
    if (Scene.RegionInfo.RegionSettings.DisableScripts)
        flags |= RegionFlags.SkipScripts;
    if (Scene.RegionInfo.RegionSettings.DisablePhysics)
        flags |= RegionFlags.SkipPhysics;

    // Additional behaviors and restrictions
    // ... (extensive flag management)

    return (uint)flags;
}
```

### Supported Region Flags

#### Core Functionality Flags
- **AllowDamage**: Enable/disable damage in the region
- **BlockTerraform**: Prevent terrain modification
- **BlockLandResell**: Restrict land resale operations
- **SkipCollisions**: Disable collision detection
- **SkipScripts**: Disable script execution
- **SkipPhysics**: Disable physics simulation

#### Access Control Flags
- **DenyAnonymous**: Require payment info for access
- **DenyAgeUnverified**: Require age verification
- **AllowDirectTeleport**: Enable direct teleportation
- **RestrictPushObject**: Limit push script functionality

#### Economic and Social Flags
- **AllowVoice**: Enable voice communication
- **BlockFly**: Restrict flying capabilities
- **NoFly**: Complete flying prohibition
- **Sandbox**: Sandbox mode restrictions

## Access Control System

### Estate Access Validation

```csharp
public bool HasAccess(UUID user)
{
    // Estate owner always has access
    if (Scene.RegionInfo.EstateSettings.IsEstateOwner(user))
        return true;

    // Estate manager access
    if (Scene.RegionInfo.EstateSettings.IsEstateManagerOrOwner(user))
        return true;

    // Check estate ban list
    if (Scene.RegionInfo.EstateSettings.IsBanned(user))
        return false;

    // Check access list if private estate
    if (Scene.RegionInfo.EstateSettings.PublicAccess)
        return true;

    return Scene.RegionInfo.EstateSettings.HasAccess(user);
}
```

### User Management Integration

```csharp
public bool IsManager(UUID avatarID)
{
    if (Scene.RegionInfo.EstateSettings.IsEstateOwner(avatarID))
        return true;

    return Scene.RegionInfo.EstateSettings.IsEstateManagerOrOwner(avatarID);
}
```

### Access Control Workflow

1. **Estate Owner Check**: Immediate access for estate owner
2. **Estate Manager Check**: Access for designated managers
3. **Ban List Validation**: Deny access for banned users
4. **Public Access Check**: Allow if estate is public
5. **Access List Validation**: Check explicit access permissions
6. **Age/Payment Verification**: Apply additional restrictions if configured

## Terrain Management

### Terrain Upload/Download System

```csharp
private EstateTerrainXferHandler TerrainUploader;

// Terrain modification handling
private void ClientOnRequestTerrain(IClientAPI client, string filename)
{
    // Terrain download implementation
}

private void ClientOnUploadTerrain(IClientAPI client, string filename,
    byte[] terrainData, IClientAPI remoteClient)
{
    // Terrain upload implementation with permission checks
}
```

### Terrain Permissions

- **Estate Owner**: Full terrain modification rights
- **Estate Manager**: Terrain modification if permitted
- **Region Settings**: BlockTerraform flag controls general access
- **Permission System**: Integration with scene permission system

## Telehub Management

### TelehubManager Integration

```csharp
public TelehubManager m_Telehub;

// Initialization in AddRegion
m_Telehub = new TelehubManager(scene);
```

### Telehub Functionality

- **Landing Point Management**: Configure teleport landing locations
- **Spawn Point Control**: Manage user spawn positions
- **Teleport Routing**: Control how users enter the region
- **Access Integration**: Combine with estate access controls

## Administrative Functions

### Region Restart Management

```csharp
public bool AllowRegionRestartFromClient { get; set; }

// Client restart request handling
private void OnRestartRegion(IClientAPI client)
{
    if (!AllowRegionRestartFromClient)
    {
        client.SendAlertMessage("Region restart from client is disabled");
        return;
    }

    // Permission validation and restart processing
}
```

### User Teleportation Controls

```csharp
// Teleport one user home
public void OnEstateTeleportOneUserHomeRequest(IClientAPI client, UUID invoice,
    UUID senderID, UUID prey, bool kick)
{
    // Permission validation
    if (!Scene.Permissions.CanIssueEstateCommand(client.AgentId, false))
        return;

    // Teleport or kick user implementation
}

// Teleport all users home
public void OnEstateTeleportAllUsersHomeRequest(IClientAPI client, UUID invoice, UUID senderID)
{
    // Mass teleportation implementation
}
```

### Estate Messaging System

```csharp
public void OnEstateMessage(UUID RegionID, UUID FromID, string FromName, string Message)
{
    // Estate-wide message broadcasting
    foreach (ScenePresence presence in Scene.GetScenePresences())
    {
        if (!presence.IsChildAgent)
        {
            presence.ControllingClient.SendChatMessage(
                Message, (byte)ChatTypeEnum.Say, Vector3.Zero,
                FromName, FromID, UUID.Zero, (byte)ChatSourceType.System,
                (byte)ChatAudibleLevel.Fully);
        }
    }

    // Trigger estate message event for external handling
    OnEstateMessage?.Invoke(RegionID, FromID, FromName, Message);
}
```

## Console Commands Integration

### EstateManagementCommands

```csharp
protected EstateManagementCommands m_commands;

// Initialization
m_commands = new EstateManagementCommands(this);
m_commands.Initialise();

// Cleanup
public void Close()
{
    m_commands.Close();
}
```

### Available Console Commands

#### Estate Information Commands
- **estate show**: Display estate information and settings
- **estate set**: Modify estate settings and parameters
- **region show**: Display region information and flags

#### Access Control Commands
- **estate ban add**: Add user to estate ban list
- **estate ban remove**: Remove user from estate ban list
- **estate access add**: Add user to estate access list
- **estate access remove**: Remove user from estate access list

#### Administrative Commands
- **region restart**: Restart the region with optional timer
- **estate teleport home**: Teleport users home from estate
- **estate message**: Send estate-wide message

## Event Handling and Notifications

### Change Notification System

```csharp
private Timer m_regionChangeTimer = new Timer();

// Timer-based change aggregation
private void RaiseRegionInfoChange(object sender, ElapsedEventArgs e)
{
    if (OnRegionInfoChange != null)
        OnRegionInfoChange(Scene.RegionInfo.RegionID);
}

public void TriggerRegionInfoChange()
{
    m_regionChangeTimer.Stop();
    m_regionChangeTimer.Start();
}
```

### Event Propagation

- **OnRegionInfoChange**: Fired when region settings change
- **OnEstateInfoChange**: Fired when estate settings change
- **OnEstateMessage**: Fired when estate messages are sent
- **Teleport Events**: Fired for user teleportation requests

## Client Integration

### New Client Registration

```csharp
private void EventManager_OnNewClient(IClientAPI client)
{
    // Register estate-related event handlers
    client.OnDetailedEstateDataRequest += ClientSendDetailedEstateData;
    client.OnSetEstateFlagsRequest += EstateSetRegionFlagsRequest;
    client.OnSetEstateTerrainBaseTexture += SetEstateTerrainBaseTexture;
    client.OnCommitEstateTerrainTextureRequest += CommitEstateTerrainTextureRequest;
    // ... extensive client event registration
}
```

### Estate Data Communication

```csharp
public void sendRegionHandshakeToAll()
{
    Scene.ForEachClient(delegate(IClientAPI client)
    {
        SendRegionHandshake(client);
    });
}

private void SendRegionHandshake(IClientAPI client)
{
    RegionHandshakeArgs args = new RegionHandshakeArgs();
    // Populate handshake data with estate and region information
    client.SendRegionHandshake(args);
}
```

## Security and Permissions

### Permission Validation

```csharp
// Estate command permission check
private bool IsEstateManagerOrOwner(IClientAPI client)
{
    return Scene.RegionInfo.EstateSettings.IsEstateManagerOrOwner(client.AgentId);
}

// Estate modification permission check
private bool CanEditEstate(IClientAPI client)
{
    if (!IsEstateManagerOrOwner(client))
    {
        client.SendAlertMessage("You are not authorized to modify estate settings");
        return false;
    }
    return true;
}
```

### Access Control Security

- **Estate Owner Rights**: Full administrative control
- **Estate Manager Rights**: Delegated administrative functions
- **User Access Validation**: Multi-layered access checking
- **Ban Enforcement**: Immediate access denial for banned users
- **Age/Payment Verification**: Additional security layers

## Performance Considerations

### Timer-Based Change Aggregation

```csharp
private Timer m_regionChangeTimer = new Timer();
m_regionChangeTimer.Interval = 10000;  // 10-second aggregation
m_regionChangeTimer.Elapsed += RaiseRegionInfoChange;
m_regionChangeTimer.AutoReset = false;
```

**Benefits**:
- **Change Aggregation**: Prevents rapid-fire change notifications
- **Performance Optimization**: Reduces event processing overhead
- **Network Efficiency**: Batches related changes

### Efficient Access Checking

- **Early Returns**: Estate owner and manager checks first
- **Cached Settings**: Estate settings cached in memory
- **Permission Caching**: Expensive permission checks cached when possible

### Resource Management

- **Event Handler Management**: Proper registration and cleanup
- **Timer Management**: Careful timer lifecycle management
- **Memory Efficiency**: Minimal object allocation in hot paths

## Integration Points

### Scene Integration

- **IEstateModule Registration**: Provides estate services to scene
- **Event Manager Integration**: Handles scene-wide events
- **Permission System**: Integrates with scene permission checking
- **User Management**: Coordinates with user management systems

### Client Communication Integration

- **Protocol Handling**: Manages estate-related client protocol messages
- **UI Integration**: Supports viewer estate management interfaces
- **Notification System**: Real-time estate change notifications

### External System Integration

- **Database Integration**: Estate settings persistence
- **Grid Services**: Cross-region estate coordination
- **Asset System**: Terrain and covenant asset management

## Error Handling and Resilience

### Configuration Validation

```csharp
public void Initialise(IConfigSource source)
{
    AllowRegionRestartFromClient = true;  // Safe default

    IConfig config = source.Configs["EstateManagement"];
    if (config != null)
    {
        // Safe configuration loading with defaults
        AllowRegionRestartFromClient = config.GetBoolean("AllowRegionRestartFromClient", true);
        // ... additional configuration with fallbacks
    }
}
```

### Client Error Handling

```csharp
private void HandleEstateRequest(IClientAPI client, ...)
{
    try
    {
        if (!IsEstateManagerOrOwner(client))
        {
            client.SendAlertMessage("Insufficient permissions");
            return;
        }

        // Process estate request
    }
    catch (Exception e)
    {
        m_log.ErrorFormat("[ESTATE]: Error processing request: {0}", e.Message);
        client.SendAlertMessage("Error processing estate request");
    }
}
```

### Graceful Degradation

- **Service Unavailable**: Module continues with reduced functionality
- **Permission Denied**: Clear error messages and graceful handling
- **Invalid Requests**: Validation and appropriate error responses

## Use Cases and Applications

### Estate Management

- **Property Administration**: Complete estate property management
- **Access Control**: Visitor and resident access management
- **Security Management**: Ban lists and access restrictions
- **Community Management**: Estate-wide communication and rules

### Commercial Applications

- **Virtual Real Estate**: Professional property management
- **Event Management**: Conference and event access controls
- **Business Operations**: Corporate virtual presence management
- **Retail Environments**: Customer access and experience management

### Educational Applications

- **Campus Management**: Educational institution estate controls
- **Classroom Access**: Student and faculty access management
- **Research Environments**: Controlled access research spaces
- **Training Simulations**: Specialized training environment controls

### Community Applications

- **Neighborhood Management**: Residential community administration
- **Social Spaces**: Community gathering space management
- **Special Interest Groups**: Hobby and interest-based estate management
- **Family Estates**: Private family virtual property management

## Dependencies

### Core Framework Dependencies

- `OpenSim.Framework` - Core data structures and utilities
- `OpenSim.Framework.Monitoring` - Performance monitoring integration
- `OpenSim.Region.Framework.Interfaces` - Module interface contracts
- `OpenSim.Region.Framework.Scenes` - Scene and presence management
- `OpenSim.Services.Interfaces` - Service interface definitions

### System Dependencies

- `System.Timers` - Change notification timer management
- `System.Collections.Concurrent` - Thread-safe collections
- `System.Security` - Security and permission validation

### OpenSimulator Dependencies

- **IUserManagement**: User identification and management
- **IEstateDataService**: Estate data persistence
- **Scene Permission System**: Access control integration
- **Client Communication**: Estate protocol message handling

## Future Enhancement Opportunities

### Advanced Access Controls

- **Role-Based Permissions**: Granular permission systems
- **Temporary Access**: Time-limited access permissions
- **Group Integration**: Group-based access controls
- **External Authentication**: Third-party authentication systems

### Enhanced Management Tools

- **Web Interface**: Browser-based estate management
- **Mobile Management**: Mobile device estate administration
- **Bulk Operations**: Mass user management capabilities
- **Analytics Integration**: Estate usage and visitor analytics

### Automation Features

- **Scheduled Operations**: Automated estate maintenance
- **Event-Driven Actions**: Automated responses to estate events
- **Integration APIs**: External system integration capabilities
- **Monitoring and Alerting**: Estate health and security monitoring

## Troubleshooting

### Common Configuration Issues

1. **Module Not Loading**
   - Verify EstateManagementModule is included in factory
   - Check that World.Estate namespace is available
   - Review startup logs for module initialization

2. **Permission Problems**
   - Verify estate owner and manager settings
   - Check estate access list configuration
   - Review ban list for unintended entries

3. **Client Communication Issues**
   - Verify client event handler registration
   - Check estate data packet transmission
   - Review client-side estate UI functionality

### Common Runtime Issues

1. **Access Denied Errors**
   - Check estate owner and manager permissions
   - Verify access list configuration
   - Review ban list for affected users

2. **Region Flag Problems**
   - Verify region settings configuration
   - Check estate override settings
   - Review permission system integration

3. **Terrain Management Issues**
   - Verify terrain upload permissions
   - Check estate owner/manager rights
   - Review terraform restrictions

### Debug Configuration

```ini
[EstateManagement]
AllowRegionRestartFromClient = true
IgnoreEstateMinorAccessControl = true
IgnoreEstatePaymentAccessControl = true

# Enable detailed logging
[Logging]
LogLevel = DEBUG
```

### Log Analysis

Monitor estate operations through comprehensive logging:
```
[ESTATE]: Estate settings changed for region RegionName
[ESTATE]: User 12345678-1234-1234-1234-123456789012 added to access list
[ESTATE]: Region flags updated: AllowDamage=true, BlockTerraform=false
```

## Conclusion

The EstateManagementModule provides essential estate management functionality for OpenSimulator environments. Its comprehensive access control system, extensive administrative tools, robust permission management, and flexible configuration options make it indispensable for both commercial and community virtual world deployments. The module's integration with scene management, client communication, and external systems ensures reliable estate administration while maintaining security and performance standards.