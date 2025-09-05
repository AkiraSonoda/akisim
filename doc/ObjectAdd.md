# ObjectAdd Module

## Overview

The `ObjectAdd` module is a region capability module that handles HTTP-based object creation requests in OpenSim. It provides a capabilities endpoint that allows clients to create primitive objects (prims) in the virtual world through POST requests containing object geometry and property data.

## Location

**File:** `src/OpenSim.Region.ClientStack.LindenCaps/ObjectCaps/ObjectAdd.cs`  
**Namespace:** `OpenSim.Region.ClientStack.Linden`

## Dependencies

This module uses **Mono.Addins** for automatic discovery and loading:
- `[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "ObjectAdd")]`

### Key Dependencies
- `OpenSim.Region.Framework.Interfaces.INonSharedRegionModule`
- `OpenSim.Framework.Capabilities.Caps`
- `OpenSim.Region.Framework.Scenes.Scene`
- `OpenMetaverse.StructuredData` (OSD format parsing)

## Functionality

### Core Features
- Provides HTTP capability endpoint for object creation
- Handles both v1 and v2 protocol formats for object data
- Processes complex primitive geometry parameters
- Validates permissions before object creation
- Creates `SceneObjectGroup` instances in the world

### Object Creation Pipeline
1. **Request Validation** - Validates scene presence and request format
2. **Data Parsing** - Extracts object parameters from OSD map
3. **Position Calculation** - Uses raycast to determine placement position
4. **Permission Check** - Verifies user can rez objects at target location
5. **Shape Creation** - Builds `PrimitiveBaseShape` with geometry data
6. **Object Instantiation** - Creates and configures `SceneObjectGroup`
7. **Response** - Returns local ID of created object

## API Endpoint

### Request Format
- **Method:** POST
- **Content-Type:** application/llsd+xml (OSD format)
- **Authentication:** Requires valid scene presence

### Protocol Versions

#### Version 2 (Modern Format)
```json
{
  "ObjectData": {
    "BypassRaycast": "boolean",
    "EveryoneMask": "binary",
    "Flags": "binary", 
    "GroupMask": "binary",
    "Material": "integer",
    "NextOwnerMask": "binary",
    "PCode": "integer",
    "Path": {
      "Begin": "integer",
      "Curve": "integer",
      "End": "integer",
      "RadiusOffset": "integer",
      "Revolutions": "integer",
      "ScaleX": "integer",
      "ScaleY": "integer",
      "ShearX": "integer",
      "ShearY": "integer",
      "Skew": "integer",
      "TaperX": "integer",
      "TaperY": "integer",
      "Twist": "integer",
      "TwistBegin": "integer"
    },
    "Profile": {
      "Begin": "integer",
      "Curve": "integer", 
      "End": "integer",
      "Hollow": "integer"
    },
    "RayEnd": ["float", "float", "float"],
    "RayStart": ["float", "float", "float"],
    "RayTargetId": "uuid",
    "RayEndIsIntersection": "boolean",
    "Scale": ["float", "float", "float"],
    "Rotation": ["float", "float", "float", "float"],
    "State": "integer",
    "LastAttachPoint": "integer"
  },
  "AgentData": {
    "GroupId": "uuid"
  }
}
```

#### Version 1 (Legacy Format)
Uses flat key-value structure with lowercase snake_case field names.

### Response Format
```xml
<llsd>
  <map>
    <key>local_id</key>
    <binary>[base64_encoded_uint]</binary>
  </map>
</llsd>
```

### Response Codes
- **200 OK** - Object created successfully, returns local ID
- **400 Bad Request** - Invalid request data or malformed parameters
- **401 Unauthorized** - User lacks permission to rez objects at location
- **410 Gone** - Avatar not present in scene

## Geometric Parameters

### Path Parameters
Control the extrusion path for complex shapes:
- **Begin/End** - Path segment range (0-100)
- **Curve** - Path curvature type
- **RadiusOffset** - Radius modification along path
- **Revolutions** - Number of twists along path
- **Scale X/Y** - Cross-section scaling
- **Shear X/Y** - Cross-section shearing
- **Skew** - Path skewing
- **Taper X/Y** - Cross-section tapering
- **Twist/TwistBegin** - Rotation along path

### Profile Parameters
Define the cross-sectional shape:
- **Begin/End** - Profile arc range
- **Curve** - Profile curve type (circle, square, triangle, etc.)
- **Hollow** - Interior hollowing amount

### Raycast Parameters
Determine object placement:
- **RayStart** - Starting point of placement ray
- **RayEnd** - End point of placement ray  
- **RayTargetId** - UUID of target object for surface placement
- **RayEndIsIntersection** - Whether ray end represents surface intersection
- **BypassRaycast** - Skip raycast and use exact position

## Security

### Permission Validation
- Validates scene presence via `m_scene.TryGetScenePresence()`
- Checks rez permissions via `m_scene.Permissions.CanRezObject()`
- Calculates placement position using `m_scene.GetNewRezLocation()`

### Input Validation
- Validates OSD map structure and data types
- Handles both v1 and v2 protocol formats gracefully
- Catches parsing exceptions and returns appropriate error codes

## Module Lifecycle

1. **Initialise()** - No configuration required
2. **AddRegion()** - Registers with scene event manager
3. **RegionLoaded()** - No additional setup
4. **RemoveRegion()** - Unregisters event handlers

## Implementation Details

### Data Type Handling
- **Binary Fields** - Uses `ReadUIntVal()` to handle big-endian binary masks
- **Vector/Quaternion** - Extracts from OSD arrays with error handling
- **UUID Fields** - Direct OSD to UUID conversion

### Shape Creation
Creates `PrimitiveBaseShape` with full geometry parameters:
```csharp
PrimitiveBaseShape pbs = PrimitiveBaseShape.CreateBox();
// Apply all path, profile, and material parameters
pbs.PathBegin = (ushort)path_begin;
// ... configure remaining properties
```

### Object Integration
- Creates `SceneObjectGroup` via `m_scene.AddNewPrim()`
- Configures permissions masks and group ownership
- Invalidates permissions cache for proper propagation
- Returns local ID for client tracking

## Usage Notes

- Module automatically loads via Mono.Addins extension system
- Supports complex primitive shapes through parametric geometry
- Handles both viewer v1 and v2 protocol formats
- Position calculation includes collision detection and ground clamping
- Created objects inherit standard OpenSim physics and scripting capabilities