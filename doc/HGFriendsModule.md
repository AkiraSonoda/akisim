# HGFriendsModule Documentation

## Overview

The `HGFriendsModule` is an advanced friends management module in Akisim that extends the base `FriendsModule` to provide **Hypergrid (HG)** friendship capabilities. It enables users to maintain friendships across different OpenSim grids, supporting cross-grid communication, status notifications, and permission management through the Hypergrid protocol.

## File Location

- **Source**: `src/OpenSim.Region.CoreModules/Avatar/Friends/HGFriendsModule.cs`
- **Namespace**: `OpenSim.Region.CoreModules.Avatar.Friends`

## Class Definition

```csharp
public class HGFriendsModule : FriendsModule, ISharedRegionModule, IFriendsModule, IFriendsSimConnector
```

## Dependencies

- **OpenMetaverse**: UUID handling and virtual world data structures
- **OpenSim.Framework**: Core framework utilities and base classes
- **OpenSim.Region.Framework.Interfaces**: Region module interfaces
- **OpenSim.Region.Framework.Scenes**: Scene management
- **OpenSim.Services.Interfaces**: Service interface definitions
- **OpenSim.Services.Connectors.Hypergrid**: Hypergrid connectivity services
- **Nini.Config**: Configuration management
- **log4net**: Logging framework

## Architecture Overview

### **Inheritance Hierarchy**
```
ISharedRegionModule
    ↑
FriendsModule  ←  HGFriendsModule
    ↑                    ↑
IFriendsModule    IFriendsSimConnector
```

### **Core Concept**
HGFriendsModule **extends** FriendsModule with hypergrid capabilities:
- **Base Functionality**: Inherits all local grid friends features
- **Cross-Grid Extensions**: Adds support for friends on other grids
- **Foreign User Management**: Handles users with `@grid.example.com` identifiers
- **Hypergrid Protocol**: Uses HG services for cross-grid communication

## Configuration

### **Module Selection**
The system chooses between FriendsModule and HGFriendsModule based on configuration in `[Modules]` section:

```ini
[Modules]
FriendsModule = HGFriendsModule   # Enable hypergrid friends support
# OR
FriendsModule = FriendsModule     # Local friends only (default)
```

### **HGFriendsModule-Specific Configuration**
```ini
[HGFriendsModule]
LevelHGFriends = 0    # Minimum user level required for cross-grid friendships
```

### **Required Base Configuration**
```ini
[Friends]
Port = 8003
Connector = "OpenSim.Services.Connectors.dll:FriendsServicesConnector"
```

## Key Components

### **Core Properties**

| Property | Type | Description |
|----------|------|-------------|
| `m_levelHGFriends` | `int` | Minimum user level for hypergrid friendship requests |
| `m_HGFriendsConnector` | `HGFriendsServicesConnector` | Hypergrid friends service connector |
| `m_StatusNotifier` | `HGStatusNotifier` | Cross-grid status notification handler |
| `UserManagementModule` | `IUserManagement` | User management with lazy loading |

### **Hypergrid-Specific Services**

| Service | Type | Purpose |
|---------|------|---------|
| `HGFriendsServicesConnector` | Connector | Cross-grid friends service communication |
| `HGStatusNotifier` | Notifier | Real-time status updates across grids |
| `IUserManagement` | Interface | Foreign user name/UUID resolution |

## Module Lifecycle

### 1. Initialization (`InitModule`)

Extends base initialization with HG-specific configuration:

```csharp
protected override void InitModule(IConfigSource config)
{
    base.InitModule(config);
    
    IConfig friendsConfig = config.Configs["HGFriendsModule"];
    if (friendsConfig != null)
    {
        m_levelHGFriends = friendsConfig.GetInt("LevelHGFriends", 0);
    }
}
```

**File**: `HGFriendsModule.cs:88`

### 2. Region Addition (`AddRegion`)

Extends base region addition with HG-specific registrations:

```csharp
public override void AddRegion(Scene scene)
{
    if (!m_Enabled) return;
    
    base.AddRegion(scene);
    scene.RegisterModuleInterface<IFriendsSimConnector>(this);
}
```

**File**: `HGFriendsModule.cs:71`

### 3. Region Loading (`RegionLoaded`)

Initializes hypergrid-specific components:

```csharp
public override void RegionLoaded(Scene scene)
{
    if (!m_Enabled) return;
    
    if (m_StatusNotifier == null)
        m_StatusNotifier = new HGStatusNotifier(this);
}
```

**File**: `HGFriendsModule.cs:80`

## Hypergrid Friend Identifier Format

### **Local Friends**
- **Format**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
- **Example**: `12345678-1234-1234-1234-123456789abc`

### **Foreign Friends (Hypergrid)**
- **Format**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx@grid.example.com`
- **Example**: `12345678-1234-1234-1234-123456789abc@grid.example.com`

## Core Functionality Extensions

### 1. **Cross-Grid Friend Requests**

#### Permission Level Checking
```csharp
protected override void OnInstantMessage(IClientAPI client, GridInstantMessage im)
{
    if ((InstantMessageDialog)im.dialog == InstantMessageDialog.FriendshipOffered)
    {
        UUID principalID = new(im.fromAgentID);
        UUID friendID = new(im.toAgentID);
        
        // Check if friendID is foreign and if principalID has permission
        if (!UserManagementModule.IsLocalGridUser(friendID))
        {
            if (avatar.GodController.UserLevel < m_levelHGFriends)
            {
                client.SendAgentAlertMessage("Unable to send friendship invitation to foreigner. Insufficient permissions.", false);
                return;
            }
        }
    }
    
    base.OnInstantMessage(client, im);
}
```

**File**: `HGFriendsModule.cs:121`

#### Security Features
- **Level-Based Permissions**: `m_levelHGFriends` configuration restricts who can befriend foreigners
- **Foreign User Detection**: Uses `UserManagementModule.IsLocalGridUser()`
- **Admin Override**: Higher-level users can bypass restrictions
- **Error Messaging**: Clear feedback for permission failures

### 2. **Foreign User Cache Management**

#### User Name Preloading
```csharp
protected override bool CacheFriends(IClientAPI client)
{
    if (base.CacheFriends(client))
    {
        UserFriendData FriendData = m_Friends[client.AgentId];
        if (FriendData.Refcount == 1) // Root agent only
        {
            foreach (FriendInfo finfo in FriendData.Friends)
            {
                if (Util.ParseFullUniversalUserIdentifier(finfo.Friend, out UUID id, out string url, out string first, out string last))
                {
                    uMan.AddUser(id, first, last, url);
                }
            }
        }
    }
}
```

**File**: `HGFriendsModule.cs:159`

#### Benefits
- **Performance**: Preloads foreign friend names to avoid lookup delays
- **User Experience**: Foreign friends display proper names instead of UUIDs
- **Caching Strategy**: Only loads for root agents to avoid duplication

### 3. **Cross-Grid Status Notifications**

#### Status Notification Interface
```csharp
public bool StatusNotify(UUID friendID, UUID userID, bool online)
{
    return LocalStatusNotification(friendID, userID, online);
}
```

**File**: `HGFriendsModule.cs:114`

#### HGStatusNotifier Integration
- **Real-Time Updates**: `HGStatusNotifier` handles cross-grid presence updates
- **Bidirectional Communication**: Status changes propagate to foreign grids
- **Local Notification**: Integrates with local status system

### 4. **Hypergrid Login Handling**

#### Foreign User Rights Management
```csharp
public override bool SendFriendsOnlineIfNeeded(IClientAPI client)
{
    if (base.SendFriendsOnlineIfNeeded(client))
    {
        AgentCircuitData aCircuit = ((Scene)client.Scene).AuthenticateHandler.GetAgentCircuitData(client.AgentId);
        if ((aCircuit.teleportFlags & (uint)Constants.TeleportFlags.ViaHGLogin) != 0)
        {
            UserAccount account = m_Scenes[0].UserAccountService.GetUserAccount(client.Scene.RegionInfo.ScopeID, client.AgentId);
            if (account is null) // foreign user
            {
                // Send friend rights to foreign user
                foreach (FriendInfo f in friends)
                {
                    int rights = f.TheirFlags;
                    if (rights != -1)
                        client.SendChangeUserRights(new UUID(f.Friend), client.AgentId, rights);
                }
            }
        }
    }
}
```

**File**: `HGFriendsModule.cs:196`

#### Features
- **HG Login Detection**: Identifies users arriving via hypergrid
- **Rights Synchronization**: Ensures foreign users receive proper friend permissions
- **Account Verification**: Distinguishes between local and foreign users

### 5. **Cross-Grid Presence Checking**

#### Enhanced Online Friend Detection
```csharp
protected override void GetOnlineFriends(UUID userID, List<string> friendList, List<UUID> online)
{
    List<string> fList = new();
    foreach (string s in friendList)
    {
        if (s.Length < 36)
            m_log.WarnFormat("Ignoring friend {0} since identifier too short", s);
        else
            fList.Add(s.Substring(0, 36)); // Extract UUID part
    }
    
    // Query local presence service
    PresenceInfo[] presence = PresenceService.GetAgents(fList.ToArray());
    
    // TODO: Also query presence status of friends in other grids
}
```

**File**: `HGFriendsModule.cs:223`

#### Current Implementation
- **Local Presence**: Checks presence service for online status
- **UUID Extraction**: Strips grid identifier for local presence queries
- **Future Enhancement**: Placeholder for cross-grid presence queries

## Integration Points

### **Base FriendsModule Integration**

All base functionality is inherited and extended:
- **Local Friends**: Full compatibility with local grid friends
- **Permission System**: Enhanced with cross-grid permission levels
- **Event Handling**: Extends base event handlers with HG logic
- **Caching System**: Adds foreign user name caching

### **Hypergrid Services Integration**

- **HGFriendsServicesConnector**: Handles cross-grid friends service calls
- **HGStatusNotifier**: Manages real-time status updates across grids
- **UserManagement**: Provides foreign user identification and caching

### **Scene Integration**

- **IFriendsSimConnector**: Registers as simulator connector for cross-grid communication
- **AgentCircuitData**: Integrates with teleport system for HG login detection
- **UserAccountService**: Distinguishes local vs foreign users

## Configuration Examples

### **Enable Hypergrid Friends**
```ini
[Modules]
FriendsModule = HGFriendsModule

[Friends]
Port = 8003
Connector = "OpenSim.Services.Connectors.dll:FriendsServicesConnector"

[HGFriendsModule]
LevelHGFriends = 0    # Allow all users to befriend foreigners
```

### **Restrict Hypergrid Friendships**
```ini
[HGFriendsModule]
LevelHGFriends = 200  # Require level 200+ for foreign friendships
```

### **Production Grid Example**
```ini
[Modules]
FriendsModule = HGFriendsModule

[Friends]
Port = 8003
Connector = "OpenSim.Services.Connectors.dll:FriendsServicesConnector"

[HGFriendsModule]
LevelHGFriends = 0

[Hypergrid]
# Additional HG configuration would go here
```

## User Experience

### **Local vs Hypergrid Friends**

#### **Local Friend Request**
1. User sends friend request to local user
2. Standard friend request dialog appears
3. Acceptance creates local friendship

#### **Hypergrid Friend Request**
1. User sends friend request to `user@foreign.grid`
2. System checks `LevelHGFriends` permission level
3. If permitted, request routes through hypergrid protocol
4. Foreign grid processes request
5. Acceptance creates cross-grid friendship

### **Friend List Display**

#### **Local Friends**
- **Display**: `John Doe`
- **Internal**: `12345678-1234-1234-1234-123456789abc`

#### **Foreign Friends**
- **Display**: `Jane Smith @ foreign.grid`
- **Internal**: `87654321-4321-4321-4321-abcdef123456@foreign.grid`

### **Status Updates**

#### **Cross-Grid Status**
- Online/offline status synchronized across grids
- Real-time updates via HGStatusNotifier
- Bidirectional status propagation

## Security Considerations

### **Permission Levels**
- **Level 0**: All users can befriend foreigners (default)
- **Level > 0**: Only users with sufficient level can send HG friend requests
- **Admin Override**: High-level users bypass restrictions

### **Foreign User Validation**
- User identifier format validation
- Grid reachability verification
- Permission propagation across grids

### **Data Privacy**
- Only friend-approved data shared across grids
- Local user data protected from foreign access
- Configurable permission levels for different data types

## Troubleshooting

### **Common Issues**

1. **Cannot Send Friend Request to Foreign User**
   - **Cause**: Insufficient permission level
   - **Solution**: Check `LevelHGFriends` setting or increase user level

2. **Foreign Friends Show as UUID**
   - **Cause**: User name cache not populated
   - **Solution**: User management module may need restart

3. **Status Updates Not Working**
   - **Cause**: HGStatusNotifier not initialized
   - **Solution**: Check region loading and HG service configuration

4. **Module Not Loading**
   - **Cause**: Incorrect configuration
   - **Solution**: Verify `FriendsModule = HGFriendsModule` setting

### **Debug Logging**
Enable debug logging to trace:
- HG friend request processing
- Foreign user cache operations
- Cross-grid status notifications
- Permission level checks

## Recent Changes

### **Mono.Addins Removal and ModuleFactory Integration**

As part of architectural modernization:

- Removed `using Mono.Addins;` directive
- Removed `[Extension]` attribute decoration
- **ModuleFactory Integration**: Loaded via configuration-based selection
- **Inheritance Preserved**: All base FriendsModule functionality maintained
- **HG Features**: All hypergrid capabilities preserved

## Performance Considerations

### **Caching Strategy**
- **Foreign User Names**: Cached on first friend list load
- **Status Information**: Cached with real-time updates
- **Permission Levels**: Cached per session

### **Network Optimization**
- **Batch Operations**: Multiple friend operations grouped
- **Lazy Loading**: Services loaded on demand
- **Connection Pooling**: Reuse HG service connections

## API Reference

### **Public Methods**

| Method | Purpose | Parameters | Returns |
|--------|---------|------------|---------|
| `StatusNotify()` | Cross-grid status notification | friendID, userID, online | bool |
| `AddRegion()` | Add region with HG support | Scene | void |
| `RegionLoaded()` | Complete region setup | Scene | void |
| `InitModule()` | Initialize with HG config | IConfigSource | void |

### **Properties**

| Property | Type | Purpose |
|----------|------|---------|
| `Name` | `string` | Returns "HGFriendsModule" |
| `UserManagementModule` | `IUserManagement` | Lazy-loaded user management |

### **Protected Override Methods**

| Method | Purpose | Enhanced Functionality |
|--------|---------|----------------------|
| `OnInstantMessage()` | Process friend requests | Permission level checking |
| `CacheFriends()` | Cache friend data | Foreign user name preloading |
| `SendFriendsOnlineIfNeeded()` | Send online status | HG login handling |
| `GetOnlineFriends()` | Check online status | UUID extraction for foreign friends |

## Version Information

- **Last Modified**: Part of Mono.Addins removal initiative
- **Compatibility**: OpenSim 0.9.x and later with Hypergrid support
- **Status**: Active development (extends FriendsModule)
- **Dependencies**: Requires Hypergrid infrastructure and services

## Related Documentation

- **FriendsModule.md**: Base friends functionality
- **Hypergrid Configuration**: Grid-to-grid setup
- **User Management**: Foreign user handling
- **Service Configuration**: Backend service setup