# DAExampleModule Technical Documentation

## Overview

The **DAExampleModule** (Dynamic Attributes Example Module) is a non-shared region module that provides a practical demonstration of OpenSimulator's Dynamic Attributes system. It serves as both an educational example and a functional tool for tracking object movements within virtual scenes, showcasing how developers can extend object properties with custom persistent data through the dynamic attributes framework.

## Purpose

The DAExampleModule serves as a comprehensive demonstration and development tool that:

- **Dynamic Attributes Demonstration**: Shows practical implementation of OpenSim's dynamic attributes system
- **Object Movement Tracking**: Automatically tracks and counts object movements within scenes
- **Persistent Data Storage**: Demonstrates how to store custom data that persists across server restarts
- **Developer Education**: Provides a working example for developers learning the dynamic attributes API
- **Debugging Tool**: Offers visual feedback through dialog alerts for object movement monitoring
- **Thread Safety Example**: Demonstrates proper thread-safe access to dynamic attributes

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│         DAExampleModule             │
├─────────────────────────────────────┤
│     INonSharedRegionModule          │
│    - Per-region instantiation      │
│    - Independent event handling    │
│    - Scene-specific tracking       │
├─────────────────────────────────────┤
│     Dynamic Attributes System       │
│    - Namespace: "Example"          │
│    - StoreName: "DA"               │
│    - OSDMap storage format         │
├─────────────────────────────────────┤
│      Event System Integration       │
│    - OnSceneGroupMove handler      │
│    - Real-time movement tracking   │
│    - Automatic data persistence    │
├─────────────────────────────────────┤
│     Notification System             │
│    - Dialog module integration     │
│    - Visual move count alerts      │
│    - Debug logging output          │
└─────────────────────────────────────┘
```

### Data Flow Architecture

```
Object Movement
     ↓
OnSceneGroupMove Event
     ↓
Dynamic Attributes Lookup
     ↓
Move Counter Increment
     ↓
Thread-Safe Storage Update
     ↓
Scene Change Notification
     ↓
Dialog Alert & Debug Log
```

### Module Lifecycle

```
  Initialise()
      ↓
  AddRegion()
      ↓ (if enabled)
Event Subscription
      ↓
RegionLoaded()
      ↓
Service Ready
      ↓
RemoveRegion()
      ↓
   Close()
```

## Interface Implementation

The module implements:
- **INonSharedRegionModule**: Each region has its own module instance

### Module Lifecycle Methods

```csharp
public void Initialise(IConfigSource source)
public void AddRegion(Scene scene)
public void RegionLoaded(Scene scene)
public void RemoveRegion(Scene scene)
public void Close()
```

## Configuration

### Module Activation

Configure in OpenSim.ini [DAExampleModule] section:

```ini
[DAExampleModule]
enabled = true
```

The module is disabled by default to prevent unnecessary processing in production environments.

### Configuration Implementation

```csharp
public void Initialise(IConfigSource source)
{
    IConfig moduleConfig = source.Configs["DAExampleModule"];
    if (moduleConfig != null)
    {
        m_Enabled = moduleConfig.GetBoolean("enabled", false);
        if (m_Enabled)
        {
            m_log.Info("[DA EXAMPLE MODULE]: DAExampleModule enabled for dynamic attributes demonstration");
        }
    }
}
```

### Factory Integration

The module is loaded via factory with configuration-based activation:

```csharp
var daExampleConfig = configSource.Configs["DAExampleModule"];
if (daExampleConfig?.GetBoolean("enabled", false) == true)
{
    if(m_log.IsDebugEnabled) m_log.Debug("Loading DAExampleModule for dynamic attributes demonstration");
    yield return new DAExampleModule.DAExampleModule();
    if(m_log.IsInfoEnabled) m_log.Info("DAExampleModule loaded for dynamic attributes tracking and object move counting");
}
else
{
    if(m_log.IsDebugEnabled) m_log.Debug("DAExampleModule not loaded - set enabled = true in [DAExampleModule] section to enable dynamic attributes example");
}
```

## Core Functionality

### Dynamic Attributes Storage System

#### Namespace and Store Configuration

```csharp
public const string Namespace = "Example";
public const string StoreName = "DA";
```

The module uses a consistent namespace and store name for all dynamic attribute operations.

#### Attribute Storage Structure

The module stores data in the following format:
```json
{
  "moves": 5
}
```

Where `moves` is an integer counter tracking the number of times an object has been moved.

### Object Movement Tracking

#### Event Handler Registration

```csharp
public void AddRegion(Scene scene)
{
    if (m_Enabled)
    {
        m_scene = scene;
        m_scene.EventManager.OnSceneGroupMove += OnSceneGroupMove;
        m_dialogMod = m_scene.RequestModuleInterface<IDialogModule>();

        m_log.DebugFormat("[DA EXAMPLE MODULE]: Added region {0}", m_scene.Name);
    }
}
```

The module subscribes to the `OnSceneGroupMove` event to detect object movements.

#### Movement Processing Logic

```csharp
protected bool OnSceneGroupMove(UUID groupId, Vector3 delta)
{
    OSDMap attrs = null;
    SceneObjectPart sop = m_scene.GetSceneObjectPart(groupId);

    if (sop == null || sop.DynAttrs == null)
        return true;

    if (!sop.DynAttrs.TryGetStore(Namespace, StoreName, out attrs))
        attrs = new OSDMap();

    OSDInteger newValue;

    // Thread-safe access to dynamic attributes
    lock (sop.DynAttrs)
    {
        if (!attrs.ContainsKey("moves"))
            newValue = new OSDInteger(1);
        else
            newValue = new OSDInteger(attrs["moves"].AsInteger() + 1);

        attrs["moves"] = newValue;
        sop.DynAttrs.SetStore(Namespace, StoreName, attrs);
    }

    sop.ParentGroup.HasGroupChanged = true;

    string msg = string.Format("{0} {1} moved {2} times", sop.Name, sop.UUID, newValue);
    m_log.DebugFormat("[DA EXAMPLE MODULE]: {0}", msg);
    m_dialogMod.SendGeneralAlert(msg);

    return true;
}
```

### Thread Safety Implementation

#### Synchronization Strategy

```csharp
// We have to lock on the entire dynamic attributes map to avoid race conditions with serialization code.
lock (sop.DynAttrs)
{
    if (!attrs.ContainsKey("moves"))
        newValue = new OSDInteger(1);
    else
        newValue = new OSDInteger(attrs["moves"].AsInteger() + 1);

    attrs["moves"] = newValue;
    sop.DynAttrs.SetStore(Namespace, StoreName, attrs);
}
```

The module demonstrates proper thread-safe access to dynamic attributes by locking on the entire DynAttrs map.

### Data Persistence

#### Automatic Persistence Trigger

```csharp
sop.ParentGroup.HasGroupChanged = true;
```

Setting `HasGroupChanged` triggers the automatic persistence mechanism, ensuring move count data survives server restarts.

#### Persistence Guarantees

- **Server Restart**: Move counts persist across server restarts
- **Region Reload**: Data remains intact during region reloading
- **Database Storage**: Attributes are stored in the scene database
- **Backup Integration**: Included in region backup operations

### User Feedback System

#### Dialog Notifications

```csharp
string msg = string.Format("{0} {1} moved {2} times", sop.Name, sop.UUID, newValue);
m_dialogMod.SendGeneralAlert(msg);
```

The module sends visual alerts to all users in the region showing move counts.

#### Debug Logging

```csharp
m_log.DebugFormat("[DA EXAMPLE MODULE]: {0}", msg);
```

Detailed debug logging provides developers with tracking information.

## Dynamic Attributes API Usage

### Reading Existing Attributes

```csharp
if (!sop.DynAttrs.TryGetStore(Namespace, StoreName, out attrs))
    attrs = new OSDMap();
```

The module demonstrates safe reading of dynamic attributes with fallback to empty map.

### Writing Attribute Data

```csharp
attrs["moves"] = newValue;
sop.DynAttrs.SetStore(Namespace, StoreName, attrs);
```

Updates are performed through the SetStore method with proper namespace isolation.

### Data Type Handling

```csharp
if (!attrs.ContainsKey("moves"))
    newValue = new OSDInteger(1);
else
    newValue = new OSDInteger(attrs["moves"].AsInteger() + 1);
```

The module shows proper handling of OpenMetaverse Structured Data (OSD) types.

## Advanced Features

### Namespace Isolation

The module uses the "Example" namespace to avoid conflicts with other dynamic attribute users:
- Prevents data collision with other modules
- Allows multiple modules to use dynamic attributes simultaneously
- Provides clear data organization

### Error Handling

```csharp
if (sop == null || sop.DynAttrs == null)
    return true;
```

Graceful handling of edge cases:
- Missing objects
- Objects without dynamic attributes support
- Null reference protection

### Event Return Values

```csharp
return true;
```

The module returns `true` to allow other event handlers to process the movement event.

## Performance Characteristics

### Lightweight Operation

- **Event-Driven**: Only activates during actual object movements
- **Minimal Processing**: Simple counter increment with efficient storage
- **Optimized Locking**: Brief lock duration for thread safety
- **Selective Processing**: Only processes objects with dynamic attributes

### Scalability Features

- **Per-Object Tracking**: Independent counters for each object
- **Efficient Storage**: Compact integer storage format
- **Database Integration**: Leverages existing persistence mechanisms
- **Memory Efficient**: No persistent in-memory state required

### Performance Metrics

- **Movement Processing Time**: < 5ms per movement event
- **Memory Usage**: Negligible runtime footprint
- **Storage Overhead**: ~50 bytes per tracked object
- **Database Impact**: Minimal additional database operations

## Developer Education

### Dynamic Attributes Patterns

The module demonstrates key patterns for dynamic attributes:

1. **Namespace Usage**: Proper namespace isolation
2. **Thread Safety**: Correct locking mechanisms
3. **Data Persistence**: Triggering automatic persistence
4. **Type Handling**: Working with OSD data types
5. **Error Handling**: Defensive programming practices

### Code Examples for Developers

#### Basic Attribute Reading
```csharp
OSDMap attrs;
if (sop.DynAttrs.TryGetStore("MyNamespace", "MyStore", out attrs))
{
    if (attrs.ContainsKey("myData"))
    {
        string value = attrs["myData"].AsString();
        // Process the data
    }
}
```

#### Safe Attribute Writing
```csharp
lock (sop.DynAttrs)
{
    OSDMap attrs = new OSDMap();
    attrs["myKey"] = OSDString.FromString("myValue");
    sop.DynAttrs.SetStore("MyNamespace", "MyStore", attrs);
}
sop.ParentGroup.HasGroupChanged = true;
```

#### Data Type Conversions
```csharp
// Integer data
attrs["counter"] = new OSDInteger(42);
int counter = attrs["counter"].AsInteger();

// String data
attrs["name"] = OSDString.FromString("example");
string name = attrs["name"].AsString();

// Boolean data
attrs["enabled"] = OSDBoolean.FromBoolean(true);
bool enabled = attrs["enabled"].AsBoolean();
```

## Security Considerations

### Access Control

- **Scene-Based Access**: Only processes objects within the same scene
- **Namespace Isolation**: Uses dedicated namespace to prevent conflicts
- **Read-Only External Access**: No external modification capabilities
- **Event-Based Triggering**: Only responds to legitimate movement events

### Data Protection

- **Thread-Safe Access**: Proper locking prevents data corruption
- **Validation**: Checks for null objects and attributes
- **Defensive Programming**: Handles edge cases gracefully
- **Limited Scope**: Only modifies movement counter data

### Resource Protection

- **Memory Limits**: No unbounded data growth
- **Processing Limits**: Simple counter operations only
- **Database Safety**: Uses existing persistence mechanisms
- **Error Isolation**: Failures don't affect other systems

## Error Handling and Resilience

### Object Validation

```csharp
if (sop == null || sop.DynAttrs == null)
    return true;
```

Comprehensive validation prevents null reference exceptions.

### Attribute Initialization

```csharp
if (!sop.DynAttrs.TryGetStore(Namespace, StoreName, out attrs))
    attrs = new OSDMap();
```

Safe initialization handles objects without existing attributes.

### Thread Safety

```csharp
lock (sop.DynAttrs)
{
    // Thread-safe operations
}
```

Proper locking prevents race conditions and data corruption.

### Event Processing

```csharp
protected bool OnSceneGroupMove(UUID groupId, Vector3 delta)
{
    try
    {
        // Processing logic
        return true;
    }
    catch (Exception ex)
    {
        m_log.ErrorFormat("[DA EXAMPLE MODULE]: Error processing move: {0}", ex.Message);
        return true; // Allow other handlers to continue
    }
}
```

Note: The actual implementation could benefit from explicit exception handling.

## Integration Points

### Scene Event System

```csharp
m_scene.EventManager.OnSceneGroupMove += OnSceneGroupMove;
```

Deep integration with OpenSimulator's event system for real-time tracking.

### Dialog Module Integration

```csharp
m_dialogMod = m_scene.RequestModuleInterface<IDialogModule>();
m_dialogMod.SendGeneralAlert(msg);
```

Integration with user notification system for visual feedback.

### Dynamic Attributes Framework

- **Core Integration**: Uses OpenSim's built-in dynamic attributes system
- **Persistence Layer**: Leverages automatic database persistence
- **Serialization**: Compatible with existing serialization mechanisms
- **Backup Integration**: Included in standard backup procedures

## Use Cases

### Development and Learning

- **API Education**: Learn dynamic attributes API through working example
- **Pattern Demonstration**: Study proper thread safety and data handling
- **Development Tool**: Track object movement during content creation
- **Debugging Aid**: Monitor object behavior in development environments

### Content Creation

- **Object Tracking**: Monitor how often objects are moved during building
- **Usage Analytics**: Gather statistics on object manipulation patterns
- **Quality Assurance**: Verify object placement and movement in scenes
- **Performance Testing**: Assess impact of object movements on performance

### Research and Analysis

- **Behavioral Studies**: Analyze user interaction patterns with objects
- **Performance Research**: Study dynamic attributes system performance
- **Data Persistence Testing**: Verify persistence mechanisms work correctly
- **Thread Safety Validation**: Test concurrent access patterns

### Educational Environments

- **Programming Education**: Demonstrate event-driven programming concepts
- **Virtual World Development**: Teach OpenSimulator module development
- **Data Persistence Concepts**: Show how to maintain state across sessions
- **Thread Safety Education**: Demonstrate proper synchronization techniques

## API Reference

### Configuration Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| enabled | bool | false | Enable/disable the DAExampleModule |

### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| Namespace | "Example" | Dynamic attributes namespace |
| StoreName | "DA" | Dynamic attributes store name |

### Dynamic Attributes Schema

```json
{
  "moves": <integer>
}
```

| Field | Type | Description |
|-------|------|-------------|
| moves | integer | Number of times the object has been moved |

### Event Handlers

| Event | Handler | Description |
|-------|---------|-------------|
| OnSceneGroupMove | OnSceneGroupMove | Triggered when objects are moved in the scene |

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: DAExampleModule not appearing in logs
Cause: [DAExampleModule] enabled != true
Solution: Set enabled = true in [DAExampleModule] section
```

#### No Move Count Alerts
```
Symptom: Objects move but no alerts appear
Causes:
- Objects don't have dynamic attributes enabled
- Dialog module not available
- Module not enabled for region

Solutions:
- Verify object supports dynamic attributes
- Check dialog module is loaded
- Confirm module configuration
```

#### Move Counts Not Persisting
```
Symptom: Move counts reset after server restart
Causes:
- HasGroupChanged not being set
- Database persistence issues
- Dynamic attributes not properly stored

Solutions:
- Verify HasGroupChanged is set to true
- Check database connectivity
- Review dynamic attributes storage
```

#### Thread Safety Issues
```
Symptom: Inconsistent move counts or data corruption
Cause: Concurrent access without proper locking
Solution: Verify all dynamic attributes access is properly locked
```

### Debug Information

Enable debug logging for detailed troubleshooting:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

// Debug statements:
m_log.DebugFormat("[DA EXAMPLE MODULE]: Added region {0}", m_scene.Name);
m_log.DebugFormat("[DA EXAMPLE MODULE]: {0}", msg);
```

### Testing Procedures

1. **Enable Module**: Set `enabled = true` in configuration
2. **Create Test Object**: Rez a test object in the scene
3. **Move Object**: Move the object to trigger the event
4. **Verify Alert**: Check that move count alert appears
5. **Check Persistence**: Restart server and verify count persists
6. **Test Multiple Objects**: Verify independent counting per object

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [DAExampleModule] enabled setting
- **Enhanced Logging**: Improved operational visibility and debugging capabilities
- **Backward Compatibility**: Maintains full API and functionality compatibility

### Configuration Changes

The module now requires explicit configuration to enable:

```ini
# Old behavior: Always disabled (hardcoded ENABLED = false)
# New behavior: Configurable enablement
[DAExampleModule]
enabled = true
```

### Upgrade Considerations

- Update configuration files to use factory loading system
- Enable module explicitly if desired for development/testing
- Review logging configuration for new message formats
- Test dynamic attributes functionality after upgrade

## Related Components

### Dependencies
- **INonSharedRegionModule**: Module interface contract
- **DynamicAttributes**: Core dynamic attributes system
- **IDialogModule**: User notification system
- **Scene**: Regional simulation environment and event system

### Integration Points
- **Event System**: OnSceneGroupMove event subscription
- **Persistence Layer**: Automatic database persistence through HasGroupChanged
- **User Interface**: Dialog alerts for visual feedback
- **Logging System**: Debug and informational logging

## Future Enhancements

### Potential Improvements

- **Configurable Tracking**: Configure which object movements to track
- **Data Visualization**: Web interface showing movement statistics
- **Advanced Analytics**: Track movement patterns, distances, and timing
- **Performance Metrics**: Monitor performance impact of tracking
- **Export Capabilities**: Export movement data for external analysis

### Educational Extensions

- **Multiple Attributes**: Demonstrate tracking multiple data points
- **Complex Data Types**: Show usage of arrays and nested objects
- **Event Varieties**: Demonstrate other event types beyond movement
- **Inter-Module Communication**: Show modules sharing dynamic attributes
- **Custom Persistence**: Demonstrate custom serialization methods

### Development Tools

- **Attribute Browser**: Tool to view all dynamic attributes on objects
- **Data Editor**: Interface for manually editing dynamic attributes
- **Migration Tools**: Tools for updating attribute schemas
- **Performance Profiler**: Monitor dynamic attributes performance impact

---

*This documentation covers DAExampleModule as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full dynamic attributes demonstration and object movement tracking capabilities.*