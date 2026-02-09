# AssetInfoModule

## Overview

The AssetInfoModule is a diagnostic and inspection tool for OpenSimulator/Akisim that provides console commands for examining and debugging assets. It allows administrators and developers to inspect asset metadata, content, and properties directly from the OpenSim console, making it an essential tool for troubleshooting asset-related issues and understanding asset structure.

## Architecture

The AssetInfoModule implements the `ISharedRegionModule` interface and provides:
- Console command registration and handling
- Asset service integration for asset retrieval
- Asset metadata inspection capabilities
- Binary asset content examination tools

### Key Components

1. **Console Command Integration**
   - Integration with OpenSim's main console system
   - Command registration during region loading
   - Parameter validation and error handling

2. **Asset Service Interface**
   - Direct integration with the region's asset service
   - Support for all asset types and formats
   - Efficient asset retrieval and caching

3. **Asset Analysis Tools**
   - Metadata extraction and display
   - Binary content inspection with hex dump
   - Asset type identification and validation
   - Content export capabilities

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
AssetInfoModule = true
```

The module is disabled by default and must be explicitly enabled. This is intentional as it's primarily a development and debugging tool.

### No Additional Configuration Required

The AssetInfoModule does not require any additional configuration sections. It automatically:
- Discovers the asset service through the scene interface
- Registers console commands when regions are loaded
- Uses the existing asset service configuration

## Features

### Asset Inspection Commands

The module provides two primary console commands:

#### 1. Asset Information Display (`show asset`)

**Syntax**: `show asset <ID>`

**Purpose**: Display comprehensive metadata and basic content information about an asset

**Output Includes**:
- Asset name and description
- Asset type (both numeric and descriptive)
- Content type (MIME type)
- Asset size in bytes
- Temporary flag status
- Asset metadata flags
- First 80 bytes of content in hexadecimal format

#### 2. Asset Content Export (`dump asset`)

**Syntax**: `dump asset <ID>`

**Purpose**: Export the complete binary content of an asset to a file

**Features**:
- Exports raw asset data to a file named with the asset ID
- Prevents overwriting existing files
- Binary-safe export for all asset types
- Useful for extracting textures, sounds, and other binary assets

### Asset Type Support

The module supports all OpenSimulator asset types:

- **Texture (0)**: Image assets (JPEG2000, TGA, JPEG)
- **Sound (1)**: Audio assets (WAV, OGG)
- **CallingCard (2)**: User calling card references
- **Landmark (3)**: Location bookmarks
- **Clothing (5)**: Avatar clothing items
- **Object (6)**: 3D objects and primitives
- **Notecard (7)**: Text documents
- **LSLText (10)**: LSL script source code
- **BodyPart (12)**: Avatar body parts
- **Animation (13, 20)**: Avatar animations
- **SoundWAV (17)**: WAV audio format
- **ImageTGA (18)**: TGA image format
- **ImageJPEG (19)**: JPEG image format
- **Gesture (21)**: Avatar gestures
- **Simstate (22)**: Simulator state data
- **Link (24)**: Inventory links
- **LinkFolder (25)**: Inventory folder links
- **Mesh (49)**: 3D mesh assets
- **Material (50)**: PBR materials

## Usage Examples

### Basic Asset Inspection

```console
OpenSim> show asset 12345678-1234-1234-1234-123456789abc
Name: Sample Texture
Description: A sample texture for testing
Type: Texture (type number = 0)
Content-type: image/jp2
Size: 45623 bytes
Temporary: no
Flags: 0
0000: ff 4f ff 51 00 2f 00 00 00 02 00 00 00 00 00 00
0010: 00 5f 00 00 00 00 00 9c 00 01 ff 52 00 0c 00 00
0020: 00 01 00 01 00 00 00 00 01 ff 5c 00 13 01 01 00
0030: 00 00 01 01 01 01 01 01 00 01 01 01 01 01 ff 64
0040: 00 1a 00 01 09 07 01 01 01 01 02 11 01 11 01 48
```

### Asset Content Export

```console
OpenSim> dump asset 12345678-1234-1234-1234-123456789abc
Asset dumped to file 12345678-1234-1234-1234-123456789abc
```

### Error Handling Examples

```console
OpenSim> show asset invalid-uuid
ERROR: invalid-uuid is not a valid ID format

OpenSim> show asset 00000000-0000-0000-0000-000000000000
Asset not found

OpenSim> dump asset existing-file-name
ERROR: File existing-file-name already exists
```

## Technical Implementation

### Asset Retrieval Process

1. **UUID Validation**: Validates that the provided asset ID is a properly formatted UUID
2. **Asset Service Query**: Retrieves the asset from the configured asset service
3. **Content Validation**: Checks that the asset exists and has valid content
4. **Data Processing**: Processes and formats the asset data for display or export

### Memory Management

- **Efficient Asset Handling**: Assets are retrieved and processed efficiently without unnecessary copying
- **Automatic Cleanup**: Asset references are properly disposed after processing
- **Stream-based Export**: Uses file streams for efficient binary data export

### Error Handling

- **Input Validation**: Comprehensive validation of command parameters
- **Service Integration**: Graceful handling of asset service errors
- **File System Safety**: Prevents overwriting existing files during export
- **User Feedback**: Clear error messages for troubleshooting

## Security Considerations

### Access Control

- **Console Access Required**: Commands require access to the OpenSim console
- **Administrative Privilege**: Intended for administrators and developers only
- **No Network Exposure**: Commands are only available through local console access

### Asset Privacy

- **Direct Asset Access**: Can access any asset by ID if known
- **Content Exposure**: Can export complete asset content to files
- **Metadata Visibility**: Exposes all asset metadata and properties

### File System Access

- **Local File Creation**: Can create files in the OpenSim working directory
- **Overwrite Protection**: Prevents accidental overwriting of existing files
- **File Permission Dependency**: Requires appropriate file system permissions

## Debugging and Troubleshooting

### Common Use Cases

1. **Asset Corruption Investigation**: Examine asset metadata and content to identify corruption
2. **Format Verification**: Verify asset types and content formats
3. **Size Analysis**: Identify unusually large or empty assets
4. **Content Extraction**: Extract asset content for external analysis
5. **Asset Service Testing**: Verify asset service connectivity and functionality

### Diagnostic Workflow

1. **Asset Discovery**: Use asset IDs from logs or database queries
2. **Metadata Inspection**: Use `show asset` to examine basic properties
3. **Content Export**: Use `dump asset` to extract content for detailed analysis
4. **External Analysis**: Use external tools to analyze exported content

### Troubleshooting Tips

- **UUID Format**: Ensure asset IDs are properly formatted UUIDs
- **Asset Existence**: Verify assets exist in the asset service
- **Service Connectivity**: Check asset service configuration and connectivity
- **File Permissions**: Ensure proper permissions for file creation during export

## Performance Considerations

### Resource Usage

- **Memory Efficient**: Processes assets without excessive memory usage
- **Network Minimal**: Single asset retrieval per command
- **CPU Light**: Minimal processing overhead for inspection
- **Disk Usage**: Export operations use disk space equivalent to asset size

### Scalability

- **Single Asset Focus**: Commands operate on individual assets
- **No Bulk Operations**: Not designed for bulk asset processing
- **Console Limitation**: Performance limited by console interface speed
- **Asset Service Dependent**: Performance depends on underlying asset service

## Integration Points

### With Asset Services

- **Service Discovery**: Automatically discovers asset service through scene interface
- **Universal Compatibility**: Works with any IAssetService implementation
- **Caching Awareness**: Respects asset service caching behavior
- **Error Propagation**: Properly handles asset service errors

### With Console System

- **Command Registration**: Integrates with OpenSim's console command system
- **Parameter Parsing**: Uses standard console parameter parsing
- **Output Formatting**: Follows console output formatting conventions
- **Help Integration**: Commands appear in console help system

### With File System

- **Working Directory**: Creates files in current working directory
- **File Safety**: Implements safety checks for file operations
- **Binary Support**: Handles binary asset content correctly
- **Platform Independence**: Works across different operating systems

## Development and Maintenance

### Code Structure

- **Simple Design**: Straightforward implementation focused on core functionality
- **Minimal Dependencies**: Few external dependencies beyond core OpenSim APIs
- **Error Resilient**: Robust error handling for various failure scenarios
- **Extensible**: Easy to extend with additional asset inspection features

### Testing Considerations

- **Asset Type Coverage**: Test with various asset types and formats
- **Error Conditions**: Test error handling with invalid inputs
- **File System Testing**: Test export functionality with various file system conditions
- **Service Integration**: Test with different asset service configurations

### Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Deployment Considerations

- **Development Tool**: Primarily intended for development and debugging environments
- **Security Awareness**: Consider security implications in production environments
- **Resource Monitoring**: Monitor resource usage during asset inspection operations
- **Access Control**: Ensure appropriate access controls for console access

## Best Practices

### Usage Guidelines

1. **Development Focus**: Use primarily in development and testing environments
2. **Security Awareness**: Be mindful of asset privacy and security in production
3. **Resource Management**: Clean up exported files when no longer needed
4. **Documentation**: Document findings from asset inspection activities

### Operational Practices

1. **Regular Cleanup**: Regularly clean up exported asset files
2. **Access Logging**: Monitor console access for asset inspection activities
3. **Privacy Compliance**: Ensure compliance with asset privacy requirements
4. **Integration Testing**: Test asset service integration after configuration changes