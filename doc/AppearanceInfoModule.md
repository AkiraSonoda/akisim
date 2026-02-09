# AppearanceInfoModule

## Overview

The AppearanceInfoModule is a shared optional module that provides comprehensive console commands for debugging and inspecting avatar appearance and wearables in OpenSimulator. It enables server administrators to diagnose appearance issues, validate baked textures, force appearance updates, and analyze wearable assets through detailed console reporting.

## Architecture

- **Type**: `ISharedRegionModule` - instantiated once per OpenSim instance and shared across all regions
- **Namespace**: `OpenSim.Region.OptionalModules.Avatar.Appearance`
- **Location**: `src/OpenSim.Region.OptionalModules/Avatar/Appearance/AppearanceInfoModule.cs`

## Key Features

### Appearance Inspection Commands
- **`show appearance`** / **`appearance show`** - Display baked texture status for avatars
- **`appearance send`** - Force appearance data transmission to other viewers
- **`appearance rebake`** - Request texture rebaking from user's viewer
- **`appearance find`** - Locate avatars using specific texture UUIDs

### Wearables Analysis Commands
- **`wearables show`** - Display wearable information and statistics
- **`wearables check`** - Validate wearable assets and dependencies

### Cross-Region Support
- Commands operate across all regions managed by this simulator instance
- Comprehensive user discovery across multiple regions
- Thread-safe region management with `RwLockedList<Scene>`

## Console Commands Reference

### Appearance Commands

#### `appearance show [<first-name> <last-name>]`
**Purpose**: Display appearance and baked texture information for avatars.

**Usage**:
```bash
# Show status for all avatars
appearance show

# Show detailed info for specific avatar
appearance show John Doe
```

**Output**:
- **All avatars**: Shows baked texture validation status (OK/incomplete)
- **Specific avatar**: Detailed baked texture report with texture UUIDs and asset availability

#### `appearance send [<first-name> <last-name>]`
**Purpose**: Force transmission of appearance data to other viewers.

**Usage**:
```bash
# Send appearance for all avatars
appearance send

# Send appearance for specific avatar
appearance send Jane Smith
```

**Effect**: Triggers `AvatarFactory.SendAppearance()` to refresh appearance display for other users.

#### `appearance rebake <first-name> <last-name>`
**Purpose**: Request the user's viewer to rebake and reupload appearance textures.

**Usage**:
```bash
# Request rebake for specific user
appearance rebake Bob Jones
```

**Output**: Reports number of textures requested for rebaking or indicates if no texture IDs are available.

#### `appearance find <uuid-or-start-of-uuid>`
**Purpose**: Find which avatar(s) use a specific texture UUID as baked texture.

**Usage**:
```bash
# Find by partial UUID
appearance find 2008a8d

# Find by full UUID (dashed format)
appearance find 550e8400-e29b-41d4-a716-446655440000
```

**Output**: Lists all avatars currently using the specified texture UUID.

### Wearables Commands

#### `wearables show [<first-name> <last-name>]`
**Purpose**: Display wearable information and statistics.

**Usage**:
```bash
# Show wearable count summary for all avatars
wearables show

# Show detailed wearable info for specific avatar
wearables show Alice Cooper
```

**Output**:
- **All avatars**: Table showing avatar names and wearable counts
- **Specific avatar**: Detailed table with wearable types, item UUIDs, and asset UUIDs

#### `wearables check <first-name> <last-name>`
**Purpose**: Validate wearable assets and their dependencies.

**Usage**:
```bash
# Check all wearable assets for specific user
wearables check David Williams
```

**Output**: Comprehensive asset validation report showing:
- Wearable types and item UUIDs
- All referenced assets with existence verification
- Asset types and UUIDs with found/missing status

## Configuration

### Enabling/Disabling the Module
The module is controlled through the `[Modules]` configuration section:

```ini
[Modules]
; Enable AppearanceInfoModule (default: false)
AppearanceInfoModule = true
```

**Default Behavior**: The module is disabled by default (`false`) since it provides debugging commands primarily needed for development and troubleshooting.

### Use Cases for Enabling
- **Development Environments**: Essential for avatar appearance debugging
- **Troubleshooting**: Diagnosing appearance issues reported by users
- **Server Administration**: Monitoring avatar appearance health
- **Asset Management**: Identifying missing or corrupt appearance assets

## Module Lifecycle

### Initialization
1. **Initialise()** - Basic module setup with debug logging
2. **PostInitialise()** - Post-initialization logging
3. **AddRegion()** - Basic region tracking with logging
4. **RegionLoaded()** - Console command registration and scene tracking
5. **Close()** - Module cleanup with logging

### Scene Management
- Maintains thread-safe list of all managed scenes (`RwLockedList<Scene>`)
- Automatically tracks regions as they are added/loaded
- Console commands registered per region but operate across all regions

## Technical Implementation

### Data Structures
- **Scenes List**: `RwLockedList<Scene>` - Thread-safe collection of all managed regions
- **Console Tables**: Uses `ConsoleDisplayTable` and `ConsoleDisplayList` for formatted output
- **Asset Validation**: Integrates with `UuidGatherer` for comprehensive asset dependency analysis

### User Discovery Algorithm
1. Iterate through all managed regions
2. Search each region's scene for users by name
3. Filter out child agents (focus on root agents only)
4. Return comprehensive results across all regions

### Baked Texture Analysis
- Retrieves baked texture faces using `AvatarFactory.GetBakedTextureFaces()`
- Validates texture cache using `AvatarFactory.ValidateBakedTextureCache()`
- Checks asset existence through `AssetService.AssetsExist()`
- Supports partial UUID matching for texture identification

### Wearable Validation Process
1. **Asset Gathering**: Uses `UuidGatherer` to collect all referenced UUIDs
2. **Dependency Resolution**: Recursively gathers all asset dependencies
3. **Existence Verification**: Batch checks asset existence via `AssetService`
4. **Report Generation**: Creates detailed formatted reports with findings

## Error Handling

### Command Validation
- Validates parameter counts for all commands
- Provides usage syntax on invalid parameters
- Handles missing users gracefully with clear feedback

### Asset Validation
- Handles missing assets without crashing
- Reports asset existence status clearly
- Manages cases where avatars have no wearables

### Cross-Region Operations
- Continues processing even if some regions fail
- Thread-safe iteration over region collections
- Handles region addition/removal during command execution

## Logging

The module provides structured debug logging for administrative oversight:

### Debug Logging Events
- Module lifecycle (initialize, post-initialize, close)
- Region management (add, remove, load) with region names
- Console command registration per region
- All logging follows consistent module naming conventions

**Log Category**: `OpenSim.Region.OptionalModules.Avatar.Appearance.AppearanceInfoModule`

**Example Log Output**:
```
[DEBUG] AppearanceInfoModule initializing
[DEBUG] AppearanceInfoModule post-initialized
[DEBUG] AppearanceInfoModule adding region MainRegion
[DEBUG] AppearanceInfoModule region loaded MainRegion
[DEBUG] AppearanceInfoModule registering console commands for region MainRegion
[DEBUG] AppearanceInfoModule removing region MainRegion
```

## Performance Considerations

### Efficiency Features
- Thread-safe region management with minimal locking
- Batch asset existence checking for wearable validation
- Efficient user lookup with early termination
- Reusable console display formatting objects

### Scalability Factors
- Performance scales with number of regions and users per region
- Asset validation can be expensive for avatars with many wearables
- UUID gathering process may trigger multiple asset service calls
- Console output generation scales with result set size

### Resource Management
- Minimal memory footprint when not actively processing commands
- Temporary collections cleaned up after command completion
- No persistent caching (commands are stateless)

## Integration Points

### Service Dependencies
- **Scene.AvatarFactory** - Core appearance management functionality
- **Scene.AssetService** - Asset existence validation and retrieval
- **UuidGatherer** - Comprehensive asset dependency analysis
- **MainConsole** - Command registration and output formatting

### Avatar Factory Integration
```csharp
// Baked texture validation
bool isValid = scene.AvatarFactory.ValidateBakedTextureCache(sp);

// Force appearance update
scene.AvatarFactory.SendAppearance(sp.UUID);

// Request texture rebaking
int rebakes = scene.AvatarFactory.RequestRebake(sp, false);
```

## Factory Integration

The module is instantiated through `OptionalModulesFactory.CreateOptionalSharedModules()`:

```csharp
// Load AppearanceInfoModule if enabled for appearance debugging commands
if (modulesConfig.GetBoolean("AppearanceInfoModule", false))
{
    if (m_log.IsDebugEnabled) m_log.Debug("Loading AppearanceInfoModule for appearance debugging commands");
    yield return new AppearanceInfoModule();
}
else
{
    if (m_log.IsDebugEnabled) m_log.Debug("AppearanceInfoModule disabled - set AppearanceInfoModule = true in [Modules] to enable appearance debugging commands");
}
```

## Troubleshooting Use Cases

### Common Appearance Issues

#### "Cloud" Avatars
**Problem**: Avatars appear as clouds to other users
**Diagnosis**: Use `appearance show <name>` to check baked texture status
**Solution**: Use `appearance rebake <name>` to request texture rebaking

#### Missing Textures
**Problem**: Avatar textures appear as gray or default
**Diagnosis**: Use `appearance find <uuid>` to locate problematic textures
**Solution**: Check asset service and request rebaking

#### Wearable Problems
**Problem**: Clothing or attachments not displaying properly  
**Diagnosis**: Use `wearables check <name>` to validate all assets
**Solution**: Identify and replace missing wearable assets

#### Cross-Region Issues
**Problem**: Appearance problems when moving between regions
**Diagnosis**: Use `appearance show` across all regions to compare states
**Solution**: Use `appearance send <name>` to force appearance updates

### Administrative Workflows

#### New User Onboarding
1. Check initial appearance status with `appearance show`
2. Validate default wearables with `wearables check`
3. Monitor for appearance issues during first login

#### Server Maintenance
1. Run `wearables show` to identify users with excessive wearables
2. Use `appearance find` to locate heavily-used texture assets
3. Validate asset service health through wearable checking

## Security and Access Control

### Console-Only Access
- All commands require server console access (administrator level)
- No in-world or HTTP access to these commands
- Built-in access control through OpenSim console security

### Privacy Considerations
- Commands can inspect any avatar's appearance details
- Asset UUIDs are exposed through detailed reports  
- Should only be enabled in trusted administrative environments

## Maintenance Notes

### Module Dependencies
- No Mono.Addins dependencies (modernized architecture)
- Depends on OpenSim core avatar factory and asset services
- Uses ThreadedClasses for thread-safe collections
- Compatible with both Standalone and Grid deployment modes

### Future Enhancements
- Could add appearance comparison commands between users
- Might support appearance backup/restore functionality
- Could include automated appearance health monitoring
- Potential for appearance template/preset management

### Testing Considerations
- Test with various avatar configurations (default, custom, heavily-modified)
- Verify cross-region functionality with multiple regions
- Test asset validation with missing/corrupt assets
- Validate performance with large numbers of avatars

This module is essential for diagnosing and resolving avatar appearance issues in OpenSimulator environments, providing comprehensive visibility into the complex avatar appearance and asset systems.