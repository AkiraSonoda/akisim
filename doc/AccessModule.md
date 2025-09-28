# AccessModule Technical Documentation

## Overview

The **AccessModule** is a core OpenSimulator module that provides login control and region access management functionality. It implements console commands for administrators to enable or disable user logins to regions, providing essential access control capabilities for virtual world management and maintenance operations.

## Architecture and Interfaces

### Core Interfaces
- **ISharedRegionModule**: Shared across regions module lifecycle
- **Login Control Interface**: Console command interface for access management

### Key Components
- **Console Command Registration**: Administrative commands for login control
- **Region State Management**: Per-region login state tracking
- **Multi-Region Support**: Commands can operate on all regions or specific regions
- **Scene Integration**: Direct integration with region scene login controls

## Login Control System

### Login State Management
The module manages login access on a per-region basis:
- **Region-specific Control**: Each region can have logins enabled or disabled independently
- **Persistent State**: Login state maintained in scene configuration
- **Administrative Override**: Console commands override default login behavior
- **Status Monitoring**: Real-time status checking and reporting

### Access Control Modes
- **Enable Logins**: Allow new user connections to the region
- **Disable Logins**: Prevent new user logins while maintaining existing connections
- **Status Display**: Show current login state for regions

## Console Command Interface

### Available Commands

#### login enable
```bash
login enable
```
- **Purpose**: Enable user logins for the current region or all regions
- **Scope**: Operates on current console scene or all scenes if no specific scene selected
- **Effect**: Sets `scene.LoginsEnabled = true` for target regions
- **Feedback**: Displays confirmation message with region name

#### login disable
```bash
login disable
```
- **Purpose**: Disable user logins for the current region or all regions
- **Scope**: Operates on current console scene or all scenes if no specific scene selected
- **Effect**: Sets `scene.LoginsEnabled = false` for target regions
- **Feedback**: Displays confirmation message with region name
- **Note**: Existing users remain connected; only new logins are prevented

#### login status
```bash
login status
```
- **Purpose**: Display current login status for regions
- **Scope**: Shows status for current console scene or all scenes
- **Output**: Reports whether logins are enabled or disabled per region
- **Format**: "Login in [RegionName] are enabled/disabled"

### Command Processing

#### Multi-Region Operation
```csharp
public void HandleLoginCommand(string module, string[] cmd)
{
    if ((Scene)MainConsole.Instance.ConsoleScene == null)
    {
        // Operate on all regions
        foreach (Scene s in m_SceneList)
        {
            if (!ProcessCommand(s, cmd))
                break;
        }
    }
    else
    {
        // Operate on current console scene only
        ProcessCommand((Scene)MainConsole.Instance.ConsoleScene, cmd);
    }
}
```

#### Command Validation
```csharp
bool ProcessCommand(Scene scene, string[] cmd)
{
    if (cmd.Length < 2)
    {
        MainConsole.Instance.Output("Syntax: login enable|disable|status");
        return false;
    }
    // Process valid commands...
}
```

## Region Integration

### Scene List Management
The module maintains a list of all regions for command processing:

```csharp
private List<Scene> m_SceneList = new List<Scene>();

public void AddRegion(Scene scene)
{
    lock (m_SceneList)
    {
        if (!m_SceneList.Contains(scene))
            m_SceneList.Add(scene);
    }
}
```

### Login State Control
Direct manipulation of scene login state:

```csharp
switch (cmd[1])
{
case "enable":
    scene.LoginsEnabled = true;
    break;
case "disable":
    scene.LoginsEnabled = false;
    break;
}
```

### Thread Safety
- **Synchronized Access**: Scene list operations are thread-safe with locking
- **Atomic Updates**: Login state changes are atomic per region
- **Concurrent Safe**: Multiple command operations can execute safely

## Administrative Use Cases

### Maintenance Operations
- **Server Maintenance**: Disable logins before performing server updates
- **Region Restart**: Control access during region restart operations
- **Emergency Access**: Quickly disable access during emergencies
- **Gradual Shutdown**: Prevent new connections while allowing graceful disconnection

### Region Management
- **New Region Setup**: Enable logins after region configuration is complete
- **Testing Environments**: Control access to development or testing regions
- **Event Management**: Manage access for special events or private gatherings
- **Load Management**: Temporarily disable logins during high-load periods

### Monitoring and Status
- **Health Checks**: Verify login status as part of system health monitoring
- **Audit Operations**: Log and track login state changes for compliance
- **Troubleshooting**: Check login status when investigating connection issues
- **Status Reports**: Generate region access status reports

## Console Integration

### Command Registration
```csharp
public void Initialise(IConfigSource config)
{
    MainConsole.Instance.Commands.AddCommand("Users", true,
            "login enable", "login enable", "Enable simulator logins", String.Empty,
            HandleLoginCommand);

    MainConsole.Instance.Commands.AddCommand("Users", true,
            "login disable", "login disable", "Disable simulator logins", String.Empty,
            HandleLoginCommand);

    MainConsole.Instance.Commands.AddCommand("Users", true,
            "login status", "login status", "Show login status", String.Empty,
            HandleLoginCommand);
}
```

### Command Categories
- **Category**: "Users" - Commands are grouped under user management
- **Access Level**: `true` - Commands require administrative privileges
- **Help Integration**: Commands appear in console help system
- **Unified Handler**: All commands use the same handler for consistency

### Output Formatting
```csharp
MainConsole.Instance.Output(String.Format("Logins are enabled for region {0}",
                                         scene.RegionInfo.RegionName));
MainConsole.Instance.Output(String.Format("Login in {0} are disabled",
                                         scene.RegionInfo.RegionName));
```

## Security Considerations

### Administrative Access
- **Console Privileges**: Commands require console access (administrative level)
- **Command Validation**: Input validation prevents malformed commands
- **Safe Defaults**: Invalid commands display help rather than causing errors
- **Controlled Scope**: Commands operate only on managed regions

### Access Control Security
- **Non-Destructive**: Disabling logins doesn't disconnect existing users
- **Reversible Operations**: All login control operations can be undone
- **State Persistence**: Login state maintained across module lifecycle
- **Audit Trail**: Command execution logged through console system

### System Integrity
- **Graceful Degradation**: Module failure doesn't affect core functionality
- **Thread Safety**: Concurrent command execution handled safely
- **Resource Protection**: Minimal resource usage and memory footprint
- **Clean Shutdown**: Proper cleanup of resources and state

## Performance Considerations

### Lightweight Operations
- **Minimal Overhead**: Simple state management with low resource usage
- **Fast Execution**: Commands execute immediately with direct scene access
- **Efficient Storage**: Uses existing scene properties without additional storage
- **Low Memory**: Small memory footprint with simple data structures

### Scalability
- **Multi-Region Support**: Efficiently handles multiple regions
- **Concurrent Access**: Thread-safe operations support concurrent administration
- **Command Batching**: Single commands can operate across all regions
- **Resource Efficiency**: No background threads or persistent connections

### Optimization Features
- **Direct Access**: Direct scene property manipulation without layers
- **Synchronized Lists**: Efficient list management with minimal locking
- **State Caching**: Login state stored directly in scene objects
- **Immediate Response**: Commands provide immediate feedback

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource config)
```
- **Command Registration**: Register all console commands with main console
- **No Configuration**: Module requires no external configuration
- **Always Active**: Module initializes automatically without dependencies

### Region Integration
```csharp
public void AddRegion(Scene scene)
public void RemoveRegion(Scene scene)
```
- **Scene Tracking**: Add/remove scenes from management list
- **Thread Safety**: Synchronized scene list operations
- **Dynamic Registration**: Handle region additions and removals during runtime

### Cleanup
```csharp
public void Close()
```
- **Resource Cleanup**: Clean up any held resources
- **Command Deregistration**: Console commands cleaned up automatically
- **State Preservation**: Login states maintained in scenes beyond module lifecycle

## Error Handling and Validation

### Command Validation
```csharp
if (cmd.Length < 2)
{
    MainConsole.Instance.Output("Syntax: login enable|disable|status");
    return false;
}
```

### Invalid Command Handling
```csharp
default:
    MainConsole.Instance.Output("Syntax: login enable|disable|status");
    return false;
```

### Safe Execution
- **Parameter Checking**: Validate command parameters before execution
- **Null Safety**: Handle null scenes and configurations gracefully
- **Exception Handling**: Graceful failure with informative error messages
- **Help Display**: Clear syntax help for invalid commands

## Integration Examples

### Basic Usage
```bash
# Enable logins for all regions
login enable

# Disable logins for current region
region change "MyRegion"
login disable

# Check login status
login status
```

### Maintenance Workflow
```bash
# Prepare for maintenance
login disable
# Perform maintenance operations
# Re-enable access
login enable
login status  # Verify access restored
```

### Programmatic Integration
```csharp
// Other modules can access scene login state directly
if (scene.LoginsEnabled)
{
    // Process new user connection
}
else
{
    // Reject new connections or queue them
}
```

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Always Enabled**: Module loaded by default as core functionality
- **No Configuration**: Module requires no configuration settings
- **Logging Integration**: Comprehensive debug and info logging for operations

### Backward Compatibility
- **Command Compatibility**: All existing console commands remain unchanged
- **State Compatibility**: Login state management behavior unchanged
- **Integration Compatibility**: Scene integration remains identical
- **API Compatibility**: No breaking changes to existing functionality

### Dependencies
- **Console System**: Requires MainConsole.Instance for command registration
- **Scene Management**: Integration with scene and region lifecycle
- **No External Dependencies**: Self-contained with minimal dependencies

## Troubleshooting

### Common Issues

#### Commands Not Available
- **Module Loading**: Verify AccessModule is loaded in factory
- **Console Access**: Ensure administrative console access
- **Command Registration**: Check for command registration errors in logs
- **Module State**: Verify module initialization completed successfully

#### Login Control Not Working
- **Scene State**: Check scene.LoginsEnabled property directly
- **Command Execution**: Verify commands execute without errors
- **Region Selection**: Ensure correct region is selected for single-region operations
- **State Persistence**: Verify login state persists across operations

#### Status Display Issues
- **Region List**: Check m_SceneList contains expected regions
- **Console Output**: Verify console output formatting is correct
- **Scene Names**: Ensure region names display correctly
- **Multi-Region**: Test both single-region and multi-region operations

## Usage Examples

### Administrative Commands
```bash
# Global login control
login disable    # Disable logins on all regions
login status     # Check status of all regions
login enable     # Re-enable logins on all regions

# Region-specific control
region change "Welcome Area"
login disable    # Disable only for Welcome Area
login status     # Check Welcome Area status
```

### Maintenance Scenarios
```bash
# Scheduled maintenance
echo "Preparing for maintenance..."
login disable
echo "Maintenance in progress - no new logins allowed"
# Perform maintenance tasks
login enable
echo "Maintenance complete - logins restored"
```

### Integration with Scripts
```bash
#!/bin/bash
# Maintenance script example
echo "login disable" | nc localhost 9000  # Assuming remote console
# Perform automated maintenance
echo "login enable" | nc localhost 9000
echo "login status" | nc localhost 9000
```

This documentation reflects the AccessModule implementation in `src/OpenSim.Region.CoreModules/World/Access/AccessModule.cs` and its integration with the factory-based module loading system.