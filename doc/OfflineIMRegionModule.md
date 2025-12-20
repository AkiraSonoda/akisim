# Offline IM Region Module V2

## Overview

The Offline IM Region Module V2 provides offline instant messaging capabilities for Akisim, allowing users to receive messages that were sent while they were offline. This module stores undelivered instant messages and delivers them when the recipient logs in, ensuring no messages are lost due to user unavailability.

## Key Features

- **Message Storage** - Stores instant messages when recipients are offline
- **Automatic Delivery** - Delivers stored messages upon user login
- **Flexible Backend** - Supports both local and remote storage services
- **Group Message Support** - Optional offline delivery for group notices and invitations
- **Inventory Offers** - Special handling for inventory transfer messages
- **Delivery Notifications** - Notifies senders when messages are stored offline

## Architecture

### Module Information
- **Namespace**: `OpenSim.OfflineIM`
- **Assembly**: `OpenSim.Addons.OfflineIM.dll`
- **Module Name**: `Offline Message Module V2`
- **Implements**: `ISharedRegionModule`, `IOfflineIMService`
- **Loading**: Loaded via ModuleFactory using reflection (no Mono.Addins dependency)

### Dependencies
- **IMessageTransferModule** - Grid message delivery and undelivered message notifications
- **IOfflineIMService** - Backend storage service (local or remote)
- **Scene** - Region scene management and event handling
- **IClientAPI** - Client connection and message delivery

## Configuration

### Required Configuration
```ini
[Messaging]
OfflineMessageModule = "Offline Message Module V2"
OfflineMessageURL = ""  ; Empty for local storage, set URL for remote service
ForwardOfflineGroupMessages = true
```

### Configuration Parameters

| Parameter | Description | Default | Required |
|-----------|-------------|---------|----------|
| `OfflineMessageModule` | Module name to enable offline IM | "" | **Yes** |
| `OfflineMessageURL` | Remote service URL (empty for local storage) | "" | No |
| `ForwardOfflineGroupMessages` | Store offline group notices/invitations | true | No |

### Storage Backend Selection

#### Local Storage Mode
```ini
[Messaging]
OfflineMessageModule = "Offline Message Module V2"
OfflineMessageURL = ""
```
Uses `OfflineIMService` with local database storage.

#### Remote Storage Mode
```ini
[Messaging]
OfflineMessageModule = "Offline Message Module V2"
OfflineMessageURL = "http://robust.server:8003"
```
Uses `OfflineIMServiceRemoteConnector` to connect to remote Robust service.

### Important Notes
- The module name must exactly match `"Offline Message Module V2"` to be loaded by ModuleFactory
- Empty `OfflineMessageURL` enables local storage mode
- Group message forwarding can be disabled by setting `ForwardOfflineGroupMessages = false`
- The module requires `IMessageTransferModule` to be loaded and active

## Core Functionality

### Message Storage
The module intercepts undelivered messages via the `IMessageTransferModule.OnUndeliveredMessage` event and stores the following message types:

1. **MessageFromAgent** - Standard peer-to-peer instant messages
2. **MessageFromObject** - Messages from scripted objects
3. **GroupNotice** - Group announcements (if enabled)
4. **GroupInvitation** - Group join invitations (if enabled)
5. **InventoryOffered** - Inventory transfer requests

Messages are stored with complete metadata including:
- Sender and recipient UUIDs
- Message content and dialog type
- Timestamp and session information
- Offline status flag

### Message Retrieval
Upon client login, the module:

1. **Detects Login** - Listens to `IClientAPI.OnRetrieveInstantMessages` event
2. **Fetches Messages** - Queries backend service for stored messages
3. **Delivers Messages** - Sends each message to the client
4. **Routing Logic**:
   - **InventoryOffered** → Direct delivery via `SendInstantMessage()`
   - **Other Types** → Scene event system via `TriggerIncomingInstantMessage()`

### Delivery Notifications
When a message is stored offline, the sender receives an automatic notification:

```
"User is not logged in. Message saved."
```

Or if storage fails:

```
"User is not logged in. Message not saved: [reason]"
```

Notifications are sent only for `MessageFromAgent` type messages.

## Performance Characteristics

### Storage Performance
- **Asynchronous Storage** - Message storage does not block message transfer
- **Database Backed** - Uses optimized database queries for storage/retrieval
- **Batch Retrieval** - All messages for a user fetched in single query

### Delivery Performance
- **Login-Time Delivery** - All messages delivered during login sequence
- **Event-Driven** - Uses scene event system for proper module coordination
- **Minimal Latency** - Direct database access for local storage mode

### Scalability Features
- **Remote Service Support** - Can offload storage to dedicated Robust service
- **Per-Region Isolation** - Each region manages its own message handling
- **Shared Backend** - Multiple regions can share remote storage service

## API Reference

### Public Methods

#### `GetMessages(UUID principalID)`
Retrieves all stored offline messages for the specified user.

**Parameters:**
- `principalID` - UUID of the user

**Returns:** `List<GridInstantMessage>` - List of stored messages

#### `StoreMessage(GridInstantMessage im, out string reason)`
Stores an instant message for offline delivery.

**Parameters:**
- `im` - The instant message to store
- `reason` - Output parameter with error reason if storage fails

**Returns:** `bool` - True if message stored successfully

#### `DeleteMessages(UUID userID)`
Deletes all stored messages for the specified user.

**Parameters:**
- `userID` - UUID of the user

**Note:** Messages are typically deleted after successful delivery.

### Events Handled
- `OnNewClient` - Registers message retrieval handler for new clients
- `OnUndeliveredMessage` - Captures undelivered messages for storage
- `OnRetrieveInstantMessages` - Triggers message delivery on client login

## Module Lifecycle

### Initialization Phase
1. **Config Check** - Validates `[Messaging]` section exists
2. **Name Validation** - Confirms `OfflineMessageModule = "Offline Message Module V2"`
3. **Backend Selection** - Creates local or remote service connector
4. **Enable Flag** - Sets `m_Enabled = true` if configuration valid

### Region Integration
1. **AddRegion** - Registers `IOfflineIMService` interface and event handlers
2. **RegionLoaded** - Validates `IMessageTransferModule` availability
3. **RemoveRegion** - Unregisters event handlers and cleans up

### Loading via ModuleFactory
The module is loaded using reflection in `ModuleFactory.cs` (line 1107):

```csharp
// Try to load OfflineIMRegionModule using reflection to avoid hard dependency
var offlineIMModuleInstance = LoadOfflineIMModuleV2();
if (offlineIMModuleInstance != null)
{
    yield return offlineIMModuleInstance;
}
```

This approach:
- Eliminates Mono.Addins dependency
- Allows optional deployment (module can be missing)
- Enables runtime assembly loading
- Supports dynamic configuration-based loading

## Message Flow Diagrams

### Outbound Message Flow (Storing Offline)
```
Client A sends IM → IMessageTransferModule
                          ↓
                    Recipient offline?
                          ↓
                    OnUndeliveredMessage event
                          ↓
                    OfflineIMRegionModule.UndeliveredMessage()
                          ↓
                    Filter by message type
                          ↓
                    StoreMessage(im, out reason)
                          ↓
                    Database/Remote Storage
                          ↓
                    Send notification to Client A
```

### Inbound Message Flow (Delivery on Login)
```
Client B logs in → OnNewClient event
                          ↓
                    Register OnRetrieveInstantMessages
                          ↓
                    Client requests messages
                          ↓
                    GetMessages(clientID)
                          ↓
                    Database/Remote Storage
                          ↓
                    For each message:
                      - InventoryOffered → Direct delivery
                      - Others → Scene event system
                          ↓
                    Message delivered to Client B
```

## Integration with Service Layer

### Local Service (OfflineIMService)
Located in `src/OpenSim.Addons.OfflineIM/Service/OfflineIMService.cs`

**Features:**
- Direct database access via `IOfflineIMData`
- Supports MySQL, SQLite, PostgreSQL
- Uses `GridInstantMessage` serialization utilities

### Remote Service (OfflineIMServiceRemoteConnector)
Located in `src/OpenSim.Addons.OfflineIM/Remote/OfflineIMServiceRemoteConnector.cs`

**Features:**
- HTTP-based communication with Robust service
- XML-RPC protocol for message serialization
- Optional BasicHttpAuthentication support
- Handles GET (retrieve), STORE, and DELETE operations

## Debug and Logging

### Log Prefixes
- `[OfflineIM.V2]` - Main module operations
- `[OfflineIM.V2.RemoteConnector]` - Remote service connector

### Key Log Messages

**Initialization:**
```
[OfflineIM.V2]: Offline messages enabled by Offline Message Module V2
```

**Message Retrieval:**
```
[OfflineIM.V2]: Retrieving stored messages for <UUID>
[OfflineIM.V2]: WARNING null message list.
```

**Dependency Issues:**
```
[OfflineIM.V2]: No message transfer module is enabled. Disabling offline messages
```

**Remote Connector:**
```
[OfflineIM.V2.RemoteConnector]: Offline IM server at http://example.com:8003 with auth BasicHttpAuthentication
[OfflineIM.V2.RemoteConnector]: GetMessages for <UUID> failed: <reason>
```

## Troubleshooting

### Common Issues

1. **Module not loading**
   - Verify `OfflineMessageModule = "Offline Message Module V2"` (exact match)
   - Check that `OpenSim.Addons.OfflineIM.dll` is in bin directory
   - Review startup logs for ModuleFactory loading messages

2. **Messages not stored**
   - Confirm `IMessageTransferModule` is loaded
   - Check message type is in supported list
   - Verify database connectivity (local mode) or service URL (remote mode)
   - Review `ForwardOfflineGroupMessages` setting for group messages

3. **Messages not delivered**
   - Check client triggers `OnRetrieveInstantMessages` event
   - Verify messages exist in storage (query database directly)
   - Review logs for retrieval errors

4. **Remote service connection failures**
   - Validate `OfflineMessageURL` is correct and reachable
   - Check authentication configuration if using BasicHttpAuthentication
   - Verify Robust service has OfflineIMServiceRobustConnector loaded

### Diagnostic Steps

1. **Enable Debug Logging**
   ```ini
   [Startup]
   LogLevel = DEBUG
   ```

2. **Check Module Load Status**
   ```
   show modules
   ```
   Should display: `Offline Message Module V2`

3. **Test Message Storage Directly**
   ```csharp
   // From script or test harness
   IOfflineIMService offlineIM = scene.RequestModuleInterface<IOfflineIMService>();
   bool success = offlineIM.StoreMessage(testMessage, out string reason);
   ```

4. **Verify Database Schema**
   - Check `OfflineMessages` table exists
   - Verify appropriate data migration has run

## Database Schema

### OfflineMessages Table
The module requires an `OfflineMessages` table with the following structure:

| Column | Type | Description |
|--------|------|-------------|
| `PrincipalID` | char(36) | Recipient UUID |
| `Message` | text | Serialized GridInstantMessage XML |
| `TMStamp` | timestamp | Storage timestamp |

**Implementations:**
- MySQL: `src/OpenSim.Data.MySQL/MySQLOfflineIMData.cs`
- PostgreSQL: `src/OpenSim.Data.PGSQL/PGSQLOfflineIMData.cs`

## Security Considerations

### Message Privacy
- Messages stored in database in serialized form
- No encryption at rest (consider database-level encryption)
- Remote service connections support authentication

### Authentication
Remote mode supports `BasicHttpAuthentication`:

```ini
[Network]
AuthType = BasicHttpAuthentication

[Messaging]
OfflineMessageURL = "http://robust.server:8003"
```

### Access Control
- Only message recipient can retrieve their messages
- Sender identity preserved in stored messages
- No third-party access to message storage

## Performance Tuning

### Optimization Tips

1. **Use Remote Storage for Grids**
   - Centralizes message storage
   - Reduces per-region database load
   - Enables message delivery across grid

2. **Database Indexing**
   - Index `PrincipalID` column for fast retrieval
   - Index `TMStamp` for message expiry features

3. **Message Limits**
   - Consider implementing message count limits per user
   - Implement automatic expiry of old messages (custom enhancement)

4. **Network Optimization** (Remote Mode)
   - Use persistent HTTP connections
   - Enable compression for message transfer
   - Co-locate Robust service with database

## Version History

### V2 Improvements over Legacy Module
- **No Mono.Addins dependency** - Factory-based loading via reflection
- **Better error handling** - Comprehensive logging and error messages
- **Enhanced notifications** - Sender feedback on storage success/failure
- **Remote service support** - Flexible deployment architectures
- **Scene event integration** - Proper message routing through scene event system
- **Improved message filtering** - Configurable group message handling

## Related Components

### Service Layer
- [OfflineIMService](../src/OpenSim.Addons.OfflineIM/Service/OfflineIMService.cs) - Local storage implementation
- [OfflineIMServiceRemoteConnector](../src/OpenSim.Addons.OfflineIM/Remote/OfflineIMServiceRemoteConnector.cs) - Remote service client
- [OfflineIMServiceRobustConnector](../src/OpenSim.Addons.OfflineIM/Remote/OfflineIMServiceRobustConnector.cs) - Robust service endpoint

### Data Layer
- [IOfflineIMData](../src/OpenSim.Data/IOfflineIMData.cs) - Data interface
- [MySQLOfflineIMData](../src/OpenSim.Data.MySQL/MySQLOfflineIMData.cs) - MySQL implementation
- [PGSQLOfflineIMData](../src/OpenSim.Data.PGSQL/PGSQLOfflineIMData.cs) - PostgreSQL implementation

### Related Modules
- **MessageTransferModule** - Message routing and delivery
- **InstantMessageModule** - Real-time instant messaging
- **GroupsMessagingModuleV2** - Group chat and notices

## Source Code Reference
- **Module**: [src/OpenSim.Addons.OfflineIM/OfflineIMRegionModule.cs](../src/OpenSim.Addons.OfflineIM/OfflineIMRegionModule.cs)
- **Factory Loading**: [src/OpenSim.Region.CoreModules/ModuleFactory.cs:1100-1124](../src/OpenSim.Region.CoreModules/ModuleFactory.cs)
