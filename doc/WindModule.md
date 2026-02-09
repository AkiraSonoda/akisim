# WindModule Technical Documentation

## Overview

The **WindModule** is a core OpenSimulator module responsible for managing wind simulation and environmental effects within regions. It provides a pluggable architecture for different wind models, allowing realistic wind effects that can influence object physics, particle systems, and environmental immersion. The module supports dynamic wind patterns through configurable plugins and real-time client updates.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **IWindModule**: Wind-specific functionality interface for external access

### Key Components
- **Pluggable Wind Models**: Swappable wind simulation algorithms
- **Real-time Updates**: Frame-based wind calculation and client transmission
- **Console Interface**: Administrative commands for wind management
- **Client Protocol**: Wind data transmission to viewers for environmental effects

## Wind Model Plugin System

### Available Wind Models

#### SimpleRandomWind
- **Description**: Basic random wind pattern generator
- **Characteristics**: Simple random wind vectors with configurable strength
- **Use Case**: Basic environmental effects without complex patterns
- **Default**: This is the default wind model (`m_dWindPluginName = "SimpleRandomWind"`)

#### ConfigurableWind
- **Description**: Advanced configurable wind simulation
- **Characteristics**: Customizable wind strength, direction, and variation patterns
- **Use Case**: Realistic wind patterns with directional consistency
- **Parameters**: Average strength, direction, turbulence, and regional effects

### Plugin Interface (IWindModelPlugin)
Each wind model plugin implements:

```csharp
public interface IWindModelPlugin : IPlugin
{
    string Description { get; }                          // Plugin description
    void WindConfig(Scene scene, IConfig windConfig);    // Configuration setup
    bool WindUpdate(uint frame);                         // Frame-based wind updates
    Vector3 WindSpeed(float x, float y, float z);        // Wind at specific coordinates
    Vector2[] WindLLClientArray();                       // Client-compatible wind data (16x16 grid)
    Dictionary<string, string> WindParams();             // Available parameters
    void WindParamSet(string param, float value);        // Set parameter value
    float WindParamGet(string param);                    // Get parameter value
}
```

## Wind Data System

### Resolution and Coverage
- **Grid Resolution**: 16x16 wind speed array (256 data points)
- **Region Coverage**: Covers entire region with uniform resolution
- **Client Protocol**: Compatible with Second Life/OpenSim viewer wind rendering
- **Update Frequency**: Configurable frame-based updates (default: every 150 frames)

### Wind Speed Calculation
- **Coordinate System**: Region-local coordinates (0-255 for standard 256m regions)
- **Vector Components**: 3D wind vectors (X, Y, Z components)
- **Client Transmission**: 2D wind vectors (X, Y) sent to clients for efficiency
- **Z-Component**: Available for physics calculations but not transmitted to clients

## Configuration System

### Wind Configuration Section
```ini
[Wind]
; Enable wind simulation (default: true)
enabled = true

; Update rate in frames (default: 150 frames between updates)
wind_update_rate = 150

; Wind model plugin to use (default: SimpleRandomWind)
wind_plugin = SimpleRandomWind

; Plugin-specific parameters (varies by plugin)
; For SimpleRandomWind:
strength = 1.0

; For ConfigurableWind:
avg_strength = 5.0
avg_direction = 0.0
var_strength = 1.0
var_direction = 30.0
rate_change = 1.0
```

### Module Loading
The WindModule is loaded by default in CoreModuleFactory but can be controlled:
```ini
[Wind]
; Disable wind simulation entirely
enabled = false
```

## Console Commands

### Wind Parameter Management
```bash
# Get current wind parameter value
wind <plugin_name> <parameter_name>

# Set wind parameter value
wind <plugin_name> <parameter_name> <value>

# Examples:
wind SimpleRandomWind strength 2.5
wind ConfigurableWind avg_strength 10.0
wind ConfigurableWind avg_direction 180.0
```

### Base Wind Commands
```bash
# Set wind update rate (minimum affects performance)
wind base wind_update_rate <frames>

# Switch wind plugin (if multiple are available)
wind base wind_plugin <plugin_name>

# Examples:
wind base wind_update_rate 100
wind base wind_plugin ConfigurableWind
```

### Parameter Discovery
The module automatically registers console commands for all available parameters from each loaded plugin, making parameter discovery and management dynamic.

## Performance Features

### Update Optimization
- **Frame-based Updates**: Configurable update frequency prevents excessive calculations
- **Thread Safety**: Uses `Util.FireAndForget` for non-blocking wind calculations
- **Update Guards**: Prevents overlapping updates with `m_inUpdate` flag
- **Client Optimization**: Sends wind data only when changes occur

### Memory Management
- **Fixed Arrays**: Pre-allocated 16x16 wind speed arrays for efficiency
- **Plugin Caching**: Loaded plugins cached for lifetime of region
- **Minimal Allocations**: Reuses wind data arrays between updates

### Scalability
- **Per-Region**: Each region maintains independent wind simulation
- **Plugin Isolation**: Wind models are isolated and don't affect each other
- **Configurable Load**: Update rate can be adjusted based on server performance

## Client Integration

### Wind Data Transmission
```csharp
client.SendWindData(m_dataVersion, windSpeeds);
```
- **Version Tracking**: `m_dataVersion` incremented with each wind update
- **Selective Updates**: Clients receive updates only when wind changes
- **Efficient Protocol**: Uses Second Life-compatible wind data format

### Viewer Effects
- **Particle Systems**: Wind affects particle behavior in viewers
- **Environmental Immersion**: Provides atmospheric effects
- **Physics Integration**: Can influence lightweight physics objects
- **Visual Feedback**: Trees and vegetation may respond to wind in some viewers

## Plugin Development

### Creating Custom Wind Models
To create a new wind model plugin:

1. **Implement IWindModelPlugin Interface**
2. **Register in WindModule**: Add to the wind plugins array in WindModule.AddRegion()
3. **Provide Configuration**: Support WindConfig() for parameter setup
4. **Implement Update Logic**: Update wind patterns in WindUpdate()
5. **Generate Wind Data**: Provide wind speeds via WindSpeed() and WindLLClientArray()

### Plugin Registration (Post-Mono.Addins)
```csharp
// In WindModule.AddRegion()
IWindModelPlugin[] windPlugins = new IWindModelPlugin[]
{
    new SimpleRandomWind(),
    new ConfigurableWind(),
    new CustomWindModel()  // Add new plugins here
};
```

## Administrative Features

### Runtime Wind Control
- **Dynamic Plugin Switching**: Change wind models without restart
- **Parameter Adjustment**: Modify wind characteristics in real-time
- **Update Rate Control**: Adjust performance vs. realism balance
- **Status Monitoring**: Track active plugin and current parameters

### Debug and Monitoring
- **Console Feedback**: Immediate response to parameter changes
- **Plugin Discovery**: Automatic detection and registration of available plugins
- **Error Handling**: Graceful handling of plugin failures
- **Logging**: Comprehensive logging of wind system operations

## Environmental Simulation

### Realistic Wind Effects
- **Directional Consistency**: Plugins can maintain wind direction over time
- **Strength Variation**: Natural variation in wind intensity
- **Regional Patterns**: Support for region-specific wind characteristics
- **Temporal Changes**: Wind patterns can evolve over time

### Physics Integration
- **Object Interaction**: Wind affects physics-enabled objects
- **Particle Systems**: Influences particle behavior and trajectories
- **Atmospheric Effects**: Contributes to environmental realism
- **Immersive Experience**: Enhances the sense of a living, dynamic world

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Direct Plugin Instantiation**: Wind model plugins now directly instantiated
- **Configuration-based Loading**: Controlled via [Wind] enabled setting
- **Default Behavior**: Loaded by default as part of environmental simulation
- **Comprehensive Logging**: Debug and info logging for all operations

### Plugin System Changes
- **Direct Registration**: Plugins registered via direct instantiation array
- **No Extension Points**: Removed Mono.Addins TypeExtensionPoint dependency
- **Backward Compatibility**: All existing wind functionality preserved
- **Performance Improvement**: Reduced plugin loading overhead

### Dependencies
- **Core Framework**: OpenSim.Framework for basic functionality
- **Scene Management**: OpenSim.Region.Framework.Scenes for region integration
- **Wind Plugins**: OpenSim.Region.CoreModules.World.Wind.Plugins for wind models
- **Client Protocol**: Integration with viewer wind data transmission

## Security Considerations

### Parameter Validation
- **Range Checking**: Wind parameters validated for reasonable ranges
- **Type Safety**: Proper type conversion and error handling
- **Console Security**: Administrative commands require appropriate permissions
- **Safe Defaults**: Fallback to safe default values on configuration errors

### Performance Protection
- **Update Rate Limits**: Minimum update intervals prevent performance degradation
- **Thread Safety**: Non-blocking updates prevent server stalls
- **Error Isolation**: Plugin failures don't affect core wind system
- **Resource Management**: Proper cleanup of wind plugin resources

## Usage Examples

### Basic Wind Configuration
```ini
[Wind]
enabled = true
wind_update_rate = 120
wind_plugin = SimpleRandomWind
strength = 1.5
```

### Advanced Configurable Wind
```ini
[Wind]
enabled = true
wind_update_rate = 100
wind_plugin = ConfigurableWind
avg_strength = 8.0
avg_direction = 270.0
var_strength = 2.0
var_direction = 45.0
rate_change = 0.8
```

### Console Management
```bash
# Check current wind settings
wind SimpleRandomWind strength
wind ConfigurableWind avg_direction

# Adjust wind for storm simulation
wind ConfigurableWind avg_strength 15.0
wind ConfigurableWind var_strength 5.0

# Optimize performance
wind base wind_update_rate 200

# Switch to different wind model
wind base wind_plugin SimpleRandomWind
```

### Programmatic Access
```csharp
// Get wind module interface
IWindModule windModule = scene.RequestModuleInterface<IWindModule>();

// Get wind speed at specific location
Vector3 windSpeed = windModule.WindSpeed(128, 128, 25);

// Adjust wind parameters
windModule.WindParamSet("ConfigurableWind", "avg_strength", 10.0f);

// Get active plugin
string activePlugin = windModule.WindActiveModelPluginName;
```

This documentation reflects the WindModule implementation in `src/OpenSim.Region.CoreModules/World/Wind/WindModule.cs` and its integration with the factory-based module loading system.