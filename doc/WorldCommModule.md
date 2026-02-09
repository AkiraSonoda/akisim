# WorldCommModule

## Overview

The WorldCommModule is a critical infrastructure component for OpenSimulator/Akisim that implements the LSL (Linden Scripting Language) chat communication system. This non-shared region module provides the foundational implementation for script-to-script and script-to-avatar communication through the `llListen()` family of functions. It manages chat listeners, filters incoming messages based on configurable criteria, and delivers matched messages to appropriate scripts. The module is essential for any LSL scripting functionality that involves chat communication, making it a core requirement for virtual world interactivity.

## Architecture

The WorldCommModule implements the following interfaces:
- `INonSharedRegionModule` - Per-region module lifecycle management
- `IWorldComm` - Script communication interface contract

### Key Components

1. **Listen Management System**
   - **Dynamic Listener Registration**: Manages active chat listeners created by `llListen()` calls
   - **Handle Generation**: Creates unique handles for each listener within script-specific scope
   - **State Management**: Controls active/inactive listener states for performance optimization
   - **Resource Limits**: Enforces configurable limits on listeners per region and per script

2. **Message Filtering Engine**
   - **Multi-Criteria Filtering**: Filters messages by channel, name, source UUID, and message content
   - **Regex Support**: Advanced pattern matching for names and messages using regular expressions
   - **Distance-Based Filtering**: Implements whisper, say, and shout distance calculations
   - **Source Validation**: Prevents scripts from receiving their own messages

3. **Chat Delivery System**
   - **Position-Based Routing**: Calculates message delivery based on speaker and listener positions
   - **Channel Management**: Efficient routing of messages based on chat channels
   - **Event Triggering**: Integrates with script engine event system for message delivery
   - **Attachment Support**: Special handling for messages to avatar attachments

4. **Serialization and Persistence**
   - **State Serialization**: Preserves listener state across script resets and region restarts
   - **Data Recovery**: Reconstructs listener information from serialized data
   - **Atomic Operations**: Thread-safe operations for concurrent access
   - **Memory Management**: Efficient cleanup of expired listeners

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
[Modules]
WorldCommModule = true
```

### Chat Distance Configuration

Configure in `[Chat]` section:
```ini
[Chat]
whisper_distance = 10    ; Distance in meters for whisper (default: 10)
say_distance = 20        ; Distance in meters for say (default: 20)
shout_distance = 100     ; Distance in meters for shout (default: 100)
```

### Listener Limits Configuration

Configure in `[LL-Functions]` section:
```ini
[LL-Functions]
max_listens_per_region = 1000   ; Maximum listeners per region (default: 1000)
max_listens_per_script = 65     ; Maximum listeners per script (default: 65)
```

### Default Behavior

- **Enabled by Default**: WorldCommModule loads by default as it's essential for LSL chat functionality
- **Automatic Distance Calculation**: Chat distances are squared internally for efficient distance calculations
- **Unlimited Listeners**: Setting limits to 0 or negative values enables unlimited listeners
- **Region-Specific**: Each region maintains its own listener registry

## Features

### LSL Listen Functions Implementation

#### llListen() Function Support

The module provides the backend implementation for LSL `llListen()` functions:

```lsl
// Basic listen setup
integer handle = llListen(0, "", NULL_KEY, "");

// Filtered listen with specific criteria
integer handle = llListen(1, "SpecificName", "avatar-uuid", "command");

// Listen with regex support (OpenSim extension)
integer handle = llListenRegex(1, "pattern.*", NULL_KEY, "msg.*pattern");
```

#### Listen Control Functions

```lsl
// Control listener state
llListenControl(handle, TRUE);   // Activate listener
llListenControl(handle, FALSE);  // Deactivate listener

// Remove listener completely
llListenRemove(handle);
```

### Message Filtering and Delivery

#### Channel-Based Communication

```lsl
// Public chat (channel 0)
llListen(0, "", NULL_KEY, "");

// Private channels
llListen(42, "", NULL_KEY, "");     // Private channel 42
llListen(-1000, "", NULL_KEY, "");  // Negative channel
```

#### Name and UUID Filtering

```lsl
// Listen only to specific avatar
llListen(0, "John Doe", NULL_KEY, "");

// Listen only to specific object
llListen(0, "", "object-uuid", "");

// Combined filtering
llListen(0, "John Doe", "avatar-uuid", "hello");
```

#### Distance-Based Filtering

The module automatically applies distance-based filtering:

- **Whisper**: 10 meter radius (configurable)
- **Say**: 20 meter radius (configurable)
- **Shout**: 100 meter radius (configurable)
- **Region**: No distance limit (entire region)

### Advanced Features

#### Regular Expression Support

OpenSim extension supporting regex patterns:

```lsl
// Listen for names matching pattern
integer OS_LISTEN_REGEX_NAME = 0x1;
integer handle = llListenRegex(0, "User.*", NULL_KEY, "", OS_LISTEN_REGEX_NAME);

// Listen for messages matching pattern
integer OS_LISTEN_REGEX_MESSAGE = 0x2;
integer handle = llListenRegex(0, "", NULL_KEY, "cmd:.*", OS_LISTEN_REGEX_MESSAGE);

// Combined regex filtering
integer handle = llListenRegex(0, "Bot.*", NULL_KEY, "status:.*",
                              OS_LISTEN_REGEX_NAME | OS_LISTEN_REGEX_MESSAGE);
```

#### Direct Message Delivery

Support for targeted message delivery:

```lsl
// Messages delivered directly to specific objects/avatars
llRegionSayTo(target_uuid, channel, message);
```

### Listener Management

#### Handle Management

```csharp
// Each script gets unique handles starting from 1
private int GetNewHandle(UUID itemID)
{
    // Generate unique handle for this script
    // Handles are script-local and start from 1
    // Maximum handles per script are configurable
}
```

#### Resource Limiting

```csharp
// Region-wide listener limits
if (m_curlisteners < m_maxlisteners)
{
    // Create new listener
}

// Per-script handle limits
if (handles.Count >= m_maxhandles)
    return -1; // No more handles available
```

## Technical Implementation

### Listener Information Structure

#### ListenerInfo Class

```csharp
public class ListenerInfo : IWorldCommListenerInfo
{
    public bool IsActive { get; private set; }
    public int Handle { get; private set; }
    public UUID ItemID { get; private set; }    // Script UUID
    public UUID HostID { get; private set; }    // Object UUID
    public int Channel { get; private set; }
    public UUID ID { get; private set; }        // Filter UUID
    public string Name { get; private set; }    // Filter name
    public string Message { get; private set; } // Filter message
    public int RegexBitfield { get; private set; } // Regex flags
}
```

### Message Filtering Implementation

#### Distance Calculation

```csharp
public void DeliverMessage(ChatTypeEnum type, int channel, string name, UUID id, string msg, Vector3 position)
{
    // Determine maximum distance based on chat type
    float maxDistanceSQ;
    switch (type)
    {
        case ChatTypeEnum.Whisper:
            maxDistanceSQ = m_whisperdistance; // Pre-squared
            break;
        case ChatTypeEnum.Say:
            maxDistanceSQ = m_saydistance;
            break;
        case ChatTypeEnum.Shout:
            maxDistanceSQ = m_shoutdistance;
            break;
        case ChatTypeEnum.Region:
            TryEnqueueMessage(channel, name, id, msg);
            return;
    }

    TryEnqueueMessage(channel, position, maxDistanceSQ, name, id, msg);
}
```

#### Multi-Criteria Filtering

```csharp
public void TryEnqueueMessage(int channel, Vector3 position, float maxDistanceSQ, string name, UUID id, string msg)
{
    lock (mainLock)
    {
        if (!m_listenersByChannel.TryGetValue(channel, out List<ListenerInfo> listeners))
            return;

        foreach (ListenerInfo li in listeners)
        {
            // Check if listener is active
            if (!li.IsActive) continue;

            // Prevent self-messaging
            if (id.Equals(li.HostID)) continue;

            // Filter by source UUID
            if (li.ID.IsNotZero() && id.NotEqual(li.ID)) continue;

            // Filter by name (with regex support)
            if (li.Name.Length > 0)
            {
                if ((li.RegexBitfield & OS_LISTEN_REGEX_NAME) != 0)
                {
                    if (!Regex.IsMatch(name, li.Name)) continue;
                }
                else
                {
                    if (!name.Equals(li.Name, StringComparison.InvariantCulture)) continue;
                }
            }

            // Filter by message content (with regex support)
            if (li.Message.Length > 0)
            {
                if ((li.RegexBitfield & OS_LISTEN_REGEX_MESSAGE) != 0)
                {
                    if (!Regex.IsMatch(msg, li.Message)) continue;
                }
                else
                {
                    if (!msg.Equals(li.Message, StringComparison.InvariantCulture)) continue;
                }
            }

            // Check distance
            SceneObjectPart sPart = m_scene.GetSceneObjectPart(li.HostID);
            if (sPart == null) return;

            if (maxDistanceSQ > Vector3.DistanceSquared(sPart.AbsolutePosition, position))
            {
                // Deliver message to script
                m_scene.EventManager.TriggerScriptListen(li.ItemID, channel, name, id, msg);
            }
        }
    }
}
```

### Listener Registration System

#### Listen Function Implementation

```csharp
public int Listen(UUID itemID, UUID hostID, int channel, string name, UUID id, string msg, int regexBitfield)
{
    // Check for existing listener with same filter criteria
    List<ListenerInfo> coll = GetListeners(itemID, channel, name, id, msg);
    if (coll.Count > 0)
    {
        // LSL compliance: return existing handle for same filter settings
        return coll[0].Handle;
    }

    lock (mainLock)
    {
        if (m_curlisteners < m_maxlisteners)
        {
            int newHandle = GetNewHandle(itemID);
            if (newHandle > 0)
            {
                ListenerInfo li = new ListenerInfo(newHandle, itemID, hostID, channel, name, id, msg, regexBitfield);

                // Add to channel-based lookup
                if (!m_listenersByChannel.TryGetValue(channel, out List<ListenerInfo> listeners))
                {
                    listeners = new List<ListenerInfo>();
                    m_listenersByChannel.Add(channel, listeners);
                }
                listeners.Add(li);
                m_curlisteners++;

                return newHandle;
            }
        }
    }
    return -1; // Failed to create listener
}
```

#### Listener Control Implementation

```csharp
public void ListenControl(UUID itemID, int handle, int active)
{
    lock (mainLock)
    {
        foreach (KeyValuePair<int, List<ListenerInfo>> lis in m_listenersByChannel)
        {
            foreach (ListenerInfo li in lis.Value)
            {
                if (handle == li.Handle && itemID.Equals(li.ItemID))
                {
                    if (active == 0)
                        li.Deactivate();
                    else
                        li.Activate();
                    return;
                }
            }
        }
    }
}
```

### Serialization and State Management

#### State Serialization

```csharp
public Object[] GetSerializationData(UUID itemID)
{
    List<Object> data = new List<Object>();
    lock (mainLock)
    {
        foreach (List<ListenerInfo> list in m_listenersByChannel.Values)
        {
            foreach (ListenerInfo l in list)
            {
                if (itemID.Equals(l.ItemID))
                    data.AddRange(l.GetSerializationData());
            }
        }
    }
    return data.ToArray();
}
```

#### State Restoration

```csharp
public void CreateFromData(UUID itemID, UUID hostID, Object[] data)
{
    int idx = 0;
    while (idx < data.Length)
    {
        // Determine data item length (6 or 7 elements)
        int dataItemLength = (idx + 7 == data.Length || (idx + 7 < data.Length && data[idx + 7] is bool)) ? 7 : 6;
        Object[] item = new Object[dataItemLength];
        Array.Copy(data, idx, item, 0, dataItemLength);

        ListenerInfo info = ListenerInfo.FromData(itemID, hostID, item);

        lock (mainLock)
        {
            if (!m_listenersByChannel.ContainsKey((int)item[2]))
            {
                m_listenersByChannel.Add((int)item[2], new List<ListenerInfo>());
            }
            m_listenersByChannel[(int)item[2]].Add(info);
        }

        idx += dataItemLength;
    }
}
```

## Performance Characteristics

### Resource Usage

- **Memory Footprint**: Moderate memory usage - stores listener information per active listener
- **CPU Impact**: Low CPU overhead - efficient hash-based channel lookups
- **Network Usage**: No network usage - processes local chat events only
- **Lock Contention**: Minimal contention using single main lock for all operations

### Scalability Features

- **Channel-Based Indexing**: O(1) lookup for listeners by channel
- **Per-Region Isolation**: Each region maintains independent listener registry
- **Resource Limits**: Configurable limits prevent memory exhaustion
- **Efficient Cleanup**: Automatic cleanup when scripts are removed

### Performance Optimization

- **Pre-squared Distances**: Chat distances are squared once during initialization
- **Early Filtering**: Multiple filter stages eliminate unnecessary processing
- **Regex Caching**: Compiled regex patterns for repeated pattern matching
- **Memory Efficiency**: Minimal object allocation during message processing

## Usage Examples

### Basic Chat Listening

```lsl
// Simple chat listener for public channel
default
{
    state_entry()
    {
        // Listen to all chat on channel 0
        integer handle = llListen(0, "", NULL_KEY, "");
        llOwnerSay("Listening to public chat. Handle: " + (string)handle);
    }

    listen(integer channel, string name, key id, string message)
    {
        llOwnerSay("Heard: " + name + " said '" + message + "'");
    }
}
```

### Filtered Chat Listening

```lsl
// Listen for specific commands from specific users
integer command_handle;

default
{
    state_entry()
    {
        // Listen for "status" command on private channel
        command_handle = llListen(42, "", NULL_KEY, "status");
        llOwnerSay("Listening for status commands on channel 42");
    }

    listen(integer channel, string name, key id, string message)
    {
        if (message == "status")
        {
            llSay(42, "System operational");
        }
    }

    touch_start(integer total_number)
    {
        // Toggle listener on/off
        llListenControl(command_handle, FALSE); // Disable
        llSleep(1.0);
        llListenControl(command_handle, TRUE);  // Re-enable
    }
}
```

### Advanced Regex Listening

```lsl
// Using regex patterns for flexible message matching
integer regex_handle;

default
{
    state_entry()
    {
        // Listen for commands starting with "cmd:"
        regex_handle = llListenRegex(1, "", NULL_KEY, "^cmd:.*", OS_LISTEN_REGEX_MESSAGE);
        llOwnerSay("Listening for commands with regex pattern");
    }

    listen(integer channel, string name, key id, string message)
    {
        // Extract command after "cmd:" prefix
        string command = llGetSubString(message, 4, -1);
        llOwnerSay("Received command: " + command);

        if (command == "stop")
        {
            llListenRemove(regex_handle);
            llOwnerSay("Stopped listening");
        }
    }
}
```

### Multiple Listener Management

```lsl
// Managing multiple listeners with different filters
list listener_handles;

default
{
    state_entry()
    {
        // Set up multiple listeners
        listener_handles = [
            llListen(0, "", NULL_KEY, "hello"),     // Public greetings
            llListen(1, "Admin", NULL_KEY, ""),     // Admin channel
            llListen(42, "", NULL_KEY, "status")    // Status requests
        ];

        llOwnerSay("Set up " + (string)llGetListLength(listener_handles) + " listeners");
    }

    listen(integer channel, string name, key id, string message)
    {
        if (channel == 0 && message == "hello")
        {
            llSay(0, "Hello there, " + name + "!");
        }
        else if (channel == 1 && name == "Admin")
        {
            llOwnerSay("Admin message: " + message);
        }
        else if (channel == 42 && message == "status")
        {
            llSay(42, "All systems operational");
        }
    }

    on_rez(integer start_param)
    {
        // Clean up listeners on rez
        integer i;
        for (i = 0; i < llGetListLength(listener_handles); i++)
        {
            llListenRemove(llList2Integer(listener_handles, i));
        }
        llResetScript();
    }
}
```

### Distance-Aware Communication

```lsl
// Respond differently based on chat type
default
{
    state_entry()
    {
        llListen(0, "", NULL_KEY, "");
        llOwnerSay("Listening for whispers, says, and shouts");
    }

    listen(integer channel, string name, key id, string message)
    {
        // Calculate distance to speaker
        vector speaker_pos = llList2Vector(llGetObjectDetails(id, [OBJECT_POS]), 0);
        vector my_pos = llGetPos();
        float distance = llVecDist(speaker_pos, my_pos);

        string response;
        if (distance <= 10.0)
        {
            response = "I heard your whisper";
            llWhisper(0, response);
        }
        else if (distance <= 20.0)
        {
            response = "I heard you say: " + message;
            llSay(0, response);
        }
        else if (distance <= 100.0)
        {
            response = "I heard your shout from " + (string)((integer)distance) + "m away";
            llShout(0, response);
        }
    }
}
```

## Integration Points

### With Script Engines

- **Event Delivery**: Integrates with script engine event system via `TriggerScriptListen`
- **YEngine Integration**: Direct integration with YEngine for efficient event delivery
- **XEngine Compatibility**: Compatible with XEngine for legacy script support
- **Handle Management**: Provides unique handles for script listener identification

### With Chat System

- **Chat Event Handling**: Subscribes to `OnChatFromClient` and `OnChatBroadcast` events
- **Message Routing**: Routes appropriate chat messages to registered listeners
- **Type Integration**: Handles all `ChatTypeEnum` types (Whisper, Say, Shout, Region)
- **Source Filtering**: Prevents scripts from receiving their own chat messages

### With Scene Management

- **Scene Integration**: Registers as `IWorldComm` service for script access
- **Object Tracking**: Uses scene object tracking for position-based filtering
- **Event Manager**: Integrates with scene event manager for chat event handling
- **Lifecycle Management**: Proper cleanup when objects are removed from scene

### With Avatar System

- **Attachment Support**: Special handling for messages delivered to avatar attachments
- **Presence Tracking**: Integrates with scene presence management for avatar chat
- **Distance Calculation**: Uses avatar and object positions for accurate distance filtering
- **Direct Messaging**: Supports targeted message delivery to specific avatars/objects

## Security Features

### Message Filtering Security

- **Source Validation**: Prevents scripts from spoofing message sources
- **Self-Message Prevention**: Scripts cannot receive their own chat messages
- **Channel Isolation**: Messages are properly isolated by channel
- **UUID Validation**: Proper UUID handling and validation

### Resource Protection

- **Listener Limits**: Configurable limits prevent resource exhaustion
- **Handle Scoping**: Listener handles are scoped to specific scripts
- **Memory Management**: Automatic cleanup of orphaned listeners
- **Thread Safety**: Thread-safe operations prevent race conditions

### Access Control

- **Script Isolation**: Listeners are isolated per script and cannot interfere
- **Channel Privacy**: Private channels provide communication isolation
- **Region Boundaries**: Messages do not cross region boundaries (except region-wide chat)
- **Permission Validation**: Proper permission checking for message delivery

## Debugging and Troubleshooting

### Common Issues

1. **Listeners Not Working**: Check that WorldCommModule is enabled and properly loaded
2. **Messages Not Received**: Verify channel, distance, and filter criteria
3. **Too Many Listeners**: Check against configured listener limits
4. **Performance Issues**: Monitor listener count and message frequency

### Diagnostic Procedures

1. **Module Loading**: Check logs for WorldCommModule loading messages
2. **Listener Registration**: Verify listeners are created with valid handles
3. **Message Filtering**: Check filter criteria matches message content
4. **Distance Calculation**: Verify object positions and chat distances

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
WorldCommModule = true

[Chat]
whisper_distance = 10
say_distance = 20
shout_distance = 100

[LL-Functions]
max_listens_per_region = 1000
max_listens_per_script = 65
```

### Debug Methods

```csharp
// Check listener count
public int ListenerCount
{
    get
    {
        lock (mainLock)
        {
            return m_curlisteners;
        }
    }
}

// Check if channel has listeners
public bool HasListeners(int channel)
{
    lock (mainLock)
        return m_listenersByChannel.TryGetValue(channel, out List<ListenerInfo> listeners) && listeners.Count > 0;
}
```

## Use Cases

### Interactive Objects

- **Smart Objects**: Objects that respond to voice commands and chat
- **Information Kiosks**: Provide information based on user queries
- **Interactive NPCs**: Scripted characters that engage in conversation
- **Command Interfaces**: Chat-based control systems for complex objects

### Communication Systems

- **Chat Relays**: Forward messages between different channels or regions
- **Translation Services**: Translate messages between different languages
- **Message Logging**: Record and store chat conversations
- **Moderation Tools**: Monitor and filter inappropriate content

### Game Mechanics

- **Quest Systems**: Chat-based quest interaction and progression
- **Combat Systems**: Voice-activated combat commands and responses
- **Social Features**: Chat-based social interaction enhancements
- **Mini-Games**: Chat-based games and puzzles

### Administrative Tools

- **Remote Control**: Chat-based administration of sim functions
- **Monitoring Systems**: Track and respond to specific chat patterns
- **Event Management**: Coordinate events through chat commands
- **Support Systems**: Provide automated help and support responses

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the CoreModuleFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[Modules]` configuration section includes `WorldCommModule = true`
- Test chat listening functionality after deployment
- Update any scripts that depend on chat communication
- Validate distance and listener limit configurations

### Deployment Considerations

- **Script Engine Integration**: Ensure compatible script engine is available and properly configured
- **Performance Tuning**: Adjust listener limits based on expected usage patterns
- **Distance Configuration**: Configure chat distances appropriate for sim design
- **Resource Monitoring**: Monitor listener usage and performance impact

## Configuration Examples

### Basic Configuration

```ini
[Modules]
WorldCommModule = true
```

### Performance Optimized Configuration

```ini
[Modules]
WorldCommModule = true

[Chat]
whisper_distance = 8     ; Slightly reduced for performance
say_distance = 20
shout_distance = 80      ; Reduced for urban environments

[LL-Functions]
max_listens_per_region = 500   ; Reduced for high-density regions
max_listens_per_script = 32    ; Conservative limit per script
```

### Development Configuration

```ini
[Modules]
WorldCommModule = true

[Chat]
whisper_distance = 15    ; Extended for testing
say_distance = 30
shout_distance = 150

[LL-Functions]
max_listens_per_region = 2000  ; Higher limits for development
max_listens_per_script = 100

[Logging]
LogLevel = DEBUG
```

### Production Configuration

```ini
[Modules]
WorldCommModule = true

[Chat]
whisper_distance = 10
say_distance = 20
shout_distance = 100

[LL-Functions]
max_listens_per_region = 1000
max_listens_per_script = 65

[Logging]
LogLevel = INFO
```

## Best Practices

### Script Development

1. **Resource Management**: Always remove listeners when no longer needed
2. **Filter Optimization**: Use specific filters to reduce unnecessary processing
3. **Handle Management**: Keep track of listener handles for proper cleanup
4. **Error Handling**: Handle listener creation failures gracefully

### Performance Guidelines

1. **Listener Limits**: Monitor and manage listener count to prevent resource exhaustion
2. **Channel Usage**: Use private channels for non-public communication
3. **Filter Efficiency**: Use specific filters rather than catch-all listeners
4. **Cleanup Procedures**: Implement proper listener cleanup in script state changes

### Security Practices

1. **Channel Privacy**: Use private channels for sensitive communication
2. **Input Validation**: Validate and sanitize received chat messages
3. **Rate Limiting**: Implement rate limiting for command processing
4. **Access Control**: Verify sender identity for privileged operations

## Future Enhancements

### Potential Improvements

1. **Enhanced Regex**: More sophisticated pattern matching capabilities
2. **Message Queuing**: Buffering for high-frequency message scenarios
3. **Cross-Region Chat**: Support for chat across region boundaries
4. **Performance Monitoring**: Built-in performance metrics and monitoring

### Compatibility Considerations

1. **LSL Evolution**: Stay current with LSL specification updates
2. **Script Engine Updates**: Maintain compatibility with script engine changes
3. **Framework Integration**: Adapt to OpenSim framework evolution
4. **Performance Standards**: Optimize for evolving hardware capabilities

### Integration Opportunities

1. **Web Integration**: HTTP-based chat integration for web clients
2. **Database Logging**: Enhanced chat logging and history capabilities
3. **AI Integration**: Support for AI-powered chat analysis and response
4. **Mobile Integration**: Chat integration with mobile applications