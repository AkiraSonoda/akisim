# ObjectAddModule Technical Documentation

## Overview

The ObjectAddModule is a region module responsible for handling object creation requests from viewers through the HTTP capabilities system. It provides the core functionality for users to create primitive objects (prims) in the virtual world environment.

## Module Classification

- **Type**: INonSharedRegionModule
- **Namespace**: OpenSim.Region.ClientStack.Linden
- **Assembly**: OpenSim.Region.ClientStack.LindenCaps
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs

## Core Functionality

### Primary Purpose

The ObjectAddModule enables users to create new primitive objects in the scene through HTTP-based capability requests. It handles the complete workflow from request parsing to object instantiation in the virtual world.

### Key Capabilities

1. **Object Creation**: Processes requests to create new primitive objects
2. **Geometry Processing**: Handles complex primitive shape parameters and geometry
3. **Permission Validation**: Enforces object creation permissions and policies
4. **Position Calculation**: Determines appropriate object placement using raycast algorithms
5. **Property Management**: Sets object properties including permissions, materials, and flags

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence
1. Initialise(IConfigSource) - Module setup and configuration
2. AddRegion(Scene) - Register with scene and hook capability events
3. RegionLoaded(Scene) - Final setup after all modules loaded
4. RegisterCaps(UUID, Caps) - Register HTTP capability handlers
```

### HTTP Capability Registration

The module registers the "ObjectAdd" capability with the scene's capability system:

```csharp
caps.RegisterSimpleHandler("ObjectAdd", new SimpleOSDMapHandler("POST", capPath,
    delegate (IOSHttpRequest httpRequest, IOSHttpResponse httpResponse, OSDMap map)
    {
        ProcessAdd(httpRequest, httpResponse, map, agentID);
    }));
```

## Request Processing

### Input Data Formats

The module supports two data format versions for backward compatibility:

#### Version 2 Format (Current)
```json
{
  "ObjectData": {
    "BypassRaycast": boolean,
    "EveryoneMask": uint,
    "Flags": uint,
    "GroupMask": uint,
    "Material": int,
    "NextOwnerMask": uint,
    "PCode": int,
    "Path": {
      "Begin": int,
      "Curve": int,
      "End": int,
      "RadiusOffset": int,
      "Revolutions": int,
      "ScaleX": int,
      "ScaleY": int,
      "ShearX": int,
      "ShearY": int,
      "Skew": int,
      "TaperX": int,
      "TaperY": int,
      "Twist": int,
      "TwistBegin": int
    },
    "Profile": {
      "Begin": int,
      "Curve": int,
      "End": int,
      "Hollow": int
    },
    "RayEnd": [x, y, z],
    "RayStart": [x, y, z],
    "RayEndIsIntersection": boolean,
    "RayTargetId": UUID,
    "Rotation": [x, y, z, w],
    "Scale": [x, y, z],
    "State": int,
    "LastAttachPoint": int
  },
  "AgentData": {
    "GroupId": UUID
  }
}
```

#### Version 1 Format (Legacy)
Flat structure with snake_case property names for backward compatibility.

### Geometry Parameter Processing

#### Path Parameters
- **Begin/End**: Defines the extent of the path sweep
- **Curve**: Specifies the path curve type (linear, circle, etc.)
- **RadiusOffset**: Offset from the center during sweep
- **Revolutions**: Number of complete rotations for helical paths
- **Scale X/Y**: Cross-sectional scaling along the path
- **Shear X/Y**: Skewing transformation of the cross-section
- **Skew**: Rotation of the cross-section around the path
- **Taper X/Y**: Scaling variation from start to end
- **Twist/TwistBegin**: Rotation around the path axis

#### Profile Parameters
- **Begin/End**: Arc extent for curved profiles
- **Curve**: Profile shape (square, circle, triangle, etc.)
- **Hollow**: Interior hollowing amount (0-95%)

## Object Creation Workflow

### 1. Request Validation
```csharp
// Verify agent presence
if(!m_scene.TryGetScenePresence(avatarID, out ScenePresence sp))
{
    httpResponse.StatusCode = (int)HttpStatusCode.Gone;
    return;
}
```

### 2. Position Calculation
```csharp
Vector3 pos = m_scene.GetNewRezLocation(
    ray_start, ray_end, ray_target_id, rotation,
    (bypass_raycast) ? (byte)1 : (byte)0,
    (ray_end_is_intersection) ? (byte)1 : (byte)0,
    true, scale, false);
```

### 3. Permission Checking
```csharp
if (!m_scene.Permissions.CanRezObject(1, avatarID, pos))
{
    httpResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
    return;
}
```

### 4. Shape Creation
```csharp
PrimitiveBaseShape pbs = PrimitiveBaseShape.CreateBox();
// Configure all geometric parameters
pbs.PathBegin = (ushort)path_begin;
pbs.PathCurve = (byte)path_curve;
// ... additional parameter assignments
```

### 5. Object Instantiation
```csharp
SceneObjectGroup obj = m_scene.AddNewPrim(avatarID, group_id, pos, rotation, pbs);
SceneObjectPart rootpart = obj.RootPart;
// Configure object properties
rootpart.Flags |= (PrimFlags)flags;
rootpart.EveryoneMask = everyone_mask;
// ... additional property assignments
```

### 6. Permission Update
```csharp
obj.InvalidateDeepEffectivePerms();
```

## Data Conversion Utilities

### Binary Data Handling

#### ReadUIntVal Method
Converts OSD binary data to unsigned integers with proper endianness handling:

```csharp
private uint ReadUIntVal(OSD obj)
{
    byte[] tmp = obj.AsBinary();
    if (BitConverter.IsLittleEndian)
        Array.Reverse(tmp);
    return Utils.BytesToUInt(tmp);
}
```

#### ConvertUintToBytes Method
Formats unsigned integers for LLSD binary encoding in response:

```csharp
private static string ConvertUintToBytes(uint val)
{
    byte[] resultbytes = Utils.UIntToBytes(val);
    if (BitConverter.IsLittleEndian)
        Array.Reverse(resultbytes);
    return String.Format("<binary>{0}</binary>", Convert.ToBase64String(resultbytes));
}
```

## Response Format

### Success Response
```xml
<llsd>
  <map>
    <key>local_id</key>
    <binary>[base64-encoded-local-id]</binary>
  </map>
</llsd>
```

### Error Responses
- **400 Bad Request**: Invalid request format or missing required data
- **401 Unauthorized**: Insufficient permissions to create objects
- **410 Gone**: Agent no longer present in scene

## Integration Points

### Scene Manager Integration
- Utilizes `Scene.GetNewRezLocation()` for intelligent object placement
- Leverages `Scene.AddNewPrim()` for object creation
- Integrates with `Scene.Permissions` for access control

### Capabilities System
- Registers with the HTTP capabilities framework
- Provides RESTful endpoint for object creation
- Handles request routing and response formatting

### Primitive System
- Creates `PrimitiveBaseShape` objects with complex geometry
- Configures `SceneObjectGroup` and `SceneObjectPart` properties
- Manages object permissions and metadata

## Configuration

### Module Configuration
The module logs initialization and region loading events:

```csharp
m_log.Info("ObjectAddModule initialized");
m_log.InfoFormat("ObjectAddModule adding region {0}", scene.Name);
m_log.InfoFormat("ObjectAddModule region loaded for {0}", scene.Name);
```

### Capability Registration Logging
```csharp
m_log.InfoFormat("ObjectAddModule registering ObjectAdd capability at {0} for agent {1}",
    capPath, agentID);
```

## Security Considerations

### Permission Enforcement
- Validates agent presence before processing requests
- Checks object creation permissions through scene permission system
- Enforces position-based restrictions for object placement

### Input Validation
- Validates OSD map structure and required fields
- Handles malformed geometry parameters gracefully
- Provides appropriate HTTP error codes for invalid requests

### Resource Management
- Limits object creation to authorized users
- Integrates with scene's resource management systems
- Ensures proper cleanup on request failures

## Performance Characteristics

### Request Processing
- Synchronous processing of object creation requests
- Efficient OSD data parsing and validation
- Minimal memory allocation during request handling

### Geometry Calculation
- Leverages scene's optimized raycast algorithms
- Efficient primitive shape creation and configuration
- Fast permission checking through cached systems

## Error Handling

### Common Error Scenarios
1. **Agent Disconnection**: Returns 410 Gone when agent no longer present
2. **Permission Denial**: Returns 401 Unauthorized for insufficient permissions
3. **Invalid Data**: Returns 400 Bad Request for malformed requests
4. **Geometry Errors**: Graceful handling of invalid geometric parameters

### Logging Strategy
- Informational logging for normal operations
- Error details captured through exception handling
- Debug information available through scene logging

## Dependencies

### Core Framework Dependencies
- `OpenSim.Framework` - Core data structures and utilities
- `OpenSim.Region.Framework` - Scene and region management
- `OpenMetaverse` - Protocol data types and utilities

### Capability System Dependencies
- `OpenSim.Framework.Capabilities` - HTTP capability framework
- `OpenSim.Framework.Servers.HttpServer` - HTTP server infrastructure

### Region Module Dependencies
- Scene management for object creation and placement
- Permission system for access control
- Event system for capability registration

## Future Enhancement Opportunities

### Advanced Geometry Support
- Support for mesh objects and complex geometries
- Enhanced material and texture handling
- Advanced physics property configuration

### Performance Optimizations
- Asynchronous request processing for complex operations
- Batch object creation capabilities
- Enhanced caching for frequently used shapes

### Security Enhancements
- Rate limiting for object creation requests
- Enhanced validation for geometric parameters
- Audit logging for object creation events

## Troubleshooting

### Common Issues
1. **Objects Not Appearing**: Check permissions and position validation
2. **Geometry Artifacts**: Verify path and profile parameter ranges
3. **Permission Errors**: Validate user permissions and land settings

### Debug Information
- Enable scene logging for detailed operation traces
- Monitor HTTP capability request/response patterns
- Check agent presence and session validity

### Configuration Verification
- Ensure module is properly loaded in region configuration
- Verify capability registration in scene startup logs
- Confirm permission system configuration

## Conclusion

The ObjectAddModule provides essential object creation functionality for the OpenSimulator platform. Its robust architecture handles complex geometry parameters while maintaining security and performance standards. The module's integration with the capabilities system enables efficient HTTP-based object creation workflows essential for content creation in virtual worlds.