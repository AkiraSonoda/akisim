# ExtendedPhysics Module (BulletSim)

## Overview

The `ExtendedPhysics` module provides advanced physics scripting capabilities for the BulletSim physics engine. It exposes sophisticated physics features through LSL (Linden Scripting Language) functions, allowing scripts to control complex physics behaviors like linkset types, joint constraints, axis locking, and advanced physics parameters.

## Location

- **File**: `src/OpenSim.Region.PhysicsModule.BulletS/ExtendedPhysics.cs`
- **Namespace**: `OpenSim.Region.PhysicsModule.BulletS`
- **Interface**: `INonSharedRegionModule`

## Functionality

### Core Purpose

This module bridges the gap between LSL scripting and advanced BulletSim physics features that are not available in standard OpenSim physics implementations. It provides script-accessible functions for:

- Advanced linkset physics control
- Constraint and joint management
- Axis locking and limiting
- Physics deactivation control
- Physics engine introspection

### Configuration

The module is controlled by the `[ExtendedPhysics]` configuration section:

```ini
[ExtendedPhysics]
Enabled = true
```

### Prerequisites

- **Physics Engine**: Requires BulletSim physics engine
- **Script Module**: Requires `IScriptModuleComms` interface
- **Scene Integration**: Must be loaded after physics and script modules

## Exposed LSL Functions

### Engine Information

#### `string physGetEngineType()`
Returns the name of the currently active physics engine.

**Returns**: `"BulletSim"` when BulletSim is active, empty string otherwise

### Deactivation Control

#### `integer physDisableDeactivation(integer disable)`
Controls whether physics objects go to sleep when idle.

**Parameters**:
- `disable`: 1 to disable deactivation, 0 to enable

**Returns**: 0 on success, -1 on error

### Axis Locking and Limiting

#### `integer physAxisLock(list parameters)`
Locks or limits movement along specific axes.

**Axis Constants**:
- `PHYS_AXIS_LOCK_LINEAR`: Lock all linear movement
- `PHYS_AXIS_LOCK_LINEAR_X/Y/Z`: Lock specific linear axes
- `PHYS_AXIS_LIMIT_LINEAR_X/Y/Z`: Set limits on linear axes
- `PHYS_AXIS_LOCK_ANGULAR`: Lock all rotational movement
- `PHYS_AXIS_LOCK_ANGULAR_X/Y/Z`: Lock specific rotational axes
- `PHYS_AXIS_LIMIT_ANGULAR_X/Y/Z`: Set limits on rotational axes
- `PHYS_AXIS_UNLOCK_*`: Unlock corresponding axes
- `PHYS_AXIS_UNLOCK`: Unlock all axes

**Usage Example**:
```lsl
// Lock movement along X axis
physAxisLock([PHYS_AXIS_LOCK_LINEAR_X]);

// Set limits on Y rotation (-45 to +45 degrees)
physAxisLock([PHYS_AXIS_LIMIT_ANGULAR_Y, -PI/4, PI/4]);
```

### Linkset Type Management

#### `integer physSetLinksetType(integer linksetType)`
Changes the physics linkset type for complex object behaviors.

**Linkset Types**:
- `PHYS_LINKSET_TYPE_CONSTRAINT`: Uses constraints between parts
- `PHYS_LINKSET_TYPE_COMPOUND`: Treats linkset as single compound object
- `PHYS_LINKSET_TYPE_MANUAL`: Manual physics management

**Special Handling**: For physical linksets, automatically toggles physics off/on during type changes to ensure proper state synchronization.

#### `integer physGetLinksetType()`
Returns the current linkset type.

**Returns**: Current linkset type constant or -1 on error

### Joint and Constraint Control

#### `integer physChangeLinkType(integer linkNum, integer typeCode)`
Changes the constraint type between root and specified link.

**Joint Types**:
- `PHYS_LINK_TYPE_FIXED`: Rigid connection (default)
- `PHYS_LINK_TYPE_HINGE`: Hinge joint (door/gate behavior)
- `PHYS_LINK_TYPE_SPRING`: Spring constraint with damping
- `PHYS_LINK_TYPE_6DOF`: Six degrees of freedom constraint
- `PHYS_LINK_TYPE_SLIDER`: Sliding joint (piston behavior)

#### `integer physGetLinkType(integer linkNum)`
Returns the current joint type for specified link.

#### `integer physChangeLinkFixed(integer linkNum)`
Convenience function to set link to fixed joint type.

### Advanced Joint Parameters

#### `integer physChangeLinkParams(integer linkNum, list parameters)`
Sets detailed parameters for joints and constraints.

**Parameter Constants**:

**Frame Reference**:
- `PHYS_PARAM_FRAMEINA_LOC/ROT`: Frame A location/rotation
- `PHYS_PARAM_FRAMEINB_LOC/ROT`: Frame B location/rotation
- `PHYS_PARAM_USE_FRAME_OFFSET`: Enable frame offset usage
- `PHYS_PARAM_USE_LINEAR_FRAMEA`: Use linear frame A reference

**Movement Limits**:
- `PHYS_PARAM_LINEAR_LIMIT_LOW/HIGH`: Linear movement limits
- `PHYS_PARAM_ANGULAR_LIMIT_LOW/HIGH`: Rotational limits

**Motors**:
- `PHYS_PARAM_ENABLE_TRANSMOTOR`: Enable translation motor
- `PHYS_PARAM_TRANSMOTOR_MAXVEL`: Motor maximum velocity
- `PHYS_PARAM_TRANSMOTOR_MAXFORCE`: Motor maximum force

**Spring Physics**:
- `PHYS_PARAM_SPRING_AXIS_ENABLE`: Enable spring on axis
- `PHYS_PARAM_SPRING_DAMPING`: Spring damping factor
- `PHYS_PARAM_SPRING_STIFFNESS`: Spring stiffness coefficient
- `PHYS_PARAM_SPRING_EQUILIBRIUM_POINT`: Spring rest position

**Solver Parameters**:
- `PHYS_PARAM_CFM`: Constraint Force Mixing
- `PHYS_PARAM_ERP`: Error Reduction Parameter
- `PHYS_PARAM_SOLVER_ITERATIONS`: Solver iteration count

**Axis Specification**:
- `PHYS_AXIS_ALL`: Apply to all axes
- `PHYS_AXIS_LINEAR_ALL`: All linear axes
- `PHYS_AXIS_ANGULAR_ALL`: All angular axes
- `PHYS_AXIS_LINEAR_X/Y/Z`: Specific linear axes (0,1,2)
- `PHYS_AXIS_ANGULAR_X/Y/Z`: Specific angular axes (3,4,5)

**Usage Example**:
```lsl
// Create a spring joint with damping
physChangeLinkType(2, PHYS_LINK_TYPE_SPRING);
physChangeLinkParams(2, [
    PHYS_PARAM_SPRING_STIFFNESS, 10.0,
    PHYS_PARAM_SPRING_DAMPING, 0.5,
    PHYS_PARAM_LINEAR_LIMIT_LOW, <-1,0,0>,
    PHYS_PARAM_LINEAR_LIMIT_HIGH, <1,0,0>
]);
```

## Technical Implementation

### Module Architecture

#### Registration System
- Integrates with `IScriptModuleComms` for LSL function exposure
- Uses `[ScriptInvocation]` attributes for function registration
- Uses `[ScriptConstant]` attributes for constant registration

#### Physics Actor Interface
- Communicates with BulletSim through `PhysicsActor.Extension()` calls
- Maps LSL functions to internal BulletSim function identifiers
- Handles parameter marshalling between LSL and physics engine

#### Object Resolution
- Resolves script host objects to physics actors
- Handles linkset hierarchy navigation
- Manages root/child relationships for joint operations

### Error Handling

#### Return Values
- **Success**: Returns 0 or positive values
- **Failure**: Returns -1 
- **Validation**: Checks for null objects, deleted linksets, missing physics actors

#### Safety Measures
- Validates linkset states before operations
- Handles non-physical linksets appropriately
- Provides comprehensive error logging

### State Management

#### Physical Linkset Handling
For physical linksets changing type:
1. Set linkset to non-physical
2. Wait for physics state synchronization (150ms)
3. Update positions to sync simulator/physics states
4. Apply linkset type change
5. Wait for stabilization (150ms)
6. Restore physical state

#### Thread Safety
- Thread-safe physics actor access
- Proper synchronization with physics simulation thread
- Safe handling of concurrent script operations

## Integration Requirements

### Scene Dependencies
- Must be loaded after physics module initialization
- Requires active BulletSim physics scene
- Needs functional script module communications

### Physics Engine Coupling
- Tightly coupled to BulletSim implementation
- Uses BulletSim-specific extension functions
- Requires specific function identifiers matching BSScene

### Script System Integration
- Integrates with OpenSim scripting system
- Supports both YEngine and older script engines
- Provides region-specific function availability

## Performance Considerations

### Optimization Features
- Direct physics actor communication (no intermediate layers)
- Minimal parameter copying and marshalling
- Efficient object lookup and caching

### Usage Guidelines
- Avoid frequent linkset type changes on physical objects
- Use appropriate joint types for specific behaviors
- Consider performance impact of complex constraint setups

## Debugging and Monitoring

### Logging System
- Comprehensive debug logging with `[EXTENDED PHYSICS]` prefix
- Detailed parameter validation logging
- Error condition reporting with context information

### Common Issues
- Missing physics actors (objects not properly initialized)
- Deleted linksets during operations
- Invalid link numbers or parameter ranges
- Physics engine state mismatches

## Security Considerations

### Validation
- Validates all input parameters before physics operations
- Checks object ownership and permissions through scene system
- Prevents operations on deleted or invalid objects

### Resource Management
- Limits scope to objects owned by script host
- Prevents unauthorized physics manipulation
- Respects normal OpenSim security boundaries

## Usage Examples

### Creating a Vehicle Wheel Joint
```lsl
// Set linkset to use constraints
physSetLinksetType(PHYS_LINKSET_TYPE_CONSTRAINT);

// Create hinge joint for wheel
physChangeLinkType(2, PHYS_LINK_TYPE_HINGE);
physChangeLinkParams(2, [
    PHYS_PARAM_ANGULAR_LIMIT_LOW, <0, -PI, 0>,
    PHYS_PARAM_ANGULAR_LIMIT_HIGH, <0, PI, 0>,
    PHYS_PARAM_ENABLE_TRANSMOTOR, TRUE,
    PHYS_PARAM_TRANSMOTOR_MAXVEL, 10.0
]);
```

### Creating a Spring Door
```lsl
// Spring-loaded door that returns to closed position
physChangeLinkType(1, PHYS_LINK_TYPE_SPRING);
physChangeLinkParams(1, [
    PHYS_PARAM_SPRING_STIFFNESS, 50.0,
    PHYS_PARAM_SPRING_DAMPING, 5.0,
    PHYS_PARAM_SPRING_EQUILIBRIUM_POINT, 0.0,
    PHYS_PARAM_ANGULAR_LIMIT_LOW, <0, 0, -PI/2>,
    PHYS_PARAM_ANGULAR_LIMIT_HIGH, <0, 0, PI/2>
]);
```