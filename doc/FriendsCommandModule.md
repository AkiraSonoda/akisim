# FriendsCommandsModule Documentation

## Overview

The `FriendsCommandsModule` is an optional debugging and administrative module in Akisim that provides console commands for inspecting friends list data. It allows administrators to examine friendship relationships, view cached friends data, and debug friends-related issues through the OpenSim console interface.

## File Location

- **Source**: `src/OpenSim.Region.OptionalModules/Avatar/Friends/FriendsCommandsModule.cs`
- **Namespace**: `OpenSim.Region.OptionalModules.Avatar.Friends`
- **Module Type**: OptionalModule (debugging/administrative tool)

## Class Definition

```csharp
public class FriendsCommandsModule : ISharedRegionModule
```

## Dependencies

- **OpenMetaverse**: UUID handling and data structures
- **OpenSim.Framework**: Core framework utilities and console infrastructure
- **OpenSim.Framework.Console**: Console command system
- **OpenSim.Framework.Monitoring**: Performance monitoring capabilities
- **OpenSim.Region.CoreModules.Avatar.Friends**: Friends module integration
- **OpenSim.Region.Framework.Interfaces**: Region module interfaces
- **OpenSim.Region.Framework.Scenes**: Scene management
- **OpenSim.Services.Interfaces**: Service interface definitions
- **NDesk.Options**: Command-line option parsing
- **log4net**: Logging framework (currently disabled)

## Purpose and Scope

### **Primary Function**
Provides administrative console commands for debugging and inspecting friends list functionality.

### **Target Audience**
- Grid administrators
- Developers debugging friends issues
- Technical support staff

### **Use Cases**
- Troubleshooting friends list problems
- Verifying friendship data integrity
- Inspecting cached vs. service data
- Debugging online status issues

## Module Configuration

### **Configuration Section**
The module is controlled by the `[Modules]` section in OpenSim configuration files:

```ini
[Modules]
FriendsCommandsModule = true    # Enable friends debugging commands
# OR
FriendsCommandsModule = false   # Disable friends debugging commands (default)
```

### **Loading Status**
✅ **OptionalModulesFactory Integration**: The module loads when `FriendsCommandsModule = true` is set in the `[Modules]` section through the new OptionalModulesFactory system.

### **Dependencies Required**
The module only activates if all required services are available:
- **IFriendsModule**: Core friends functionality
- **IUserManagement**: User name resolution
- **IPresenceService**: Online status checking
- **FriendsModule.Scene**: Valid scene context

## Module Lifecycle

### 1. Initialization (`Initialise`)
- Currently a no-op with commented debug logging
- **File**: `FriendsCommandsModule.cs:67`

### 2. Post-Initialization (`PostInitialise`)
- Currently a no-op with commented debug logging
- **File**: `FriendsCommandsModule.cs:72`

### 3. Region Addition (`AddRegion`)
- Currently a no-op with commented debug logging
- **File**: `FriendsCommandsModule.cs:82`

### 4. Region Loading (`RegionLoaded`)
- **Service Discovery**: Locates required service interfaces
- **Command Registration**: Registers console commands if all services available
- **Dependency Validation**: Ensures all required modules are loaded
- **File**: `FriendsCommandsModule.cs:92`

### 5. Region Removal (`RemoveRegion`)
- Currently a no-op with commented debug logging
- **File**: `FriendsCommandsModule.cs:87`

### 6. Close (`Close`)
- Currently a no-op with commented debug logging
- **File**: `FriendsCommandsModule.cs:77`

## Service Dependencies

### Required Services

| Service | Type | Purpose |
|---------|------|---------|
| `m_friendsModule` | `IFriendsModule` | Access to friends data and caching |
| `m_userManagementModule` | `IUserManagement` | User name/UUID resolution |
| `m_presenceService` | `IPresenceService` | Online status checking |

### Service Initialization

```csharp
if (m_friendsModule != null && ((FriendsModule)m_friendsModule).Scene != null && 
    m_userManagementModule != null && m_presenceService != null)
{
    // Register console commands
}
```

**File**: `FriendsCommandsModule.cs:103`

## Console Commands

### `friends show` Command

#### Syntax
```
friends show [--cache] <first-name> <last-name>
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `--cache` / `-c` | Flag | No | Show cached data instead of querying service |
| `first-name` | String | Yes | User's first name |
| `last-name` | String | Yes | User's last name |

#### Examples

```bash
# Show friends from service (live data)
friends show John Doe

# Show cached friends data
friends show --cache Jane Smith
friends show -c Avatar User
```

#### Command Registration

```csharp
m_scene.AddCommand(
    "Friends", this, "friends show",
    "friends show [--cache] <first-name> <last-name>",
    "Show the friends for the given user if they exist.",
    "The --cache option will show locally cached information for that user.",
    HandleFriendsShowCommand);
```

**File**: `FriendsCommandsModule.cs:105`

## Command Implementation

### Command Handler (`HandleFriendsShowCommand`)

#### Process Flow

1. **Option Parsing**: Uses NDesk.Options to parse command-line flags
2. **Parameter Validation**: Ensures correct number of parameters
3. **User Resolution**: Converts name to UUID using UserManagement
4. **Data Source Selection**: Chooses between cached or service data
5. **Data Retrieval**: Fetches friends list from appropriate source
6. **Output Formatting**: Displays results in tabular format

**File**: `FriendsCommandsModule.cs:114`

#### Option Parsing

```csharp
Dictionary<string, object> options = new Dictionary<string, object>();
OptionSet optionSet = new OptionSet().Add("c|cache", delegate (string v) { options["cache"] = v != null; });
List<string> mainParams = optionSet.Parse(cmd);
```

**File**: `FriendsCommandsModule.cs:116`

#### User Resolution

```csharp
UUID userId = m_userManagementModule.GetUserIdByName(firstName, lastName);
if (userId.IsZero())
{
    MainConsole.Instance.Output("No such user as {0} {1}", firstName, lastName);
    return;
}
```

**File**: `FriendsCommandsModule.cs:130`

## Data Retrieval Methods

### Cache-Based Retrieval

```csharp
if (options.ContainsKey("cache"))
{
    if (!m_friendsModule.AreFriendsCached(userId))
    {
        MainConsole.Instance.Output("No friends cached on this simulator for {0} {1}", firstName, lastName);
        return;
    }
    else
    {
        friends = m_friendsModule.GetFriendsFromCache(userId);
    }
}
```

**File**: `FriendsCommandsModule.cs:143`

### Service-Based Retrieval

```csharp
else
{
    // FIXME: We're forced to do this right now because IFriendsService has no region connectors.
    friends = ((FriendsModule)m_friendsModule).FriendsService.GetFriends(userId);
}
```

**File**: `FriendsCommandsModule.cs:155`

**Note**: Direct service access is used due to architectural limitations in IFriendsService region connectors.

## Output Formatting

### Table Header

```csharp
MainConsole.Instance.Output(
    "{0,-36}  {1,-36}  {2,-7}  {3,7}  {4,10}", 
    "UUID", "Name", "Status", "MyFlags", "TheirFlags");
```

### Friend Entry Display

```csharp
MainConsole.Instance.Output(
    "{0,-36}  {1,-36}  {2,-7}  {3,-7}  {4,-10}",
    friend.Friend, friendName, onlineText, friend.MyFlags, friend.TheirFlags);
```

**File**: `FriendsCommandsModule.cs:165`

### Output Columns

| Column | Width | Description |
|--------|-------|-------------|
| UUID | 36 chars | Friend's UUID (left-aligned) |
| Name | 36 chars | Resolved display name (left-aligned) |
| Status | 7 chars | "online" or "offline" (left-aligned) |
| MyFlags | 7 chars | Permissions granted to friend (right-aligned) |
| TheirFlags | 10 chars | Permissions granted by friend (right-aligned) |

## Online Status Resolution

### Presence Checking

```csharp
OpenSim.Services.Interfaces.PresenceInfo[] pi = m_presenceService.GetAgents(new string[] { friend.Friend });
if (pi.Length > 0)
    onlineText = "online";
else
    onlineText = "offline";
```

**File**: `FriendsCommandsModule.cs:186`

### Name Resolution

```csharp
UUID friendId;
string friendName;

if (UUID.TryParse(friend.Friend, out friendId))
    friendName = m_userManagementModule.GetUserName(friendId);
else
    friendName = friend.Friend;
```

**File**: `FriendsCommandsModule.cs:177`

## Error Handling

### Invalid User

```bash
No such user as John InvalidUser
```

### No Cached Data

```bash
No friends cached on this simulator for Jane Doe
```

### Invalid Command Syntax

```bash
Usage: friends show [--cache] <first-name> <last-name>
```

## Integration Points

### FriendsModule Integration

- **Cache Access**: `m_friendsModule.AreFriendsCached(userId)`
- **Cache Retrieval**: `m_friendsModule.GetFriendsFromCache(userId)`
- **Service Access**: `((FriendsModule)m_friendsModule).FriendsService.GetFriends(userId)`

### UserManagement Integration

- **Name Resolution**: `m_userManagementModule.GetUserIdByName(firstName, lastName)`
- **UUID Resolution**: `m_userManagementModule.GetUserName(friendId)`

### Presence Service Integration

- **Online Status**: `m_presenceService.GetAgents(new string[] { friend.Friend })`

## Current Status

### **Module State: OptionalModulesFactory Integration**

After Mono.Addins removal and OptionalModulesFactory implementation:

- ✅ **Code Quality**: Clean of deprecated dependencies
- ✅ **Automatic Loading**: Integrated via OptionalModulesFactory
- ✅ **Configuration Support**: Controlled by [Modules] section
- ✅ **Functionality**: Fully restored with modern architecture

### **OptionalModulesFactory Architecture**

The solution uses a dedicated factory for OptionalModules:

```csharp
// RegionModulesControllerPlugin.cs integration
foreach (var module in OptionalModulesFactory.CreateOptionalSharedModules(configSource))
{
    m_log.DebugFormat("Initializing shared optional module: {0}", module.GetType().Name);
    module.Initialise(configSource);
    m_sharedInstances.Add(module);
}
```

**OptionalModulesFactory.cs**:
```csharp
if (modulesConfig.GetBoolean("FriendsCommandsModule", false))
{
    if (m_log.IsDebugEnabled) m_log.Debug("Loading FriendsCommandsModule for friends debugging commands");
    yield return new FriendsCommandsModule();
}
```

## Troubleshooting

### Common Issues

1. **Command Not Available**
   - **Cause**: Module not enabled in configuration
   - **Solution**: Add `FriendsCommandsModule = true` to [Modules] section

2. **"No such user" Error**
   - **Cause**: Invalid user name or user not found
   - **Solution**: Verify spelling and user existence

3. **"No friends cached" Message**
   - **Cause**: User hasn't logged in recently or cache cleared
   - **Solution**: Use command without --cache flag

4. **Service Dependencies Missing**
   - **Cause**: Required modules not loaded
   - **Solution**: Ensure FriendsModule, UserManagement, and PresenceService are active

### Debug Information

The module has extensive commented debug logging that can be enabled:

```csharp
// Enable these lines for debugging
// m_log.DebugFormat("[FRIENDS COMMAND MODULE]: INITIALIZED MODULE");
// m_log.DebugFormat("[FRIENDS COMMAND MODULE]: POST INITIALIZED MODULE");
// m_log.DebugFormat("[FRIENDS COMMAND MODULE]: REGION {0} LOADED", scene.RegionInfo.RegionName);
```

## Example Output

```bash
Region (root) # friends show John Doe
Friends for John Doe aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:

UUID                                  Name                                  Status   MyFlags  TheirFlags
11111111-2222-3333-4444-555555555555  Jane Smith                           online        1          1
66666666-7777-8888-9999-aaaaaaaaaaaa  Bob Wilson                           offline       3          1
bbbbbbbb-cccc-dddd-eeee-ffffffffffff  Alice Johnson @ grid.example.com     online        1          3
```

## Configuration Examples

### Enable Friends Debugging Commands
```ini
[Modules]
FriendsCommandsModule = true
```

### Disable Friends Debugging Commands (Default)
```ini
[Modules]
FriendsCommandsModule = false
# OR simply omit the setting (defaults to false)
```

### Complete Configuration Example
```ini
[Modules]
FriendsModule = FriendsModule           # Use local friends
FriendsCommandsModule = true            # Enable debugging commands

[Friends]
Port = 8003
Connector = "OpenSim.Services.Connectors.dll:FriendsServicesConnector"
```

## Recent Changes

### Mono.Addins Removal and OptionalModulesFactory Implementation

As part of architectural modernization:

- Removed `using Mono.Addins;` directive from FriendsCommandsModule
- Removed `[Extension]` attribute decoration
- **Created OptionalModulesFactory**: New factory system for OptionalModules
- **Integrated RegionModulesController**: Added OptionalModules support to module loading
- **Added Project References**: RegionModulesController can now reference OptionalModules
- **Configuration Support**: Full [Modules] section configuration support restored

## Future Enhancements

### Potential Improvements

1. **Enhanced Output Formatting**
   - Color coding for online/offline status
   - Detailed permission flag interpretation
   - Sortable columns

2. **Additional Commands**
   - `friends add` for testing
   - `friends remove` for cleanup
   - `friends cache clear` for debugging

3. **Export Functionality**
   - CSV export for analysis
   - JSON format for automation

4. **Filtering Options**
   - Online friends only
   - Permission-based filtering
   - Search by partial name

## API Reference

### Public Methods

| Method | Purpose | Parameters | Returns |
|--------|---------|------------|---------|
| `Initialise(IConfigSource)` | Module initialization | Configuration source | void |
| `PostInitialise()` | Post-initialization | None | void |
| `AddRegion(Scene)` | Add region | Scene to add | void |
| `RegionLoaded(Scene)` | Complete region setup | Loaded scene | void |
| `RemoveRegion(Scene)` | Remove region | Scene to remove | void |
| `Close()` | Module shutdown | None | void |

### Properties

| Property | Type | Purpose |
|----------|------|---------|
| `Name` | `string` | Returns "Appearance Information Module" |
| `ReplaceableInterface` | `Type` | Returns null (not replaceable) |

### Private Methods

| Method | Purpose | Parameters |
|--------|---------|------------|
| `HandleFriendsShowCommand()` | Process friends show command | module, cmd array |

## Version Information

- **Last Modified**: Part of Mono.Addins removal initiative with ModuleFactory integration
- **Compatibility**: OpenSim 0.9.x and later
- **Status**: Active (configuration-controlled)
- **Dependencies**: Requires FriendsModule, UserManagement, and PresenceService