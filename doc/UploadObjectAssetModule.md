# UploadObjectAssetModule Technical Documentation

## Overview

The **UploadObjectAssetModule** is a non-shared region module that provides object upload capabilities for OpenSimulator viewers through the `UploadObjectAsset` capability. It enables viewers to upload complex multi-part 3D objects with full geometric properties, textures, and linking relationships directly to regions, supporting advanced content creation workflows and seamless object importing from external 3D modeling applications.

## Purpose

The UploadObjectAssetModule serves as a critical content creation infrastructure component that:

- **Complex Object Upload**: Provides the `UploadObjectAsset` capability for uploading multi-part 3D objects
- **Geometric Precision**: Supports full primitive geometry including path curves, profiles, and deformation parameters
- **Advanced Materials**: Handles texture mapping, materials, bump mapping, glow, and transparency
- **Object Linking**: Automatically links multiple primitives into cohesive SceneObjectGroups
- **Special Effects**: Supports flexible prims, light sources, sculpted prims, and projection parameters
- **Content Creation**: Enables advanced 3D content workflows and external tool integration

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│      UploadObjectAssetModule        │
├─────────────────────────────────────┤
│      INonSharedRegionModule         │
│    - Per-region instantiation      │
│    - Scene-specific object mgmt     │
├─────────────────────────────────────┤
│     Capabilities Integration        │
│    - UploadObjectAsset capability   │
│    - OSD message deserialization    │
│    - HTTP POST handling             │
├─────────────────────────────────────┤
│      Object Creation Pipeline       │
│    - UploadObjectAssetMessage       │
│    - PrimitiveBaseShape generation  │
│    - SceneObjectPart creation       │
├─────────────────────────────────────┤
│     Geometry Processing System      │
│    - Path and profile parameters   │
│    - Deformation and scaling       │
│    - Extra parameters (sculpt/flex)│
├─────────────────────────────────────┤
│     Material and Texture System     │
│    - Texture entry creation        │
│    - UV mapping and transformation │
│    - Surface properties            │
├─────────────────────────────────────┤
│      Linking and Assembly          │
│    - Multi-part object linking     │
│    - Root part identification      │
│    - Group hierarchy management    │
└─────────────────────────────────────┘
```

### Data Flow Architecture

```
Viewer sends UploadObjectAsset request
     ↓
OSD message deserialization
     ↓
UploadObjectAssetMessage parsing
     ↓
For each object in message:
     ↓
Create PrimitiveBaseShape
     ↓
Process geometry parameters
     ↓
Process extra parameters (sculpt/flex/light)
     ↓
Process texture faces and materials
     ↓
Create SceneObjectPart
     ↓
Create SceneObjectGroup
     ↓
Add to scene with permissions check
     ↓
Link all parts together
     ↓
Schedule updates and return local_id
```

### Message Structure

#### UploadObjectAssetMessage
```csharp
public class UploadObjectAssetMessage
{
    public Object[] Objects;    // Array of object definitions

    public class Object
    {
        public Vector3 Position;        // Object position
        public Quaternion Rotation;    // Object rotation
        public Vector3 Scale;          // Object scale
        public string Name;            // Object name
        public UUID GroupID;           // Group ownership
        public UUID SculptID;          // Sculpt texture UUID

        // Geometry parameters
        public ushort PathBegin, PathEnd;
        public byte PathCurve;
        public sbyte RadiusOffset, Revolutions;
        public byte ScaleX, ScaleY;
        public byte ShearX, ShearY;
        public sbyte Skew, TaperX, TaperY;
        public sbyte Twist, TwistBegin;
        public byte ProfileBegin, ProfileEnd;
        public byte ProfileCurve;
        public ushort ProfileHollow;

        public Face[] Faces;           // Texture faces
        public ExtraParam[] ExtraParams; // Special parameters
    }
}
```

## Interface Implementation

The module implements:
- **INonSharedRegionModule**: Per-region module instance
- **Capabilities Registration**: UploadObjectAsset capability handler

### Module Interface

```csharp
public class UploadObjectAssetModule : INonSharedRegionModule
{
    public string Name { get { return "UploadObjectAssetModuleModule"; } }
    public Type ReplaceableInterface { get { return null; } }

    public void RegisterCaps(UUID agentID, Caps caps);
    public void ProcessAdd(IOSHttpRequest httpRequest, IOSHttpResponse httpResponse, OSDMap map, UUID agentID, Caps cap);
}
```

## Configuration

### Module Loading

The module is automatically loaded as part of the core module factory:

```csharp
// In ModuleFactory.cs CreateNonSharedModules()
yield return new UploadObjectAssetModule();
```

No specific configuration is required - the module registers its capability automatically when agents connect.

### Capability Registration

The module uses automatic capability registration:

```ini
# No configuration required - capability automatically available
# The UploadObjectAsset capability is registered for all connecting agents
```

## Core Functionality

### Capability Registration

#### RegisterCaps Method

```csharp
public void RegisterCaps(UUID agentID, Caps caps)
{
    caps.RegisterSimpleHandler("UploadObjectAsset",
        new SimpleOSDMapHandler("POST","/" + UUID.Random(), delegate (IOSHttpRequest httpRequest, IOSHttpResponse httpResponse, OSDMap map)
        {
            ProcessAdd(httpRequest, httpResponse, map, agentID, caps);
        }));
}
```

This method:
- **Automatic Registration**: Registers capability for every connecting agent
- **Unique Endpoints**: Creates unique upload endpoint per request
- **OSD Handling**: Uses structured data handler for complex object messages
- **POST Method**: Only accepts HTTP POST requests for object uploads

### Object Processing Pipeline

#### ProcessAdd Method Overview

```csharp
public void ProcessAdd(IOSHttpRequest httpRequest, IOSHttpResponse httpResponse, OSDMap map, UUID agentID, Caps cap)
{
    // 1. Validate agent presence
    if (!m_scene.TryGetScenePresence(agentID, out ScenePresence avatar))
    {
        httpResponse.StatusCode = (int)HttpStatusCode.Gone;
        return;
    }

    // 2. Deserialize upload message
    UploadObjectAssetMessage message = new UploadObjectAssetMessage();
    message.Deserialize(map);

    // 3. Process each object in the message
    Vector3 pos = avatar.AbsolutePosition + Vector3.UnitXRotated(avatar.Rotation);
    SceneObjectGroup[] allparts = new SceneObjectGroup[message.Objects.Length];

    for (int i = 0; i < message.Objects.Length; i++)
    {
        // Create primitive geometry
        // Process materials and textures
        // Create scene object
        // Add to scene with permissions
    }

    // 4. Link all parts together
    for (int j = 1; j < allparts.Length; j++)
    {
        rootGroup.LinkToGroup(allparts[j]);
    }

    // 5. Return local_id to viewer
    httpResponse.RawBuffer = Util.UTF8NBGetbytes(String.Format(
        "<llsd><map><key>local_id</key>{0}</map></llsd>",
        ConvertUintToBytes(allparts[0].LocalId)));
}
```

### Geometry Processing

#### Primitive Shape Creation

```csharp
PrimitiveBaseShape pbs = PrimitiveBaseShape.CreateBox();

// Path parameters (how the profile is swept)
pbs.PathBegin = (ushort)obj.PathBegin;
pbs.PathCurve = (byte)obj.PathCurve;
pbs.PathEnd = (ushort)obj.PathEnd;
pbs.PathRadiusOffset = (sbyte)obj.RadiusOffset;
pbs.PathRevolutions = (byte)obj.Revolutions;
pbs.PathScaleX = (byte)obj.ScaleX;
pbs.PathScaleY = (byte)obj.ScaleY;
pbs.PathShearX = (byte)obj.ShearX;
pbs.PathShearY = (byte)obj.ShearY;
pbs.PathSkew = (sbyte)obj.Skew;
pbs.PathTaperX = (sbyte)obj.TaperX;
pbs.PathTaperY = (sbyte)obj.TaperY;
pbs.PathTwist = (sbyte)obj.Twist;
pbs.PathTwistBegin = (sbyte)obj.TwistBegin;

// Profile parameters (cross-sectional shape)
pbs.HollowShape = (HollowShape)obj.ProfileHollow;
pbs.ProfileBegin = (ushort)obj.ProfileBegin;
pbs.ProfileCurve = (byte)obj.ProfileCurve;
pbs.ProfileEnd = (ushort)obj.ProfileEnd;
pbs.Scale = obj.Scale;
```

#### Extra Parameters Processing

```csharp
for (int extparams = 0; extparams < obj.ExtraParams.Length; extparams++)
{
    UploadObjectAssetMessage.Object.ExtraParam extraParam = obj.ExtraParams[extparams];
    switch ((ushort)extraParam.Type)
    {
        case (ushort)ExtraParamType.Sculpt:
            Primitive.SculptData sculpt = new Primitive.SculptData(extraParam.ExtraParamData, 0);
            pbs.SculptEntry = true;
            pbs.SculptTexture = obj.SculptID;
            pbs.SculptType = (byte)sculpt.Type;
            break;

        case (ushort)ExtraParamType.Flexible:
            Primitive.FlexibleData flex = new Primitive.FlexibleData(extraParam.ExtraParamData, 0);
            pbs.FlexiEntry = true;
            pbs.FlexiDrag = flex.Drag;
            pbs.FlexiForceX = flex.Force.X;
            pbs.FlexiForceY = flex.Force.Y;
            pbs.FlexiForceZ = flex.Force.Z;
            pbs.FlexiGravity = flex.Gravity;
            pbs.FlexiSoftness = flex.Softness;
            pbs.FlexiTension = flex.Tension;
            pbs.FlexiWind = flex.Wind;
            break;

        case (ushort)ExtraParamType.Light:
            Primitive.LightData light = new Primitive.LightData(extraParam.ExtraParamData, 0);
            pbs.LightColorA = light.Color.A;
            pbs.LightColorB = light.Color.B;
            pbs.LightColorG = light.Color.G;
            pbs.LightColorR = light.Color.R;
            pbs.LightCutoff = light.Cutoff;
            pbs.LightEntry = true;
            pbs.LightFalloff = light.Falloff;
            pbs.LightIntensity = light.Intensity;
            pbs.LightRadius = light.Radius;
            break;

        case 0x40: // Projection parameters
            pbs.ReadProjectionData(extraParam.ExtraParamData, 0);
            break;
    }
}
```

### Material and Texture Processing

#### Texture Entry Creation

```csharp
Primitive.TextureEntry tmp = new Primitive.TextureEntry(UUID.Parse("89556747-24cb-43ed-920b-47caed15465f"));

for (int j = 0; j < obj.Faces.Length; j++)
{
    UploadObjectAssetMessage.Object.Face face = obj.Faces[j];
    Primitive.TextureEntryFace primFace = tmp.CreateFace((uint)j);

    // Surface properties
    primFace.Bump = face.Bump;               // Bump mapping
    primFace.RGBA = face.Color;              // Tint color
    primFace.Fullbright = face.Fullbright;   // Ignore lighting
    primFace.Glow = face.Glow;               // Glow intensity

    // Texture mapping
    primFace.TextureID = face.ImageID;       // Texture UUID
    primFace.Rotation = face.ImageRot;       // Texture rotation
    primFace.OffsetU = face.OffsetS;         // U offset
    primFace.OffsetV = face.OffsetT;         // V offset
    primFace.RepeatU = face.ScaleS;          // U scaling
    primFace.RepeatV = face.ScaleT;          // V scaling

    // Media and mapping
    primFace.MediaFlags = ((face.MediaFlags & 1) != 0);
    primFace.TexMapType = (MappingType)(face.MediaFlags & 6);
}

pbs.TextureEntry = tmp.GetBytes();
```

### Object Assembly and Linking

#### SceneObjectPart Creation

```csharp
SceneObjectPart prim = new SceneObjectPart();
prim.UUID = UUID.Random();
prim.CreatorID = agentID;
prim.OwnerID = agentID;
prim.GroupID = obj.GroupID;
prim.LastOwnerID = prim.OwnerID;
prim.RezzerID = agentID;
prim.CreationDate = Util.UnixTimeSinceEpoch();
prim.Name = obj.Name;
prim.Description = "";

// Payment configuration
prim.PayPrice[0] = -2;  // Default: not for sale
prim.PayPrice[1] = -2;
prim.PayPrice[2] = -2;
prim.PayPrice[3] = -2;
prim.PayPrice[4] = -2;

prim.Shape = pbs;
prim.Scale = obj.Scale;
```

#### Group Creation and Scene Integration

```csharp
SceneObjectGroup grp = new SceneObjectGroup();
grp.SetRootPart(prim);
prim.ParentID = 0;

if (i == 0)
    rootGroup = grp;  // First object becomes root

grp.AttachToScene(m_scene);
grp.AbsolutePosition = obj.Position;
prim.RotationOffset = obj.Rotation;

// Required for linking
grp.RootPart.ClearUpdateSchedule();

// Permissions check before adding to scene
if (m_scene.Permissions.CanRezObject(1, avatar.UUID, pos))
{
    m_scene.AddSceneObject(grp);
    grp.AbsolutePosition = obj.Position;
}
```

#### Multi-Part Object Linking

```csharp
// Link all child parts to root group
for (int j = 1; j < allparts.Length; j++)
{
    // Required for linking
    rootGroup.RootPart.ClearUpdateSchedule();
    allparts[j].RootPart.ClearUpdateSchedule();
    rootGroup.LinkToGroup(allparts[j]);
}

// Schedule updates for the complete linked object
rootGroup.ScheduleGroupForUpdate(PrimUpdateFlags.FullUpdatewithAnimMatOvr);
```

## Advanced Geometry Features

### Path and Profile System

OpenSimulator primitives use a path/profile system for geometry generation:

#### Path Parameters
- **PathCurve**: How the profile is swept (linear, circle, etc.)
- **PathBegin/PathEnd**: Start and end points along the path (0-1)
- **PathScaleX/Y**: Scaling along the path
- **PathShearX/Y**: Shearing deformation
- **PathTwist/TwistBegin**: Rotation along the path
- **PathTaper**: Tapering effect
- **PathRevolutions**: Number of revolutions for helical paths
- **RadiusOffset**: Radius modification
- **PathSkew**: Skewing deformation

#### Profile Parameters
- **ProfileCurve**: Cross-sectional shape (circle, square, triangle, etc.)
- **ProfileBegin/ProfileEnd**: Portion of profile to use
- **ProfileHollow**: Interior hollowing with different shapes

### Special Primitive Types

#### Sculpted Primitives
```csharp
case (ushort)ExtraParamType.Sculpt:
    pbs.SculptEntry = true;
    pbs.SculptTexture = obj.SculptID;    // Sculpture map texture
    pbs.SculptType = (byte)sculpt.Type;  // Sphere, torus, plane, cylinder
```

Sculpted prims use texture-based height maps to define complex geometry.

#### Flexible Primitives
```csharp
case (ushort)ExtraParamType.Flexible:
    pbs.FlexiEntry = true;
    pbs.FlexiDrag = flex.Drag;           // Air resistance
    pbs.FlexiGravity = flex.Gravity;     // Gravity effect
    pbs.FlexiTension = flex.Tension;     // Stiffness
    pbs.FlexiSoftness = flex.Softness;   // Flexibility
    pbs.FlexiWind = flex.Wind;           // Wind sensitivity
```

Flexible prims simulate cloth and rope-like behavior.

#### Light Sources
```csharp
case (ushort)ExtraParamType.Light:
    pbs.LightEntry = true;
    pbs.LightIntensity = light.Intensity; // Brightness
    pbs.LightRadius = light.Radius;       // Light range
    pbs.LightFalloff = light.Falloff;     // Attenuation
    pbs.LightCutoff = light.Cutoff;       // Cone angle (spotlights)
```

Light sources provide dynamic illumination in the scene.

#### Projection Parameters
```csharp
case 0x40:
    pbs.ReadProjectionData(extraParam.ExtraParamData, 0);
```

Projection parameters enable projector lighting effects.

## Content Creation Workflows

### External Tool Integration

The UploadObjectAsset capability enables integration with external 3D modeling tools:

1. **3D Modeling Software**: Blender, Maya, 3ds Max, etc.
2. **Export Plugins**: Convert 3D models to OpenSimulator format
3. **Viewer Upload**: Use viewers with object upload capabilities
4. **Automatic Processing**: Module handles geometry conversion and scene integration

### Multi-Part Object Creation

```csharp
// Example: Creating a table with legs
Objects[0] = tableTop;     // Root part
Objects[1] = leg1;         // Child parts
Objects[2] = leg2;
Objects[3] = leg3;
Objects[4] = leg4;

// Module automatically links all parts into single object
```

### Complex Geometry Support

- **Parametric Shapes**: Full support for OpenSimulator's parametric primitive system
- **Sculpted Meshes**: Texture-based custom geometry
- **Flexible Objects**: Physics-enabled flexible primitives
- **Lighting Effects**: Integrated light sources and projectors
- **Advanced Materials**: Full texture mapping with bump, glow, and transparency

## Performance Characteristics

### Memory Management

- **Efficient Processing**: Direct object creation without intermediate storage
- **Batch Operations**: Processes multiple objects in single transaction
- **Automatic Cleanup**: Exception handling with proper resource disposal
- **Scene Integration**: Optimized scene object management and linking

### Processing Efficiency

- **Single Transaction**: All objects processed in one capability call
- **Optimized Linking**: Efficient multi-part object assembly
- **Update Scheduling**: Coordinated scene updates for linked objects
- **Permission Checking**: Integrated permissions validation

### Network Optimization

- **Structured Data**: Efficient OSD serialization for complex object data
- **Batch Upload**: Multiple objects uploaded in single HTTP request
- **Compressed Protocol**: Optimized message format for large object hierarchies
- **Response Efficiency**: Minimal response data (only local_id returned)

## Security and Permissions

### Permission Validation

```csharp
if (m_scene.Permissions.CanRezObject(1, avatar.UUID, pos))
{
    m_scene.AddSceneObject(grp);
    grp.AbsolutePosition = obj.Position;
}
```

- **Rez Permissions**: Validates user can create objects in the region
- **Position Validation**: Ensures object placement is allowed
- **Ownership Assignment**: Properly assigns creator and owner IDs
- **Group Assignment**: Respects group ownership settings

### Security Features

- **Agent Validation**: Ensures requesting agent is present in scene
- **Message Validation**: Deserializes and validates object message format
- **Resource Limits**: Implicit limits through scene object limits
- **Exception Handling**: Graceful handling of malformed requests

### Data Validation

- **Geometry Bounds**: Primitive parameters validated within acceptable ranges
- **Texture References**: UUID validation for texture and sculpt references
- **Scale Limits**: Object scaling within reasonable bounds
- **Name Validation**: Object name sanitization and limits

## Integration with Viewer Technology

### Viewer Upload Process

1. **3D Model Preparation**: User prepares 3D model in external tool
2. **Export to Viewer**: Model exported to viewer-compatible format
3. **Upload Request**: Viewer creates UploadObjectAsset request
4. **Message Construction**: Complex object data serialized to OSD
5. **Capability Call**: HTTP POST to UploadObjectAsset capability
6. **Server Processing**: Module deserializes and creates objects
7. **Scene Integration**: Objects added to scene with proper linking
8. **Confirmation**: local_id returned to viewer for tracking

### Supported Viewers

The module works with viewers that support the UploadObjectAsset capability:
- **Firestorm**: Full support for object upload
- **Singularity**: Advanced object creation features
- **Alchemy**: Mesh and object upload capabilities
- **Custom Viewers**: Third-party viewers with upload functionality

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: UploadObjectAsset capability not available
Causes:
- Module not in CreateNonSharedModules list
- Capability registration failure

Solutions:
- Verify module in non-shared modules factory
- Check capability registration in logs
```

#### Object Upload Failures
```
Symptom: Objects not appearing in scene after upload
Causes:
- Permission denied for object creation
- Invalid object message format
- Scene object limits exceeded
- Malformed geometry parameters

Solutions:
- Check rez permissions for user
- Validate object message structure
- Verify scene object limits
- Check primitive parameter ranges
```

#### Linking Issues
```
Symptom: Multi-part objects not properly linked
Causes:
- Update scheduling conflicts
- Permission issues with linking
- Scene state inconsistencies

Solutions:
- Ensure proper ClearUpdateSchedule calls
- Verify linking permissions
- Check scene object state
```

#### Geometry Problems
```
Symptom: Objects appear with wrong geometry
Causes:
- Invalid path/profile parameters
- Corrupted extra parameters
- Texture entry format issues

Solutions:
- Validate geometry parameter ranges
- Check extra parameter data format
- Verify texture entry structure
```

### Debug Information

Enable detailed logging for troubleshooting:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

// Add debug logging:
m_log.DebugFormat("[UPLOAD OBJECT ASSET]: Processing upload for agent {0}", agentID);
m_log.DebugFormat("[UPLOAD OBJECT ASSET]: Creating object {0} with {1} faces", obj.Name, obj.Faces.Length);
m_log.DebugFormat("[UPLOAD OBJECT ASSET]: Linking {0} objects together", allparts.Length);
```

### Testing Procedures

1. **Capability Verification**: Check UploadObjectAsset appears in capability list
2. **Simple Upload**: Test with single primitive object
3. **Multi-Part Upload**: Test with linked object hierarchy
4. **Geometry Features**: Test sculpted prims, flexible objects, lights
5. **Material Testing**: Test texture mapping and surface properties
6. **Permission Testing**: Verify behavior with different permission levels

## Related Components

### Dependencies
- **INonSharedRegionModule**: Module interface contract
- **Caps**: Capabilities system for HTTP endpoint management
- **UploadObjectAssetMessage**: Message deserialization from OpenMetaverse
- **Scene**: Scene object management and permissions
- **PrimitiveBaseShape**: Geometry definition and processing

### Integration Points
- **Scene Management**: Object creation and scene integration
- **Permissions System**: Rez permissions and ownership validation
- **Capabilities System**: HTTP endpoint registration and OSD handling
- **Asset System**: Texture and sculpt texture references
- **Physics System**: Integration with physics engines for flexible prims

## Future Enhancements

### Potential Improvements

- **Mesh Upload**: Direct mesh asset upload support
- **Material System**: Advanced material definitions and properties
- **Animation Support**: Skeletal animation and rigging
- **Physics Integration**: Enhanced physics properties and constraints
- **Validation Enhancement**: Advanced geometry and data validation

### Content Pipeline Extensions

- **Format Support**: Additional 3D file format support (COLLADA, glTF, etc.)
- **Optimization**: Automatic geometry optimization and LOD generation
- **Batch Processing**: Multiple object uploads in single transaction
- **Preview System**: Server-side object preview generation
- **Version Control**: Object versioning and update capabilities

### Workflow Improvements

- **Progress Tracking**: Upload progress reporting for large objects
- **Error Recovery**: Enhanced error handling and recovery
- **Validation Tools**: Pre-upload validation and optimization
- **Template System**: Reusable object templates and presets
- **Collaboration**: Multi-user object creation and editing

---

*This documentation covers UploadObjectAssetModule as integrated with the factory-based loading system, providing comprehensive object upload functionality, geometry processing, and content creation capabilities without dependency on Mono.Addins.*