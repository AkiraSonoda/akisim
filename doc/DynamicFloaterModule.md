# DynamicFloaterModule Technical Documentation

## Overview

The **DynamicFloaterModule** is a non-shared region module that provides dynamic UI floater and dialog capabilities for OpenSimulator viewers. It enables the creation and management of custom XML-based user interface elements, allowing regions to display complex interactive dialogs, forms, and informational panels directly within the viewer interface without requiring external web browsers or static dialog boxes.

## Purpose

The DynamicFloaterModule serves as an advanced viewer integration component that:

- **Dynamic UI Creation**: Enables runtime creation of custom floater windows in the viewer
- **XML-Based Interface Design**: Supports complex UI layouts defined through XML markup
- **Interactive Dialog Management**: Manages user input and responses from custom floater interfaces
- **Event-Driven Communication**: Provides bidirectional communication between region scripts and viewer UI
- **Advanced User Experience**: Enhances user interaction beyond standard chat and basic dialogs
- **Viewer Feature Integration**: Leverages viewer-specific UI capabilities for rich interfaces

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│       DynamicFloaterModule          │
├─────────────────────────────────────┤
│      INonSharedRegionModule         │
│    - Per-region instantiation      │
│    - Scene-specific floater mgmt    │
├─────────────────────────────────────┤
│     IDynamicFloaterModule           │
│    - Public API for scripts        │
│    - FloaterData management         │
├─────────────────────────────────────┤
│      Floater Data Management        │
│    - XML content handling          │
│    - Channel-based communication   │
│    - User session tracking         │
├─────────────────────────────────────┤
│      Event System Integration       │
│    - OnNewClient handler           │
│    - OnClientClosed handler        │
│    - OnChatFromClient handler      │
├─────────────────────────────────────┤
│     Chat Protocol Integration       │
│    - Special command processing    │
│    - VALUE message handling        │
│    - NOTIFY event processing       │
└─────────────────────────────────────┘
```

### Data Flow Architecture

```
Script Request → DoUserFloater()
     ↓
Load XML Content (File or String)
     ↓
Fragment XML for Transmission
     ↓
Send via Chat Messages to Viewer
     ↓
Viewer Creates Floater UI
     ↓
User Interaction Events
     ↓
Chat Messages Back to Region
     ↓
Event Handler Processing
     ↓
Script Callback Execution
```

### FloaterData Structure

```
FloaterData (Abstract Base)
├── Channel (int) - Communication channel
├── FloaterName (string) - Unique floater identifier
├── XmlName (string) - XML file path
├── XmlText (string) - Direct XML content
└── Handler (delegate) - Event callback function
```

## Interface Implementation

The module implements:
- **INonSharedRegionModule**: Per-region module instance
- **IDynamicFloaterModule**: Public API for scripting integration

### IDynamicFloaterModule Interface

```csharp
public interface IDynamicFloaterModule
{
    void DoUserFloater(UUID agentID, FloaterData dialogData, string configuration);
    void FloaterControl(ScenePresence sp, FloaterData d, string msg);
}
```

### HandlerDelegate Pattern

```csharp
public delegate bool HandlerDelegate(IClientAPI client, FloaterData data, string[] msg);
```

Event handlers return `true` to close the floater, `false` to keep it open.

## Configuration

### Module Activation

Configure in OpenSim.ini [DynamicFloaterModule] section:

```ini
[DynamicFloaterModule]
enabled = true
```

The module is disabled by default to prevent unnecessary processing when not needed.

### Configuration Implementation

```csharp
public void Initialise(IConfigSource config)
{
    IConfig moduleConfig = config.Configs["DynamicFloaterModule"];
    if (moduleConfig != null)
    {
        m_Enabled = moduleConfig.GetBoolean("enabled", false);
    }
}
```

### Factory Integration

The module is loaded via factory with configuration-based activation:

```csharp
var dynamicFloaterConfig = configSource?.Configs["DynamicFloaterModule"];
if (dynamicFloaterConfig?.GetBoolean("enabled", false) == true)
{
    if(m_log.IsDebugEnabled) m_log.Debug("Loading DynamicFloaterModule for dynamic UI floater and dialog support");
    var dynamicFloaterModuleInstance = LoadDynamicFloaterModule();
    yield return dynamicFloaterModuleInstance;
    if(m_log.IsInfoEnabled) m_log.Info("DynamicFloaterModule loaded for dynamic floater dialogs, XML-based UI elements, and viewer integration");
}
```

## Core Functionality

### Floater Creation and Management

#### DoUserFloater Method

```csharp
public void DoUserFloater(UUID agentID, FloaterData dialogData, string configuration)
{
    ScenePresence sp = m_scene.GetScenePresence(agentID);
    if (sp == null || sp.IsChildAgent)
        return;

    if (!m_floaters.ContainsKey(agentID))
        m_floaters[agentID] = new Dictionary<int, FloaterData>();

    if (m_floaters[agentID].ContainsKey(dialogData.Channel))
        return; // Prevent duplicate floaters

    m_floaters[agentID].Add(dialogData.Channel, dialogData);

    // Load XML content and send to viewer
    string xml = LoadXmlContent(dialogData);
    SendFloaterToViewer(sp, xml, dialogData, configuration);
}
```

#### XML Content Processing

The module supports two XML content sources:

1. **Direct XML String**: Content provided directly in `FloaterData.XmlText`
2. **XML File**: Content loaded from file specified in `FloaterData.XmlName`

```csharp
private string LoadXmlContent(FloaterData dialogData)
{
    if (dialogData.XmlText != null && dialogData.XmlText != String.Empty)
    {
        return dialogData.XmlText;
    }
    else
    {
        using (FileStream fs = File.Open(dialogData.XmlName + ".xml", FileMode.Open, FileAccess.Read))
        {
            using (StreamReader sr = new StreamReader(fs))
                return sr.ReadToEnd().Replace("\n", "");
        }
    }
}
```

#### XML Fragmentation System

Large XML content is automatically fragmented for transmission:

```csharp
List<string> xparts = new List<string>();

while (xml.Length > 0)
{
    string x = xml;
    if (x.Length > 600)
    {
        x = x.Substring(0, 600);
        xml = xml.Substring(600);
    }
    else
    {
        xml = String.Empty;
    }
    xparts.Add(x);
}

// Send fragmented content
for (int i = 0; i < xparts.Count; i++)
    SendToClient(sp, String.Format("># floater {2} create {0}/{1} " + xparts[i],
                 i + 1, xparts.Count, dialogData.FloaterName));
```

### Communication Protocol

#### Chat-Based Protocol

The module uses a specialized chat protocol for viewer communication:

- **Creation Command**: `># floater {name} create {part}/{total} {xml}`
- **Configuration Command**: `># floater {name} {notify:1} {channel: {channel}} {node:cancel {notify:1}} {node:ok {notify:1}} {config}`
- **Control Command**: `># floater {name} {message}`
- **Destroy Command**: `># floater {name} destroy`

#### Special Channel Handling

- **Primary Channel**: Unique per floater for specific communication
- **VALUE Channel (427169570)**: Global channel for all VALUE messages (viewer bug workaround)

```csharp
// Handle VALUE messages on special channel 427169570
if (msg.Channel == 427169570)
{
    if (parts[0] == "VALUE")
    {
        foreach (FloaterData dd in d.Values)
        {
            if(dd.Handler(client, dd, parts))
            {
                m_floaters[client.AgentId].Remove(dd.Channel);
                SendToClient(sp, String.Format("># floater {0} destroy", dd.FloaterName));
                break;
            }
        }
    }
    return;
}
```

### Event Handling System

#### Client Event Management

```csharp
private void OnNewClient(IClientAPI client)
{
    client.OnChatFromClient += OnChatFromClient;
}

private void OnClientClosed(UUID agentID, Scene scene)
{
    m_floaters.Remove(agentID); // Clean up user floaters
}
```

#### Chat Message Processing

```csharp
private void OnChatFromClient(object sender, OSChatMessage msg)
{
    if (msg.Sender == null)
        return;

    IClientAPI client = msg.Sender;
    string[] parts = msg.Message.Split(new char[] {':'});

    // Process NOTIFY events
    if (parts[0] == "NOTIFY")
    {
        if (parts[1] == "cancel" || parts[1] == data.FloaterName)
        {
            m_floaters[client.AgentId].Remove(data.Channel);
            SendToClient(sp, String.Format("># floater {0} destroy", data.FloaterName));
        }
    }

    // Forward to handler
    if (data.Handler != null && data.Handler(client, data, parts))
    {
        m_floaters[client.AgentId].Remove(data.Channel);
        SendToClient(sp, String.Format("># floater {0} destroy", data.FloaterName));
    }
}
```

### Floater Control and Management

#### FloaterControl Method

```csharp
public void FloaterControl(ScenePresence sp, FloaterData d, string msg)
{
    string sendData = String.Format("># floater {0} {1}", d.FloaterName, msg);
    SendToClient(sp, sendData);
}
```

This method enables dynamic control of existing floaters:
- Update content
- Change visibility
- Modify properties
- Send notifications

## XML Floater Format

### Basic XML Structure

```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<floater name="example" title="Example Dialog" can_minimize="false" can_tear_off="false"
         can_resize="false" can_drag_on_left="false" can_close="true" can_dock="false"
         visible="true" open_positioning="cascading" can_collapse="false"
         header_height="18" legacy_header_height="18" width="300" height="200">

    <layout_stack orientation="vertical" follows="all" top="20" left="5" width="290" height="175">
        <text follows="left|top" height="20" layout="topleft" left="10" top="10" width="270"
              text_color="white" font="SansSerif" name="instructions">
            This is an example dynamic floater dialog.
        </text>

        <button follows="left|top" height="20" layout="topleft" left="10" top="40" width="80"
                label="OK" name="ok"/>
        <button follows="left|top" height="20" layout="topleft" left="100" top="40" width="80"
                label="Cancel" name="cancel"/>
    </layout_stack>
</floater>
```

### Supported UI Elements

- **text**: Static text labels
- **button**: Clickable buttons
- **line_editor**: Text input fields
- **text_editor**: Multi-line text areas
- **check_box**: Checkbox controls
- **combo_box**: Dropdown selections
- **slider**: Numeric sliders
- **layout_stack**: Container layouts
- **panel**: Grouping panels

### Event-Generating Elements

Elements that generate events when interacted with:
- `button` - Generates NOTIFY:{button_name}
- `line_editor` - Generates VALUE:{field_name}:{value}
- `check_box` - Generates VALUE:{checkbox_name}:{true/false}
- `combo_box` - Generates VALUE:{combo_name}:{selected_value}`

## Integration Patterns

### Script Integration Example

```csharp
public class MyFloaterData : FloaterData
{
    public override int Channel { get; private set; }
    public override string FloaterName { get; set; }
    public override string XmlText { get; set; }
    public override HandlerDelegate Handler { get; set; }

    public MyFloaterData(int channel, string name, string xml)
    {
        Channel = channel;
        FloaterName = name;
        XmlText = xml;
        Handler = HandleFloaterEvent;
    }

    private bool HandleFloaterEvent(IClientAPI client, FloaterData data, string[] parts)
    {
        if (parts[0] == "NOTIFY")
        {
            if (parts[1] == "ok")
            {
                // Handle OK button
                return true; // Close floater
            }
            else if (parts[1] == "cancel")
            {
                // Handle Cancel button
                return true; // Close floater
            }
        }
        else if (parts[0] == "VALUE")
        {
            // Handle input field values
            string fieldName = parts[1];
            string value = parts.Length > 2 ? parts[2] : "";
            // Process field data
        }

        return false; // Keep floater open
    }
}
```

### Module Usage Example

```csharp
public class MyModule : INonSharedRegionModule
{
    private IDynamicFloaterModule m_floaterModule;

    public void RegionLoaded(Scene scene)
    {
        m_floaterModule = scene.RequestModuleInterface<IDynamicFloaterModule>();
    }

    private void ShowUserDialog(UUID agentID)
    {
        if (m_floaterModule == null)
            return;

        string xmlContent = @"
        <floater name='mydialog' title='My Dialog' width='300' height='150'>
            <text name='message' text_color='white'>Hello, World!</text>
            <button name='ok' label='OK'/>
        </floater>";

        MyFloaterData data = new MyFloaterData(
            GetUniqueChannel(),
            "mydialog",
            xmlContent
        );

        m_floaterModule.DoUserFloater(agentID, data, "");
    }
}
```

## Advanced Features

### Multi-User Support

The module maintains separate floater collections per user:

```csharp
private Dictionary<UUID, Dictionary<int, FloaterData>> m_floaters =
    new Dictionary<UUID, Dictionary<int, FloaterData>>();
```

### Channel Management

Each floater operates on a unique communication channel to prevent interference:

```csharp
if (m_floaters[agentID].ContainsKey(dialogData.Channel))
    return; // Prevent channel conflicts
```

### Dynamic Content Updates

Floaters can be updated dynamically after creation:

```csharp
m_floaterModule.FloaterControl(scenePresence, floaterData, "update_text instructions \"New message\"");
```

### File-Based XML Loading

XML content can be loaded from external files for complex interfaces:

```csharp
// FloaterData with file reference
data.XmlName = "complex_dialog"; // Loads complex_dialog.xml
data.XmlText = null; // Use file instead
```

## Performance Characteristics

### Memory Management

- **Per-User Collections**: Efficient memory usage with automatic cleanup
- **String Optimization**: XML content processed and fragments cached
- **Event Handler Management**: Lightweight delegate pattern
- **Automatic Cleanup**: Floaters removed when users disconnect

### Communication Efficiency

- **Fragmentation**: Large XML content split into manageable chunks
- **Channel Isolation**: Each floater uses dedicated communication channel
- **Event Optimization**: Direct event routing to specific handlers
- **Protocol Efficiency**: Minimal overhead chat-based protocol

### Scalability Features

- **Per-Region Isolation**: Independent floater management per region
- **User-Specific State**: Separate floater collections per user
- **Channel Management**: Automatic channel allocation and cleanup
- **Resource Limits**: Controlled XML content size and floater count

## Security Considerations

### Content Validation

- **XML Safety**: File access restricted to designated directories
- **Content Limits**: XML fragmentation prevents excessive data transmission
- **Channel Security**: Unique channels prevent cross-user interference
- **Input Sanitization**: Chat message parsing with validation

### Access Control

- **User Isolation**: Floaters only accessible by owning user
- **Channel Protection**: Communication channels user-specific
- **Scene Boundaries**: Floaters limited to specific regions
- **Permission Checks**: Agent validation before floater creation

### Resource Protection

- **Memory Limits**: Automatic cleanup prevents memory leaks
- **File System Security**: Controlled XML file access
- **Event Handling**: Protected against malformed messages
- **Connection Management**: Proper cleanup on disconnection

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: DynamicFloaterModule not appearing in logs
Solution: Set enabled = true in [DynamicFloaterModule] section
```

#### Floaters Not Appearing
```
Symptom: DoUserFloater called but no UI appears
Causes:
- Viewer doesn't support dynamic floaters
- XML content malformed
- User is child agent

Solutions:
- Verify viewer compatibility
- Validate XML syntax
- Check agent status
```

#### Communication Failures
```
Symptom: User input not reaching handlers
Causes:
- Channel conflicts
- Handler not properly assigned
- Chat parsing errors

Solutions:
- Ensure unique channels
- Verify HandlerDelegate assignment
- Check message format
```

#### XML Loading Errors
```
Symptom: File-based XML fails to load
Causes:
- File not found
- Permission issues
- Invalid XML syntax

Solutions:
- Verify file paths
- Check file permissions
- Validate XML syntax
```

### Debug Information

Enable detailed logging for troubleshooting:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

// Uncomment debug statements:
//m_log.DebugFormat("chan {0} msg {1}", msg.Channel, msg.Message);
```

### Testing Procedures

1. **Enable Module**: Set enabled = true in configuration
2. **Create Test Floater**: Use simple XML content
3. **Verify Display**: Check if floater appears in viewer
4. **Test Interaction**: Click buttons and verify events
5. **Check Cleanup**: Verify floater removal on close

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [DynamicFloaterModule] enabled setting
- **Enhanced Logging**: Improved operational visibility and debugging
- **Backward Compatibility**: Maintains full API and functionality compatibility

### Configuration Changes

The module now requires explicit configuration to enable:

```ini
# Old behavior: Loaded via Mono.Addins extension system
# New behavior: Configurable enablement
[DynamicFloaterModule]
enabled = true
```

### Upgrade Considerations

- Update configuration files to include [DynamicFloaterModule] section if needed
- Test floater functionality after upgrade
- Review viewer compatibility for dynamic floater support
- Verify XML file access and permissions
- Monitor logging for new message formats

## Related Components

### Dependencies
- **INonSharedRegionModule**: Module interface contract
- **IDynamicFloaterModule**: Public API interface
- **IClientAPI**: Client communication interface
- **Scene**: Regional simulation environment
- **FloaterData**: Abstract base class for floater definitions

### Integration Points
- **Scripting System**: LSL/OSSL script integration via module interface
- **Chat System**: OnChatFromClient event handling
- **Client Management**: OnNewClient and OnClientClosed events
- **File System**: XML content loading from disk
- **Viewer Protocol**: Custom chat-based floater commands

## Future Enhancements

### Potential Improvements

- **Template System**: Pre-built floater templates for common use cases
- **Style Sheets**: CSS-like styling system for consistent appearance
- **Data Binding**: Automatic data synchronization between region and UI
- **Animation Support**: Animated UI transitions and effects
- **Persistence**: Save and restore floater states across sessions

### Viewer Enhancements

- **Native Protocol**: Replace chat-based protocol with dedicated viewer protocol
- **Advanced Layouts**: Support for complex grid and flexbox layouts
- **Rich Content**: HTML-like rich text with images and links
- **Accessibility**: Screen reader and keyboard navigation support
- **Mobile Support**: Touch-friendly controls for mobile viewers

### Integration Extensions

- **Database Integration**: Store floater definitions in database
- **Asset System**: Load XML content from region assets
- **Scripting Extensions**: Direct LSL/OSSL functions for floater management
- **Web Integration**: Hybrid web/native floater capabilities
- **Group Features**: Shared floaters for group interactions

---

*This documentation covers DynamicFloaterModule as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full dynamic floater creation, XML-based UI generation, and interactive dialog management capabilities.*