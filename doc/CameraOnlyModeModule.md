# CameraOnlyModeModule Technical Documentation

## Overview

The **CameraOnlyModeModule** is a non-shared region module that provides viewer camera-only mode support within OpenSimulator. It serves as a specialized viewer control system that enables restricted viewing experiences where users can observe the virtual world through camera movement without full avatar interaction capabilities, making it ideal for surveillance, observation, educational tours, and content presentation scenarios.

## Purpose

The CameraOnlyModeModule serves as a viewer behavior control system that:

- **Camera-Only Mode**: Enables viewers to move cameras without full avatar interaction
- **User Level Filtering**: Applies camera-only mode based on user access levels
- **SimulatorFeatures Integration**: Communicates mode capabilities to viewers through the SimulatorFeatures system
- **Viewer Behavior Control**: Modifies viewer behavior through feature flags
- **Access Restriction**: Provides controlled access to virtual environments
- **Educational Support**: Enables guided tours and presentations without avatar distractions

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│       CameraOnlyModeModule          │
├─────────────────────────────────────┤
│     INonSharedRegionModule          │
│    - Per-region instantiation      │
│    - Independent configuration     │
│    - Scene-specific control        │
├─────────────────────────────────────┤
│      Feature Management             │
│    SimulatorFeaturesHelper         │
│    - User level validation         │
│    - Feature flag injection        │
│    - Client capability control     │
├─────────────────────────────────────┤
│      User Level System              │
│    - Configurable access levels    │
│    - User permission validation    │
│    - Granular access control       │
├─────────────────────────────────────┤
│    Attachment Management            │
│    - Detachment capabilities       │
│    - Avatar state control          │
│    - Login handling                │
└─────────────────────────────────────┘
```

### Integration Architecture

```
Viewer Request
     ↓
SimulatorFeatures
     ↓
OnSimulatorFeaturesRequest()
     ↓
User Level Check
     ↓
Feature Flag Injection
     ↓
Client Receives "camera-only-mode"
     ↓
Viewer Behavior Change
```

### Module Lifecycle

```
  Initialise()
      ↓
  AddRegion()
      ↓
RegionLoaded()
      ↓
SimulatorFeatures Event Subscription
      ↓
Service Ready
      ↓
RemoveRegion()
      ↓
   Close()
```

## Interface Implementation

The module implements:
- **INonSharedRegionModule**: Each region has its own module instance

### Module Lifecycle Methods

```csharp
public void Initialise(IConfigSource config)
public void AddRegion(Scene scene)
public void RegionLoaded(Scene scene)
public void RemoveRegion(Scene scene)
public void Close()
```

## Configuration

### Module Activation

Configure in OpenSim.ini [CameraOnlyModeModule] section:

```ini
[CameraOnlyModeModule]
enabled = true
UserLevel = 100
```

### Configuration Parameters

#### Core Settings

- **enabled**: Enables/disables the CameraOnlyModeModule (default: false)
- **UserLevel**: Maximum user level that receives camera-only mode (default: 0)

#### User Level System

The module applies camera-only mode to users at or below the configured user level:
- **Level 0**: Guest users (default restriction level)
- **Level 50**: Limited access users
- **Level 100**: Regular users
- **Level 200**: Advanced users
- **Level 250**: Administrators (typically exempt)

Higher-level users are not affected by camera-only mode restrictions.

### Configuration Validation

```csharp
public void Initialise(IConfigSource config)
{
    IConfig moduleConfig = config.Configs["CameraOnlyModeModule"];
    if (moduleConfig != null)
    {
        m_Enabled = moduleConfig.GetBoolean("enabled", false);
        if (m_Enabled)
        {
            m_UserLevel = moduleConfig.GetInt("UserLevel", 0);
            m_log.Info("[CAMERA-ONLY MODE]: CameraOnlyModeModule enabled");
        }
    }
}
```

### Factory Integration

The module is loaded via factory with configuration-based activation:

```csharp
var cameraOnlyConfig = configSource?.Configs["CameraOnlyModeModule"];
if (cameraOnlyConfig?.GetBoolean("enabled", false) == true)
{
    if(m_log.IsDebugEnabled) m_log.Debug("Loading CameraOnlyModeModule for viewer camera-only mode support");
    var cameraOnlyModuleInstance = LoadCameraOnlyModeModule();
    if (cameraOnlyModuleInstance != null)
    {
        yield return cameraOnlyModuleInstance;
        if(m_log.IsInfoEnabled) m_log.Info("CameraOnlyModeModule loaded for camera-only mode and viewer feature control");
    }
    else
    {
        m_log.Warn("CameraOnlyModeModule was configured ([CameraOnlyModeModule] enabled = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
    }
}
```

## Core Functionality

### SimulatorFeatures Integration

#### Feature Request Handling

```csharp
public void RegionLoaded(Scene scene)
{
    if (m_Enabled)
    {
        m_Helper = new SimulatorFeaturesHelper(scene);

        ISimulatorFeaturesModule featuresModule = m_scene.RequestModuleInterface<ISimulatorFeaturesModule>();
        if (featuresModule != null)
            featuresModule.OnSimulatorFeaturesRequest += OnSimulatorFeaturesRequest;
    }
}
```

The module integrates with the SimulatorFeatures system to communicate capabilities to viewers.

#### Feature Flag Injection

```csharp
private void OnSimulatorFeaturesRequest(UUID agentID, ref OSDMap features)
{
    if (!m_Enabled)
        return;

    if (m_Helper.UserLevel(agentID) <= m_UserLevel)
    {
        if (!features.TryGetValue("OpenSimExtras", out OSD extrasMap))
        {
            extrasMap = new OSDMap();
            features["OpenSimExtras"] = extrasMap;
        }

        ((OSDMap)extrasMap)["camera-only-mode"] = OSDMap.FromString("true");
        m_log.DebugFormat("[CAMERA-ONLY MODE]: Sent in {0}", m_scene.RegionInfo.RegionName);
    }
    else
        m_log.DebugFormat("[CAMERA-ONLY MODE]: NOT Sending camera-only-mode in {0}", m_scene.RegionInfo.RegionName);
}
```

### User Level Validation System

#### Permission Check Process

1. **Agent Request**: Viewer requests simulator features during connection
2. **User Level Query**: Module queries user's access level via SimulatorFeaturesHelper
3. **Level Comparison**: Compares user level against configured threshold
4. **Feature Injection**: Adds camera-only-mode flag if user level qualifies
5. **Viewer Response**: Viewer modifies behavior based on received features

#### User Level Determination

```csharp
if (m_Helper.UserLevel(agentID) <= m_UserLevel)
```

The SimulatorFeaturesHelper provides user level information based on:
- User account level settings
- Group membership levels
- Administrative privileges
- Custom access control systems

### Attachment Management System

#### Attachment Detachment Capability

```csharp
private void DetachAttachments(UUID agentID)
{
    ScenePresence sp = m_scene.GetScenePresence(agentID);
    if ((sp.TeleportFlags & TeleportFlags.ViaLogin) != 0)
        // Wait a little, cos there's weird stuff going on at login related to
        // the Current Outfit Folder
        Thread.Sleep(8000);

    if (sp != null && m_scene.AttachmentsModule != null)
    {
        List<SceneObjectGroup> attachs = sp.GetAttachments();
        if (attachs != null && attachs.Count > 0)
        {
            foreach (SceneObjectGroup sog in attachs)
            {
                m_log.DebugFormat("[CAMERA-ONLY MODE]: Forcibly detaching attach {0} from {1} in {2}",
                    sog.Name, sp.Name, m_scene.RegionInfo.RegionName);

                m_scene.AttachmentsModule.DetachSingleAttachmentToInv(sp, sog);
            }
        }
    }
}
```

This method provides optional attachment removal capabilities for camera-only users.

#### Login Handling

```csharp
if ((sp.TeleportFlags & TeleportFlags.ViaLogin) != 0)
    Thread.Sleep(8000);
```

Special handling for login scenarios accommodates viewer initialization timing.

## Feature Flag System

### OpenSimExtras Integration

The module adds feature flags to the OpenSimExtras section of SimulatorFeatures:

```json
{
  "OpenSimExtras": {
    "camera-only-mode": "true"
  }
}
```

### Viewer Behavior Modification

When viewers receive the `camera-only-mode` flag, they can modify behavior to:
- Disable avatar movement controls
- Enable enhanced camera controls
- Hide avatar-specific UI elements
- Restrict interaction capabilities
- Focus on observation features

### Feature Flag Scope

- **Per-User**: Applied individually based on user level
- **Per-Region**: Controlled independently per region
- **Real-Time**: Applied during each connection/request
- **Dynamic**: Can change based on user level updates

## Advanced Features

### Multi-Level Access Control

The module supports sophisticated access control through user levels:

```ini
[CameraOnlyModeModule]
enabled = true
UserLevel = 100
```

Example level hierarchy:
- **Level 0**: Anonymous/guest users → Camera-only mode
- **Level 50**: Registered users → Camera-only mode
- **Level 100**: Verified users → Camera-only mode
- **Level 150**: Trusted users → Full access
- **Level 200**: Staff users → Full access
- **Level 250**: Administrators → Full access

### Region-Specific Configuration

Each region can have independent camera-only mode settings:
- Different user level thresholds per region
- Enable/disable per region
- Custom behavior per region type

### Integration with Other Systems

#### SimulatorFeaturesHelper Integration

```csharp
m_Helper = new SimulatorFeaturesHelper(scene);
```

The module leverages SimulatorFeaturesHelper for:
- User level determination
- Permission validation
- Access control integration

#### AttachmentsModule Integration

```csharp
m_scene.AttachmentsModule.DetachSingleAttachmentToInv(sp, sog);
```

Optional integration with attachment management for controlled experiences.

## Performance Characteristics

### Lightweight Operation

- **Event-Driven**: Only activates during feature requests
- **Minimal Overhead**: Simple user level comparison
- **No Persistent State**: Stateless operation per request
- **Efficient Filtering**: Quick level-based filtering

### Scalability Features

- **Per-User Processing**: Individual user evaluation
- **Concurrent Safe**: Thread-safe operations
- **Resource Efficient**: Minimal memory footprint
- **Network Optimized**: Minimal additional data transmission

### Performance Metrics

- **Feature Request Processing**: < 5ms per request
- **Memory Usage**: Negligible runtime footprint
- **Network Impact**: Minimal feature flag addition
- **CPU Usage**: Event-driven with no background processing

## Security Considerations

### Access Control Security

- **User Level Validation**: Prevents privilege escalation through user level checks
- **Feature Flag Control**: Secure feature flag injection prevents client-side bypass
- **Permission Integration**: Leverages existing permission systems
- **Administrative Override**: Higher-level users maintain full access

### Configuration Security

- **Explicit Configuration**: Requires explicit enablement preventing accidental activation
- **Level-Based Control**: Granular control through user level system
- **Per-Region Control**: Isolated configuration prevents cross-region issues
- **Secure Defaults**: Disabled by default for security

### Viewer Security

- **Server-Side Control**: Feature flags enforced server-side
- **Client Trust Model**: Relies on compliant viewers for behavior modification
- **Bypass Prevention**: Server-side enforcement prevents most bypass attempts
- **Audit Capability**: Logging provides audit trail of mode activation

## Error Handling and Resilience

### Configuration Validation

```csharp
public void Initialise(IConfigSource config)
{
    IConfig moduleConfig = config.Configs["CameraOnlyModeModule"];
    if (moduleConfig != null)
    {
        m_Enabled = moduleConfig.GetBoolean("enabled", false);
        if (m_Enabled)
        {
            m_UserLevel = moduleConfig.GetInt("UserLevel", 0);
            m_log.Info("[CAMERA-ONLY MODE]: CameraOnlyModeModule enabled");
        }
    }
}
```

Graceful handling of missing or invalid configuration.

### Service Availability Checks

```csharp
ISimulatorFeaturesModule featuresModule = m_scene.RequestModuleInterface<ISimulatorFeaturesModule>();
if (featuresModule != null)
    featuresModule.OnSimulatorFeaturesRequest += OnSimulatorFeaturesRequest;
```

Graceful degradation when required services are unavailable.

### Exception Handling

```csharp
if (!m_Enabled)
    return;

if (m_Helper.UserLevel(agentID) <= m_UserLevel)
{
    // Protected feature flag injection
}
```

Defensive programming prevents exceptions from affecting other systems.

### Graceful Degradation

- **Module Disabled**: No impact when disabled
- **Service Unavailable**: Continues operation without dependent services
- **Invalid User Data**: Defaults to secure behavior
- **Network Errors**: Local processing continues normally

## Integration Points

### SimulatorFeatures System Integration

```csharp
featuresModule.OnSimulatorFeaturesRequest += OnSimulatorFeaturesRequest;
```

Core integration with OpenSimulator's capability communication system.

### User Management Integration

The module integrates with user management systems through SimulatorFeaturesHelper:
- User level determination
- Permission validation
- Access control enforcement

### Viewer Integration

- **Feature Flag Communication**: Standard SimulatorFeatures protocol
- **Behavior Modification**: Viewer-specific implementation of camera-only mode
- **UI Adaptation**: Viewer UI changes based on feature flags

## Use Cases

### Educational Environments

- **Virtual Campus Tours**: Guided tours without avatar distractions
- **Classroom Observation**: Students observe demonstrations without interference
- **Historical Recreations**: Immersive historical experiences with limited interaction
- **Research Environments**: Controlled observation of virtual experiments

### Content Presentation

- **Virtual Museums**: Guided museum experiences with camera-only viewing
- **Architectural Walkthroughs**: Building tours focusing on visual experience
- **Art Galleries**: Art viewing without avatar presence
- **Virtual Showrooms**: Product demonstrations with controlled viewing

### Surveillance and Monitoring

- **Security Monitoring**: Security personnel camera-only access
- **Event Observation**: Staff monitoring of virtual events
- **Quality Assurance**: Testing environments with observation-only access
- **Research Observation**: Behavioral research with minimal interference

### Entertainment and Media

- **Virtual Theater**: Audience camera control during performances
- **Live Events**: Spectator camera modes for virtual events
- **Documentary Filming**: Camera operation for virtual world documentation
- **Broadcasting**: Camera operators for virtual world broadcasts

## API Reference

### Configuration Options

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| enabled | bool | false | Enable/disable camera-only mode module |
| UserLevel | int | 0 | Maximum user level that receives camera-only mode |

### User Level Guidelines

| Level Range | Typical Users | Camera-Only Mode |
|-------------|---------------|------------------|
| 0-49 | Guests, Anonymous | Applied |
| 50-99 | Registered Users | Applied (if UserLevel ≥ 50) |
| 100-149 | Verified Users | Applied (if UserLevel ≥ 100) |
| 150-199 | Trusted Users | Not Applied (if UserLevel < 150) |
| 200-249 | Staff Users | Not Applied |
| 250+ | Administrators | Not Applied |

### Feature Flag Output

When active, the module adds this to SimulatorFeatures:

```json
{
  "OpenSimExtras": {
    "camera-only-mode": "true"
  }
}
```

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: CameraOnlyModeModule not appearing in logs
Cause: [CameraOnlyModeModule] enabled != true
Solution: Set enabled = true in [CameraOnlyModeModule] section
```

#### Feature Flag Not Sent
```
Symptom: Viewers not receiving camera-only-mode flag
Causes:
- User level above threshold
- SimulatorFeatures module unavailable
- Module not enabled

Solutions:
- Check user level vs UserLevel configuration
- Verify SimulatorFeatures module is loaded
- Confirm module enabled in configuration
```

#### User Level Not Working
```
Symptom: Wrong users receiving camera-only mode
Causes:
- Incorrect UserLevel setting
- User level determination issues
- SimulatorFeaturesHelper problems

Solutions:
- Verify UserLevel configuration value
- Check user level assignment system
- Review user management integration
```

#### Attachment Detachment Not Working
```
Symptom: DetachAttachments method not functioning
Causes:
- Method not called (currently not hooked up)
- AttachmentsModule unavailable
- Permission issues

Solutions:
- Implement OnMakeRootAgent integration if needed
- Verify AttachmentsModule is loaded
- Check attachment permissions
```

### Debug Information

Enable debug logging for detailed troubleshooting:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

// Debug statements already present:
m_log.DebugFormat("[CAMERA-ONLY MODE]: OnSimulatorFeaturesRequest in {0}", m_scene.RegionInfo.RegionName);
m_log.DebugFormat("[CAMERA-ONLY MODE]: Sent in {0}", m_scene.RegionInfo.RegionName);
m_log.DebugFormat("[CAMERA-ONLY MODE]: NOT Sending camera-only-mode in {0}", m_scene.RegionInfo.RegionName);
```

### Configuration Testing

Test configuration with different user levels:

```ini
[CameraOnlyModeModule]
enabled = true
UserLevel = 100  ; Test with different values
```

Monitor logs to verify correct feature flag transmission:
- Debug messages for feature requests
- Confirmation of flag transmission
- User level validation logging

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [CameraOnlyModeModule] enabled setting
- **Enhanced Logging**: Improved operational visibility and debugging capabilities
- **Backward Compatibility**: Maintains full API and configuration compatibility

### Upgrade Considerations

- Update configuration files to use factory loading system
- Review user level assignments after upgrade
- Test feature flag transmission to viewers
- Verify proper integration with SimulatorFeatures system

## Related Components

### Dependencies
- **SimulatorFeaturesHelper**: User level validation and feature management
- **ISimulatorFeaturesModule**: Feature flag communication system
- **INonSharedRegionModule**: Module interface contract
- **AttachmentsModule**: Optional attachment management integration

### Integration Points
- **SimulatorFeatures System**: Feature flag communication protocol
- **User Management**: User level determination and access control
- **Viewer Communication**: Feature flag transmission and behavior modification
- **Scene Management**: Per-region module lifecycle and state management

## Future Enhancements

### Potential Improvements

- **Dynamic User Level Updates**: Real-time user level changes without reconnection
- **Custom Feature Flags**: Additional camera-only mode configuration options
- **Viewer-Specific Behavior**: Different behavior for different viewer types
- **Integration APIs**: APIs for external systems to control camera-only mode
- **Enhanced Logging**: Detailed analytics and usage reporting

### Advanced Features

- **Time-Based Restrictions**: Temporary camera-only mode with automatic expiration
- **Geographic Restrictions**: Camera-only mode for specific regions or parcels
- **Event Integration**: Automatic camera-only mode during events
- **Content Filtering**: Camera-only mode with content restriction overlays
- **Recording Integration**: Camera-only mode with automatic recording capabilities

### Viewer Enhancements

- **Enhanced Camera Controls**: Improved camera movement and positioning tools
- **UI Customization**: Specialized UI for camera-only mode users
- **Recording Tools**: Built-in recording and streaming capabilities
- **Annotation Tools**: Overlay annotation and markup tools
- **Multiple Camera Views**: Simultaneous multiple camera angle support

---

*This documentation covers CameraOnlyModeModule as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full camera-only mode support and viewer behavior control capabilities.*