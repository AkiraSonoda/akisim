# HGInventoryBroker Module

## Overview

The HGInventoryBroker (Hypergrid Inventory Broker) module is a sophisticated inventory service broker for OpenSimulator/Akisim that provides seamless inventory management across hypergrid environments. It intelligently routes inventory operations between local grid users and foreign hypergrid users, ensuring efficient and transparent access to inventory data regardless of the user's home grid.

## Architecture

The HGInventoryBroker implements multiple interfaces:
- `ISharedRegionModule` - Core module lifecycle management
- `IInventoryService` - Complete inventory service interface for seamless integration

### Key Components

1. **Intelligent Routing System**
   - **Local Grid Service**: Direct routing to local grid inventory service
   - **Foreign Grid Services**: Dynamic routing to hypergrid user inventory services
   - **Service URL Caching**: Efficient caching of inventory service URLs
   - **Connection Pooling**: Reusable connections to foreign inventory services

2. **Caching Infrastructure**
   - **Inventory Cache**: Local caching of frequently accessed inventory data
   - **Service Connector Cache**: Cached connections to foreign inventory services
   - **URL Cache**: Cached mapping of users to their inventory service URLs
   - **Expiration Management**: Automatic cache cleanup and expiration

3. **User Management Integration**
   - **Local User Detection**: Automatic identification of local grid users
   - **Foreign User Handling**: Special processing for hypergrid users
   - **Service URL Discovery**: Dynamic discovery of user inventory service URLs
   - **Session Management**: Proper handling of user sessions and disconnections

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
InventoryServices = HGInventoryBroker
```

### Core Configuration

Configuration is handled through the `[InventoryService]` section:

```ini
[InventoryService]
; Local grid inventory service connector (required)
LocalGridInventoryService = OpenSim.Services.Connectors.dll:XInventoryServicesConnector

; Additional inventory service configuration
InventoryServerURI = "http://127.0.0.1:8003/xinventory"
```

### Grid Configuration Examples

#### Standalone Hypergrid Configuration

```ini
[Modules]
InventoryServices = HGInventoryBroker

[InventoryService]
LocalGridInventoryService = OpenSim.Services.InventoryService.dll:XInventoryService
StorageProvider = OpenSim.Data.MySQL.dll:MySQLXInventoryData
ConnectionString = "Data Source=localhost;Database=opensim;User ID=opensim;Password=***;Old Guids=true;"
```

#### Grid Hypergrid Configuration

```ini
[Modules]
InventoryServices = HGInventoryBroker

[InventoryService]
LocalGridInventoryService = OpenSim.Services.Connectors.dll:XInventoryServicesConnector
InventoryServerURI = "http://gridserver.example.com:8003/xinventory"
```

## Features

### Hypergrid User Support

1. **Automatic User Classification**: Distinguishes between local and foreign users
2. **Service URL Discovery**: Automatically discovers foreign user inventory service URLs
3. **Transparent Routing**: Seamless routing of operations to appropriate services
4. **Session Management**: Proper handling of hypergrid user sessions

### Performance Optimization

1. **Intelligent Caching**: Multi-level caching for optimal performance
2. **Connection Reuse**: Efficient reuse of connections to foreign services
3. **Request Serialization**: Prevents concurrent access issues
4. **Cache Warming**: Proactive caching of frequently accessed data

### Service Integration

1. **Local Service Integration**: Seamless integration with local inventory services
2. **Foreign Service Connectivity**: Dynamic connectivity to hypergrid inventory services
3. **Fallback Mechanisms**: Robust fallback handling for service failures
4. **Load Balancing**: Efficient distribution of requests across services

## Technical Implementation

### User Classification Process

1. **User Management Integration**: Uses IUserManagement to classify users
2. **Local User Detection**: Identifies users belonging to the local grid
3. **Foreign User Handling**: Special processing for hypergrid users
4. **Service URL Caching**: Efficient caching of user service mappings

### Inventory Operation Routing

#### Local User Operations

1. **Direct Routing**: Operations routed directly to local inventory service
2. **No URL Lookup**: Bypasses URL discovery for local users
3. **Optimized Performance**: Minimal overhead for local operations
4. **Full Feature Support**: Complete inventory functionality

#### Foreign User Operations

1. **URL Discovery**: Automatic discovery of user's inventory service URL
2. **Service Connection**: Dynamic connection establishment to foreign services
3. **Operation Forwarding**: Transparent forwarding of inventory operations
4. **Response Handling**: Proper handling of foreign service responses

### Caching Strategy

#### Service URL Caching

- **Agent Connection**: URLs cached when agents connect
- **Session Persistence**: URLs maintained throughout user sessions
- **Automatic Cleanup**: URLs removed when users disconnect
- **Fallback Discovery**: Alternative URL discovery methods

#### Inventory Data Caching

- **Root Folder Caching**: Cached root folder information
- **Folder Type Caching**: Cached special folder locations
- **Content Caching**: Cached folder contents for performance
- **Item Caching**: Cached individual item information

#### Connection Caching

- **Service Connectors**: Reusable connections to foreign services
- **Expiration Management**: Automatic cleanup of expired connections
- **Resource Optimization**: Efficient resource utilization
- **Connection Pooling**: Shared connections for multiple operations

## API Methods

### Core Inventory Operations

- `CreateUserInventory(UUID userID)` - Initialize user inventory
- `GetInventorySkeleton(UUID userID)` - Get complete inventory structure
- `GetRootFolder(UUID userID)` - Get user's root inventory folder
- `GetFolderForType(UUID userID, FolderType type)` - Get special purpose folders

### Folder Operations

- `GetFolderContent(UUID userID, UUID folderID)` - Get folder contents
- `GetMultipleFoldersContent(UUID userID, UUID[] folderIDs)` - Bulk folder retrieval
- `GetFolderItems(UUID userID, UUID folderID)` - Get folder items only
- `AddFolder(InventoryFolderBase folder)` - Create new folder
- `UpdateFolder(InventoryFolderBase folder)` - Update folder properties
- `DeleteFolders(UUID ownerID, List<UUID> folderIDs)` - Delete multiple folders
- `MoveFolder(InventoryFolderBase folder)` - Move folder to new location
- `PurgeFolder(InventoryFolderBase folder)` - Permanently delete folder contents

### Item Operations

- `GetItem(UUID principalID, UUID itemID)` - Get individual item
- `GetMultipleItems(UUID userID, UUID[] itemIDs)` - Bulk item retrieval
- `AddItem(InventoryItemBase item)` - Create new item
- `UpdateItem(InventoryItemBase item)` - Update item properties
- `MoveItems(UUID ownerID, List<InventoryItemBase> items)` - Move multiple items
- `DeleteItems(UUID ownerID, List<UUID> itemIDs)` - Delete multiple items

### Special Operations

- `GetFolder(UUID principalID, UUID folderID)` - Get folder metadata
- `HasInventoryForUser(UUID userID)` - Check if user has inventory
- `GetActiveGestures(UUID userId)` - Get user's active gestures
- `GetAssetPermissions(UUID userID, UUID assetID)` - Get asset permissions

## Performance Characteristics

### Routing Efficiency

- **Local User Optimization**: Direct routing for local users with minimal overhead
- **Foreign User Caching**: Efficient caching of foreign user service information
- **Connection Reuse**: Optimized connection management for foreign services
- **Request Batching**: Support for bulk operations to reduce network overhead

### Caching Strategy

- **Multi-Level Caching**: Inventory data, service URLs, and connections cached
- **Intelligent Expiration**: Automatic cleanup of expired cache entries
- **Memory Optimization**: Efficient memory usage with proper cache sizing
- **Cache Warming**: Proactive caching of frequently accessed data

### Network Optimization

- **Connection Pooling**: Reusable connections to foreign inventory services
- **Request Serialization**: Prevents concurrent access conflicts
- **Bandwidth Optimization**: Efficient data transfer protocols
- **Timeout Management**: Proper handling of network timeouts and failures

## Security Features

### Access Control

- **User Authentication**: Verification of user identity before operations
- **Permission Validation**: Enforcement of inventory access permissions
- **Service Authentication**: Secure communication with foreign services
- **Session Validation**: Proper validation of user sessions

### Data Protection

- **Secure Communication**: Encrypted communication with foreign services
- **Data Validation**: Validation of inventory data integrity
- **Privacy Protection**: Protection of user inventory privacy
- **Audit Logging**: Comprehensive logging of inventory operations

## Integration Points

### With User Management

- **User Classification**: Integration with IUserManagement for user type detection
- **Service URL Discovery**: Retrieval of user service URLs from user management
- **Session Tracking**: Integration with user session management
- **Authentication**: Coordination with user authentication systems

### With Local Inventory Services

- **Service Discovery**: Automatic discovery of local inventory services
- **Configuration Integration**: Dynamic loading of configured local services
- **Operation Forwarding**: Transparent forwarding of local user operations
- **Error Handling**: Proper handling of local service errors

### With Hypergrid Infrastructure

- **Service URL Management**: Integration with hypergrid service URL infrastructure
- **Cross-Grid Communication**: Secure communication across grid boundaries
- **Protocol Compliance**: Full compliance with hypergrid protocols
- **Version Compatibility**: Support for multiple hypergrid protocol versions

## Debugging and Troubleshooting

### Common Issues

1. **Service Configuration**: Incorrect local inventory service configuration
2. **Network Connectivity**: Problems connecting to foreign inventory services
3. **User Classification**: Issues with local/foreign user detection
4. **Cache Problems**: Cache inconsistencies or performance issues

### Diagnostic Tools

1. **Debug Logging**: Comprehensive logging for troubleshooting
2. **Cache Analysis**: Tools for analyzing cache performance and contents
3. **Connection Monitoring**: Monitoring of foreign service connections
4. **Performance Metrics**: Analysis of operation performance and timing

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
InventoryServices = HGInventoryBroker
```

## Use Cases

### Hypergrid Environments

- **Cross-Grid Inventory**: Seamless inventory access across multiple grids
- **User Mobility**: Support for users moving between grids
- **Federated Services**: Integration with federated inventory services
- **Grid Interoperability**: Enhanced interoperability between different grids

### High-Performance Scenarios

- **Large User Bases**: Efficient handling of large numbers of users
- **Heavy Inventory Usage**: Optimized for environments with heavy inventory activity
- **Distributed Services**: Support for distributed inventory architectures
- **Load Balancing**: Efficient distribution of inventory operations

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Deployment Considerations

- **Service Dependencies**: Ensure local inventory service is properly configured
- **Network Configuration**: Configure appropriate network settings for hypergrid communication
- **Cache Sizing**: Optimize cache sizes based on expected usage patterns
- **Performance Monitoring**: Monitor cache performance and service connectivity

## Configuration Examples

### Basic Hypergrid Setup

```ini
[Modules]
InventoryServices = HGInventoryBroker

[InventoryService]
LocalGridInventoryService = OpenSim.Services.Connectors.dll:XInventoryServicesConnector
InventoryServerURI = "http://127.0.0.1:8003/xinventory"
```

### Standalone Hypergrid Setup

```ini
[Modules]
InventoryServices = HGInventoryBroker

[InventoryService]
LocalGridInventoryService = OpenSim.Services.InventoryService.dll:XInventoryService
StorageProvider = OpenSim.Data.MySQL.dll:MySQLXInventoryData
ConnectionString = "Data Source=localhost;Database=opensim;User ID=opensim;Password=***;"
```

### Production Grid Setup

```ini
[Modules]
InventoryServices = HGInventoryBroker

[InventoryService]
LocalGridInventoryService = OpenSim.Services.Connectors.dll:XInventoryServicesConnector
InventoryServerURI = "http://inventory.grid.example.com:8003/xinventory"

[Network]
http_listener_port = 9000
```

## Best Practices

### Configuration Management

1. **Service URL Validation**: Ensure all service URLs are properly configured and accessible
2. **Network Security**: Implement appropriate network security measures
3. **Cache Tuning**: Optimize cache settings based on usage patterns
4. **Monitoring**: Implement comprehensive monitoring of service health

### Performance Optimization

1. **Cache Sizing**: Configure appropriate cache sizes for your environment
2. **Connection Management**: Monitor and optimize foreign service connections
3. **Resource Allocation**: Allocate appropriate system resources
4. **Load Testing**: Perform regular load testing to identify bottlenecks

### Operational Practices

1. **Service Monitoring**: Continuously monitor inventory service health
2. **Error Analysis**: Regular analysis of error patterns and resolution
3. **Capacity Planning**: Plan for growth in users and inventory operations
4. **Documentation**: Maintain documentation of configuration and procedures

## Future Enhancements

### Potential Improvements

1. **Enhanced Caching**: More sophisticated caching strategies and algorithms
2. **Load Balancing**: Advanced load balancing for foreign service connections
3. **Performance Analytics**: Enhanced performance monitoring and analytics
4. **Protocol Evolution**: Support for evolving hypergrid protocols and standards

### Compatibility Considerations

1. **Protocol Updates**: Stay current with hypergrid protocol developments
2. **Service Evolution**: Adapt to changes in inventory service architectures
3. **Scalability**: Enhanced scalability for larger hypergrid environments
4. **Integration**: Improved integration with emerging hypergrid technologies