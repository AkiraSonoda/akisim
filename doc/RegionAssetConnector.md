# RegionAssetConnector Module

## Overview

The RegionAssetConnector module is a comprehensive asset service connector for OpenSimulator/Akisim that provides intelligent routing between local asset services, hypergrid asset services, and caching layers. It acts as the primary interface for asset operations within regions, handling both local and foreign (hypergrid) asset requests with sophisticated caching and permission management.

## Architecture

The RegionAssetConnector implements multiple interfaces:
- `ISharedRegionModule` - Core module lifecycle management
- `IAssetService` - Complete asset service interface

### Key Components

1. **Multi-Service Asset Routing**
   - **Local Asset Service**: Primary asset service for grid-local assets
   - **Hypergrid Asset Service**: Secondary service for foreign/hypergrid assets
   - **Asset Cache Integration**: Seamless integration with caching modules
   - **Asset Permissions**: Configurable import/export permission controls

2. **Asynchronous Processing**
   - **Local Request Queue**: Dedicated queue for local asset requests
   - **Remote Request Queue**: Separate queue for hypergrid asset requests
   - **Request Deduplication**: Prevents duplicate requests for the same asset
   - **Callback Management**: Handles multiple callbacks for the same asset request

3. **Intelligent Asset Identification**
   - **Hypergrid Detection**: Automatic detection of hypergrid asset IDs
   - **Foreign Asset Parsing**: Parsing of foreign asset URIs and UUIDs
   - **Service Routing**: Automatic routing to appropriate asset service

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
AssetServices = RegionAssetConnector
```

### Asset Service Configuration

Configuration is handled through the `[AssetService]` section:

```ini
[AssetService]
; Local grid asset service connector (required)
LocalGridAssetService = OpenSim.Services.Connectors.dll:AssetServicesConnector

; Hypergrid asset service connector (optional)
HypergridAssetService = OpenSim.Services.HypergridService.dll:HGAssetService

; Additional asset service configuration
AssetServerURI = "http://127.0.0.1:8003"
```

### Hypergrid Asset Permissions

Configuration through the `[HGAssetService]` section:

```ini
[HGAssetService]
; Asset types allowed for import (comma-separated list)
; Types: 0=Texture, 1=Sound, 2=CallingCard, 3=Landmark, 5=Clothing, 6=Object, 7=Notecard,
;        10=LSLText, 12=BodyPart, 13=Animation, 17=SoundWAV, 18=ImageTGA, 19=ImageJPEG,
;        20=Animation, 21=Gesture, 22=Simstate, 24=Link, 25=LinkFolder, 49=Mesh, 50=Material
AllowedImportTypes = "0,1,6,10,11,12,13,17,18,19,20,21,24,25"

; Asset types allowed for export (comma-separated list)
AllowedExportTypes = "0,1,6,10,11,12,13,17,18,19,20,21,24,25"

; Whether to restrict asset operations by default
RestrictImports = true
RestrictExports = true
```

## Features

### Asset Service Routing

1. **Local Assets**: Direct routing to configured local asset service
2. **Hypergrid Assets**: Automatic detection and routing to hypergrid service
3. **Foreign Assets**: Special handling for assets from external grids
4. **Cache Integration**: Seamless integration with asset caching modules

### Performance Optimizations

1. **Request Deduplication**: Multiple requests for the same asset are consolidated
2. **Asynchronous Processing**: Non-blocking asset operations using job queues
3. **Memory Cache Priority**: Fast retrieval from memory cache when available
4. **Negative Caching**: Integration with cache negative tracking

### Hypergrid Support

1. **Foreign Asset Detection**: Automatic identification of hypergrid asset IDs
2. **Permission Controls**: Configurable import/export restrictions
3. **Local Storage**: Option to store foreign assets locally
4. **URI Parsing**: Intelligent parsing of foreign asset service URIs

## Asset Request Flow

### Standard Asset Request

1. **Cache Check**: Query asset cache for existing asset
2. **Local Service**: Retrieve from local asset service
3. **Cache Storage**: Store retrieved asset in cache
4. **Negative Caching**: Mark non-existent assets to avoid repeated lookups

### Hypergrid Asset Request

1. **ID Detection**: Identify hypergrid asset by ID format
2. **Foreign Parsing**: Parse foreign asset service information
3. **Permission Check**: Verify import permissions
4. **Cache Check**: Query cache for existing foreign asset
5. **Local Check**: Check local service first
6. **Foreign Retrieval**: Retrieve from foreign service if needed
7. **Local Storage**: Optionally store foreign asset locally
8. **Cache Storage**: Cache retrieved asset

### Asynchronous Request Handling

1. **Memory Cache Check**: Fast retrieval from memory cache
2. **Request Queuing**: Queue asset request if not in memory
3. **Deduplication**: Consolidate multiple requests for same asset
4. **Background Processing**: Process requests in dedicated worker threads
5. **Callback Execution**: Execute all registered callbacks upon completion

## API Methods

### Synchronous Methods

- `Get(string id)` - Retrieve asset by ID
- `Get(string id, string ForeignAssetService, bool StoreOnLocalGrid)` - Retrieve foreign asset
- `GetMetadata(string id)` - Get asset metadata only
- `GetData(string id)` - Get asset data only
- `GetCached(string id)` - Get cached asset only
- `Store(AssetBase asset)` - Store asset
- `UpdateContent(string id, byte[] data)` - Update asset content
- `Delete(string id)` - Delete asset
- `AssetsExist(string[] ids)` - Check multiple asset existence

### Asynchronous Methods

- `Get(string id, object sender, AssetRetrieved callBack)` - Async asset retrieval
- `Get(string id, string ForeignAssetService, bool StoreOnLocalGrid, SimpleAssetRetrieved callBack)` - Async foreign asset retrieval

## Thread Safety

The module implements comprehensive thread safety:

- Thread-safe request queuing and deduplication
- Proper locking for callback handler management
- Atomic operations for asset existence tracking
- Safe handling of concurrent requests for the same asset

## Integration Points

### With Asset Cache Modules

- Automatic discovery and integration with `IAssetCache` implementations
- Seamless cache population and retrieval
- Negative cache integration for failed lookups
- Memory cache prioritization for performance

### With Asset Services

- Dynamic loading of configured asset service connectors
- Support for multiple asset service types
- Fallback handling for service failures
- Load balancing through separate queues

### With Hypergrid Infrastructure

- Integration with hypergrid asset permissions
- Foreign asset service connectivity
- Cross-grid asset sharing capabilities
- Asset import/export control

## Performance Characteristics

### Caching Strategy

- **Memory First**: Prioritize memory cache for fastest access
- **Local Second**: Check local asset service before foreign
- **Foreign Last**: Retrieve from foreign services only when necessary
- **Negative Tracking**: Avoid repeated failed lookups

### Request Processing

- **Asynchronous Operations**: Non-blocking asset operations
- **Request Consolidation**: Multiple requests for same asset are merged
- **Parallel Processing**: Separate queues for local and remote requests
- **Callback Batching**: Efficient callback execution using fire-and-forget

### Network Optimization

- **Connection Reuse**: Efficient HTTP connection management
- **Request Batching**: Batch multiple asset existence checks
- **Foreign Asset Caching**: Local storage of foreign assets reduces network load
- **Permission Pre-filtering**: Early permission checks prevent unnecessary transfers

## Error Handling

The module implements robust error handling:

- Graceful degradation when services are unavailable
- Automatic fallback between local and foreign services
- Exception isolation in worker threads
- Safe callback execution with error suppression

## Security Features

### Asset Permissions

- **Import Controls**: Restrict which asset types can be imported
- **Export Controls**: Restrict which asset types can be exported
- **Type-based Filtering**: Granular control by asset type
- **Default Restrictions**: Configurable default permission policies

### Identity Verification

- **Asset ID Validation**: Verification of asset ID formats
- **Service Authentication**: Secure communication with asset services
- **Permission Enforcement**: Strict enforcement of configured permissions
- **Audit Logging**: Comprehensive logging of asset operations

## Use Cases

### Standalone Grid Deployments

- Single asset service connectivity
- Local asset caching for performance
- Basic asset management operations
- Development and testing environments

### Multi-Grid Hypergrid Deployments

- Cross-grid asset sharing
- Foreign asset import/export controls
- Distributed asset service connectivity
- Production hypergrid environments

### High-Performance Deployments

- Multi-threaded asset processing
- Request deduplication and optimization
- Advanced caching strategies
- Load-balanced asset service access

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[AssetService]` configuration sections
- Update asset service connector references
- Configure hypergrid permissions if needed
- Test asset operations after deployment

### Deployment Considerations

- Ensure asset service connectivity before startup
- Configure appropriate permission policies
- Monitor asset request performance
- Plan for hypergrid connectivity requirements

## Monitoring and Maintenance

### Performance Monitoring

Monitor module performance through:
- Asset request completion times
- Cache hit rates and effectiveness
- Queue lengths and processing times
- Foreign asset request patterns

### Operational Monitoring

- Asset service connectivity status
- Permission violation attempts
- Failed asset requests and causes
- Hypergrid connectivity issues

### Maintenance Tasks

- Regular monitoring of asset service health
- Performance tuning based on usage patterns
- Permission policy reviews and updates
- Log analysis for security and performance issues

## Troubleshooting

### Common Issues

1. **Asset Service Connectivity**: Check service URLs and network connectivity
2. **Permission Denials**: Review hypergrid permission configurations
3. **Cache Issues**: Verify asset cache module configuration and operation
4. **Performance Problems**: Monitor queue lengths and processing times

### Debug Configuration

Enable detailed logging by:
- Setting appropriate log levels for asset operations
- Monitoring asset request queues
- Analyzing cache hit/miss patterns
- Tracking foreign asset operations

### Diagnostic Commands

The module can be diagnosed through:
- Standard OpenSim console commands for asset operations
- Cache status commands (if cache module provides them)
- Service health checks through monitoring interfaces
- Log analysis for operation patterns

## Integration Examples

### Basic Standalone Configuration

```ini
[Modules]
AssetServices = RegionAssetConnector

[AssetService]
LocalGridAssetService = OpenSim.Services.Connectors.dll:AssetServicesConnector
AssetServerURI = "http://127.0.0.1:8003"
```

### Hypergrid-Enabled Configuration

```ini
[Modules]
AssetServices = RegionAssetConnector

[AssetService]
LocalGridAssetService = OpenSim.Services.Connectors.dll:AssetServicesConnector
HypergridAssetService = OpenSim.Services.HypergridService.dll:HGAssetService
AssetServerURI = "http://127.0.0.1:8003"

[HGAssetService]
AllowedImportTypes = "0,1,6,10,11,12,13,17,18,19,20,21"
AllowedExportTypes = "0,1,6,10,11,12,13,17,18,19,20,21"
RestrictImports = true
RestrictExports = true
```

### With Asset Caching

```ini
[Modules]
AssetServices = RegionAssetConnector
AssetCaching = FlotsamAssetCache

[AssetService]
LocalGridAssetService = OpenSim.Services.Connectors.dll:AssetServicesConnector
AssetServerURI = "http://127.0.0.1:8003"

[AssetCache]
FileCacheEnabled = true
MemoryCacheEnabled = true
CacheDirectory = assetcache
```