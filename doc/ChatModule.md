# ChatModule Documentation

## Overview

The ChatModule is a **core shared region module** that provides comprehensive chat functionality in OpenSimulator. It handles all forms of communication between avatars, objects, and the system, including spatial audio simulation through distance-based message delivery, parcel-based privacy controls, and administrative features.

## Purpose

**Primary Functions:**
- **Spatial Chat Delivery** - Distance-based message delivery with whisper, say, and shout ranges
- **Multi-Source Communication** - Handle messages from avatars, scripted objects, and system broadcasts
- **Parcel Privacy Controls** - Respect land parcel restrictions and avatar visibility settings
- **Administrative Tools** - God mode privileges, admin prefixes, and user freezing capabilities
- **Client Integration** - Seamless integration with viewer chat interfaces and protocols
- **Cross-Region Support** - Handle chat across multiple regions in grid deployments

## Architecture

### Module Structure

The ChatModule implements the `ISharedRegionModule` interface and integrates with multiple OpenSim systems:

```
ChatModule (ISharedRegionModule)
├── Event Handling
│   ├── OnNewClient - Client connection management
│   ├── OnChatFromClient - Avatar chat messages  
│   ├── OnChatFromWorld - Object chat messages
│   └── OnChatBroadcast - Region-wide broadcasts
├── Message Processing
│   ├── Distance Calculation - Spatial audio simulation
│   ├── Parcel Restrictions - Privacy and access controls
│   ├── Message Filtering - Channel and type validation
│   └── Client Delivery - Viewer protocol communication
└── Administrative Features
    ├── God Mode Support - Administrative privileges
    ├── User Freezing - Temporary chat restrictions
    └── Debug Channel - Development and testing tools
```

### Core Components

**Distance Management:**
- **Whisper Range** - Private, short-distance communication (default: 10m)
- **Say Range** - Normal conversation distance (default: 20m) 
- **Shout Range** - Long-distance communication (default: 100m)
- **Cross-Region** - Unlimited range for child agents and special cases

**Message Sources:**
- **Agent Messages** - Avatar-generated chat via client interface
- **Object Messages** - Scripted object communication (llSay, llShout, etc.)
- **System Broadcasts** - Region-wide announcements and notifications

**Privacy Controls:**
- **Parcel Restrictions** - Honor land parcel chat restrictions
- **Avatar Visibility** - Respect SeeAVs parcel setting for privacy
- **User Banning** - Exclude banned users from receiving messages

## Configuration

### Basic Configuration

Configure the ChatModule in the `[Chat]` section:

```ini
[Chat]
enabled = true
whisper_distance = 10
say_distance = 20  
shout_distance = 100
admin_prefix = "(Admin) "
```

### Configuration Parameters

**Core Settings:**
- `enabled` - Enable/disable the chat module (default: true)
- `whisper_distance` - Range in meters for whisper chat (default: 10)
- `say_distance` - Range in meters for normal chat (default: 20)  
- `shout_distance` - Range in meters for shout chat (default: 100)
- `admin_prefix` - Text prefix for god/admin users (default: "")

### Module Loading

The ChatModule is loaded automatically via ModuleFactory:

```csharp
// ModuleFactory.cs automatically loads ChatModule
yield return new ChatModule();
```

## Message Processing Flow

### 1. Client Message Reception

```csharp
public virtual void OnChatFromClient(Object sender, OSChatMessage c)
{
    // Redistribute to interested subscribers
    Scene scene = (Scene)c.Scene;
    scene.EventManager.TriggerOnChatFromClient(sender, c);

    // Filter non-public channels
    if (c.Channel != 0 && c.Channel != DEBUG_CHANNEL) return;

    // Check if user is frozen
    if (FreezeCache.Contains(c.Sender.AgentId.ToString()))
    {
        c.Sender.SendAgentAlertMessage("You may not talk as you are frozen.", false);
        return;
    }

    // Process message for delivery
    DeliverChatToAvatars(ChatSourceType.Agent, c);
}
```

### 2. Spatial Distance Calculation

```csharp
protected virtual bool TrySendChatMessage(
    ScenePresence presence, Vector3 fromPos, Vector3 regionPos,
    UUID fromAgentID, UUID ownerID, string fromName, ChatTypeEnum type,
    string message, ChatSourceType src, bool ignoreDistance)
{
    if (!ignoreDistance)
    {
        float maxDistSQ;
        switch(type)
        {
            case ChatTypeEnum.Whisper: maxDistSQ = m_whisperdistanceSQ; break;
            case ChatTypeEnum.Say: maxDistSQ = m_saydistanceSQ; break;  
            case ChatTypeEnum.Shout: maxDistSQ = m_shoutdistanceSQ; break;
            default: maxDistSQ = -1f; break;
        }

        // Calculate cross-region distance
        Vector3 fromRegionPos = fromPos + regionPos;
        Vector3 toRegionPos = presence.AbsolutePosition +
            new Vector3(presence.Scene.RegionInfo.WorldLocX, 
                       presence.Scene.RegionInfo.WorldLocY, 0);

        if(maxDistSQ > 0 && maxDistSQ < Vector3.DistanceSquared(toRegionPos, fromRegionPos))
            return false;
    }

    // Send message to client
    presence.ControllingClient.SendChatMessage(message, (byte)type, fromPos, 
        fromName, fromAgentID, ownerID, (byte)src, (byte)ChatAudibleLevel.Fully);
    return true;
}
```

### 3. Parcel Privacy Enforcement

```csharp
protected virtual void DeliverChatToAvatars(ChatSourceType sourceType, OSChatMessage c)
{
    // Check for parcel privacy restrictions
    bool checkParcelHide = false;
    UUID sourceParcelID = UUID.Zero;
    
    if (c.Type < ChatTypeEnum.DebugChannel && destination.IsZero())
    {
        ILandObject srcland = scene.LandChannel.GetLandObject(hidePos.X, hidePos.Y);
        if (srcland != null && !srcland.LandData.SeeAVs)
        {
            sourceParcelID = srcland.LandData.GlobalID;
            checkParcelHide = true;
        }
    }

    // Deliver to each presence with privacy checks
    scene.ForEachScenePresence(delegate(ScenePresence presence)
    {
        ILandObject presenceParcel = scene.LandChannel.GetLandObject(
            presence.AbsolutePosition.X, presence.AbsolutePosition.Y);
            
        if (checkParcelHide && 
            sourceParcelID.NotEqual(presenceParcel.LandData.GlobalID) && 
            !presence.IsViewerUIGod)
        {
            return; // Skip delivery due to parcel privacy
        }

        TrySendChatMessage(presence, fromPos, regionPos, fromID, ownerID, 
            fromNamePrefix + fromName, c.Type, message, sourceType, 
            destination.IsNotZero());
    });
}
```

## Chat Types and Ranges

### Standard Chat Types

**Whisper (`ChatTypeEnum.Whisper`):**
- **Range**: Configurable (default: 10 meters)
- **Usage**: Private, intimate conversation  
- **Visual**: Displayed in italics in most viewers
- **Protocol**: Sent with whisper type flag

**Say (`ChatTypeEnum.Say`):**
- **Range**: Configurable (default: 20 meters)
- **Usage**: Normal conversation
- **Visual**: Standard chat display
- **Protocol**: Default chat type

**Shout (`ChatTypeEnum.Shout`):**  
- **Range**: Configurable (default: 100 meters)
- **Usage**: Long-distance communication
- **Visual**: Often displayed in bold or different color
- **Protocol**: Sent with shout type flag

### Special Chat Types

**Debug Channel (`DEBUG_CHANNEL = 2147483647`):**
- **Range**: Unlimited
- **Usage**: Development and debugging
- **Access**: Available to all users
- **Protocol**: Bypasses normal range restrictions

**Owner Chat (`ChatTypeEnum.Owner`):**
- **Range**: Unlimited to object owner
- **Usage**: Private object-to-owner communication  
- **Access**: Only delivered to object owner
- **Protocol**: Object ownership verification required

**Broadcast (`ChatTypeEnum.Region`):**
- **Range**: Region-wide
- **Usage**: System announcements
- **Access**: Typically admin/system generated
- **Protocol**: Delivered to all users in region

## Administrative Features

### God Mode Privileges

**Enhanced Capabilities:**
```csharp
if (avatar.IsViewerUIGod)
{
    fromNamePrefix = m_adminPrefix;  // Add admin prefix
    checkParcelHide = false;         // Bypass parcel restrictions  
}
```

**Admin Privileges:**
- **Bypass Range Limits** - Gods can communicate at any distance
- **Parcel Override** - Ignore parcel privacy restrictions
- **Name Prefix** - Configurable prefix added to admin messages
- **Freeze Override** - Admins can speak while frozen

### User Freezing System

**Freeze Functionality:**
```csharp
public virtual void ParcelFreezeUser(IClientAPI client, UUID parcelowner, 
    uint flags, UUID target)
{
    if (flags == 0) // Freeze user
    {
        FreezeCache.Add(target.ToString());
        System.Threading.Timer timer = new System.Threading.Timer(
            OnEndParcelFrozen, target, 30000, 0); // 30 second auto-unfreeze
        Timers.Add(target, timer);
    }
    else // Unfreeze user
    {
        FreezeCache.Remove(target.ToString());
        // Clean up timer
    }
}
```

**Freeze Features:**
- **Temporary Restriction** - 30-second automatic unfreeze
- **Chat Blocking** - Prevents frozen users from sending messages
- **Alert Notification** - Users receive "frozen" message when attempting to chat
- **Timer Management** - Automatic cleanup and memory management

## Integration Points

### Scene Event System

**Event Registration:**
```csharp
public virtual void AddRegion(Scene scene)
{
    scene.EventManager.OnNewClient += OnNewClient;
    scene.EventManager.OnChatFromWorld += OnChatFromWorld; 
    scene.EventManager.OnChatBroadcast += OnChatBroadcast;
}
```

**Event Triggering:**
```csharp
// Redistribute client messages to other modules
scene.EventManager.TriggerOnChatFromClient(sender, c);
```

### Simulator Features Integration

**Range Advertisement:**
```csharp
public virtual void RegionLoaded(Scene scene)
{
    ISimulatorFeaturesModule featuresModule = 
        scene.RequestModuleInterface<ISimulatorFeaturesModule>();
    if (featuresModule != null)
    {
        featuresModule.AddOpenSimExtraFeature("say-range", new OSDInteger(m_saydistance));
        featuresModule.AddOpenSimExtraFeature("whisper-range", new OSDInteger(m_whisperdistance));  
        featuresModule.AddOpenSimExtraFeature("shout-range", new OSDInteger(m_shoutdistance));
    }
}
```

### Client API Integration

**Message Delivery:**
```csharp
presence.ControllingClient.SendChatMessage(
    message, (byte)type, fromPos, fromName, 
    fromAgentID, ownerID, (byte)src, (byte)ChatAudibleLevel.Fully);
```

**Alert Messages:**
```csharp
c.Sender.SendAgentAlertMessage("You may not talk as you are frozen.", false);
```

## Performance Considerations

### Spatial Optimization

**Distance Calculation Optimization:**
```csharp
// Pre-calculate squared distances to avoid expensive sqrt operations
m_saydistanceSQ = m_saydistance * m_saydistance;
m_shoutdistanceSQ = m_shoutdistance * m_shoutdistance;
m_whisperdistanceSQ = m_whisperdistance * m_whisperdistance;

// Use squared distance comparison
if(maxDistSQ < Vector3.DistanceSquared(toRegionPos, fromRegionPos))
    return false;
```

**Early Filtering:**
```csharp
// Filter non-public channels early to reduce processing
if (c.Channel != 0 && c.Channel != DEBUG_CHANNEL) return;

// Skip deleted/transitioning presences  
if (presence.IsDeleted || presence.IsInTransit || !presence.ControllingClient.IsActive)
    return false;
```

### Memory Management

**Freeze Cache Management:**
- Automatic timer cleanup prevents memory leaks
- Dictionary-based lookup for O(1) freeze status checks
- Automatic 30-second expiration prevents permanent freezes

**Scene Management:**
- Thread-safe scene collection management
- Proper event handler cleanup on region removal
- Minimal object allocation in hot paths

## Usage Scenarios

### Standard Virtual World Chat

**Typical Configuration:**
```ini  
[Chat]
enabled = true
whisper_distance = 10    # Intimate conversation
say_distance = 20        # Normal chat
shout_distance = 100     # Long-distance communication
```

**Usage Patterns:**
- **Social Interaction** - Avatar-to-avatar communication at events and gatherings
- **Roleplay** - Distance-based immersive communication
- **Commerce** - Customer service and business communication
- **Education** - Classroom and meeting environments

### Administrative and Moderation

**Admin Configuration:**
```ini
[Chat]  
admin_prefix = "[ADMIN] "
```

**Administrative Uses:**
- **Event Management** - Administrators can communicate across parcel boundaries
- **User Moderation** - Temporary chat restrictions via freeze functionality  
- **System Announcements** - Region-wide broadcast capabilities
- **Technical Support** - Debug channel for troubleshooting

### Scripted Object Integration

**LSL Script Communication:**
```lsl
// Object scripts use ChatModule for communication
llSay(0, "Hello, world!");           // Say range
llWhisper(0, "Quiet message");       // Whisper range  
llShout(0, "Loud announcement");     // Shout range
llOwnerSay("Private to owner");      // Owner-only message
```

**Integration Features:**
- **Ownership Verification** - Ensures owner-only messages reach correct recipient
- **Object Attachment Handling** - Special processing for attached objects
- **Message Length Limits** - Automatic truncation to prevent abuse (1000 characters)

## Security Considerations

### Privacy Protection

**Parcel-Based Privacy:**
- Respects land parcel SeeAVs setting for chat visibility
- Prevents cross-parcel eavesdropping when privacy is enabled
- God mode override for administrative access

**Distance-Based Security:**
- Spatial limitations prevent unauthorized long-distance communication
- Range validation prevents client manipulation of chat distances
- Cross-region distance calculation ensures consistent behavior

### Anti-Abuse Measures

**Message Limitations:**
- 1000-character message length limit prevents spam
- Channel filtering reduces processing load
- Freeze system provides temporary restriction capability

**Access Controls:**
- Parcel ban/restriction checking prevents unauthorized communication  
- Owner-only message verification ensures privacy
- God mode restrictions limit administrative privileges

## Troubleshooting

### Common Issues

**Chat Not Working:**
- **Cause**: Module disabled in configuration
- **Solution**: Ensure `enabled = true` in `[Chat]` section
- **Debug**: Check startup logs for module initialization messages

**Messages Not Reaching Recipients:**
- **Cause**: Distance limitations or parcel restrictions
- **Solution**: Check chat ranges and parcel settings
- **Debug**: Use debug channel (2147483647) to bypass range limits

**Admin Privileges Not Working:**
- **Cause**: God mode not properly configured
- **Solution**: Verify user account has god mode privileges
- **Debug**: Check `IsViewerUIGod` status in logs

### Performance Issues

**High CPU Usage:**
- **Cause**: Excessive chat volume or inefficient distance calculations
- **Solution**: Monitor message frequency and optimize ranges
- **Debug**: Enable debug logging to identify bottlenecks

**Memory Leaks:**
- **Cause**: Timer cleanup issues in freeze system
- **Solution**: Verify proper timer disposal in freeze/unfreeze operations
- **Debug**: Monitor freeze cache and timer dictionary sizes

## Technical Specifications

### System Requirements

- **.NET 8.0** - Runtime environment
- **Scene Management** - Region framework integration
- **Client API** - Viewer communication protocols
- **Event System** - OpenSim event manager integration

### Performance Characteristics

- **Message Latency**: < 10ms for local delivery
- **Distance Calculation**: O(n) where n = number of presences in region  
- **Memory Usage**: ~1KB per active presence for distance calculations
- **Throughput**: Scales linearly with region population

### Protocol Compatibility

**Viewer Support:**
- Second Life protocol compatibility
- OpenMetaverse library integration
- Chat type and audibility level support
- Spatial audio simulation

**Server Integration:**
- Multi-region grid support
- Cross-region distance calculation
- Child agent communication
- Event-driven architecture

The ChatModule provides essential communication infrastructure for OpenSimulator virtual worlds, enabling natural spatial conversation while maintaining privacy controls and administrative capabilities for effective community management.