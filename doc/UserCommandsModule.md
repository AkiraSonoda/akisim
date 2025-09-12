# UserCommandsModule

## Overview

The UserCommandsModule is a shared optional module that provides console commands for administrator user management in OpenSimulator. It enables server administrators to perform user operations such as teleporting users between locations and regions through the server console interface.

## Architecture

- **Type**: `ISharedRegionModule` - instantiated once per OpenSim instance and shared across all regions
- **Namespace**: `OpenSim.Region.OptionalModules.Avatar.Commands`
- **Location**: `src/OpenSim.Region.OptionalModules/Avatar/Commands/UserCommandsModule.cs`

## Key Features

### User Teleportation Command
- **Command**: `teleport user <first-name> <last-name> <destination>`
- **Functionality**: Teleports any user present in the simulator to specified coordinates
- **Scope**: Cross-region teleportation support (can teleport to different regions)
- **Access**: Server console only (administrator access)

### Destination Format Support
The module supports flexible destination formats:
- **Within Region**: `20/30/40` (x/y/z coordinates in current region)
- **Inter-Region**: `regionone/20/30/40` (region name with coordinates)
- **Quoted Regions**: `"region one/20/30/40"` (for region names containing spaces)

### User Discovery
- Searches across all regions managed by this simulator instance
- Locates users by first and last name
- Filters out child agents (only operates on root agents)

## Configuration

### Enabling/Disabling the Module
The module is controlled through the `[Modules]` configuration section:

```ini
[Modules]
; Enable UserCommandsModule (default: false)
UserCommandsModule = true
```

**Default Behavior**: The module is disabled by default (`false`) since it provides administrative commands that should be explicitly enabled by administrators.

### Security Considerations
- Module provides powerful administrative capabilities
- Should only be enabled in trusted environments
- Console access already requires server-level permissions
- No additional authentication within the module (relies on console security)

## Module Lifecycle

### Initialization
1. **Initialise()** - Basic module setup with debug logging
2. **PostInitialise()** - Post-initialization logging
3. **AddRegion()** - Registers console commands and adds region to tracking
4. **RegionLoaded()** - Region loaded confirmation logging
5. **Close()** - Module cleanup with logging

### Region Management
- Maintains a thread-safe dictionary of all regions (`RwLockedDictionary<UUID, Scene>`)
- Tracks regions by RegionID for efficient lookup
- Automatically adds/removes regions as they come online/offline

## Technical Implementation

### Data Structures
- **Scenes Dictionary**: `RwLockedDictionary<UUID, Scene>` - Thread-safe collection of all managed regions
- **Regex Patterns**: Pre-compiled regular expressions for destination parsing
  - `InterRegionDestinationRegex`: Matches `regionname/x/y/z` format
  - `WithinRegionDestinationRegex`: Matches `x/y/z` format

### Console Command Registration
```csharp
scene.AddCommand(
    "Users",                    // Command category
    this,                      // Module instance
    "teleport user",           // Command name
    TeleportUserCommandSyntax, // Syntax string
    "Description...",          // Help text
    HandleTeleportUser);       // Command handler
```

### User Location Algorithm
1. Iterate through all managed regions
2. Search each region's scene for user by name
3. Verify user is a root agent (not child agent)
4. Return first matching root agent found

### Destination Parsing Logic
1. **Step 1**: Attempt to parse as within-region format (`x/y/z`)
2. **Step 2**: If that fails, parse as inter-region format (`region/x/y/z`)
3. **Step 3**: Extract region name (use current region if not specified)
4. **Step 4**: Extract and validate coordinates

### Teleportation Process
1. Locate target user across all regions
2. Parse and validate destination format
3. Log teleportation action for audit trail
4. Call `Scene.RequestTeleportLocation()` with parsed parameters
5. Apply `TeleportFlags.ViaLocation` for location-based teleport

## Error Handling

### User Not Found
- Searches all regions comprehensively
- Provides clear feedback when user doesn't exist
- Handles partial name matches gracefully

### Invalid Destinations
- Validates destination format using regex patterns
- Provides syntax help on format errors
- Handles quoted region names with spaces

### Command Syntax Validation
- Requires minimum parameter count (5 parameters)
- Shows usage syntax on insufficient parameters
- Parses coordinates with error handling

## Logging

The module provides comprehensive logging for administrative oversight:

### Debug Logging
- Module lifecycle events (initialize, add/remove regions)
- Region management operations with region names
- All logging uses structured format with module name prefix

### Info Logging  
- Teleportation operations with full details (user, destination, region)
- Provides audit trail for administrative actions

**Log Category**: `OpenSim.Region.OptionalModules.Avatar.Commands.UserCommandsModule`

**Example Log Output**:
```
[DEBUG] UserCommandsModule initializing
[DEBUG] UserCommandsModule adding region RegionName
[DEBUG] UserCommandsModule region loaded RegionName
[INFO] UserCommandsModule teleporting John Doe to 128,128,25 in MainRegion
[DEBUG] UserCommandsModule removing region RegionName
```

## Performance Considerations

### Efficiency Features
- Pre-compiled regex patterns for destination parsing
- Thread-safe region dictionary with read-write locking
- Efficient user lookup with early termination on match
- Minimal memory footprint per region

### Scalability
- Scales linearly with number of regions
- User search performance: O(regions × users_per_region)
- Suitable for typical OpenSim deployments
- Region dictionary provides fast region lookup by UUID

## Usage Examples

### Basic Teleportation Commands
```bash
# Teleport user within current region
teleport user John Doe 128/128/25

# Teleport user to different region
teleport user Jane Smith RegionTwo/64/64/30

# Teleport to region with spaces in name
teleport user Bob Jones "My Region/100/100/40"
```

### Command Syntax
- **Syntax**: `teleport user <first-name> <last-name> <destination>`
- **Parameters**: All parameters are required and positional
- **Coordinates**: Integer values for x, y, z positions
- **Region Names**: Case-sensitive, spaces require quotes

## Integration Points

### Service Dependencies
- **Scene.RequestTeleportLocation()** - Core teleportation functionality
- **Scene.GetScenePresence()** - User lookup by name
- **MainConsole.Instance** - Console output and command registration

### Module Interactions
- Works alongside other region modules
- Independent of user services (operates on present users only)
- Compatible with both Standalone and Grid modes

## Factory Integration

The module is instantiated through `OptionalModulesFactory.CreateOptionalSharedModules()`:

```csharp
// Load UserCommandsModule if enabled for user administration commands
if (modulesConfig.GetBoolean("UserCommandsModule", false))
{
    if (m_log.IsDebugEnabled) m_log.Debug("Loading UserCommandsModule for user administration commands");
    yield return new UserCommandsModule();
}
else
{
    if (m_log.IsDebugEnabled) m_log.Debug("UserCommandsModule disabled - set UserCommandsModule = true in [Modules] to enable user administration commands");
}
```

## Security and Administrative Notes

### Access Control
- **Console Only**: Commands only available through server console
- **Administrator Level**: Requires server administrator access
- **No Client Access**: Cannot be triggered by in-world users
- **Audit Trail**: All actions logged for accountability

### Deployment Considerations
- Disable in production unless needed for administration
- Consider impact of cross-region teleportations
- Monitor logs for administrative actions
- Test destination parsing with your region naming scheme

### Limitations
- Only works with users currently online
- Cannot teleport users not present in managed regions
- Coordinate validation relies on client/region limits
- No undo functionality for teleportation actions

## Maintenance Notes

### Module Dependencies
- No Mono.Addins dependencies (modernized architecture)  
- Depends on OpenSim core framework and scene management
- Uses ThreadedClasses for thread-safe collections
- Compatible with both Standalone and Grid deployment modes

### Future Enhancements
- Could add user listing commands
- Might support user status/info commands  
- Could include user kick/ban functionality
- Potential for user messaging commands

### Testing Considerations
- Requires multiple regions for cross-region testing
- Test with various destination formats
- Verify user lookup across all managed regions
- Test quoted region names with special characters

This module is designed for server administrators who need direct user management capabilities through the OpenSimulator console interface.