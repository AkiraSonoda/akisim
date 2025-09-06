# PhysicsParameters Module Documentation

## Overview

The PhysicsParameters module is an optional administrative module that provides **runtime physics parameter management** through console commands. It enables administrators to dynamically adjust physics engine settings without restarting the simulator, making it invaluable for debugging, performance tuning, and physics experimentation.

## Purpose

**Primary Functions:**
- **Runtime Parameter Access** - Get and set physics engine parameters while the simulator is running
- **Physics Debugging** - Adjust parameters to isolate and diagnose physics-related issues
- **Performance Tuning** - Optimize physics behavior for specific regions or scenarios
- **Experimentation** - Test different physics settings without service interruption

## Architecture

### Module Structure

The PhysicsParameters module implements the `ISharedRegionModule` interface and integrates with the OpenSimulator console system to provide physics parameter management.

```
PhysicsParameters (ISharedRegionModule)
├── Console Command Registration
│   ├── physics get [<param>|ALL]
│   ├── physics set <param> <value> [localID|ALL]
│   └── physics list
├── Parameter Management
│   ├── Get Parameter Values
│   ├── Set Parameter Values
│   └── List Available Parameters
└── Physics Engine Integration
    └── IPhysicsParameters Interface
```

### Dependencies

- **IPhysicsParameters Interface** - Physics engines must implement this to expose tunable parameters
- **Console Command System** - Uses OpenSim's MainConsole for command registration
- **Scene Management** - Operates on currently selected region via SceneManager

## Configuration

### Enabling the Module

Add to the `[Modules]` section in OpenSim.ini:

```ini
[Modules]
PhysicsParametersModule = true
```

### Physics Engine Compatibility

The module's effectiveness depends on physics engine support:

| Physics Engine | Support Level | Available Parameters |
|----------------|---------------|---------------------|
| **BulletSim** | ✅ Full Support | Gravity, damping, collision margins, solver iterations, etc. |
| **ubODE** | ❌ No Support | Configuration file only |
| **POS** | ❌ No Support | Minimal parameter exposure |

## Console Commands

### Command Syntax

#### Get Parameters
```bash
# Get a specific physics parameter
physics get <parameter_name>

# Get all available physics parameters
physics get ALL
```

#### Set Parameters
```bash
# Set parameter for all objects
physics set <parameter_name> <value> ALL

# Set parameter for specific object (by localID)
physics set <parameter_name> <value> <localID>

# Set parameter with boolean values
physics set <parameter_name> TRUE
physics set <parameter_name> FALSE
```

#### List Parameters
```bash
# List all available physics parameters with descriptions
physics list
```

### Command Examples

#### Getting Parameter Values
```bash
Region (MyRegion) # physics get Gravity
MyRegion/Gravity = -9.8

Region (MyRegion) # physics get ALL
MyRegion/Gravity = -9.8
MyRegion/LinearDamping = 0.0
MyRegion/AngularDamping = 0.0
MyRegion/ContactProcessingThreshold = 0.1
```

#### Setting Parameter Values
```bash
# Adjust world gravity
Region (MyRegion) # physics set Gravity -12.0 ALL

# Set linear damping for specific object
Region (MyRegion) # physics set LinearDamping 0.5 12345

# Enable/disable collision margins
Region (MyRegion) # physics set UseCollisionMargin TRUE ALL
```

#### Listing Available Parameters
```bash
Region (MyRegion) # physics list
Available physics parameters:
- Gravity: World gravity setting in m/s²
- LinearDamping: Global linear motion damping factor
- AngularDamping: Global angular motion damping factor
- ContactProcessingThreshold: Contact processing optimization threshold
- CollisionMargin: Collision detection margin size
- MaxSubSteps: Maximum physics sub-steps per frame
```

## BulletSim Physics Parameters

### Core World Parameters

**Gravity Settings:**
- `Gravity` - World gravity magnitude (default: -9.8)
- `TerrainGravity` - Gravity specific to terrain interaction

**Damping Parameters:**
- `LinearDamping` - Reduces linear motion over time (0.0-1.0)
- `AngularDamping` - Reduces rotational motion over time (0.0-1.0)
- `DeactivationTime` - Time before objects auto-sleep (seconds)
- `LinearSleepingThreshold` - Linear velocity threshold for sleeping
- `AngularSleepingThreshold` - Angular velocity threshold for sleeping

### Collision Detection Parameters

**Contact Processing:**
- `ContactProcessingThreshold` - Optimization threshold for contact processing
- `ContactBreakingThreshold` - Distance at which contacts are broken
- `CollisionMargin` - Safety margin for collision detection
- `MaxCollisionsPerFrame` - Maximum collisions processed per frame

**Solver Parameters:**
- `SolverIterations` - Number of constraint solver iterations
- `MaxSubSteps` - Maximum physics sub-steps per simulation step
- `FixedTimeStep` - Fixed timestep for deterministic simulation
- `MaxUpdatesPerFrame` - Maximum physics updates per render frame

### Vehicle and Movement Parameters

**Vehicle Physics:**
- `VehicleLinearFactor` - Linear movement scaling for vehicles
- `VehicleAngularFactor` - Angular movement scaling for vehicles
- `VehicleLinearDeflection` - Vehicle linear deflection characteristics
- `VehicleAngularDeflection` - Vehicle angular deflection characteristics

**Character Movement:**
- `AvatarStepHeight` - Maximum step height for avatar movement
- `AvatarStepUpCorrectionFactor` - Step-up correction strength

## Usage Scenarios

### Physics Debugging

**Problem**: Objects falling through terrain
```bash
# Check collision margins
physics get CollisionMargin

# Increase collision margin
physics set CollisionMargin 0.1 ALL

# Check contact processing
physics get ContactProcessingThreshold
```

**Problem**: Objects moving too slowly
```bash
# Check damping settings
physics get LinearDamping
physics get AngularDamping

# Reduce damping
physics set LinearDamping 0.0 ALL
physics set AngularDamping 0.0 ALL
```

### Performance Tuning

**High Physics Load:**
```bash
# Reduce solver precision for performance
physics set SolverIterations 8 ALL

# Limit sub-steps
physics set MaxSubSteps 3 ALL

# Increase sleep thresholds
physics set LinearSleepingThreshold 0.8 ALL
physics set AngularSleepingThreshold 1.0 ALL
```

**Precision Requirements:**
```bash
# Increase solver precision for accuracy
physics set SolverIterations 20 ALL

# Reduce timestep for smoother simulation
physics set FixedTimeStep 0.01666 ALL  # 60 FPS physics
```

### Vehicle Tuning

**Vehicle Handling:**
```bash
# Adjust vehicle responsiveness
physics set VehicleLinearFactor 1.0 12345
physics set VehicleAngularFactor 1.0 12345

# Fine-tune deflection behavior
physics set VehicleLinearDeflection 0.05 12345
```

## Implementation Details

### Parameter Types and Values

**Numeric Parameters:**
- Float values for continuous parameters (gravity, damping, etc.)
- Integer values for iteration counts and limits
- Boolean values converted to 1.0 (true) or 0.0 (false)

**Target Specification:**
- `ALL` - Apply to all objects in the region
- `<localID>` - Apply to specific object by local ID
- No target - Apply globally to physics engine

**Special Constants:**
```csharp
// Apply-to flags
PhysParameterEntry.APPLY_TO_ALL = 0xfffffff3
PhysParameterEntry.APPLY_TO_NONE = 0xfffffff4

// Boolean conversion values
PhysParameterEntry.NUMERIC_TRUE = 1.0f
PhysParameterEntry.NUMERIC_FALSE = 0.0f
```

### Error Handling

**Common Error Messages:**
- `"Region '{0}' physics engine has no gettable physics parameters"` - Physics engine doesn't support IPhysicsParameters
- `"Failed fetch of parameter '{0}' from region '{1}'"` - Parameter doesn't exist or cannot be retrieved
- `"Parameter count error. Invocation: {command}"` - Incorrect command syntax
- `"Error: no region selected. Use 'change region' to select a region."` - No active region context

## Integration with Physics Engines

### IPhysicsParameters Interface

Physics engines implement this interface to expose runtime parameters:

```csharp
public interface IPhysicsParameters
{
    // Get list of available parameters
    PhysParameterEntry[] GetParameterList();
    
    // Set parameter value
    bool SetPhysicsParameter(string parm, string value, uint localID);
    
    // Get parameter value
    bool GetPhysicsParameter(string parm, out string value);
}
```

### Parameter Entry Structure

```csharp
public struct PhysParameterEntry
{
    public string name;  // Parameter name
    public string desc;  // Human-readable description
}
```

## Best Practices

### Parameter Adjustment Guidelines

1. **Make Incremental Changes** - Adjust parameters gradually to understand their impact
2. **Document Changes** - Keep track of parameter modifications for reproducibility
3. **Test Impact** - Observe physics behavior after each change
4. **Monitor Performance** - Watch for performance implications of parameter changes

### Safety Considerations

**Parameter Ranges:**
- Always stay within reasonable parameter ranges to avoid simulation instability
- Test parameter changes in development environments first
- Be cautious with solver iterations and sub-step limits

**Backup Settings:**
- Record original parameter values before making changes
- Use `physics get ALL` to capture current configuration
- Have a rollback plan for critical production environments

### Performance Monitoring

**Key Metrics to Watch:**
- Physics simulation time per frame
- Number of active physics objects
- Collision detection performance
- Memory usage of physics simulation

## Troubleshooting

### Module Not Loading

**Problem**: PhysicsParametersModule not available
- **Solution**: Ensure `PhysicsParametersModule = true` in `[Modules]` section
- **Check**: Verify OpenSim.Region.OptionalModules.dll is available

### No Parameters Available

**Problem**: `physics list` shows no parameters
- **Cause**: Physics engine doesn't implement IPhysicsParameters
- **Solution**: Switch to BulletSim physics engine
- **Alternative**: Configure physics parameters via configuration files

### Commands Not Working

**Problem**: Physics commands not recognized
- **Cause**: Module not loaded or region not selected
- **Solution**: Use `change region <region>` to select target region
- **Check**: Verify module loaded successfully in startup logs

## Technical Specifications

### System Requirements
- **Physics Engine**: BulletSim (recommended for full functionality)
- **Memory**: Minimal overhead - parameters stored in physics engine
- **CPU**: Negligible impact on physics simulation performance
- **Dependencies**: IPhysicsParameters interface implementation

### Limitations
- **Physics Engine Dependent** - Functionality limited by physics engine capabilities
- **Region Scope** - Parameters apply per-region, not globally
- **No Persistence** - Parameter changes lost on restart unless saved to configuration
- **Console Only** - No programmatic API for scripts or external tools

## Future Enhancements

### Potential Improvements
- **Configuration Persistence** - Save parameter changes to configuration files
- **Parameter Profiles** - Predefined parameter sets for different scenarios
- **Batch Operations** - Apply multiple parameter changes atomically
- **Performance Metrics** - Built-in performance impact measurement
- **Web Interface** - HTTP-based parameter management for remote administration

### Compatibility Considerations
- **ubODE Integration** - Potential future support for ubODE parameter tuning
- **Cross-Platform** - Ensure parameter behavior consistency across platforms
- **Version Migration** - Handle parameter name/format changes between versions

The PhysicsParameters module provides essential tools for advanced physics management in OpenSimulator environments, enabling administrators to fine-tune physics behavior for optimal performance and realism.