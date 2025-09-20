# FlotsamAssetCache Module

## Overview

The FlotsamAssetCache module is a high-performance, multi-tier asset caching system for OpenSimulator/Akisim. It provides efficient storage and retrieval of assets through a combination of memory caching, file system caching, and smart expiration policies.

## Architecture

The FlotsamAssetCache implements multiple interfaces:
- `ISharedRegionModule` - Core module lifecycle management
- `IAssetCache` - Asset caching interface
- `IAssetService` - Direct asset service interface

### Key Components

1. **Multi-tier Cache System**
   - **Weak References Cache**: Lightweight references to in-memory assets
   - **Memory Cache**: Fast in-memory asset storage with configurable expiration
   - **File Cache**: Persistent disk-based asset storage with hierarchical directory structure
   - **Negative Cache**: Tracks assets that don't exist to avoid repeated failed lookups

2. **Asynchronous File Operations**
   - Uses `ObjectJobEngine` for non-blocking file writes
   - Prevents duplicate writes through `m_CurrentlyWriting` tracking
   - Atomic file operations using temporary files and move operations

3. **Intelligent Expiration**
   - Configurable expiration times for memory and file caches
   - Background cleanup processes that preserve assets still in use
   - Scene asset scanning to protect actively used assets from expiration

## Configuration

### Module Activation

Set in `[Modules]` section:
```ini
AssetCaching = FlotsamAssetCache
```

### Main Configuration

Configuration is handled through the `[AssetCache]` section:

```ini
[AssetCache]
; Enable/disable file-based caching
FileCacheEnabled = true

; Cache directory path (relative or absolute)
CacheDirectory = assetcache

; Enable/disable memory caching
MemoryCacheEnabled = true

; Memory cache timeout in hours
MemoryCacheTimeout = 2.0

; Enable/disable negative caching (tracking of non-existent assets)
NegativeCacheEnabled = true

; Negative cache timeout in seconds
NegativeCacheTimeout = 120

; Update file access time on cache hits (affects expiration)
UpdateFileTimeOnCacheHit = false

; Logging level (0=minimal, 1=normal, 2=verbose)
LogLevel = 0

; How often to display hit rate statistics
HitRateDisplay = 100

; File cache timeout in hours
FileCacheTimeout = 48

; How often to run cleanup timer in hours
FileCleanupTimer = 1.0

; Directory structure depth (1-3 levels)
CacheDirectoryTiers = 1

; Characters per directory tier (1-4 characters)
CacheDirectoryTierLength = 3

; Warn when cache directory exceeds this many files
CacheWarnAt = 30000
```

## Directory Structure

The file cache uses a hierarchical directory structure to avoid filesystem limitations:

```
assetcache/
├── abc/
│   ├── abc123-4567-8901-2345-678901234567
│   └── abc456-7890-1234-5678-901234567890
├── def/
│   └── def789-0123-4567-8901-234567890123
└── RegionStatus_uuid.fac
```

- Directory tiers are based on asset ID prefixes
- Configurable depth (1-3 levels) and length (1-4 characters per level)
- Region status files track deep scan operations

## Features

### Performance Optimizations

1. **Cache Hierarchy**: Checks weak references → memory cache → file cache → asset service
2. **Asynchronous Operations**: File writes don't block the main thread
3. **Efficient Serialization**: Uses BinaryFormatter for fast serialization/deserialization
4. **Hit Rate Tracking**: Monitors cache effectiveness with detailed statistics

### Asset Management

1. **Smart Expiration**: Protects assets currently in use by scanning scenes
2. **Default Asset Handling**: Special handling for system default assets
3. **Cache Warming**: Can pre-load assets from configured asset loaders
4. **Negative Caching**: Avoids repeated failed lookups for non-existent assets

### Administrative Commands

The module provides several console commands for cache management:

- `fcache status` - Display cache statistics and configuration
- `fcache clear [file] [memory]` - Clear cache contents
- `fcache clearnegatives` - Clear negative cache entries
- `fcache assets` - Perform deep scan and cache all scene assets
- `fcache expire <datetime>` - Purge assets older than specified date
- `fcache cachedefaultassets` - Load default assets into cache
- `fcache deletedefaultassets` - Remove default assets from cache

## Thread Safety

The module is designed for multi-threaded environments:

- Thread-safe collections and locking mechanisms
- Atomic file operations prevent corruption
- Background cleanup processes don't interfere with normal operations
- Proper synchronization for shared state

## Integration Points

### With OpenSimulator Core

- Registers as `IAssetCache` interface with each scene
- Integrates with asset service chain for cache misses
- Provides asset service interface for direct access
- Responds to scene lifecycle events

### With Asset Services

- Acts as a caching layer in front of the main asset service
- Forwards cache misses to configured asset service
- Can operate in standalone or grid mode
- Supports asset service fallback patterns

## Performance Characteristics

### Memory Usage

- Configurable memory cache with automatic expiration
- Weak references provide minimal memory overhead
- Cleanup processes free unused memory

### Disk Usage

- Hierarchical directory structure scales to millions of assets
- Configurable expiration prevents unlimited growth
- Efficient binary serialization minimizes disk space

### Network Impact

- Reduces asset service requests through effective caching
- Especially beneficial in grid configurations
- Configurable negative caching reduces failed request overhead

## Error Handling

The module implements robust error handling:

- Graceful degradation when file operations fail
- Automatic cleanup of corrupted cache files
- Logging of errors with appropriate detail levels
- Continuation of service even with partial failures

## Use Cases

### Standalone Deployments

- Reduces asset loading times for frequently accessed content
- Provides persistence across simulator restarts
- Minimizes asset service load

### Grid Deployments

- Reduces network traffic to remote asset services
- Provides redundancy and improved response times
- Essential for geographically distributed grids

### Development Environments

- Speeds up testing and development cycles
- Provides asset persistence during frequent restarts
- Cache statistics help identify performance issues

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Deployment Considerations

- Ensure cache directory has appropriate permissions
- Monitor disk space usage in production
- Consider cache warming strategies for new deployments
- Plan cleanup schedules based on usage patterns

## Monitoring and Maintenance

### Cache Statistics

Monitor cache effectiveness through:
- Hit rates for different cache tiers
- Request patterns and overlap during writes
- Memory and disk usage trends
- Negative cache effectiveness

### Maintenance Tasks

- Regular monitoring of cache directory size
- Periodic cleanup of expired assets
- Backup considerations for critical cached assets
- Performance tuning based on usage patterns

## Troubleshooting

### Common Issues

1. **High Memory Usage**: Adjust memory cache timeout or disable memory caching
2. **Disk Space Issues**: Reduce file cache timeout or increase cleanup frequency
3. **Performance Issues**: Check hit rates and adjust cache configuration
4. **File Permission Errors**: Ensure proper filesystem permissions for cache directory

### Debugging

- Enable debug logging (`LogLevel = 2`) for detailed operation tracing
- Use `fcache status` command to check current state
- Monitor asset request patterns through hit rate statistics
- Check filesystem permissions and available space