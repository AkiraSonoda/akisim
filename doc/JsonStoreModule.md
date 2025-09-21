# JsonStoreModule Technical Documentation

## Overview

The JsonStoreModule is a specialized data storage component for OpenSimulator/Akisim that provides persistent JSON data storage capabilities for LSL scripts. This optional non-shared region module enables scripts to create, manipulate, and persist structured JSON data across script resets and region restarts. The module serves as a powerful scripting tool for complex data management scenarios, offering hierarchical data storage, object-specific data binding, and comprehensive JSON manipulation capabilities. It's particularly valuable for creating sophisticated scripted objects that require persistent data storage, configuration management, or complex data structures.

## Architecture

The JsonStoreModule implements the following interfaces:
- `INonSharedRegionModule` - Per-region module lifecycle management
- `IJsonStoreModule` - JSON storage service interface contract

### Key Components

1. **JSON Store Management**
   - **Store Creation**: Dynamic creation of JSON stores with unique identifiers
   - **Store Persistence**: Thread-safe storage with memory management
   - **Store Lifecycle**: Automatic cleanup and garbage collection
   - **Store Statistics**: Performance monitoring and usage tracking

2. **Data Storage Engine**
   - **Hierarchical Storage**: Nested JSON object and array support
   - **Path-Based Access**: JSONPath-style data access and manipulation
   - **Type Safety**: Strong typing with value type detection
   - **Memory Management**: Configurable memory limits and string space tracking

3. **Object Store Integration**
   - **Object Binding**: Automatic store creation for scene objects
   - **Lifecycle Management**: Automatic cleanup when objects are removed
   - **Per-Object Isolation**: Independent data storage per object
   - **Scene Integration**: Deep integration with scene object lifecycle

4. **LSL Integration Layer**
   - **Function Registration**: Integration with script module communication system
   - **Event Handling**: Asynchronous data operations with callback support
   - **Type Conversion**: Automatic conversion between LSL and JSON types
   - **Error Handling**: Comprehensive error reporting and recovery

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
[Modules]
JsonStoreModule = true
```

### JsonStore Configuration

Configure in `[JsonStore]` section:
```ini
[JsonStore]
Enabled = true                    ; Enable JsonStore functionality
EnableObjectStore = true          ; Enable object-specific stores
MaxStringSpace = 2147483647       ; Maximum string storage (bytes)
```

### Memory Management

The module provides configurable memory limits:
- **MaxStringSpace**: Maximum total string storage per store
- **String Space Tracking**: Automatic memory usage monitoring
- **Garbage Collection**: Automatic cleanup of unused stores

### Default Behavior

- **Disabled by Default**: JsonStoreModule must be explicitly enabled
- **Object Stores Disabled**: Object-specific stores disabled by default for security
- **Unlimited Storage**: No memory limits by default (MaxStringSpace = Int32.MaxValue)
- **Persistent Storage**: Data persists across script resets

## Features

### LSL JSON Functions Implementation

The module provides the backend for various LSL JSON functions:

#### Store Management Functions

```lsl
// Create a new JSON store
key jsonCreateStore(string data);

// Destroy a JSON store
integer jsonDestroyStore(key store);

// Test if a store exists
integer jsonTestStore(key store);

// Get store statistics
list jsonGetStats(key store);
```

#### Data Manipulation Functions

```lsl
// Set a value in the store
integer jsonSetValue(key store, string path, string value);
integer jsonSetValueJson(key store, string path, string value);

// Get a value from the store
string jsonGetValue(key store, string path);
string jsonGetValueJson(key store, string path);

// Remove a value from the store
integer jsonRemoveValue(key store, string path);

// Get array length
integer jsonGetArrayLength(key store, string path);

// Get value type
integer jsonGetValueType(key store, string path);
integer jsonGetNodeType(key store, string path);
```

#### Asynchronous Operations

```lsl
// Asynchronous read operations
string jsonTakeValue(key store, string path);
string jsonTakeValueJson(key store, string path);

string jsonReadValue(key store, string path);
string jsonReadValueJson(key store, string path);
```

### Data Types and Structures

#### Supported JSON Types

The module supports all standard JSON data types:
- **String**: Text data with UTF-8 encoding
- **Number**: Integer and floating-point numbers
- **Boolean**: True/false values
- **Array**: Ordered collections of values
- **Object**: Key-value collections
- **Null**: Null values

#### Path Syntax

JSONPath-style syntax for accessing nested data:
```lsl
// Object access
jsonSetValue(store, "user.name", "John Doe");
jsonSetValue(store, "user.age", "30");

// Array access
jsonSetValue(store, "scores[0]", "100");
jsonSetValue(store, "scores[1]", "95");

// Nested structures
jsonSetValue(store, "users[0].profile.email", "john@example.com");
```

### Object Store Features

#### Automatic Object Binding

```lsl
// Automatically create store for current object
integer jsonAttachObjectStore(key objectID);

// Object store is automatically cleaned up when object is deleted
```

#### Object-Specific Data

Each object can have its own independent JSON store:
- Automatic cleanup on object removal
- Isolated data per object
- No cross-object data access
- Lifecycle synchronization

### Advanced Features

#### Memory Management

```csharp
// Check string space usage
if (map.StringSpace > m_maxStringSpace)
{
    m_log.WarnFormat("{0} exceeded string size; {1} bytes used of {2} limit",
                     storeID, map.StringSpace, m_maxStringSpace);
    return false;
}
```

#### Type Detection

```csharp
public JsonStoreValueType GetValueType(UUID storeID, string path)
{
    // Returns: Undefined, Value, Array, Object
    // Provides type information for dynamic scripting
}

public JsonStoreNodeType GetNodeType(UUID storeID, string path)
{
    // Returns detailed node type information
    // Enables type-safe operations
}
```

## Technical Implementation

### Store Management Architecture

#### Store Dictionary Management

```csharp
private RwLockedDictionary<UUID,JsonStore> m_JsonValueStore;

public bool CreateStore(string value, ref UUID result)
{
    if (result.IsZero())
        result = UUID.Random();

    JsonStore map = null;
    if (!m_enabled) return false;

    try
    {
        map = new JsonStore(value);
    }
    catch (Exception)
    {
        m_log.ErrorFormat("Unable to initialize store from {0}", value);
        return false;
    }

    m_JsonValueStore.Add(result, map);
    return true;
}
```

#### Thread-Safe Operations

```csharp
public bool SetValue(UUID storeID, string path, string value, bool useJson)
{
    if (!m_enabled) return false;

    JsonStore map = null;
    if (!m_JsonValueStore.TryGetValue(storeID, out map))
    {
        m_log.InfoFormat("Missing store {0}", storeID);
        return false;
    }

    try
    {
        lock (map)
        {
            if (map.StringSpace > m_maxStringSpace)
            {
                m_log.WarnFormat("{0} exceeded string size; {1} bytes used of {2} limit",
                                 storeID, map.StringSpace, m_maxStringSpace);
                return false;
            }

            return map.SetValue(path, value, useJson);
        }
    }
    catch (Exception e)
    {
        m_log.Error(string.Format("Unable to assign {0} to {1} in {2}", value, path, storeID), e);
    }

    return false;
}
```

### Object Store Implementation

#### Automatic Object Integration

```csharp
public bool AttachObjectStore(UUID objectID)
{
    if (!m_enabled) return false;
    if (!m_enableObjectStore) return false;

    SceneObjectPart sop = m_scene.GetSceneObjectPart(objectID);
    if (sop == null)
    {
        m_log.ErrorFormat("unable to attach to unknown object; {0}", objectID);
        return false;
    }

    if (m_JsonValueStore.ContainsKey(objectID))
        return true;

    JsonStore map = new JsonObjectStore(m_scene, objectID);
    m_JsonValueStore.Add(objectID, map);

    return true;
}
```

#### Lifecycle Management

```csharp
public void EventManagerOnObjectBeingRemovedFromScene(SceneObjectGroup obj)
{
    obj.ForEachPart(delegate(SceneObjectPart sop) { DestroyStore(sop.UUID); });
}
```

### Asynchronous Operations Implementation

#### Callback-Based Operations

```csharp
public void TakeValue(UUID storeID, string path, bool useJson, TakeValueCallback cback)
{
    if (!m_enabled)
    {
        cback(String.Empty);
        return;
    }

    JsonStore map = null;
    if (!m_JsonValueStore.TryGetValue(storeID, out map))
    {
        cback(String.Empty);
        return;
    }

    try
    {
        lock (map)
        {
            map.TakeValue(path, useJson, cback);
            return;
        }
    }
    catch (Exception e)
    {
        m_log.Error("unable to retrieve value", e);
    }

    cback(String.Empty);
}
```

### Memory Management Implementation

#### String Space Tracking

```csharp
// Memory usage is tracked at the JsonStore level
// Each store monitors its string space usage
// Operations are rejected if limits are exceeded

if (map.StringSpace > m_maxStringSpace)
{
    m_log.WarnFormat("{0} exceeded string size; {1} bytes used of {2} limit",
                     storeID, map.StringSpace, m_maxStringSpace);
    return false;
}
```

#### Statistics and Monitoring

```csharp
public JsonStoreStats GetStoreStats()
{
    JsonStoreStats stats;
    stats.StoreCount = m_JsonValueStore.Count;
    return stats;
}
```

## Performance Characteristics

### Resource Usage

- **Memory Footprint**: Variable - depends on stored JSON data size
- **CPU Impact**: Low CPU overhead - optimized JSON operations
- **Storage Usage**: Persistent memory storage for JSON data
- **Thread Safety**: Thread-safe operations using read-write locks

### Scalability Features

- **Store Isolation**: Independent stores prevent cross-contamination
- **Memory Limits**: Configurable memory limits prevent resource exhaustion
- **Efficient Storage**: Optimized JSON storage and retrieval
- **Cleanup Automation**: Automatic cleanup prevents memory leaks

### Performance Optimization

- **Lazy Loading**: Stores created on demand
- **Path Optimization**: Efficient path-based data access
- **Lock Optimization**: Minimal lock contention using read-write locks
- **Memory Monitoring**: Real-time memory usage tracking

## Usage Examples

### Basic JSON Store Operations

```lsl
// Create and populate a JSON store
default
{
    key store;

    state_entry()
    {
        // Create a new store with initial data
        string initial_data = "{\"name\":\"Test Object\",\"version\":1.0}";
        store = jsonCreateStore(initial_data);

        if (store != NULL_KEY)
        {
            llOwnerSay("Store created: " + (string)store);

            // Add more data
            jsonSetValue(store, "status", "active");
            jsonSetValue(store, "created", (string)llGetUnixTime());

            // Read the data back
            string name = jsonGetValue(store, "name");
            llOwnerSay("Object name: " + name);
        }
        else
        {
            llOwnerSay("Failed to create store");
        }
    }

    touch_start(integer total_number)
    {
        // Display store contents
        string full_data = jsonGetValueJson(store, "");
        llOwnerSay("Full store data: " + full_data);
    }
}
```

### Array Manipulation

```lsl
// Working with JSON arrays
default
{
    key store;

    state_entry()
    {
        // Create store with array data
        store = jsonCreateStore("{\"scores\":[],\"players\":[]}");

        // Add array elements
        jsonSetValue(store, "scores[0]", "100");
        jsonSetValue(store, "scores[1]", "95");
        jsonSetValue(store, "scores[2]", "88");

        jsonSetValue(store, "players[0]", "Alice");
        jsonSetValue(store, "players[1]", "Bob");
        jsonSetValue(store, "players[2]", "Charlie");

        // Get array length
        integer score_count = jsonGetArrayLength(store, "scores");
        llOwnerSay("Number of scores: " + (string)score_count);

        // Iterate through array
        integer i;
        for (i = 0; i < score_count; i++)
        {
            string player = jsonGetValue(store, "players[" + (string)i + "]");
            string score = jsonGetValue(store, "scores[" + (string)i + "]");
            llOwnerSay(player + ": " + score);
        }
    }
}
```

### Complex Data Structures

```lsl
// Managing complex nested data
default
{
    key store;

    state_entry()
    {
        // Create store for user management system
        store = jsonCreateStore("{}");

        // Add user data with nested structures
        jsonSetValue(store, "users[0].id", "1001");
        jsonSetValue(store, "users[0].profile.name", "John Doe");
        jsonSetValue(store, "users[0].profile.email", "john@example.com");
        jsonSetValue(store, "users[0].settings.notifications", "true");
        jsonSetValue(store, "users[0].settings.theme", "dark");

        jsonSetValue(store, "users[1].id", "1002");
        jsonSetValue(store, "users[1].profile.name", "Jane Smith");
        jsonSetValue(store, "users[1].profile.email", "jane@example.com");
        jsonSetValue(store, "users[1].settings.notifications", "false");
        jsonSetValue(store, "users[1].settings.theme", "light");

        // Query the data
        string user1_name = jsonGetValue(store, "users[0].profile.name");
        string user1_theme = jsonGetValue(store, "users[0].settings.theme");

        llOwnerSay("User: " + user1_name + ", Theme: " + user1_theme);

        // Get full user record as JSON
        string user1_data = jsonGetValueJson(store, "users[0]");
        llOwnerSay("User 1 data: " + user1_data);
    }

    touch_start(integer total_number)
    {
        // Add a new user dynamically
        integer user_count = jsonGetArrayLength(store, "users");
        string new_index = (string)user_count;

        jsonSetValue(store, "users[" + new_index + "].id", (string)(1000 + user_count + 1));
        jsonSetValue(store, "users[" + new_index + "].profile.name", "New User");
        jsonSetValue(store, "users[" + new_index + "].profile.email", "newuser@example.com");

        llOwnerSay("Added user at index " + new_index);
    }
}
```

### Configuration Management

```lsl
// Using JsonStore for configuration management
default
{
    key config_store;

    state_entry()
    {
        // Initialize configuration store
        config_store = jsonCreateStore("{}");

        load_default_config();
        llOwnerSay("Configuration system initialized");
    }

    load_default_config()
    {
        // Server settings
        jsonSetValue(config_store, "server.max_avatars", "50");
        jsonSetValue(config_store, "server.auto_backup", "true");
        jsonSetValue(config_store, "server.backup_interval", "3600");

        // Feature flags
        jsonSetValue(config_store, "features.voice_enabled", "true");
        jsonSetValue(config_store, "features.media_enabled", "true");
        jsonSetValue(config_store, "features.scripting_enabled", "true");

        // Security settings
        jsonSetValue(config_store, "security.require_payment_info", "false");
        jsonSetValue(config_store, "security.age_verification", "false");
        jsonSetValue(config_store, "security.max_script_memory", "65536");
    }

    get_config(string path)
    {
        string value = jsonGetValue(config_store, path);
        if (value == "")
        {
            llOwnerSay("Configuration key '" + path + "' not found");
            return "";
        }
        return value;
    }

    set_config(string path, string value)
    {
        if (jsonSetValue(config_store, path, value))
        {
            llOwnerSay("Configuration updated: " + path + " = " + value);
        }
        else
        {
            llOwnerSay("Failed to update configuration: " + path);
        }
    }

    listen(integer channel, string name, key id, string message)
    {
        // Simple configuration command interface
        list parts = llParseString2List(message, [" "], []);
        string command = llList2String(parts, 0);

        if (command == "get" && llGetListLength(parts) >= 2)
        {
            string path = llList2String(parts, 1);
            string value = get_config(path);
            llSay(0, path + " = " + value);
        }
        else if (command == "set" && llGetListLength(parts) >= 3)
        {
            string path = llList2String(parts, 1);
            string value = llList2String(parts, 2);
            set_config(path, value);
        }
        else if (command == "dump")
        {
            string full_config = jsonGetValueJson(config_store, "");
            llOwnerSay("Full configuration: " + full_config);
        }
    }
}
```

### Object-Specific Data Storage

```lsl
// Using object stores for per-object data
default
{
    state_entry()
    {
        // Attach a store to this object
        if (jsonAttachObjectStore(llGetKey()))
        {
            llOwnerSay("Object store attached successfully");

            // Store object-specific data
            key object_store = llGetKey();  // Object UUID is the store ID
            jsonSetValue(object_store, "object_type", "smart_door");
            jsonSetValue(object_store, "access_level", "2");
            jsonSetValue(object_store, "last_accessed", (string)llGetUnixTime());

            // Initialize access log array
            jsonSetValue(object_store, "access_log", "[]");
        }
        else
        {
            llOwnerSay("Failed to attach object store");
        }
    }

    touch_start(integer total_number)
    {
        key toucher = llDetectedKey(0);
        key object_store = llGetKey();

        // Check access level
        string access_level = jsonGetValue(object_store, "access_level");
        integer required_level = (integer)access_level;

        // Get user access level (simplified example)
        integer user_level = get_user_access_level(toucher);

        if (user_level >= required_level)
        {
            // Grant access and log it
            log_access(object_store, toucher, "granted");
            llSay(0, "Access granted");

            // Update last accessed time
            jsonSetValue(object_store, "last_accessed", (string)llGetUnixTime());
        }
        else
        {
            // Deny access and log it
            log_access(object_store, toucher, "denied");
            llSay(0, "Access denied");
        }
    }

    log_access(key store, key user, string result)
    {
        // Add entry to access log
        integer log_count = jsonGetArrayLength(store, "access_log");
        string log_index = (string)log_count;

        jsonSetValue(store, "access_log[" + log_index + "].user", (string)user);
        jsonSetValue(store, "access_log[" + log_index + "].timestamp", (string)llGetUnixTime());
        jsonSetValue(store, "access_log[" + log_index + "].result", result);

        // Keep only last 10 entries
        if (log_count >= 10)
        {
            jsonRemoveValue(store, "access_log[0]");
        }
    }

    integer get_user_access_level(key user)
    {
        // Simplified access level determination
        if (user == llGetOwner())
            return 10;  // Owner has full access
        else
            return 1;   // Others have basic access
    }
}
```

### Data Persistence and Recovery

```lsl
// Demonstrating data persistence across resets
key persistent_store;
string STORE_BACKUP_KEY = "backup_data";

default
{
    state_entry()
    {
        // Try to recover from previous session
        recover_data();
    }

    recover_data()
    {
        // Check if we have backup data in object description
        string desc = llGetObjectDesc();
        if (desc != "")
        {
            // Try to recreate store from backup
            persistent_store = jsonCreateStore(desc);
            if (persistent_store != NULL_KEY)
            {
                llOwnerSay("Data recovered from previous session");
                string last_save = jsonGetValue(persistent_store, "last_save");
                llOwnerSay("Last saved: " + last_save);
                return;
            }
        }

        // No backup found, create new store
        create_new_store();
    }

    create_new_store()
    {
        persistent_store = jsonCreateStore("{}");
        jsonSetValue(persistent_store, "created", (string)llGetUnixTime());
        jsonSetValue(persistent_store, "reset_count", "1");
        llOwnerSay("Created new data store");
    }

    backup_data()
    {
        if (persistent_store != NULL_KEY)
        {
            // Update last save time
            jsonSetValue(persistent_store, "last_save", (string)llGetUnixTime());

            // Get full store as JSON and save to object description
            string backup = jsonGetValueJson(persistent_store, "");
            llSetObjectDesc(backup);
            llOwnerSay("Data backed up to object description");
        }
    }

    touch_start(integer total_number)
    {
        if (persistent_store != NULL_KEY)
        {
            // Increment touch count
            string touches = jsonGetValue(persistent_store, "touch_count");
            integer count = (integer)touches + 1;
            jsonSetValue(persistent_store, "touch_count", (string)count);

            llOwnerSay("Touch count: " + (string)count);

            // Backup data every 10 touches
            if (count % 10 == 0)
            {
                backup_data();
            }
        }
    }

    changed(integer change)
    {
        if (change & CHANGED_INVENTORY)
        {
            // Backup data when inventory changes
            backup_data();
        }
    }

    on_rez(integer start_param)
    {
        // Backup data when object is rezzed
        backup_data();
    }
}
```

## Integration Points

### With Script Engines

- **Function Registration**: Integrates with script module communication system
- **Event Delivery**: Provides asynchronous callback operations
- **Type Conversion**: Automatic conversion between LSL and JSON types
- **Error Handling**: Comprehensive error reporting to scripts

### With Scene Management

- **Object Lifecycle**: Integrates with scene object creation and removal
- **Event Subscription**: Subscribes to scene events for automatic cleanup
- **Resource Management**: Participates in scene resource management
- **Service Registration**: Registers as IJsonStoreModule service

### With Memory Management

- **Memory Tracking**: Monitors string space usage for each store
- **Limit Enforcement**: Enforces configurable memory limits
- **Cleanup Automation**: Automatic cleanup of unused stores
- **Resource Protection**: Prevents memory exhaustion through limits

### With Configuration System

- **Configuration Loading**: Loads settings from JsonStore configuration section
- **Feature Toggles**: Supports enabling/disabling object stores
- **Memory Limits**: Configurable memory usage limits
- **Runtime Configuration**: Dynamic configuration changes

## Security Features

### Access Control

- **Store Isolation**: Stores are isolated and cannot access each other
- **Object Binding**: Object stores are bound to specific scene objects
- **Permission Validation**: Proper permission checking for store operations
- **Cleanup Automation**: Automatic cleanup prevents data leaks

### Memory Protection

- **Memory Limits**: Configurable limits prevent memory exhaustion
- **Usage Monitoring**: Real-time monitoring of memory usage
- **Limit Enforcement**: Operations rejected when limits are exceeded
- **Garbage Collection**: Automatic cleanup of unused resources

### Data Security

- **Type Safety**: Strong typing prevents data corruption
- **Input Validation**: Comprehensive validation of JSON data
- **Error Isolation**: Errors don't affect other stores or operations
- **Safe Operations**: All operations are safe and non-destructive by design

## Debugging and Troubleshooting

### Common Issues

1. **Store Not Created**: Check JsonStore configuration and enable status
2. **Memory Limit Exceeded**: Check MaxStringSpace configuration and usage
3. **Object Store Disabled**: Verify EnableObjectStore is set to true
4. **Data Not Persisting**: Ensure proper store lifecycle management

### Diagnostic Procedures

1. **Module Loading**: Check logs for JsonStoreModule loading messages
2. **Store Statistics**: Use GetStoreStats() to monitor store usage
3. **Memory Usage**: Monitor string space usage and limits
4. **Configuration Validation**: Verify JsonStore configuration section

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
MaxStringSpace = 2147483647
```

### Debug Methods

```csharp
// Monitor store statistics
public JsonStoreStats GetStoreStats()
{
    JsonStoreStats stats;
    stats.StoreCount = m_JsonValueStore.Count;
    return stats;
}

// Check store existence
public bool TestStore(UUID storeID)
{
    return m_JsonValueStore.ContainsKey(storeID);
}
```

## Use Cases

### Data Persistence

- **Configuration Storage**: Persistent configuration data for objects
- **State Management**: Complex state information across script resets
- **User Preferences**: Store user-specific settings and preferences
- **Game Progress**: Track progress and achievements in scripted games

### Structured Data Management

- **Inventory Systems**: Complex inventory management with hierarchical data
- **User Management**: User profiles and account information
- **Content Management**: Structured content storage and retrieval
- **Analytics**: Store and analyze usage data and metrics

### Inter-Object Communication

- **Shared Data**: Shared data structures between multiple objects
- **Message Queuing**: Implement message queues using JSON arrays
- **Event Logging**: Centralized event logging and audit trails
- **Coordination**: Object coordination through shared data stores

### Advanced Scripting

- **Dynamic Configuration**: Runtime configuration changes and management
- **Complex Algorithms**: Support for complex algorithmic data structures
- **Template Systems**: Template-based content generation
- **Workflow Management**: Complex workflow state management

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[Modules]` configuration section includes `JsonStoreModule = true`
- Test JSON store functionality after deployment
- Update memory limit configurations as needed
- Validate object store functionality if enabled

### Deployment Considerations

- **Memory Planning**: Plan memory usage based on expected data volumes
- **Object Store Security**: Consider security implications of object stores
- **Performance Impact**: Monitor performance impact of large data sets
- **Backup Strategies**: Implement backup strategies for critical data

## Configuration Examples

### Basic Configuration

```ini
[Modules]
JsonStoreModule = true

[JsonStore]
Enabled = true
```

### Production Configuration

```ini
[Modules]
JsonStoreModule = true

[JsonStore]
Enabled = true
EnableObjectStore = false        ; Disable for security in production
MaxStringSpace = 1048576         ; 1MB limit per store
```

### Development Configuration

```ini
[Modules]
JsonStoreModule = true

[JsonStore]
Enabled = true
EnableObjectStore = true         ; Enable for testing
MaxStringSpace = 2147483647      ; No limits for development

[Logging]
LogLevel = DEBUG
```

### High-Performance Configuration

```ini
[Modules]
JsonStoreModule = true

[JsonStore]
Enabled = true
EnableObjectStore = true
MaxStringSpace = 10485760        ; 10MB limit for high-volume usage
```

## Best Practices

### Script Development

1. **Store Lifecycle**: Always clean up stores when no longer needed
2. **Memory Management**: Monitor memory usage with large data sets
3. **Error Handling**: Implement proper error handling for store operations
4. **Data Validation**: Validate JSON data before storing

### Performance Guidelines

1. **Memory Efficiency**: Use appropriate data structures for your needs
2. **Path Optimization**: Use efficient path patterns for data access
3. **Batch Operations**: Group related operations for better performance
4. **Cleanup Procedures**: Implement proper cleanup procedures

### Security Practices

1. **Access Control**: Implement proper access control for sensitive data
2. **Data Validation**: Validate all input data before storage
3. **Memory Limits**: Set appropriate memory limits for production
4. **Audit Logging**: Implement audit logging for sensitive operations

## Future Enhancements

### Potential Improvements

1. **Performance Optimization**: Enhanced performance for large data sets
2. **Query Language**: More sophisticated query capabilities
3. **Data Export**: Enhanced data export and import capabilities
4. **Backup Integration**: Integrated backup and restore functionality

### Compatibility Considerations

1. **JSON Standards**: Support for evolving JSON standards
2. **Performance Scaling**: Optimization for larger data volumes
3. **Security Standards**: Implementation of evolving security practices
4. **Integration Evolution**: Enhanced integration with other modules

### Integration Opportunities

1. **Database Integration**: Integration with external database systems
2. **Web Services**: Enhanced web service integration capabilities
3. **Monitoring Tools**: Better integration with monitoring systems
4. **Development Tools**: Enhanced development and debugging tools