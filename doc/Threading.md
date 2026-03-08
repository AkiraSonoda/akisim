# Threading in OpenSim

## Overview

OpenSim uses a centralized threading pattern called "FireAndForget" to handle asynchronous operations throughout the simulator. This system allows operations to be executed on background threads without blocking the main simulation loop.

As of this version, OpenSim uses .NET's built-in `ThreadPool` instead of the third-party SmartThreadPool library, providing better integration with the .NET runtime and reducing maintenance complexity.

## Architecture

### FireAndForget Pattern

The FireAndForget pattern is implemented in `OpenSim.Framework.Util` and provides a consistent way to execute work asynchronously across the entire codebase. All asynchronous operations should use this pattern rather than creating threads directly.

**Key Features:**
- Centralized thread management
- Timeout monitoring (10-minute default timeout)
- Cooperative cancellation support
- Comprehensive logging and statistics
- Configurable thread pool sizing

### Threading Methods

OpenSim supports multiple threading methods, configured via the `async_call_method` setting:

| Method | Description | Use Case |
|--------|-------------|----------|
| `QueueUserWorkItem` | Uses .NET's built-in ThreadPool (recommended) | Production environments |
| `Thread` | Creates a dedicated thread per operation | Debugging or special cases |
| `None` | Executes synchronously in the calling thread | Debugging only |
| `RegressionTest` | Synchronous execution without exception handling | Automated testing |

**Default:** `QueueUserWorkItem`

## Configuration

### OpenSim.ini Settings

```ini
[Startup]
    ; Threading method to use
    async_call_method = QueueUserWorkItem

    ; Minimum number of worker threads
    MinPoolThreads = 2

    ; Maximum number of worker threads
    MaxPoolThreads = 300
```

### Thread Pool Configuration

When using `QueueUserWorkItem`, the thread pool is configured during startup:

- **MinPoolThreads**: Minimum number of threads kept alive in the pool
- **MaxPoolThreads**: Maximum number of threads the pool can create
- **Default values**: Min=2, Max=300

The .NET ThreadPool will dynamically adjust the number of active threads based on workload, within these bounds.

## Usage

### Basic FireAndForget

```csharp
using OpenSim.Framework;

// Simple fire-and-forget operation
Util.FireAndForget(delegate(object o)
{
    // Your async work here
    DoSomethingExpensive();
});

// With parameter
Util.FireAndForget(delegate(object o)
{
    MyData data = (MyData)o;
    ProcessData(data);
}, myDataObject);

// With context for better logging
Util.FireAndForget(
    callback: MyCallback,
    obj: myData,
    context: "MyModule.ProcessRequest",
    dotimeout: true  // Enable timeout monitoring
);
```

### Timeout Management

The FireAndForget system includes automatic timeout monitoring:

- **Default timeout**: 10 minutes (600,000 ms)
- **Watchdog timer**: Checks every 1 second
- **Action on timeout**: Cooperative cancellation via `CancellationToken`

To disable timeout for a specific operation:

```csharp
Util.FireAndForget(callback, obj, "LongRunningOperation", dotimeout: false);
```

### Cancellation Support

Operations can be cancelled cooperatively:

```csharp
Util.FireAndForget(delegate(object o)
{
    for (int i = 0; i < 1000000; i++)
    {
        // Check for cancellation
        if (token.IsCancellationRequested)
        {
            m_log.Info("Operation cancelled");
            return;
        }

        DoWork(i);
    }
}, null, "CancellableOperation");
```

**Note:** .NET Core/5+ does not support forceful thread termination (`Thread.Abort()`). Timeout monitoring will log warnings and attempt cooperative cancellation, but hung threads cannot be forcefully killed.

## Monitoring and Statistics

### Thread Pool Information

Get current thread pool statistics:

```csharp
ThreadPoolInfo info = Util.GetThreadPoolInfo();

if (info != null)
{
    m_log.InfoFormat("ThreadPool Stats:");
    m_log.InfoFormat("  Max Threads: {0}", info.MaxThreads);
    m_log.InfoFormat("  Min Threads: {0}", info.MinThreads);
    m_log.InfoFormat("  Active Threads: {0}", info.ActiveThreads);
    m_log.InfoFormat("  In Use Threads: {0}", info.InUseThreads);
    m_log.InfoFormat("  Available Threads: {0}", info.AvailableThreads);
    m_log.InfoFormat("  Waiting Callbacks: {0}", info.WaitingCallbacks);
}
```

### Console Commands

View thread pool statistics in the OpenSim console:

```
show stats
```

This displays:
- Thread pool name
- Max/min threads configured
- Currently allocated threads
- Threads currently in use
- Work items waiting in queue

### Logging

Thread pool logging can be enabled for debugging:

```csharp
// Set in code or via console
Util.LogThreadPool = 1;  // Basic logging
Util.LogThreadPool = 2;  // Full stack traces
Util.LogThreadPool = 3;  // Full traces including common threads
```

## Performance Considerations

### Thread Pool Sizing

**Minimum Threads:**
- Too low: Increased latency as new threads are spun up
- Too high: Wastes memory on idle threads
- **Recommended**: 2-10 for most scenarios

**Maximum Threads:**
- Too low: Operations may queue up under high load
- Too high: Excessive context switching and memory usage
- **Recommended**: 100-500 depending on hardware
  - Small regions: 100-200
  - Large regions: 200-300
  - Grid services: 300-500

### Best Practices

1. **Keep operations short**: FireAndForget is for quick async operations, not long-running tasks
2. **Avoid blocking**: Don't use `Thread.Sleep()` or blocking I/O in FireAndForget callbacks
3. **Use async I/O**: For database and network operations, prefer async methods
4. **Pool resources**: Reuse expensive resources (DB connections, HTTP clients)
5. **Monitor queue depth**: High queue depth indicates thread pool exhaustion

### Common Issues

**High queue depth with low thread usage:**
- Operations are blocking (e.g., synchronous I/O)
- Consider increasing MinThreads or using async I/O

**Thread pool exhaustion:**
- Too many long-running operations
- Consider using dedicated threads for truly long-running work
- Break up operations into smaller chunks

**Timeout warnings:**
- Operations taking >10 minutes
- Check for infinite loops or deadlocks
- Consider disabling timeout if legitimately long-running

## Migration from SmartThreadPool

OpenSim previously used the SmartThreadPool library. The migration to .NET ThreadPool includes these changes:

### Behavioral Differences

| Aspect | SmartThreadPool | .NET ThreadPool |
|--------|-----------------|-----------------|
| Thread Abortion | Supported (unreliable) | Not supported (cooperative only) |
| Thread Pool Sizing | Fixed min/max | Dynamic within bounds |
| Statistics | Rich built-in stats | Custom tracking required |
| ExecutionContext | Suppressed by default | Suppressed via UnsafeQueue |
| Priority Control | Supported | Not available |

### Code Changes Required

**Before (SmartThreadPool):**
```csharp
async_call_method = SmartThreadPool
MaxPoolThreads = 300
```

**After (.NET ThreadPool):**
```csharp
async_call_method = QueueUserWorkItem
MaxPoolThreads = 300
MinPoolThreads = 2
```

### What Stays the Same

- `Util.FireAndForget()` API is unchanged
- Timeout monitoring still works
- Statistics collection continues (different implementation)
- Configuration file structure is compatible

## Internal Implementation

### Key Components

**File:** `src/OpenSim.Framework/Util.cs`

**Main Methods:**
- `InitThreadPool(int minThreads, int maxThreads)` - Configures .NET ThreadPool
- `FireAndForget(WaitCallback callback, object obj, string context, bool dotimeout)` - Main entry point
- `GetThreadPoolInfo()` - Returns current statistics
- `ThreadPoolWatchdog(object state)` - Monitors for timeouts (runs every 1 second)

**ThreadInfo Class:**
Tracks active operations for timeout monitoring:
- Start time
- Stack trace at queue time
- Current thread reference
- CancellationTokenSource for cooperative cancellation
- Context string for logging

### Thread Pool Initialization

```csharp
public static void InitThreadPool(int minThreads, int maxThreads)
{
    // Validate parameters
    if (maxThreads < 2)
        throw new ArgumentOutOfRangeException(nameof(maxThreads));

    if (minThreads > maxThreads || minThreads < 2)
        throw new ArgumentOutOfRangeException(nameof(minThreads));

    // Configure .NET ThreadPool
    ThreadPool.SetMinThreads(minThreads, minThreads);
    ThreadPool.SetMaxThreads(maxThreads, maxThreads);

    // Start watchdog timer (1 second interval)
    m_threadPoolWatchdog = new Timer(ThreadPoolWatchdog, null, 0, 1000);
}
```

### Execution Flow

1. **Queue Request**: `FireAndForget()` called with callback and parameters
2. **Tracking Setup**: Create `ThreadInfo` object with context and timing
3. **Queue to ThreadPool**: Use `ThreadPool.UnsafeQueueUserWorkItem()` (doesn't capture ExecutionContext)
4. **Execution**: Worker thread picks up request and executes callback
5. **Monitoring**: Watchdog timer checks for timeouts every second
6. **Cleanup**: Remove from tracking when complete or timed out

## Debugging

### Enable Verbose Logging

```csharp
// In OpenSim console or Robust console
debug threadpool 3
```

This logs:
- Every FireAndForget call with stack trace
- Thread allocation and deallocation
- Timeout events
- Performance metrics

### Common Debug Scenarios

**Finding hung threads:**
```
show stats
```
Look for high "In Use Threads" with low work being done.

**Identifying hot paths:**
Enable `LogThreadPool = 2` and analyze which code paths generate the most FireAndForget calls.

**Tracking down timeouts:**
Timeout warnings include the stack trace from when the operation was queued, helping identify the source.

## See Also

- `src/OpenSim.Framework/Util.cs` - Implementation
- `src/OpenSim.Server.RegionServer/OpenSim.cs` - Initialization
- `src/OpenSim.Framework.Monitoring/ServerStatsCollector.cs` - Statistics collection
- Microsoft Docs: [Thread Pool Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/threading/the-managed-thread-pool)

## Version History

- **0.9.4+** - Migrated from SmartThreadPool to .NET ThreadPool
- **0.9.3 and earlier** - Used SmartThreadPool library

---

*Last Updated: February 2026*
