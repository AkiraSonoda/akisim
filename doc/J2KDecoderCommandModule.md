# J2KDecoderCommandModule

## Overview

The J2KDecoderCommandModule is a specialized debugging and administrative tool for OpenSimulator/Akisim that provides console commands for analyzing and testing JPEG2000 texture decoding operations. This optional module serves as a diagnostic utility for texture administrators, developers, and support personnel, offering direct access to the JPEG2000 decoding subsystem for troubleshooting texture-related issues, validating texture assets, and analyzing texture layer structures in the virtual world environment.

## Architecture

The J2KDecoderCommandModule implements the following interface:
- `ISharedRegionModule` - Shared module lifecycle management across all regions

### Key Components

1. **Console Command Interface**
   - **Command Registration**: Integration with OpenSim's console command system
   - **Asset Validation**: Direct access to asset service for texture retrieval
   - **Decoder Integration**: Interface with IJ2KDecoder for testing decode operations
   - **Result Reporting**: Comprehensive output of decode results and diagnostics

2. **Asset Processing Pipeline**
   - **Asset Retrieval**: Direct asset service integration for texture loading
   - **Type Validation**: Verification of asset types for texture compatibility
   - **Format Verification**: JPEG2000 format validation and compliance checking
   - **Error Handling**: Comprehensive error reporting for failed operations

3. **Diagnostic Capabilities**
   - **Layer Analysis**: Detailed analysis of JPEG2000 layer structure
   - **Component Reporting**: Component count and structure analysis
   - **Success/Failure Reporting**: Clear indication of decode operation results
   - **Performance Monitoring**: Basic performance metrics for decode operations

4. **Administrative Integration**
   - **Console Integration**: Full integration with OpenSim console system
   - **Permission Integration**: Administrative access control through console permissions
   - **Multi-region Support**: Shared functionality across all regions in instance
   - **Runtime Diagnostics**: Live diagnostic capabilities during server operation

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
[Modules]
J2KDecoderCommandModule = true
```

### Usage Requirements

The module requires:
- **J2KDecoderModule**: Must be loaded and functional for decode operations
- **Asset Service**: Functional asset service for texture retrieval
- **Console Access**: Administrative console access for command execution
- **Texture Assets**: JPEG2000 texture assets in the asset database

### Integration Dependencies

- **IJ2KDecoder Interface**: Requires functional J2KDecoderModule implementation
- **Asset Service**: Integration with OpenSim asset service for texture access
- **Console System**: Integration with MainConsole for command processing
- **Scene Management**: Access to scene for module interface resolution

## Features

### Console Commands

The module provides the following console commands:

#### j2k decode Command

**Syntax**: `j2k decode <asset-id>`

**Purpose**: Performs JPEG2000 decoding of a specified texture asset

**Parameters**:
- `<asset-id>`: UUID of the texture asset to decode

**Example Usage**:
```
j2k decode 89556747-24cb-43ed-920b-47caed15465f
j2k decode 12345678-1234-1234-1234-123456789abc
```

**Output Examples**:

*Successful Decode*:
```
Successfully decoded asset 89556747-24cb-43ed-920b-47caed15465f with 5 layers and 3 components
```

*Failed Decode*:
```
Decode of asset 89556747-24cb-43ed-920b-47caed15465f failed
```

*Asset Not Found*:
```
ERROR: No asset found with ID 89556747-24cb-43ed-920b-47caed15465f
```

*Invalid Asset Type*:
```
ERROR: Asset 89556747-24cb-43ed-920b-47caed15465f is not a texture type
```

### Diagnostic Capabilities

1. **Asset Validation**
   - **UUID Format Validation**: Verifies proper UUID format for asset IDs
   - **Asset Existence Check**: Confirms asset exists in asset database
   - **Type Verification**: Ensures asset is a texture type (AssetType.Texture)
   - **Data Availability**: Verifies asset data is available for processing

2. **Decoder Testing**
   - **Decoder Availability**: Checks for functional IJ2KDecoder module
   - **Decode Operation**: Performs actual JPEG2000 decode operation
   - **Layer Analysis**: Reports number of quality layers in texture
   - **Component Analysis**: Reports number of color components

3. **Error Reporting**
   - **Format Errors**: Clear reporting of UUID format errors
   - **Missing Assets**: Detailed reporting of missing asset scenarios
   - **Type Mismatches**: Clear indication of incorrect asset types
   - **Decode Failures**: Specific reporting of decode operation failures

## Technical Implementation

### Command Processing Architecture

#### Command Registration

The module registers console commands during region loading:

```csharp
MainConsole.Instance.Commands.AddCommand(
    "Assets",                    // Command category
    false,                       // Shared command
    "j2k decode",               // Command name
    "j2k decode <ID>",          // Usage syntax
    "Do JPEG2000 decoding of an asset.",           // Short description
    "This is for debugging purposes. The asset id given must contain JPEG2000 data.",  // Long description
    HandleDecode);               // Handler method
```

#### Command Processing Flow

1. **Parameter Validation**: Verify correct number of command parameters
2. **UUID Parsing**: Parse and validate asset UUID format
3. **Asset Retrieval**: Load asset from asset service
4. **Asset Validation**: Verify asset exists and is texture type
5. **Decoder Resolution**: Resolve IJ2KDecoder module interface
6. **Decode Operation**: Perform JPEG2000 decode with layer analysis
7. **Result Reporting**: Output detailed results to console

### Asset Processing Implementation

#### Asset Retrieval and Validation

```csharp
// Parse asset UUID
UUID assetId;
if (!UUID.TryParse(rawAssetId, out assetId))
{
    MainConsole.Instance.Output("ERROR: {0} is not a valid ID format", rawAssetId);
    return;
}

// Retrieve asset from service
AssetBase asset = m_scene.AssetService.Get(assetId.ToString());
if (asset == null)
{
    MainConsole.Instance.Output("ERROR: No asset found with ID {0}", assetId);
    return;
}

// Validate asset type
if (asset.Type != (sbyte)AssetType.Texture)
{
    MainConsole.Instance.Output("ERROR: Asset {0} is not a texture type", assetId);
    return;
}
```

#### Decoder Interface Resolution

```csharp
// Resolve J2K decoder interface
IJ2KDecoder decoder = m_scene.RequestModuleInterface<IJ2KDecoder>();
if (decoder == null)
{
    MainConsole.Instance.Output("ERROR: No IJ2KDecoder module available");
    return;
}
```

#### Decode Operation and Analysis

```csharp
// Perform decode with layer analysis
OpenJPEG.J2KLayerInfo[] layers;
int components;
if (decoder.Decode(assetId, asset.Data, out layers, out components))
{
    MainConsole.Instance.Output(
        "Successfully decoded asset {0} with {1} layers and {2} components",
        assetId, layers.Length, components);
}
else
{
    MainConsole.Instance.Output("Decode of asset {0} failed", assetId);
}
```

### Error Handling System

The module implements comprehensive error handling:

1. **Input Validation**: All user inputs are validated before processing
2. **Graceful Degradation**: Continues operation even with invalid inputs
3. **Clear Error Messages**: Provides specific, actionable error messages
4. **Safe Operations**: No operations that could compromise server stability
5. **Resource Safety**: Proper handling of asset resources and memory

## Performance Characteristics

### Resource Usage

- **Memory Footprint**: Minimal memory usage - only loads during command execution
- **CPU Impact**: Low CPU impact - only active during command execution
- **Network Usage**: Minimal network usage for asset retrieval
- **Storage Impact**: No persistent storage requirements

### Scalability Features

- **Shared Module**: Single instance shared across all regions
- **On-demand Processing**: Only processes textures when explicitly requested
- **Resource Cleanup**: Automatic cleanup of resources after operation
- **Concurrent Safety**: Safe for use during normal server operation

### Performance Optimization

- **Efficient Asset Access**: Direct asset service integration for fast retrieval
- **Minimal Overhead**: Low overhead during normal operation
- **Command Caching**: Console command registration cached at startup
- **Memory Efficiency**: No persistent memory usage between commands

## Usage Examples

### Basic Texture Analysis

```
j2k decode 89556747-24cb-43ed-920b-47caed15465f
```

Expected output for a typical texture:
```
Successfully decoded asset 89556747-24cb-43ed-920b-47caed15465f with 5 layers and 3 components
```

### Troubleshooting Failed Textures

```
j2k decode 12345678-1234-1234-1234-123456789abc
```

Possible outputs:
```
ERROR: No asset found with ID 12345678-1234-1234-1234-123456789abc
```
or
```
Decode of asset 12345678-1234-1234-1234-123456789abc failed
```

### Validating Texture Assets

```
j2k decode abcd1234-5678-90ef-ghij-klmnopqrstuv
```

If the asset is not a texture:
```
ERROR: Asset abcd1234-5678-90ef-ghij-klmnopqrstuv is not a texture type
```

### Bulk Texture Testing

For testing multiple textures, administrators can use scripts or batch commands:

```bash
# Example shell script for batch testing
for texture_id in 89556747-24cb-43ed-920b-47caed15465f 12345678-1234-1234-1234-123456789abc; do
    echo "Testing texture: $texture_id"
    echo "j2k decode $texture_id" | nc localhost 9000  # Example remote console
done
```

## Integration Points

### With J2KDecoderModule

- **Decoder Interface**: Direct integration with IJ2KDecoder interface
- **Shared Functionality**: Leverages core decoder functionality for testing
- **Layer Analysis**: Uses decoder's layer boundary analysis capabilities
- **Component Detection**: Utilizes decoder's component analysis features

### With Asset System

- **Asset Service**: Direct integration with OpenSim asset service
- **Asset Validation**: Uses asset system's type validation
- **Asset Retrieval**: Leverages asset caching and retrieval mechanisms
- **Asset Metadata**: Accesses asset metadata for validation

### With Console System

- **Command Registration**: Full integration with console command system
- **Output Formatting**: Uses console output formatting standards
- **Command Categories**: Properly categorized under "Assets" category
- **Help Integration**: Integrated with console help system

### With Scene Management

- **Module Resolution**: Uses scene module interface resolution
- **Region Independence**: Functions consistently across all regions
- **Service Access**: Accesses region services through scene interface
- **Lifecycle Management**: Proper integration with region lifecycle

## Security Features

### Access Control

- **Console Access**: Requires administrative console access
- **Command Permissions**: Integrated with console permission system
- **Safe Operations**: No operations that modify server state
- **Read-only Access**: Only reads and analyzes existing assets

### Input Validation

- **UUID Validation**: Comprehensive UUID format validation
- **Parameter Checking**: Validates all command parameters
- **Range Checking**: Ensures parameters are within valid ranges
- **Injection Prevention**: Protected against command injection attacks

### Resource Protection

- **Memory Safety**: Safe memory usage patterns
- **Resource Limits**: No unbounded resource usage
- **Error Isolation**: Errors don't affect other server operations
- **Graceful Failure**: Fails gracefully without server impact

## Debugging and Troubleshooting

### Common Issues

1. **Command Not Available**: Check that J2KDecoderCommandModule is enabled
2. **Decoder Not Found**: Verify J2KDecoderModule is loaded and functional
3. **Asset Not Found**: Confirm asset UUID is correct and asset exists
4. **Permission Denied**: Ensure user has console access permissions

### Diagnostic Procedures

1. **Module Verification**: Check module loading in server logs
2. **Asset Verification**: Verify asset exists using asset service commands
3. **Decoder Testing**: Test decoder with known good texture assets
4. **Permission Testing**: Verify console access and command permissions

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
J2KDecoderCommandModule = true
J2KDecoderModule = true
```

## Use Cases

### Texture Asset Validation

- **Upload Verification**: Verify newly uploaded textures decode correctly
- **Asset Migration**: Validate textures during asset database migration
- **Quality Assurance**: Test texture assets for compliance and quality
- **Corruption Detection**: Identify corrupted or invalid texture assets

### Technical Support

- **User Issue Resolution**: Diagnose texture-related user issues
- **Performance Analysis**: Analyze texture decode performance issues
- **Asset Investigation**: Investigate specific texture asset problems
- **System Validation**: Validate texture processing system functionality

### Development and Testing

- **Decoder Testing**: Test J2KDecoderModule functionality
- **Asset Format Testing**: Test support for various JPEG2000 formats
- **Performance Benchmarking**: Benchmark texture decode performance
- **Regression Testing**: Test texture functionality after updates

### Administrative Operations

- **System Maintenance**: Routine texture system health checks
- **Asset Auditing**: Audit texture asset integrity and compliance
- **Troubleshooting**: Diagnose and resolve texture-related issues
- **Documentation**: Generate reports on texture asset status

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[Modules]` configuration section includes `J2KDecoderCommandModule = true`
- Test console command functionality after deployment
- Update any automation scripts that use the commands
- Validate integration with J2KDecoderModule

### Deployment Considerations

- **Console Access**: Ensure administrative console access is properly configured
- **J2KDecoderModule**: Verify J2KDecoderModule is loaded and functional
- **Asset Service**: Ensure asset service is properly configured and operational
- **Permission System**: Configure appropriate console access permissions

## Configuration Examples

### Basic Command Module

```ini
[Modules]
J2KDecoderCommandModule = true
J2KDecoderModule = true
```

### Development Configuration

```ini
[Modules]
J2KDecoderCommandModule = true
J2KDecoderModule = true

[Startup]
UseCSJ2K = true

[Logging]
LogLevel = DEBUG
```

### Production Configuration

```ini
[Modules]
J2KDecoderCommandModule = false  ; Disable in production for security
J2KDecoderModule = true

[Logging]
LogLevel = INFO
```

### Testing Configuration

```ini
[Modules]
J2KDecoderCommandModule = true
J2KDecoderModule = true
AssetInfoModule = true  ; Additional asset debugging

[Startup]
UseCSJ2K = true

[Logging]
LogLevel = DEBUG
```

## Best Practices

### Security Guidelines

1. **Production Deployment**: Disable in production environments unless needed
2. **Access Control**: Restrict console access to authorized personnel only
3. **Command Auditing**: Monitor usage of debugging commands
4. **Asset Privacy**: Be mindful of asset privacy when analyzing textures

### Operational Practices

1. **Regular Testing**: Use for routine texture system health checks
2. **Documentation**: Document findings from texture analysis
3. **Issue Tracking**: Track texture-related issues discovered through analysis
4. **Performance Monitoring**: Monitor impact on server performance during use

### Development Guidelines

1. **Test Coverage**: Use for testing texture decode functionality
2. **Regression Testing**: Include in regression testing procedures
3. **Performance Testing**: Use for texture decode performance analysis
4. **Quality Assurance**: Include in texture asset quality assurance processes

## Future Enhancements

### Potential Improvements

1. **Batch Processing**: Support for analyzing multiple textures in batch
2. **Output Formats**: Support for different output formats (JSON, XML, CSV)
3. **Performance Metrics**: Enhanced performance measurement capabilities
4. **Asset Statistics**: Comprehensive texture asset statistics and reporting

### Compatibility Considerations

1. **Console Evolution**: Adapt to console system updates and changes
2. **Asset System Updates**: Maintain compatibility with asset system changes
3. **Decoder Updates**: Stay compatible with J2KDecoderModule updates
4. **Security Standards**: Implement evolving security best practices

### Integration Opportunities

1. **Web Interface**: Web-based texture analysis interface
2. **Monitoring Integration**: Integration with server monitoring systems
3. **Reporting Systems**: Integration with asset reporting systems
4. **Automation Tools**: Enhanced automation and scripting capabilities