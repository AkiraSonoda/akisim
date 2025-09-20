# FetchInventory2Module

## Overview

The FetchInventory2Module is a crucial inventory management module for OpenSimulator/Akisim that implements enhanced inventory fetching capabilities for Second Life compatible viewers. It provides the `FetchInventory2` and `FetchLib2` capabilities, which are essential for efficient inventory management and library access in modern viewers.

## Architecture

The FetchInventory2Module implements the `ISharedRegionModule` interface and provides:
- HTTP capabilities registration for inventory operations
- Integration with inventory and library services
- Request throttling and bad request tracking
- Support for both local and remote capability endpoints

### Key Components

1. **Capability Registration**
   - `FetchInventory2` capability for enhanced inventory fetching
   - `FetchLib2` capability for library content access
   - Flexible endpoint configuration (local or remote)

2. **Service Integration**
   - Direct integration with `IInventoryService` for inventory operations
   - Integration with `ILibraryService` for library content access
   - Automatic service discovery through scene interfaces

3. **Request Management**
   - Bad request tracking with automatic expiration
   - Request throttling and abuse prevention
   - Efficient request handler management

## Configuration

### Module Activation

The module can be activated in two ways:

#### 1. Through OptionalModulesFactory (Recommended)
Set in `[Modules]` section:
```ini
FetchInventory2Module = true
```

#### 2. Through Capability Configuration
Set in `[ClientStack.LindenCaps]` section:
```ini
Cap_FetchInventory2 = localhost
Cap_FetchLib2 = localhost
```

### Capability Configuration

Configuration is handled through the `[ClientStack.LindenCaps]` section:

```ini
[ClientStack.LindenCaps]
; FetchInventory2 capability endpoint
; "localhost" = handle locally, URL = redirect to external service, "" = disable
Cap_FetchInventory2 = localhost

; FetchLib2 capability endpoint for library access
; "localhost" = handle locally, URL = redirect to external service, "" = disable
Cap_FetchLib2 = localhost
```

### Configuration Options

- **localhost**: Handle requests locally using internal handlers
- **URL**: Redirect requests to an external service (e.g., `http://example.com/caps/fetchinv2`)
- **empty/not set**: Disable the capability entirely

## Features

### Enhanced Inventory Fetching

The module provides optimized inventory fetching through:

1. **Bulk Operations**: Efficient handling of multiple inventory item requests
2. **Caching Integration**: Leverages existing inventory caching mechanisms
3. **Performance Optimization**: Reduced network overhead compared to legacy methods
4. **Protocol Compliance**: Full compatibility with Second Life viewer inventory protocols

### Library Access

The `FetchLib2` capability provides:

1. **Library Content Access**: Efficient access to default library items
2. **Shared Resource Management**: Optimized handling of shared library content
3. **Version Management**: Support for library versioning and updates
4. **Performance Optimization**: Cached library content delivery

### Request Management

1. **Bad Request Tracking**: Automatic tracking and temporary blocking of problematic requests
2. **Abuse Prevention**: Protection against inventory flooding and spam requests
3. **Resource Management**: Efficient memory and network resource utilization
4. **Error Handling**: Robust error handling with graceful degradation

## Technical Implementation

### Capability Registration Process

1. **Module Initialization**: Configuration validation and service setup
2. **Region Loading**: Service discovery and capability preparation
3. **Agent Connection**: Dynamic capability registration per agent
4. **Handler Creation**: Instantiation of request handlers with proper context

### Request Processing Flow

#### FetchInventory2 Requests

1. **Request Reception**: HTTP POST request with inventory item specifications
2. **Authentication**: Agent identity verification and permission checking
3. **Inventory Query**: Service integration for inventory data retrieval
4. **Response Generation**: Structured response with inventory metadata and content
5. **Delivery**: Optimized response delivery to requesting client

#### FetchLib2 Requests

1. **Library Request**: HTTP POST request for library content
2. **Service Integration**: Library service query for requested items
3. **Content Preparation**: Library content formatting and optimization
4. **Response Delivery**: Structured library content response

### Bad Request Management

The module implements sophisticated bad request tracking:

- **Automatic Detection**: Identification of malformed or abusive requests
- **Temporary Blocking**: Time-based blocking of problematic request sources
- **Expiration Management**: Automatic cleanup of expired tracking entries
- **Performance Protection**: Prevention of service degradation from bad requests

## Protocol Details

### FetchInventory2 Capability

**Endpoint**: `/caps/{uuid}/FetchInventory2`
**Method**: POST
**Content-Type**: application/llsd+xml

**Request Format**:
```xml
<llsd>
  <map>
    <key>items</key>
    <array>
      <map>
        <key>owner_id</key>
        <uuid>agent-uuid</uuid>
        <key>item_id</key>
        <uuid>item-uuid</uuid>
      </map>
    </array>
  </map>
</llsd>
```

**Response Format**:
```xml
<llsd>
  <map>
    <key>items</key>
    <array>
      <map>
        <key>item_id</key>
        <uuid>item-uuid</uuid>
        <key>owner_id</key>
        <uuid>owner-uuid</uuid>
        <key>name</key>
        <string>Item Name</string>
        <!-- Additional inventory metadata -->
      </map>
    </array>
  </map>
</llsd>
```

### FetchLib2 Capability

**Endpoint**: `/caps/{uuid}/FetchLib2`
**Method**: POST
**Content-Type**: application/llsd+xml

**Request Format**:
```xml
<llsd>
  <map>
    <key>items</key>
    <array>
      <map>
        <key>item_id</key>
        <uuid>library-item-uuid</uuid>
      </map>
    </array>
  </map>
</llsd>
```

## Performance Characteristics

### Scalability

- **Concurrent Requests**: Efficient handling of multiple simultaneous requests
- **Memory Usage**: Optimized memory allocation and cleanup
- **Network Efficiency**: Reduced protocol overhead compared to UDP-based methods
- **Service Integration**: Efficient integration with backend inventory services

### Optimization Features

- **Request Batching**: Support for bulk inventory operations
- **Caching Integration**: Leverages existing inventory and library caching
- **Connection Reuse**: Efficient HTTP connection management
- **Resource Pooling**: Optimized handler and resource management

## Security Features

### Access Control

- **Agent Authentication**: Verification of agent identity and session validity
- **Permission Checking**: Enforcement of inventory access permissions
- **Owner Validation**: Verification of item ownership before access
- **Capability Security**: Secure capability token management

### Abuse Prevention

- **Rate Limiting**: Protection against inventory request flooding
- **Bad Request Tracking**: Automatic detection and blocking of abusive patterns
- **Resource Protection**: Prevention of resource exhaustion attacks
- **Error Isolation**: Secure error handling without information disclosure

## Integration Points

### With Inventory Services

- **Service Discovery**: Automatic detection of available inventory services
- **Multi-Service Support**: Support for various inventory service implementations
- **Caching Integration**: Seamless integration with inventory caching layers
- **Permission Integration**: Respect for existing inventory permission systems

### With Library Services

- **Library Discovery**: Automatic detection of library service availability
- **Content Synchronization**: Integration with library content management
- **Version Management**: Support for library versioning and updates
- **Access Control**: Integration with library access permission systems

### With Capability System

- **Dynamic Registration**: Runtime capability registration per agent
- **Secure Endpoints**: Integration with OpenSim's capability security model
- **Handler Management**: Efficient capability handler lifecycle management
- **Protocol Compliance**: Full compliance with Second Life capability protocols

## Debugging and Troubleshooting

### Common Issues

1. **Capability Not Available**: Check configuration and module loading
2. **Inventory Access Failures**: Verify inventory service connectivity
3. **Permission Denied**: Check agent permissions and item ownership
4. **Performance Issues**: Monitor request patterns and service health

### Diagnostic Tools

1. **Configuration Validation**: Verify capability configuration settings
2. **Service Health Checks**: Monitor inventory and library service status
3. **Request Monitoring**: Track request patterns and response times
4. **Error Analysis**: Analyze error patterns and bad request tracking

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[ClientStack.LindenCaps]
; Enable debug logging for capability issues
Cap_FetchInventory2 = localhost
Cap_FetchLib2 = localhost
```

## Use Cases

### Standard Viewer Operations

- **Inventory Loading**: Efficient inventory folder and item loading
- **Search Operations**: Fast inventory search result retrieval
- **Library Access**: Quick access to default library content
- **Bulk Operations**: Efficient handling of multiple inventory operations

### Advanced Scenarios

- **Large Inventories**: Optimized handling of users with extensive inventories
- **Library Management**: Efficient library content distribution
- **Performance Optimization**: Reduced viewer loading times
- **Network Optimization**: Minimized network overhead for inventory operations

## Migration and Deployment

### From Mono.Addins

The module has been updated to work with the OptionalModulesFactory system:

- No Mono.Addins dependencies were present in the original module
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Deployment Considerations

- **Service Dependencies**: Ensure inventory and library services are available
- **Network Configuration**: Configure appropriate network settings for capabilities
- **Performance Monitoring**: Monitor capability endpoint performance
- **Client Compatibility**: Verify compatibility with target viewer versions

## Best Practices

### Configuration Management

1. **Local Processing**: Use `localhost` for better performance and control
2. **Service Health**: Monitor inventory and library service health
3. **Resource Allocation**: Allocate appropriate resources for inventory operations
4. **Security**: Implement appropriate access controls and monitoring

### Performance Optimization

1. **Caching Strategy**: Implement effective inventory caching
2. **Request Patterns**: Monitor and optimize request patterns
3. **Service Tuning**: Tune inventory and library services for optimal performance
4. **Network Optimization**: Optimize network configuration for capability endpoints

### Operational Practices

1. **Monitoring**: Continuously monitor capability endpoint health
2. **Capacity Planning**: Plan for inventory growth and usage patterns
3. **Error Handling**: Implement robust error handling and recovery procedures
4. **Documentation**: Maintain documentation of configuration and customizations

## Integration Examples

### Basic Configuration

```ini
[Modules]
FetchInventory2Module = true

[ClientStack.LindenCaps]
Cap_FetchInventory2 = localhost
Cap_FetchLib2 = localhost
```

### External Service Configuration

```ini
[Modules]
FetchInventory2Module = true

[ClientStack.LindenCaps]
Cap_FetchInventory2 = http://inventory.example.com/fetch2
Cap_FetchLib2 = http://library.example.com/fetchlib2
```

### Selective Capability Configuration

```ini
[Modules]
FetchInventory2Module = true

[ClientStack.LindenCaps]
; Enable only inventory fetching, disable library
Cap_FetchInventory2 = localhost
Cap_FetchLib2 =
```

## Future Enhancements

### Potential Improvements

1. **Enhanced Caching**: More sophisticated caching strategies
2. **Load Balancing**: Support for distributed inventory services
3. **Compression**: Request/response compression for improved performance
4. **Analytics**: Enhanced analytics and monitoring capabilities

### Compatibility Considerations

1. **Viewer Updates**: Stay current with viewer protocol changes
2. **Service Evolution**: Adapt to inventory service improvements
3. **Protocol Extensions**: Support for future protocol enhancements
4. **Performance Requirements**: Scale with growing user bases and inventory sizes