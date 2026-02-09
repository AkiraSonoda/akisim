# ServiceThrottleModule Technical Documentation

## Overview

The **ServiceThrottleModule** (also known as GridServiceThrottleModule) is a shared region module that provides asynchronous job queuing and throttling capabilities for grid service operations in OpenSimulator. It serves as a performance optimization layer that prevents blocking operations from impacting the main simulation thread, particularly for grid service requests such as region handle lookups.

## Purpose

The ServiceThrottleModule serves as a critical infrastructure component that:

- **Asynchronous Processing**: Offloads blocking grid service operations from the main simulation thread
- **Request Throttling**: Manages the rate and concurrency of service requests to prevent overload
- **Performance Optimization**: Eliminates frame rate drops caused by synchronous service calls
- **Job Queue Management**: Provides a reliable queuing system for deferred operations
- **Client Responsiveness**: Maintains smooth client experience during intensive service operations

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│        ServiceThrottleModule        │
├─────────────────────────────────────┤
│        JobEngine (5000ms)           │
│     - Category-based queuing        │
│     - Worker thread pool (2)        │
│     - Duplicate job detection       │
├─────────────────────────────────────┤
│      IServiceThrottleModule         │
│    - Public queuing interface       │
│    - Generic job submission         │
├─────────────────────────────────────┤
│     Region Handle Processing        │
│   - OnRegionHandleRequest handler   │
│   - Grid service integration        │
│   - Client callback management      │
└─────────────────────────────────────┘
```

### Thread Architecture

- **Main Thread**: Receives requests and queues jobs
- **Worker Threads**: Execute queued operations asynchronously (configurable, default: 2 threads)
- **Job Engine**: Manages job lifecycle and thread coordination

## Interface Implementation

The module implements:
- **ISharedRegionModule**: Shared across all regions in the simulator
- **IServiceThrottleModule**: Provides public API for job queuing

### IServiceThrottleModule Interface

```csharp
public interface IServiceThrottleModule
{
    void Enqueue(string category, string itemid, Action continuation);
}
```

## Core Functionality

### Job Engine Configuration

The module uses a JobEngine with the following parameters:
- **Name**: "ServiceThrottle"
- **Description**: "ServiceThrottle"
- **Job Timeout**: 5000ms (5 seconds)
- **Worker Threads**: 2 concurrent workers

### Primary Use Cases

#### 1. Region Handle Requests

The module automatically handles `OnRegionHandleRequest` events:

```csharp
public void OnRegionHandleRequest(IClientAPI client, UUID regionID)
{
    Action action = delegate
    {
        // Validate client and scene state
        if(!client.IsActive || m_scenes.Count == 0 || m_scenes[0] == null)
            return;

        Scene baseScene = m_scenes[0];
        if(baseScene.ShuttingDown)
            return;

        // Perform grid service lookup
        GridRegion r = baseScene.GridService.GetRegionByUUID(UUID.Zero, regionID);

        // Send response to client
        if (client.IsActive && r != null && r.RegionHandle != 0)
            client.SendRegionHandle(regionID, r.RegionHandle);
    };

    m_processorJobEngine.QueueJob("regionHandle", action, regionID.ToString());
}
```

#### 2. Generic Job Queuing

External modules can queue arbitrary operations:

```csharp
IServiceThrottleModule throttle = scene.RequestModuleInterface<IServiceThrottleModule>();
if (throttle != null)
{
    throttle.Enqueue("myCategory", "uniqueJobId", () => {
        // Expensive operation here
        DoTimeConsumingTask();
    });
}
```

### Job Management Features

#### Category-Based Organization
- Jobs are organized by category (e.g., "regionHandle", "myCategory")
- Enables category-specific monitoring and management
- Supports different processing priorities

#### Duplicate Detection
- Uses item ID parameter to prevent duplicate job submission
- If a job with the same category and item ID is already queued, duplicates are ignored
- Reduces unnecessary processing load

#### Graceful Shutdown
- Properly stops job processing during module shutdown
- Ensures no orphaned worker threads
- Cleans up resources appropriately

## Configuration

### Module Activation

Configure in OpenSim.ini:

```ini
[ServiceThrottle]
Enabled = true
```

The module is enabled by default for performance optimization. To disable:

```ini
[ServiceThrottle]
Enabled = false
```

### Factory Integration

The module is loaded via factory with intelligent defaults:

```csharp
// ServiceThrottleModule is typically enabled by default for performance optimization
var serviceThrottleConfig = configSource.Configs["ServiceThrottle"];
bool enableServiceThrottle = serviceThrottleConfig?.GetBoolean("Enabled", true) ?? true;
```

## Performance Characteristics

### Threading Model

- **Non-blocking**: Main simulation thread never blocks on service operations
- **Controlled Concurrency**: Limited worker threads prevent resource exhaustion
- **Timeout Protection**: 5-second timeout prevents hung operations from blocking workers

### Memory Management

- **Reference Cleanup**: Explicitly nulls client references after processing
- **Scene Validation**: Checks scene state before processing to avoid memory leaks
- **Thread Safety**: Uses RwLockedList for safe scene collection access

### Performance Metrics

- **Job Queue Depth**: Monitor via JobEngine statistics
- **Processing Latency**: Time from queue to completion
- **Timeout Rate**: Frequency of job timeouts
- **Worker Utilization**: Thread pool efficiency

## Integration Points

### Scene Integration

```csharp
public void AddRegion(Scene scene)
{
    m_scenes.Add(scene);
    scene.RegisterModuleInterface<IServiceThrottleModule>(this);
    scene.EventManager.OnNewClient += OnNewClient;
}
```

### Client Event Handling

```csharp
void OnNewClient(IClientAPI client)
{
    client.OnRegionHandleRequest += OnRegionHandleRequest;
}
```

### Grid Service Integration

The module integrates seamlessly with grid services:
- Uses `scene.GridService.GetRegionByUUID()` for region lookups
- Handles all grid service response patterns
- Manages service timeouts and failures gracefully

## Error Handling and Resilience

### Client State Validation

```csharp
if(!client.IsActive || m_scenes.Count == 0 || m_scenes[0] == null)
{
    client = null;
    return;
}
```

### Scene State Validation

```csharp
Scene baseScene = m_scenes[0];
if(baseScene.ShuttingDown)
{
    client = null;
    return;
}
```

### Resource Cleanup

- Explicitly nulls client references to prevent memory leaks
- Validates client state before sending responses
- Handles race conditions during shutdown

## Monitoring and Debugging

### Logging Integration

The module provides debug logging capabilities:

```csharp
private static readonly ILog m_log = LogManager.GetLogger(
    MethodBase.GetCurrentMethod().DeclaringType);

// Example usage (currently commented out)
//m_log.DebugFormat("[SERVICE THROTTLE]: RegionHandleRequest {0}", regionID);
```

### Factory Logging

The factory integration provides comprehensive operational logging:

```csharp
if(m_log.IsDebugEnabled)
    m_log.Debug("Loading ServiceThrottleModule for grid service request throttling and performance optimization");

if(m_log.IsInfoEnabled)
    m_log.Info("ServiceThrottleModule loaded for grid service throttling, region handle requests, and job queue management");
```

### Performance Monitoring

Key metrics to monitor:
- **Job Queue Length**: Number of pending jobs
- **Worker Thread Utilization**: Percentage of time workers are busy
- **Average Job Duration**: Time per job execution
- **Timeout Frequency**: Rate of job timeouts

## Use Cases and Benefits

### Primary Use Cases

1. **Region Handle Lookups**: Automatic handling of client region handle requests
2. **Grid Service Queries**: Asynchronous region discovery and metadata retrieval
3. **Cross-Grid Operations**: Hypergrid region lookups and validations
4. **Bulk Operations**: Processing multiple service requests efficiently

### Performance Benefits

- **Eliminated Frame Drops**: No main thread blocking during service calls
- **Improved Responsiveness**: Clients remain responsive during heavy operations
- **Controlled Load**: Prevents service overload through throttling
- **Resource Efficiency**: Optimal thread utilization

### Scalability Benefits

- **Concurrent Processing**: Multiple jobs processed simultaneously
- **Queue Management**: Handles burst loads gracefully
- **Memory Efficiency**: Controlled resource usage
- **Timeout Protection**: Prevents resource exhaustion

## Advanced Configuration

### Custom Job Categories

External modules can use custom categories for organization:

```csharp
// Asset operations
throttle.Enqueue("assets", assetID.ToString(), () => ProcessAsset(assetID));

// Inventory operations
throttle.Enqueue("inventory", userID.ToString(), () => LoadInventory(userID));

// Grid operations
throttle.Enqueue("gridLookup", regionName, () => FindRegion(regionName));
```

### Integration with Other Modules

```csharp
public class MyModule : INonSharedRegionModule
{
    private IServiceThrottleModule m_throttle;

    public void RegionLoaded(Scene scene)
    {
        m_throttle = scene.RequestModuleInterface<IServiceThrottleModule>();
    }

    private void DoExpensiveOperation(string itemId)
    {
        if (m_throttle != null)
        {
            m_throttle.Enqueue("myModule", itemId, () => {
                // Expensive work here
                PerformDatabaseOperation();
            });
        }
        else
        {
            // Fallback to synchronous operation
            PerformDatabaseOperation();
        }
    }
}
```

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: ServiceThrottleModule not appearing in logs
Solution: Check [ServiceThrottle] Enabled = true in configuration
```

#### Performance Degradation
```
Symptom: Increased response times
Cause: Job queue backing up
Solution: Monitor job queue depth and worker thread utilization
```

#### Memory Leaks
```
Symptom: Gradually increasing memory usage
Cause: Client references not being cleaned up
Solution: Verify proper client validation and null assignment
```

### Debugging Steps

1. **Enable Debug Logging**: Uncomment debug statements in OnRegionHandleRequest
2. **Monitor Job Queue**: Check JobEngine statistics for queue depth
3. **Validate Configuration**: Ensure proper [ServiceThrottle] section
4. **Check Dependencies**: Verify JobEngine and ThreadedClasses availability

## Security Considerations

### Resource Protection

- **Timeout Enforcement**: 5-second timeout prevents hung operations
- **Thread Limits**: Maximum 2 worker threads prevents resource exhaustion
- **Input Validation**: Validates client and scene state before processing

### Memory Safety

- **Reference Cleanup**: Explicit null assignment prevents memory leaks
- **State Validation**: Checks shutdown state before processing
- **Exception Handling**: Implicit exception handling via JobEngine

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [ServiceThrottle] Enabled setting
- **Enhanced Logging**: Improved operational visibility
- **Backward Compatibility**: Maintains full API compatibility

### Upgrade Considerations

- Update configuration files to include [ServiceThrottle] section if custom control needed
- Review performance monitoring for new logging messages
- Test job queuing functionality after upgrade
- Verify proper integration with dependent modules

## Related Components

### Dependencies
- **JobEngine**: Core job processing engine from OpenSim.Framework.Monitoring
- **ThreadedClasses**: Thread management utilities
- **IClientAPI**: Client communication interface

### Integration Points
- **Scene Management**: Region lifecycle integration
- **Grid Services**: Service operation throttling
- **Client Events**: Request/response handling
- **Module Interface**: IServiceThrottleModule implementation

## Future Enhancements

### Potential Improvements

- **Configurable Timeouts**: Per-category timeout settings
- **Priority Queuing**: Different priority levels for job categories
- **Dynamic Thread Scaling**: Automatic worker thread adjustment
- **Metrics Exposure**: REST API for queue statistics
- **Category-Specific Throttling**: Different limits per job category

### Performance Optimizations

- **Job Batching**: Combine similar jobs for efficiency
- **Smart Scheduling**: Priority-based job execution
- **Load Balancing**: Distribute jobs across multiple engines
- **Caching Integration**: Cache frequently requested data

---

*This documentation covers ServiceThrottleModule as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full asynchronous job processing and grid service throttling capabilities.*