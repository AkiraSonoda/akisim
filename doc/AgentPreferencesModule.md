# AgentPreferencesModule

## Overview

The AgentPreferencesModule is a shared region module that handles avatar preferences and capabilities for client-server communication in OpenSimulator. This module provides support for agent preferences, avatar hover height, language settings, and default object permissions through HTTP capabilities.

## Architecture

- **Type**: `ISharedRegionModule` - instantiated once per OpenSim instance and shared across all regions
- **Namespace**: `OpenSim.Region.ClientStack.LindenCaps`
- **Location**: `src/OpenSim.Region.ClientStack.LindenCaps/AgentPreferencesModule.cs`

## Key Features

### Avatar Preferences Management
- Access preferences (maturity rating settings)
- Default object permission masks (Everyone, Group, NextOwner)
- Avatar hover height adjustment
- Language and language visibility settings

### HTTP Capabilities
The module registers three HTTP capabilities:
- **AgentPreferences** - Main preferences handling
- **UpdateAgentLanguage** - Language-specific updates
- **UpdateAgentInformation** - General agent information updates

### Simulator Features
- Automatically enables `AvatarHoverHeightEnabled` simulator feature
- Integrates with `ISimulatorFeaturesModule` to advertise capabilities to clients

## Configuration

The AgentPreferencesModule is automatically loaded as part of the ClientStack.LindenCaps assembly. It does not require specific configuration to be enabled, as it is essential for proper Second Life client compatibility.

### Related Services
The module requires:
- `IAgentPreferencesService` - For persistent storage of agent preferences
- `IAvatarFactoryModule` - For applying hover height changes to avatars
- `ISimulatorFeaturesModule` - For advertising capabilities to clients

## Module Lifecycle

### Initialization
1. **Initialise()** - Basic module setup with debug logging
2. **AddRegion()** - Adds each region to the internal scenes list with logging
3. **RegionLoaded()** - Sets up event handlers and enables simulator features
4. **PostInitialise()** - No-op
5. **Close()** - Cleanup (currently no-op)

### Event Handling
- Registers for `OnRegisterCaps` events from each scene's EventManager
- Automatically registers HTTP capabilities when agents connect

## Technical Implementation

### Data Structures
The module works with `AgentPrefs` objects containing:
- **AccessPrefs**: Maturity rating preferences (string)
- **PermEveryone**: Default permissions for everyone (integer bitmask)
- **PermGroup**: Default permissions for group members (integer bitmask)  
- **PermNextOwner**: Default permissions for next owner (integer bitmask)
- **HoverHeight**: Avatar hover height offset (float)
- **Language**: Preferred language code (string)
- **LanguageIsPublic**: Whether language preference is publicly visible (boolean)

### HTTP Request Processing
All capabilities use the same `UpdateAgentPreferences` method:
1. Validates POST request method
2. Deserializes LLSD XML from request body
3. Loads existing preferences or creates new ones
4. Updates changed fields based on request data
5. Persists changes through `IAgentPreferencesService`
6. Applies hover height changes through `IAvatarFactoryModule`
7. Returns updated preferences as LLSD response

### Thread Safety
- Uses `lock(m_scenes)` for thread-safe access to the scenes list
- HTTP capability handlers are called on separate threads per request

## Integration Points

### Service Dependencies
```csharp
// Agent preferences persistence
IAgentPreferencesService aps = m_scenes[0].AgentPreferencesService;

// Avatar appearance updates
IAvatarFactoryModule afm = m_scenes[0].RequestModuleInterface<IAvatarFactoryModule>();

// Simulator feature advertisement
ISimulatorFeaturesModule simFeatures = scene.RequestModuleInterface<ISimulatorFeaturesModule>();
```

### Event Integration
```csharp
// Capability registration on agent login
scene.EventManager.OnRegisterCaps += RegisterCaps;
```

## Logging

The module provides comprehensive debug logging for:
- Module initialization and lifecycle events
- Region addition/removal with region names
- Capability registration per agent
- Simulator feature enablement

**Log Category**: `OpenSim.Region.ClientStack.LindenCaps.AgentPreferencesModule`

**Example Log Output**:
```
[DEBUG] AgentPreferencesModule initializing
[DEBUG] AgentPreferencesModule adding region RegionName
[DEBUG] AgentPreferencesModule region loaded RegionName
[DEBUG] AgentPreferencesModule enabled AvatarHoverHeightEnabled simulator feature
[DEBUG] AgentPreferencesModule registering capabilities for agent xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

## Error Handling

### HTTP Request Validation
- Returns `400 Bad Request` for invalid LLSD XML
- Returns `404 Not Found` for non-POST requests
- Gracefully handles missing or null service dependencies

### Service Integration
- Creates default `AgentPrefs` if none exist in storage
- Uses null-conditional operators for optional service calls
- Continues operation even if some services are unavailable

## Performance Considerations

### Efficiency Features
- Reuses capability paths with UUID generation
- Only updates storage when preferences actually change
- Uses conditional service calls to avoid unnecessary work
- Thread-safe but minimal locking scope

### Resource Usage
- Maintains lightweight scenes list
- Uses lambda delegates for capability handlers
- Minimal memory footprint per agent

## Security Considerations

### Input Validation
- Validates HTTP request methods
- Uses structured LLSD parsing with exception handling
- No direct file system or database access (uses service layer)

### Permission Handling
- Respects service layer permissions and validation
- Does not perform authorization (delegated to services)
- Maintains agent UUID association throughout request lifecycle

## Module Loading

The AgentPreferencesModule is located in the ClientStack.LindenCaps project and is loaded through the standard OpenSimulator module discovery mechanism. Since it implements `ISharedRegionModule`, it will be automatically discovered and loaded by the module loading system without requiring factory registration.

The module is part of the core client protocol implementation and is essential for proper avatar preference handling in Second Life-compatible viewers.

## Maintenance Notes

### Module Dependencies
- No Mono.Addins dependencies (modernized architecture)
- Depends on OpenSim core services and interfaces
- Compatible with both Standalone and Grid deployment modes

### Future Enhancements
- Currently has placeholder for "god_level" in responses (TODO: Add this)
- Could be extended to support additional preference types
- Capability paths could be made configurable

### Testing Considerations
- Requires functioning `IAgentPreferencesService` implementation
- Best tested with connected viewer clients making preference changes
- Hover height changes are visually verifiable in-world

This module is essential for proper avatar customization and preference management in OpenSimulator deployments.