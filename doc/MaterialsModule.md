# MaterialsModule

## Overview

The MaterialsModule is a comprehensive materials management system for OpenSimulator/Akisim that provides advanced Physically Based Rendering (PBR) materials support and glTF compatibility. It enables sophisticated material rendering capabilities including normal maps, specular maps, transparency effects, and modern PBR workflows, bringing high-quality visual rendering to virtual worlds.

## Architecture

The MaterialsModule implements multiple interfaces:
- `INonSharedRegionModule` - Per-region module instance management
- `IMaterialsModule` - Materials management interface for external access

### Key Components

1. **PBR Materials System**
   - **Face Materials**: Advanced material properties per object face
   - **Texture Management**: Multi-texture support (diffuse, normal, specular, emissive)
   - **Material Properties**: Metallic/roughness, alpha modes, emission factors
   - **Reference Counting**: Efficient memory management through reference counting

2. **Capability System**
   - **RenderMaterials Capability**: HTTP endpoint for material data exchange
   - **ModifyMaterialParams Capability**: Advanced material parameter modification
   - **Compression Support**: Zlib compression for efficient data transfer
   - **Caching System**: Intelligent caching of material data for performance

3. **glTF Integration**
   - **glTF JSON Processing**: Native glTF material format support
   - **Material Overrides**: Per-face material parameter overrides
   - **Texture Transform**: Advanced texture transformation support
   - **PBR Standard Compliance**: Full PBR metallic-roughness workflow

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
MaterialsModule = true
```

### Materials Configuration

Configuration is handled through the `[Materials]` section:

```ini
[Materials]
; Enable materials support (default: true)
enable_materials = true

; Maximum materials per transaction to prevent abuse
MaxMaterialsPerTransaction = 50
```

### Configuration Options

- **enable_materials**: Master switch for materials functionality
- **MaxMaterialsPerTransaction**: Limits the number of materials that can be processed in a single request

## Features

### Advanced Material Properties

The module supports comprehensive PBR material properties:

1. **Base Color**: Diffuse color and transparency
2. **Metallic Factor**: Controls metallic vs dielectric surface response
3. **Roughness Factor**: Surface roughness for reflection characteristics
4. **Normal Maps**: Surface detail through normal mapping
5. **Emissive Properties**: Self-illuminated surface areas
6. **Alpha Modes**: Opaque, mask, and blend transparency modes

### Texture Support

1. **Multi-texture Materials**: Up to 4 textures per material
   - Base Color Texture (Diffuse)
   - Normal Map Texture
   - Metallic-Roughness Texture
   - Occlusion Texture
   - Emissive Texture

2. **Texture Transformations**: Advanced texture manipulation
   - Offset (UV translation)
   - Rotation (UV rotation)
   - Scale (UV scaling)

### Performance Features

1. **Reference Counting**: Efficient memory management
2. **Material Caching**: Intelligent caching with automatic expiration
3. **Compression**: Zlib compression for network efficiency
4. **Background Processing**: Asynchronous material storage and cleanup

## Technical Implementation

### Material Storage and Retrieval

#### Material Creation Process

1. **Material Definition**: Define material properties and textures
2. **ID Generation**: Generate unique material ID based on properties
3. **Reference Management**: Track material usage across objects
4. **Asset Storage**: Store material as asset in the asset service
5. **Cache Management**: Cache material for efficient retrieval

#### Material Application Process

1. **Face Identification**: Identify target object face
2. **Permission Check**: Verify modification permissions
3. **Material Assignment**: Apply material ID to face texture entry
4. **Reference Update**: Update material reference counting
5. **Object Update**: Trigger object update to viewers

### Capability Endpoints

#### RenderMaterials Capability

**GET Request**: Retrieve all materials in the region
- Returns compressed list of all available materials
- Cached response with 30-second expiration
- Binary compressed format for efficiency

**POST Request**: Request specific materials by ID
- Accept list of material IDs
- Return requested material data
- Handle missing materials gracefully

**PUT Request**: Upload/modify materials
- Process material definitions from viewer
- Validate permissions and object ownership
- Update object texture entries
- Manage reference counting

#### ModifyMaterialParams Capability

**POST Request**: Modify PBR material parameters
- Accept glTF JSON material definitions
- Process material overrides per face
- Handle texture transformations
- Update render materials on objects

### glTF Integration

The module provides comprehensive glTF material support:

#### Material Properties Processing

```json
{
  "materials": [{
    "pbrMetallicRoughness": {
      "baseColorFactor": [1.0, 1.0, 1.0, 1.0],
      "metallicFactor": 0.0,
      "roughnessFactor": 1.0,
      "baseColorTexture": {
        "index": 0,
        "extensions": {
          "KHR_texture_transform": {
            "offset": [0.0, 0.0],
            "rotation": 0.0,
            "scale": [1.0, 1.0]
          }
        }
      }
    },
    "normalTexture": {"index": 1},
    "emissiveTexture": {"index": 2},
    "emissiveFactor": [0.0, 0.0, 0.0],
    "alphaMode": "OPAQUE",
    "alphaCutoff": 0.5,
    "doubleSided": false
  }]
}
```

## API Methods

### Core Material Management

- `GetMaterial(UUID ID)` - Retrieve material by ID
- `GetMaterialCopy(UUID ID)` - Get copy of material for modification
- `AddNewMaterial(FaceMaterial fm)` - Add new material to system
- `RemoveMaterial(UUID id)` - Remove material and update references

### Material Properties

Materials support the following properties:

- **DiffuseAlphaMode**: Alpha rendering mode (0=opaque, 1=blend, 2=mask)
- **AlphaCutoff**: Alpha cutoff value for mask mode
- **NormalMapID**: Normal map texture UUID
- **SpecularMapID**: Specular/metallic-roughness map UUID
- **SpecularColor**: Specular tint color
- **SpecularMapOffsetX/Y**: Texture offset coordinates
- **SpecularMapRepeatX/Y**: Texture repeat values
- **SpecularMapRotation**: Texture rotation angle
- **EnvironmentIntensity**: Environment map intensity

### Capability Handlers

- `RenderMaterialsGetCap()` - Handle GET requests for materials
- `RenderMaterialsPostCap()` - Handle POST requests for specific materials
- `RenderMaterialsPutCap()` - Handle PUT requests for material updates
- `ModifyMaterialParams()` - Handle advanced material parameter modifications

## Usage Examples

### Basic Material Creation

```csharp
// Create new face material
FaceMaterial material = new FaceMaterial();
material.DiffuseAlphaMode = 0; // Opaque
material.NormalMapID = normalTextureUUID;
material.SpecularMapID = metallicRoughnessUUID;
material.SpecularColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

// Add to materials system
UUID materialID = materialsModule.AddNewMaterial(material);

// Apply to object face
textureEntry.CreateFace(faceIndex).MaterialID = materialID;
```

### glTF Material Override

```csharp
// Example glTF material override JSON
string glTFMaterial = @"{
  ""materials"": [{
    ""pbrMetallicRoughness"": {
      ""baseColorFactor"": [0.8, 0.8, 0.8, 1.0],
      ""metallicFactor"": 0.5,
      ""roughnessFactor"": 0.3
    },
    ""alphaMode"": ""BLEND""
  }]
}";

// Apply via ModifyMaterialParams capability
// (typically called through viewer HTTP requests)
```

## Performance Characteristics

### Memory Management

- **Reference Counting**: Automatic cleanup of unused materials
- **Delayed Deletion**: Batch cleanup of expired materials
- **Cache Expiration**: 30-second cache for GET requests
- **Asset Integration**: Leverage asset service caching

### Network Optimization

- **Compression**: Zlib compression for all material data
- **Caching**: Intelligent caching to reduce redundant transfers
- **Batch Operations**: Support for bulk material operations
- **Delta Updates**: Only transfer changed material data

### Storage Efficiency

- **Background Storage**: Non-blocking material asset storage
- **Reference Tracking**: Efficient tracking of material usage
- **Cleanup Throttling**: Controlled cleanup to prevent performance spikes
- **Legacy Migration**: Automatic migration from legacy material storage

## Security Features

### Access Control

- **Permission Validation**: Verify edit permissions before material changes
- **Object Ownership**: Respect object ownership for material modifications
- **Agent Authentication**: Validate agent identity for all operations
- **Transaction Limits**: Prevent abuse through transaction size limits

### Data Validation

- **Input Sanitization**: Validate all material property inputs
- **Format Validation**: Ensure proper glTF format compliance
- **Range Checking**: Validate numeric values within acceptable ranges
- **UUID Validation**: Verify texture UUIDs and references

## Integration Points

### With Asset Services

- **Material Storage**: Store materials as assets in asset service
- **Texture Management**: Integrate with texture asset management
- **Cache Integration**: Leverage asset service caching mechanisms
- **Cleanup Coordination**: Coordinate cleanup with asset expiration

### With Object System

- **Texture Entry Integration**: Seamless integration with object texture entries
- **Face Management**: Per-face material assignment and management
- **Update Notifications**: Trigger appropriate object updates
- **Change Events**: Generate script change events for material modifications

### With Viewer Capabilities

- **HTTP Endpoints**: Provide standardized HTTP capability endpoints
- **Compression Support**: Efficient data transfer with compression
- **Format Compatibility**: Full compatibility with viewer material formats
- **Feature Negotiation**: Advertise material capabilities to viewers

## Debugging and Troubleshooting

### Common Issues

1. **Materials Not Displaying**: Check viewer PBR support and material validity
2. **Performance Issues**: Monitor cache hit rates and material reference counts
3. **Storage Problems**: Verify asset service connectivity and storage capacity
4. **Permission Errors**: Check object edit permissions and ownership

### Diagnostic Tools

1. **Material Inspection**: Tools for examining material properties and references
2. **Cache Analysis**: Monitor cache performance and hit rates
3. **Reference Tracking**: Analyze material reference patterns
4. **Performance Metrics**: Monitor material processing performance

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
MaterialsModule = true

[Materials]
enable_materials = true
MaxMaterialsPerTransaction = 10  # Reduced for debugging
```

## Use Cases

### Advanced Rendering

- **PBR Workflows**: Full physically based rendering support
- **Architectural Visualization**: High-quality material rendering for buildings
- **Product Visualization**: Realistic material representation for objects
- **Artistic Applications**: Creative material effects and combinations

### Content Creation

- **Builder Tools**: Advanced materials for content creators
- **Import/Export**: glTF material compatibility for content workflows
- **Texture Workflows**: Multi-texture material creation
- **Visual Effects**: Special materials for visual effects and lighting

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Legacy Material Migration

The module automatically handles legacy material migration:

- **DynAttrs Migration**: Converts old DynAttrs-based materials
- **Reference Reconstruction**: Rebuilds material reference counts
- **Format Conversion**: Converts legacy formats to current standards
- **Cleanup Integration**: Integrates with modern cleanup systems

### Deployment Considerations

- **Asset Service**: Ensure asset service supports material assets
- **Viewer Compatibility**: Verify viewer PBR support
- **Performance Planning**: Plan for material cache and storage requirements
- **Feature Testing**: Test material features with target viewer versions

## Configuration Examples

### Basic Materials Setup

```ini
[Modules]
MaterialsModule = true

[Materials]
enable_materials = true
MaxMaterialsPerTransaction = 50
```

### Performance-Optimized Setup

```ini
[Modules]
MaterialsModule = true

[Materials]
enable_materials = true
MaxMaterialsPerTransaction = 100

[AssetCache]
MemoryCacheEnabled = true
FileCacheEnabled = true
```

### Development/Testing Setup

```ini
[Modules]
MaterialsModule = true

[Materials]
enable_materials = true
MaxMaterialsPerTransaction = 10

[Logging]
LogLevel = DEBUG
```

## Best Practices

### Performance Optimization

1. **Material Reuse**: Encourage reuse of similar materials
2. **Cache Monitoring**: Monitor cache hit rates and performance
3. **Reference Management**: Keep track of material reference patterns
4. **Cleanup Tuning**: Optimize cleanup frequency for your environment

### Content Guidelines

1. **Texture Optimization**: Use appropriate texture sizes and formats
2. **Material Complexity**: Balance visual quality with performance
3. **glTF Standards**: Follow glTF best practices for materials
4. **Testing**: Test materials across different viewers and settings

### Operational Practices

1. **Monitoring**: Monitor material usage and performance metrics
2. **Maintenance**: Regular cleanup and optimization of material storage
3. **Updates**: Keep up with PBR and glTF standard developments
4. **Documentation**: Document custom material workflows and standards

## Future Enhancements

### Potential Improvements

1. **Extended PBR Features**: Additional PBR material properties
2. **Performance Optimization**: Enhanced caching and compression
3. **glTF Extensions**: Support for additional glTF extensions
4. **Batch Operations**: More efficient bulk material operations

### Compatibility Considerations

1. **Viewer Evolution**: Stay current with viewer PBR developments
2. **Standard Updates**: Adapt to evolving glTF and PBR standards
3. **Performance Scaling**: Enhanced scalability for larger deployments
4. **Integration**: Improved integration with content creation tools