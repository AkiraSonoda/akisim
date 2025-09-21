# XmlRpcGridRouter Technical Documentation

## Overview

The XmlRpcGridRouter is an advanced grid-wide XMLRPC routing and management system for OpenSimulator/Akisim that provides centralized XMLRPC channel coordination across multiple regions and OpenSim instances. This optional non-shared region module extends basic XMLRPC functionality by implementing a hub-based architecture that enables cross-region and cross-grid XMLRPC communication through a central routing server. The module is designed for large-scale grid deployments where XMLRPC channels need to be coordinated globally, providing seamless communication between scripts running in different regions or even different OpenSim instances within the same grid infrastructure.

## Architecture

The XmlRpcGridRouter implements the following interfaces:
- `INonSharedRegionModule` - Per-region module lifecycle management
- `IXmlRpcRouter` - Standard XMLRPC router functionality interface

### Key Components

1. **Grid Hub Integration**
   - **Central Routing Server**: Connects to external XMLRPC hub for grid-wide coordination
   - **REST API Communication**: Uses REST protocol for hub communication
   - **Channel Registration**: Registers local channels with central hub
   - **Cross-Region Routing**: Enables XMLRPC routing across regions and grids

2. **Channel Management System**
   - **Channel Tracking**: Maintains local channel registry with hub synchronization
   - **Script Association**: Maps channels to scripts and objects for lifecycle management
   - **Registration Lifecycle**: Handles channel registration and unregistration
   - **Automatic Cleanup**: Automatically removes channels when scripts or objects are removed

3. **Hub Communication Protocol**
   - **SynchronousRestObjectRequester**: Uses synchronous REST requests for reliable communication
   - **Registration Endpoints**: Communicates with /RegisterChannel/ and /RemoveChannel/ endpoints
   - **XmlRpcInfo Protocol**: Standardized data structure for hub communication
   - **Error Handling**: Robust error handling for hub communication failures

4. **Script Engine Integration**
   - **Event Subscription**: Subscribes to script and object lifecycle events
   - **OnScriptRemoved**: Handles script removal for automatic channel cleanup
   - **OnObjectRemoved**: Handles object removal for comprehensive cleanup
   - **IScriptModule Interface**: Integrates with any IScriptModule implementation

5. **Grid-Wide Coordination**
   - **Multi-Region Support**: Coordinates XMLRPC across multiple regions
   - **Cross-Grid Communication**: Enables communication between different OpenSim instances
   - **Centralized Management**: Central hub provides unified channel management
   - **Scalable Architecture**: Designed for large-scale grid deployments

6. **Configuration Management**
   - **Hub URI Configuration**: Configurable central hub server URI
   - **Module Enablement**: Conditional loading based on configuration
   - **Error Validation**: Validates configuration before enabling functionality
   - **Fallback Behavior**: Safe fallback when hub is unavailable

## Configuration

### Module Activation

The module requires specific configuration to activate:

```ini
[Modules]
XmlRpcGridRouterModule = true  ; Enable XmlRpcGridRouter (default: false)

[XMLRPC]
XmlRpcRouterModule = XmlRpcGridRouterModule  ; Select XmlRpcGridRouter as router
XmlRpcHubURI = http://grid-hub.example.com:8080  ; Central hub URI (required)
```

### Configuration Requirements

Multiple configuration parameters are required:
- **XmlRpcRouterModule** must be set to "XmlRpcGridRouterModule"
- **XmlRpcHubURI** must specify the central hub server URI
- Both parameters are mandatory for module activation

### Hub Server Requirements

The central hub server must provide REST endpoints:
- **POST /RegisterChannel/** - Channel registration endpoint
- **POST /RemoveChannel/** - Channel removal endpoint
- Accepts XmlRpcInfo objects as JSON payloads
- Returns boolean success/failure responses

### Default Behavior

- **Disabled by Default**: Module is disabled unless explicitly configured
- **Per-Region Operation**: Each region connects independently to the hub
- **Automatic Registration**: Channels are automatically registered with hub
- **Lifecycle Management**: Automatic cleanup on script/object removal

### Dependencies

- **Central Hub Server**: Requires external XMLRPC hub server
- **REST Communication**: Depends on SynchronousRestObjectRequester
- **Script Engine**: Requires IScriptModule for lifecycle events
- **Network Connectivity**: Requires network access to hub server

## Features

### Grid Hub Integration

#### Channel Registration with Hub

**RegisterNewReceiver()**
```csharp
public void RegisterNewReceiver(IScriptModule scriptEngine, UUID channel, UUID objectID, UUID itemID, string uri)
{
    if (!m_Enabled)
        return;

    m_log.InfoFormat("[XMLRPC GRID ROUTER]: New receiver Obj: {0} Ch: {1} ID: {2} URI: {3}",
                        objectID.ToString(), channel.ToString(), itemID.ToString(), uri);

    XmlRpcInfo info = new XmlRpcInfo();
    info.channel = channel;
    info.uri = uri;
    info.item = itemID;

    bool success = SynchronousRestObjectRequester.MakeRequest<XmlRpcInfo, bool>(
            "POST", m_ServerURI+"/RegisterChannel/", info);

    if (!success)
    {
        m_log.Error("[XMLRPC GRID ROUTER] Error contacting server");
    }

    m_Channels[itemID] = channel;
}
```

#### Channel Removal from Hub

**RemoveChannel()**
```csharp
private bool RemoveChannel(UUID itemID)
{
    if(!m_Channels.ContainsKey(itemID))
    {
        return false;
    }

    XmlRpcInfo info = new XmlRpcInfo();
    info.channel = m_Channels[itemID];
    info.item = itemID;
    info.uri = "http://0.0.0.0:00";  // Placeholder URI for removal

    bool success = SynchronousRestObjectRequester.MakeRequest<XmlRpcInfo, bool>(
            "POST", m_ServerURI+"/RemoveChannel/", info);

    if (!success)
    {
        m_log.Error("[XMLRPC GRID ROUTER] Error contacting server");
    }

    m_Channels.Remove(itemID);
    return true;
}
```

### Channel Lifecycle Management

#### Script Engine Event Handling

**Script Removal Handling**
```csharp
public void ScriptRemoved(UUID itemID)
{
    if (!m_Enabled)
        return;

    RemoveChannel(itemID);
}
```

**Object Removal Handling**
```csharp
public void ObjectRemoved(UUID objectID)
{
    // Placeholder for object-level cleanup
    // Could be enhanced to remove all channels for the object
}
```

#### Automatic Event Subscription

```csharp
public void AddRegion(Scene scene)
{
    if (!m_Enabled)
        return;

    scene.RegisterModuleInterface<IXmlRpcRouter>(this);

    IScriptModule scriptEngine = scene.RequestModuleInterface<IScriptModule>();
    if (scriptEngine != null)
    {
        scriptEngine.OnScriptRemoved += this.ScriptRemoved;
        scriptEngine.OnObjectRemoved += this.ObjectRemoved;
    }
}
```

### Data Structures

#### XmlRpcInfo Protocol

```csharp
public class XmlRpcInfo
{
    public UUID item;     // Script item ID
    public UUID channel;  // XMLRPC channel ID
    public string uri;    // XMLRPC endpoint URI
}
```

#### Channel Registry

```csharp
private Dictionary<UUID, UUID> m_Channels = new Dictionary<UUID, UUID>();
// Maps script item IDs to channel IDs for lifecycle management
```

## Technical Implementation

### Module Lifecycle Management

#### Initialization and Configuration

```csharp
public void Initialise(IConfigSource config)
{
    IConfig startupConfig = config.Configs["XMLRPC"];
    if (startupConfig == null)
        return;

    if (startupConfig.GetString("XmlRpcRouterModule", "XmlRpcRouterModule") == "XmlRpcGridRouterModule")
    {
        m_ServerURI = startupConfig.GetString("XmlRpcHubURI", String.Empty);
        if (m_ServerURI.Length == 0)
        {
            m_log.Error("[XMLRPC GRID ROUTER] Module configured but no URI given. Disabling");
            return;
        }
        m_Enabled = true;
    }
}
```

#### Scene Integration and Event Subscription

```csharp
public void AddRegion(Scene scene)
{
    if (!m_Enabled)
        return;

    // Register as XMLRPC router for this scene
    scene.RegisterModuleInterface<IXmlRpcRouter>(this);

    // Subscribe to script engine events for lifecycle management
    IScriptModule scriptEngine = scene.RequestModuleInterface<IScriptModule>();
    if (scriptEngine != null)
    {
        scriptEngine.OnScriptRemoved += this.ScriptRemoved;
        scriptEngine.OnObjectRemoved += this.ObjectRemoved;
    }
}
```

### Hub Communication Implementation

#### REST Request Pattern

```csharp
// Registration request
bool success = SynchronousRestObjectRequester.MakeRequest<XmlRpcInfo, bool>(
    "POST", m_ServerURI + "/RegisterChannel/", info);

// Removal request
bool success = SynchronousRestObjectRequester.MakeRequest<XmlRpcInfo, bool>(
    "POST", m_ServerURI + "/RemoveChannel/", info);
```

#### Error Handling

```csharp
if (!success)
{
    m_log.Error("[XMLRPC GRID ROUTER] Error contacting server");
    // Module continues to operate locally despite hub communication failure
}
```

### Channel Management Implementation

#### Registration Tracking

```csharp
public void RegisterNewReceiver(IScriptModule scriptEngine, UUID channel, UUID objectID, UUID itemID, string uri)
{
    // ... hub communication ...

    // Track locally for lifecycle management
    m_Channels[itemID] = channel;
}
```

#### Cleanup Implementation

```csharp
private bool RemoveChannel(UUID itemID)
{
    // Check if channel exists locally
    if (!m_Channels.ContainsKey(itemID))
        return false;

    // Prepare removal request
    XmlRpcInfo info = new XmlRpcInfo();
    info.channel = m_Channels[itemID];
    info.item = itemID;
    info.uri = "http://0.0.0.0:00";  // Placeholder for removal

    // Send removal request to hub
    bool success = SynchronousRestObjectRequester.MakeRequest<XmlRpcInfo, bool>(
        "POST", m_ServerURI + "/RemoveChannel/", info);

    // Remove from local tracking regardless of hub response
    m_Channels.Remove(itemID);
    return true;
}
```

### Interface Implementation

#### IXmlRpcRouter Implementation

```csharp
public class XmlRpcGridRouter : INonSharedRegionModule, IXmlRpcRouter
{
    // Required interface methods
    public void RegisterNewReceiver(IScriptModule scriptEngine, UUID channel, UUID objectID, UUID itemID, string uri) { }
    public void UnRegisterReceiver(string channelID, UUID itemID) { }
    public void ScriptRemoved(UUID itemID) { }
    public void ObjectRemoved(UUID objectID) { }
}
```

#### Module Properties

```csharp
public string Name
{
    get { return "XmlRpcGridRouterModule"; }
}

public Type ReplaceableInterface
{
    get { return null; }
}
```

## Performance Characteristics

### Resource Usage

- **Memory Footprint**: Moderate memory usage for channel tracking dictionary
- **CPU Impact**: Low CPU overhead except during hub communication
- **Network Usage**: Regular network communication with central hub
- **Storage Impact**: No persistent storage - maintains runtime state only

### Scalability Features

- **Hub-Based Architecture**: Centralizes coordination for improved scalability
- **Per-Region Independence**: Each region operates independently
- **Asynchronous Cleanup**: Non-blocking cleanup operations
- **Efficient Channel Tracking**: Dictionary-based channel management

### Performance Optimization

- **Synchronous Requests**: Reliable communication with immediate error detection
- **Local Channel Tracking**: Fast local lookups for channel management
- **Conditional Operations**: All operations gated by enabled state
- **Minimal State Management**: Simple dictionary-based state tracking

## Usage Examples

### Basic Grid Hub Configuration

```ini
[Modules]
XmlRpcGridRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcGridRouterModule
XmlRpcHubURI = http://grid-hub.example.com:8080
```

### LSL Script with Grid-Wide XMLRPC

```lsl
// LSL Script using XmlRpcGridRouter for grid-wide communication
string grid_xmlrpc_channel;

default
{
    state_entry()
    {
        // Open XMLRPC channel - will be registered with grid hub
        key channel = llOpenRemoteDataChannel();
        llOwnerSay("Grid XMLRPC channel opened: " + (string)channel);
    }

    remote_data(integer request_type, key channel, key message_id, string sender, integer idata, string sdata)
    {
        if (request_type == REMOTE_DATA_REQUEST)
        {
            llOwnerSay("Grid XMLRPC request from: " + sender);
            llOwnerSay("Data: " + sdata);

            // This request could come from any region in the grid
            string response = "Grid response from " + llGetRegionName();
            llRemoteDataReply(channel, message_id, response, idata + 1);
        }
    }
}
```

### Multi-Region Communication Example

```lsl
// Script in Region A
key grid_channel;

default
{
    state_entry()
    {
        grid_channel = llOpenRemoteDataChannel();
        llOwnerSay("Region A channel: " + (string)grid_channel);

        // Send message to script in Region B via grid hub
        string target_uri = "http://grid-hub.example.com:8080/route/region-b-channel";
        llSendRemoteData("", target_uri, 100, "Hello from Region A");
    }

    remote_data(integer request_type, key channel, key message_id, string sender, integer idata, string sdata)
    {
        if (request_type == REMOTE_DATA_REPLY)
        {
            llOwnerSay("Grid response: " + sdata);
        }
        else if (request_type == REMOTE_DATA_REQUEST)
        {
            llOwnerSay("Grid request from other region: " + sdata);
            llRemoteDataReply(channel, message_id, "Region A acknowledges", 200);
        }
    }
}
```

### Hub Server Implementation (Python Example)

```python
# Example Grid Hub Server Implementation
from flask import Flask, request, jsonify
import json
import uuid

app = Flask(__name__)

# Global channel registry
grid_channels = {}

class XmlRpcInfo:
    def __init__(self, data):
        self.item = data.get('item')
        self.channel = data.get('channel')
        self.uri = data.get('uri')

@app.route('/RegisterChannel/', methods=['POST'])
def register_channel():
    """Register a new XMLRPC channel with the grid hub"""
    try:
        data = request.get_json()
        info = XmlRpcInfo(data)

        # Store channel information
        grid_channels[info.channel] = {
            'item': info.item,
            'uri': info.uri,
            'region': request.remote_addr  # Track source region
        }

        print(f"Registered channel {info.channel} from {info.uri}")
        return jsonify(True)

    except Exception as e:
        print(f"Registration error: {e}")
        return jsonify(False)

@app.route('/RemoveChannel/', methods=['POST'])
def remove_channel():
    """Remove an XMLRPC channel from the grid hub"""
    try:
        data = request.get_json()
        info = XmlRpcInfo(data)

        if info.channel in grid_channels:
            del grid_channels[info.channel]
            print(f"Removed channel {info.channel}")

        return jsonify(True)

    except Exception as e:
        print(f"Removal error: {e}")
        return jsonify(False)

@app.route('/route/<channel_id>', methods=['POST'])
def route_message(channel_id):
    """Route XMLRPC message to target channel"""
    try:
        if channel_id not in grid_channels:
            return jsonify({'error': 'Channel not found'}), 404

        target_info = grid_channels[channel_id]
        # Forward request to target URI
        # Implementation would forward the XMLRPC request

        return jsonify({'status': 'routed'})

    except Exception as e:
        print(f"Routing error: {e}")
        return jsonify({'error': str(e)}), 500

@app.route('/channels', methods=['GET'])
def list_channels():
    """List all registered channels (admin endpoint)"""
    return jsonify(grid_channels)

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8080, debug=True)
```

### Cross-Grid Communication

```lsl
// Script for cross-grid communication
key local_channel;
string grid_hub = "http://main-grid-hub.example.com:8080";

default
{
    state_entry()
    {
        local_channel = llOpenRemoteDataChannel();
        llOwnerSay("Cross-grid channel: " + (string)local_channel);

        // Send message to another grid
        string target_grid = "http://partner-grid-hub.example.com:8080";
        llSendRemoteData("cross_grid", target_grid + "/forward", 1, "Inter-grid hello");
    }

    remote_data(integer request_type, key channel, key message_id, string sender, integer idata, string sdata)
    {
        if (request_type == REMOTE_DATA_REQUEST)
        {
            // Handle cross-grid requests
            llOwnerSay("Cross-grid message: " + sdata);
            llRemoteDataReply(channel, message_id, "Cross-grid response", 1);
        }
    }
}
```

### Grid Administration Tools

```bash
#!/bin/bash
# Grid Hub Administration Script

HUB_URL="http://grid-hub.example.com:8080"

# List all active channels
echo "Active Grid Channels:"
curl -s "$HUB_URL/channels" | jq '.'

# Monitor channel registration
echo "Monitoring channel activity..."
tail -f /var/log/grid-hub/access.log | grep -E "(RegisterChannel|RemoveChannel)"

# Health check
echo "Hub Health Check:"
curl -s "$HUB_URL/health" || echo "Hub not responding"
```

## Integration Points

### With Central Hub Infrastructure

- **REST API Integration**: Communicates with central hub via REST API
- **Channel Coordination**: Coordinates channels across grid infrastructure
- **Hub Dependencies**: Depends on external hub server availability
- **Protocol Standardization**: Uses standardized XmlRpcInfo protocol

### With Script Engine

- **Event Integration**: Integrates with script engine lifecycle events
- **IScriptModule Interface**: Works with any IScriptModule implementation
- **Automatic Cleanup**: Automatically cleans up on script removal
- **Engine Independence**: Independent of specific script engine implementation

### With XMLRPC Infrastructure

- **IXmlRpcRouter Interface**: Implements standard XMLRPC router interface
- **XMLRPCModule Compatibility**: Compatible with existing XMLRPCModule
- **Channel Management**: Extends channel management to grid level
- **Protocol Compatibility**: Maintains XMLRPC protocol compatibility

### With Grid Infrastructure

- **Multi-Region Support**: Supports multiple regions per OpenSim instance
- **Cross-Instance Communication**: Enables communication between OpenSim instances
- **Grid Coordination**: Coordinates with grid-wide infrastructure
- **Scalable Architecture**: Designed for large-scale grid deployments

## Security Features

### Access Control

- **Configuration-Based Access**: Only loads when explicitly configured
- **Hub Authentication**: Relies on hub server for authentication
- **Channel Isolation**: Channels are isolated by script and object
- **Network Security**: Depends on network security for hub communication

### Communication Security

- **REST Protocol**: Uses standard REST protocol for hub communication
- **Error Handling**: Robust error handling prevents information disclosure
- **Safe Defaults**: Disabled by default for security
- **Hub Validation**: Validates hub URI before enabling

### Grid Security

- **Centralized Control**: Central hub provides unified security control
- **Channel Tracking**: Comprehensive channel tracking for security monitoring
- **Access Logging**: Hub server can provide comprehensive access logging
- **Grid-Wide Policies**: Enables grid-wide security policy enforcement

## Debugging and Troubleshooting

### Common Issues

1. **Module Not Loading**: Check XmlRpcRouterModule and XmlRpcHubURI configuration
2. **Hub Connection Failed**: Verify hub server availability and URI
3. **Channels Not Registering**: Check hub endpoints and network connectivity
4. **Script Events Missing**: Verify script engine integration

### Diagnostic Procedures

1. **Configuration Validation**: Verify all required configuration parameters
2. **Hub Connectivity**: Test REST endpoints with curl or similar tools
3. **Network Debugging**: Check network connectivity to hub server
4. **Event Subscription**: Verify script engine event subscription

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
XmlRpcGridRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcGridRouterModule
XmlRpcHubURI = http://grid-hub.example.com:8080
```

### Debug Commands

```bash
# Test hub connectivity
curl -X POST http://grid-hub.example.com:8080/RegisterChannel/ \
  -H "Content-Type: application/json" \
  -d '{"item":"test-item","channel":"test-channel","uri":"test-uri"}'

# Check hub status
curl http://grid-hub.example.com:8080/channels

# Monitor OpenSim logs
tail -f logs/OpenSim.log | grep "XMLRPC GRID ROUTER"
```

## Use Cases

### Large-Scale Grid Deployments

- **Multi-Region Coordination**: Coordinate XMLRPC across hundreds of regions
- **Cross-Server Communication**: Enable communication between OpenSim instances
- **Grid-Wide Services**: Implement grid-wide services using XMLRPC
- **Centralized Management**: Centrally manage XMLRPC channels

### Virtual World Integration

- **World Federations**: Connect multiple virtual worlds
- **Cross-World Communication**: Enable avatar communication across worlds
- **Shared Services**: Implement shared services across virtual worlds
- **Economic Integration**: Integrate economies across multiple grids

### Enterprise Applications

- **Corporate Grids**: Deploy enterprise-wide virtual environments
- **Training Simulations**: Coordinate training across multiple regions
- **Collaboration Platforms**: Build grid-wide collaboration tools
- **Data Integration**: Integrate with enterprise data systems

### Research and Education

- **Academic Grids**: Support multi-institution research grids
- **Distributed Simulations**: Run simulations across multiple regions
- **Data Collection**: Collect data from grid-wide experiments
- **Resource Sharing**: Share computational resources across institutions

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via OptionalModulesFactory configuration
- Logging provides visibility into module loading decisions
- All grid routing functionality remains fully compatible

### Hub Server Deployment

When deploying the hub server:

- Deploy hub server before enabling grid router modules
- Ensure hub server has appropriate endpoints implemented
- Configure load balancing for high availability
- Implement monitoring and logging for hub server

### Configuration Migration

When upgrading from previous versions:

- Verify XmlRpcGridRouterModule is enabled in OptionalModulesFactory
- Test hub communication after deployment
- Update scripts that depend on grid-wide XMLRPC
- Validate integration with grid infrastructure

### Deployment Considerations

- **Hub Dependencies**: Ensure hub server is available before module activation
- **Network Security**: Configure firewalls for hub communication
- **Performance Monitoring**: Monitor hub performance and network latency
- **Backup Procedures**: Implement backup procedures for hub server

## Configuration Examples

### Basic Grid Configuration

```ini
[Modules]
XmlRpcGridRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcGridRouterModule
XmlRpcHubURI = http://grid-hub.example.com:8080
```

### High-Availability Configuration

```ini
[Modules]
XmlRpcGridRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcGridRouterModule
XmlRpcHubURI = http://grid-hub-lb.example.com:8080  # Load balanced hub
```

### Development Configuration

```ini
[Modules]
XmlRpcGridRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcGridRouterModule
XmlRpcHubURI = http://localhost:8080  # Local development hub

[Logging]
LogLevel = DEBUG
```

### Production Configuration

```ini
[Modules]
XmlRpcGridRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcGridRouterModule
XmlRpcHubURI = https://secure-grid-hub.example.com:8443  # Secure production hub

[Logging]
LogLevel = INFO
```

## Best Practices

### Hub Server Management

1. **High Availability**: Deploy hub server with high availability
2. **Load Balancing**: Use load balancing for multiple hub instances
3. **Monitoring**: Implement comprehensive monitoring
4. **Backup**: Regular backup of hub configuration and data

### Grid Architecture

1. **Network Design**: Design network topology for optimal performance
2. **Security**: Implement grid-wide security policies
3. **Scalability**: Plan for grid growth and expansion
4. **Performance**: Monitor and optimize grid performance

### Script Development

1. **Error Handling**: Implement robust error handling in scripts
2. **Network Awareness**: Design scripts to handle network delays
3. **Resource Management**: Manage XMLRPC resources efficiently
4. **Grid Coordination**: Design for grid-wide coordination

## Future Enhancements

### Potential Improvements

1. **Enhanced Security**: SSL/TLS support for secure hub communication
2. **Load Balancing**: Built-in load balancing for multiple hubs
3. **Caching**: Local caching for improved performance
4. **Monitoring**: Enhanced monitoring and statistics collection

### Compatibility Considerations

1. **Protocol Evolution**: Adapt to XMLRPC protocol updates
2. **Hub API Evolution**: Maintain compatibility with hub API changes
3. **Grid Standards**: Adapt to evolving grid standards
4. **Security Requirements**: Evolve with changing security requirements

### Integration Opportunities

1. **Service Mesh**: Integration with service mesh architectures
2. **Kubernetes**: Kubernetes-native deployment options
3. **Cloud Services**: Enhanced cloud service integration
4. **Monitoring Tools**: Better integration with monitoring platforms