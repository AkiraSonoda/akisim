# EtcdMonitoringModule

## Overview

The **EtcdMonitoringModule** is a non-shared region module that provides high-availability storage for monitoring data using etcd, a distributed key-value store. It enables OpenSim regions to store, retrieve, and watch monitoring data in a cluster-resilient manner, making it ideal for production grid deployments that require reliable monitoring data persistence.

## Architecture

### Module Type
- **Interface**: `INonSharedRegionModule`, `IEtcdModule`
- **Namespace**: `OpenSim.Region.OptionalModules.Framework.Monitoring`
- **Location**: `src/OpenSim.Region.OptionalModules/Framework/Monitoring/EtcdMonitoringModule.cs`

### Dependencies
- **External**: `netcd` - .NET client library for etcd
- **Core**: `OpenSim.Framework` - Base framework types
- **Interface**: `IEtcdModule` - Service interface for etcd operations

## Functionality

### Core Features

#### 1. High-Availability Data Storage
- **Distributed Storage**: Connects to etcd cluster for fault-tolerant data persistence
- **Automatic Failover**: Handles etcd node failures transparently
- **Consistent Reads**: Ensures data consistency across cluster nodes

#### 2. Key-Value Operations
- **Store**: Save key-value pairs with optional TTL (time-to-live)
- **Get**: Retrieve values by key
- **Delete**: Remove keys from storage
- **Watch**: Monitor key changes with callback notifications

#### 3. Region-Specific Namespacing
- **Base Path Configuration**: Configurable path prefix for all keys
- **Region ID Separation**: Optional region UUID appending for multi-region isolation
- **Hierarchical Organization**: Directory-based key organization

#### 4. Service Interface Registration
- **IEtcdModule Interface**: Registers as scene service for other modules to use
- **Centralized Access**: Single point of access for etcd operations within regions

### Etcd Integration

#### Connection Management
- **Multi-Endpoint Support**: Connects to multiple etcd cluster endpoints
- **Connection Pooling**: Efficient connection management through netcd client
- **Error Handling**: Robust error handling with detailed logging

#### Data Serialization
- **Default Serializers**: Uses netcd's default JSON serialization
- **Flexible Format**: Supports string-based values with transparent serialization

#### Directory Structure
- **Automatic Creation**: Creates base directories on region initialization
- **Hierarchical Paths**: Supports nested key structures
- **Region Isolation**: Optional per-region directory separation

## Configuration

### Section: [Etcd]
```ini
[Etcd]
    ; Comma-separated list of etcd cluster endpoints
    ; Required for module to be enabled
    EtcdUrls = http://etcd1:2379,http://etcd2:2379,http://etcd3:2379

    ; Base path for all keys (optional)
    ; Default: empty (root level)
    BasePath = /opensim/monitoring/

    ; Whether to append region UUID to base path
    ; Default: true
    AppendRegionID = true
```

### Factory Integration
The module is loaded through the `CoreModuleFactory` with the following behavior:
- **Configuration-Driven**: Only loaded when `[Etcd]` section exists and `EtcdUrls` is configured
- **Reflection-Based**: Loaded via reflection to avoid hard dependency on OptionalModules
- **Graceful Fallback**: Warns if configured but dependencies unavailable

### Key Path Structure
Final key paths follow this pattern:
```
{BasePath}{RegionID}/key
```

Examples:
- With BasePath="/monitoring/" and AppendRegionID=true: `/monitoring/550e8400-e29b-41d4-a716-446655440000/agent_count`
- With BasePath="" and AppendRegionID=false: `agent_count`

## Implementation Details

### Initialization Process
1. **Configuration Validation**: Checks for `[Etcd]` section and required `EtcdUrls`
2. **Endpoint Parsing**: Parses comma-separated etcd URLs into URI list
3. **Client Creation**: Initializes etcd client with endpoint list and serializers
4. **Connection Testing**: Validates connection to etcd cluster

### Region Integration
1. **Path Setup**: Configures final base path with optional region ID
2. **Directory Creation**: Creates base directory structure in etcd
3. **Interface Registration**: Registers `IEtcdModule` service interface
4. **Logging Setup**: Initializes comprehensive debug and info logging

### Data Operations

#### Store Operation
```csharp
public bool Store(string key, string value, int ttl = 0)
```
- **TTL Support**: Optional time-to-live for automatic key expiration
- **Error Handling**: Returns false on failure with detailed logging
- **Path Resolution**: Automatically prepends configured base path

#### Get Operation
```csharp
public string Get(string key)
```
- **Null-Safe**: Returns empty string on missing keys or errors
- **Error Logging**: Logs detailed error information for troubleshooting
- **Consistent Reads**: Uses etcd's strong consistency guarantees

#### Delete Operation
```csharp
public void Delete(string key)
```
- **Fire-and-Forget**: Does not return success status
- **Exception Handling**: Catches and logs deletion errors
- **Cleanup Support**: Useful for temporary monitoring data

#### Watch Operation
```csharp
public void Watch(string key, Action<string> callback)
```
- **Real-time Notifications**: Triggers callback on key value changes
- **Persistent Watches**: Maintains watch until module shutdown
- **Error Recovery**: Handles watch failures with retry mechanisms

## Usage Examples

### Basic Configuration
```ini
[Etcd]
EtcdUrls = http://localhost:2379
BasePath = /opensim/
AppendRegionID = true
```

### Multi-Node Cluster
```ini
[Etcd]
EtcdUrls = http://etcd1:2379,http://etcd2:2379,http://etcd3:2379
BasePath = /grid/monitoring/
AppendRegionID = true
```

### Script Usage (via IEtcdModule)
```csharp
// Get etcd service from scene
IEtcdModule etcd = scene.RequestModuleInterface<IEtcdModule>();

// Store monitoring data
etcd.Store("agent_count", "15");
etcd.Store("temp_session", "active", 3600); // 1 hour TTL

// Retrieve data
string count = etcd.Get("agent_count");

// Watch for changes
etcd.Watch("region_status", (newValue) => {
    Console.WriteLine($"Region status changed to: {newValue}");
});

// Clean up
etcd.Delete("temp_session");
```

### Integration with Other Modules
```csharp
public class MyMonitoringModule : ISharedRegionModule
{
    private IEtcdModule m_etcd;

    public void RegionLoaded(Scene scene)
    {
        m_etcd = scene.RequestModuleInterface<IEtcdModule>();
        if (m_etcd != null)
        {
            // Store region performance data
            m_etcd.Store("sim_fps", scene.StatsReporter.LastReportedSimStats[(int)StatsIndex.SimFPS].ToString());
        }
    }
}
```

## Performance Considerations

### Network Latency
- **Local etcd**: Minimal latency for same-datacenter deployments
- **Remote etcd**: Consider network latency for cross-datacenter setups
- **Batch Operations**: Group related operations to reduce round trips

### Storage Efficiency
- **Key Naming**: Use consistent, hierarchical key naming conventions
- **TTL Usage**: Implement TTL for temporary data to prevent storage bloat
- **Value Size**: Keep values reasonably small for optimal performance

### High Availability
- **Cluster Size**: Use odd number of etcd nodes (3, 5, 7) for quorum
- **Node Distribution**: Distribute etcd nodes across failure domains
- **Monitoring**: Monitor etcd cluster health separately from OpenSim

## Troubleshooting

### Common Issues

#### 1. Connection Failures
**Symptoms**: Module fails to initialize, connection errors in logs
**Solutions**:
- Verify etcd cluster is running and accessible
- Check firewall rules for etcd ports (default 2379)
- Validate etcd URLs in configuration

#### 2. Permission Denied
**Symptoms**: Storage operations fail with permission errors
**Solutions**:
- Check etcd authentication configuration
- Verify client certificates if using TLS
- Ensure etcd user has read/write permissions

#### 3. Key Not Found
**Symptoms**: Get operations return empty strings
**Solutions**:
- Verify key path construction (BasePath + RegionID + key)
- Check if TTL expired for stored keys
- Confirm etcd cluster data integrity

#### 4. Watch Failures
**Symptoms**: Watch callbacks stop triggering
**Solutions**:
- Check etcd cluster connectivity
- Monitor for etcd leader elections
- Restart watches after connection failures

### Debug Information
Enable debug logging to see detailed etcd operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Etcd connection establishment
- All store/get/delete operations with full key paths
- Watch setup and trigger events
- Error details with stack traces

### Monitoring Etcd Health
Monitor these etcd metrics:
- **Cluster Health**: All nodes responding
- **Leader Elections**: Frequency of leader changes
- **Storage Usage**: Disk space and compaction
- **Network Latency**: Response times between nodes

## Integration Notes

### Factory Loading
- Loaded via `CoreModuleFactory.CreateSharedModules()` using reflection
- Requires `OpenSim.Region.OptionalModules.dll` assembly
- Graceful degradation if etcd dependencies unavailable

### Dependencies
- **netcd**: .NET etcd client library
- **Optional Assembly**: Part of OptionalModules, not CoreModules
- **External Service**: Requires separate etcd cluster deployment

### Service Interface
- Implements `IEtcdModule` for standardized access
- Registered per-region for isolated operation
- Available to all modules within the same region

## Security Considerations

### Network Security
- **TLS Encryption**: Use HTTPS endpoints in production
- **Client Certificates**: Implement mutual TLS authentication
- **Network Isolation**: Restrict etcd network access to authorized clients

### Data Protection
- **Sensitive Data**: Avoid storing sensitive information without encryption
- **Key Prefixes**: Use consistent prefixes to prevent key collisions
- **Access Control**: Implement etcd RBAC for production deployments

### Configuration Security
- **URL Protection**: Secure etcd URLs in configuration files
- **Credential Management**: Use secure credential storage for authentication
- **Regular Updates**: Keep etcd cluster and netcd library updated

## See Also
- [etcd Documentation](https://etcd.io/docs/) - Official etcd documentation
- [netcd Library](https://github.com/wangjia184/etcd.net) - .NET etcd client
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [MonitorModule](./MonitorModule.md) - Related monitoring functionality