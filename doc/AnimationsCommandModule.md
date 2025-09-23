# AnimationsCommandModule

## Overview

The AnimationsCommandModule is a shared optional module that provides console commands for debugging and inspecting avatar animations in OpenSimulator. It enables server administrators to analyze animation states, examine animator data, and diagnose animation-related issues through detailed console reporting. This module is essential for troubleshooting avatar movement, gesture playback, and animation synchronization problems.

## Architecture

- **Type**: `ISharedRegionModule` - instantiated once per OpenSim instance and shared across all regions
- **Namespace**: `OpenSim.Region.OptionalModules.Avatar.Animations`
- **Location**: `src/OpenSim.Region.OptionalModules/Avatar/Animations/AnimationsCommandModule.cs`

## Key Features

### Animation Inspection Commands
- **`show animations`** - Display comprehensive animation information for avatars
- Cross-region animation analysis across all simulator regions
- Thread-safe region management with `RwLockedList<Scene>`

### Animation Data Analysis
- Current movement animation inspection
- Default and implicit default animation reporting
- Complete animation sequence analysis with UUIDs and names
- Animation source object identification

### Avatar Presence Management
- Operates on root scene presences (non-child agents)
- Supports both individual avatar and bulk avatar inspection
- Real-time animation state capture

## Console Commands Reference

### Animation Commands

#### `show animations [<first-name> <last-name>]`
**Purpose**: Display detailed animation information for avatars in the simulator.

**Usage**:
```bash
# Show animations for all avatars across all regions
show animations

# Show detailed animation info for specific avatar
show animations John Doe
```

**Output Format**:

For each avatar, the command displays:

1. **Avatar Identification**
   - Avatar name and presence information

2. **Current Movement Animation**
   - Animation name from DefaultAvatarAnimations
   - Animation UUID

3. **Default Animation**
   - Default animation UUID
   - Animation name resolution

4. **Implicit Default Animation**
   - Implicit default animation UUID
   - Animation name resolution

5. **Animation Sequence Table**
   - **Animation ID**: UUID of the animation
   - **Name**: Resolved animation name (from assets or defaults)
   - **Seq**: Sequence number for animation ordering
   - **Object ID**: UUID of the object that triggered the animation

**Example Output**:
```
Animations for John Doe
  Current movement anim: Stand, 2408fe9e-df1d-1d7d-f4ff-1384d0b84bad
  Default anim        : 2408fe9e-df1d-1d7d-f4ff-1384d0b84bad, Stand
  Implicit default anim: 2408fe9e-df1d-1d7d-f4ff-1384d0b84bad, Stand

  Animation ID                          Name                 Seq  Object ID
  2408fe9e-df1d-1d7d-f4ff-1384d0b84bad  Stand                1    00000000-0000-0000-0000-000000000000
  15468e00-fce0-1398-d1aa-4f6fb42417dc  hello                2    f47ac10b-58cc-4372-a567-0e02b2c3d479
```

## Configuration

### Module Activation
The AnimationsCommandModule is loaded through the OptionalModulesFactory when enabled in configuration:

```ini
[Modules]
AnimationsCommandModule = true
```

### Default State
- **Default**: Disabled (`false`)
- **Requirement**: Must be explicitly enabled in configuration
- **Dependencies**: None - operates independently

## Technical Implementation

### Core Components

#### Scene Management
```csharp
private RwLockedList<Scene> m_scenes = new RwLockedList<Scene>();
```
- Thread-safe collection of regions managed by this simulator
- Supports concurrent read access with write protection
- Automatically maintained through region lifecycle events

#### Animation Data Sources
- **ScenePresenceAnimator**: Primary source for animation state
- **AnimationSet**: Container for avatar's current animations
- **DefaultAvatarAnimations**: Resolver for standard animation names
- **Asset System**: Source for custom animation names

### Module Lifecycle

#### Initialization
```csharp
public void Initialise(IConfigSource source)
```
- Performs basic module setup
- Logs initialization status at debug level

#### Region Integration
```csharp
public void RegionLoaded(Scene scene)
```
- Registers the scene for animation monitoring
- Adds console commands to the scene's command system
- Logs successful command registration

#### Cleanup
```csharp
public void Close()
```
- Clears scene collection
- Logs shutdown status

### Animation Data Processing

#### Avatar Discovery
The module uses different strategies based on command parameters:

**All Avatars**:
```csharp
scene.ForEachRootScenePresence(sp => GetAttachmentsReport(sp, sb));
```

**Specific Avatar**:
```csharp
ScenePresence sp = scene.GetScenePresence(optionalTargetFirstName, optionalTargetLastName);
if (sp != null && !sp.IsChildAgent)
    GetAttachmentsReport(sp, sb);
```

#### Animation Name Resolution
The module attempts to resolve animation names through multiple sources:
1. **Built-in Animations**: Uses `DefaultAvatarAnimations.GetDefaultAnimation()`
2. **Custom Animations**: Uses `sp.Animator.GetAnimName()` for asset-based names
3. **Fallback**: Displays UUID if name resolution fails

## Logging and Debugging

### Log Levels
- **Debug**: Detailed operation logging including region events and command execution
- **Info**: High-level status messages for successful module loading and command registration
- **Error**: (Inherited from base logging) - system errors and exceptions

### Log Message Format
All log messages are prefixed with "AnimationsCommandModule:" for easy identification in log files.

**Example Log Messages**:
```
DEBUG AnimationsCommandModule: Region 'Welcome Area' loaded, registering animation debugging commands
INFO  AnimationsCommandModule: Animation debugging commands registered for region 'Welcome Area'
DEBUG AnimationsCommandModule: Showing animations for specific user: John Doe
DEBUG AnimationsCommandModule: Generated animation report for 3 avatars
```

## Use Cases

### Animation Debugging
- **Stuck Animations**: Identify animations that aren't properly stopping or transitioning
- **Missing Animations**: Detect when expected animations aren't playing
- **Sequence Issues**: Analyze animation ordering and sequencing problems

### Avatar Troubleshooting
- **Movement Problems**: Examine movement animation states when avatars appear frozen
- **Gesture Issues**: Investigate gesture animation playback problems
- **Cross-Region Issues**: Analyze animation state consistency across region boundaries

### Development and Testing
- **Animation Asset Validation**: Verify that custom animations are properly loaded and named
- **State Verification**: Confirm animation states during scripted sequences
- **Performance Analysis**: Monitor animation counts and complexity per avatar

## Integration Points

### Scene Framework
- Integrates with `Scene.AddCommand()` for console command registration
- Uses `Scene.ForEachRootScenePresence()` for avatar enumeration
- Accesses `Scene.GetScenePresence()` for individual avatar lookup

### Avatar Animation System
- Reads from `ScenePresence.Animator` for current animation state
- Accesses `ScenePresenceAnimator.Animations` for animation collections
- Uses `ScenePresenceAnimator.CurrentMovementAnimation` for movement state

### Asset Resolution
- Leverages `DefaultAvatarAnimations` for built-in animation names
- Uses asset system through `GetAnimName()` for custom animation resolution
- Handles both UUID and name-based animation identification

## Security Considerations

### Access Control
- Commands are only available through the simulator console
- No remote access or in-world command capabilities
- Limited to users with simulator administrative access

### Data Privacy
- Reports animation UUIDs which could be considered asset identifiers
- Shows avatar names and animation states
- Does not expose sensitive user data beyond animation information

## Performance Characteristics

### Command Execution Time
- **Single Avatar**: O(1) for targeted queries
- **All Avatars**: O(n) where n is the number of avatars across all regions
- **Memory Usage**: Minimal - uses StringBuilder for report generation

### Resource Impact
- Read-only operations with minimal system impact
- Thread-safe region collection reduces contention
- No persistent state beyond scene references

## Best Practices

### Administration Usage
1. **Targeted Queries**: Use specific avatar names when troubleshooting individual issues
2. **Log Monitoring**: Enable debug logging when investigating animation problems
3. **Regular Checks**: Periodically run bulk queries to identify system-wide animation issues

### Development Integration
1. **Test Automation**: Use commands in test scripts to verify animation behavior
2. **State Validation**: Incorporate into deployment verification procedures
3. **Performance Monitoring**: Monitor animation complexity in production environments

## Troubleshooting

### Common Issues

#### "Usage: show animations [<first-name> <last-name>]"
- **Cause**: Incorrect command syntax
- **Solution**: Ensure proper spacing and parameter count in command

#### Empty or Partial Results
- **Cause**: Avatars might be child agents or not properly loaded
- **Solution**: Verify avatar presence and root agent status in target regions

#### Animation Names Show as UUIDs
- **Cause**: Custom animations may not have resolvable names
- **Solution**: Check asset loading and animation upload process

### Debugging Steps
1. Enable debug logging for the AnimationsCommandModule
2. Verify module is loaded in configuration
3. Check region loading status in logs
4. Confirm avatar presence in target regions
5. Validate animation asset availability

## Related Modules

### Complementary Modules
- **AppearanceInfoModule**: For avatar appearance and texture debugging
- **UserCommandsModule**: For user management and status commands
- **SceneCommandsModule**: For scene-level debugging and statistics

### Integration Dependencies
- **Scene Framework**: Core scene management and avatar presence
- **Animation System**: Avatar animation state and management
- **Asset System**: Animation asset resolution and name lookup
- **Console System**: Command registration and execution framework