# YEngine Technical Documentation

## Overview

YEngine is an advanced LSL (Linden Scripting Language) script engine for OpenSimulator/Akisim that provides high-performance script execution with Just-In-Time (JIT) compilation, sophisticated memory management, and enhanced debugging capabilities. Based on the original XMREngine by Mike Rieker (DreamNation) and Melanie Thielker, YEngine has been extensively modified for cross-platform compatibility and improved performance. It serves as a modern replacement for traditional script engines, offering better performance, more robust error handling, and advanced features for complex scripting scenarios in virtual worlds.

## Architecture

YEngine implements multiple interfaces:
- `INonSharedRegionModule` - Per-region module instance management
- `IScriptEngine` - Core script engine interface for script lifecycle management
- `IScriptModule` - Script module interface for script operations and API access

### Key Components

1. **JIT Compilation System**
   - **Dynamic Compilation**: Real-time compilation of LSL scripts to .NET IL bytecode
   - **Performance Optimization**: Advanced optimization techniques for script execution
   - **Cross-platform Support**: Platform-agnostic compilation and execution
   - **Memory-mapped Execution**: Efficient script loading and execution patterns

2. **Script Instance Management**
   - **XMRInstance Framework**: Comprehensive script instance lifecycle management
   - **State Management**: Advanced script state persistence and restoration
   - **Event Queue System**: Sophisticated event queuing and processing
   - **Resource Tracking**: Memory and execution time tracking per script

3. **Advanced Memory Management**
   - **Heap Tracking**: Detailed memory allocation and deallocation tracking
   - **Stack Management**: Configurable stack size management for script execution
   - **Garbage Collection**: Integration with .NET garbage collection for optimal memory usage
   - **Resource Limits**: Configurable memory and execution limits per script

4. **Debugging and Development Tools**
   - **Source Code Preservation**: Optional source code saving for debugging
   - **IL Code Generation**: Optional IL code saving for performance analysis
   - **Execution Tracing**: Comprehensive execution tracing and logging
   - **Error Reporting**: Advanced error reporting with stack traces and context

5. **Enhanced LSL Implementation**
   - **Complete LSL API**: Full implementation of LSL functions and events
   - **OSSL Extensions**: Support for OpenSim-specific LSL extensions
   - **Type System**: Advanced type system with automatic casting and conversion
   - **Event System**: Sophisticated event handling and dispatch mechanisms

## Configuration

### Script Engine Activation

Set in `[YEngine]` or `[Startup]` section:
```ini
[YEngine]
ScriptEngine = YEngine
```

or

```ini
[Startup]
ScriptEngine = YEngine
```

### Core Configuration Options

Configure in `[YEngine]` section:

#### Performance Settings
```ini
[YEngine]
; Script engine selection
ScriptEngine = YEngine

; Maximum script stack size in bytes (default: 2048)
ScriptStackSize = 2048

; Maximum script heap size in bytes (default: 1048576, 1MB)
ScriptHeapSize = 1048576

; Enable verbose logging for debugging (default: false)
Verbose = false

; Enable script execution tracing (default: false)
TraceCalls = false
```

#### Debugging and Development
```ini
[YEngine]
; Enable script debugging features (default: false)
ScriptDebug = false

; Save script source code for debugging (default: false)
ScriptDebugSaveSource = false

; Save compiled IL code for analysis (default: false)
ScriptDebugSaveIL = false

; Late initialization for development scenarios (default: false)
LateInit = false
```

#### Maintenance and Monitoring
```ini
[YEngine]
; Maintenance interval in seconds (default: 10)
MaintenanceInterval = 10

; Enable script processing (default: true)
StartProcessing = true
```

## Features

### Advanced Script Compilation

YEngine provides sophisticated compilation capabilities:

1. **JIT Compilation**
   - **Dynamic IL Generation**: Real-time compilation to .NET Intermediate Language
   - **Optimization Pipeline**: Advanced optimization passes for performance
   - **Cross-platform Bytecode**: Platform-independent compiled script format
   - **Incremental Compilation**: Efficient recompilation of modified scripts

2. **Language Support**
   - **Complete LSL**: Full Linden Scripting Language implementation
   - **OSSL Extensions**: OpenSim-specific language extensions
   - **C# Interop**: Integration with .NET Framework capabilities
   - **Type Safety**: Strong typing with automatic type conversion

3. **Code Generation**
   - **Efficient Bytecode**: Optimized IL bytecode generation
   - **Memory Layout**: Optimal memory layout for script data
   - **Function Inlining**: Automatic inlining of frequently used functions
   - **Loop Optimization**: Specialized optimization for script loops

### Script Instance Management

1. **Lifecycle Management**
   - **Creation and Initialization**: Efficient script instantiation
   - **State Persistence**: Automatic script state saving and restoration
   - **Resource Cleanup**: Proper cleanup of script resources on termination
   - **Hot Reloading**: Runtime script replacement and updating

2. **Execution Control**
   - **Event-driven Execution**: Sophisticated event processing system
   - **Concurrency Control**: Safe concurrent execution of multiple scripts
   - **Resource Limiting**: Configurable limits on memory and execution time
   - **Priority Management**: Script execution priority and scheduling

3. **Error Handling**
   - **Exception Management**: Comprehensive exception handling and recovery
   - **Stack Trace Generation**: Detailed stack traces for debugging
   - **Error Reporting**: Clear error messages with context information
   - **Graceful Degradation**: Continued operation despite script errors

### Performance Optimization

1. **Memory Management**
   - **Heap Tracking**: Detailed memory allocation tracking
   - **Stack Optimization**: Efficient stack usage patterns
   - **Garbage Collection**: Integration with .NET GC for optimal performance
   - **Memory Pooling**: Object pooling for frequently allocated objects

2. **Execution Optimization**
   - **JIT Compilation**: Just-in-time compilation for optimal performance
   - **Caching**: Compiled script caching for faster startup
   - **Threading**: Efficient threading model for script execution
   - **Event Processing**: Optimized event queue processing

## Technical Implementation

### Compilation Pipeline Architecture

#### Script Processing Flow

1. **Source Analysis**: Parse and analyze LSL source code
2. **AST Generation**: Generate Abstract Syntax Tree from source
3. **Type Checking**: Perform type analysis and validation
4. **Optimization**: Apply optimization passes for performance
5. **IL Generation**: Generate .NET Intermediate Language bytecode
6. **Assembly Creation**: Create .NET assembly for script execution
7. **Instance Creation**: Instantiate script for execution

#### Memory Management System

```csharp
// Script instance memory tracking
public class XMRHeapTracker
{
    private int heapSize;
    private int maxHeapSize;
    private Dictionary<object, int> allocations;

    public void TrackAllocation(object obj, int size)
    {
        heapSize += size;
        allocations[obj] = size;
        if (heapSize > maxHeapSize)
            throw new OutOfMemoryException("Script heap limit exceeded");
    }
}
```

#### Event System Implementation

```csharp
// Script event processing
public void ProcessEvent(EventParams eventParams)
{
    lock (m_EventQueue)
    {
        if (m_EventQueue.Count >= MAX_EVENTS)
        {
            // Handle event queue overflow
            DropOldestEvent();
        }
        m_EventQueue.Enqueue(eventParams);
    }

    // Wake up script execution thread
    m_ExecutionSemaphore.Release();
}
```

### Script State Management

#### State Persistence System

YEngine implements comprehensive state persistence:

```csharp
// Script state serialization
public class ScriptState
{
    public Dictionary<string, object> GlobalVariables { get; set; }
    public string CurrentState { get; set; }
    public Queue<EventParams> PendingEvents { get; set; }
    public long ExecutionTime { get; set; }
    public int MemoryUsage { get; set; }
}

// State saving and loading
public void SaveScriptState(XMRInstance instance)
{
    var state = new ScriptState();
    SerializeGlobalVariables(instance, state);
    SerializeExecutionState(instance, state);
    WriteStateToStorage(instance.ItemID, state);
}
```

#### Script Communication System

```csharp
// Inter-script communication
public class ScriptCommunication
{
    public void SendMessage(UUID fromScript, UUID toScript, object message)
    {
        var targetInstance = GetScriptInstance(toScript);
        if (targetInstance != null)
        {
            var eventParams = new EventParams("dataserver",
                new object[] { fromScript.ToString(), message },
                new DetectParams[0]);
            targetInstance.QueueEvent(eventParams);
        }
    }
}
```

### Error Handling and Debugging

#### Exception Management

```csharp
// Script exception handling
public class ScriptException : Exception
{
    public string ScriptName { get; set; }
    public int LineNumber { get; set; }
    public string SourceCode { get; set; }
    public Dictionary<string, object> Variables { get; set; }

    public override string ToString()
    {
        return $"Script Error in {ScriptName} at line {LineNumber}: {Message}";
    }
}
```

#### Debug Information System

```csharp
// Debug information collection
public class DebugInfo
{
    public bool SaveSource { get; set; }
    public bool SaveIL { get; set; }
    public bool TraceExecution { get; set; }

    public void LogExecution(string function, object[] parameters)
    {
        if (TraceExecution)
        {
            m_log.DebugFormat("Script executing {0} with parameters: {1}",
                function, string.Join(", ", parameters));
        }
    }
}
```

## Performance Characteristics

### Compilation Performance

- **JIT Compilation**: Near-native execution speed after compilation
- **Compilation Caching**: Cached compiled scripts for faster startup
- **Incremental Building**: Only recompile changed portions when possible
- **Memory Efficiency**: Optimized memory layout for script data structures

### Runtime Performance

- **Event Processing**: High-performance event queue and processing system
- **Memory Management**: Efficient memory allocation and garbage collection
- **Threading Model**: Optimized threading for concurrent script execution
- **Resource Monitoring**: Real-time monitoring of script resource usage

### Scalability Features

- **Script Limits**: Configurable limits prevent resource exhaustion
- **Load Balancing**: Efficient distribution of script execution load
- **Memory Pooling**: Object pooling reduces allocation overhead
- **Concurrent Execution**: Safe concurrent execution of multiple scripts

## API Integration

### IScriptEngine Interface Implementation

#### Core Methods

- `GetScriptByName(string name)` - Retrieve script instance by name
- `GetScriptByID(UUID itemID)` - Retrieve script instance by item ID
- `SetScriptState(UUID itemID, bool running)` - Control script execution state
- `GetScriptErrors(UUID itemID)` - Retrieve script compilation and runtime errors

#### Script Lifecycle Methods

- `OnRemoveScript(uint localID, UUID itemID)` - Handle script removal
- `OnScriptReset(uint localID, UUID itemID)` - Handle script reset operations
- `OnGetScriptRunning(IClientAPI controllingClient, UUID objectID, UUID itemID)` - Query script running state

### IScriptModule Interface Implementation

#### Script Management

- `SaveAllState()` - Save all script states for persistence
- `StartProcessing()` - Begin script processing operations
- `StopAllScripts()` - Stop all running scripts safely
- `SuspendScript(UUID itemID)` - Suspend individual script execution
- `ResumeScript(UUID itemID)` - Resume suspended script execution

#### Configuration and Monitoring

- `GetScriptErrors(UUID itemID)` - Comprehensive error reporting
- `GetScriptStatReport()` - Performance and usage statistics
- `GetObjectScriptsExecutionTimes()` - Execution time analysis

## Usage Examples

### Basic Script Engine Configuration

```ini
[YEngine]
ScriptEngine = YEngine
ScriptStackSize = 4096
ScriptHeapSize = 2097152
```

### Development Configuration

```ini
[YEngine]
ScriptEngine = YEngine
ScriptDebug = true
ScriptDebugSaveSource = true
ScriptDebugSaveIL = true
Verbose = true
TraceCalls = true
```

### Production Configuration

```ini
[YEngine]
ScriptEngine = YEngine
ScriptStackSize = 2048
ScriptHeapSize = 1048576
MaintenanceInterval = 30
StartProcessing = true
```

### High-Performance Configuration

```ini
[YEngine]
ScriptEngine = YEngine
ScriptStackSize = 8192
ScriptHeapSize = 4194304
MaintenanceInterval = 5
Verbose = false
```

## Script Development Examples

### Basic LSL Script with YEngine Features

```lsl
// Enhanced LSL script leveraging YEngine capabilities
default
{
    state_entry()
    {
        // YEngine provides enhanced string handling
        string message = "Hello from YEngine!";
        llSay(0, message);

        // Efficient timer management
        llSetTimerEvent(1.0);
    }

    timer()
    {
        // YEngine optimizes repeated operations
        vector position = llGetPos();
        llSetText("Position: " + (string)position, <1,1,1>, 1.0);
    }

    touch_start(integer total_number)
    {
        // Enhanced error handling and performance
        integer i;
        for (i = 0; i < total_number; i++)
        {
            key toucher = llDetectedKey(i);
            llInstantMessage(toucher, "Thanks for touching me!");
        }
    }
}
```

### Advanced State Management

```lsl
// Demonstration of YEngine's advanced state management
default
{
    state_entry()
    {
        llSay(0, "Starting advanced state demo");
        llSetTimerEvent(5.0);
    }

    timer()
    {
        llSay(0, "Transitioning to working state");
        state working;
    }
}

state working
{
    state_entry()
    {
        llSay(0, "Now in working state - YEngine preserves all variables");
        // YEngine efficiently manages state transitions
        llSetTimerEvent(10.0);
    }

    timer()
    {
        llSay(0, "Returning to default state");
        state default;
    }

    touch_start(integer total_number)
    {
        llSay(0, "Working state touched - transitioning to interactive");
        state interactive;
    }
}

state interactive
{
    state_entry()
    {
        llSay(0, "Interactive mode activated");
    }

    touch_start(integer total_number)
    {
        llSay(0, "Interactive touch detected");
        state default;
    }
}
```

## Integration Points

### With Region Management

- **Scene Integration**: Deep integration with OpenSim scene management
- **Object Lifecycle**: Coordination with object creation and destruction
- **Asset System**: Integration with script asset loading and caching
- **Serialization**: Participation in region serialization and backup

### With Avatar System

- **Attachment Scripts**: Support for avatar attachment scripting
- **HUD Integration**: Heads-up display script support
- **Animation Control**: Script-driven avatar animation control
- **Gesture Support**: Integration with avatar gesture system

### With Physics System

- **Physical Objects**: Script control of physical object properties
- **Collision Detection**: Advanced collision event handling
- **Movement Control**: Precise control of object movement and rotation
- **Joint Management**: Support for physical joint constraints

### With Communication Systems

- **HTTP Requests**: Built-in HTTP request capabilities
- **Email Integration**: Script-based email sending functionality
- **Chat Integration**: Advanced chat and communication features
- **Inter-script Communication**: Efficient script-to-script messaging

## Security Features

### Script Isolation

- **Sandboxed Execution**: Scripts execute in isolated environments
- **Resource Limits**: Strict limits on memory and execution time
- **API Restrictions**: Controlled access to system APIs and functions
- **Permission System**: Integration with OpenSim permission system

### Code Validation

- **Syntax Checking**: Comprehensive syntax validation before execution
- **Type Safety**: Strong typing prevents many categories of errors
- **Resource Validation**: Validation of resource access requests
- **Security Scanning**: Analysis of script code for security issues

### Runtime Protection

- **Exception Isolation**: Script exceptions don't affect other scripts
- **Resource Monitoring**: Real-time monitoring of resource usage
- **Execution Limits**: Automatic termination of runaway scripts
- **Memory Protection**: Protection against memory-based attacks

## Debugging and Troubleshooting

### Common Issues

1. **Scripts Not Starting**: Check ScriptEngine configuration and module loading
2. **Memory Errors**: Verify heap and stack size settings
3. **Compilation Failures**: Enable debug logging to identify syntax errors
4. **Performance Issues**: Monitor script execution times and optimize code

### Diagnostic Tools

1. **Debug Logging**: Comprehensive logging for troubleshooting
2. **Source Code Saving**: Save source code for post-mortem analysis
3. **IL Code Analysis**: Examine generated IL code for optimization
4. **Performance Profiling**: Built-in performance monitoring and reporting

### Debug Configuration

Enable comprehensive debugging:

```ini
[Logging]
LogLevel = DEBUG

[YEngine]
ScriptEngine = YEngine
ScriptDebug = true
ScriptDebugSaveSource = true
ScriptDebugSaveIL = true
Verbose = true
TraceCalls = true
```

## Use Cases

### High-Performance Scripting

- **Complex Simulations**: CPU-intensive simulations and calculations
- **Real-time Systems**: Time-critical script operations
- **Large-scale Automation**: Automated systems managing many objects
- **Data Processing**: Scripts that process large amounts of data

### Educational Applications

- **Programming Education**: Teaching programming concepts through LSL
- **Interactive Demonstrations**: Dynamic educational content
- **Student Projects**: Platform for student scripting projects
- **Research Applications**: Academic research requiring script automation

### Commercial Applications

- **Virtual Businesses**: Script-driven virtual business operations
- **Entertainment Systems**: Complex entertainment and gaming systems
- **Automated Services**: Automated customer service and interaction
- **Content Management**: Dynamic content creation and management

### Creative Projects

- **Interactive Art**: Script-driven interactive art installations
- **Dynamic Architecture**: Buildings that respond to user interaction
- **Procedural Content**: Automatically generated content and environments
- **Experimental Interfaces**: Novel user interaction methods

## Migration and Deployment

### From Mono.Addins

YEngine has been migrated from Mono.Addins to the CoreModuleFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via ScriptEngine configuration
- Logging provides visibility into module loading decisions

### From Other Script Engines

When migrating from other script engines:

- Update configuration to specify `ScriptEngine = YEngine`
- Test script compatibility and performance
- Adjust memory and performance settings as needed
- Validate debugging and development workflows

### Configuration Migration

When upgrading YEngine installations:

- Verify YEngine configuration section exists and is properly configured
- Test script compilation and execution after deployment
- Update development tools and debugging configurations
- Validate performance characteristics meet requirements

### Deployment Considerations

- **Performance Requirements**: Plan for increased performance capabilities
- **Memory Usage**: YEngine may use more memory for better performance
- **Debugging Tools**: Configure debugging features for development environments
- **Compatibility Testing**: Test existing scripts for compatibility

## Configuration Examples

### Basic YEngine Setup

```ini
[YEngine]
ScriptEngine = YEngine
```

### Development Environment

```ini
[YEngine]
ScriptEngine = YEngine
ScriptDebug = true
ScriptDebugSaveSource = true
ScriptDebugSaveIL = true
Verbose = true
TraceCalls = true
ScriptStackSize = 4096
ScriptHeapSize = 2097152

[Logging]
LogLevel = DEBUG
```

### Production Environment

```ini
[YEngine]
ScriptEngine = YEngine
ScriptStackSize = 2048
ScriptHeapSize = 1048576
MaintenanceInterval = 30
StartProcessing = true

[Logging]
LogLevel = INFO
```

### High-Performance Setup

```ini
[YEngine]
ScriptEngine = YEngine
ScriptStackSize = 8192
ScriptHeapSize = 4194304
MaintenanceInterval = 5
Verbose = false
TraceCalls = false
```

## Best Practices

### Performance Guidelines

1. **Memory Management**: Monitor script memory usage and optimize as needed
2. **Compilation Caching**: Leverage compiled script caching for performance
3. **Resource Limits**: Set appropriate resource limits for your environment
4. **Code Optimization**: Write efficient LSL code that leverages YEngine features

### Development Practices

1. **Testing**: Thoroughly test scripts in development environment
2. **Debugging**: Use YEngine's advanced debugging features during development
3. **Error Handling**: Implement robust error handling in scripts
4. **Documentation**: Document complex scripts and their interactions

### Operational Practices

1. **Monitoring**: Monitor script performance and resource usage
2. **Maintenance**: Regular maintenance of script engines and configurations
3. **Backup**: Regular backup of script sources and states
4. **Updates**: Keep YEngine updated for performance and security improvements

## Future Enhancements

### Potential Improvements

1. **Enhanced Optimization**: Additional optimization passes for better performance
2. **Debugging Tools**: More sophisticated debugging and profiling tools
3. **Language Extensions**: Additional language features and capabilities
4. **IDE Integration**: Better integration with development environments

### Compatibility Considerations

1. **LSL Evolution**: Stay current with LSL specification updates
2. **Platform Updates**: Maintain compatibility with .NET platform updates
3. **OpenSim Integration**: Ensure compatibility with OpenSim framework evolution
4. **Performance Standards**: Optimize for evolving hardware capabilities
