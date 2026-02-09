# VegetationModule Technical Documentation

## Overview

The **VegetationModule** is a core OpenSimulator module responsible for creating and managing vegetation objects (trees and grass) within regions. It provides essential functionality for vegetation creation, phantom object handling, and integration with the scene's entity creation system.

## Architecture and Interfaces

### Core Interfaces
- **INonSharedRegionModule**: Per-region instance module lifecycle
- **IVegetationModule**: Vegetation-specific functionality interface
- **IEntityCreator**: Generic entity creation interface

### Key Components
- **Tree Creation**: Supports both legacy Tree and NewTree PCode types
- **Grass Creation**: Grass object creation with PCode.Grass
- **Phantom Objects**: Automatic phantom flag assignment for vegetation
- **Scale Adaptation**: Automatic tree scaling based on tree type
- **Scene Integration**: Full integration with OpenSim scene management

## Vegetation Types and PCodes

### Supported PCode Types
The module handles three specific primitive code types for vegetation:

1. **PCode.Grass**: Grass objects for ground vegetation
2. **PCode.Tree**: Legacy tree objects (older format)
3. **PCode.NewTree**: Modern tree objects (newer format)

### Tree Types and Scaling
The module implements intelligent scaling for different tree varieties:

#### Cypress Trees
- **Tree.Cypress1**: Scale multiplier (8, 8, 20) - tall, narrow profile
- **Tree.Cypress2**: Scale multiplier (8, 8, 20) - tall, narrow profile

#### Default Trees
- **All Other Types**: Scale multiplier (8, 8, 8) - standard cubic scaling

## Vegetation Creation System

### Tree Creation API
The primary tree creation method provides comprehensive control:

```csharp
SceneObjectGroup AddTree(
    UUID uuid,           // Unique identifier for the tree object
    UUID groupID,        // Group ownership identifier
    Vector3 scale,       // Initial scale before type-based adaptation
    Quaternion rotation, // Tree orientation
    Vector3 position,    // World position
    Tree treeType,       // Specific tree variety (affects scaling)
    bool newTree        // Use NewTree PCode (true) or legacy Tree PCode (false)
)
```

### Entity Creation API
Generic entity creation for all vegetation types:

```csharp
SceneObjectGroup CreateEntity(
    UUID ownerID,              // Object owner
    UUID groupID,              // Group ownership
    Vector3 pos,               // World position
    Quaternion rot,            // Object rotation
    PrimitiveBaseShape shape   // Primitive shape definition with PCode
)
```

## Object Properties and Behavior

### Phantom Objects
All vegetation objects created by this module are automatically configured as phantom:
- **PrimFlags.Phantom**: Objects do not collide with avatars or vehicles
- **Physics Integration**: Vegetation does not interfere with movement
- **Performance**: Reduces physics calculation overhead

### Tree Shape Configuration
Trees are created with specific primitive shape properties:
- **PathCurve**: Set to 16 for proper tree rendering
- **PathEnd**: Set to 49900 for complete path rendering
- **PCode**: Tree or NewTree based on newTree parameter
- **State**: Tree type stored in shape state for variety identification

### Grass Behavior
Grass objects maintain simpler configuration:
- **No Scale Adaptation**: Grass uses provided scale directly
- **Phantom Flag**: Applied automatically like trees
- **Ground Coverage**: Designed for surface vegetation placement

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource source)
```
- **No Configuration**: Module requires no specific configuration
- **Lightweight Setup**: Minimal initialization overhead

### Region Integration
```csharp
public void AddRegion(Scene scene)
```
- **Scene Registration**: Registers IVegetationModule interface
- **Service Availability**: Makes vegetation services available to other modules

### Region Cleanup
```csharp
public void RemoveRegion(Scene scene)
```
- **Interface Cleanup**: Unregisters module interface
- **Resource Cleanup**: Ensures proper module shutdown

## Integration Points

### Scene Integration
- **Scene.AddNewPrim**: Direct integration with scene primitive creation
- **Scene.AddNewSceneObject**: Full scene object integration with permissions
- **Module Interface**: Available to other modules via scene.RequestModuleInterface

### Permissions System
- **Object Ownership**: Full support for owner and group assignment
- **Permission Validation**: Integration with scene permission systems
- **Deep Permissions**: Calls InvalidateDeepEffectivePerms for proper permission cascade

### Viewer Integration
- **PCode Support**: Direct viewer support for Tree, NewTree, and Grass PCodes
- **Rendering**: Native viewer rendering for vegetation objects
- **LOD System**: Automatic level-of-detail support through viewer

## Performance Considerations

### Phantom Objects
- **Physics Optimization**: Phantom objects reduce physics calculation load
- **Collision Avoidance**: No collision detection required for vegetation
- **Scene Performance**: Minimal impact on scene simulation

### Memory Management
- **Lightweight Objects**: Vegetation objects have minimal memory footprint
- **Efficient Creation**: Fast object creation with minimal shape data
- **Scale Adaptation**: Local scaling calculations without external dependencies

## Configuration

### Module Configuration
```ini
[Modules]
; Enable/disable VegetationModule (default: true)
VegetationModule = true
```

### No Additional Configuration
The VegetationModule requires no additional configuration sections and operates with default settings that are suitable for all deployment scenarios.

## Administrative Features

### Programmatic Creation
- **Module Interface Access**: Other modules can create vegetation programmatically
- **Batch Creation**: Support for creating multiple vegetation objects efficiently
- **Position Management**: Precise positioning and rotation control

### Land Management Integration
- **Land Ownership**: Respects land ownership for vegetation placement
- **Group Management**: Supports group-owned vegetation objects
- **Permission Control**: Integrates with OpenSim permission systems

## Usage Examples

### Basic Tree Creation
```csharp
IVegetationModule vegetation = scene.RequestModuleInterface<IVegetationModule>();
SceneObjectGroup tree = vegetation.AddTree(
    UUID.Random(),           // Unique ID
    UUID.Zero,              // No group
    Vector3.One,            // Standard scale
    Quaternion.Identity,    // No rotation
    new Vector3(128, 128, 25), // Center of region, 25m height
    Tree.Cypress1,          // Cypress tree type
    true                    // Use NewTree PCode
);
```

### Generic Vegetation Creation
```csharp
PrimitiveBaseShape grassShape = new PrimitiveBaseShape();
grassShape.PCode = (byte)PCode.Grass;
grassShape.Scale = new Vector3(2, 2, 1); // 2x2m grass patch

SceneObjectGroup grass = vegetation.CreateEntity(
    ownerUUID,
    UUID.Zero,
    grassPosition,
    Quaternion.Identity,
    grassShape
);
```

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Configuration-based Loading**: Enabled/disabled via OpenSim.ini configuration
- **Default Behavior**: Loaded by default due to essential vegetation functionality
- **Logging Integration**: Comprehensive debug and info logging for operations

### Dependencies
- **Core Framework**: OpenSim.Framework for basic functionality
- **Scene Management**: OpenSim.Region.Framework.Scenes for region integration
- **OpenMetaverse**: Tree enum and Vector3/Quaternion mathematics
- **Interface System**: IVegetationModule and IEntityCreator interfaces

## Security Considerations

### Object Creation Control
- **Owner Validation**: Proper owner assignment and validation
- **Permission Integration**: Respects scene permission systems
- **Group Management**: Secure group ownership handling

### Phantom Object Safety
- **No Collision**: Phantom objects cannot be used to block movement
- **Performance Safety**: Phantom flag prevents physics abuse
- **Automatic Configuration**: No user control over phantom setting prevents exploitation

## Development Integration

### Module Interface Usage
Other modules can access vegetation functionality through the standard module interface pattern:

```csharp
IVegetationModule vegModule = scene.RequestModuleInterface<IVegetationModule>();
if (vegModule != null)
{
    // Use vegetation creation functionality
}
```

### Entity Creator Integration
The module participates in the entity creation system, allowing scene components to query creation capabilities and delegate appropriate object creation to the vegetation module.

This documentation reflects the VegetationModule implementation in `src/OpenSim.Region.CoreModules/World/Vegetation/VegetationModule.cs` and its integration with the factory-based module loading system.