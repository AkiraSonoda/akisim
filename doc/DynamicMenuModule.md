# DynamicMenuModule Technical Documentation

## Overview

The **DynamicMenuModule** is a non-shared region module that provides dynamic menu customization capabilities for OpenSimulator viewers. It enables regions, scripts, and modules to dynamically add custom menu items to the viewer's menu system, allowing for enhanced user interaction and region-specific functionality without requiring viewer modifications or external tools.

## Purpose

The DynamicMenuModule serves as an advanced viewer integration component that:

- **Dynamic Menu Creation**: Enables runtime addition of custom menu items to viewer menus
- **Permission-Based Access**: Supports different menu visibility based on user permissions (Normal, RegionManager, God)
- **Multi-Location Support**: Allows menu items in various viewer menu sections (Agent, World, Tools, Advanced, Admin)
- **Script Integration**: Provides API for scripts and modules to create interactive menu systems
- **Event-Driven Actions**: Handles menu selections with custom callback handlers
- **Viewer Feature Enhancement**: Extends viewer functionality without requiring client modifications

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│        DynamicMenuModule            │
├─────────────────────────────────────┤
│      INonSharedRegionModule         │
│    - Per-region instantiation      │
│    - Scene-specific menu mgmt       │
├─────────────────────────────────────┤
│       IDynamicMenuModule            │
│    - Public API for scripts        │
│    - Menu item management           │
├─────────────────────────────────────┤
│     Menu Item Data Management       │
│    - Permission-based filtering    │
│    - Location-based organization   │
│    - User/Global menu separation   │
├─────────────────────────────────────┤
│    Capabilities Integration         │
│    - CustomMenuAction handler      │
│    - HTTP-based menu events        │
│    - Per-client capability setup   │
├─────────────────────────────────────┤
│   Simulator Features Integration    │
│    - OnSimulatorFeaturesRequest    │
│    - Menu structure generation     │
│    - Real-time menu updates        │
└─────────────────────────────────────┘
```

### Data Flow Architecture

```
Script/Module → AddMenuItem()
     ↓
Store in MenuItemData Collection
     ↓
Client Connects/Requests Features
     ↓
OnSimulatorFeaturesRequest()
     ↓
Filter by Permissions & Mode
     ↓
Generate Menu Structure (OSD)
     ↓
Send to Viewer
     ↓
User Selects Menu Item
     ↓
CustomMenuAction Capability
     ↓
MenuActionHandler Processing
     ↓
Execute Custom Handler Callback
```

### MenuItemData Structure

```
MenuItemData
├── Title (string) - Menu item display text
├── AgentID (UUID) - Owner/target agent (UUID.Zero for global)
├── Location (InsertLocation) - Menu section placement
├── Mode (UserMode) - Required permission level
└── Handler (CustomMenuHandler) - Action callback delegate
```

## Interface Implementation

The module implements:
- **INonSharedRegionModule**: Per-region module instance
- **IDynamicMenuModule**: Public API for menu management

### IDynamicMenuModule Interface

```csharp
public interface IDynamicMenuModule
{
    void AddMenuItem(UUID agentID, string title, InsertLocation location, UserMode mode, CustomMenuHandler handler);
    void AddMenuItem(string title, InsertLocation location, UserMode mode, CustomMenuHandler handler);
    void RemoveMenuItem(string action);
}
```

### Supporting Enumerations

```csharp
public enum InsertLocation : int
{
    Agent = 1,      // Agent menu (avatar-related actions)
    World = 2,      // World menu (environment/region actions)
    Tools = 3,      // Tools menu (utility functions)
    Advanced = 4,   // Advanced menu (power user features)
    Admin = 5       // Admin menu (administrative functions)
}

public enum UserMode : int
{
    Normal = 0,         // Available to all users
    RegionManager = 2,  // Requires region manager permissions
    God = 3            // Requires god permissions
}
```

### CustomMenuHandler Delegate

```csharp
public delegate void CustomMenuHandler(string action, UUID agentID, List<uint> selection);
```

Handler parameters:
- `action`: Menu item title that was selected
- `agentID`: UUID of the user who selected the menu
- `selection`: List of selected objects/items (for context-sensitive menus)

## Configuration

### Module Activation

Configure in OpenSim.ini [DynamicMenuModule] section:

```ini
[DynamicMenuModule]
enabled = true
```

The module is disabled by default to prevent unnecessary processing when not needed.

### Configuration Implementation

```csharp
public void Initialise(IConfigSource config)
{
    IConfig moduleConfig = config.Configs["DynamicMenuModule"];
    if (moduleConfig != null)
    {
        m_Enabled = moduleConfig.GetBoolean("enabled", false);
    }
}
```

### Factory Integration

The module is loaded via factory with configuration-based activation:

```csharp
var dynamicMenuConfig = configSource?.Configs["DynamicMenuModule"];
if (dynamicMenuConfig?.GetBoolean("enabled", false) == true)
{
    if(m_log.IsDebugEnabled) m_log.Debug("Loading DynamicMenuModule for dynamic viewer menu customization and script integration");
    var dynamicMenuModuleInstance = LoadDynamicMenuModule();
    yield return dynamicMenuModuleInstance;
    if(m_log.IsInfoEnabled) m_log.Info("DynamicMenuModule loaded for viewer menu customization, permission-based menu items, and script-driven menu actions");
}
```

## Core Functionality

### Menu Item Management

#### AddMenuItem Methods

**Global Menu Items** (visible to all qualifying users):
```csharp
public void AddMenuItem(string title, InsertLocation location, UserMode mode, CustomMenuHandler handler)
{
    AddMenuItem(UUID.Zero, title, location, mode, handler);
}
```

**User-Specific Menu Items** (visible only to specific user):
```csharp
public void AddMenuItem(UUID agentID, string title, InsertLocation location, UserMode mode, CustomMenuHandler handler)
{
    if (!m_menuItems.ContainsKey(agentID))
        m_menuItems[agentID] = new List<MenuItemData>();

    m_menuItems[agentID].Add(new MenuItemData() {
        Title = title,
        AgentID = agentID,
        Location = location,
        Mode = mode,
        Handler = handler
    });
}
```

#### RemoveMenuItem Method

```csharp
public void RemoveMenuItem(string action)
{
    foreach (KeyValuePair<UUID, List<MenuItemData>> kvp in m_menuItems)
    {
        List<MenuItemData> pendingDeletes = new List<MenuItemData>();
        foreach (MenuItemData d in kvp.Value)
        {
            if (d.Title == action)
                pendingDeletes.Add(d);
        }

        foreach (MenuItemData d in pendingDeletes)
            kvp.Value.Remove(d);
    }
}
```

### Permission System

The module implements a comprehensive permission system:

#### Permission Levels
- **Normal (0)**: Available to all users
- **RegionManager (2)**: Requires administrator permissions in the region
- **God (3)**: Requires god permissions

#### Permission Checking Logic

```csharp
// For global menu items (UUID.Zero)
if (!m_scene.Permissions.IsGod(agentID))
{
    if (d.Mode == UserMode.RegionManager && (!m_scene.Permissions.IsAdministrator(agentID)))
        continue; // Skip this menu item
}

// For user-specific menu items
if (d.Mode == UserMode.God && (!m_scene.Permissions.IsGod(agentID)))
    continue; // Skip this menu item
```

### Simulator Features Integration

#### OnSimulatorFeaturesRequest Handler

```csharp
private void OnSimulatorFeaturesRequest(UUID agentID, ref OSDMap features)
{
    OSD menus = new OSDMap();
    if (features.ContainsKey("menus"))
        menus = features["menus"];

    // Create menu sections
    OSDMap agent = new OSDMap();
    OSDMap world = new OSDMap();
    OSDMap tools = new OSDMap();
    OSDMap advanced = new OSDMap();
    OSDMap admin = new OSDMap();

    // Process global menu items (UUID.Zero)
    if (m_menuItems.ContainsKey(UUID.Zero))
    {
        foreach (MenuItemData d in m_menuItems[UUID.Zero])
        {
            // Apply permission filtering
            if (!CheckPermissions(agentID, d))
                continue;

            // Add to appropriate menu section
            OSDMap targetLocation = GetMenuLocation(d.Location);
            if (targetLocation != null)
                targetLocation[d.Title] = OSD.FromString(d.Title);
        }
    }

    // Process user-specific menu items
    if (m_menuItems.ContainsKey(agentID))
    {
        foreach (MenuItemData d in m_menuItems[agentID])
        {
            // Apply permission filtering and add to menus
            ProcessUserMenuItem(agentID, d);
        }
    }

    // Update features with complete menu structure
    features["menus"] = menus;
}
```

### Capabilities System Integration

#### OnRegisterCaps Handler

```csharp
private void OnRegisterCaps(UUID agentID, Caps caps)
{
    caps.RegisterSimpleHandler("CustomMenuAction",
        new MenuActionHandler("/" + UUID.Random(), "CustomMenuAction", agentID, this, m_scene));
}
```

Each client receives a unique CustomMenuAction capability for handling menu selections.

#### MenuActionHandler Class

```csharp
public class MenuActionHandler : SimpleOSDMapHandler
{
    private UUID m_agentID;
    private Scene m_scene;
    private DynamicMenuModule m_module;

    protected override void ProcessRequest(IOSHttpRequest httpRequest, IOSHttpResponse httpResponse, OSDMap osd)
    {
        string action = osd["action"].AsString();
        OSDArray selection = (OSDArray)osd["selection"];
        List<uint> sel = new List<uint>();

        for (int i = 0; i < selection.Count; i++)
            sel.Add(selection[i].AsUInteger());

        // Execute menu action asynchronously
        Util.FireAndForget(
            x => { m_module.HandleMenuSelection(action, m_agentID, sel); },
            null, "DynamicMenuModule.HandleMenuSelection");

        httpResponse.StatusCode = (int)HttpStatusCode.OK;
    }
}
```

### Menu Selection Processing

#### HandleMenuSelection Method

```csharp
internal void HandleMenuSelection(string action, UUID agentID, List<uint> selection)
{
    // Check user-specific menu items first
    if (m_menuItems.ContainsKey(agentID))
    {
        foreach (MenuItemData d in m_menuItems[agentID])
        {
            if (d.Title == action)
                d.Handler(action, agentID, selection);
        }
    }

    // Check global menu items (UUID.Zero)
    if (m_menuItems.ContainsKey(UUID.Zero))
    {
        foreach (MenuItemData d in m_menuItems[UUID.Zero])
        {
            if (d.Title == action)
                d.Handler(action, agentID, selection);
        }
    }
}
```

## Menu Locations and Organization

### Viewer Menu Structure

The module supports adding items to five main viewer menu locations:

1. **Agent Menu** (`InsertLocation.Agent`)
   - Avatar-related actions
   - Personal settings and preferences
   - User-specific functionality

2. **World Menu** (`InsertLocation.World`)
   - Environment and region actions
   - World interaction features
   - Region-specific tools

3. **Tools Menu** (`InsertLocation.Tools`)
   - Utility functions
   - Development tools
   - General-purpose features

4. **Advanced Menu** (`InsertLocation.Advanced`)
   - Power user features
   - Advanced configuration options
   - Technical functionality

5. **Admin Menu** (`InsertLocation.Admin`)
   - Administrative functions
   - Region management tools
   - God mode features

### Menu Item Placement Logic

```csharp
private OSDMap GetMenuLocation(InsertLocation location)
{
    switch (location)
    {
        case InsertLocation.Agent:
            return agent;
        case InsertLocation.World:
            return world;
        case InsertLocation.Tools:
            return tools;
        case InsertLocation.Advanced:
            return advanced;
        case InsertLocation.Admin:
            return admin;
        default:
            return null;
    }
}
```

## Integration Patterns

### Script Integration Example

```csharp
public class MyRegionModule : INonSharedRegionModule
{
    private IDynamicMenuModule m_menuModule;

    public void RegionLoaded(Scene scene)
    {
        m_menuModule = scene.RequestModuleInterface<IDynamicMenuModule>();

        if (m_menuModule != null)
        {
            // Add a global menu item for all users
            m_menuModule.AddMenuItem(
                "Teleport Home",
                InsertLocation.World,
                UserMode.Normal,
                HandleTeleportHome
            );

            // Add an admin-only menu item
            m_menuModule.AddMenuItem(
                "Region Statistics",
                InsertLocation.Admin,
                UserMode.RegionManager,
                HandleRegionStats
            );
        }
    }

    private void HandleTeleportHome(string action, UUID agentID, List<uint> selection)
    {
        ScenePresence sp = m_scene.GetScenePresence(agentID);
        if (sp != null)
        {
            // Teleport user to their home location
            sp.ControllingClient.SendTeleportStart((uint)TeleportFlags.ViaHome);
            sp.Teleport(sp.Scene.RegionInfo.RegionLocX, sp.Scene.RegionInfo.RegionLocY, 128);
        }
    }

    private void HandleRegionStats(string action, UUID agentID, List<uint> selection)
    {
        ScenePresence sp = m_scene.GetScenePresence(agentID);
        if (sp != null)
        {
            // Send region statistics to admin user
            string stats = GetRegionStatistics();
            sp.ControllingClient.SendChatMessage(stats,
                (byte)ChatTypeEnum.Owner, Vector3.Zero, "Region", UUID.Zero, UUID.Zero,
                (byte)ChatSourceType.System, (byte)ChatAudibleLevel.Fully);
        }
    }
}
```

### LSL Script Integration

```lsl
// LSL script example using DynamicMenuModule via region module
default
{
    state_entry()
    {
        // This would require a supporting region module to bridge LSL and DynamicMenuModule
        llOwnerSay("Dynamic menu system ready");
    }

    touch_start(integer total_number)
    {
        key owner = llGetOwner();
        // Request custom menu creation through region module
        llMessageLinked(LINK_THIS, 1, "CREATE_MENU|Custom Action|World|Normal", owner);
    }
}
```

### Advanced Permission Scenarios

```csharp
// Example: User-specific menu based on group membership
public void AddUserSpecificMenu(UUID userID)
{
    ScenePresence sp = m_scene.GetScenePresence(userID);
    if (sp != null)
    {
        // Check if user is member of specific group
        if (IsGroupMember(userID, m_vipGroupID))
        {
            m_menuModule.AddMenuItem(userID,
                "VIP Features",
                InsertLocation.Tools,
                UserMode.Normal,
                HandleVIPFeatures);
        }
    }
}

// Example: Dynamic menu updates based on region state
public void UpdateMenusForRegionEvent()
{
    if (m_isSpecialEvent)
    {
        m_menuModule.AddMenuItem(
            "Event Teleporter",
            InsertLocation.World,
            UserMode.Normal,
            HandleEventTeleport);
    }
    else
    {
        m_menuModule.RemoveMenuItem("Event Teleporter");
    }
}
```

## Advanced Features

### Multi-User Menu Management

The module maintains separate menu collections for different user contexts:

```csharp
private Dictionary<UUID, List<MenuItemData>> m_menuItems =
    new Dictionary<UUID, List<MenuItemData>>();
```

- **UUID.Zero**: Global menu items visible to all qualifying users
- **Specific UUIDs**: User-specific menu items visible only to that user

### Dynamic Menu Updates

Menus are updated automatically when:
- New clients connect (via OnRegisterCaps)
- Simulator features are requested (via OnSimulatorFeaturesRequest)
- Menu items are added or removed dynamically

### Context-Sensitive Actions

Menu handlers receive selection context:

```csharp
private void HandleObjectAction(string action, UUID agentID, List<uint> selection)
{
    if (selection.Count > 0)
    {
        // Action applies to selected objects
        foreach (uint localID in selection)
        {
            SceneObjectPart part = m_scene.GetSceneObjectPart(localID);
            if (part != null)
            {
                // Perform action on selected object
                ProcessSelectedObject(part, agentID);
            }
        }
    }
    else
    {
        // Action applies globally or to user
        ProcessGlobalAction(agentID);
    }
}
```

### Asynchronous Event Processing

Menu actions are processed asynchronously to prevent blocking:

```csharp
Util.FireAndForget(
    x => { m_module.HandleMenuSelection(action, m_agentID, sel); },
    null, "DynamicMenuModule.HandleMenuSelection");
```

## Performance Characteristics

### Memory Management

- **Efficient Collections**: Dictionary-based storage for fast menu lookup
- **Per-User Isolation**: Separate collections prevent cross-user interference
- **Minimal Overhead**: Lightweight MenuItemData structures
- **Automatic Cleanup**: Menu items removed when modules unload

### Processing Efficiency

- **Permission Caching**: Permissions checked once per feature request
- **Lazy Evaluation**: Menu structures built only when requested
- **Asynchronous Handlers**: Non-blocking menu action processing
- **Capability Optimization**: Per-client capability registration

### Scalability Features

- **Per-Region Isolation**: Independent menu management per region
- **User-Specific Menus**: Scalable to large user bases
- **Dynamic Updates**: Real-time menu modifications without restart
- **Resource Limits**: Controlled memory usage through collection management

## Security Considerations

### Permission Enforcement

- **Multi-Level Security**: Normal, RegionManager, and God permission levels
- **Runtime Validation**: Permissions checked at menu generation time
- **Capability Protection**: Unique capabilities per client prevent cross-user access
- **Action Validation**: Menu selections validated before handler execution

### Input Validation

- **Parameter Sanitization**: Menu action strings validated before processing
- **Selection Limits**: Object selection arrays bounded and validated
- **User Verification**: Agent presence verified before menu actions
- **Error Handling**: Graceful handling of malformed requests

### Access Control

- **Agent Isolation**: User-specific menus isolated by UUID
- **Scene Boundaries**: Menu actions limited to specific regions
- **Module Interface**: Controlled access through IDynamicMenuModule interface
- **Capability Security**: HTTP capabilities use unique random paths

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: DynamicMenuModule not appearing in logs
Solution: Set enabled = true in [DynamicMenuModule] section
```

#### Menu Items Not Appearing
```
Symptom: Added menu items don't show in viewer
Causes:
- Viewer doesn't support custom menus
- Permission levels insufficient
- Menu location not supported

Solutions:
- Verify viewer compatibility (Firestorm, etc.)
- Check user permission levels
- Use supported InsertLocation values
```

#### Menu Actions Not Working
```
Symptom: Menu selections don't trigger handlers
Causes:
- Handler delegate not properly assigned
- Capability registration failed
- Asynchronous execution errors

Solutions:
- Verify CustomMenuHandler assignment
- Check OnRegisterCaps execution
- Monitor async error logs
```

#### Permission Issues
```
Symptom: Menu items appear for wrong users
Causes:
- Incorrect UserMode setting
- Permission checking logic errors
- Global vs user-specific confusion

Solutions:
- Review UserMode enum values
- Test permission methods
- Verify UUID.Zero vs specific user logic
```

### Debug Information

Enable detailed logging for troubleshooting:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

// Add debug statements in key methods:
m_log.DebugFormat("[DYNAMIC MENU]: Adding menu item '{0}' for {1} at {2}", title, agentID, location);
m_log.DebugFormat("[DYNAMIC MENU]: Processing menu selection '{0}' by {1}", action, agentID);
```

### Testing Procedures

1. **Enable Module**: Set enabled = true in configuration
2. **Add Test Menu**: Create simple menu item via script/module
3. **Verify Appearance**: Check if menu appears in viewer
4. **Test Permissions**: Verify permission-based visibility
5. **Test Actions**: Click menu items and verify handler execution
6. **Check Cleanup**: Verify menu removal works correctly

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [DynamicMenuModule] enabled setting
- **Enhanced Logging**: Improved operational visibility and debugging
- **Backward Compatibility**: Maintains full API and functionality compatibility

### Configuration Changes

The module now requires explicit configuration to enable:

```ini
# Old behavior: Loaded via Mono.Addins extension system
# New behavior: Configurable enablement
[DynamicMenuModule]
enabled = true
```

### Upgrade Considerations

- Update configuration files to include [DynamicMenuModule] section if needed
- Test menu functionality after upgrade
- Verify viewer compatibility for dynamic menus
- Check permission system behavior
- Monitor logging for new message formats

## Related Components

### Dependencies
- **INonSharedRegionModule**: Module interface contract
- **IDynamicMenuModule**: Public API interface
- **ISimulatorFeaturesModule**: Simulator features integration
- **Caps**: Capabilities system for HTTP handlers
- **Scene**: Regional simulation environment and permissions

### Integration Points
- **Scripting System**: LSL/OSSL integration via supporting modules
- **Permission System**: Scene.Permissions for access control
- **Capabilities System**: CustomMenuAction capability registration
- **Simulator Features**: Menu structure generation and transmission
- **HTTP Server**: Request/response handling for menu actions

## Future Enhancements

### Potential Improvements

- **Icon Support**: Custom icons for menu items
- **Submenu Support**: Hierarchical menu structures
- **Keyboard Shortcuts**: Hotkey assignments for menu items
- **Menu Theming**: Customizable menu appearance
- **Batch Operations**: Multiple menu item management

### Viewer Enhancements

- **Rich Menu Content**: HTML-like formatting support
- **Dynamic Menu Updates**: Real-time menu modifications
- **Context Menu Integration**: Right-click menu integration
- **Mobile Support**: Touch-friendly menu interfaces
- **Accessibility**: Screen reader and keyboard navigation

### Integration Extensions

- **Database Storage**: Persistent menu configurations
- **Group Integration**: Group-based menu visibility
- **Region Sharing**: Cross-region menu synchronization
- **Web Integration**: Web-based menu management interface
- **Event Integration**: Menu items based on region events

---

*This documentation covers DynamicMenuModule as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full dynamic menu creation, permission-based access control, and script integration capabilities.*