# RestartModule

## Overview

The RestartModule is a non-shared core module that provides comprehensive region restart management functionality in OpenSimulator. It enables administrators to schedule graceful region restarts with customizable countdown timers, user notifications, and coordination mechanisms. The module supports both immediate and scheduled restarts with multiple notification intervals, ensuring users receive adequate warning before region shutdowns.

## Architecture

- **Type**: `INonSharedRegionModule` - instantiated once per region for localized restart management
- **Interface**: Implements `IRestartModule` for programmatic restart control
- **Namespace**: `OpenSim.Region.CoreModules.World.Region`
- **Location**: `src/OpenSim.Region.CoreModules/World/Region/RestartModule.cs`

## Key Features

### Scheduled Restart Management
- **Flexible Countdown Timers** - Support for multiple alert intervals with customizable timing
- **User Notification System** - Configurable bluebox and notice-based user alerts
- **Graceful Restart Coordination** - Ensures proper shutdown sequence and state preservation
- **Restart Abort Functionality** - Ability to cancel scheduled restarts with user notification

### Region State Management
- **Marker File Support** - Optional file-based restart state tracking
- **Empty Region Optimization** - Configurable immediate restart when no users present
- **Cross-Region Coordination** - Support for grid-wide restart coordination

### Console Command Interface
- **Interactive Restart Scheduling** - Console commands for administrative restart management
- **Real-time Status Monitoring** - Current restart state and countdown information
- **Flexible Messaging** - Customizable restart messages and notifications

## Console Commands Reference

### Restart Commands

#### `region restart bluebox <message> <delta seconds>+`
**Purpose**: Schedule a region restart with bluebox notifications to users.

**Usage**:
```bash
# Schedule restart with single 300-second warning
region restart bluebox "Server maintenance in {0}" 300

# Schedule restart with multiple warnings
region restart bluebox "Scheduled maintenance in {0}" 600 300 60

# Schedule restart with custom message
region restart bluebox "Emergency restart required in {0}" 120
```

**Behavior**:
- **Message Format**: Use `{0}` placeholder for time remaining (e.g., "5 minutes", "30 seconds")
- **Multiple Intervals**: Sends notifications at each specified time interval
- **Bluebox Display**: Shows dismissable blue dialog box to all users in region
- **Countdown Sequencing**: Alerts sent in descending time order

#### `region restart notice <message> <delta seconds>+`
**Purpose**: Schedule a region restart with transient notice notifications to users.

**Usage**:
```bash
# Schedule restart with notice-style alerts
region restart notice "System restart in {0}" 300

# Multiple warnings with notices
region restart notice "Maintenance restart in {0}" 900 600 300 60
```

**Behavior**:
- **Notice Display**: Shows temporary notification messages instead of bluebox dialogs
- **Less Intrusive**: Notifications don't require user dismissal
- **Same Timing Logic**: Supports multiple countdown intervals like bluebox mode

#### `region restart abort [<message>]`
**Purpose**: Cancel a scheduled region restart and optionally notify users.

**Usage**:
```bash
# Abort restart silently
region restart abort

# Abort restart with user notification
region restart abort "Maintenance has been postponed"
```

**Behavior**:
- **Timer Cancellation**: Immediately stops countdown timer and cancels restart
- **User Notification**: Optional message sent to all users if provided
- **Marker Cleanup**: Removes restart marker files if configured
- **State Reset**: Clears all restart-related state variables

## Configuration

### RestartModule Section
```ini
[RestartModule]
; Path for restart marker files (optional)
MarkerPath = /var/opensim/markers
```

### Startup Section Integration
```ini
[Startup]
; Skip restart delays when region is empty (default: false)
SkipDelayOnEmptyRegion = true

; Restart command shuts down entire grid instead of just region (default: false)
InworldRestartShutsDown = false
```

### Module Loading
The RestartModule is always loaded by the CoreModuleFactory as a core essential module. No explicit configuration is required for basic functionality.

## Technical Implementation

### Core Components

#### Timer Management
```csharp
protected Timer m_CountdownTimer = null;
protected DateTime m_RestartBegin;
protected List<int> m_Alerts;
```
- **Precision Timing**: Uses `System.Timers.Timer` for accurate countdown intervals
- **State Tracking**: Maintains restart start time and remaining alert intervals
- **Alert Sequencing**: Manages ordered list of notification timestamps

#### User Notification System
```csharp
protected IDialogModule m_DialogModule = null;
protected bool m_Notice = false;
protected string m_Message;
protected UUID m_Initiator;
```
- **Dialog Integration**: Leverages `IDialogModule` for user communication
- **Message Templating**: Supports time placeholder substitution in messages
- **Notification Modes**: Bluebox (dismissable) vs. Notice (transient) display options

#### State Persistence
```csharp
protected string m_MarkerPath = String.Empty;
```
- **Marker Files**: Optional file-based restart state persistence
- **Process Tracking**: Records process ID for restart coordination
- **Cleanup Management**: Automatic marker file removal on completion/abort

### Module Lifecycle

#### Initialization
```csharp
public void Initialise(IConfigSource config)
```
- **Configuration Loading**: Reads RestartModule and Startup configuration sections
- **Path Validation**: Sets up marker file directory if configured
- **Feature Flags**: Configures empty region optimization and grid shutdown behavior

#### Region Integration
```csharp
public void AddRegion(Scene scene)
public void RegionLoaded(Scene scene)
```
- **Interface Registration**: Registers `IRestartModule` with the scene
- **Command Registration**: Adds console commands to the region's command system
- **Dialog Module Binding**: Establishes connection to user notification system
- **Marker Cleanup**: Removes any existing restart markers from previous sessions

#### Restart Scheduling
```csharp
public void ScheduleRestart(UUID initiator, string message, int[] alerts, bool notice)
```
- **Timer Management**: Cancels existing timers and establishes new countdown
- **Alert Processing**: Sorts and validates countdown intervals
- **Immediate Execution**: Handles zero-delay restarts directly
- **State Initialization**: Sets up all restart-related variables

### Countdown Processing

#### Notification Logic
```csharp
public int DoOneNotice(bool sendOut)
```
- **Time Calculation**: Converts seconds to human-readable time strings
- **Message Formatting**: Substitutes time placeholders in user messages
- **Notification Dispatch**: Sends alerts via dialog module based on mode
- **Interval Management**: Calculates next countdown interval

#### Alert Timing
The module supports flexible alert timing:
- **Multiple Intervals**: Any number of countdown alerts (e.g., 600, 300, 60, 10 seconds)
- **Time Formatting**: Automatic conversion to minutes/seconds (e.g., "5 minutes and 30 seconds")
- **Deduplication**: Removes duplicate alert times automatically
- **Ordering**: Automatically sorts alerts in descending order

### Advanced Features

#### Empty Region Optimization
```csharp
protected bool m_shortCircuitDelays = false;

if (m_shortCircuitDelays && CountAgents() == 0)
{
    m_Scene.RestartNow();
    return;
}
```
- **User Detection**: Counts non-child, non-NPC agents in region
- **Immediate Restart**: Bypasses countdown when no users present
- **Grid Awareness**: Can count across all regions if configured

#### Grid-Wide Coordination
```csharp
protected bool m_rebootAll = false;

if (m_rebootAll)
{
    foreach (Scene s in SceneManager.Instance.Scenes)
        // Count agents across all scenes
}
```
- **Multi-Region Support**: Coordinates restarts across entire grid
- **Agent Counting**: Considers users across all regions for empty checks
- **Unified Shutdown**: Single command can trigger grid-wide restart

### Error Handling and Recovery

#### Timer Safety
- **Null Checks**: Comprehensive timer state validation
- **Exception Handling**: Safe timer disposal and cleanup
- **State Consistency**: Ensures restart state remains valid

#### File Operations
- **Safe File Access**: Protected marker file creation and deletion
- **Path Validation**: Handles missing or invalid marker directories
- **Cleanup Guarantee**: Marker files removed on abort or completion

## Logging and Debugging

### Log Levels
- **Debug**: Detailed operation logging including timer events, file operations, and state changes
- **Info**: High-level restart events, user notifications, and completion status
- **Warn**: Non-critical issues like missing dialog modules or configuration problems

### Log Message Format
All log messages are prefixed with "RestartModule:" for easy identification in log files.

**Example Log Messages**:
```
INFO  RestartModule: Scheduling restart for region 'Welcome Area' - Initiator: 00000000-0000-0000-0000-000000000000, Message: 'Maintenance in {0}', Notice: false
DEBUG RestartModule: Restart countdown started for region 'Welcome Area' with 3 alert intervals: [300, 60, 10]
INFO  RestartModule: Aborting restart for region 'Welcome Area' - Message: 'Maintenance postponed'
DEBUG RestartModule: Removed restart marker file for region 'Welcome Area'
```

## Use Cases

### Scheduled Maintenance
- **Planned Downtime**: Schedule maintenance restarts with appropriate user warning
- **Update Deployment**: Coordinate region restarts for software updates
- **Configuration Changes**: Restart regions after configuration modifications

### Emergency Response
- **Critical Issues**: Immediate restart capability for emergency situations
- **Resource Management**: Restart overloaded or problematic regions
- **State Recovery**: Reset regions experiencing persistent issues

### Grid Management
- **Rolling Restarts**: Coordinate sequential region restarts across grid
- **Load Balancing**: Restart regions during off-peak hours
- **Synchronization**: Ensure all regions restart with consistent state

## Integration Points

### Scene Framework
- **Scene.RestartNow()**: Final restart execution through scene management
- **SceneManager.Instance**: Access to grid-wide scene collection
- **Console Command System**: Integration with region-specific command handling

### User Communication
- **IDialogModule**: Primary interface for user notifications
- **Bluebox Notifications**: Dismissable dialog messages to users
- **Notice Notifications**: Transient alert messages to users

### Configuration System
- **IConfigSource**: Configuration file integration for module settings
- **Section Parsing**: Reads RestartModule and Startup configuration sections
- **Default Values**: Provides sensible defaults for all configuration options

## Security Considerations

### Access Control
- **Console Limitation**: Commands only available through simulator console
- **Administrative Access**: Requires simulator administrative privileges
- **No Remote Access**: No in-world or web-based restart capabilities

### State Protection
- **Timer Isolation**: Each region maintains independent restart state
- **Abort Safety**: Restart abort immediately cancels all timers and notifications
- **Cleanup Assurance**: Proper cleanup on module shutdown or region removal

## Performance Characteristics

### Resource Usage
- **Timer Overhead**: Minimal - single timer per scheduled restart
- **Memory Usage**: Small footprint with temporary alert lists
- **CPU Impact**: Negligible during normal operation, brief spikes during notifications

### Scalability
- **Region Independence**: Each region module operates independently
- **Grid Coordination**: Efficient cross-region agent counting when needed
- **Notification Efficiency**: Batch user notifications via dialog module

## Best Practices

### Restart Scheduling
1. **Adequate Warning**: Provide sufficient advance notice for users (minimum 5-10 minutes)
2. **Multiple Alerts**: Use escalating alert intervals (e.g., 900, 600, 300, 120, 60, 10 seconds)
3. **Clear Messaging**: Include reason for restart and expected duration
4. **Off-Peak Timing**: Schedule maintenance during low-usage periods

### Emergency Procedures
1. **Immediate Restart**: Use zero-delay restart for critical issues
2. **User Communication**: Always provide explanation when possible
3. **Grid Coordination**: Consider impact on neighboring regions
4. **State Backup**: Ensure region state is saved before emergency restart

### Grid Management
1. **Rolling Deployment**: Restart regions sequentially rather than simultaneously
2. **Load Monitoring**: Monitor user distribution during restart sequences
3. **Coordination Planning**: Plan grid-wide restarts during maintenance windows
4. **Recovery Preparation**: Have rollback procedures ready for failed updates

## Troubleshooting

### Common Issues

#### "No dialog module found" Warning
- **Cause**: DialogModule not loaded or available in region
- **Impact**: Restart notifications will not be sent to users
- **Solution**: Verify DialogModule is enabled in core module configuration

#### Restart Commands Not Available
- **Cause**: Module not loaded or command registration failed
- **Solution**: Check module loading logs and CoreModuleFactory integration

#### Timer Not Cancelling
- **Cause**: Timer disposal errors or state inconsistency
- **Solution**: Check for exceptions in logs, verify timer cleanup in RemoveRegion

### Debugging Steps
1. Enable debug logging for RestartModule
2. Verify module registration in region loading logs
3. Check console command registration success
4. Monitor timer creation and disposal events
5. Validate dialog module availability and functionality

## Related Modules

### Complementary Modules
- **DialogModule**: Required for user notification functionality
- **RegionCommandsModule**: Provides additional region management commands
- **GodsModule**: Administrative functions and emergency controls

### Integration Dependencies
- **Scene Framework**: Core scene management and restart execution
- **Console System**: Command registration and execution framework
- **Configuration System**: Module settings and behavioral configuration
- **Dialog System**: User communication and notification delivery

## Advanced Configuration Examples

### High-Availability Setup
```ini
[RestartModule]
MarkerPath = /shared/opensim/markers

[Startup]
SkipDelayOnEmptyRegion = true
InworldRestartShutsDown = false
```

### Grid-Wide Restart Coordination
```ini
[Startup]
InworldRestartShutsDown = true
SkipDelayOnEmptyRegion = false
```

### Development Environment
```ini
[RestartModule]
; No marker path for development

[Startup]
SkipDelayOnEmptyRegion = true
InworldRestartShutsDown = false
```