# DataSnapshotManager (External Data Generator)

## Overview

The **DataSnapshotManager** is a shared region module that provides external data generation and indexing capabilities for OpenSim grids. It creates XML snapshots of region data that can be used by external services for indexing, search, monitoring, and grid management purposes. The module supports multiple data exposure levels and can notify external data services of grid status changes.

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`, `IDataSnapshot`
- **Namespace**: `OpenSim.Region.DataSnapshot`
- **Location**: `src/OpenSim.Region.OptionalModules/DataSnapshot/DataSnapshotManager.cs`

### Dependencies
- **HTTP Framework**: RestClient for external service notifications
- **Snapshot Providers**: IDataSnapshotProvider implementations for different data types
- **Storage**: SnapshotStore for caching and managing snapshot data

## Functionality

### Core Features

#### 1. Data Snapshot Generation
- **XML Format**: Generates structured XML documents containing region data
- **Multi-Provider Support**: Aggregates data from multiple snapshot providers
- **Configurable Exposure**: Supports "minimum" and "all" data exposure levels
- **Caching System**: Intelligent caching with staleness detection

#### 2. External Service Integration
- **Service Notification**: Notifies external services of online/offline status
- **HTTP Endpoints**: Provides HTTP handlers for snapshot requests
- **Authentication**: Uses secret-based authentication for service access
- **Multiple Services**: Supports multiple external data service endpoints

#### 3. Data Providers
Built-in snapshot providers include:
- **LandSnapshot**: Parcel and land ownership information
- **ObjectSnapshot**: Scene object and primitive data
- **EstateSnapshot**: Estate settings and management data

#### 4. Update Management
- **Staleness Detection**: Tracks data changes and triggers updates
- **Configurable Timing**: Adjustable update periods and staleness thresholds
- **Intelligent Updates**: Updates only when data has changed or thresholds reached

### Snapshot Structure

#### Root XML Structure
```xml
<?xml version="1.0"?>
<regiondata>
    <expire>20</expire>
    <region>
        <!-- Region-specific data from providers -->
    </region>
</regiondata>
```

#### Data Providers
Each provider contributes specific data types:

- **LandSnapshot**: Land parcels, ownership, pricing, flags
- **ObjectSnapshot**: Scene objects, primitives, scripts, textures
- **EstateSnapshot**: Estate settings, managers, restrictions

## Configuration

### Section: [DataSnapshot]
```ini
[DataSnapshot]
    ; Enable/disable the data snapshot module
    ; Default: false
    index_sims = true

    ; Grid name for identification
    ; Default: "the lost continent of hippo"
    gridname = "My OpenSim Grid"

    ; Data exposure level: "minimum" or "all"
    ; Default: "minimum"
    data_exposure = minimum

    ; Snapshot update period in seconds
    ; Default: 20
    default_snapshot_period = 30

    ; Maximum changes before forced update
    ; Default: 500
    max_changes_before_update = 1000

    ; Snapshot cache directory
    ; Default: "DataSnapshot"
    snapshot_cache_directory = "DataSnapshot"

    ; External data services (legacy format)
    data_services = http://example.com/datasink.php

    ; External data services (new format, one per line)
    DATA_SRV_01 = http://service1.example.com/endpoint
    DATA_SRV_02 = http://service2.example.com/endpoint
```

### Network Configuration
```ini
[Network]
    ; HTTP listener port for snapshot requests
    http_listener_port = 9000
```

### Grid Information
```ini
[Startup]
    ; Gatekeeper URI for hypergrid identification
    GatekeeperURI = http://yourgrid.example.com:8002

[GridService]
    ; Alternative gatekeeper configuration
    Gatekeeper = http://yourgrid.example.com:8002
```

### Factory Integration
The module is loaded through the `CoreModuleFactory` with the following behavior:
- **Configuration-Driven**: Only loaded when `[DataSnapshot] index_sims = true`
- **Reflection-Based**: Loaded via reflection to avoid hard dependency on OptionalModules
- **Auto-Discovery**: Automatically discovers and loads data snapshot providers

## Implementation Details

### Initialization Process
1. **Configuration Loading**: Reads [DataSnapshot] section and related configurations
2. **Grid Information**: Extracts gatekeeper URL and grid identification
3. **Service Configuration**: Parses external data service endpoints
4. **Provider Discovery**: Uses reflection to find IDataSnapshotProvider implementations

### Region Integration
1. **Snapshot Store**: Creates shared snapshot storage for all regions
2. **Provider Loading**: Instantiates and initializes data providers for each region
3. **Event Handlers**: Registers HTTP handlers for snapshot requests
4. **Service Notification**: Notifies external services of grid online status

### Update Mechanism
1. **Staleness Tracking**: Providers mark data as stale when changes occur
2. **Threshold Management**: Updates triggered by time or staleness count
3. **Batch Processing**: Processes all stale regions in single update cycle
4. **Cache Management**: Maintains cached snapshots until next update

### HTTP Interface

#### Snapshot Request Endpoints
The module registers HTTP handlers for snapshot access:

- **GET `/DataSnapshot/`** - Returns data for all regions
- **GET `/DataSnapshot/{regionName}`** - Returns data for specific region

#### Request Parameters
- **Authentication**: Secret-based authentication for service access
- **Region Selection**: Optional region name for targeted snapshots
- **Format**: XML format with structured region data

#### Response Format
```xml
<?xml version="1.0"?>
<regiondata>
    <expire>20</expire>
    <region>
        <uuid>550e8400-e29b-41d4-a716-446655440000</uuid>
        <regionname>My Region</regionname>
        <estate>
            <!-- Estate data from EstateSnapshot -->
        </estate>
        <land>
            <!-- Land data from LandSnapshot -->
        </land>
        <objects>
            <!-- Object data from ObjectSnapshot -->
        </objects>
    </region>
</regiondata>
```

### External Service Integration

#### Service Notification Protocol
The module notifies external services using GET requests with parameters:

```
GET {service_url}?service={status}&host={hostname}&port={port}&secret={secret}
```

Parameters:
- **service**: "online" or "offline" status
- **host**: External hostname for the grid
- **port**: HTTP listener port
- **secret**: Authentication secret for service verification

#### Service Configuration
Services can be configured in two ways:

1. **Legacy Format**: Single semicolon-separated string
```ini
data_services = http://service1.com;http://service2.com
```

2. **New Format**: Individual DATA_SRV_* entries
```ini
DATA_SRV_01 = http://service1.com/endpoint
DATA_SRV_02 = http://service2.com/endpoint
```

## Usage Examples

### Basic Configuration
```ini
[DataSnapshot]
index_sims = true
gridname = "My OpenSim Grid"
data_exposure = minimum
```

### Advanced Configuration
```ini
[DataSnapshot]
index_sims = true
gridname = "Production Grid"
data_exposure = all
default_snapshot_period = 60
max_changes_before_update = 2000
snapshot_cache_directory = "/var/cache/opensim/snapshots"
DATA_SRV_01 = http://indexer.example.com/opensim
DATA_SRV_02 = http://search.example.com/regions
```

### HTTP Access Examples
```bash
# Get snapshot for all regions
curl http://opensim.example.com:9000/DataSnapshot/

# Get snapshot for specific region
curl http://opensim.example.com:9000/DataSnapshot/WelcomeRegion

# Example response processing
wget -O regions.xml http://opensim.example.com:9000/DataSnapshot/
xmllint --format regions.xml
```

### External Service Implementation
```php
<?php
// Example external service endpoint
$service = $_GET['service'] ?? '';
$host = $_GET['host'] ?? '';
$port = $_GET['port'] ?? '';
$secret = $_GET['secret'] ?? '';

if ($service === 'online') {
    // Grid came online, start indexing
    $snapshot_url = "http://{$host}:{$port}/DataSnapshot/";
    indexGridData($snapshot_url, $secret);
} elseif ($service === 'offline') {
    // Grid went offline, mark as unavailable
    markGridOffline($host, $secret);
}

function indexGridData($url, $secret) {
    // Fetch and process snapshot data
    $xml = file_get_contents($url);
    $data = simplexml_load_string($xml);
    // Process region data for search indexing
}
?>
```

## Data Exposure Levels

### Minimum Exposure
Limited data suitable for basic indexing:
- Region names and UUIDs
- Basic estate information
- Public land parcels
- General region statistics

### All Exposure
Complete data for comprehensive indexing:
- Detailed estate settings
- All land parcels with ownership
- Complete object inventories
- Script information and metadata
- Texture and asset references

## Performance Considerations

### Snapshot Generation
- **Caching**: Snapshots cached until data becomes stale
- **Lazy Updates**: Updates only triggered by actual changes
- **Provider Efficiency**: Individual providers optimized for their data types
- **XML Optimization**: Structured XML format for efficient parsing

### Memory Usage
- **Shared Storage**: Single SnapshotStore shared across all regions
- **Provider Instances**: Separate provider instances per region
- **Cache Management**: Automatic cleanup of stale snapshot data
- **Collection Management**: Proper cleanup of removed regions and providers

### Network Impact
- **On-Demand Generation**: Snapshots generated only when requested
- **Service Notifications**: Lightweight GET requests for status updates
- **Configurable Updates**: Adjustable update frequency to balance freshness and load
- **Error Handling**: Graceful handling of service notification failures

### HTTP Performance
- **Thread Safety**: Thread-safe snapshot generation with proper locking
- **Request Handling**: Efficient HTTP request processing
- **Error Responses**: Structured error responses for debugging
- **Timeout Management**: Proper timeout handling for external services

## Troubleshooting

### Common Issues

#### 1. Module Not Loading
**Symptoms**: No snapshots generated, services not notified
**Solutions**:
- Check `[DataSnapshot] index_sims = true` in configuration
- Verify OptionalModules.dll is available
- Check for initialization errors in log files

#### 2. Snapshot Generation Failures
**Symptoms**: HTTP requests return errors or empty data
**Solutions**:
- Verify data providers are loading correctly
- Check XML generation in log files
- Ensure snapshot cache directory is writable

#### 3. Service Notification Failures
**Symptoms**: External services not receiving updates
**Solutions**:
- Verify service URLs are accessible
- Check network connectivity and firewall rules
- Monitor service endpoint responses

#### 4. Performance Issues
**Symptoms**: Slow snapshot generation, high memory usage
**Solutions**:
- Adjust update periods and staleness thresholds
- Monitor provider performance in logs
- Consider data exposure level reduction

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization and configuration
- Provider loading and registration
- Snapshot generation and caching
- Service notifications and responses
- Update triggers and staleness detection

### Monitoring External Services
Monitor these aspects of external service integration:
- **Service Response Times**: HTTP request latency to services
- **Notification Success Rate**: Percentage of successful notifications
- **Snapshot Access Patterns**: Frequency and timing of snapshot requests
- **Data Freshness**: Time between updates and service requests

## Security Considerations

### Access Control
- **Secret Authentication**: Services authenticated using shared secret
- **Network Security**: Consider HTTPS for sensitive deployments
- **Service Validation**: Validate external service endpoints

### Data Privacy
- **Exposure Levels**: Use minimum exposure for privacy-sensitive deployments
- **Data Filtering**: Consider custom providers for sensitive data filtering
- **Access Logging**: Monitor access to snapshot endpoints

### Service Security
- **Endpoint Validation**: Verify external service endpoints before configuration
- **Error Information**: Avoid exposing sensitive information in error messages
- **Rate Limiting**: Consider implementing rate limiting for snapshot requests

## Integration Notes

### Factory Loading
- Loaded via `CoreModuleFactory.CreateSharedModules()` using reflection
- Requires `OpenSim.Region.OptionalModules.dll` assembly
- Graceful degradation if OptionalModules unavailable

### Data Provider Interface
- Implements `IDataSnapshotProvider` for extensibility
- Auto-discovery of providers using reflection
- Event-driven staleness notification system

### HTTP Framework Integration
- Uses OpenSim's HTTP server infrastructure
- Integrates with MainServer for request handling
- Supports standard HTTP response codes and headers

## See Also
- [MonitorModule](./MonitorModule.md) - Related monitoring functionality
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [HTTP Server Configuration](../docs/HttpServer.md) - Web endpoint setup
- [External Service Integration](../docs/ExternalServices.md) - Service integration patterns