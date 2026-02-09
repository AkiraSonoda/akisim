# JsonStoreScriptModule Technical Documentation

## Overview

The JsonStoreScriptModule is a critical LSL (Linden Scripting Language) integration component for OpenSimulator/Akisim that provides comprehensive scripting interfaces for JsonStore functionality. This optional non-shared region module serves as the bridge between LSL scripts and the JsonStore data storage system, enabling scripts to create, manipulate, read, and persist JSON data structures through a rich set of script-callable functions. The module extends OpenSimulator's scripting capabilities by providing persistent data storage that survives script resets and object lifecycle events, making it essential for complex scripted systems that require reliable data persistence and cross-script communication.

## Architecture

The JsonStoreScriptModule implements the following interface:
- `INonSharedRegionModule` - Per-region module lifecycle management with script integration

### Key Components

1. **Script Integration Framework**
   - **IScriptModuleComms Interface**: Integration with OpenSim's script communication system
   - **Script Invocation Registration**: Automatic registration of LSL-callable functions
   - **Constant Registration**: Provides JSON-related constants to scripts
   - **Event-Driven Communication**: Asynchronous script callbacks and event dispatching

2. **JsonStore Interface Layer**
   - **IJsonStoreModule Dependency**: Direct interface to JsonStore functionality
   - **Store Lifecycle Management**: Creation, destruction, and testing of JSON stores
   - **Data Operations**: Reading, writing, and manipulation of JSON data structures
   - **Type System Integration**: Support for JSON node types and value types

3. **LSL Function Implementation**
   - **Store Management Functions**: Create, destroy, attach, and test JSON stores
   - **Data Access Functions**: Get, set, and remove JSON values and structures
   - **Path Navigation**: JSON path-based data access and manipulation
   - **Type Introspection**: Node type and value type detection
   - **Asynchronous Operations**: Non-blocking read/write operations with callbacks

4. **Notecard Integration**
   - **Notecard Reading**: Parse notecards as JSON data into stores
   - **Notecard Writing**: Export JSON store data to notecards
   - **Asset Management**: Creation and management of notecard assets
   - **Inventory Integration**: Automatic inventory item creation for generated notecards

5. **Object Rezzing System**
   - **Dynamic Object Creation**: Rez objects with associated JSON data stores
   - **Parameter Passing**: Pass JSON data to newly rezzed objects
   - **Permissions Management**: Respect object permissions and rezzing rules
   - **Script Initialization**: Automatic script activation in rezzed objects

6. **Script State Management**
   - **Script Reset Handling**: Cleanup of stores when scripts reset
   - **Store Tracking**: Per-script store ownership tracking
   - **Resource Management**: Automatic cleanup of orphaned stores
   - **Memory Management**: Efficient handling of script-store associations

## Configuration

### Module Activation

The module automatically loads when JsonStoreModule is enabled:

```ini
[Modules]
JsonStoreModule = true  ; This automatically enables JsonStoreScriptModule
```

### JsonStore Configuration

Configure JsonStore functionality in `[JsonStore]` section:
```ini
[JsonStore]
Enabled = true                    ; Enable JsonStore functionality
EnableObjectStore = true          ; Enable object-specific stores
MaxStringSpace = 2147483647       ; Maximum string storage (bytes)
```

### Default Behavior

- **Automatic Loading**: Loads automatically when JsonStoreModule is enabled
- **Per-Region Operation**: Operates independently in each region
- **Script Integration**: Automatically registers with script communication system
- **Event Handling**: Automatic script reset and removal event handling

### Dependencies

- **JsonStoreModule**: Required for core JSON storage functionality
- **IScriptModuleComms**: Required for script function registration and communication
- **Scene Context**: Operates within scene context for script and object management
- **Asset Service**: Required for notecard operations and asset management

## Features

### LSL Constants

The module provides comprehensive JSON-related constants for scripts:

#### Node Type Constants
- `JSON_NODETYPE_UNDEF` (0) - Undefined node type
- `JSON_NODETYPE_OBJECT` (1) - JSON object node
- `JSON_NODETYPE_ARRAY` (2) - JSON array node
- `JSON_NODETYPE_VALUE` (3) - JSON value node

#### Value Type Constants
- `JSON_VALUETYPE_UNDEF` (0) - Undefined value type
- `JSON_VALUETYPE_BOOLEAN` (1) - Boolean value
- `JSON_VALUETYPE_INTEGER` (2) - Integer value
- `JSON_VALUETYPE_FLOAT` (3) - Float value
- `JSON_VALUETYPE_STRING` (4) - String value

### Core LSL Functions

#### Store Management Functions

**JsonCreateStore(string value)**
- Creates a new JSON store with initial data
- Returns: Store UUID for subsequent operations
- Example: `key store = JsonCreateStore("{\"name\":\"test\"}");`

**JsonDestroyStore(key storeID)**
- Destroys a JSON store and frees its resources
- Returns: 1 on success, 0 on failure
- Example: `JsonDestroyStore(store);`

**JsonTestStore(key storeID)**
- Tests if a JSON store exists and is valid
- Returns: 1 if store exists, 0 otherwise
- Example: `if (JsonTestStore(store)) { ... }`

**JsonAttachObjectStore()**
- Attaches a JSON store to the current object
- Returns: Object UUID as store identifier
- Example: `key objStore = JsonAttachObjectStore();`

#### Data Access Functions

**JsonGetValue(key storeID, string path)**
- Retrieves a value from the JSON store as a string
- Returns: String representation of the value
- Example: `string name = JsonGetValue(store, "name");`

**JsonGetJson(key storeID, string path)**
- Retrieves JSON structure from the store
- Returns: JSON string representation
- Example: `string json = JsonGetJson(store, "config");`

**JsonSetValue(key storeID, string path, string value)**
- Sets a value in the JSON store
- Returns: 1 on success, 0 on failure
- Example: `JsonSetValue(store, "name", "new_name");`

**JsonSetJson(key storeID, string path, string value)**
- Sets JSON structure in the store
- Returns: 1 on success, 0 on failure
- Example: `JsonSetJson(store, "config", "{\"enabled\":true}");`

**JsonRemoveValue(key storeID, string path)**
- Removes a value from the JSON store
- Returns: 1 on success, 0 on failure
- Example: `JsonRemoveValue(store, "temp_data");`

#### Array Operations

**JsonGetArrayLength(key storeID, string path)**
- Gets the length of a JSON array
- Returns: Array length, -1 on error
- Example: `integer len = JsonGetArrayLength(store, "items");`

#### Type Introspection Functions

**JsonGetNodeType(key storeID, string path)**
- Gets the node type at the specified path
- Returns: Node type constant (JSON_NODETYPE_*)
- Example: `integer type = JsonGetNodeType(store, "config");`

**JsonGetValueType(key storeID, string path)**
- Gets the value type at the specified path
- Returns: Value type constant (JSON_VALUETYPE_*)
- Example: `integer vtype = JsonGetValueType(store, "count");`

#### Path Utility Functions

**JsonList2Path(list pathComponents)**
- Converts a list of path components to a JSON path string
- Returns: JSON path string or "**INVALID**" on error
- Example: `string path = JsonList2Path(["config", "database", "host"]);`

### Asynchronous Operations

#### Asynchronous Read Functions

**JsonReadValue(key storeID, string path)**
- Asynchronously reads a value from the store
- Returns: Request UUID for callback identification
- Callback: Triggered via script communication system
- Example: `key reqID = JsonReadValue(store, "data");`

**JsonReadValueJson(key storeID, string path)**
- Asynchronously reads JSON structure from the store
- Returns: Request UUID for callback identification
- Example: `key reqID = JsonReadValueJson(store, "config");`

#### Asynchronous Take Functions

**JsonTakeValue(key storeID, string path)**
- Asynchronously reads and removes a value from the store
- Returns: Request UUID for callback identification
- Example: `key reqID = JsonTakeValue(store, "queue[0]");`

**JsonTakeValueJson(key storeID, string path)**
- Asynchronously reads and removes JSON structure from the store
- Returns: Request UUID for callback identification
- Example: `key reqID = JsonTakeValueJson(store, "batch_data");`

### Notecard Integration

#### Notecard Reading

**JsonReadNotecard(key storeID, string path, string notecardName)**
- Reads notecard content into JSON store
- Returns: Request UUID for callback identification
- Supports: Both notecard names and asset UUIDs
- Example: `key reqID = JsonReadNotecard(store, "config", "settings");`

#### Notecard Writing

**JsonWriteNotecard(key storeID, string path, string name)**
- Writes JSON store data to a new notecard
- Returns: Request UUID for callback identification
- Creates: New notecard in object inventory
- Example: `key reqID = JsonWriteNotecard(store, "", "backup_data");`

### Object Rezzing

#### Dynamic Object Creation

**JsonRezAtRoot(string objectName, vector position, vector velocity, rotation rot, string jsonData)**
- Rezzes an object with associated JSON data
- Returns: Request UUID for callback identification
- Features: Automatic JSON store creation for rezzed object
- Example: `key reqID = JsonRezAtRoot("MyObject", pos, vel, rot, "{}");`

## Technical Implementation

### Module Lifecycle Management

#### Initialization and Configuration

```csharp
public void Initialise(IConfigSource config)
{
    try
    {
        if ((m_config = config.Configs["JsonStore"]) == null)
        {
            // No configuration - module disabled
            return;
        }

        m_enabled = m_config.GetBoolean("Enabled", m_enabled);
    }
    catch (Exception e)
    {
        m_log.ErrorFormat("[JsonStoreScripts]: initialization error: {0}", e.Message);
        return;
    }

    if (m_enabled)
        m_log.DebugFormat("[JsonStoreScripts]: module is enabled");
}
```

#### Script Integration Setup

```csharp
public void RegionLoaded(Scene scene)
{
    if (m_enabled)
    {
        m_scene = scene;

        // Get script communication interface
        m_comms = m_scene.RequestModuleInterface<IScriptModuleComms>();
        if (m_comms == null)
        {
            m_log.ErrorFormat("[JsonStoreScripts]: ScriptModuleComms interface not defined");
            m_enabled = false;
            return;
        }

        // Get JsonStore interface
        m_store = m_scene.RequestModuleInterface<IJsonStoreModule>();
        if (m_store == null)
        {
            m_log.ErrorFormat("[JsonStoreScripts]: JsonModule interface not defined");
            m_enabled = false;
            return;
        }

        try
        {
            // Register script functions and constants
            m_comms.RegisterScriptInvocations(this);
            m_comms.RegisterConstants(this);
        }
        catch (Exception e)
        {
            m_log.WarnFormat("[JsonStoreScripts]: script method registration failed; {0}", e.Message);
            m_enabled = false;
        }
    }
}
```

### Script Function Implementation

#### Store Creation Implementation

```csharp
[ScriptInvocation]
public UUID JsonCreateStore(UUID hostID, UUID scriptID, string value)
{
    UUID uuid = UUID.Zero;
    if (!m_store.CreateStore(value, ref uuid))
        GenerateRuntimeError("Failed to create Json store");

    // Track store ownership by script
    lock (m_scriptStores)
    {
        if (!m_scriptStores.ContainsKey(scriptID))
            m_scriptStores[scriptID] = new HashSet<UUID>();

        m_scriptStores[scriptID].Add(uuid);
    }
    return uuid;
}
```

#### Data Access Implementation

```csharp
[ScriptInvocation]
public string JsonGetValue(UUID hostID, UUID scriptID, UUID storeID, string path)
{
    string value = String.Empty;
    m_store.GetValue(storeID, path, false, out value);
    return value;
}

[ScriptInvocation]
public int JsonSetValue(UUID hostID, UUID scriptID, UUID storeID, string path, string value)
{
    return m_store.SetValue(storeID, path, value, false) ? 1 : 0;
}
```

#### Asynchronous Operation Implementation

```csharp
[ScriptInvocation]
public UUID JsonReadValue(UUID hostID, UUID scriptID, UUID storeID, string path)
{
    UUID reqID = UUID.Random();
    Util.FireAndForget(
        o => DoJsonReadValue(scriptID, reqID, storeID, path, false),
        null, "JsonStoreScriptModule.DoJsonReadValue");
    return reqID;
}

private void DoJsonReadValue(UUID scriptID, UUID reqID, UUID storeID, string path, bool useJson)
{
    try
    {
        m_store.ReadValue(storeID, path, useJson,
            delegate(string value) { DispatchValue(scriptID, reqID, value); });
        return;
    }
    catch (Exception e)
    {
        m_log.InfoFormat("[JsonStoreScripts]: unable to retrieve value; {0}", e.ToString());
    }

    DispatchValue(scriptID, reqID, String.Empty);
}
```

### Script State Management

#### Script Reset Handling

```csharp
private void HandleScriptReset(uint localID, UUID itemID)
{
    HashSet<UUID> stores;

    lock (m_scriptStores)
    {
        if (!m_scriptStores.TryGetValue(itemID, out stores))
            return;
        m_scriptStores.Remove(itemID);
    }

    // Clean up all stores owned by the script
    foreach (UUID id in stores)
        m_store.DestroyStore(id);
}
```

#### Event Handler Registration

```csharp
public void AddRegion(Scene scene)
{
    scene.EventManager.OnScriptReset += HandleScriptReset;
    scene.EventManager.OnRemoveScript += HandleScriptReset;
}

public void RemoveRegion(Scene scene)
{
    scene.EventManager.OnScriptReset -= HandleScriptReset;
    scene.EventManager.OnRemoveScript -= HandleScriptReset;
}
```

### Notecard Operations

#### Notecard Reading Implementation

```csharp
private void DoJsonReadNotecard(
    UUID reqID, UUID hostID, UUID scriptID, UUID storeID, string path, string notecardIdentifier)
{
    UUID assetID;

    // Handle both notecard names and asset UUIDs
    if (!UUID.TryParse(notecardIdentifier, out assetID))
    {
        SceneObjectPart part = m_scene.GetSceneObjectPart(hostID);
        assetID = ScriptUtils.GetAssetIdFromItemName(part, notecardIdentifier, (int)AssetType.Notecard);
    }

    // Retrieve and validate asset
    AssetBase a = m_scene.AssetService.Get(assetID.ToString());
    if (a == null)
        GenerateRuntimeError(String.Format("Unable to find notecard asset {0}", assetID));

    if (a.Type != (sbyte)AssetType.Notecard)
        GenerateRuntimeError(String.Format("Invalid notecard asset {0}", assetID));

    try
    {
        // Parse notecard content
        string[] data = SLUtil.ParseNotecardToArray(a.Data);
        if (data.Length == 0)
        {
            result = m_store.SetValue(storeID, path, string.Empty, true) ? 1 : 0;
        }
        else
        {
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < data.Length; ++i)
                sb.AppendLine(data[i]);
            result = m_store.SetValue(storeID, path, sb.ToString(), true) ? 1 : 0;
        }

        m_comms.DispatchReply(scriptID, result, "", reqID.ToString());
        return;
    }
    catch (Exception e)
    {
        m_log.WarnFormat("[JsonStoreScripts]: Json parsing failed; {0}", e.Message);
    }

    GenerateRuntimeError(String.Format("Json parsing failed for {0}", assetID));
    m_comms.DispatchReply(scriptID, 0, "", reqID.ToString());
}
```

#### Notecard Writing Implementation

```csharp
private void DoJsonWriteNotecard(UUID reqID, UUID hostID, UUID scriptID, UUID storeID, string path, string name)
{
    string data;
    if (!m_store.GetValue(storeID, path, true, out data))
    {
        m_comms.DispatchReply(scriptID, 0, UUID.Zero.ToString(), reqID.ToString());
        return;
    }

    SceneObjectPart host = m_scene.GetSceneObjectPart(hostID);

    // Create new notecard asset
    UUID assetID = UUID.Random();
    AssetBase asset = new AssetBase(assetID, name, (sbyte)AssetType.Notecard, host.OwnerID.ToString());
    asset.Description = "Json store";

    // Format as proper notecard
    int textLength = data.Length;
    data = "Linden text version 2\n{\nLLEmbeddedItems version 1\n{\ncount 0\n}\nText length "
            + textLength.ToString() + "\n" + data + "}\n";

    asset.Data = Util.UTF8.GetBytes(data);
    m_scene.AssetService.Store(asset);

    // Create inventory item
    TaskInventoryItem taskItem = new TaskInventoryItem();
    taskItem.ResetIDs(host.UUID);
    taskItem.ParentID = host.UUID;
    taskItem.CreationDate = (uint)Util.UnixTimeSinceEpoch();
    taskItem.Name = asset.Name;
    taskItem.Description = asset.Description;
    taskItem.Type = (int)AssetType.Notecard;
    taskItem.InvType = (int)InventoryType.Notecard;
    taskItem.OwnerID = host.OwnerID;
    taskItem.CreatorID = host.OwnerID;
    taskItem.BasePermissions = (uint)PermissionMask.All;
    taskItem.CurrentPermissions = (uint)PermissionMask.All;
    taskItem.EveryonePermissions = 0;
    taskItem.NextPermissions = (uint)PermissionMask.All;
    taskItem.GroupID = host.GroupID;
    taskItem.GroupPermissions = 0;
    taskItem.Flags = 0;
    taskItem.PermsGranter = UUID.Zero;
    taskItem.PermsMask = 0;
    taskItem.AssetID = asset.FullID;

    host.Inventory.AddInventoryItem(taskItem, false);
    host.ParentGroup.InvalidateEffectivePerms();
    m_comms.DispatchReply(scriptID, 1, assetID.ToString(), reqID.ToString());
}
```

### Object Rezzing Implementation

#### Dynamic Object Creation

```csharp
private void DoJsonRezObject(UUID hostID, UUID scriptID, UUID reqID, string name, Vector3 pos, Vector3 vel, Quaternion rot, string param)
{
    // Validate rotation parameters
    if (Double.IsNaN(rot.X) || Double.IsNaN(rot.Y) || Double.IsNaN(rot.Z) || Double.IsNaN(rot.W))
    {
        GenerateRuntimeError("Invalid rez rotation");
        return;
    }

    // Get host object
    SceneObjectGroup host = m_scene.GetSceneObjectGroup(hostID);
    if (host == null)
    {
        GenerateRuntimeError(String.Format("Unable to find rezzing host '{0}'", hostID));
        return;
    }

    // Find inventory item
    TaskInventoryItem item = host.RootPart.Inventory.GetInventoryItem(name);
    if (item == null)
    {
        GenerateRuntimeError(String.Format("Unable to find object to rez '{0}'", name));
        return;
    }

    if (item.InvType != (int)InventoryType.Object)
    {
        GenerateRuntimeError("Can't create requested object; object is missing from database");
        return;
    }

    // Prepare objects for rezzing
    List<SceneObjectGroup> objlist;
    List<Vector3> veclist;
    Vector3 bbox = new Vector3();
    float offsetHeight;
    bool success = host.RootPart.Inventory.GetRezReadySceneObjects(item, out objlist, out veclist, out bbox, out offsetHeight);

    if (!success)
    {
        GenerateRuntimeError("Failed to create object");
        return;
    }

    // Check permissions
    int totalPrims = 0;
    foreach (SceneObjectGroup group in objlist)
        totalPrims += group.PrimCount;

    if (!m_scene.Permissions.CanRezObject(totalPrims, item.OwnerID, pos))
    {
        GenerateRuntimeError("Not allowed to create the object");
        return;
    }

    // Handle copy permissions
    if (!m_scene.Permissions.BypassPermissions())
    {
        if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
            host.RootPart.Inventory.RemoveInventoryItem(item.ItemID);
    }

    // Rez each object
    for (int i = 0; i < objlist.Count; i++)
    {
        SceneObjectGroup group = objlist[i];
        Vector3 curpos = pos + veclist[i];

        // Handle attachment state
        if (group.IsAttachment == false && group.RootPart.Shape.State != 0)
        {
            group.RootPart.AttachedPos = group.AbsolutePosition;
            group.RootPart.Shape.LastAttachPoint = (byte)group.AttachmentPoint;
        }

        // Add to scene
        group.RezzerID = host.RootPart.UUID;
        m_scene.AddNewSceneObject(group, true, curpos, rot, vel);

        // Create associated JSON store
        UUID storeID = group.UUID;
        if (!m_store.CreateStore(param, ref storeID))
        {
            GenerateRuntimeError("Unable to create jsonstore for new object");
            continue;
        }

        // Initialize scripts
        group.RootPart.SetDieAtEdge(true);
        group.CreateScriptInstances(0, true, m_scene.DefaultScriptEngine, 3);
        group.ResumeScripts();
        group.ScheduleGroupForFullUpdate();

        // Send callback to host script
        m_comms.DispatchReply(scriptID, objlist.Count - i - 1, group.RootPart.UUID.ToString(), reqID.ToString());
    }
}
```

### Path Processing

#### Path List Conversion

```csharp
protected static Regex m_ArrayPattern = new Regex("^([0-9]+|\\+)$");

private string ConvertList2Path(object[] pathlist)
{
    string path = "";
    for (int i = 0; i < pathlist.Length; i++)
    {
        string token = "";

        if (pathlist[i] is string)
        {
            token = pathlist[i].ToString();

            // Check for bare numbers that need bracket notation
            if (m_ArrayPattern.IsMatch(token))
                token = '[' + token + ']';
        }
        else if (pathlist[i] is int)
        {
            token = "[" + pathlist[i].ToString() + "]";
        }
        else
        {
            token = "." + pathlist[i].ToString() + ".";
        }

        path += token + ".";
    }

    return path;
}
```

## Performance Characteristics

### Resource Usage

- **Memory Footprint**: Moderate memory usage for script-store associations and callback tracking
- **CPU Impact**: Low CPU overhead - functions execute quickly with minimal processing
- **Network Usage**: No network usage - operates locally within region
- **Storage Impact**: Utilizes JsonStore for persistence with configurable limits

### Scalability Features

- **Per-Script Tracking**: Efficient tracking of store ownership per script
- **Asynchronous Operations**: Non-blocking operations prevent script engine stalling
- **Event-Driven Callbacks**: Efficient callback system using script communication framework
- **Resource Cleanup**: Automatic cleanup on script reset prevents resource leaks

### Performance Optimization

- **Direct Interface Access**: Efficient direct access to JsonStore functionality
- **Fire-and-Forget Pattern**: Asynchronous operations using thread pool for scalability
- **Minimal Locking**: Focused locking only where necessary for thread safety
- **Efficient Path Processing**: Optimized path parsing and validation routines

## Usage Examples

### Basic Store Operations

```lsl
// Create a JSON store
key store = JsonCreateStore("{\"name\":\"test\",\"value\":42}");

// Set a value
JsonSetValue(store, "name", "updated_name");

// Get a value
string name = JsonGetValue(store, "name");
llOwnerSay("Name: " + name);

// Test if store exists
if (JsonTestStore(store))
{
    llOwnerSay("Store is valid");
}

// Clean up
JsonDestroyStore(store);
```

### Complex Data Structures

```lsl
// Create a complex JSON structure
string json = "{\"config\":{\"database\":{\"host\":\"localhost\",\"port\":3306},\"features\":[\"json\",\"scripts\"]}}";
key store = JsonCreateStore(json);

// Access nested values
string host = JsonGetValue(store, "config.database.host");
integer port = (integer)JsonGetValue(store, "config.database.port");

// Work with arrays
integer arrayLen = JsonGetArrayLength(store, "config.features");
string feature0 = JsonGetValue(store, "config.features[0]");

// Add new array element
JsonSetValue(store, "config.features[+]", "networking");

// Get full JSON structure
string fullConfig = JsonGetJson(store, "config");
llOwnerSay("Full config: " + fullConfig);
```

### Asynchronous Operations

```lsl
key store;
key requestID;

default
{
    state_entry()
    {
        store = JsonCreateStore("{\"data\":\"initial\"}");

        // Start asynchronous read
        requestID = JsonReadValue(store, "data");
    }

    link_message(integer sender_num, integer num, string str, key id)
    {
        if (id == requestID)
        {
            llOwnerSay("Async result: " + str);
        }
    }
}
```

### Notecard Integration

```lsl
key store;
key readRequest;
key writeRequest;

default
{
    state_entry()
    {
        store = JsonCreateStore("{}");

        // Read configuration from notecard
        readRequest = JsonReadNotecard(store, "config", "settings");
    }

    link_message(integer sender_num, integer num, string str, key id)
    {
        if (id == readRequest)
        {
            if (num == 1)
            {
                llOwnerSay("Configuration loaded successfully");

                // Modify some settings
                JsonSetValue(store, "config.last_updated", llGetTimestamp());

                // Write back to a new notecard
                writeRequest = JsonWriteNotecard(store, "", "backup_settings");
            }
            else
            {
                llOwnerSay("Failed to load configuration");
            }
        }
        else if (id == writeRequest)
        {
            if (num == 1)
            {
                llOwnerSay("Backup saved with asset ID: " + str);
            }
            else
            {
                llOwnerSay("Failed to save backup");
            }
        }
    }
}
```

### Object Rezzing with Data

```lsl
key store;
key rezRequest;

default
{
    state_entry()
    {
        // Create initial data for the object to be rezzed
        string objectData = "{\"owner\":\"" + (string)llGetOwner() + "\",\"created\":\"" + llGetTimestamp() + "\"}";

        vector pos = llGetPos() + <1, 0, 0>;
        vector vel = <0, 0, 0>;
        rotation rot = ZERO_ROTATION;

        // Rez object with JSON data
        rezRequest = JsonRezAtRoot("DataObject", pos, vel, rot, objectData);
    }

    link_message(integer sender_num, integer num, string str, key id)
    {
        if (id == rezRequest)
        {
            if (num >= 0)
            {
                llOwnerSay("Object rezzed with UUID: " + str + ", " + (string)num + " remaining");
            }
        }
    }
}
```

### Path Utilities

```lsl
// Convert list to JSON path
list pathComponents = ["config", "database", "settings", 0];
string path = JsonList2Path(pathComponents);
// Results in: config.database.settings[0]

// Use the path
key store = JsonCreateStore("{\"config\":{\"database\":{\"settings\":[\"value1\",\"value2\"]}}}");
string value = JsonGetValue(store, path);
llOwnerSay("Value: " + value); // Outputs: value1
```

### Type Introspection

```lsl
key store = JsonCreateStore("{\"name\":\"test\",\"count\":42,\"active\":true,\"items\":[1,2,3]}");

// Check node types
integer nameType = JsonGetNodeType(store, "name");
if (nameType == JSON_NODETYPE_VALUE)
{
    integer valueType = JsonGetValueType(store, "name");
    if (valueType == JSON_VALUETYPE_STRING)
    {
        llOwnerSay("name is a string value");
    }
}

// Check array
integer itemsType = JsonGetNodeType(store, "items");
if (itemsType == JSON_NODETYPE_ARRAY)
{
    integer arrayLength = JsonGetArrayLength(store, "items");
    llOwnerSay("items array has " + (string)arrayLength + " elements");
}
```

## Integration Points

### With JsonStoreModule

- **Core Functionality**: All JSON operations delegate to JsonStoreModule
- **Store Management**: Uses JsonStore's create, destroy, and test operations
- **Data Operations**: Utilizes JsonStore's get, set, and remove functionality
- **Persistence**: Leverages JsonStore's data persistence mechanisms

### With Script Engine

- **Function Registration**: Registers all LSL functions via IScriptModuleComms
- **Constant Definitions**: Provides JSON-related constants to script engine
- **Event Callbacks**: Uses script communication system for asynchronous callbacks
- **Error Handling**: Integrates with script engine's error reporting system

### With Scene Management

- **Object Lifecycle**: Participates in scene object lifecycle events
- **Asset Operations**: Integrates with scene asset management for notecards
- **Inventory Management**: Creates and manages inventory items
- **Permissions**: Respects scene permission system for object operations

### with Script State Management

- **Script Reset Handling**: Cleans up stores when scripts reset
- **Script Removal**: Handles script removal events for resource cleanup
- **State Persistence**: Maintains script-store associations across operations
- **Resource Tracking**: Tracks resource usage per script for cleanup

## Security Features

### Script Isolation

- **Per-Script Stores**: Each script owns its stores independently
- **Automatic Cleanup**: Stores automatically cleaned up on script reset
- **Resource Limits**: Respects JsonStore resource limits and quotas
- **Permission Checks**: All operations subject to script permissions

### Data Protection

- **Store Ownership**: Only owning scripts can access their stores
- **Path Validation**: All JSON paths validated before operations
- **Error Isolation**: Errors don't affect other scripts or stores
- **Safe Operations**: All operations designed to be non-destructive to system

### Asset Security

- **Notecard Permissions**: Respects inventory permissions for notecard access
- **Asset Validation**: Validates asset types and ownership before operations
- **Inventory Integration**: Proper inventory item creation with appropriate permissions
- **Owner Verification**: Verifies object ownership for all operations

## Debugging and Troubleshooting

### Common Issues

1. **Functions Not Available**: Check that JsonStoreModule is enabled and loaded
2. **Store Creation Fails**: Verify JSON syntax and JsonStore limits
3. **Async Callbacks Missing**: Ensure link_message handler is implemented
4. **Notecard Operations Fail**: Check notecard exists and has proper permissions

### Diagnostic Procedures

1. **Module Loading**: Check logs for JsonStoreScriptModule loading messages
2. **Function Registration**: Verify script functions are properly registered
3. **Store Operations**: Test basic store create/destroy operations
4. **Callback System**: Verify script communication system is working

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
JsonStoreModule = true

[JsonStore]
Enabled = true
EnableObjectStore = true
MaxStringSpace = 10485760
```

### Debug Scripts

```lsl
// Basic functionality test
default
{
    state_entry()
    {
        // Test store creation
        key store = JsonCreateStore("{\"test\":true}");
        if (store == NULL_KEY)
        {
            llOwnerSay("ERROR: Store creation failed");
            return;
        }
        llOwnerSay("SUCCESS: Store created: " + (string)store);

        // Test value access
        string value = JsonGetValue(store, "test");
        llOwnerSay("Test value: " + value);

        // Test store destruction
        integer result = JsonDestroyStore(store);
        llOwnerSay("Store destroyed: " + (string)result);
    }
}
```

## Use Cases

### Configuration Management

- **Script Configuration**: Store and manage script configuration data
- **Settings Persistence**: Maintain settings across script resets
- **Configuration Import/Export**: Load configuration from notecards
- **Dynamic Reconfiguration**: Update configuration at runtime

### Data Processing

- **Structured Data Storage**: Store complex data structures in JSON format
- **Data Transformation**: Convert between different data formats
- **Batch Processing**: Process large datasets using JSON storage
- **Data Exchange**: Share data between scripts and objects

### Inter-Script Communication

- **Message Passing**: Use JSON stores for complex message passing
- **Shared State**: Maintain shared state between multiple scripts
- **Event Systems**: Implement event systems using JSON data
- **Coordination**: Coordinate activities between distributed scripts

### Content Creation

- **Dynamic Content**: Generate content dynamically using JSON templates
- **Asset Generation**: Create notecards and other assets programmatically
- **Object Rezzing**: Create objects with associated data
- **Inventory Management**: Manage dynamic inventory content

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via JsonStoreModule configuration
- Logging provides visibility into module loading decisions
- All LSL functions remain fully compatible

### Configuration Migration

When upgrading from previous versions:

- Verify JsonStoreModule is enabled to activate script functions
- Test script function availability after deployment
- Update any scripts that depend on JsonStore functionality
- Validate integration with existing scripted systems

### Deployment Considerations

- **JsonStore Dependency**: Verify JsonStoreModule is properly loaded and configured
- **Script Engine Compatibility**: Ensure script engine supports function registration
- **Performance Impact**: Monitor performance with increased JSON operations
- **Resource Usage**: Monitor JsonStore resource usage with script integration

## Configuration Examples

### Basic Configuration

```ini
[Modules]
JsonStoreModule = true  ; Automatically enables JsonStoreScriptModule

[JsonStore]
Enabled = true
```

### Development Configuration

```ini
[Modules]
JsonStoreModule = true

[JsonStore]
Enabled = true
EnableObjectStore = true
MaxStringSpace = 10485760  ; 10MB for development

[Logging]
LogLevel = DEBUG
```

### Production Configuration

```ini
[Modules]
JsonStoreModule = true

[JsonStore]
Enabled = true
EnableObjectStore = false      ; Disabled for security
MaxStringSpace = 2147483647    ; Maximum storage

[Logging]
LogLevel = INFO
```

### High-Performance Configuration

```ini
[Modules]
JsonStoreModule = true

[JsonStore]
Enabled = true
EnableObjectStore = true
MaxStringSpace = 104857600     ; 100MB for large datasets

[ScriptEngine]
MaxScriptEventQueue = 1024     ; Larger event queue for async operations
```

## Best Practices

### Script Development

1. **Resource Management**: Always clean up stores when no longer needed
2. **Error Handling**: Check return values and handle errors appropriately
3. **Async Operations**: Use asynchronous functions for large data operations
4. **Path Validation**: Validate JSON paths before operations

### Performance Optimization

1. **Batch Operations**: Group related operations to minimize overhead
2. **Efficient Paths**: Use efficient JSON path expressions
3. **Memory Management**: Clean up temporary stores and data
4. **Callback Handling**: Handle callbacks efficiently to prevent blocking

### Security Practices

1. **Data Validation**: Validate all input data before storing
2. **Permission Checks**: Verify permissions before operations
3. **Error Isolation**: Handle errors without affecting system stability
4. **Resource Limits**: Respect configured resource limits

## Future Enhancements

### Potential Improvements

1. **Enhanced Functions**: Additional LSL functions for advanced operations
2. **Performance Optimization**: Further performance improvements for large datasets
3. **Extended Integration**: Enhanced integration with other script systems
4. **Advanced Features**: Support for JSON schema validation and advanced queries

### Compatibility Considerations

1. **Script Engine Evolution**: Adapt to script engine updates and improvements
2. **JsonStore Updates**: Maintain compatibility with JsonStore enhancements
3. **LSL Standard**: Follow LSL standard evolution and new features
4. **Performance Requirements**: Scale with increasing performance demands

### Integration Opportunities

1. **Web Services**: Enhanced integration with web services and APIs
2. **Database Systems**: Direct integration with external database systems
3. **Monitoring Tools**: Enhanced integration with system monitoring
4. **Development Tools**: Better development and debugging tool integration