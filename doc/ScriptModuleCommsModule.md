# ScriptModuleCommsModule Technical Documentation

## Overview

The ScriptModuleCommsModule is a critical infrastructure component for OpenSimulator/Akisim that provides a sophisticated bidirectional communication bridge between LSL scripts and region modules. This non-shared region module serves as the foundational layer for script-to-module interoperability, enabling scripts to invoke functions defined in region modules and allowing modules to register constants, functions, and event handlers that scripts can access. It's essential for extending script functionality beyond the core LSL API and enables powerful custom functionality through module integration.

## Architecture

The ScriptModuleCommsModule implements the following interfaces:
- `INonSharedRegionModule` - Per-region module lifecycle management
- `IScriptModuleComms` - Script-to-module communication contract

### Key Components

1. **Function Registration System**
   - **Dynamic Method Registration**: Automatic discovery and registration of module methods marked with attributes
   - **Type-Safe Invocation**: Runtime type checking and parameter validation for registered functions
   - **Return Type Mapping**: Comprehensive mapping between .NET types and LSL types for seamless data exchange
   - **Delegate Creation**: Dynamic delegate generation for efficient function invocation from scripts

2. **Constant Registration System**
   - **Global Constants**: Registration of module-defined constants accessible to all scripts
   - **Attribute-Based Discovery**: Automatic registration of fields marked with ScriptConstantAttribute
   - **Type-Safe Storage**: Thread-safe storage and retrieval of typed constant values
   - **Runtime Registration**: Dynamic constant registration during module initialization

3. **Event Communication System**
   - **Script Event Dispatch**: Bidirectional event communication between modules and scripts
   - **Command Processing**: Structured command/response pattern for complex interactions
   - **Event Routing**: Efficient routing of events to appropriate script handlers
   - **Response Handling**: Automated response delivery back to originating scripts

4. **Type System Integration**
   - **LSL Type Mapping**: Comprehensive mapping between LSL types and .NET types
   - **Parameter Conversion**: Automatic parameter conversion for cross-boundary calls
   - **Return Value Processing**: Type-safe return value conversion and validation
   - **Collection Support**: Support for complex types including lists and arrays

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
[Modules]
ScriptModuleCommsModule = true
```

### Default Behavior

- **Enabled by Default**: ScriptModuleCommsModule loads by default as it's essential for script functionality
- **No Configuration Required**: Works out-of-the-box without additional configuration
- **Automatic Discovery**: Automatically discovers and registers module functions and constants
- **Per-Region Isolation**: Each region gets its own instance for proper isolation

### Integration Dependencies

- **Script Engine**: Requires a functioning script engine (YEngine, XEngine) for script communication
- **Region Framework**: Integrates with OpenSim region infrastructure for module discovery
- **Event System**: Relies on the region event system for script-module communication
- **Type System**: Depends on .NET reflection for dynamic method discovery and invocation

## Features

### Function Registration and Invocation

#### Automatic Function Discovery

The module automatically discovers functions in region modules marked with `ScriptInvocationAttribute`:

```csharp
public class MyModule : IRegionModuleBase
{
    [ScriptInvocation]
    public string MyScriptFunction(UUID hostId, UUID scriptId, string parameter)
    {
        // Function implementation
        return "result";
    }
}
```

#### Manual Function Registration

Modules can manually register functions for script invocation:

```csharp
// Register a single method
scriptComms.RegisterScriptInvocation(this, "MyMethodName");

// Register multiple methods
scriptComms.RegisterScriptInvocation(this, new string[] { "Method1", "Method2" });

// Register all attributed methods
scriptComms.RegisterScriptInvocations(this);
```

#### LSL Function Invocation

Scripts can invoke registered module functions using the modInvoke LSL functions:

```lsl
// Invoke function returning string
string result = llModInvokeS("MyScriptFunction", ["parameter"]);

// Invoke function returning integer
integer value = llModInvokeI("GetNumberValue", []);

// Invoke function returning float
float number = llModInvokeF("CalculateValue", [1.5]);

// Invoke function returning key (UUID)
key id = llModInvokeK("GenerateUUID", []);

// Invoke function returning vector
vector pos = llModInvokeV("GetPosition", []);

// Invoke function returning rotation
rotation rot = llModInvokeR("GetRotation", []);

// Invoke function returning list
list items = llModInvokeL("GetItemList", []);

// Invoke function with no return value
llModInvokeN("DoSomething", ["parameter"]);
```

### Constant Registration and Access

#### Automatic Constant Discovery

The module automatically registers constants marked with `ScriptConstantAttribute`:

```csharp
public class MyModule : IRegionModuleBase
{
    [ScriptConstant]
    public static readonly int MY_CONSTANT = 42;

    [ScriptConstant]
    public static readonly string MY_STRING_CONSTANT = "Hello World";
}
```

#### Manual Constant Registration

Modules can manually register constants:

```csharp
// Register individual constants
scriptComms.RegisterConstant("MY_CONSTANT", 42);
scriptComms.RegisterConstant("MY_STRING", "Hello");

// Register all attributed constants
scriptComms.RegisterConstants(this);
```

#### LSL Constant Access

Scripts access registered constants through the script engine's constant resolution system:

```lsl
// Constants become available as global identifiers
integer value = MY_CONSTANT;
string text = MY_STRING_CONSTANT;
```

### Event Communication System

#### Script-to-Module Events

Scripts can raise events that modules can listen for:

```csharp
// In module: Subscribe to script events
scriptComms.OnScriptCommand += HandleScriptCommand;

private void HandleScriptCommand(UUID script, string id, string module, string command, string data)
{
    // Handle the event from script
    if (module == "MyModule" && command == "DoSomething")
    {
        // Process the command
        ProcessCommand(script, data);

        // Send response back to script
        scriptComms.DispatchReply(script, 200, "Success", id);
    }
}
```

```lsl
// In script: Raise events for modules
llModuleComms("MyModule", "DoSomething", "data");
```

#### Module-to-Script Events

Modules can send events directly to scripts:

```csharp
// Send response to specific script
scriptComms.DispatchReply(scriptId, responseCode, responseText, requestId);

// Raise events for script handling
scriptComms.RaiseEvent(scriptId, eventId, moduleName, command, data);
```

```lsl
// In script: Handle module responses
link_message(integer sender_num, integer num, string str, key id)
{
    if (sender_num == -1) // Response from module
    {
        // num = response code
        // str = response text
        // id = request id
        llOwnerSay("Module response: " + str);
    }
}
```

## Technical Implementation

### Function Registration Architecture

#### Method Discovery and Validation

```csharp
public void RegisterScriptInvocations(IRegionModuleBase target)
{
    foreach(MethodInfo method in target.GetType().GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
    {
        if(method.GetCustomAttributes(typeof(ScriptInvocationAttribute), true).Any())
        {
            if(method.IsStatic)
                RegisterScriptInvocation(target.GetType(), method);
            else
                RegisterScriptInvocation(target, method);
        }
    }
}
```

#### Dynamic Delegate Creation

```csharp
public void RegisterScriptInvocation(object target, MethodInfo mi)
{
    // Create appropriate delegate type based on method signature
    Type delegateType = typeof(void);
    List<Type> typeArgs = mi.GetParameters().Select(p => p.ParameterType).ToList();

    if (mi.ReturnType == typeof(void))
    {
        delegateType = Expression.GetActionType(typeArgs.ToArray());
    }
    else
    {
        typeArgs.Add(mi.ReturnType);
        delegateType = Expression.GetFuncType(typeArgs.ToArray());
    }

    // Create delegate for efficient invocation
    Delegate fcall;
    if (!(target is Type))
        fcall = Delegate.CreateDelegate(delegateType, target, mi);
    else
        fcall = Delegate.CreateDelegate(delegateType, (Type)target, mi.Name);

    // Store function metadata for runtime lookup
    m_scriptInvocation[fcall.Method.Name] =
        new ScriptInvocationData(fcall.Method.Name, fcall, parmTypes, fcall.Method.ReturnType);
}
```

#### Function Invocation System

```csharp
public object InvokeOperation(UUID hostid, UUID scriptid, string fname, params object[] parms)
{
    // Prepare parameter list with required host/script IDs
    List<object> olist = new List<object>();
    olist.Add(hostid);
    olist.Add(scriptid);
    foreach (object o in parms)
        olist.Add(o);

    // Lookup and invoke registered function
    Delegate fn = LookupScriptInvocation(fname);
    return fn.DynamicInvoke(olist.ToArray());
}
```

### Type Mapping System

#### LSL to .NET Type Resolution

```csharp
public string LookupModInvocation(string fname)
{
    ScriptInvocationData sid;
    if (m_scriptInvocation.TryGetValue(fname, out sid))
    {
        // Map .NET return types to LSL function signatures
        if (sid.ReturnType == typeof(string))
            return "modInvokeS";
        else if (sid.ReturnType == typeof(int))
            return "modInvokeI";
        else if (sid.ReturnType == typeof(float))
            return "modInvokeF";
        else if (sid.ReturnType == typeof(UUID))
            return "modInvokeK";
        else if (sid.ReturnType == typeof(OpenMetaverse.Vector3))
            return "modInvokeV";
        else if (sid.ReturnType == typeof(OpenMetaverse.Quaternion))
            return "modInvokeR";
        else if (sid.ReturnType == typeof(object[]))
            return "modInvokeL";
        else if (sid.ReturnType == typeof(void))
            return "modInvokeN";
    }
    return null;
}
```

### Constant Management System

#### Thread-Safe Constant Storage

```csharp
private RwLockedDictionary<string,object> m_constants =
    new RwLockedDictionary<string, object>();

public void RegisterConstant(string cname, object value)
{
    m_constants.Add(cname, value);
}

public object LookupModConstant(string cname)
{
    object value = null;
    if (m_constants.TryGetValue(cname, out value))
        return value;
    return null;
}
```

#### Attribute-Based Constant Discovery

```csharp
public void RegisterConstants(IRegionModuleBase target)
{
    foreach (FieldInfo field in target.GetType().GetFields(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
    {
        if (field.GetCustomAttributes(typeof(ScriptConstantAttribute), true).Any())
        {
            RegisterConstant(field.Name, field.GetValue(target));
        }
    }
}
```

### Event Communication Implementation

#### Event Dispatching System

```csharp
public void RaiseEvent(UUID script, string id, string module, string command, string k)
{
    ScriptCommand c = OnScriptCommand;
    if (c == null)
        return;
    c(script, id, module, command, k);
}

public void DispatchReply(UUID script, int code, string text, string k)
{
    if (m_scriptModule == null)
        return;

    Object[] args = new Object[] {-1, code, text, k};
    m_scriptModule.PostScriptEvent(script, "link_message", args);
}
```

## Performance Characteristics

### Resource Usage

- **Memory Footprint**: Low memory usage - stores function delegates and constant references
- **CPU Impact**: Minimal CPU overhead - uses efficient delegate invocation
- **Lookup Performance**: O(1) constant and function lookup using hash tables
- **Thread Safety**: Thread-safe operations using RwLockedDictionary for concurrent access

### Scalability Features

- **Per-Region Isolation**: Each region has its own module instance preventing cross-contamination
- **Efficient Caching**: Function delegates cached for fast repeated invocation
- **Lazy Registration**: Functions and constants registered on-demand during module initialization
- **Memory Efficiency**: Minimal memory overhead per registered function/constant

### Performance Optimization

- **Delegate Caching**: Pre-compiled delegates for maximum invocation speed
- **Hash-Based Lookup**: O(1) lookup time for functions and constants
- **Type Validation**: Compile-time type validation reduces runtime overhead
- **Efficient Parameter Passing**: Direct parameter passing without unnecessary copying

## Usage Examples

### Basic Module Integration

```csharp
public class MyCustomModule : ISharedRegionModule
{
    private IScriptModuleComms m_scriptComms;

    public void RegionLoaded(Scene scene)
    {
        m_scriptComms = scene.RequestModuleInterface<IScriptModuleComms>();
        if (m_scriptComms != null)
        {
            // Register functions and constants
            m_scriptComms.RegisterScriptInvocations(this);
            m_scriptComms.RegisterConstants(this);

            // Subscribe to script events
            m_scriptComms.OnScriptCommand += HandleScriptCommand;
        }
    }

    [ScriptInvocation]
    public string GetModuleInfo(UUID hostId, UUID scriptId)
    {
        return "MyCustomModule v1.0";
    }

    [ScriptConstant]
    public static readonly int MODULE_VERSION = 1;

    private void HandleScriptCommand(UUID script, string id, string module, string command, string data)
    {
        if (module == "MyCustomModule")
        {
            // Process command and send response
            m_scriptComms.DispatchReply(script, 200, "Command processed", id);
        }
    }
}
```

### Advanced Function Registration

```csharp
public class AdvancedModule : INonSharedRegionModule
{
    [ScriptInvocation]
    public vector CalculatePosition(UUID hostId, UUID scriptId, vector start, vector end, float factor)
    {
        // Complex calculation returning a vector
        return start + (end - start) * factor;
    }

    [ScriptInvocation]
    public list GetObjectData(UUID hostId, UUID scriptId, string objectName)
    {
        // Return complex data as list
        return new object[] { objectName, 1.0f, new Vector3(1, 2, 3) };
    }

    [ScriptConstant]
    public static readonly string MODULE_NAME = "AdvancedModule";

    [ScriptConstant]
    public static readonly float DEFAULT_FACTOR = 0.5f;
}
```

### Script Usage Examples

```lsl
// Using module functions
default
{
    state_entry()
    {
        // Get module information
        string info = llModInvokeS("GetModuleInfo", []);
        llOwnerSay("Module: " + info);

        // Use module constants
        llOwnerSay("Version: " + (string)MODULE_VERSION);

        // Calculate position with module function
        vector start = <0, 0, 0>;
        vector end = <10, 10, 10>;
        vector result = llModInvokeV("CalculatePosition", [start, end, 0.5]);
        llOwnerSay("Calculated position: " + (string)result);

        // Get complex data
        list data = llModInvokeL("GetObjectData", ["TestObject"]);
        llOwnerSay("Object data: " + llDumpList2String(data, ", "));

        // Send command to module
        llModuleComms("MyCustomModule", "ProcessData", "sample data");
    }

    link_message(integer sender_num, integer num, string str, key id)
    {
        if (sender_num == -1) // Response from module
        {
            llOwnerSay("Module response: " + str + " (code: " + (string)num + ")");
        }
    }
}
```

### Database Integration Example

```csharp
public class DatabaseModule : ISharedRegionModule
{
    [ScriptInvocation]
    public string SaveData(UUID hostId, UUID scriptId, string table, string key, string data)
    {
        try
        {
            // Save to database
            DatabaseService.Save(table, key, data);
            return "SUCCESS";
        }
        catch (Exception e)
        {
            return "ERROR: " + e.Message;
        }
    }

    [ScriptInvocation]
    public string LoadData(UUID hostId, UUID scriptId, string table, string key)
    {
        try
        {
            return DatabaseService.Load(table, key);
        }
        catch (Exception e)
        {
            return "ERROR: " + e.Message;
        }
    }

    [ScriptConstant]
    public static readonly string DEFAULT_TABLE = "script_data";
}
```

## Integration Points

### With Script Engines

- **YEngine Integration**: Direct integration with YEngine for function and constant resolution
- **XEngine Compatibility**: Compatible with XEngine for legacy script support
- **Function Resolution**: Provides function metadata for script compilation and runtime
- **Type System Integration**: Seamless type conversion between script and module boundaries

### With Region Framework

- **Module Discovery**: Automatic discovery of modules implementing script communication
- **Lifecycle Management**: Proper integration with region module lifecycle events
- **Service Registration**: Registers as IScriptModuleComms service for module access
- **Event Integration**: Integrates with region event system for script-module communication

### With LSL API

- **modInvoke Functions**: Provides backend implementation for llModInvoke* LSL functions
- **Constant Resolution**: Integrates with script engine constant resolution system
- **Event Handling**: Supports llModuleComms and link_message event handling
- **Type Conversion**: Automatic conversion between LSL and .NET types

### With Module System

- **Attribute Discovery**: Automatic discovery of attributed methods and fields
- **Registration API**: Provides API for manual function and constant registration
- **Event Publishing**: Event system for module-to-script communication
- **Interface Implementation**: Standardized interface for script communication

## Security Features

### Function Access Control

- **Method Validation**: Only methods explicitly marked with attributes are accessible
- **Parameter Validation**: Runtime parameter type checking and validation
- **Return Type Safety**: Type-safe return value handling
- **Access Isolation**: Per-region isolation prevents cross-region access

### Constant Protection

- **Read-Only Access**: Constants are read-only from script perspective
- **Type Safety**: Type-safe constant storage and retrieval
- **Scope Isolation**: Constants scoped to specific modules and regions
- **Attribute Control**: Only explicitly marked fields are exposed

### Event Security

- **Script Identification**: All events include script and host UUID for identification
- **Module Validation**: Events tagged with module identification for proper routing
- **Response Validation**: Responses validated and routed to correct scripts
- **Isolation Enforcement**: Events isolated to appropriate region and script contexts

## Debugging and Troubleshooting

### Common Issues

1. **Functions Not Available**: Check that module implements IRegionModuleBase and registers functions
2. **Constants Not Found**: Verify fields are marked with ScriptConstantAttribute
3. **Type Mismatches**: Ensure parameter types match between module and script calls
4. **Module Not Loaded**: Verify module is properly loaded and ScriptModuleCommsModule is enabled

### Diagnostic Procedures

1. **Function Registration**: Check logs for function registration messages
2. **Module Discovery**: Verify modules are discovered during RegionLoaded
3. **Type Validation**: Check for type mismatch errors in logs
4. **Event Routing**: Verify event handlers are properly registered

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
ScriptModuleCommsModule = true
```

### Debug Methods

```csharp
// List all registered functions
Delegate[] functions = scriptComms.GetScriptInvocationList();
foreach (Delegate func in functions)
{
    m_log.DebugFormat("Registered function: {0}", func.Method.Name);
}

// List all registered constants
Dictionary<string, object> constants = scriptComms.GetConstants();
foreach (KeyValuePair<string, object> kvp in constants)
{
    m_log.DebugFormat("Registered constant: {0} = {1}", kvp.Key, kvp.Value);
}
```

## Use Cases

### Module Extension APIs

- **Custom Functionality**: Expose custom module functionality to scripts
- **Service Integration**: Integrate external services with script environment
- **Database Access**: Provide script access to database operations
- **Web Services**: Enable script interaction with web APIs

### Configuration and Constants

- **Module Settings**: Expose module configuration as script constants
- **System Information**: Provide system status and information to scripts
- **Feature Flags**: Control script behavior through module-defined constants
- **Version Information**: Expose module and system version information

### Inter-Module Communication

- **Event Broadcasting**: Broadcast events from scripts to multiple modules
- **Service Coordination**: Coordinate multiple services through script orchestration
- **Data Exchange**: Exchange data between modules through script intermediation
- **Workflow Management**: Manage complex workflows involving multiple modules

### Advanced Scripting

- **Extended LSL**: Extend LSL functionality beyond core capabilities
- **Complex Operations**: Provide complex operations not feasible in pure LSL
- **Performance Enhancement**: Offload computationally intensive operations to modules
- **Resource Access**: Provide script access to system resources and services

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the CoreModuleFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[Modules]` configuration section includes `ScriptModuleCommsModule = true`
- Test script-module communication after deployment
- Update any scripts that depend on module functions
- Validate constant registration and access

### Deployment Considerations

- **Script Engine Integration**: Ensure compatible script engine is available
- **Module Dependencies**: Verify all dependent modules are properly loaded
- **Configuration Validation**: Validate all configuration sections are proper
- **Performance Testing**: Test script-module communication performance

## Configuration Examples

### Basic Configuration

```ini
[Modules]
ScriptModuleCommsModule = true
```

### Development Configuration

```ini
[Modules]
ScriptModuleCommsModule = true

[Logging]
LogLevel = DEBUG
```

### Production Configuration

```ini
[Modules]
ScriptModuleCommsModule = true

[Logging]
LogLevel = INFO
```

## Best Practices

### Module Development

1. **Attribute Usage**: Use ScriptInvocation and ScriptConstant attributes consistently
2. **Parameter Validation**: Validate all parameters in registered functions
3. **Error Handling**: Implement comprehensive error handling in module functions
4. **Documentation**: Document all script-accessible functions and constants

### Script Development

1. **Function Discovery**: Use module documentation to discover available functions
2. **Type Awareness**: Be aware of LSL-to-.NET type mapping for parameters
3. **Error Handling**: Handle function call errors appropriately in scripts
4. **Performance**: Cache function results when appropriate to minimize calls

### Performance Guidelines

1. **Function Design**: Design functions for efficiency and minimal resource usage
2. **Constant Usage**: Use constants instead of repeated function calls where possible
3. **Event Optimization**: Minimize event frequency to reduce communication overhead
4. **Resource Management**: Properly manage resources in module functions

## Future Enhancements

### Potential Improvements

1. **Async Support**: Support for asynchronous function calls from scripts
2. **Enhanced Types**: Support for more complex .NET types in script communication
3. **Performance Metrics**: Built-in performance monitoring for function calls
4. **Security Enhancement**: Enhanced security features for function access control

### Compatibility Considerations

1. **Script Engine Updates**: Maintain compatibility with script engine updates
2. **Framework Changes**: Adapt to OpenSim framework changes
3. **API Evolution**: Maintain backward compatibility while adding new features
4. **Performance Optimization**: Continuously optimize for better performance

### Integration Opportunities

1. **Web Integration**: Enhanced web service integration capabilities
2. **Database Enhancements**: Improved database integration patterns
3. **Monitoring Integration**: Integration with server monitoring systems
4. **Development Tools**: Enhanced development and debugging tools
