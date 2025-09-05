# EstateChangeInfoCapModule

## Overview

The `EstateChangeInfoCapModule` is a region capability module that provides HTTP-based API access for modifying estate settings in OpenSim. It implements the `INonSharedRegionModule` interface and handles estate configuration changes through the Second Life/OpenSimulator capabilities system.

## Location

**File:** `src/OpenSim.Region.ClientStack.LindenCaps/EstateChangeInfo.cs`  
**Namespace:** `OpenSim.Region.ClientStack.Linden`

## Dependencies

This module does **not** use Mono.Addins and implements the region module interface directly.

### Key Dependencies
- `OpenSim.Region.Framework.Interfaces.INonSharedRegionModule`
- `OpenSim.Region.Framework.Interfaces.IEstateModule` 
- `OpenSim.Framework.Capabilities.Caps`

## Configuration

The module is configured through the `[ClientStack.LindenCaps]` section in OpenSim configuration:

```ini
[ClientStack.LindenCaps]
Cap_EstateChangeInfo = localhost
```

The module is only enabled when `Cap_EstateChangeInfo` is set to `"localhost"`.

## Functionality

### Core Features
- Provides HTTP capability endpoint for estate settings modification
- Handles estate information changes via POST requests
- Validates user permissions before processing changes
- Integrates with the estate module system

### Supported Estate Settings
- **Estate Name** - Name of the estate
- **External Visibility** - Whether the region appears in search
- **Direct Teleport** - Allow direct teleportation to the region
- **Anonymous Access** - Deny anonymous users
- **Age Verification** - Require age verification
- **Voice Chat** - Enable voice chat in the region
- **Public Access Override** - Override public access restrictions
- **Environment Override** - Allow region environment overrides

## API Endpoint

### Request Format
- **Method:** POST
- **Content-Type:** application/llsd+xml (OSD format)
- **Authentication:** Requires estate management permissions

### Request Parameters
```json
{
  "estate_name": "string",
  "invoice": "uuid", 
  "is_externally_visible": "boolean",
  "allow_direct_teleport": "boolean", 
  "deny_anonymous": "boolean",
  "deny_age_unverified": "boolean",
  "allow_voice_chat": "boolean",
  "override_public_access": "boolean",
  "override_environment": "boolean"
}
```

### Response Codes
- **200 OK** - Settings updated successfully
- **400 Bad Request** - Invalid request data or processing failed
- **401 Unauthorized** - User lacks estate management permissions  
- **404 Not Found** - Non-POST request method
- **501 Not Implemented** - Estate settings not available

## Security

### Permission Checks
- Validates scene presence of requesting agent
- Requires estate command permissions via `m_scene.Permissions.CanIssueEstateCommand()`
- Only processes requests from authorized estate managers

### Input Validation
- Deserializes and validates OSD request format
- Handles malformed requests gracefully
- Returns appropriate HTTP status codes for errors

## Module Lifecycle

1. **Initialise()** - Reads configuration and enables module if configured
2. **AddRegion()** - Associates module with scene if enabled
3. **RegionLoaded()** - Validates estate settings and registers capabilities
4. **RemoveRegion()** - Cleans up event handlers when region is removed

## Integration Points

### Estate Module Integration
The module delegates actual estate changes to the `IEstateModule` implementation through:
```csharp
m_EstateModule.handleEstateChangeInfoCap(estateName, invoice, 
    externallyVisible, allowDirectTeleport, denyAnonymous, 
    denyAgeUnverified, alloVoiceChat, overridePublicAccess, 
    allowEnvironmentOverride);
```

### Capabilities System
Registers the "EstateChangeInfo" capability with a random UUID path for security.

## Usage Notes

- Module only activates when explicitly configured with localhost capability URL
- Requires functional estate module to be present in the region
- Changes are processed synchronously and return immediate success/failure status  
- Estate settings validation occurs at the `IEstateModule` level