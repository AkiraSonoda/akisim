# Groups Messaging Module V2

## Overview

The Groups Messaging Module V2 is part of the Groups Module V2 system in Akisim, providing comprehensive group instant messaging capabilities for virtual world environments. This module handles all group chat sessions, message distribution, and session management within the OpenSimulator-based virtual world platform.

## Key Features

- **Real-time group messaging** - Instant message delivery to group members
- **Online-only messaging** - Optimized delivery only to online group members  
- **Session management** - Automatic chat session creation and tracking
- **Cross-region messaging** - Message distribution across multiple regions/simulators
- **Performance caching** - Cached presence information for efficient message delivery
- **Debug capabilities** - Comprehensive logging and debugging features

## Architecture

### Module Information
- **Namespace**: `OpenSim.Groups`
- **Assembly**: `OpenSim.Addons.Groups.dll`
- **Module Name**: `Groups Messaging Module V2`
- **Implements**: `ISharedRegionModule`, `IGroupsMessagingModule`
- **Loading**: Loaded via ModuleFactory (no Mono.Addins dependency)

### Dependencies
- **IGroupsServicesConnector** - Groups data access
- **IMessageTransferModule** - Grid-wide message transfer
- **IUserManagement** - User information management
- **IPresenceService** - Online presence tracking
- **IEventQueue** - Client event delivery

## Configuration

### Required Configuration
```ini
[Groups]
Enabled = true
Module = "Groups Module V2"
MessagingModule = "Groups Messaging Module V2"
MessagingEnabled = true
MessageOnlineUsersOnly = true
```

### Configuration Parameters

| Parameter | Description | Default | Required |
|-----------|-------------|---------|----------|
| `MessagingEnabled` | Enable/disable group messaging functionality | true | No |
| `MessageOnlineUsersOnly` | Only send messages to online users | false | **Yes** (V2 requires `true`) |
| `MessagingDebugEnabled` | Enable verbose debug logging | false | No |

### Important Notes
- **MessageOnlineUsersOnly must be set to `true`** - Groups Messaging Module V2 requires this setting and will disable itself if set to false
- The module automatically loads when Groups Module V2 is enabled
- No separate configuration section needed beyond the main `[Groups]` section

## Core Functionality

### Message Distribution
- **Online Member Detection** - Uses presence service to identify online group members
- **Efficient Caching** - Caches online member lists for 20 seconds to reduce presence service load
- **Regional Optimization** - Sends only one message per region to avoid duplicate delivery
- **Local vs Remote Delivery** - Handles both local (same region) and cross-region message delivery

### Session Management
- **Automatic Session Creation** - Creates chat sessions when users send first message
- **Session Tracking** - Tracks which users have joined/dropped from sessions
- **Invitation System** - Automatically invites new participants to ongoing sessions
- **ChatterBox Integration** - Uses viewer's ChatterBox system for session UI

### Message Types Handled
- `SessionGroupStart` - Initiating a group chat session
- `SessionSend` - Sending messages within group chat
- `SessionAdd` - Adding members to chat session
- `SessionDrop` - Members leaving chat session

## Performance Characteristics

### Caching Strategy
- **Online Users Cache** - 20-second expiry cache for group member presence
- **Regional Message Deduplication** - Prevents sending duplicate messages to same region
- **Session State Tracking** - In-memory tracking of session participation

### Scalability Features
- **Grid-Wide Distribution** - Supports message distribution across multiple simulators
- **Presence Service Integration** - Efficiently queries only for group member presence
- **Threaded Operations** - Uses ThreadedClasses for performance-critical operations

## API Reference

### Public Methods

#### `StartGroupChatSession(UUID agentID, UUID groupID)`
Validates and starts a group chat session for the specified agent and group.

**Parameters:**
- `agentID` - UUID of the agent starting the session
- `groupID` - UUID of the target group

**Returns:** `bool` - True if group exists and session can be started

#### `SendMessageToGroup(GridInstantMessage im, UUID groupID)`
Sends a message to all online members of the specified group.

**Parameters:**
- `im` - The instant message to send
- `groupID` - UUID of the target group

#### `SendMessageToGroup(GridInstantMessage im, UUID groupID, UUID sendingAgentForGroupCalls, Func<GroupMembersData, bool> sendCondition)`
Advanced message sending with custom filtering conditions.

**Parameters:**
- `im` - The instant message to send
- `groupID` - UUID of the target group  
- `sendingAgentForGroupCalls` - Agent UUID for permission checks
- `sendCondition` - Optional filter function for recipient selection

### Events Handled
- `OnNewClient` - Client connection events
- `OnMakeRootAgent` - Agent becoming root in region
- `OnMakeChildAgent` - Agent becoming child in region
- `OnIncomingInstantMessage` - Grid instant messages
- `OnClientLogin` - Client login events

## Debug Features

### Console Commands
```
debug groups messaging verbose <true|false>
```
Enables or disables verbose debug logging for group messaging operations.

### Debug Information
When debug mode is enabled, the module logs:
- Message routing decisions
- Online member detection
- Session management operations
- Regional message distribution
- Performance timing information

## Integration Notes

### Groups Module V2 Integration
- Automatically loaded by ModuleFactory when Groups Module V2 is enabled
- Shares the same configuration section (`[Groups]`)
- Requires Groups Module V2 to be active and properly configured
- Uses reflection-based loading to avoid hard assembly dependencies

### Message Transfer Module
- Relies on Message Transfer Module for cross-region message delivery
- Uses asynchronous message sending for grid-wide distribution
- Handles message transfer failures gracefully

### Event Queue Integration
- Uses IEventQueue for ChatterBox invitation delivery
- Sends session start replies via event queue
- Handles agent list updates for chat sessions

## Performance Monitoring

### Key Metrics
- **Message Distribution Time** - Time taken to distribute messages to all recipients
- **Online Member Detection** - Time to query and cache online member status
- **Cache Hit Rate** - Effectiveness of online member caching
- **Regional Message Count** - Number of regions messaged per group message

### Performance Considerations
- Large groups (>100 members) may experience slight delays during initial message distribution
- Presence service load scales with group size and message frequency
- Cache expiry affects balance between message delivery speed and member list accuracy

## Troubleshooting

### Common Issues

1. **Messages not delivered**
   - Verify `MessageOnlineUsersOnly = true`
   - Check that Groups Module V2 is properly loaded
   - Ensure Message Transfer Module is active

2. **Session not starting**
   - Confirm group exists and agent has permissions
   - Check IEventQueue module is loaded
   - Verify ChatterBox capability is available

3. **Debug logging not working**
   - Use console command: `debug groups messaging verbose true`
   - Check log4net configuration includes Groups.Messaging logger

### Log Examples
```
[Groups.Messaging]: SendMessageToGroup for group 12345678-1234-1234-1234-123456789012 with 25 visible members, 15 online took 45ms
[Groups.Messaging]: Delivering to agent 87654321-4321-4321-4321-210987654321 via Grid
[Groups.Messaging]: Found root agent for client : John Doe
```

## Version History

### V2 Improvements over Legacy Module
- **Online-only messaging** - Eliminates failed delivery attempts to offline users
- **Enhanced caching** - ThreadedClasses-based caching system for better performance
- **Regional optimization** - Intelligent message routing to prevent duplicates
- **Better error handling** - Graceful degradation when dependencies unavailable
- **Cleaner architecture** - Separated from XmlRpc dependencies

## Related Documentation
- [Groups Module V2](GroupsModuleV2.md)
- [Groups Service HG Connector Module](GroupsServiceHGConnectorModule.md)