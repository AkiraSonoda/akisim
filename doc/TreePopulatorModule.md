# TreePopulatorModule Technical Documentation

## Overview

The **TreePopulatorModule** is an advanced OpenSimulator optional module that provides sophisticated automated tree ecosystem management. It creates and maintains dynamic tree populations through natural lifecycle simulation including growth, seeding, and death cycles. The module implements the concept of "copses" (groups of trees) with configurable parameters for realistic vegetation management.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **ICommandableModule**: Console command integration for tree management
- **IVegetationModule**: Tree creation and vegetation functionality

### Key Components
- **Copse Management**: Tree group definitions with lifecycle parameters
- **Automated Growth**: Dynamic tree scaling over time
- **Natural Seeding**: Probabilistic tree propagation from existing trees
- **Death Simulation**: Realistic tree removal based on age and density
- **Console Interface**: Comprehensive administrative commands
- **XML Serialization**: Persistent copse definitions

## Copse System

### Copse Definition
A copse represents a managed group of trees with shared characteristics:

```csharp
public class Copse
{
    public string m_name;              // Unique copse identifier
    public Boolean m_frozen;           // Activity state (frozen/active)
    public Tree m_tree_type;           // Tree variety (Cypress1, Cypress2, etc.)
    public int m_tree_quantity;        // Target tree population
    public float m_treeline_low;       // Minimum altitude for tree growth
    public float m_treeline_high;      // Maximum altitude for tree growth
    public Vector3 m_seed_point;       // Central seeding location
    public double m_range;             // Maximum spread radius from seed point
    public Vector3 m_initial_scale;    // Starting tree size
    public Vector3 m_maximum_scale;    // Full-grown tree size
    public Vector3 m_rate;             // Growth rate per update cycle
    public List<UUID> m_trees;         // Managed tree object UUIDs
}
```

### Copse Configuration Methods
1. **XML File Loading**: Load copse definitions from external XML files
2. **String Definition**: Parse copse parameters from formatted strings
3. **Programmatic Creation**: Direct instantiation with parameters

## Tree Lifecycle Management

### Growth System
Trees dynamically grow from initial scale to maximum scale:
- **Growth Rate**: Configurable per-axis scaling increments
- **Random Variation**: 20%-100% growth rate randomization per cycle
- **Maximum Constraints**: Growth stops when reaching maximum scale
- **Performance Optimization**: Growth affects only trees below maximum size

### Seeding System
Automated tree propagation simulates natural forest expansion:
- **Parent Selection**: Random selection from existing mature trees
- **Seeding Criteria**: Trees must reach 75% of maximum scale to seed
- **Proximity Spawning**: New trees created within 1.25x maximum scale distance
- **Range Constraints**: All trees must stay within copse range from seed point
- **Population Control**: Seeding stops when reaching target quantity
- **Altitude Validation**: New trees respect treeline constraints

### Death System
Realistic tree removal maintains ecosystem balance:
- **Population Pressure**: More deaths when exceeding 98% of target quantity
- **Size-based Probability**: Larger trees have higher death probability
- **Random Selection**: Stochastic death events for natural variation
- **Cleanup Handling**: Automatic removal of deleted or lost tree references

## Console Commands

### Copse Management
```bash
tree load <filename>              # Load copse definition from XML file
tree plant <copse_name>           # Start initial tree planting for copse
tree remove <copse_name>          # Remove copse and delete all trees
tree reload                       # Reload copses from existing scene trees
tree statistics                   # Display comprehensive copse statistics
```

### Activity Control
```bash
tree active <true|false>          # Enable/disable automated tree lifecycle
tree freeze <copse_name> <true|false>  # Freeze/unfreeze specific copse
tree rate <milliseconds>          # Set update rate (minimum 1000ms)
```

### Tree Naming Convention
Trees are automatically named with encoded copse definitions:
- **Active Trees**: `ATPM:copse_name;parameters...`
- **Frozen Trees**: `FTPM:copse_name;parameters...`

## Configuration System

### Module Configuration
```ini
[Trees]
; Enable the TreePopulatorModule (default: true)
enabled = true

; Enable automated tree growth, seeding, and death (default: false)
active_trees = true

; Allow trees to grow in size over time (default: true)
allowGrow = true

; Update rate in milliseconds (default: 1000, minimum: 1000)
update_rate = 5000
```

### Module Loading
```ini
[Modules]
; Enable TreePopulatorModule (default: false - must be explicitly enabled)
TreePopulatorModule = true
```

## Performance Features

### Optimized Updates
- **Configurable Update Rate**: Minimum 1000ms between lifecycle cycles
- **Thread Safety**: Mutex-protected copse operations
- **Efficient Iteration**: Direct scene object lookup by UUID
- **Cleanup Handling**: Automatic removal of invalid tree references

### Memory Management
- **UUID Tracking**: Lightweight tree reference management
- **Lost Tree Cleanup**: Automatic removal of deleted objects from copse lists
- **Lock Optimization**: Non-blocking mutex attempts prevent thread stalls

### Regional Constraints
- **Boundary Checking**: Trees cannot spawn outside region boundaries
- **Altitude Validation**: Respect configured treeline constraints
- **Range Enforcement**: Trees stay within copse radius limits

## Tree Creation Integration

### VegetationModule Compatibility
TreePopulatorModule includes its own tree creation functionality:
- **Phantom Objects**: Trees automatically created with phantom flag
- **Scene Integration**: Proper scene object registration and permissions
- **PCode Support**: Uses Tree PCode type for viewer rendering

### Shape Configuration
```csharp
treeShape.PathCurve = 16;           // Tree-specific path curve
treeShape.PathEnd = 49900;          // Complete path rendering
treeShape.PCode = (byte)PCode.Tree; // Tree primitive type
treeShape.Scale = scale;            // Dynamic scaling
treeShape.State = (byte)treeType;   // Tree variety identifier
```

## Administrative Features

### Copse Discovery
The module automatically discovers existing managed trees:
- **Scene Scanning**: Identifies trees with ATPM/FTPM naming convention
- **Copse Reconstruction**: Rebuilds copse definitions from scene data
- **Automatic Registration**: Re-registers trees with appropriate copses

### Debug and Monitoring
- **Comprehensive Logging**: Debug logging for all lifecycle operations
- **Statistics Reporting**: Detailed copse status and tree counts
- **Error Handling**: Graceful handling of invalid copse definitions

### Freeze/Thaw System
Individual copse activity can be controlled:
- **Selective Freezing**: Disable lifecycle for specific copses
- **Name Updates**: Trees renamed between ATPM/FTPM conventions
- **State Persistence**: Freeze state maintained across restarts

## XML Serialization

### Copse File Format
```xml
<Copse>
    <m_name>ForestCopse</m_name>
    <m_frozen>false</m_frozen>
    <m_tree_type>Cypress1</m_tree_type>
    <m_tree_quantity>50</m_tree_quantity>
    <m_treeline_low>20.0</m_treeline_low>
    <m_treeline_high>80.0</m_treeline_high>
    <m_seed_point>&lt;128, 128, 25&gt;</m_seed_point>
    <m_range>64.0</m_range>
    <m_initial_scale>&lt;1, 1, 1&gt;</m_initial_scale>
    <m_maximum_scale>&lt;4, 4, 8&gt;</m_maximum_scale>
    <m_rate>&lt;0.1, 0.1, 0.1&gt;</m_rate>
</Copse>
```

### Serialization Methods
- **SerializeObject**: Save copse definitions to XML files
- **DeserializeObject**: Load copse definitions from XML files
- **Error Handling**: Comprehensive exception handling for file operations

## String-based Configuration

### Copse Definition Format
```
<State>TPM: <name>; <quantity>; <treeline_high>; <treeline_low>; <range>; <tree_type>; <seed_point>; <initial_scale>; <maximum_scale>; <rate>;
```

Example:
```
ATPM: OakGrove; 25; 60.0; 20.0; 32.0; Tree; <100, 100, 22>; <1, 1, 1>; <3, 3, 6>; <0.05, 0.05, 0.08>;
```

## Integration Points

### Scene Integration
- **Height Map Access**: Trees positioned based on terrain height
- **Region Boundaries**: Automatic region size detection and constraint
- **Object Management**: Full scene object lifecycle integration
- **Permission System**: Estate owner assignment for created trees

### Timer Integration
- **System.Timers.Timer**: Reliable lifecycle update scheduling
- **Auto-reset Control**: Manual timer restart for rate changes
- **Thread Safety**: Timer callbacks use mutex protection

### Event System
- **OnPrimsLoaded**: Automatic copse reload when region starts
- **OnPluginConsole**: Console command processing integration
- **Module Registration**: Scene commander interface registration

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Optional Module**: Added to OptionalModulesFactory.CreateOptionalRegionModules
- **Configuration-based Loading**: Disabled by default, must be explicitly enabled
- **Comprehensive Logging**: Debug and info logging for all operations

### Dependencies
- **Core Framework**: OpenSim.Framework for basic functionality
- **Scene Management**: OpenSim.Region.Framework.Scenes for region integration
- **Interface Commander**: Console command framework integration
- **XML Serialization**: System.Xml for copse persistence

## Security Considerations

### Command Authorization
- **Hazardous Commands**: Most tree commands marked as hazardous operations
- **Estate Owner**: Trees created with estate owner permissions
- **Scene Integration**: Respects scene permission and security systems

### Resource Management
- **Population Limits**: Configurable tree quantity constraints
- **Update Rate Limits**: Minimum 1000ms update rate prevents excessive CPU usage
- **Region Boundaries**: Prevents tree creation outside valid coordinates

## Usage Examples

### Basic Copse Creation
```bash
# Load a copse definition from XML file
tree load "/path/to/copse-definition.xml"

# Plant the initial seed tree
tree plant "ForestCopse"

# Activate automated lifecycle
tree active true
```

### Dynamic Copse Management
```bash
# Check current status
tree statistics

# Adjust update rate for performance
tree rate 2000

# Temporarily freeze a copse
tree freeze "ForestCopse" true

# Remove problematic copse
tree remove "ForestCopse"
```

### Configuration Setup
```ini
[Trees]
enabled = true
active_trees = true
allowGrow = true
update_rate = 3000

[Modules]
TreePopulatorModule = true
```

This documentation reflects the TreePopulatorModule implementation in `src/OpenSim.Region.OptionalModules/World/TreePopulator/TreePopulatorModule.cs` and its integration with the factory-based module loading system.