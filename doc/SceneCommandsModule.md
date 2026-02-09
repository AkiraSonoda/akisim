# SceneCommandsModule Technical Documentation

## Overview

The **SceneCommandsModule** is an optional OpenSimulator module that provides console commands for managing and debugging scene-level options and behaviors. It offers administrators powerful runtime control over scene subsystems including physics, scripting, animations, backups, and performance monitoring. The module is essential for debugging, performance tuning, and operational troubleshooting.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **ISceneCommandsModule**: Scene debugging functionality interface for external access
- **Console Command Interface**: Debug command registration and handling

### Key Components
- **Scene Debug Options**: Runtime control over scene subsystem behaviors
- **Performance Monitoring**: Debug output for frame timing and performance issues
- **Subsystem Control**: Enable/disable physics, scripting, collisions, and other features
- **Operational Management**: Control scene updates, backups, and maintenance operations

## Configuration

### Module Enablement
```ini
[Modules]
; Enable SceneCommandsModule for scene debugging commands
SceneCommandsModule = true
```

### Default Behavior
- **Disabled by Default**: Module must be explicitly enabled
- **Debug Focus**: Designed primarily for debugging and development
- **No Configuration File**: Runtime configuration through console commands only
- **Per-Region**: Each region has independent scene debug settings

## Console Commands

### debug scene get
```bash
debug scene get
```
- **Purpose**: Display current scene debug options and their states
- **Parameters**: None
- **Output**: Formatted list of all scene debug flags with current values
- **Usage**: Check current scene debug configuration

**Example Output:**
```
Scene MyRegion options:
active      : True
animations  : False
pbackup     : True
physics     : True
scripting   : True
teleport    : False
updates     : False
```

### debug scene set
```bash
debug scene set <parameter> <value>
```
- **Purpose**: Modify scene debug options at runtime
- **Parameters**: Parameter name and boolean value (true/false)
- **Validation**: Parameter validation and boolean parsing
- **Persistence**: Changes affect current session only (not persisted)

**Supported Parameters:**
- **active**: Main scene update and maintenance loops
- **animations**: Extra animation debug information logging
- **pbackup**: Periodic scene backup operations
- **physics**: Physics simulation for all objects
- **scripting**: Script execution and operations
- **teleport**: Extra teleport debug information logging
- **updates**: Frame timing debug information for slow frames

## Scene Debug Options

### Scene Activity Control

#### active
```csharp
m_scene.Active = active;
```
- **Purpose**: Control main scene update and maintenance loops
- **Default**: true
- **Effect**: When false, suspends core scene processing
- **Use Cases**: Emergency scene freeze, debugging race conditions

#### pbackup
```csharp
m_scene.PeriodicBackup = active;
```
- **Purpose**: Control periodic scene backup operations
- **Default**: true
- **Effect**: Enables/disables automatic scene state backups
- **Use Cases**: Disable during testing, reduce I/O during debugging

### Subsystem Control

#### physics
```csharp
m_scene.PhysicsEnabled = enablePhysics;
```
- **Purpose**: Enable/disable physics simulation
- **Default**: true
- **Effect**: Makes all physics objects non-physical when disabled
- **Use Cases**: Performance testing, physics debugging, object placement

#### scripting
```csharp
m_scene.ScriptsEnabled = enableScripts;
```
- **Purpose**: Enable/disable script execution
- **Default**: true
- **Effect**: Stops all script operations when disabled
- **Use Cases**: Performance isolation, script debugging, security testing

### Debug Logging Control

#### animations
```csharp
m_scene.DebugAnimations = active;
```
- **Purpose**: Enable extra animation debug logging
- **Default**: false
- **Effect**: Increases animation-related log output
- **Use Cases**: Animation debugging, avatar behavior analysis

#### teleport
```csharp
m_scene.DebugTeleporting = enableTeleportDebugging;
```
- **Purpose**: Enable extra teleport debug logging
- **Default**: false
- **Effect**: Detailed teleport operation logging
- **Use Cases**: Teleport failure debugging, cross-region movement analysis

#### updates
```csharp
m_scene.DebugUpdates = enableUpdateDebugging;
```
- **Purpose**: Enable frame timing debug logging
- **Default**: false
- **Effect**: Logs frames exceeding double the maximum desired frame time
- **Use Cases**: Performance analysis, frame rate debugging, lag detection

## Implementation Details

### Command Processing
```csharp
private void HandleDebugSceneSetCommand(string module, string[] args)
{
    if (args.Length == 5)
    {
        if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene != null)
            return;

        string key = args[3];
        string value = args[4];
        SetSceneDebugOptions(new Dictionary<string, string>() { { key, value } });

        MainConsole.Instance.Output("Set {0} debug scene {1} = {2}", m_scene.Name, key, value);
    }
    else
    {
        MainConsole.Instance.Output("Usage: debug scene set <param> <value>");
    }
}
```

### Option Configuration
```csharp
public void SetSceneDebugOptions(Dictionary<string, string> options)
{
    if (options.ContainsKey("active"))
    {
        bool active;
        if (bool.TryParse(options["active"], out active))
            m_scene.Active = active;
    }

    if (options.ContainsKey("physics"))
    {
        bool enablePhysics;
        if (bool.TryParse(options["physics"], out enablePhysics))
            m_scene.PhysicsEnabled = enablePhysics;
    }
    // ... additional option handling
}
```

### Scene Context Validation
```csharp
if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene != null)
    return;
```
- **Context Checking**: Commands operate only on selected scene
- **Multi-Region Safety**: Prevents cross-region command interference
- **Console Integration**: Respects console scene selection

## Administrative Use Cases

### Performance Debugging
- **Frame Rate Analysis**: Enable updates debugging to identify slow frames
- **Subsystem Isolation**: Disable physics or scripting to isolate performance issues
- **Resource Monitoring**: Monitor scene activity and backup operations
- **Load Testing**: Disable expensive operations during stress testing

### Development and Testing
- **Physics Testing**: Disable physics for object placement and testing
- **Script Development**: Toggle scripting for script debugging
- **Animation Analysis**: Enable animation debugging for avatar work
- **Teleport Debugging**: Detailed teleport logging for cross-region issues

### Operational Management
- **Emergency Response**: Freeze scene activity during emergencies
- **Maintenance Operations**: Disable backups during maintenance
- **Resource Conservation**: Temporarily disable expensive operations
- **Diagnostic Data**: Collect detailed debug information for analysis

### Troubleshooting Support
- **User Support**: Isolate issues through selective subsystem disabling
- **Bug Investigation**: Enable debug logging for specific issues
- **Performance Analysis**: Identify bottlenecks through systematic testing
- **System Health**: Monitor scene subsystem health and operation

## Security and Safety Features

### Access Control
```csharp
if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene != null)
    return;
```

### Administrative Privileges
- **Console Access**: Commands require administrative console access
- **Scene Context**: Commands operate only on selected scene
- **Parameter Validation**: Input validation prevents malformed commands
- **Safe Defaults**: Invalid parameters gracefully ignored

### Runtime Safety
- **Non-Persistent**: Debug settings don't persist across restarts
- **Reversible**: All debug options can be easily toggled back
- **Validation**: Boolean parameter validation prevents invalid values
- **Graceful Degradation**: Invalid parameters don't crash the scene

## Performance Considerations

### Debug Impact

#### Low Impact Options
- **active**: Minimal overhead when toggling
- **pbackup**: Affects only backup operations
- **physics**: Clean physics enable/disable

#### Logging Options
- **animations**: Increased log I/O when enabled
- **teleport**: Additional logging during teleport operations
- **updates**: Performance monitoring overhead

#### High Impact Options
- **scripting**: Immediate script suspension/resumption
- **physics**: Physics subsystem state changes

### Operational Efficiency
- **Runtime Changes**: No scene restart required for changes
- **Immediate Effect**: Most changes take effect immediately
- **Memory Efficient**: Minimal memory overhead for debug tracking
- **Fast Toggle**: Quick enable/disable for testing scenarios

## Error Handling and Validation

### Input Validation
```csharp
if (args.Length == 5)
{
    // Process valid command
}
else
{
    MainConsole.Instance.Output("Usage: debug scene set <param> <value>");
}
```

### Parameter Validation
```csharp
bool active;
if (bool.TryParse(options["active"], out active))
    m_scene.Active = active;
```

### Safe Operations
- **Parameter Counting**: Verify correct number of command arguments
- **Boolean Parsing**: Safe parsing with fallback for invalid values
- **Scene Availability**: Check scene state before operations
- **Context Validation**: Ensure commands run in correct scene context

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource source)
{
    // No specific configuration required
}
```
- **No Configuration**: Module requires no external configuration
- **Optional Loading**: Module loads only when explicitly enabled

### Region Integration
```csharp
public void AddRegion(Scene scene)
{
    m_scene = scene;
    m_scene.RegisterModuleInterface<ISceneCommandsModule>(this);
}

public void RegionLoaded(Scene scene)
{
    scene.AddCommand("Debug", this, "debug scene get", ...);
    scene.AddCommand("Debug", this, "debug scene set", ...);
}
```

### Command Registration
- **Debug Category**: Commands registered under "Debug" category
- **Module Association**: Commands associated with module instance
- **Help Integration**: Commands include comprehensive help text
- **Scene-Specific**: Commands registered per scene

### Interface Registration
```csharp
m_scene.RegisterModuleInterface<ISceneCommandsModule>(this);
```
- **Module Interface**: Provides ISceneCommandsModule for external access
- **API Access**: Other modules can programmatically set debug options
- **Integration Support**: Enables integration with other debugging tools

## Integration Examples

### Basic Debug Operations
```bash
# Check current scene debug status
debug scene get

# Disable physics for testing
debug scene set physics false

# Enable animation debugging
debug scene set animations true

# Monitor frame performance
debug scene set updates true
```

### Performance Troubleshooting
```bash
# Baseline performance check
debug scene get

# Isolate physics performance
debug scene set physics false
# Monitor performance, then re-enable
debug scene set physics true

# Isolate script performance
debug scene set scripting false
# Monitor performance, then re-enable
debug scene set scripting true
```

### Development Workflow
```bash
# Set up development environment
debug scene set animations true
debug scene set teleport true
debug scene set updates true

# Reset to production settings
debug scene set animations false
debug scene set teleport false
debug scene set updates false
```

### Programmatic Access
```csharp
// Access scene commands interface
ISceneCommandsModule sceneCommands = scene.RequestModuleInterface<ISceneCommandsModule>();

if (sceneCommands != null)
{
    // Set debug options programmatically
    var options = new Dictionary<string, string>
    {
        {"physics", "false"},
        {"updates", "true"}
    };

    sceneCommands.SetSceneDebugOptions(options);
}
```

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Configuration-based Loading**: Controlled via SceneCommandsModule setting in [Modules]
- **Default Behavior**: Disabled by default, requires explicit configuration
- **Logging Integration**: Comprehensive debug and info logging for operations

### Namespace Correction
- **Fixed Namespace**: Corrected from Avatar.Attachments to World.SceneCommands
- **File Location**: Properly located in World/SceneCommands directory
- **Interface Consistency**: Maintains ISceneCommandsModule interface

### Dependencies
- **Console System**: Integration with scene-specific command registration
- **Scene Management**: Direct integration with scene properties and state
- **Interface Registration**: ISceneCommandsModule interface for external access
- **No External Dependencies**: Self-contained debugging functionality

## Troubleshooting

### Common Issues

#### Module Not Loading
- **Check Configuration**: Ensure SceneCommandsModule = true in [Modules]
- **Log Messages**: Check for loading debug messages in server logs
- **Dependencies**: Verify no missing dependencies or conflicts
- **Module Loading**: Confirm module appears in loaded modules list

#### Commands Not Available
- **Scene Context**: Ensure commands are run with correct scene selected
- **Module Registration**: Verify module registered command properly
- **Console Access**: Ensure administrative console access
- **Command Category**: Look for commands under "Debug" category

#### Debug Options Not Working
- **Parameter Names**: Verify correct parameter names (case-sensitive)
- **Boolean Values**: Use "true" or "false" (case-insensitive)
- **Scene State**: Ensure scene is running and accessible
- **Immediate Effect**: Most changes take effect immediately

#### Performance Impact
- **Logging Overhead**: Debug logging options can impact performance
- **Subsystem Toggling**: Physics/scripting changes may cause temporary lag
- **Resource Usage**: Monitor resource usage when enabling debug options
- **Production Use**: Avoid extensive debug logging in production

## Usage Examples

### Daily Operations
```bash
# Morning debug status check
debug scene get

# Enable performance monitoring
debug scene set updates true

# Check for slow frames in logs
tail -f opensim.log | grep "frame"
```

### Performance Analysis
```bash
# Baseline measurement
debug scene get
debug scene set updates true

# Test physics impact
debug scene set physics false
# Monitor performance for 5 minutes
debug scene set physics true

# Test script impact
debug scene set scripting false
# Monitor performance for 5 minutes
debug scene set scripting true

# Restore normal operation
debug scene set updates false
```

### Development Session
```bash
# Set up debugging environment
debug scene set animations true
debug scene set teleport true
debug scene set updates true

# Work on features with enhanced logging
# ... development work ...

# Clean up debugging
debug scene set animations false
debug scene set teleport false
debug scene set updates false
```

### Emergency Response
```bash
# Emergency scene freeze
debug scene set active false
debug scene set physics false
debug scene set scripting false

# Investigate issues while scene is stable
# ... investigation ...

# Gradual restoration
debug scene set active true
debug scene set physics true
debug scene set scripting true
```

This documentation reflects the SceneCommandsModule implementation in `src/OpenSim.Region.OptionalModules/World/SceneCommands/SceneCommandsModule.cs` and its integration with the factory-based module loading system.