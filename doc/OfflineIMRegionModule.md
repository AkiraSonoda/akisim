# OfflineIMRegionModule Technical Documentation

## Overview

The OfflineIMRegionModule is a shared region module that provides offline instant messaging capabilities for OpenSimulator. It enables avatars to receive instant messages even when they are not online, storing messages for later delivery when the recipient logs in. The module supports both local database storage and remote service-based storage through connectors.

## Module Classification

- **Type**: ISharedRegionModule, IOfflineIMService
- **Namespace**: OpenSim.OfflineIM
- **Assembly**: OpenSim.Addons.OfflineIM
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs with configuration-based loading

## Core Functionality

### Primary Purpose

The OfflineIMRegionModule serves as the central hub for offline instant message management in OpenSimulator grids. It intercepts instant messages intended for offline users, stores them persistently, and delivers them when recipients come online. The module provides both local storage capabilities and integration with external IM services through configurable connectors.

### Key Features

1. **Offline Message Storage**: Persistent storage of instant messages for offline recipients
2. **Online Status Detection**: Real-time monitoring of avatar online/offline status
3. **Message Delivery**: Automatic delivery of stored messages upon user login
4. **Service Connector Support**: Integration with external IM services via HTTP connectors
5. **Local Database Storage**: Direct database storage for standalone deployments
6. **Message Limiting**: Configurable limits on stored messages per user
7. **Cross-Region Support**: Shared module design for multi-region grids
8. **Grid Service Integration**: Seamless integration with grid-based user services

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for shared modules
1. Initialise(IConfigSource) - Configuration loading and service setup
2. AddRegion(Scene) - Register module interface and scene association
3. RegionLoaded(Scene) - Final region-specific setup
4. PostInitialise() - Post-initialization setup (no-op)
5. RemoveRegion(Scene) - Scene cleanup and event unsubscription
6. Close() - Module cleanup (no-op)
```

### Interface Implementation

The module implements two key interfaces:

#### ISharedRegionModule
Provides shared functionality across multiple regions in a grid.

#### IOfflineIMService
Defines the offline instant messaging service contract:

```csharp
public interface IOfflineIMService
{
    bool StoreMessage(GridInstantMessage im, out string reason);
    List<GridInstantMessage> GetMessages(UUID principalID, out string reason);
    bool DeleteMessages(UUID userID);
}
```

### Configuration Architecture

```csharp
public void Initialise(IConfigSource config)
{
    IConfig cnf = config.Configs["Messaging"];
    if (cnf == null) return;

    if (cnf.GetString("OfflineMessageModule", "") != "Offline Message Module V2") return;

    m_Enabled = true;
    string connectorName = cnf.GetString("OfflineMessageConnector", "OpenSim.Addons.OfflineIM.dll:OpenSim.OfflineIM.OfflineIMServiceConnector");

    // Load connector using reflection
    m_Connector = LoadConnector(cnf, connectorName);
    m_MaxRetrieval = cnf.GetInt("MaxRetrieval", 20);
}
```

## Configuration System

### Module Configuration

#### Required Configuration ([Messaging] section)
- **OfflineMessageModule**: `string` - Must be "Offline Message Module V2" to enable the module
- **OfflineMessageConnector**: `string` - Connector specification for message storage

#### Optional Configuration Parameters
- **MaxRetrieval**: `int` - Maximum number of messages to retrieve per request (default: 20)

### Configuration Examples

#### Local Database Storage
```ini
[Messaging]
OfflineMessageModule = Offline Message Module V2
OfflineMessageConnector = OpenSim.Addons.OfflineIM.dll:OpenSim.OfflineIM.OfflineIMServiceConnector

# Local database configuration
[OfflineIM]
StorageProvider = OpenSim.Data.MySQL.dll:MySQLOfflineIMData
ConnectionString = "Data Source=localhost;Database=opensim;User ID=opensim;Password=secret;"
```

#### Remote Service Integration
```ini
[Messaging]
OfflineMessageModule = Offline Message Module V2
OfflineMessageConnector = OpenSim.Addons.OfflineIM.dll:OpenSim.OfflineIM.OfflineIMServiceRemoteConnector

# Remote service configuration
[OfflineIM]
OfflineMessageURL = http://robust-server:8003/offlineIM
MaxRetrieval = 30
```

#### Grid Service Integration
```ini
[Messaging]
OfflineMessageModule = Offline Message Module V2
OfflineMessageConnector = OpenSim.Services.Connectors.dll:OpenSim.Services.Connectors.InstantMessage.OfflineIMServiceConnector

# Grid service configuration
[OfflineIM]
LocalServiceModule = OpenSim.Services.OfflineIMService.dll:OpenSim.Services.OfflineIMService.OfflineIMService
StorageProvider = OpenSim.Data.MySQL.dll:MySQLOfflineIMData
ConnectionString = "Data Source=localhost;Database=opensim;User ID=opensim;Password=secret;"
```

## Message Processing Architecture

### Instant Message Interception

```csharp
private void OnIncomingInstantMessage(GridInstantMessage im)
{
    if (im.dialog != (byte)InstantMessageDialog.MessageFromAgent &&
        im.dialog != (byte)InstantMessageDialog.MessageFromObject)
        return;

    Scene scene = FindScene(new UUID(im.fromAgentID));
    if (scene == null) scene = m_SceneList[0];

    bool online = false;
    if (scene.Services.PresenceService != null)
        online = scene.Services.PresenceService.GetAgent(new UUID(im.toAgentID)) != null;

    if (!online)
    {
        string reason;
        bool success = m_Connector.StoreMessage(im, out reason);
        if (!success)
            m_log.ErrorFormat("[OFFLINE MESSAGING]: Unable to save message from {0} to {1}. Reason: {2}", im.fromAgentName, im.toAgentName, reason);
    }
}
```

### Message Retrieval and Delivery

```csharp
private void RetrieveInstantMessages(IClientAPI client)
{
    if (m_Connector == null) return;

    string reason;
    List<GridInstantMessage> msglist = m_Connector.GetMessages(client.AgentId, out reason);

    if (msglist != null)
    {
        foreach (GridInstantMessage im in msglist)
        {
            if (im.dialog == (byte)InstantMessageDialog.InventoryOffered)
                client.SendInstantMessage(im);
            else
                client.SendInstantMessage(im);
        }

        m_Connector.DeleteMessages(client.AgentId);
    }
}
```

## Connector Architecture

### Connector Loading System

```csharp
private IOfflineIMService LoadConnector(IConfig config, string connectorName)
{
    string[] parts = connectorName.Split(':');
    if (parts.Length != 2) return null;

    string filename = parts[0];
    string classname = parts[1];

    try
    {
        Assembly pluginAssembly = Assembly.LoadFrom(filename);
        Type pluginType = pluginAssembly.GetType(classname);
        IOfflineIMService connector = (IOfflineIMService)Activator.CreateInstance(pluginType, new object[] { config });
        return connector;
    }
    catch (Exception e)
    {
        m_log.ErrorFormat("[OFFLINE MESSAGING]: Exception loading connector {0}: {1}", connectorName, e.Message);
        return null;
    }
}
```

### Supported Connector Types

#### Local Database Connector
- **Purpose**: Direct database storage for standalone deployments
- **Class**: `OpenSim.OfflineIM.OfflineIMServiceConnector`
- **Storage**: Uses OpenSim's data abstraction layer

#### Remote Service Connector
- **Purpose**: HTTP-based communication with external IM services
- **Class**: `OpenSim.OfflineIM.OfflineIMServiceRemoteConnector`
- **Protocol**: REST API with JSON serialization

#### Grid Service Connector
- **Purpose**: Integration with OpenSim's grid service architecture
- **Class**: `OpenSim.Services.Connectors.InstantMessage.OfflineIMServiceConnector`
- **Transport**: Internal service communication

## Message Storage Operations

### Message Storage Process

```csharp
public bool StoreMessage(GridInstantMessage im, out string reason)
{
    reason = string.Empty;

    // Validate message content
    if (string.IsNullOrEmpty(im.message))
    {
        reason = "Message content is empty";
        return false;
    }

    // Check message limits per user
    int messageCount = GetMessageCount(new UUID(im.toAgentID));
    if (messageCount >= m_MaxRetrieval)
    {
        reason = "Message storage limit exceeded";
        return false;
    }

    // Store message in configured backend
    return m_Data.StoreIM(im);
}
```

### Message Retrieval Process

```csharp
public List<GridInstantMessage> GetMessages(UUID principalID, out string reason)
{
    reason = string.Empty;

    try
    {
        List<GridInstantMessage> messages = m_Data.Get("PrincipalID", principalID.ToString());

        // Apply retrieval limits
        if (messages.Count > m_MaxRetrieval)
            messages = messages.Take(m_MaxRetrieval).ToList();

        return messages;
    }
    catch (Exception e)
    {
        reason = e.Message;
        return null;
    }
}
```

## Event Management and Lifecycle

### Scene Integration

```csharp
public void AddRegion(Scene scene)
{
    if (!m_Enabled) return;

    lock (m_SceneList)
    {
        m_SceneList.Add(scene);
        scene.RegisterModuleInterface<IOfflineIMService>(this);
        scene.EventManager.OnIncomingInstantMessage += OnIncomingInstantMessage;
    }
}

public void RegionLoaded(Scene scene)
{
    if (!m_Enabled) return;

    if (m_Connector == null)
    {
        m_log.Error("[OFFLINE MESSAGING]: No connector configured. Module disabled.");
        m_Enabled = false;
        return;
    }

    scene.EventManager.OnNewClient += OnNewClient;
}
```

### Client Event Handling

```csharp
private void OnNewClient(IClientAPI client)
{
    client.OnRetrieveInstantMessages += RetrieveInstantMessages;
    client.OnMuteListRequest += OnMuteListRequest;
}

private void OnClientClosed(UUID agentID, Scene scene)
{
    // Cleanup any pending operations for the disconnected client
    // No specific cleanup needed for offline IM
}
```

## Data Structures and Serialization

### GridInstantMessage Structure

```csharp
public class GridInstantMessage
{
    public Guid fromAgentID;        // Sender's UUID
    public string fromAgentName;    // Sender's display name
    public Guid toAgentID;          // Recipient's UUID
    public byte dialog;             // Message type (dialog ID)
    public bool fromGroup;          // True if sent from a group
    public string message;          // Message content
    public Guid imSessionID;        // IM session identifier
    public bool offline;            // True if recipient was offline
    public Vector3 Position;        // Sender's position when sent
    public byte[] binaryBucket;     // Additional binary data
    public uint ParentEstateID;     // Estate ID where message originated
    public Guid RegionID;           // Region UUID where message originated
    public uint timestamp;          // Unix timestamp of message
}
```

### Message Dialog Types

```csharp
public enum InstantMessageDialog : byte
{
    MessageFromAgent = 0,           // Standard IM from avatar
    MessageFromObject = 19,         // IM from scripted object
    InventoryOffered = 4,          // Inventory item offer
    InventoryAccepted = 5,         // Inventory offer accepted
    InventoryDeclined = 6,         // Inventory offer declined
    GroupInvitation = 3,           // Group invitation
    FriendshipOffered = 39,        // Friendship offer
    // Additional dialog types...
}
```

## Performance Considerations

### Message Limiting and Throttling

```csharp
// Configurable message retrieval limits
private int m_MaxRetrieval = 20;

// Apply limits during retrieval
public List<GridInstantMessage> GetMessages(UUID principalID, out string reason)
{
    List<GridInstantMessage> messages = m_Data.Get("PrincipalID", principalID.ToString());

    if (messages.Count > m_MaxRetrieval)
    {
        messages = messages.Take(m_MaxRetrieval).ToList();
        m_log.WarnFormat("[OFFLINE MESSAGING]: User {0} has more than {1} offline messages. Only retrieving first {1}.",
                        principalID, m_MaxRetrieval);
    }

    return messages;
}
```

### Database Optimization

- **Indexed Queries**: Queries use PrincipalID index for efficient message retrieval
- **Batch Operations**: Messages are retrieved and deleted in batches to minimize database calls
- **Connection Pooling**: Leverages OpenSim's database connection pooling for optimal performance

### Memory Management

- **Message Cleanup**: Automatic deletion of retrieved messages to prevent accumulation
- **Connector Caching**: Connector instances are cached per module instance
- **Scene-Shared Resources**: Single module instance shared across all regions

## Security Considerations

### Access Control

- **UUID-based Access**: Messages are stored and retrieved using agent UUIDs only
- **No Cross-User Access**: No mechanisms allow users to access other users' messages
- **Service Authentication**: Remote connectors support authentication for secure communication

### Data Protection

- **Message Validation**: Input validation prevents malformed messages from being stored
- **SQL Injection Prevention**: Parameterized queries prevent SQL injection attacks
- **Transport Security**: HTTPS support for remote connector communications

### Privacy Considerations

- **Message Encryption**: Messages stored in plaintext (encryption handled at transport layer)
- **Automatic Cleanup**: Messages are deleted after delivery to prevent long-term storage
- **No Message Logging**: Module does not log message content for privacy

## Error Handling and Resilience

### Connector Failure Handling

```csharp
private void OnIncomingInstantMessage(GridInstantMessage im)
{
    if (m_Connector == null)
    {
        m_log.Warn("[OFFLINE MESSAGING]: No connector available, message lost");
        return;
    }

    try
    {
        string reason;
        bool success = m_Connector.StoreMessage(im, out reason);
        if (!success)
        {
            m_log.ErrorFormat("[OFFLINE MESSAGING]: Failed to store message: {0}", reason);
        }
    }
    catch (Exception e)
    {
        m_log.ErrorFormat("[OFFLINE MESSAGING]: Exception storing message: {0}", e.Message);
    }
}
```

### Database Connection Handling

- **Connection Retry**: Database connectors implement automatic retry logic
- **Transaction Safety**: Database operations use transactions for consistency
- **Graceful Degradation**: Module continues operating even if some messages fail to store

### Service Availability

- **Remote Service Failover**: Multiple remote service URLs can be configured
- **Local Fallback**: Can fall back to local storage if remote services are unavailable
- **Health Checking**: Periodic health checks ensure service availability

## Integration Points

### Grid Services Integration

- **Presence Service**: Queries presence service to determine if users are online
- **User Account Service**: Validates user accounts for message recipients
- **Grid Service**: Integrates with grid-wide service architecture

### Client Protocol Integration

- **LoginService Integration**: Messages are delivered during the login process
- **Client Event Handling**: Responds to client requests for offline messages
- **Inventory Integration**: Handles inventory offers sent via offline messages

### Database Integration

- **Data Layer Abstraction**: Uses OpenSim's data abstraction layer for database independence
- **Migration Support**: Supports database schema migrations for upgrades
- **Multiple Database Support**: Works with MySQL, SQLite, and PostgreSQL

## Use Cases and Applications

### Standalone Deployments

- **Local Message Storage**: Direct database storage for single-server deployments
- **Personal Grids**: Offline messaging for small personal or family grids
- **Development Environment**: Local testing of offline messaging functionality

### Grid Deployments

- **Cross-Region Messaging**: Consistent offline messaging across multiple regions
- **Scalable Architecture**: Centralized message storage for large grid deployments
- **Service-Based Storage**: External service integration for enterprise deployments

### Commercial Applications

- **Business Communications**: Offline messaging for virtual business environments
- **Educational Platforms**: Message delivery for educational virtual worlds
- **Social Platforms**: Enhanced communication features for social virtual environments

## Dependencies

### Core Framework Dependencies

- `OpenSim.Framework` - Core data structures and utilities
- `OpenSim.Region.Framework.Interfaces` - Module interface contracts
- `OpenSim.Region.Framework.Scenes` - Scene management and events
- `OpenSim.Services.Interfaces` - Service interface definitions

### System Dependencies

- `System.Reflection` - Dynamic connector loading
- `System.Collections.Generic` - Collection management
- `log4net` - Logging framework
- `Nini.Config` - Configuration management

### Optional Dependencies

- Database providers (MySQL, SQLite, PostgreSQL)
- External service connectors
- Authentication frameworks for remote services

## Troubleshooting

### Common Configuration Issues

1. **Module Not Loading**
   - Verify `[Messaging]` section exists with correct `OfflineMessageModule` setting
   - Check that connector specification is valid and assembly is available
   - Review startup logs for connector loading errors

2. **Connector Loading Failures**
   - Ensure specified connector assembly exists and is accessible
   - Verify connector class name is correct and implements IOfflineIMService
   - Check assembly dependencies are satisfied

3. **Database Connection Issues**
   - Verify database connection string is correct
   - Ensure database server is accessible and credentials are valid
   - Check that required database tables exist

### Common Runtime Issues

1. **Messages Not Being Stored**
   - Check that users are actually offline when messages are sent
   - Verify connector is properly configured and functional
   - Review logs for storage operation failures

2. **Messages Not Being Delivered**
   - Ensure messages are being retrieved during login process
   - Check that client is requesting offline messages
   - Verify message deletion is working properly

3. **Performance Issues**
   - Monitor message retrieval limits and adjust MaxRetrieval setting
   - Check database performance and indexing
   - Review connector response times for remote services

### Debug Configuration

```ini
[Messaging]
OfflineMessageModule = Offline Message Module V2
OfflineMessageConnector = OpenSim.Addons.OfflineIM.dll:OpenSim.OfflineIM.OfflineIMServiceConnector

# Debug logging
[Logging]
LogLevel = DEBUG

# Reduced message limits for testing
[OfflineIM]
MaxRetrieval = 5
```

### Log Analysis

Monitor module operation through log messages:
```
[OFFLINE MESSAGING]: User 12345678-1234-1234-1234-123456789012 has 3 offline messages
[OFFLINE MESSAGING]: Successfully stored offline message from TestUser to OfflineUser
[OFFLINE MESSAGING]: Retrieved 3 offline messages for user 12345678-1234-1234-1234-123456789012
```

### Database Testing

Test database connectivity independently:
```sql
-- Check offline message storage
SELECT * FROM im_offline WHERE PrincipalID = '12345678-1234-1234-1234-123456789012';

-- Verify table structure
DESCRIBE im_offline;
```

## Deployment Considerations

### Grid Architecture Planning

- **Centralized vs Distributed**: Choose between centralized message storage or per-region storage
- **Service Dependencies**: Plan for presence service and user account service integration
- **Backup and Recovery**: Implement backup strategies for offline message data

### Performance Planning

- **Message Volume**: Estimate expected offline message volume for capacity planning
- **Database Sizing**: Size database storage based on message retention policies
- **Network Bandwidth**: Plan for message synchronization traffic in grid deployments

### Security Planning

- **Service Authentication**: Implement proper authentication for remote service access
- **Transport Encryption**: Use HTTPS for remote service communications
- **Access Controls**: Implement proper access controls for message storage services

## Future Enhancement Opportunities

### Advanced Features

- **Message Encryption**: End-to-end encryption for sensitive communications
- **Message Expiration**: Automatic expiration and cleanup of old messages
- **Message Priority**: Priority-based message delivery and storage
- **Rich Content Support**: Support for multimedia content in offline messages

### Performance Improvements

- **Message Caching**: Local caching of frequently accessed messages
- **Batch Processing**: Batch message operations for improved database performance
- **Compression**: Message compression for reduced storage requirements
- **Asynchronous Operations**: Fully asynchronous message storage and retrieval

### Integration Enhancements

- **Push Notifications**: Integration with mobile push notification services
- **Email Integration**: Email delivery for offline messages when users are away
- **Web Interface**: Web-based interface for message management
- **API Extensions**: REST API for external application integration

## Conclusion

The OfflineIMRegionModule provides essential offline instant messaging capabilities for OpenSimulator grids of all sizes. Its flexible connector architecture, comprehensive configuration options, and robust error handling make it suitable for both standalone and grid deployments. The module's integration with OpenSim's service architecture ensures seamless operation while maintaining high performance and reliability for critical communication features.