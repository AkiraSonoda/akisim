# WebStatsModule (ViewerStatsModule)

## Overview

The **WebStatsModule** (also known as ViewerStatsModule) is a shared region module that provides comprehensive viewer statistics collection, web-based reporting, and real-time monitoring capabilities for OpenSim deployments. It collects detailed client performance data, system information, and user behavior metrics to enable grid administrators to monitor user experience and system performance.

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`
- **Namespace**: `OpenSim.Region.UserStatistics`
- **Location**: `src/OpenSim.Region.OptionalModules/UserStatistics/WebStatsModule.cs`

### Dependencies
- **Database**: SQLite for local statistics storage
- **Web Server**: Integrated HTTP handlers for web-based reporting
- **Capabilities**: ViewerStats capability for client data collection

## Functionality

### Core Features

#### 1. Viewer Statistics Collection
- **Performance Metrics**: FPS, ping, memory usage, frame times
- **System Information**: CPU, GPU, OS, RAM specifications
- **Network Statistics**: Packet rates, bandwidth usage, failures
- **User Behavior**: Regions visited, meters traveled, session duration
- **Client Details**: Viewer version, language, system specifications

#### 2. Web-Based Reporting Interface
- **Real-time Dashboard**: Live statistics and performance monitoring
- **Historical Reports**: Session data, client analytics, performance trends
- **AJAX Endpoints**: Dynamic content updates without page refresh
- **Multiple Report Types**: Default, client, session, and custom reports

#### 3. Database Storage
- **SQLite Backend**: Local database storage for statistics
- **Session Tracking**: Per-user session data with unique identifiers
- **Statistical Analysis**: Min/max/average/mode calculations for metrics
- **Data Persistence**: Long-term storage of performance data

#### 4. HTTP API Endpoints
- **Statistics API**: `/SStats/` endpoint for accessing reports
- **ViewerStats Capability**: `/VS` endpoint for client data submission
- **Multiple Formats**: HTML, JSON, JavaScript, CSS content types

### Data Collection

#### Client Performance Metrics
- **Frame Rate**: Viewer FPS performance data
- **Simulation FPS**: Server-side simulation performance
- **Ping Statistics**: Network latency measurements
- **Memory Usage**: Client memory consumption
- **Agents in View**: Number of avatars visible to the client

#### Network Performance
- **Packet Statistics**: Incoming/outgoing packet rates and sizes
- **Download Data**: Object, texture, and world data transfer rates
- **Network Failures**: Dropped packets, failed resends, invalid packets
- **Bandwidth Usage**: Detailed network utilization metrics

#### System Information
- **Hardware Details**: CPU, GPU, RAM specifications
- **Operating System**: OS version and platform information
- **Client Version**: Viewer version and build information
- **Language Settings**: Client language preferences

#### User Activity
- **Session Duration**: Total time spent in the virtual world
- **Movement Tracking**: Distance traveled within regions
- **Region Visits**: Number of regions visited during session
- **Login Statistics**: Session start and end times

## Configuration

### Section: [WebStats]
```ini
[WebStats]
    ; Enable/disable the web statistics module
    ; Default: false
    enabled = true
```

### Factory Integration
The module is loaded through the `CoreModuleFactory` with the following behavior:
- **Configuration-Driven**: Only loaded when `[WebStats] enabled = true`
- **Reflection-Based**: Loaded via reflection to avoid hard dependency on OptionalModules
- **Database Auto-Creation**: Automatically creates SQLite database on first run

### Database Configuration
The module automatically creates a local SQLite database:
- **File**: `LocalUserStatistics.db` in the OpenSim bin directory
- **Auto-Creation**: Database and tables created automatically
- **Schema Management**: Built-in table creation with proper indexes

## Implementation Details

### Initialization Process
1. **Configuration Check**: Reads `[WebStats]` section for enable/disable setting
2. **Database Setup**: Creates SQLite connection and initializes tables
3. **Report Registration**: Sets up built-in report handlers
4. **HTTP Handler Registration**: Registers web endpoints for statistics access

### Region Integration
1. **Event Handlers**: Registers for caps, client, and agent events
2. **Session Tracking**: Creates user sessions for statistics collection
3. **Statistics Counters**: Initializes per-region performance counters
4. **Update Scheduling**: Sets up periodic statistics updates

### Data Flow
1. **Client Data**: Received via ViewerStats capability
2. **Server Stats**: Collected from scene statistics reporter
3. **Processing**: Statistical analysis (min/max/average/mode)
4. **Storage**: Persisted to SQLite database
5. **Reporting**: Made available via web interface

### Web Interface

#### Available Reports
- **default.report**: Main dashboard with overview statistics
- **clients.report**: Client-specific analytics and performance data
- **sessions.report**: Session-based reporting and user analytics
- **activeconnectionsajax.html**: Real-time connection monitoring
- **simstatsajax.html**: Live simulation statistics
- **activelogajax.html**: Real-time log file monitoring

#### Static Resources
- **prototype.js**: JavaScript framework for dynamic functionality
- **updater.js**: AJAX update mechanisms for real-time data
- **jquery.js**: jQuery library for enhanced web interface
- **sim.css**: Stylesheet for statistics dashboard
- **sim.html**: Main statistics HTML interface

#### API Endpoints
- **GET /SStats/**: Main statistics endpoint
- **GET /SStats/[report]**: Specific report access
- **POST /VS**: ViewerStats capability endpoint

### Database Schema

#### Table: stats_session_data
Primary table storing comprehensive session statistics:

```sql
CREATE TABLE stats_session_data (
    session_id VARCHAR(36) PRIMARY KEY,     -- Unique session identifier
    agent_id VARCHAR(36),                   -- Avatar UUID
    region_id VARCHAR(36),                  -- Region UUID
    last_updated INT,                       -- Unix timestamp
    remote_ip VARCHAR(16),                  -- Client IP address
    name_f VARCHAR(50),                     -- First name
    name_l VARCHAR(50),                     -- Last name

    -- Performance Statistics
    avg_agents_in_view FLOAT,               -- Average avatars visible
    min_agents_in_view INT,                 -- Minimum avatars visible
    max_agents_in_view INT,                 -- Maximum avatars visible
    mode_agents_in_view INT,                -- Most common avatar count

    avg_fps FLOAT,                          -- Average viewer FPS
    min_fps FLOAT,                          -- Minimum viewer FPS
    max_fps FLOAT,                          -- Maximum viewer FPS
    mode_fps FLOAT,                         -- Most common FPS

    avg_sim_fps FLOAT,                      -- Average simulation FPS
    min_sim_fps FLOAT,                      -- Minimum simulation FPS
    max_sim_fps FLOAT,                      -- Maximum simulation FPS
    mode_sim_fps FLOAT,                     -- Most common sim FPS

    avg_ping FLOAT,                         -- Average network ping
    min_ping FLOAT,                         -- Minimum ping
    max_ping FLOAT,                         -- Maximum ping
    mode_ping FLOAT,                        -- Most common ping

    -- System Information
    a_language VARCHAR(25),                 -- Client language
    mem_use FLOAT,                          -- Memory usage
    s_cpu VARCHAR(255),                     -- CPU information
    s_gpu VARCHAR(255),                     -- GPU information
    s_os VARCHAR(2255),                     -- Operating system
    s_ram INT,                              -- RAM amount

    -- Activity Metrics
    meters_traveled FLOAT,                  -- Distance traveled
    regions_visited INT,                    -- Regions visited count
    run_time FLOAT,                         -- Session duration
    start_time FLOAT,                       -- Session start time
    client_version VARCHAR(255),            -- Viewer version

    -- Network Statistics
    d_object_kb FLOAT,                      -- Object download KB
    d_texture_kb FLOAT,                     -- Texture download KB
    d_world_kb FLOAT,                       -- World data download KB
    n_in_kb FLOAT,                          -- Network input KB
    n_in_pk INT,                            -- Input packets
    n_out_kb FLOAT,                         -- Network output KB
    n_out_pk INT,                           -- Output packets

    -- Failure Statistics
    f_dropped INT,                          -- Dropped packets
    f_failed_resends INT,                   -- Failed resends
    f_invalid INT,                          -- Invalid packets
    f_off_circuit INT,                      -- Off-circuit failures
    f_resent INT,                           -- Resent packets
    f_send_packet INT                       -- Send packet failures
);
```

## Usage Examples

### Basic Configuration
```ini
[WebStats]
enabled = true
```

### Accessing Web Interface
```bash
# Main statistics dashboard
http://your-opensim-server:9000/SStats/

# Specific reports
http://your-opensim-server:9000/SStats/default.report
http://your-opensim-server:9000/SStats/clients.report
http://your-opensim-server:9000/SStats/sessions.report

# Real-time AJAX endpoints
http://your-opensim-server:9000/SStats/activeconnectionsajax.html
http://your-opensim-server:9000/SStats/simstatsajax.html

# JSON format output
http://your-opensim-server:9000/SStats/default.report?json=true
```

### Database Queries
```sql
-- Get average FPS by client version
SELECT client_version, AVG(avg_fps) as average_fps, COUNT(*) as sessions
FROM stats_session_data
GROUP BY client_version
ORDER BY average_fps DESC;

-- Find sessions with performance issues
SELECT name_f, name_l, avg_fps, avg_ping, session_id
FROM stats_session_data
WHERE avg_fps < 20 OR avg_ping > 500;

-- Regional performance analysis
SELECT region_id, AVG(avg_sim_fps) as avg_region_fps, COUNT(*) as sessions
FROM stats_session_data
GROUP BY region_id
ORDER BY avg_region_fps DESC;
```

### Custom Report Development
```csharp
// Example custom report implementation
public class CustomStatsReport : IStatsController
{
    public Hashtable ProcessModel(Hashtable pParams)
    {
        // Access database connection
        SQLiteConnection db = (SQLiteConnection)pParams["DatabaseConnection"];

        // Access scene list
        List<Scene> scenes = (List<Scene>)pParams["Scenes"];

        // Process statistics and return model
        Hashtable model = new Hashtable();
        model["custom_data"] = ProcessCustomStatistics(db);
        return model;
    }

    public string RenderView(Hashtable model)
    {
        // Return HTML representation
        return GenerateHTML(model);
    }

    public string RenderJson(Hashtable model)
    {
        // Return JSON representation
        return GenerateJSON(model);
    }
}
```

## Performance Considerations

### Database Performance
- **Connection Pooling**: Single shared SQLite connection
- **Batch Operations**: Statistics updated in batches
- **Index Usage**: Primary keys and frequently queried columns indexed
- **Transaction Management**: Proper transaction boundaries for consistency

### Memory Usage
- **Session Caching**: Active sessions kept in memory
- **Circular Buffers**: Limited-size collections for statistical calculations
- **Garbage Collection**: Proper cleanup of expired sessions
- **Resource Management**: Database connections properly disposed

### Network Impact
- **Capability-Based**: Uses existing capability infrastructure
- **Optional Collection**: Only collects data from clients that support it
- **Minimal Overhead**: Lightweight data collection protocol
- **Configurable Updates**: Update frequency can be adjusted

### Web Interface Performance
- **Static Resources**: CSS/JS files served efficiently
- **AJAX Updates**: Partial page updates reduce bandwidth
- **Concurrent Access**: Thread-safe report generation
- **Content Caching**: Appropriate HTTP caching headers

## Troubleshooting

### Common Issues

#### 1. Module Not Loading
**Symptoms**: No statistics collection, web interface unavailable
**Solutions**:
- Check `[WebStats] enabled = true` in configuration
- Verify OptionalModules.dll is available
- Check for initialization errors in log files

#### 2. Database Errors
**Symptoms**: Statistics not saving, database connection failures
**Solutions**:
- Verify write permissions to OpenSim bin directory
- Check SQLite library availability
- Monitor disk space for database growth

#### 3. Web Interface Not Accessible
**Symptoms**: HTTP 404 errors, reports not loading
**Solutions**:
- Verify HTTP server is running
- Check firewall rules for HTTP ports
- Confirm URL paths and endpoints

#### 4. Missing Statistics
**Symptoms**: Empty reports, no client data
**Solutions**:
- Verify clients support ViewerStats capability
- Check capability registration in log files
- Ensure clients are sending statistics data

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization and configuration
- Session creation and management
- Database operations and updates
- HTTP request handling and routing
- Capability registration and data processing

### Database Maintenance
- **Cleanup**: Periodically remove old session data
- **Backup**: Regular database backups for data preservation
- **Analysis**: Use SQL queries for performance analysis
- **Monitoring**: Watch database file size growth

## Integration Notes

### Factory Loading
- Loaded via `CoreModuleFactory.CreateSharedModules()` using reflection
- Requires `OpenSim.Region.OptionalModules.dll` assembly
- Graceful degradation if OptionalModules unavailable

### Dependencies
- **SQLite**: System.Data.SQLite for database operations
- **HTTP Server**: MainServer instance for web endpoints
- **Capabilities**: Scene capability infrastructure

### Client Compatibility
- **Viewer Support**: Requires viewers with ViewerStats capability
- **Data Format**: LLSD/OSD structured data format
- **Backward Compatibility**: Graceful handling of missing client data

## Security Considerations

### Data Privacy
- **Personal Information**: Collects system specs and performance data
- **Session Tracking**: Links data to avatar identities
- **IP Addresses**: Records client IP addresses for sessions

### Access Control
- **Public Interface**: Web statistics may be publicly accessible
- **Data Filtering**: Consider filtering sensitive information
- **Authentication**: Add authentication for administrative reports

### Database Security
- **File Permissions**: Secure database file access permissions
- **Data Retention**: Implement data retention policies
- **Backup Security**: Secure backup files appropriately

## See Also
- [MonitorModule](./MonitorModule.md) - Related monitoring functionality
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [Capabilities Documentation](../docs/Capabilities.md) - Capability system details
- [HTTP Server Configuration](../docs/HttpServer.md) - Web endpoint setup
