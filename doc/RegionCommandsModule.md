# RegionCommandsModule Technical Documentation

## Overview

The **RegionCommandsModule** is a core OpenSimulator module that provides comprehensive console commands for managing, inspecting, and configuring regions. It offers administrators powerful tools for region analysis, performance monitoring, configuration management, and grid connectivity information. The module is essential for region administration, performance troubleshooting, and operational management.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **Console Command Interface**: Comprehensive console command registration and handling

### Key Components
- **Region Information Display**: Detailed region configuration and status reporting
- **Scene Statistics**: Real-time performance and operational metrics
- **Region Configuration**: Runtime configuration parameter management
- **Grid Connectivity**: Neighbor region discovery and visibility analysis
- **Performance Monitoring**: Live scene statistics and performance metrics

## Console Command Categories

### Region Information Commands

#### show region / region get
```bash
show region
region get
```
- **Purpose**: Display comprehensive region configuration and settings
- **Aliases**: Both commands provide identical functionality
- **Output**: Complete region properties, settings, and configuration details
- **Usage**: Region analysis, configuration verification, administrative overview

**Displayed Information:**
- **Region Identity**: ID, handle, location, size, type, maturity rating
- **Network Configuration**: Server URI, endpoints, access level
- **Capacity Settings**: Agent limits, prim capacity, linkset capacity
- **Prim Constraints**: Size limits for physical and non-physical objects
- **Region Rules**: Damage, land operations, flight restrictions
- **Physics Settings**: Collision and physics controls
- **Environment**: Sun position, water height, terrain limits
- **Map Information**: Maptile data and refresh timestamps

#### region set
```bash
region set <parameter> <value>
```
- **Purpose**: Modify specific region configuration parameters at runtime
- **Parameters**: Limited set of runtime-modifiable settings
- **Persistence**: Changes are saved to region settings automatically
- **Validation**: Parameter validation and constraint checking

**Configurable Parameters:**
- **agent-limit**: Current root agent limit (persisted across restarts)
- **max-agent-limit**: Maximum agent capacity (not persisted, requires regions file entry)

### Scene Statistics Commands

#### show scene
```bash
show scene
```
- **Purpose**: Display real-time scene performance and operational statistics
- **Data Source**: Live statistics from SimStatsReporter
- **Update Frequency**: Real-time current values
- **Usage**: Performance monitoring, troubleshooting, capacity planning

**Statistical Categories:**
- **Performance Metrics**: Time dilation, simulation FPS, physics FPS
- **Population Data**: Root agents, child agents, active scripts
- **Object Statistics**: Total prims, physics-enabled prims
- **Timing Analysis**: Frame times for different simulation components
- **Network Statistics**: Packet rates, unacknowledged bytes, pending transfers
- **Script Performance**: Active scripts, script lines processed per second

### Grid Connectivity Commands

#### show neighbours
```bash
show neighbours
```
- **Purpose**: Display all neighboring regions detected by the grid service
- **Data Source**: Grid service neighbor discovery
- **Output**: List of adjacent regions with coordinates
- **Usage**: Grid connectivity verification, neighbor relationship analysis

#### show regionsinview
```bash
show regionsinview
```
- **Purpose**: Display all regions visible from current region within view distance
- **Calculation**: Based on MaxRegionViewDistance setting
- **Scope**: All regions within maximum view range
- **Usage**: View distance analysis, region visibility planning

## Detailed Information Display

### Region Configuration Report
```csharp
private void HandleShowRegion(string module, string[] cmd)
{
    RegionInfo ri = m_scene.RegionInfo;
    RegionSettings rs = ri.RegionSettings;

    ConsoleDisplayList dispList = new ConsoleDisplayList();
    dispList.AddRow("Region ID", ri.RegionID);
    dispList.AddRow("Region handle", ri.RegionHandle);
    dispList.AddRow("Region location", string.Format("{0},{1}", ri.RegionLocX, ri.RegionLocY));
    dispList.AddRow("Region size", string.Format("{0}x{1}", ri.RegionSizeX, ri.RegionSizeY));
    dispList.AddRow("Maturity", rs.Maturity);
    // ... additional configuration details
}
```

### Real-Time Statistics
```csharp
private void HandleShowScene(string module, string[] cmd)
{
    SimStatsReporter r = m_scene.StatsReporter;
    float[] stats = r.LastReportedSimStats;

    float timeDilation = stats[0];
    float simFps = stats[1];
    float physicsFps = stats[2];
    float rootAgents = stats[4];
    float totalPrims = stats[6];
    float activeScripts = stats[19];
    // ... process and display statistics
}
```

### Grid Connectivity Analysis
```csharp
public void HandleShowNeighboursCommand(string module, string[] cmdparams)
{
    RegionInfo sr = m_scene.RegionInfo;
    List<GridRegion> regions = m_scene.GridService.GetNeighbours(sr.ScopeID, sr.RegionID);

    foreach (GridRegion r in regions)
        caps.AppendFormat("    {0} @ {1}-{2}\n", r.RegionName,
                         Util.WorldToRegionLoc((uint)r.RegionLocX),
                         Util.WorldToRegionLoc((uint)r.RegionLocY));
}
```

## Configuration Management

### Runtime Parameter Modification
```csharp
private void HandleRegionSet(string module, string[] args)
{
    string param = args[2];
    string rawValue = args[3];

    RegionInfo ri = m_scene.RegionInfo;
    RegionSettings rs = ri.RegionSettings;

    if (param == "agent-limit")
    {
        int newValue;
        if (!ConsoleUtil.TryParseConsoleNaturalInt(MainConsole.Instance, rawValue, out newValue))
            return;

        if (newValue > ri.AgentCapacity)
        {
            MainConsole.Instance.Output("Cannot set {0} to {1} as max-agent-limit is {3}",
                                       "agent-limit", newValue, ri.AgentCapacity);
        }
        else
        {
            rs.AgentLimit = newValue;
            MainConsole.Instance.Output("{0} set to {1}", "agent-limit", newValue);
        }
        rs.Save();
    }
}
```

### Validation and Constraints
- **Agent Limit Validation**: Ensures agent-limit doesn't exceed max-agent-limit
- **Parameter Type Checking**: Validates numeric parameters
- **Bounds Checking**: Prevents invalid configuration values
- **Automatic Adjustment**: Adjusts dependent parameters when needed

## Performance Monitoring

### Key Performance Indicators
```csharp
// Core performance metrics
float timeDilation;      // Simulation time vs. real time (1.0 = normal)
float simFps;           // Simulation frames per second
float physicsFps;       // Physics simulation frames per second

// Population metrics
float rootAgents;       // Primary avatars in region
float childAgents;      // Child agents from neighboring regions

// Object and script metrics
float totalPrims;       // Total primitive objects
float activePrims;      // Physics-enabled primitives
float activeScripts;    // Currently running scripts
float scriptLinesPerSecond; // Script execution rate

// Timing analysis
float totalFrameTime;   // Total simulation frame time
float physicsFrameTime; // Physics calculation time
float otherFrameTime;   // Other simulation overhead
```

### Network Performance
```csharp
// Network statistics
float inPacketsPerSecond;   // Incoming packets from clients
float outPacketsPerSecond;  // Outgoing packets to clients
float unackedBytes;         // Unacknowledged data
float pendingDownloads;     // Pending asset downloads
float pendingUploads;       // Pending asset uploads
```

### Performance Analysis
- **Time Dilation**: Values below 1.0 indicate simulation lag
- **Frame Rates**: Target simulation FPS varies by configuration
- **Network Load**: High unacknowledged bytes suggest network issues
- **Script Performance**: High script line rates may indicate performance impact

## Grid Integration

### Neighbor Discovery
```csharp
List<GridRegion> regions = m_scene.GridService.GetNeighbours(sr.ScopeID, sr.RegionID);
```
- **Grid Service Query**: Uses grid service for neighbor discovery
- **Scope Awareness**: Respects grid scope boundaries
- **Real-Time Data**: Current neighbor status from grid

### View Distance Calculation
```csharp
public void HandleShowRegionsInViewCommand(string module, string[] cmdparams)
{
    int maxview = (int)m_scene.MaxRegionViewDistance;
    RegionInfo sr = m_scene.RegionInfo;

    int startX = (int)sr.WorldLocX - maxview;
    int endX = startX + (int)sr.RegionSizeX + maxview;
    int startY = (int)sr.WorldLocY - maxview;
    int endY = startY + (int)sr.RegionSizeY + maxview;

    List<GridRegion> regions = m_scene.GridService.GetRegionRange(sr.ScopeID, startX, endX, startY, endY);
}
```

### Connectivity Analysis
- **View Distance**: Calculates region visibility based on MaxRegionViewDistance
- **Coordinate Mapping**: Converts world coordinates to region coordinates
- **Range Queries**: Efficient grid queries for region ranges
- **Self-Exclusion**: Excludes current region from neighbor lists

## Administrative Use Cases

### Region Health Monitoring
- **Performance Assessment**: Monitor FPS, time dilation, frame times
- **Capacity Planning**: Track agent counts, prim usage, script load
- **Network Analysis**: Monitor packet rates and transfer status
- **Resource Utilization**: Assess physics load and simulation overhead

### Configuration Management
- **Runtime Tuning**: Adjust agent limits based on performance
- **Capacity Management**: Set appropriate limits for region resources
- **Settings Verification**: Confirm region configuration parameters
- **Documentation**: Generate configuration reports for compliance

### Grid Operations
- **Connectivity Testing**: Verify neighbor region connectivity
- **View Distance Planning**: Analyze region visibility and draw distance
- **Network Topology**: Understand regional relationships
- **Grid Coordination**: Coordinate with neighboring region operators

### Troubleshooting Support
- **Performance Diagnosis**: Identify simulation bottlenecks
- **Network Issues**: Diagnose connection and transfer problems
- **Configuration Problems**: Verify and correct region settings
- **Grid Connectivity**: Resolve neighbor discovery issues

## Security and Access Control

### Console Access Control
```csharp
if (!(MainConsole.Instance.ConsoleScene == null || MainConsole.Instance.ConsoleScene == m_scene))
    return;
```

### Administrative Privileges
- **Console Access**: Commands require administrative console access
- **Scene Context**: Commands operate only on selected scene
- **Parameter Validation**: Input validation prevents malformed commands
- **Configuration Safety**: Validation prevents invalid configurations

### Safe Configuration Changes
- **Constraint Checking**: Parameter changes validated against limits
- **Automatic Persistence**: Changes automatically saved to region settings
- **Rollback Safety**: Invalid changes rejected without affecting region
- **Clear Feedback**: Detailed success/failure messages for all operations

## Error Handling and Validation

### Input Validation
```csharp
if (args.Length != 4)
{
    MainConsole.Instance.Output("Usage: region set <param> <value>");
    return;
}

if (!ConsoleUtil.TryParseConsoleNaturalInt(MainConsole.Instance, rawValue, out newValue))
    return;
```

### Safe Operations
- **Parameter Counting**: Verify correct number of command arguments
- **Type Validation**: Ensure parameters are correct data types
- **Range Checking**: Validate parameter values within acceptable ranges
- **Scene Availability**: Check scene availability before operations

### Graceful Error Handling
- **Clear Error Messages**: Descriptive error messages for invalid input
- **Safe Fallbacks**: Operations fail safely without affecting region
- **Status Reporting**: Clear feedback on operation success/failure
- **Help Information**: Usage information provided for invalid commands

## Performance Considerations

### Efficient Data Access
- **Direct Scene Access**: Uses optimized scene data structures
- **Cached Statistics**: Leverages pre-calculated statistics from SimStatsReporter
- **Lazy Evaluation**: Statistics calculated only when requested
- **Minimal Overhead**: Commands have minimal impact on simulation performance

### Real-Time Operations
- **Live Data**: Statistics reflect current simulation state
- **Non-Blocking**: Commands don't interfere with simulation
- **Quick Response**: Fast command execution for operational use
- **Memory Efficient**: Minimal memory allocation for display operations

### Grid Query Optimization
- **Efficient Queries**: Optimized grid service queries for neighbor discovery
- **Range Limiting**: View distance calculations limit query scope
- **Caching Friendly**: Grid queries suitable for service-side caching
- **Batch Operations**: Single queries for multiple region data

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource source)
{
    // No specific configuration required
}
```
- **No Configuration**: Module requires no external configuration
- **Always Active**: Module initializes automatically as core functionality

### Region Integration
```csharp
public void AddRegion(Scene scene)
{
    m_scene = scene;
    m_console = MainConsole.Instance;

    // Register all console commands
    m_console.Commands.AddCommand("Regions", false, "show scene", ...);
    m_console.Commands.AddCommand("Regions", false, "show region", ...);
    // ... additional command registrations
}
```

### Command Registration
The module registers comprehensive command sets:
- **6 Total Commands**: Complete region management suite
- **Category "Regions"**: Organized under Regions command category
- **Admin Level**: Commands require administrative console access
- **Help Integration**: All commands include detailed help text

### Cleanup
```csharp
public void RemoveRegion(Scene scene)
{
    // Commands automatically cleaned up by console system
}
```

## Integration Examples

### Region Status Check
```bash
# Get complete region information
show region

# Check real-time performance
show scene

# Verify grid connectivity
show neighbours
show regionsinview
```

### Configuration Management
```bash
# View current agent limit
show region | grep "Agent limit"

# Adjust agent limit
region set agent-limit 50

# Set maximum agent capacity
region set max-agent-limit 100

# Verify changes
show region
```

### Performance Monitoring
```bash
# Monitor simulation performance
show scene

# Check for performance issues
# Look for: Time dilation < 1.0, Low FPS, High frame times

# Monitor over time with scripted checks
while true; do
    echo "$(date): $(show scene | grep 'Time Dilation')"
    sleep 30
done
```

### Grid Analysis
```bash
# Check region connectivity
show neighbours

# Analyze view distance coverage
show regionsinview

# Compare with expected neighbors
# Verify all expected adjacent regions appear
```

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Always Enabled**: Module loaded by default as essential functionality
- **No Configuration**: Module requires no configuration settings
- **Logging Integration**: Comprehensive debug and info logging for operations

### Backward Compatibility
- **Command Compatibility**: All existing console commands remain unchanged
- **Output Format**: Console output format and structure unchanged
- **Parameter Syntax**: All command parameters and options unchanged
- **Help System**: Command help and documentation remain identical

### Dependencies
- **Console System**: Requires MainConsole.Instance for command registration
- **Scene Management**: Integration with scene and region lifecycle
- **Grid Service**: Depends on grid service for neighbor discovery
- **Statistics Reporter**: Uses SimStatsReporter for real-time metrics

## Troubleshooting

### Common Issues

#### Commands Not Available
- **Module Loading**: Verify RegionCommandsModule is loaded in factory
- **Console Access**: Ensure administrative console access
- **Command Registration**: Check for command registration errors in logs
- **Scene Context**: Verify commands are run in correct scene context

#### Statistics Not Updating
- **SimStatsReporter**: Verify statistics reporter is functioning
- **Scene State**: Ensure scene is fully loaded and running
- **Timing Issues**: Statistics may have slight delays
- **Module Synchronization**: Check module load order dependencies

#### Grid Connectivity Issues
- **Grid Service**: Verify grid service is accessible and responding
- **Network Connectivity**: Check network connectivity to grid service
- **Scope Configuration**: Ensure correct scope ID configuration
- **Service Discovery**: Verify grid service discovery mechanisms

#### Configuration Changes Not Persisting
- **Write Permissions**: Check file system write permissions
- **Configuration Files**: Verify region settings file accessibility
- **Parameter Validation**: Ensure parameter values pass validation
- **Service Availability**: Check region settings save functionality

## Usage Examples

### Daily Operations
```bash
# Morning region health check
show scene
show region

# Check for performance issues
# Time dilation should be close to 1.0
# FPS should be at target levels

# Verify grid connectivity
show neighbours
```

### Performance Troubleshooting
```bash
# Identify performance bottlenecks
show scene

# Look for indicators:
# - Time dilation < 0.9 (simulation lag)
# - Low FPS (processing overload)
# - High physics frame time (physics issues)
# - High script lines/sec (script load)
```

### Capacity Management
```bash
# Check current capacity usage
show region
show scene

# Adjust limits based on performance
region set agent-limit 40  # Reduce if experiencing lag
region set max-agent-limit 60

# Verify changes
show region
```

### Grid Administration
```bash
# Analyze regional relationships
show neighbours
show regionsinview

# Document region configuration
show region > region-config.txt
show scene > region-stats.txt
```

This documentation reflects the RegionCommandsModule implementation in `src/OpenSim.Region.CoreModules/World/Region/RegionCommandsModule.cs` and its integration with the factory-based module loading system.