# JsonStoreCommandsModule Technical Documentation

## Overview

The JsonStoreCommandsModule is a specialized administrative component for OpenSimulator/Akisim that provides console command interfaces for managing and monitoring JsonStore operations. This optional non-shared region module extends the JsonStoreModule functionality by offering command-line tools for administrators to inspect, monitor, and troubleshoot JSON data storage systems. The module serves as a critical debugging and administration tool for servers using JsonStore functionality, enabling real-time monitoring of store statistics, usage patterns, and system health from the console interface.

## Architecture

The JsonStoreCommandsModule implements the following interface:
- `INonSharedRegionModule` - Per-region module lifecycle management

### Key Components

1. **Console Command Interface**
   - **Command Registration**: Integration with OpenSim's console command system
   - **Context-Aware Commands**: Commands operate on the current console scene context
   - **Administrative Access**: Secure access through console authentication
   - **Real-Time Monitoring**: Live statistics and status reporting

2. **JsonStore Integration**
   - **Module Discovery**: Automatic discovery of JsonStoreModule interface
   - **Direct Interface Access**: Direct access to JsonStore functionality
   - **Statistics Retrieval**: Real-time statistics and performance data
   - **Dependency Management**: Proper dependency resolution and error handling

3. **Statistics and Monitoring**
   - **Store Count Tracking**: Real-time monitoring of active store count
   - **Performance Metrics**: Store usage and performance statistics
   - **Health Monitoring**: System health and status reporting
   - **Diagnostic Information**: Detailed diagnostic data for troubleshooting

4. **Administrative Tools**
   - **Console Integration**: Full integration with OpenSim console system
   - **Multi-Region Support**: Per-region command execution and reporting
   - **Error Reporting**: Comprehensive error reporting and status messages
   - **Operational Visibility**: Clear visibility into JsonStore operations

## Configuration

### Module Activation

The module automatically loads when JsonStoreModule is enabled:

```ini
[Modules]
JsonStoreModule = true  ; This automatically enables JsonStoreCommandsModule
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
- **Context-Aware**: Commands operate on the current console scene
- **Console Integration**: Full integration with OpenSim console commands

### Dependencies

- **JsonStoreModule**: Required for functionality - will disable if not available
- **Console System**: Requires access to OpenSim console command system
- **Scene Context**: Operates within scene context for region-specific commands
- **Interface Access**: Requires IJsonStoreModule interface availability

## Features

### Console Commands

#### jsonstore stats Command

**Syntax**: `jsonstore stats`

**Purpose**: Display statistics about JsonStore usage in the current region

**Usage Context**: Must be executed with a specific region selected as console scene

**Output Format**:
```
RegionName    StoreCount
MyRegion      15
```

**Example Usage**:
```
Region (My Region) # jsonstore stats
My Region    15
```

**Information Provided**:
- **Region Name**: Name of the region being reported
- **Store Count**: Number of active JSON stores in the region

### Administrative Operations

#### Statistics Monitoring

The module provides real-time monitoring capabilities:

```csharp
JsonStoreStats stats = m_store.GetStoreStats();
MainConsole.Instance.Output("{0}\t{1}", m_scene.RegionInfo.RegionName, stats.StoreCount);
```

#### Health Checking

Automatic health checking and dependency validation:
- Verifies JsonStoreModule availability
- Reports configuration issues
- Provides error messages for troubleshooting

### Multi-Region Support

#### Region Context Awareness

Commands operate in the context of the currently selected console region:

```csharp
if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene != null)
    return;
```

#### Per-Region Statistics

Each region maintains independent statistics and monitoring:
- Region-specific store counts
- Independent monitoring per region
- Context-aware command execution

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
            // Module disabled if no JsonStore configuration
            return;
        }

        m_enabled = m_config.GetBoolean("Enabled", m_enabled);
    }
    catch (Exception e)
    {
        m_log.Error("[JsonStore]: initialization error: {0}", e);
        return;
    }

    if (m_enabled)
        m_log.DebugFormat("[JsonStore]: module is enabled");
}
```

#### Interface Discovery and Registration

```csharp
public void RegionLoaded(Scene scene)
{
    if (m_enabled)
    {
        m_scene = scene;

        m_store = (JsonStoreModule) m_scene.RequestModuleInterface<IJsonStoreModule>();
        if (m_store == null)
        {
            m_log.ErrorFormat("[JsonStoreCommands]: JsonModule interface not defined");
            m_enabled = false;
            return;
        }

        scene.AddCommand("JsonStore", this, "jsonstore stats", "jsonstore stats",
                         "Display statistics about the state of the JsonStore module", "",
                         CmdStats);
    }
}
```

### Command Implementation

#### Statistics Command Implementation

```csharp
private void CmdStats(string module, string[] cmd)
{
    // Ensure command runs in correct region context
    if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene != null)
        return;

    // Retrieve and display statistics
    JsonStoreStats stats = m_store.GetStoreStats();
    MainConsole.Instance.Output("{0}\t{1}", m_scene.RegionInfo.RegionName, stats.StoreCount);
}
```

#### Command Registration Pattern

```csharp
scene.AddCommand(
    "JsonStore",                           // Command category
    this,                                  // Module instance
    "jsonstore stats",                     // Command name
    "jsonstore stats",                     // Usage syntax
    "Display statistics about the state of the JsonStore module",  // Description
    "",                                    // Help text
    CmdStats                              // Handler method
);
```

### Error Handling and Validation

#### Dependency Validation

```csharp
// Check for JsonStore module availability
m_store = (JsonStoreModule) m_scene.RequestModuleInterface<IJsonStoreModule>();
if (m_store == null)
{
    m_log.ErrorFormat("[JsonStoreCommands]: JsonModule interface not defined");
    m_enabled = false;
    return;
}
```

#### Configuration Error Handling

```csharp
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
    m_log.Error("[JsonStore]: initialization error: {0}", e);
    return;
}
```

### Statistics Integration

#### Real-Time Statistics Retrieval

```csharp
// Retrieve current store statistics
JsonStoreStats stats = m_store.GetStoreStats();

// Format and display statistics
MainConsole.Instance.Output("{0}\t{1}",
    m_scene.RegionInfo.RegionName,
    stats.StoreCount);
```

## Performance Characteristics

### Resource Usage

- **Memory Footprint**: Minimal memory usage - only command registration overhead
- **CPU Impact**: Negligible CPU overhead - only active during command execution
- **Network Usage**: No network usage - operates locally
- **Storage Impact**: No persistent storage requirements

### Scalability Features

- **Per-Region Independence**: Each region operates independently
- **On-Demand Execution**: Only processes commands when explicitly invoked
- **Efficient Statistics**: Direct access to JsonStore statistics without overhead
- **Minimal Resource Usage**: No continuous background processing

### Performance Optimization

- **Direct Interface Access**: Efficient direct access to JsonStore functionality
- **Context Awareness**: Avoids unnecessary processing outside current region
- **Lazy Initialization**: Module components initialized only when needed
- **Efficient Command Processing**: Minimal overhead for command execution

## Usage Examples

### Basic Statistics Monitoring

```bash
# Select a region and check JsonStore statistics
Region (My Region) # jsonstore stats
My Region    15

# Switch to another region and check its statistics
Region (Other Region) # jsonstore stats
Other Region    8
```

### Administrative Monitoring Workflow

```bash
# 1. Check current region context
Region (My Region) # show info
Region Name: My Region
Region UUID: 12345678-1234-1234-1234-123456789abc
...

# 2. Monitor JsonStore usage
Region (My Region) # jsonstore stats
My Region    23

# 3. Switch to another region for comparison
Region (Test Region) # jsonstore stats
Test Region    5

# 4. Monitor over time by repeating commands
Region (My Region) # jsonstore stats
My Region    25    # Store count increased
```

### Troubleshooting Scenarios

```bash
# Scenario 1: JsonStore not working - check if module loaded
Region (My Region) # jsonstore stats
# If no output or error, JsonStore may not be enabled

# Scenario 2: Verify JsonStore is active across regions
Region (Region1) # jsonstore stats
Region1    12

Region (Region2) # jsonstore stats
Region2    8

Region (Region3) # jsonstore stats
Region3    0    # No stores in this region
```

### Monitoring Script Usage

```bash
# Monitor JsonStore usage after script activities
Region (My Region) # jsonstore stats
My Region    10

# ... scripts create/destroy stores ...

Region (My Region) # jsonstore stats
My Region    15    # Store count increased

# ... scripts clean up stores ...

Region (My Region) # jsonstore stats
My Region    12    # Some stores cleaned up
```

### Multi-Region Administration

```bash
# Check JsonStore usage across multiple regions
Region (Hub) # jsonstore stats
Hub    45

Region (Residential) # jsonstore stats
Residential    23

Region (Commercial) # jsonstore stats
Commercial    67

Region (Educational) # jsonstore stats
Educational    12

# Total stores across all regions: 147
```

## Integration Points

### With JsonStoreModule

- **Direct Interface Access**: Direct access to IJsonStoreModule functionality
- **Statistics Integration**: Real-time access to store statistics and metrics
- **Dependency Management**: Proper dependency checking and error handling
- **Lifecycle Coordination**: Coordinated initialization and shutdown

### With Console System

- **Command Registration**: Full integration with OpenSim console command system
- **Context Management**: Proper handling of console scene context
- **Output Formatting**: Consistent output formatting with console standards
- **Help Integration**: Integrated with console help and documentation system

### With Scene Management

- **Region Context**: Operates within specific region contexts
- **Multi-Region Support**: Independent operation across multiple regions
- **Scene Integration**: Proper integration with scene lifecycle management
- **Resource Management**: Participates in scene resource management

### With Administrative Tools

- **Monitoring Integration**: Provides data for administrative monitoring tools
- **Diagnostic Support**: Supports diagnostic and troubleshooting workflows
- **Status Reporting**: Provides status information for administrative oversight
- **Health Checking**: Contributes to overall system health monitoring

## Security Features

### Access Control

- **Console Access**: Requires administrative console access
- **Authentication**: Integrated with console authentication system
- **Command Permissions**: Protected through console permission system
- **Region Context**: Commands restricted to appropriate region contexts

### Information Security

- **Read-Only Operations**: All commands are read-only and non-destructive
- **Safe Statistics**: Only exposes safe statistical information
- **No Data Access**: Does not provide access to actual JSON data content
- **Audit Trail**: All command executions logged through console system

### Operational Security

- **Error Isolation**: Errors don't affect JsonStore operations
- **Safe Execution**: All operations are safe and non-disruptive
- **Resource Protection**: No resource exhaustion possibilities
- **Graceful Degradation**: Degrades gracefully when JsonStore unavailable

## Debugging and Troubleshooting

### Common Issues

1. **Commands Not Available**: Check that JsonStoreModule is enabled and loaded
2. **No Output from Commands**: Verify correct region context is selected
3. **Module Not Loading**: Check JsonStore configuration section exists
4. **Interface Not Found**: Verify JsonStoreModule is loaded before commands module

### Diagnostic Procedures

1. **Module Loading**: Check logs for JsonStoreCommandsModule loading messages
2. **Interface Availability**: Verify JsonStoreModule interface is available
3. **Command Registration**: Check that commands are properly registered
4. **Console Context**: Verify correct region is selected in console

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
```

### Debug Information

```bash
# Check module loading in logs
# Look for these log messages:
# [JsonStore]: module is enabled
# JsonStoreCommandsModule loaded for JsonStore console commands and store statistics

# Verify console command registration
Region (My Region) # help jsonstore
# Should show available jsonstore commands

# Test command functionality
Region (My Region) # jsonstore stats
# Should show region name and store count
```

## Use Cases

### System Administration

- **Usage Monitoring**: Monitor JsonStore usage across regions
- **Performance Tracking**: Track JsonStore performance and resource usage
- **Capacity Planning**: Plan capacity based on store usage patterns
- **Health Monitoring**: Monitor system health and detect issues

### Development and Testing

- **Development Monitoring**: Monitor JsonStore usage during script development
- **Testing Validation**: Validate JsonStore behavior during testing
- **Performance Testing**: Monitor performance during load testing
- **Regression Testing**: Verify JsonStore functionality after updates

### Operational Support

- **Troubleshooting**: Diagnose JsonStore-related issues
- **User Support**: Support users with JsonStore-related problems
- **System Maintenance**: Monitor system during maintenance activities
- **Documentation**: Document system usage and patterns

### Forensic Analysis

- **Usage Analysis**: Analyze JsonStore usage patterns
- **Problem Investigation**: Investigate performance or functionality issues
- **Audit Support**: Provide audit trail information
- **Trend Analysis**: Analyze usage trends over time

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via JsonStoreModule configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify JsonStoreModule is enabled to activate commands module
- Test console command functionality after deployment
- Update any administrative scripts using JsonStore commands
- Validate integration with monitoring systems

### Deployment Considerations

- **Administrative Access**: Ensure administrative console access is properly configured
- **JsonStore Dependency**: Verify JsonStoreModule is properly loaded and configured
- **Console Integration**: Ensure console system is properly configured
- **Multi-Region Setup**: Test commands across all regions in deployment

## Configuration Examples

### Basic Configuration

```ini
[Modules]
JsonStoreModule = true  ; Automatically enables JsonStoreCommandsModule

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
MaxStringSpace = 1048576       ; 1MB limit

[Logging]
LogLevel = INFO
```

### Administrative Configuration

```ini
[Modules]
JsonStoreModule = true

[JsonStore]
Enabled = true
EnableObjectStore = true
MaxStringSpace = 10485760      ; 10MB for administrative use

[Console]
# Configure console access for administrators
```

## Best Practices

### Administrative Operations

1. **Regular Monitoring**: Monitor JsonStore usage regularly
2. **Trend Analysis**: Track usage trends over time
3. **Capacity Planning**: Plan capacity based on observed usage
4. **Documentation**: Document observed patterns and issues

### Troubleshooting Procedures

1. **Systematic Approach**: Use systematic approach to diagnose issues
2. **Log Analysis**: Analyze logs for error patterns and issues
3. **Context Awareness**: Always verify correct region context
4. **Documentation**: Document troubleshooting procedures and solutions

### Security Practices

1. **Access Control**: Restrict console access to authorized personnel
2. **Audit Logging**: Monitor console command usage
3. **Regular Review**: Review access patterns and usage
4. **Documentation**: Document security procedures and policies

## Future Enhancements

### Potential Improvements

1. **Enhanced Statistics**: More detailed statistics and metrics
2. **Historical Data**: Historical usage data and trend analysis
3. **Export Capabilities**: Data export for external analysis
4. **Alerting**: Automated alerting for usage thresholds

### Compatibility Considerations

1. **Console Evolution**: Adapt to console system updates
2. **JsonStore Updates**: Maintain compatibility with JsonStore updates
3. **Administrative Tools**: Integration with evolving administrative tools
4. **Monitoring Systems**: Enhanced integration with monitoring systems

### Integration Opportunities

1. **Web Interface**: Web-based administration interface
2. **API Integration**: REST API for programmatic access
3. **Monitoring Tools**: Enhanced integration with monitoring systems
4. **Automation**: Automated monitoring and alerting capabilities