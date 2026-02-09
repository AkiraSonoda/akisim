# EnvironmentModule Technical Documentation

## Overview

The **EnvironmentModule** is a core OpenSimulator module that manages environmental settings including Enhanced Environment Platform (EEP) and legacy Windlight system. It provides comprehensive day/night cycle management, sky and water settings, sun/moon positioning, and atmospheric effects. The module handles both region-wide and parcel-specific environment configurations, supporting modern EEP viewers as well as legacy Windlight compatibility.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **IEnvironmentModule**: Environment-specific functionality interface for external access

### Key Components
- **EEP Support**: Enhanced Environment Platform for modern viewers
- **Legacy Windlight**: Backward compatibility with older viewers
- **Day/Night Cycles**: Dynamic time-based environmental changes
- **Parcel Environments**: Per-parcel environment overrides
- **Asset Management**: Environment asset storage and retrieval
- **HTTP Capabilities**: REST API for environment manipulation

## Environment System Architecture

### Environment Types
The module supports multiple environment systems:

#### Enhanced Environment Platform (EEP)
- **Modern Standard**: Latest environment system for current viewers
- **Asset-based**: Environment settings stored as assets
- **Day Cycles**: Complex time-based transitions
- **Sky Settings**: Advanced atmospheric rendering parameters
- **Water Settings**: Realistic water rendering and reflections

#### Legacy Windlight
- **Backward Compatibility**: Support for older viewers
- **Direct Settings**: Simple atmospheric parameters
- **Migration Support**: Automatic conversion from Windlight to EEP
- **Generic Messages**: Uses Windlight protocol for legacy clients

### Environment Hierarchy
Environment settings follow a hierarchical priority system:

1. **Avatar-specific Environment**: Personal environment overrides (highest priority)
2. **Parcel Environment**: Land-specific environment settings
3. **Region Environment**: Default region-wide environment
4. **Default Environment**: Fallback default settings (lowest priority)

## Configuration System

### Module Enablement
```ini
[ClientStack.LindenCaps]
; Enable EnvironmentModule by setting capability to localhost
Cap_EnvironmentSettings = localhost
```

### Default Environment Assets
The module uses predefined default environment assets:
- **Default Day Cycle**: `5646d39e-d3d7-6aff-ed71-30fc87d64a92` (3:1 day-to-night ratio)
- **Default Sky**: `3ae23978-ac82-bcf3-a9cb-ba6e52dcb9ad`
- **Default Water**: `59d1a851-47e7-0e5f-1ed7-6b715154f41a`

### Estate Settings
```ini
; Allow parcel owners to override region environment
AllowEnvironmentOverride = true
```

## HTTP Capabilities and API

### Environment Settings Capability
**Endpoint**: `EnvironmentSettings`
- **GET**: Retrieve current environment for avatar's location
- **POST**: Set region-wide environment (requires estate permissions)

#### GET Response Format
Returns environment data in viewer-compatible format based on:
- Avatar's personal environment (if set)
- Current parcel environment (if enabled and available)
- Region environment (fallback)

#### POST Request Format
Accepts legacy Windlight arrays for backward compatibility:
```json
[
  {
    "messageID": "uuid",
    "regionID": "uuid",
    "waterColor": [r, g, b, a],
    "waterFogDensityExponent": float,
    // ... additional Windlight parameters
  }
]
```

### Extended Environment Capability
**Endpoint**: `ExtEnvironment`
- **GET**: Retrieve EEP environment for region or specific parcel
- **PUT/POST**: Set EEP environment with full asset support
- **DELETE**: Remove environment settings

#### Query Parameters
- `parcelid`: Target specific parcel (default: region-wide)
- `trackno`: Environment track number (currently unsupported)

#### EEP Environment Format
```json
{
  "environment": {
    "day_asset": "asset-uuid",
    "day_name": "Custom Day Cycle",
    "day_cycle": {
      // Complete day cycle definition
    }
  }
}
```

## Environment Data Management

### ViewerEnvironment Class
Central data structure for environment management:
```csharp
public class ViewerEnvironment
{
    public int version;           // Environment version for change tracking
    public int DayLength;         // Day cycle length in seconds
    public int DayOffset;         // Time offset for day cycle
    // Sky, water, and day cycle data
}
```

### Environment Storage
- **Region Storage**: Persisted in simulation data service
- **Parcel Storage**: Stored in land data
- **Asset Storage**: Environment assets in asset service
- **Version Tracking**: Incremental versioning for change detection

### Data Persistence
- **Region Environment**: Stored as LLSD notation in simulation database
- **Environment Migration**: Automatic conversion from legacy Windlight to EEP
- **Asset References**: Environment assets referenced by UUID

## Day/Night Cycle System

### Time Calculation
```csharp
public float GetDayFractionTime(ViewerEnvironment env)
{
    double dayfrac = env.DayLength;
    dayfrac = ((Util.UnixTimeSinceEpochSecs() + env.DayOffset) % dayfrac) / dayfrac;
    return (float)Utils.Clamp(dayfrac, 0, 1);
}
```

### Sun and Moon Positioning
The module provides comprehensive celestial positioning:

#### Regional Positioning
- `GetRegionSunDir(float altitude)`: Sun direction vector
- `GetRegionSunRot(float altitude)`: Sun rotation quaternion
- `GetRegionMoonDir(float altitude)`: Moon direction vector
- `GetRegionMoonRot(float altitude)`: Moon rotation quaternion

#### Position-specific Positioning
- `GetSunDir(Vector3 pos)`: Sun direction at specific location
- `GetMoonDir(Vector3 pos)`: Moon direction at specific location
- Supports parcel-specific environments

### Time Synchronization
- **Client Updates**: Regular time updates sent to viewers
- **Frame-based Updates**: Updates every 2.5 seconds (configurable)
- **Viewer Compatibility**: Different time formats for EEP vs legacy viewers

## Client Integration and Compatibility

### Viewer Capability Detection
```csharp
uint vflags = client.GetViewerCaps();
if ((vflags & 0x8000) != 0)      // EEP-capable viewer
if ((vflags & 0x4000) != 0)      // WindlightRefresh capable
// else: Legacy Windlight viewer
```

### Update Mechanisms

#### EEP Viewers (Modern)
- Uses `HandleRegionInfoRequest` for region environment
- Parcel environment via land updates
- Full EEP feature support

#### Windlight Refresh Viewers
- Uses `WindlightRefreshEvent` via event queue
- Supports basic environment changes
- Limited EEP feature compatibility

#### Legacy Viewers
- Uses `SendGenericMessage` with "Windlight" protocol
- Basic atmospheric parameters only
- No advanced EEP features

### Environment Refresh System
```csharp
public void WindlightRefresh(int interpolate, bool forRegion = true)
```
- **interpolate**: Transition time for environment changes
- **forRegion**: Whether to refresh region-wide or parcel-specific
- **Selective Updates**: Only active, non-NPC avatars receive updates

## Permission System

### Estate Permissions
- **Region Environment**: Requires estate command permissions
- **Permission Check**: `m_scene.Permissions.CanIssueEstateCommand(agentID, false)`
- **Estate Override**: Controls parcel environment capability

### Parcel Permissions
- **Parcel Environment**: Requires parcel edit permissions
- **Group Powers**: `GroupPowers.AllowEnvironment` for group-owned land
- **Permission Check**: `m_scene.Permissions.CanEditParcelProperties(...)`

### Environment Restrictions
- **Personal Environment**: Prevents region/parcel changes when active
- **Parcel Conflicts**: Region changes blocked if avatar on parcel with custom environment
- **Viewer Requirements**: Modern environment features require compatible viewers

## Asset Integration

### Environment Asset Types
- **Type 0**: Sky settings asset
- **Type 1**: Water settings asset
- **Type 2**: Day cycle asset

### Asset Operations
```csharp
public byte[] GetDefaultAssetData(int type)  // Generate default environment assets
public UUID GetDefaultAsset(int type)       // Get default asset UUIDs
```

### Asset Loading
- **Lazy Loading**: Assets loaded on demand
- **Caching**: Default environment cached for performance
- **Error Handling**: Graceful fallback to defaults on asset failures

## Legacy Windlight Support

### Windlight Data Conversion
```csharp
public void FromLightShare(RegionLightShareData ls)  // Import legacy Windlight
public RegionLightShareData ToLightShare()          // Export to legacy format
```

### Binary Windlight Protocol
- **249-byte Message**: Packed binary format for legacy viewers
- **Parameter Encoding**: Float and vector data serialization
- **Generic Message**: Uses "Windlight" message type

### Migration Support
- **Automatic Detection**: Identifies legacy Windlight settings
- **Transparent Conversion**: Converts to EEP format automatically
- **Data Preservation**: Maintains compatibility during migration

## Performance Optimization

### Update Throttling
- **Frame-based Updates**: Configurable update frequency (default: 2.5 seconds)
- **Change Detection**: Only update when environment actually changes
- **Selective Broadcasting**: Updates only sent to relevant avatars

### Memory Management
- **Static Defaults**: Shared default environment instances
- **Lazy Initialization**: Environment loaded only when needed
- **Reference Tracking**: Efficient environment object sharing

### Network Optimization
- **Capability-based**: Uses HTTP capabilities for bulk operations
- **Binary Encoding**: Efficient binary formats for legacy viewers
- **Compressed Data**: LLSD notation for efficient storage

## Administrative Features

### Environment Management
- **Region Reset**: `ResetEnvironmentSettings(UUID regionUUID)`
- **Force Refresh**: Manual environment refresh for troubleshooting
- **Version Tracking**: Incremental versioning for change management

### Debug and Monitoring
- **Comprehensive Logging**: Debug logging for environment operations
- **Error Handling**: Graceful handling of malformed environment data
- **Status Reporting**: Environment state information for administrators

### Estate Integration
- **Estate Module**: Integration with estate management
- **Region Info**: Environment data included in region information
- **Override Control**: Estate-level control of parcel environment overrides

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Capability-based Loading**: Enabled via ClientStack.LindenCaps configuration
- **Default Behavior**: Disabled by default, requires explicit capability configuration
- **Comprehensive Logging**: Debug and info logging for operations

### Configuration Requirements
The module requires specific capability configuration to function:
```ini
[ClientStack.LindenCaps]
Cap_EnvironmentSettings = localhost
```

### Dependencies
- **Estate Module**: Required for estate permission integration
- **Event Queue**: Required for viewer environment updates
- **Asset Service**: Required for environment asset management
- **Land Channel**: Required for parcel environment support

## Security Considerations

### Permission Validation
- **Estate Permissions**: Strict validation for region environment changes
- **Parcel Permissions**: Proper authorization for parcel environment settings
- **Avatar Validation**: Checks for valid, non-NPC avatars

### Data Validation
- **Asset Validation**: Validates environment asset format and content
- **Parameter Bounds**: Ensures reasonable environment parameter values
- **Error Isolation**: Malformed data doesn't affect other environments

### Network Security
- **Capability Protection**: Environment endpoints protected by capability system
- **Input Sanitization**: Proper parsing and validation of client input
- **Error Messages**: Secure error reporting without information disclosure

## Usage Examples

### Basic EEP Environment Setup
```ini
[ClientStack.LindenCaps]
Cap_EnvironmentSettings = localhost

[Estate]
AllowEnvironmentOverride = true
```

### Programmatic Environment Access
```csharp
// Get environment module interface
IEnvironmentModule envModule = scene.RequestModuleInterface<IEnvironmentModule>();

// Get current region day fraction (0.0 to 1.0)
float dayFraction = envModule.GetRegionDayFractionTime();

// Get sun direction for specific altitude
Vector3 sunDir = envModule.GetRegionSunDir(100.0f);

// Get environment at specific position (considers parcel overrides)
Vector3 position = new Vector3(128, 128, 25);
int dayLength = envModule.GetDayLength(position);
Vector3 moonDir = envModule.GetMoonDir(position);
```

### Environment Asset Creation
```csharp
// Create default sky asset data
byte[] skyData = envModule.GetDefaultAssetData(0);

// Create default water asset data
byte[] waterData = envModule.GetDefaultAssetData(1);

// Create default day cycle asset data
byte[] dayData = envModule.GetDefaultAssetData(2);
```

### Legacy Windlight Integration
```csharp
// Convert current environment to legacy Windlight format
RegionLightShareData windlightData = envModule.ToLightShare();

// Apply legacy Windlight settings to region
RegionLightShareData newSettings = new RegionLightShareData();
// ... configure windlight parameters
envModule.FromLightShare(newSettings);
```

This documentation reflects the EnvironmentModule implementation in `src/OpenSim.Region.CoreModules/World/LightShare/EnvironmentModule.cs` and its integration with the factory-based module loading system.