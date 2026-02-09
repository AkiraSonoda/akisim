# EstateModule Technical Documentation

## Overview

The EstateModule is a shared region module that provides estate communication and management functionality for OpenSimulator. It handles cross-region estate communications, processes estate-level commands and messages, manages teleport operations for estate owners, and coordinates with external estate management systems through HTTP connectors.

## Module Classification

- **Type**: ISharedRegionModule
- **Namespace**: OpenSim.Region.CoreModules.World.Estate
- **Assembly**: OpenSim.Region.CoreModules
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs with configuration-based loading

## Core Functionality

### Primary Purpose

The EstateModule serves as the communication hub for estate-level operations across multiple regions within an OpenSimulator grid. It provides centralized management of estate settings, coordinates estate-wide messaging, handles administrative teleport operations, and integrates with external estate management services through REST API connectors.

### Key Features

1. **Cross-Region Estate Communication**: Coordinates estate operations across multiple regions
2. **Estate Information Synchronization**: Synchronizes estate and region settings updates
3. **Estate Messaging**: Broadcasts messages to all users within an estate
4. **Administrative Teleport Management**: Handles estate owner teleport operations (kick and teleport home)
5. **HTTP Connector Integration**: REST API communication with external estate services
6. **Real-time Event Processing**: Responds to estate and region information changes
7. **Token-based Authentication**: Secure communication with external services
8. **Multi-Region Coordination**: Manages operations across all regions in an estate

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for shared modules
1. Initialise(IConfigSource) - Configuration loading, HTTP server setup, and connector initialization
2. AddRegion(Scene) - Register scenes for estate operations
3. RegionLoaded(Scene) - Subscribe to estate module events
4. PostInitialise() - Post-initialization setup (no-op)
5. RemoveRegion(Scene) - Scene cleanup and event unsubscription
6. Close() - Module cleanup (no-op)
```

### Configuration Architecture

```csharp
public void Initialise(IConfigSource config)
{
    uint port = MainServer.Instance.Port;

    IConfig estateConfig = config.Configs["Estates"];
    if (estateConfig != null)
    {
        if (estateConfig.GetString("EstateCommunicationsHandler", Name) == Name)
            m_enabled = true;
        else
            return;

        port = (uint)estateConfig.GetInt("Port", 0);
        token = estateConfig.GetString("Token", token);
    }
    else
    {
        m_enabled = true;  // Default enabled if no config
    }

    m_EstateConnector = new EstateConnector(this, token, port);

    if(port == 0)
         port = MainServer.Instance.Port;

    // Setup HTTP request handler
    IHttpServer server = MainServer.GetHttpServer(port);
    server.AddSimpleStreamHandler(new EstateSimpleRequestHandler(this, token));
}
```

## Configuration System

### Module Configuration

#### Optional Configuration ([Estates] section)
- **EstateCommunicationsHandler**: `string` - Handler name for estate communications (default: "EstateModule")
- **Port**: `int` - HTTP port for estate communication services (default: MainServer port)
- **Token**: `string` - Authentication token for estate service communication

### Configuration Examples

#### Basic Configuration (Default Enabled)
```ini
# No configuration required - module defaults to enabled
# Uses MainServer port and default authentication token
```

#### Custom Port and Authentication
```ini
[Estates]
EstateCommunicationsHandler = EstateModule
Port = 9000
Token = my-secure-estate-token-123
```

#### Alternative Handler (Disabled)
```ini
[Estates]
EstateCommunicationsHandler = CustomEstateHandler
# This will disable the EstateModule in favor of alternative handler
```

#### External Service Integration
```ini
[Estates]
EstateCommunicationsHandler = EstateModule
Port = 8080
Token = grid-estate-service-token
# Configure for external estate management service integration
```

## Estate Communication Architecture

### EstateConnector Integration

```csharp
protected EstateConnector m_EstateConnector;

// Connector initialization
m_EstateConnector = new EstateConnector(this, token, port);
```

### HTTP Request Handler Setup

```csharp
IHttpServer server = MainServer.GetHttpServer(port);
server.AddSimpleStreamHandler(new EstateSimpleRequestHandler(this, token));
```

The module establishes both outbound (EstateConnector) and inbound (EstateSimpleRequestHandler) communication channels for estate operations.

## Event Management and Estate Operations

### Estate Module Event Subscription

```csharp
public void RegionLoaded(Scene scene)
{
    if (!m_enabled) return;

    IEstateModule em = scene.RequestModuleInterface<IEstateModule>();

    em.OnRegionInfoChange += OnRegionInfoChange;
    em.OnEstateInfoChange += OnEstateInfoChange;
    em.OnEstateMessage += OnEstateMessage;
    em.OnEstateTeleportOneUserHomeRequest += OnEstateTeleportOneUserHomeRequest;
    em.OnEstateTeleportAllUsersHomeRequest += OnEstateTeleportAllUsersHomeRequest;
}
```

### Region Information Change Handling

```csharp
private void OnRegionInfoChange(UUID RegionID)
{
    Scene s = FindScene(RegionID);
    if (s == null) return;

    if (!m_InInfoUpdate)
        m_EstateConnector.SendUpdateCovenant(s.RegionInfo.EstateSettings.EstateID,
                                           s.RegionInfo.RegionSettings.Covenant);
}
```

### Estate Information Change Handling

```csharp
private void OnEstateInfoChange(UUID RegionID)
{
    Scene s = FindScene(RegionID);
    if (s == null) return;

    if (!m_InInfoUpdate)
        m_EstateConnector.SendUpdateEstate(s.RegionInfo.EstateSettings.EstateID);
}
```

### Estate Messaging System

```csharp
private void OnEstateMessage(UUID RegionID, UUID FromID, string FromName, string Message)
{
    Scene senderScenes = FindScene(RegionID);
    if (senderScenes == null) return;

    uint estateID = senderScenes.RegionInfo.EstateSettings.EstateID;

    // Broadcast to all regions in the estate
    foreach (Scene s in m_Scenes)
    {
        if (s.RegionInfo.EstateSettings.EstateID == estateID)
        {
            IDialogModule dm = s.RequestModuleInterface<IDialogModule>();
            if (dm != null)
            {
                dm.SendNotificationToUsersInRegion(FromID, FromName, Message);
            }
        }
    }

    // Forward to external estate service
    if (!m_InInfoUpdate)
        m_EstateConnector.SendEstateMessage(estateID, FromID, FromName, Message);
}
```

## Administrative Teleport Operations

### Single User Teleport/Kick

```csharp
private void OnEstateTeleportOneUserHomeRequest(IClientAPI client, UUID invoice, UUID senderID, UUID prey, bool kick)
{
    if (prey.IsZero()) return;

    Scene scene = client.Scene as Scene;
    if (scene == null) return;

    // Verify permissions
    if (!scene.Permissions.CanIssueEstateCommand(client.AgentId, false))
        return;

    uint estateID = scene.RegionInfo.EstateSettings.EstateID;

    // Search all estate regions for the target user
    foreach (Scene s in m_Scenes)
    {
        if (s.RegionInfo.EstateSettings.EstateID != estateID)
            continue;

        ScenePresence p = scene.GetScenePresence(prey);
        if (p != null && !p.IsChildAgent && !p.IsDeleted && !p.IsInTransit)
        {
            if (kick)
            {
                p.ControllingClient.Kick("You have been kicked out");
                s.CloseAgent(p.UUID, false);
            }
            else
            {
                p.ControllingClient.SendTeleportStart(16);
                if (!s.TeleportClientHome(prey, client))
                {
                    p.ControllingClient.Kick("You were teleported home by the region owner, but the TP failed");
                    s.CloseAgent(p.UUID, false);
                }
            }
            return;
        }
    }

    // If user not found locally, forward to estate service
    m_EstateConnector.SendTeleportHomeOneUser(estateID, prey);
}
```

### Estate-Wide User Teleport

```csharp
private void OnEstateTeleportAllUsersHomeRequest(IClientAPI client, UUID invoice, UUID senderID)
{
    Scene scene = client.Scene as Scene;
    if(scene == null) return;

    // Verify permissions
    if (!scene.Permissions.CanIssueEstateCommand(client.AgentId, false))
        return;

    uint estateID = scene.RegionInfo.EstateSettings.EstateID;

    // Process all regions in the estate
    foreach (Scene s in m_Scenes)
    {
        if (s.RegionInfo.EstateSettings.EstateID != estateID)
            continue;

        // Teleport all users in the region
        scene.ForEachScenePresence(delegate(ScenePresence p)
        {
            if (p != null && !p.IsChildAgent && !p.IsDeleted && !p.IsInTransit)
            {
                p.ControllingClient.SendTeleportStart(16);
                scene.TeleportClientHome(p.ControllingClient.AgentId, client);
                if (!s.TeleportClientHome(p.ControllingClient.AgentId, client))
                {
                    p.ControllingClient.Kick("You were teleported home by the region owner, but the TP failed - you have been logged out.");
                    s.CloseAgent(p.UUID, false);
                }
            }
        });
    }

    // Notify estate service
    m_EstateConnector.SendTeleportHomeAllUsers(estateID);
}
```

## Scene Management and Discovery

### Scene Registration

```csharp
public void AddRegion(Scene scene)
{
    if (!m_enabled) return;

    lock (m_Scenes)
        m_Scenes.Add(scene);
}

public void RemoveRegion(Scene scene)
{
    if (!m_enabled) return;

    lock (m_Scenes)
        m_Scenes.Remove(scene);
}
```

### Scene Discovery by Region ID

```csharp
private Scene FindScene(UUID RegionID)
{
    foreach (Scene s in m_Scenes)
    {
        if (s.RegionInfo.RegionID.Equals(RegionID))
            return s;
    }
    return null;
}
```

### Multi-Scene Coordination

The module maintains a list of all scenes and performs operations across all regions within the same estate:

```csharp
protected List<Scene> m_Scenes = new List<Scene>();

// Estate-wide operations
foreach (Scene s in m_Scenes)
{
    if (s.RegionInfo.EstateSettings.EstateID == estateID)
    {
        // Perform estate-wide operation
    }
}
```

## Update Coordination and Loop Prevention

### Update State Management

```csharp
protected bool m_InInfoUpdate = false;

public bool InInfoUpdate
{
    get { return m_InInfoUpdate; }
    set { m_InInfoUpdate = value; }
}
```

### Loop Prevention Logic

```csharp
if (!m_InInfoUpdate)
{
    m_EstateConnector.SendUpdateEstate(estateID);
    m_EstateConnector.SendEstateMessage(estateID, FromID, FromName, Message);
}
```

The `m_InInfoUpdate` flag prevents infinite update loops when external systems push updates back to the module.

## External Service Integration

### EstateConnector Operations

The module coordinates with external estate services through the EstateConnector:

```csharp
// Estate information updates
m_EstateConnector.SendUpdateEstate(s.RegionInfo.EstateSettings.EstateID);

// Covenant updates
m_EstateConnector.SendUpdateCovenant(s.RegionInfo.EstateSettings.EstateID,
                                   s.RegionInfo.RegionSettings.Covenant);

// Estate messaging
m_EstateConnector.SendEstateMessage(estateID, FromID, FromName, Message);

// Teleport operations
m_EstateConnector.SendTeleportHomeOneUser(estateID, prey);
m_EstateConnector.SendTeleportHomeAllUsers(estateID);
```

### HTTP Request Processing

The module establishes an HTTP endpoint through EstateSimpleRequestHandler to receive external requests:

```csharp
server.AddSimpleStreamHandler(new EstateSimpleRequestHandler(this, token));
```

## Permission and Security System

### Estate Command Permissions

```csharp
if (!scene.Permissions.CanIssueEstateCommand(client.AgentId, false))
    return;
```

All administrative operations verify that the requesting user has estate command permissions.

### Token-based Authentication

```csharp
private string token = "7db8eh2gvgg45jj";  // Default token

// Token configuration
token = estateConfig.GetString("Token", token);

// Used in connector and request handler initialization
m_EstateConnector = new EstateConnector(this, token, port);
server.AddSimpleStreamHandler(new EstateSimpleRequestHandler(this, token));
```

### Agent State Validation

```csharp
if (p != null && !p.IsChildAgent && !p.IsDeleted && !p.IsInTransit)
{
    // Safe to perform teleport operations
}
```

The module validates agent states before performing teleport or kick operations.

## Error Handling and Edge Cases

### Null Reference Protection

```csharp
Scene s = FindScene(RegionID);
if (s == null) return;

Scene scene = client.Scene as Scene;
if(scene == null) return;

IDialogModule dm = s.RequestModuleInterface<IDialogModule>();
if (dm != null)
{
    // Safe to use dialog module
}
```

### UUID Validation

```csharp
if (prey.IsZero()) return;
```

### Estate ID Matching

```csharp
if (s.RegionInfo.EstateSettings.EstateID != estateID)
    continue;
```

### Teleport Failure Handling

```csharp
if (!s.TeleportClientHome(prey, client))
{
    p.ControllingClient.Kick("You were teleported home by the region owner, but the TP failed");
    s.CloseAgent(p.UUID, false);
}
```

## Performance Considerations

### Efficient Scene Discovery

The module uses direct UUID comparison for fast scene lookup:

```csharp
if (s.RegionInfo.RegionID.Equals(RegionID))
    return s;
```

### Estate-Scoped Operations

Operations are limited to regions within the same estate to minimize processing overhead:

```csharp
if (s.RegionInfo.EstateSettings.EstateID == estateID)
{
    // Process only estate regions
}
```

### Thread Safety

Scene list operations are protected with locks:

```csharp
lock (m_Scenes)
{
    m_Scenes.Add(scene);
    m_Scenes.Remove(scene);
}
```

### Event-Driven Architecture

The module uses event-driven processing to respond only to relevant changes, minimizing unnecessary operations.

## Network Communication

### HTTP Server Integration

```csharp
uint port = MainServer.Instance.Port;  // Default to main server port
if(port == 0)
     port = MainServer.Instance.Port;

IHttpServer server = MainServer.GetHttpServer(port);
server.AddSimpleStreamHandler(new EstateSimpleRequestHandler(this, token));
```

### Port Configuration

The module supports custom port configuration for estate communications:

```csharp
port = (uint)estateConfig.GetInt("Port", 0);
```

### Bidirectional Communication

- **Outbound**: EstateConnector for pushing updates to external services
- **Inbound**: EstateSimpleRequestHandler for receiving external commands

## Integration Points

### Estate Management Module Integration

The module integrates directly with the core EstateManagementModule through event subscriptions:

```csharp
IEstateModule em = scene.RequestModuleInterface<IEstateModule>();
em.OnRegionInfoChange += OnRegionInfoChange;
em.OnEstateInfoChange += OnEstateInfoChange;
// Additional event subscriptions
```

### Dialog Module Integration

```csharp
IDialogModule dm = s.RequestModuleInterface<IDialogModule>();
if (dm != null)
{
    dm.SendNotificationToUsersInRegion(FromID, FromName, Message);
}
```

### Scene Permission System Integration

```csharp
if (!scene.Permissions.CanIssueEstateCommand(client.AgentId, false))
    return;
```

### MainServer Integration

```csharp
uint port = MainServer.Instance.Port;
IHttpServer server = MainServer.GetHttpServer(port);
```

## Use Cases and Applications

### Multi-Region Estate Management

- **Centralized Control**: Single point of control for estate-wide operations
- **Cross-Region Messaging**: Broadcast messages to all users within an estate
- **Coordinated Administration**: Synchronized administrative actions across regions

### External Estate Services

- **Grid Management**: Integration with external grid management systems
- **Web Interfaces**: Support for web-based estate management tools
- **API Integration**: REST API connectivity for external applications

### Administrative Operations

- **Bulk User Management**: Estate-wide teleport and kick operations
- **Policy Enforcement**: Automated enforcement of estate-wide policies
- **Event Coordination**: Synchronized events across estate regions

## Dependencies

### Core Framework Dependencies

- `OpenSim.Framework` - Core data structures and utilities
- `OpenSim.Region.Framework.Interfaces` - Module interface contracts
- `OpenSim.Region.Framework.Scenes` - Scene management and events
- `OpenSim.Services.Interfaces` - Service interface definitions

### System Dependencies

- `System.Collections` - Collection management for scene lists
- `log4net` - Logging framework for debug and error messages
- `Nini.Config` - Configuration management
- `Nwc.XmlRpc` - XML-RPC communication support

### Server Dependencies

- `OpenSim.Server.Base` - Server base functionality
- `OpenSim.Framework.Servers` - Server framework components
- `OpenSim.Framework.Servers.HttpServer` - HTTP server functionality

### Service Dependencies

- Estate Management Module for estate events and operations
- Dialog Module for user notification functionality
- Scene Permission System for authorization

## Troubleshooting

### Common Configuration Issues

1. **Module Not Loading**
   - Check that `[Estates]` section has correct `EstateCommunicationsHandler` setting
   - Verify module is not disabled by alternative handler configuration
   - Review startup logs for initialization errors

2. **HTTP Communication Failures**
   - Verify port configuration is not conflicting with other services
   - Check that HTTP server is properly initialized
   - Ensure token authentication is correctly configured

3. **Estate Events Not Processing**
   - Verify Estate Management Module is loaded and functional
   - Check that regions have valid estate settings
   - Ensure event subscriptions are properly established

### Common Runtime Issues

1. **Cross-Region Operations Not Working**
   - Check that all regions belong to the same estate
   - Verify scene registration is working correctly
   - Ensure estate ID matching is functioning properly

2. **Teleport Operations Failing**
   - Verify users have proper estate command permissions
   - Check that target users exist and are in valid states
   - Ensure teleport fallback mechanisms are working

3. **External Service Communication Issues**
   - Verify EstateConnector configuration and connectivity
   - Check authentication token validity
   - Monitor network connectivity to external services

### Debug Configuration

```ini
[Estates]
EstateCommunicationsHandler = EstateModule
Port = 9000
Token = debug-token-123

# Enable debug logging
[Logging]
LogLevel = DEBUG

# Monitor specific module activity
[Log4Net]
logger.OpenSim.Region.CoreModules.World.Estate.EstateModule = DEBUG
```

### Log Analysis

Monitor module operation through log messages:
```
[EstateModule]: Loading EstateModule for estate communications, region info updates, and estate management
[EstateModule]: Estate message from TestUser: "Welcome to our estate!"
[EstateModule]: Processing teleport home request for user 12345678-1234-1234-1234-123456789012
```

### Network Testing

Test HTTP endpoint functionality:
```bash
# Test estate communication endpoint
curl -X POST http://localhost:9000/estate \
     -H "Authorization: Bearer debug-token-123" \
     -d "{'action': 'test'}"
```

## Deployment Considerations

### Grid Architecture Planning

- **Estate Distribution**: Plan distribution of estate regions across grid nodes
- **Communication Topology**: Design network topology for estate communications
- **Load Distribution**: Balance estate operations across multiple servers

### Performance Planning

- **User Volume**: Consider maximum users per estate for teleport operations
- **Message Volume**: Plan for estate-wide messaging capacity
- **Network Bandwidth**: Factor in cross-region communication overhead

### Security Planning

- **Token Management**: Implement secure token distribution and rotation
- **Permission Validation**: Ensure proper estate command permission enforcement
- **Network Security**: Secure HTTP communications between estate services

## Future Enhancement Opportunities

### Advanced Features

- **Estate Analytics**: Usage tracking and reporting for estate operations
- **Policy Automation**: Automated enforcement of estate-wide policies
- **Event Scheduling**: Scheduled estate-wide events and operations
- **User Management**: Enhanced bulk user management capabilities

### Performance Improvements

- **Connection Pooling**: Reuse HTTP connections for estate communications
- **Batch Operations**: Bulk processing of estate operations
- **Caching**: Cache estate configuration and user state information
- **Asynchronous Processing**: Non-blocking estate operation processing

### Integration Enhancements

- **Database Integration**: Direct database connectivity for estate management
- **Web Dashboard**: Real-time web interface for estate monitoring
- **API Extensions**: Extended REST API for advanced estate operations
- **Mobile Support**: Mobile application support for estate management

## Conclusion

The EstateModule provides essential estate communication and management capabilities for OpenSimulator grids requiring coordinated multi-region estate operations. Its integration with HTTP services, comprehensive event handling, and robust permission system make it suitable for both small private estates and large commercial grid deployments. The module's configuration-based loading, external service integration, and comprehensive error handling ensure reliable operation in complex grid environments.