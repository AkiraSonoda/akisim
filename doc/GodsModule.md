# GodsModule Technical Documentation

## Overview

The **GodsModule** is a core OpenSimulator module that provides god powers and administrative control functionality for virtual worlds. It implements user kicking, freezing/unfreezing, god power management, and cross-region administrative actions. The module is essential for grid administration, user moderation, and maintaining order in virtual environments.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **IGodsModule**: God-specific functionality interface for external access

### Key Components
- **God Powers Management**: Handle requests for god-like abilities
- **User Moderation**: Kick, freeze, and unfreeze user functionality
- **Cross-Region Actions**: Handle administrative actions across grid regions
- **God Level Hierarchy**: Enforce god level restrictions and permissions
- **CAPS Integration**: Handle viewer capabilities for god functions

## God Powers System

### God Level Hierarchy
The module implements a hierarchical god level system:
- **Regular Users**: Level 0 (no god powers)
- **Regional Gods**: Levels 1-199 (region-specific powers)
- **Grid Gods**: Levels 200+ (grid-wide powers)
- **Super Grid Gods**: Level 240+ (maximum authority)

### Power Management
```csharp
public void RequestGodlikePowers(UUID agentID, UUID sessionID, UUID token, bool godLike)
{
    ScenePresence sp = m_scene.GetScenePresence(agentID);
    if(sp == null || sp.IsDeleted || sp.IsNPC)
        return;

    if (sessionID != sp.ControllingClient.SessionId)
        return;

    sp.GrantGodlikePowers(token, godLike);

    if (godLike && !sp.IsViewerUIGod && m_dialogModule != null)
       m_dialogModule.SendAlertToUser(agentID, "Request for god powers denied");
}
```

### God Authority Validation
- **Session Validation**: Verify correct session ID for god power requests
- **Presence Checks**: Ensure requesting user is valid and present
- **UI God Status**: Check viewer UI god status for power grants
- **Token Authentication**: Validate god power tokens for security

## User Moderation Features

### Kick Functionality

#### Standard User Kick
```csharp
public void KickUser(UUID godID, UUID agentID, uint kickflags, string reason)
{
    // Validate god permissions
    if(!m_scene.Permissions.IsGod(godID))
        return;

    // Check god level hierarchy
    int godlevel = 200;
    ScenePresence god = m_scene.GetScenePresence(godID);
    if(god != null && god.GodController.GodLevel > godlevel)
        godlevel = god.GodController.GodLevel;

    // Execute kick based on flags
    doKickmodes(godID, sp, kickflags, reason);
}
```

#### Kick Modes
- **Kick (Flag 0)**: Forcibly disconnect user from region/grid
- **Freeze (Flag 1)**: Disable user movement and display reason
- **Unfreeze (Flag 2)**: Restore user movement and display reason

#### Mass Actions
```csharp
if(agentID == ALL_AGENTS)
{
    m_scene.ForEachRootScenePresence(delegate(ScenePresence p)
    {
        if (p.UUID != godID)
        {
            if(godlevel > p.GodController.GodLevel)
                doKickmodes(godID, p, kickflags, reason);
        }
    });
}
```

### Cross-Region Actions

#### Grid-Wide Kicks
```csharp
public void GridKickUser(UUID agentID, string reason)
{
    int godlevel = 240; // grid god default

    // Handle non-local users via instant message
    if (sp == null || sp.IsChildAgent)
    {
        IMessageTransferModule transferModule =
                m_scene.RequestModuleInterface<IMessageTransferModule>();
        if (transferModule != null)
        {
            transferModule.SendInstantMessage(new GridInstantMessage(
                    m_scene, Constants.servicesGodAgentID, "GRID", agentID, (byte)250, false,
                    reason, UUID.Zero, true, new Vector3(), new byte[] {0}, true),
                    delegate(bool success) {} );
        }
    }
}
```

#### Non-Local User Handling
- **Message Transfer**: Use instant messaging for cross-region actions
- **Grid Authority**: Special handling for grid-level administrative actions
- **Service God Agent**: Use dedicated service agent ID for grid operations
- **Fallback Mechanisms**: Graceful handling when users are not locally present

## CAPS Integration

### UntrustedSimulatorMessage Handler
```csharp
private void HandleUntrustedSimulatorMessage(IOSHttpRequest request, IOSHttpResponse response)
{
    OSDMap osd = (OSDMap)OSDParser.DeserializeLLSDXml(request.InputStream);
    string message = osd["message"].AsString();

    if (message == "GodKickUser")
    {
        OSDMap body = (OSDMap)osd["body"];
        OSDArray userInfo = (OSDArray)body["UserInfo"];
        OSDMap userData = (OSDMap)userInfo[0];

        UUID agentID = userData["AgentID"].AsUUID();
        UUID godID = userData["GodID"].AsUUID();
        UUID godSessionID = userData["GodSessionID"].AsUUID();
        uint kickFlags = userData["KickFlags"].AsUInteger();
        string reason = userData["Reason"].AsString();

        // Validate god authority and execute kick
        ScenePresence god = m_scene.GetScenePresence(godID);
        if (god == null || god.ControllingClient.SessionId != godSessionID)
        {
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        KickUser(godID, agentID, kickFlags, reason);
    }
}
```

### Security Features
- **Session Validation**: Verify god session IDs for CAPS requests
- **Authority Checking**: Validate god permissions before executing actions
- **Request Authentication**: Ensure requests come from authorized sources
- **Error Responses**: Proper HTTP status codes for invalid requests

## Client Integration

### Event Subscription
```csharp
public void SubscribeToClientEvents(IClientAPI client)
{
    client.OnGodKickUser += KickUser;
    client.OnRequestGodlikePowers += RequestGodlikePowers;
}
```

### Client Events
- **OnGodKickUser**: Handle viewer-initiated kick requests
- **OnRequestGodlikePowers**: Process god power activation requests
- **Session Management**: Automatic subscription for new clients

### Viewer Integration
- **God Panel Support**: Interface with viewer god control panels
- **Status Updates**: Provide feedback for administrative actions
- **Permission Feedback**: Notify users of denied god power requests
- **Action Confirmation**: Confirm successful kicks, freezes, and unfreezes

## Instant Messaging Integration

### Cross-Region Communication
```csharp
private void OnIncomingInstantMessage(GridInstantMessage msg)
{
    if (msg.dialog == (uint)250) // Nonlocal kick
    {
        UUID agentID = new UUID(msg.toAgentID);
        string reason = msg.message;
        UUID godID = new UUID(msg.fromAgentID);
        uint kickMode = (uint)msg.binaryBucket[0];

        if(godID == Constants.servicesGodAgentID)
            GridKickUser(agentID, reason);
        else
            KickUser(godID, agentID, kickMode, reason);
    }
}
```

### Message Types
- **Dialog 250**: Nonlocal kick messages for cross-region actions
- **Grid Messages**: Special handling for grid service messages
- **Binary Data**: Kick mode flags transmitted in binary bucket
- **Authentication**: Validate message sources and authority

## Security and Authority

### Permission Validation
```csharp
if(!m_scene.Permissions.IsGod(godID))
    return;

if (godlevel <= sp.GodController.GodLevel) // no god wars
{
    if(m_dialogModule != null)
        m_dialogModule.SendAlertToUser(sp.UUID,
            "Kick from " + godID.ToString() + " ignored, kick reason: " + reason);
    return;
}
```

### God War Prevention
- **Level Hierarchy**: Higher level gods can override lower level gods
- **Peer Protection**: Gods of equal level cannot kick each other
- **Self-Protection**: Gods cannot perform actions on themselves
- **Authority Validation**: Verify god status before allowing actions

### Session Security
- **Session Matching**: Verify session IDs match for all god actions
- **Token Validation**: Authenticate god power tokens
- **Capability Security**: Secure CAPS handler with proper validation
- **Cross-Region Authentication**: Validate authority for remote actions

## NPC Handling

### NPC-Specific Actions
```csharp
if (sp.IsNPC)
{
    INPCModule npcmodule = sp.Scene.RequestModuleInterface<INPCModule>();
    if (npcmodule != null)
    {
        npcmodule.DeleteNPC(sp.UUID, sp.Scene);
        return;
    }
}
```

### Special Considerations
- **NPC Detection**: Identify and handle NPCs differently from users
- **NPC Deletion**: Use NPC module for proper NPC removal
- **Module Integration**: Coordinate with NPC management systems
- **Graceful Fallback**: Handle cases where NPC module is unavailable

## Error Handling and Validation

### Input Validation
```csharp
ScenePresence sp = m_scene.GetScenePresence(agentID);
if(sp == null || sp.IsDeleted || sp.IsNPC)
    return;

if (sessionID != sp.ControllingClient.SessionId)
    return;
```

### Safety Checks
- **Presence Validation**: Ensure target users exist and are valid
- **Deletion Checks**: Prevent actions on deleted scene presences
- **Child Agent Filtering**: Handle child agents appropriately
- **Session Verification**: Validate all session-based operations

### Error Recovery
- **Graceful Degradation**: Continue operation when optional modules unavailable
- **Dialog Fallbacks**: Handle missing dialog module gracefully
- **Network Resilience**: Recover from cross-region communication failures
- **Authority Fallbacks**: Provide clear feedback for unauthorized actions

## Performance Considerations

### Efficient Operations
- **Direct Scene Access**: Minimize overhead with direct scene operations
- **Cached God Levels**: Use cached god level information when available
- **Batch Processing**: Support mass actions with efficient iteration
- **Minimal Allocations**: Reuse objects and minimize memory allocations

### Scalability Features
- **Per-Region Instances**: Independent module instances per region
- **Cross-Region Messaging**: Efficient grid-wide action propagation
- **Event-Driven Architecture**: Responsive to client and system events
- **Resource Management**: Minimal resource usage for administrative functions

### Optimization Strategies
- **Early Returns**: Quick validation and early returns for invalid operations
- **Level Caching**: Cache god levels to avoid repeated lookups
- **Message Batching**: Efficient cross-region message handling
- **Connection Reuse**: Leverage existing connections for administrative actions

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource source)
{
    // No specific configuration required
}
```
- **No Configuration**: Module requires no external configuration
- **Always Active**: Module initializes automatically as core functionality

### Region Integration
```csharp
public void AddRegion(Scene scene)
{
    m_scene = scene;
    m_scene.RegisterModuleInterface<IGodsModule>(this);
    m_scene.EventManager.OnNewClient += SubscribeToClientEvents;
    m_scene.EventManager.OnRegisterCaps += OnRegisterCaps;
    scene.EventManager.OnIncomingInstantMessage += OnIncomingInstantMessage;
}
```

### Event Registration
- **Client Events**: Subscribe to new client connections
- **CAPS Registration**: Register capabilities for god functions
- **Message Handling**: Listen for cross-region instant messages
- **Interface Registration**: Make IGodsModule available to other modules

### Cleanup
```csharp
public void RemoveRegion(Scene scene)
{
    m_scene.UnregisterModuleInterface<IGodsModule>(this);
    m_scene.EventManager.OnNewClient -= SubscribeToClientEvents;
    m_scene = null;
}
```

## API Interface

### IGodsModule Methods

#### KickUser
```csharp
public void KickUser(UUID godID, UUID agentID, uint kickflags, string reason)
```
- **Purpose**: Kick, freeze, or unfreeze a user
- **Parameters**: God ID, target agent, kick flags, reason message
- **Authority**: Validates god permissions and level hierarchy
- **Scope**: Can handle local and cross-region actions

#### GridKickUser
```csharp
public void GridKickUser(UUID agentID, string reason)
```
- **Purpose**: Grid-level user kick with maximum authority
- **Parameters**: Target agent ID, reason message
- **Authority**: Grid god level (240+) permissions
- **Scope**: Grid-wide action affecting all regions

#### RequestGodlikePowers
```csharp
public void RequestGodlikePowers(UUID agentID, UUID sessionID, UUID token, bool godLike)
```
- **Purpose**: Grant or revoke god powers for a user
- **Parameters**: Agent ID, session ID, authentication token, power state
- **Validation**: Session verification and token authentication
- **Feedback**: Provides user notification for denied requests

## Integration Examples

### Basic God Action
```csharp
// Get gods module interface
IGodsModule godsModule = scene.RequestModuleInterface<IGodsModule>();

// Kick a user
UUID godID = // ... god user ID
UUID targetID = // ... target user ID
godsModule.KickUser(godID, targetID, 0, "Violating terms of service");

// Freeze a user
godsModule.KickUser(godID, targetID, 1, "Suspended for review");

// Unfreeze a user
godsModule.KickUser(godID, targetID, 2, "Suspension lifted");
```

### Grid Administration
```csharp
// Grid-wide kick (requires grid god powers)
IGodsModule godsModule = scene.RequestModuleInterface<IGodsModule>();
godsModule.GridKickUser(violatorID, "Grid-wide ban for severe violations");
```

### God Power Management
```csharp
// Handle god power requests programmatically
IGodsModule godsModule = scene.RequestModuleInterface<IGodsModule>();
godsModule.RequestGodlikePowers(userID, sessionID, authToken, true);
```

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Always Enabled**: Module loaded by default as essential functionality
- **No Configuration**: Module requires no configuration settings
- **Logging Integration**: Comprehensive debug and info logging for operations

### Backward Compatibility
- **API Compatibility**: All existing IGodsModule methods remain unchanged
- **Event Compatibility**: Client event handling behavior unchanged
- **CAPS Compatibility**: Capability handlers remain identical
- **Cross-Region Compatibility**: Grid communication protocols unchanged

### Dependencies
- **Scene Management**: Integration with scene and region lifecycle
- **Client API**: Client event handling for god power requests
- **Dialog Module**: Optional dependency for user notifications (graceful degradation)
- **Message Transfer**: Cross-region instant messaging for grid actions
- **NPC Module**: Optional dependency for NPC handling

## Troubleshooting

### Common Issues

#### God Powers Not Working
- **Permission Check**: Verify user has god permissions in scene
- **God Level**: Check god level hierarchy and restrictions
- **Session Validation**: Ensure session IDs match for requests
- **Module Loading**: Verify GodsModule is loaded properly

#### Kick Commands Failing
- **Authority Level**: Verify kicking god has sufficient level
- **Target Validation**: Ensure target user exists and is accessible
- **Cross-Region**: Check instant messaging for non-local targets
- **NPC Handling**: Verify NPC module availability for NPC targets

#### CAPS Handler Issues
- **Registration**: Check CAPS handler registration in logs
- **Request Format**: Verify CAPS request data format
- **Authentication**: Ensure proper god session validation
- **HTTP Status**: Check HTTP response codes for error details

#### Cross-Region Actions
- **Message Transfer**: Verify message transfer module availability
- **Grid Communication**: Check instant messaging connectivity
- **Authority Propagation**: Ensure grid god authority is recognized
- **Service Agent**: Verify service god agent ID configuration

## Usage Examples

### Administrative Commands
```csharp
// Emergency user kick
IGodsModule gods = scene.RequestModuleInterface<IGodsModule>();
gods.KickUser(adminID, griefer ID, 0, "Emergency removal for griefing");

// Temporary freeze for investigation
gods.KickUser(adminID, suspectID, 1, "Frozen pending investigation");

// Mass freeze all users (emergency only)
gods.KickUser(adminID, GodsModule.ALL_AGENTS, 1, "Emergency server maintenance");
```

### Event-Driven Actions
```csharp
// Automatic god power handling
void OnNewClient(IClientAPI client)
{
    client.OnRequestGodlikePowers += (agentID, sessionID, token, godLike) =>
    {
        IGodsModule gods = scene.RequestModuleInterface<IGodsModule>();
        gods.RequestGodlikePowers(agentID, sessionID, token, godLike);
    };
}
```

### Grid Management
```csharp
// Grid-wide enforcement
void HandleSevereViolation(UUID violatorID, string evidence)
{
    IGodsModule gods = scene.RequestModuleInterface<IGodsModule>();
    gods.GridKickUser(violatorID, $"Grid ban: {evidence}");

    // Log the action
    m_log.WarnFormat("Grid ban issued for {0}: {1}", violatorID, evidence);
}
```

This documentation reflects the GodsModule implementation in `src/OpenSim.Region.CoreModules/Avatar/Gods/GodsModule.cs` and its integration with the factory-based module loading system.