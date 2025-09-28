# SpecialUIModule Technical Documentation

## Overview

The **SpecialUIModule** is a non-shared region module that provides advanced viewer UI customization capabilities for OpenSimulator regions. It enables the delivery of custom toolbar panels and floater UI elements to viewers, allowing regions to enhance the user interface with custom functionality, tools, and region-specific features without requiring viewer modifications.

## Purpose

The SpecialUIModule serves as an advanced viewer integration component that:

- **Custom UI Delivery**: Transmits custom XML-based UI elements directly to viewers
- **Toolbar Customization**: Provides custom toolbar panels for region-specific functionality
- **Floater Management**: Delivers custom floater windows and dialogs
- **User Level Integration**: Supports user-level-based UI customization and access control
- **File-Based Configuration**: Uses external XML files for flexible UI definition
- **Viewer Enhancement**: Extends viewer capabilities without requiring client modifications

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│         SpecialUIModule             │
├─────────────────────────────────────┤
│      INonSharedRegionModule         │
│    - Per-region instantiation      │
│    - Scene-specific UI delivery     │
├─────────────────────────────────────┤
│     SimulatorFeaturesHelper         │
│    - User level validation         │
│    - UserAccount service access    │
├─────────────────────────────────────┤
│      File System Integration       │
│    - XML file loading              │
│    - ViewerSupport directory       │
│    - Dynamic content discovery     │
├─────────────────────────────────────┤
│   Simulator Features Integration    │
│    - OnSimulatorFeaturesRequest    │
│    - OpenSimExtras framework       │
│    - Special UI content delivery   │
├─────────────────────────────────────┤
│     User Level Access Control      │
│    - UserLevel configuration       │
│    - Permission-based delivery     │
│    - Account validation            │
└─────────────────────────────────────┘
```

### Data Flow Architecture

```
Client Connects/Requests Features
     ↓
OnSimulatorFeaturesRequest()
     ↓
Check User Level (SimulatorFeaturesHelper)
     ↓
User Level <= Configured Level?
     ↓ (Yes)
Load panel_toolbar.xml
     ↓
Scan ViewerSupport/Floaters/ Directory
     ↓
Load All *.xml Floater Files
     ↓
Package in OpenSimExtras/special-ui
     ↓
Send to Viewer via Simulator Features
```

### File Structure

```
ViewerSupport/
├── panel_toolbar.xml          # Main toolbar configuration
└── Floaters/                  # Custom floater definitions
    ├── custom_dialog.xml
    ├── region_tools.xml
    └── *.xml                  # Additional floaters
```

## Interface Implementation

The module implements:
- **INonSharedRegionModule**: Per-region module instance
- **Internal Helper Classes**: SimulatorFeaturesHelper for user validation

### SimulatorFeaturesHelper Class

```csharp
public class SimulatorFeaturesHelper
{
    private Scene m_scene;

    public SimulatorFeaturesHelper(Scene scene)
    {
        m_scene = scene;
    }

    public int UserLevel(UUID agentID)
    {
        int level = 0;
        UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, agentID);
        if (account != null)
            level = account.UserLevel;
        return level;
    }
}
```

## Configuration

### Module Activation

Configure in OpenSim.ini [SpecialUIModule] section:

```ini
[SpecialUIModule]
enabled = true
UserLevel = 0
```

### Configuration Parameters

- **enabled**: Enable/disable the SpecialUIModule (default: false)
- **UserLevel**: Maximum user level that receives special UI elements (default: 0)

### Configuration Implementation

```csharp
public void Initialise(IConfigSource config)
{
    IConfig moduleConfig = config.Configs["SpecialUIModule"];
    if (moduleConfig != null)
    {
        m_Enabled = moduleConfig.GetBoolean("enabled", false);
        if (m_Enabled)
        {
            m_UserLevel = moduleConfig.GetInt("UserLevel", 0);
            m_log.Info("[SPECIAL UI]: SpecialUIModule enabled");
        }
    }
}
```

### Factory Integration

The module is loaded via factory with configuration-based activation:

```csharp
var specialUIConfig = configSource?.Configs["SpecialUIModule"];
if (specialUIConfig?.GetBoolean("enabled", false) == true)
{
    if(m_log.IsDebugEnabled) m_log.Debug("Loading SpecialUIModule for custom viewer UI elements and toolbar customization");
    var specialUIModuleInstance = LoadSpecialUIModule();
    yield return specialUIModuleInstance;
    if(m_log.IsInfoEnabled) m_log.Info("SpecialUIModule loaded for viewer UI customization, toolbar panels, and user-level-based UI elements");
}
```

## Core Functionality

### User Level Access Control

#### User Level Validation

```csharp
private void OnSimulatorFeaturesRequest(UUID agentID, ref OSDMap features)
{
    if (m_Helper.UserLevel(agentID) <= m_UserLevel)
    {
        // User qualifies for special UI elements
        DeliverSpecialUI(agentID, ref features);
    }
    else
    {
        // User level too high, skip special UI delivery
        m_log.DebugFormat("[SPECIAL UI]: NOT Sending panel_toolbar.xml in {0}", m_scene.RegionInfo.RegionName);
    }
}
```

#### Access Control Logic

- **UserLevel 0**: All users receive special UI
- **UserLevel 1**: Only level 0-1 users receive special UI
- **UserLevel N**: Only users with level ≤ N receive special UI

This allows fine-grained control over which users see custom UI elements based on their account level.

### Toolbar Panel Delivery

#### panel_toolbar.xml Loading

```csharp
OSDMap specialUI = new OSDMap();
using (StreamReader s = new StreamReader(Path.Combine(VIEWER_SUPPORT_DIR, "panel_toolbar.xml")))
{
    if (!features.TryGetValue("OpenSimExtras", out extrasMap))
    {
        extrasMap = new OSDMap();
        features["OpenSimExtras"] = extrasMap;
    }

    specialUI["toolbar"] = OSDMap.FromString(s.ReadToEnd());
    ((OSDMap)extrasMap)["special-ui"] = specialUI;
}
```

The main toolbar configuration is loaded from `ViewerSupport/panel_toolbar.xml` and delivered to qualifying users.

### Custom Floater Management

#### Dynamic Floater Discovery

```csharp
if (Directory.Exists(Path.Combine(VIEWER_SUPPORT_DIR, "Floaters")))
{
    OSDMap floaters = new OSDMap();
    uint n = 0;
    foreach (String name in Directory.GetFiles(Path.Combine(VIEWER_SUPPORT_DIR, "Floaters"), "*.xml"))
    {
        using (StreamReader s = new StreamReader(name))
        {
            string simple_name = Path.GetFileNameWithoutExtension(name);
            OSDMap floater = new OSDMap();
            floaters[simple_name] = OSDMap.FromString(s.ReadToEnd());
            n++;
        }
    }
    specialUI["floaters"] = floaters;
    m_log.DebugFormat("[SPECIAL UI]: Sending {0} floaters", n);
}
```

The module automatically discovers and loads all XML files in the `ViewerSupport/Floaters/` directory.

### OpenSimExtras Integration

#### Special UI Framework

```csharp
// Structure delivered to viewer:
OpenSimExtras: {
    special-ui: {
        toolbar: "<xml content from panel_toolbar.xml>",
        floaters: {
            "floater_name1": "<xml content from floater1.xml>",
            "floater_name2": "<xml content from floater2.xml>",
            ...
        }
    }
}
```

The module integrates with OpenSimulator's OpenSimExtras framework to deliver custom UI elements.

## XML File Formats

### Toolbar Panel XML (panel_toolbar.xml)

```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<panel name="region_toolbar" follows="all" width="100" height="30"
       background_visible="true" bg_alpha_color="0.2 0.2 0.2 0.8">

    <button name="region_info" follows="left|top" left="5" top="5" width="80" height="20"
            label="Region Info" font="SansSerif"
            commit_callback.function="OpenSim.SpecialUI.RegionInfo"/>

    <button name="teleport_home" follows="left|top" left="90" top="5" width="80" height="20"
            label="Home" font="SansSerif"
            commit_callback.function="OpenSim.SpecialUI.TeleportHome"/>

</panel>
```

### Custom Floater XML (Floaters/*.xml)

```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<floater name="region_tools" title="Region Tools" can_minimize="true" can_tear_off="false"
         can_resize="false" can_drag_on_left="false" can_close="true" can_dock="false"
         visible="false" open_positioning="cascading" can_collapse="false"
         header_height="18" legacy_header_height="18" width="300" height="200">

    <layout_stack orientation="vertical" follows="all" top="20" left="5" width="290" height="175">
        <text follows="left|top" height="20" layout="topleft" left="10" top="10" width="270"
              text_color="white" font="SansSerif" name="title">
            Region Management Tools
        </text>

        <button follows="left|top" height="25" layout="topleft" left="10" top="40" width="120"
                label="Restart Region" name="restart_region"
                commit_callback.function="OpenSim.SpecialUI.RestartRegion"/>

        <button follows="left|top" height="25" layout="topleft" left="140" top="40" width="120"
                label="Region Stats" name="region_stats"
                commit_callback.function="OpenSim.SpecialUI.RegionStats"/>
    </layout_stack>
</floater>
```

## Directory Structure and File Management

### Required Directory Structure

```
ViewerSupport/
├── panel_toolbar.xml          # Required - Main toolbar panel
└── Floaters/                  # Optional - Custom floater directory
    ├── admin_tools.xml        # Example floater
    ├── user_guide.xml         # Example floater
    └── custom_features.xml    # Example floater
```

### File Discovery Process

1. **Toolbar Loading**: Always attempts to load `ViewerSupport/panel_toolbar.xml`
2. **Floater Discovery**: Scans `ViewerSupport/Floaters/` directory if it exists
3. **XML Processing**: Reads all `.xml` files in the Floaters directory
4. **Content Packaging**: Packages content in OpenSimExtras special-ui structure

### Error Handling

```csharp
try
{
    using (StreamReader s = new StreamReader(Path.Combine(VIEWER_SUPPORT_DIR, "panel_toolbar.xml")))
    {
        specialUI["toolbar"] = OSDMap.FromString(s.ReadToEnd());
    }
}
catch (Exception ex)
{
    m_log.ErrorFormat("[SPECIAL UI]: Failed to load panel_toolbar.xml: {0}", ex.Message);
}
```

The module handles missing files gracefully and logs appropriate error messages.

## User Level Integration

### UserAccount Service Integration

```csharp
public int UserLevel(UUID agentID)
{
    int level = 0;
    UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, agentID);
    if (account != null)
        level = account.UserLevel;
    return level;
}
```

The module integrates with OpenSimulator's user account system to determine user levels.

### Access Control Scenarios

#### Scenario 1: Public Access (UserLevel = 0)
```ini
[SpecialUIModule]
enabled = true
UserLevel = 0
```
**Result**: All users receive special UI elements

#### Scenario 2: Registered Users Only (UserLevel = 1)
```ini
[SpecialUIModule]
enabled = true
UserLevel = 1
```
**Result**: Only users with level 0-1 receive special UI elements

#### Scenario 3: Staff/Admin Only (UserLevel = 200)
```ini
[SpecialUIModule]
enabled = true
UserLevel = 200
```
**Result**: Only high-level users receive special UI elements

## Integration Patterns

### Region-Specific UI Elements

```xml
<!-- Example: Region weather panel -->
<panel name="weather_panel" follows="all" width="200" height="60">
    <text name="weather_status" follows="all" text_color="white">
        Current Weather: {WEATHER_STATUS}
    </text>
    <button name="weather_control" label="Change Weather"
            commit_callback.function="OpenSim.Weather.Control"/>
</panel>
```

### Administrative Tools Integration

```xml
<!-- Example: Admin toolbar for region management -->
<panel name="admin_toolbar" follows="all" width="400" height="30">
    <button name="user_management" label="Users"
            commit_callback.function="OpenSim.Admin.UserManagement"/>
    <button name="region_settings" label="Settings"
            commit_callback.function="OpenSim.Admin.RegionSettings"/>
    <button name="land_management" label="Land"
            commit_callback.function="OpenSim.Admin.LandManagement"/>
</panel>
```

### Educational Environment Support

```xml
<!-- Example: Student tools panel -->
<panel name="student_tools" follows="all" width="300" height="30">
    <button name="help_system" label="Help"
            commit_callback.function="OpenSim.Education.Help"/>
    <button name="tutorial" label="Tutorial"
            commit_callback.function="OpenSim.Education.Tutorial"/>
    <button name="assignments" label="Assignments"
            commit_callback.function="OpenSim.Education.Assignments"/>
</panel>
```

## Advanced Features

### Dynamic Content Management

The module supports dynamic content through:
- **File-based configuration**: XML files can be updated without module restart
- **Directory scanning**: New floaters automatically discovered on restart
- **User-level filtering**: Different UI elements for different user classes

### Multi-File Floater Support

```csharp
// Multiple floaters delivered simultaneously:
floaters: {
    "welcome_dialog": "<xml content>",
    "region_map": "<xml content>",
    "tools_panel": "<xml content>",
    "help_system": "<xml content>"
}
```

Each XML file in the Floaters directory becomes a separately accessible floater.

### Conditional UI Delivery

```csharp
// Only users with appropriate level receive UI
if (m_Helper.UserLevel(agentID) <= m_UserLevel)
{
    // Deliver special UI elements
    DeliverCustomUI(agentID, ref features);
}
```

UI elements are conditionally delivered based on user account levels.

## Performance Characteristics

### File I/O Optimization

- **Cached Loading**: XML files read once per simulator features request
- **Streaming Processing**: Files processed with StreamReader for memory efficiency
- **Directory Scanning**: Performed only when Floaters directory exists
- **On-Demand Loading**: Content loaded only when users request features

### Memory Management

- **Efficient String Processing**: XML content processed directly to OSD structures
- **Automatic Cleanup**: StreamReader properly disposed after use
- **Minimal Overhead**: Helper class instances lightweight and scene-scoped
- **Resource Limits**: File operations bounded by directory structure

### Network Efficiency

- **Single Transmission**: All UI elements delivered in one simulator features response
- **Compressed Format**: OSD format provides efficient serialization
- **User-Specific Delivery**: Content only sent to qualifying users
- **Feature Integration**: Leverages existing simulator features mechanism

## Security Considerations

### File System Security

- **Restricted Paths**: Only accesses ViewerSupport directory and subdirectories
- **Extension Filtering**: Only processes .xml files in Floaters directory
- **Read-Only Access**: Module only reads files, never writes or modifies
- **Error Handling**: Graceful handling of missing or inaccessible files

### Access Control

- **User Level Validation**: Strict checking of user account levels
- **Scene Boundaries**: UI delivery limited to specific regions
- **Feature Integration**: Uses secure simulator features delivery mechanism
- **Content Validation**: XML content processed through safe OSD parsing

### Content Security

- **XML Parsing**: Uses OpenMetaverse OSD parsing for secure XML processing
- **Input Validation**: File paths validated and restricted to safe directories
- **Resource Limits**: File scanning limited to specific directory structure
- **Error Recovery**: Failed file operations don't affect other functionality

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: SpecialUIModule not appearing in logs
Solution: Set enabled = true in [SpecialUIModule] section
```

#### Toolbar Not Appearing
```
Symptom: Custom toolbar not visible in viewer
Causes:
- panel_toolbar.xml missing or malformed
- User level too high
- Viewer doesn't support special UI

Solutions:
- Verify ViewerSupport/panel_toolbar.xml exists
- Check UserLevel configuration
- Test with supported viewer (Firestorm, etc.)
```

#### Floaters Not Loading
```
Symptom: Custom floaters not available
Causes:
- Floaters directory missing
- XML files malformed
- File permission issues

Solutions:
- Create ViewerSupport/Floaters/ directory
- Validate XML syntax
- Check file permissions
```

#### User Level Issues
```
Symptom: Wrong users receiving UI elements
Causes:
- UserLevel configuration incorrect
- User account levels misconfigured
- SimulatorFeaturesHelper errors

Solutions:
- Review UserLevel setting
- Check user account levels in database
- Monitor helper class execution
```

### Debug Information

Enable detailed logging for troubleshooting:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

// Debug statements provide detailed logging:
m_log.DebugFormat("[SPECIAL UI]: OnSimulatorFeaturesRequest in {0}", m_scene.RegionInfo.RegionName);
m_log.DebugFormat("[SPECIAL UI]: Sending panel_toolbar.xml in {0}", m_scene.RegionInfo.RegionName);
m_log.DebugFormat("[SPECIAL UI]: Sending {0} floaters", n);
```

### Testing Procedures

1. **Enable Module**: Set enabled = true in configuration
2. **Create UI Files**: Add panel_toolbar.xml and test floaters
3. **Configure User Level**: Set appropriate UserLevel value
4. **Test Delivery**: Connect with different user levels
5. **Verify Content**: Check if UI elements appear in viewer
6. **Check Logs**: Monitor debug output for delivery confirmation

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [SpecialUIModule] enabled setting
- **Enhanced Logging**: Improved operational visibility and debugging
- **Backward Compatibility**: Maintains full API and functionality compatibility

### Configuration Changes

The module now requires explicit configuration to enable:

```ini
# Old behavior: Loaded via Mono.Addins extension system
# New behavior: Configurable enablement
[SpecialUIModule]
enabled = true
UserLevel = 0
```

### Upgrade Considerations

- Update configuration files to include [SpecialUIModule] section if needed
- Verify ViewerSupport directory structure and file permissions
- Test UI delivery with different user levels
- Check XML file formats and viewer compatibility
- Monitor logging for new message formats

## Related Components

### Dependencies
- **INonSharedRegionModule**: Module interface contract
- **ISimulatorFeaturesModule**: Simulator features integration
- **UserAccountService**: User level validation
- **Scene**: Regional simulation environment
- **SimulatorFeaturesHelper**: User validation helper class

### Integration Points
- **Simulator Features**: OnSimulatorFeaturesRequest integration
- **User Account System**: UserLevel-based access control
- **File System**: XML content loading and discovery
- **OpenSimExtras**: Special UI framework integration
- **Viewer Protocol**: UI element delivery mechanism

## Future Enhancements

### Potential Improvements

- **Dynamic Reloading**: Hot-reload XML files without restart
- **Template System**: Parameterized UI templates
- **Database Integration**: Store UI definitions in database
- **Version Control**: UI versioning and update management
- **User Preferences**: Per-user UI customization options

### Content Management

- **Web Interface**: Web-based UI file management
- **Validation Tools**: XML syntax and compatibility checking
- **Preview System**: UI preview before deployment
- **Asset Integration**: Load UI elements from asset system
- **Multi-Language**: Localized UI content support

### Advanced Features

- **Conditional Logic**: JavaScript-like conditions in XML
- **Data Binding**: Dynamic content from region data
- **Event Integration**: UI elements responding to region events
- **Cross-Region UI**: Shared UI elements across regions
- **Mobile Support**: Touch-optimized UI elements

---

*This documentation covers SpecialUIModule as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full viewer UI customization, file-based content management, and user-level-based access control capabilities.*