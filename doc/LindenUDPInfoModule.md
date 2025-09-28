# LindenUDPInfoModule Technical Documentation

## Overview

The LindenUDPInfoModule is a shared region module that provides comprehensive diagnostic and monitoring capabilities for the Linden UDP client stack in OpenSimulator. It exposes console commands for inspecting UDP client queues, throttle settings, packet statistics, and image transfer queues, enabling administrators to monitor and troubleshoot network performance and client connectivity issues.

## Module Classification

- **Type**: ISharedRegionModule
- **Namespace**: OpenSim.Region.OptionalModules.UDP.Linden
- **Assembly**: OpenSim.Region.OptionalModules
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs with automatic loading

## Core Functionality

### Primary Purpose

The LindenUDPInfoModule serves as a diagnostic and monitoring tool for the OpenSimulator Linden UDP client stack. It provides administrators with detailed insights into client connection states, packet queue statistics, throttle configurations, and image transfer performance through a comprehensive set of console commands.

### Key Features

1. **Priority Queue Monitoring**: Inspect priority-based packet queues for each client
2. **General Queue Statistics**: Monitor packet in/out, resent packets, and unacknowledged bytes
3. **Image Queue Analysis**: Detailed inspection of texture download queues and status
4. **Throttle Configuration Display**: View bandwidth throttle settings for all packet types
5. **Multi-Region Support**: Operates across all regions in a grid deployment
6. **Client Type Filtering**: Distinguish between root agents and child agents
7. **Individual Client Targeting**: Focus monitoring on specific users
8. **Real-time Statistics**: Live monitoring of UDP stack performance

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for shared modules
1. Initialise(IConfigSource) - Basic module initialization
2. AddRegion(Scene) - Register scene and console commands
3. RegionLoaded(Scene) - Final region setup
4. PostInitialise() - Post-initialization setup (no-op)
5. RemoveRegion(Scene) - Scene cleanup and command removal
6. Close() - Module cleanup (no-op)
```

### Scene Management

```csharp
protected RwLockedDictionary<UUID, Scene> m_scenes = new RwLockedDictionary<UUID, Scene>();

public void AddRegion(Scene scene)
{
    m_scenes[scene.RegionInfo.RegionID] = scene;
    // Register console commands for this scene
}

public void RemoveRegion(Scene scene)
{
    m_scenes.Remove(scene.RegionInfo.RegionID);
}
```

## Console Commands

### Priority Queue Inspection

#### Command: `show pqueues [full]`

**Purpose**: Display priority queue data for each client showing packet distribution across priority levels.

**Usage**:
```
show pqueues          # Show root agents only
show pqueues full     # Show root and child agents
show pqueues <first-name> <last-name>  # Show specific user
```

**Output Format**:
```
User              Region        Type Pri 0  Pri 1  Pri 2  Pri 3  Pri 4  Pri 5  Pri 6  Pri 7  Pri 8  Pri 9  Pri 10 Pri 11
TestUser          MainRegion    Rt      45     23     12      8      3      1      0      0      0      0      0      0
```

**Implementation**:
```csharp
protected string GetPQueuesReport(string[] showParams)
{
    bool showChildren = false;
    string pname = "";

    if (showParams.Length > 2 && showParams[2] == "full")
        showChildren = true;
    else if (showParams.Length > 3)
        pname = showParams[2] + " " + showParams[3];

    foreach (Scene scene in m_scenes.Values)
    {
        scene.ForEachClient(delegate(IClientAPI client)
        {
            if (client is LLClientView llclient)
            {
                bool isChild = client.SceneAgent.IsChildAgent;
                if (isChild && !showChildren) return;
                if (pname != "" && client.Name != pname) return;

                report.AppendLine(llclient.EntityUpdateQueue.ToString());
            }
        });
    }
}
```

### General Queue Statistics

#### Command: `show queues [full]`

**Purpose**: Display comprehensive packet queue statistics including traffic, latency, and unacknowledged data.

**Usage**:
```
show queues          # Show root agents only
show queues full     # Show root and child agents
show queues <first-name> <last-name>  # Show specific user
```

**Output Format**:
```
User              Region        Type   Since   Pkts    Pkts    Pkts     Bytes Q Pkts Q Pkts Q Pkts Q Pkts Q Pkts  Q Pkts Q Pkts
                                      Last In    In     Out   Resent   Unacked Resend   Land   Wind  Cloud   Task Texture Asset
TestUser          MainRegion    Rt       150  1245    1198       12     4096       0      0      0      0      3      15     2
```

**Key Metrics**:
- **Since Last In**: Milliseconds since last packet received
- **Pkts In/Out**: Total packets processed/sent
- **Pkts Resent**: Packets requiring retransmission
- **Bytes Unacked**: Unacknowledged data awaiting confirmation
- **Q Pkts**: Queued packets by type (Resend, Land, Wind, Cloud, Task, Texture, Asset)

### Image Queue Analysis

#### Command: `show image queues <first-name> <last-name> [full]`

**Purpose**: Detailed inspection of texture download queues for specific clients.

**Usage**:
```
show image queues TestUser Smith        # Show root agent only
show image queues TestUser Smith full   # Show root and child agents
```

**Output Format**:
```
In region MainRegion (root agent)
Images in queue: 15
Texture ID                            Last Seq  Priority   Start Pkt  Has Asset  Decoded
a1b2c3d4-e5f6-7890-abcd-ef1234567890         5      1000          0       True     True
b2c3d4e5-f6g7-8901-bcde-f23456789012        12       850          0       True    False
```

**Key Information**:
- **Texture ID**: UUID of the texture being downloaded
- **Last Seq**: Last sequence number processed
- **Priority**: Download priority (higher = more important)
- **Start Pkt**: Starting packet number for this download
- **Has Asset**: Whether the asset exists on the server
- **Decoded**: Whether the image has been decoded successfully

#### Command: `clear image queues <first-name> <last-name>`

**Purpose**: Clear all pending image downloads for a specific client.

**Implementation**:
```csharp
protected string HandleImageQueuesClear(string[] cmd)
{
    string firstName = cmd[3];
    string lastName = cmd[4];

    List<ScenePresence> foundAgents = new();
    foreach (Scene scene in m_scenes.Values)
    {
        ScenePresence sp = scene.GetScenePresence(firstName, lastName);
        if (sp is not null)
            foundAgents.Add(sp);
    }

    foreach (ScenePresence agent in foundAgents)
    {
        if (agent.ControllingClient is LLClientView client)
        {
            int requestsDeleted = client.ImageManager.ClearImageQueue();
            report.AppendFormat("In region {0} ({1} agent) cleared {2} requests\n",
                agent.Scene.RegionInfo.RegionName,
                agent.IsChildAgent ? "child" : "root",
                requestsDeleted);
        }
    }
}
```

### Throttle Configuration Display

#### Command: `show throttles [full]`

**Purpose**: Display bandwidth throttle settings for all packet types and clients.

**Usage**:
```
show throttles          # Show root agents only
show throttles full     # Show root and child agents
show throttles <first-name> <last-name>  # Show specific user
```

**Output Format**:
```
User              Region        Type      Max   Target  Actual   Resend     Land     Wind    Cloud     Task  Texture    Asset
                                         kb/s     kb/s    kb/s     kb/s     kb/s     kb/s     kb/s     kb/s     kb/s     kb/s

TestUser          MainRegion    Rt        500      450     420       50       25       10       10       75      200       50
```

**Throttle Types**:
- **Max**: Maximum allowed bandwidth
- **Target**: Adaptive throttling target
- **Actual**: Current throttle setting
- **Resend**: Bandwidth for packet retransmission
- **Land**: Land data updates
- **Wind**: Wind simulation data
- **Cloud**: Cloud simulation data
- **Task**: Task/script communications
- **Texture**: Texture downloads
- **Asset**: Other asset downloads

## Data Collection and Analysis

### Client Statistics Collection

```csharp
scene.ForEachClient(delegate(IClientAPI client)
{
    if (client is IStatsCollector collector)
    {
        IStatsCollector stats = collector;
        report.AppendLine(stats.Report());
    }
});
```

### LLClientView Integration

```csharp
if (client is LLClientView llClient)
{
    LLUDPClient llUdpClient = llClient.UDPClient;
    ClientInfo ci = llUdpClient.GetClientInfo();

    // Access detailed client information
    // - Throttle settings
    // - Packet statistics
    // - Queue states
}
```

### Image Manager Integration

```csharp
if (agent.ControllingClient is LLClientView client)
{
    J2KImage[] images = client.ImageManager.GetImages();

    foreach (J2KImage image in images)
    {
        // Detailed image download status
        // - Texture ID, priority, progress
        // - Asset availability and decode status
    }
}
```

## Diagnostic Capabilities

### Network Performance Monitoring

1. **Latency Tracking**: Monitor "Since Last In" values to identify connection issues
2. **Packet Loss Detection**: Track resent packets to identify network problems
3. **Bandwidth Utilization**: Compare actual vs. target throttle settings
4. **Queue Congestion**: Identify bottlenecks through queue depth analysis

### Client Connection Analysis

1. **Root vs. Child Agents**: Distinguish primary connections from neighbor interactions
2. **Multi-Region Presence**: Track users across multiple regions
3. **Connection State**: Monitor active vs. idle connections

### Asset Transfer Monitoring

1. **Texture Download Performance**: Track image queue status and completion rates
2. **Priority-Based Processing**: Monitor how priority affects download order
3. **Asset Availability**: Identify missing or problematic assets

## Error Handling and Edge Cases

### Client Type Validation

```csharp
if (client is LLClientView llclient)
{
    // Safe to access LLClientView-specific functionality
}
else
{
    return "This command is only supported for LLClientView";
}
```

### Scene Presence Validation

```csharp
ScenePresence sp = scene.GetScenePresence(firstName, lastName);
if (sp is not null)
{
    foundAgents.Add(sp);
}
```

### Agent State Checking

```csharp
bool isChild = client.SceneAgent.IsChildAgent;
if (isChild && !showChildren)
    return;  // Skip child agents unless explicitly requested
```

## Performance Considerations

### Efficient Scene Iteration

```csharp
foreach (Scene scene in m_scenes.Values)
{
    scene.ForEachClient(delegate(IClientAPI client)
    {
        // Process each client efficiently
    });
}
```

### Memory Management

- Uses StringBuilder for efficient string building in reports
- Implements filtering to reduce data processing overhead
- Utilizes thread-safe collections for scene management

### Real-time Data Collection

- Collects live statistics without impacting client performance
- Uses existing client statistics infrastructure
- Minimizes additional overhead through efficient iteration

## Output Formatting and Presentation

### Column-based Layout

```csharp
protected string GetColumnEntry(string entry, int maxLength, int columnPadding)
{
    return string.Format(
        "{0,-" + maxLength +  "}{1,-" + columnPadding + "}",
        entry.Length > maxLength ? entry[..maxLength] : entry,
        "");
}
```

### Structured Reports

- Consistent column alignment across all commands
- Header rows for clarity
- Units specified where applicable (kb/s, milliseconds)
- Child/Root agent type indicators

### Data Filtering

- Optional full display including child agents
- User-specific filtering by name
- Region-specific information display

## Integration Points

### Console Command System

```csharp
scene.AddCommand(
    "Comms", this, "show pqueues",
    "show pqueues [full]",
    "Show priority queue data for each client",
    "Help text with usage details",
    (mod, cmd) => MainConsole.Instance.Output(GetPQueuesReport(cmd)));
```

### LLClientView Integration

- Direct access to UDP client statistics
- Entity update queue monitoring
- Image manager queue inspection
- Throttle configuration access

### Scene Management Integration

- Multi-region operation support
- Scene presence lookup
- Client enumeration across regions

## Use Cases and Applications

### Network Performance Monitoring

- **Bandwidth Analysis**: Monitor throttle effectiveness and utilization
- **Latency Tracking**: Identify high-latency clients or network issues
- **Packet Loss Investigation**: Track resent packets to diagnose connectivity problems

### Client Troubleshooting

- **Connection Issues**: Diagnose clients with high latency or packet loss
- **Asset Download Problems**: Investigate texture loading failures
- **Performance Optimization**: Identify clients consuming excessive bandwidth

### Grid Administration

- **Capacity Planning**: Monitor overall network utilization patterns
- **Load Balancing**: Identify regions with high client loads
- **Quality of Service**: Ensure fair bandwidth distribution

### Development and Testing

- **Protocol Debugging**: Inspect UDP stack behavior during development
- **Performance Testing**: Monitor system behavior under load
- **Feature Validation**: Verify throttling and queue management effectiveness

## Dependencies

### Core Framework Dependencies

- `OpenSim.Framework` - Core data structures and interfaces
- `OpenSim.Region.Framework.Interfaces` - Module interface contracts
- `OpenSim.Region.Framework.Scenes` - Scene management and client access
- `OpenSim.Region.ClientStack.LindenUDP` - UDP client stack integration

### System Dependencies

- `System.Collections.Generic` - Collection management
- `System.Linq` - LINQ operations for data processing
- `System.Text` - StringBuilder for efficient string operations
- `log4net` - Logging framework

### Client Stack Dependencies

- LLClientView for UDP client access
- IStatsCollector for statistics gathering
- J2KImage for image queue inspection
- ClientInfo for throttle and connection data

## Troubleshooting

### Common Issues

1. **Commands Not Available**
   - Verify module is loaded in factory
   - Check that scenes are properly registered
   - Ensure console has access to scene commands

2. **No Data Displayed**
   - Verify clients are connected using LLClientView
   - Check that target users exist and are online
   - Ensure proper agent type filtering (root vs. child)

3. **Incomplete Statistics**
   - Verify UDP client stack is functioning properly
   - Check that statistics collection is enabled
   - Ensure clients support the IStatsCollector interface

### Debug Commands

```bash
# Test basic functionality
show pqueues

# Check specific user
show queues TestUser Smith

# Inspect image downloads
show image queues TestUser Smith full

# Monitor throttle settings
show throttles full
```

### Log Analysis

Monitor module initialization and operation:
```
[LindenUDPInfoModule]: INITIALIZED MODULE
[LindenUDPInfoModule]: REGION MainRegion ADDED
[LindenUDPInfoModule]: REGION MainRegion LOADED
```

## Future Enhancement Opportunities

### Advanced Features

- **Historical Statistics**: Track performance trends over time
- **Automated Alerts**: Trigger alerts for performance thresholds
- **Export Capabilities**: Export statistics to external monitoring systems
- **Real-time Dashboards**: Web-based monitoring interfaces

### Performance Improvements

- **Cached Statistics**: Cache frequently accessed data
- **Filtered Collection**: Collect only relevant statistics
- **Asynchronous Reporting**: Non-blocking report generation
- **Configurable Refresh**: Adjustable update intervals

### Integration Enhancements

- **Metrics Integration**: Integration with metrics collection systems
- **API Endpoints**: REST API for programmatic access
- **Database Logging**: Store statistics for historical analysis
- **External Monitoring**: Integration with network monitoring tools

## Conclusion

The LindenUDPInfoModule provides essential diagnostic and monitoring capabilities for OpenSimulator's UDP client stack. Its comprehensive console commands, real-time statistics collection, and detailed reporting make it an invaluable tool for network administrators, developers, and grid operators who need to monitor and troubleshoot client connectivity, performance, and asset transfer issues. The module's integration with the factory system ensures it's automatically available in all OpenSimulator deployments without requiring additional configuration.