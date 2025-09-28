# PrimLimitsModule Technical Documentation

## Overview

The **PrimLimitsModule** is an optional non-shared region module that enables selective enforcement of parcel prim limits within OpenSimulator regions. This module provides fine-grained control over object creation and movement based on parcel capacity and user permissions.

## Purpose

The PrimLimitsModule serves as a comprehensive prim management system that:

- Enforces parcel prim limits during object rezzing operations
- Controls object entry and movement between parcels
- Implements per-user prim quotas when configured
- Provides administrative exemptions for estate owners and managers
- Prevents parcel overcrowding and resource abuse

## Configuration

### Enabling the Module

To enable PrimLimitsModule, add it to the `permissionmodules` configuration setting in your OpenSim.ini file:

```ini
[Startup]
permissionmodules = "DefaultPermissionsModule,PrimLimitsModule"
```

Or in the [Permissions] section:

```ini
[Permissions]
permissionmodules = "DefaultPermissionsModule,PrimLimitsModule"
```

The module will only load if explicitly specified in the permissionmodules configuration.

### Per-User Prim Limits

Configure per-user prim limits in the [Startup] section:

```ini
[Startup]
MaxPrimsPerUser = 500
```

- Set to `-1` to disable per-user limits
- Set to `0` or positive number to enforce per-user quotas
- Applies to non-parcel owners, non-estate owners, and non-estate managers

## Technical Architecture

### Interface Implementation

The PrimLimitsModule implements the `INonSharedRegionModule` interface, making it a region-specific module that is instantiated once per region.

### Permission System Integration

The module integrates with OpenSimulator's permission system by hooking into the following permission events:

- `OnRezObject` - Controls object creation from inventory
- `OnObjectEntry` - Controls object movement between locations
- `OnObjectEnterWithScripts` - Controls scripted object movement
- `OnDuplicateObject` - Controls object duplication operations

### Module Lifecycle

1. **Initialization** - Reads configuration and determines if module should be active
2. **Region Addition** - Registers permission event handlers when added to a region
3. **Region Loading** - Obtains reference to dialog module for user notifications
4. **Region Removal** - Unregisters event handlers when removed from a region

## Functionality

### Prim Limit Enforcement

The module enforces limits through a comprehensive checking system:

#### Parcel Capacity Limits

- Checks if adding objects would exceed the parcel's total prim capacity
- Uses `ILandObject.GetSimulatorMaxPrimCount()` to determine parcel limits
- Compares current usage (`PrimCounts.Simulator`) against capacity

#### Per-User Quotas

When `MaxPrimsPerUser` is configured (≥ 0), the module enforces individual user limits:

- **Exempt Users**: Parcel owners, estate owners, estate managers
- **Limited Users**: All other users are subject to the per-user quota
- **Quota Calculation**: Uses `PrimCounts.Users[ownerID]` to track per-user usage

### Object Operations

#### Object Rezzing (`CanRezObject`)

Triggered when users rez objects from inventory:

- Validates parcel capacity constraints
- Enforces per-user quotas for non-exempt users
- Provides user feedback through dialog alerts
- Returns `true` to allow, `false` to deny

#### Object Movement (`CanObjectEnter`)

Triggered when objects move between locations:

- Only enforces limits when crossing parcel boundaries
- Ignores movement within the same parcel
- Handles both manual movement and automated teleportation
- Supports objects entering from other regions

#### Scripted Object Movement (`CanObjectEnterWithScripts`)

Specialized handling for script-driven object movement:

- Applies same limits as manual movement
- Silent failure mode (no user notifications)
- Used by scripts like llSetPos, llSetRegionPos

#### Object Duplication (`CanDuplicateObject`)

Controls object copying operations:

- Enforces same limits as object rezzing
- Considers the full prim count of duplicated objects
- Provides immediate user feedback on failures

### User Feedback System

The module provides clear user notifications through the dialog system:

- **Parcel Full**: "Unable to rez object because the parcel is full"
- **User Quota Exceeded**: "Unable to rez object because you have reached your limit"
- Notifications sent via `IDialogModule.SendAlertToUser()`

## Implementation Details

### Permission Hierarchy

The module implements a three-tier permission hierarchy:

1. **Estate Owners** - Unlimited access to all parcels
2. **Estate Managers** - Unlimited access to all parcels
3. **Parcel Owners** - Unlimited access to owned parcels only
4. **Regular Users** - Subject to per-user quotas (if configured)

### Edge Cases Handled

- **Temporary Prims**: Framework exists for special handling (TODO implementation)
- **Region Boundaries**: Proper handling of cross-region object movement
- **Group-Owned Parcels**: Non-owners are subject to user quotas
- **Missing Dialog Module**: Graceful degradation when dialog system unavailable

### Performance Considerations

- **Lazy Loading**: Dialog module reference obtained during RegionLoaded phase
- **Efficient Lookups**: Direct access to parcel data via land channel
- **Minimal Overhead**: Only processes boundary-crossing movements
- **Event-Driven**: No polling or background processing required

## Configuration Examples

### Basic Prim Limits Only

```ini
[Startup]
permissionmodules = "DefaultPermissionsModule,PrimLimitsModule"
MaxPrimsPerUser = -1  # Disable per-user limits
```

### Comprehensive Limits

```ini
[Startup]
permissionmodules = "DefaultPermissionsModule,PrimLimitsModule"
MaxPrimsPerUser = 200  # 200 prims per user

[Permissions]
# Other permission settings...
```

### Development/Testing Environment

```ini
[Startup]
permissionmodules = "DefaultPermissionsModule"
# PrimLimitsModule disabled for unrestricted building
```

## Troubleshooting

### Module Not Loading

- Verify `permissionmodules` includes "PrimLimitsModule"
- Check that OpenSim.Region.OptionalModules.dll is present
- Review startup logs for loading errors

### Unexpected Behavior

- Confirm MaxPrimsPerUser setting matches intended policy
- Verify parcel settings and ownership configuration
- Test with estate owner/manager accounts for expected exemptions

### Performance Issues

- Monitor for excessive object movement across parcel boundaries
- Consider parcel layout optimization to minimize boundary crossings
- Review per-user quotas to prevent resource concentration

## Integration Notes

### Factory Loading System

The PrimLimitsModule is integrated with the CoreModuleFactory system:

- Loaded via reflection to avoid circular dependencies
- Configuration-driven activation based on permissionmodules setting
- Comprehensive logging for debugging and monitoring
- Graceful fallback when module unavailable

### Compatibility

- Compatible with all OpenSimulator grid configurations
- Works with both standalone and grid setups
- Supports hypergrid-enabled regions
- No conflicts with other permission modules

## Security Considerations

- Prevents resource exhaustion through prim limits
- Maintains parcel ownership boundaries
- Respects estate management hierarchy
- Provides audit trail through comprehensive logging

## Future Enhancements

Potential areas for module enhancement:

- **Temporary Prim Support**: Special handling for temporary objects
- **Group Quota Systems**: Per-group prim allocations
- **Dynamic Limits**: Time-based or event-driven limit adjustments
- **Metrics Integration**: Enhanced monitoring and reporting
- **API Extensions**: RESTful management interfaces

---

*This documentation covers PrimLimitsModule version as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full functionality.*
