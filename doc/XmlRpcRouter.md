# XmlRpcRouter Technical Documentation

## Overview

The XmlRpcRouter is a specialized XMLRPC routing and event management module for OpenSimulator/Akisim that provides lightweight XMLRPC channel routing functionality for LSL scripts. This optional non-shared region module serves as a simplified XMLRPC event dispatcher that handles URI registration and basic event routing for scripts using XMLRPC functionality. Unlike the full XMLRPCModule which provides comprehensive bidirectional XMLRPC communication, XmlRpcRouter focuses on script event notification and URI management, making it ideal for scenarios requiring basic XMLRPC integration with minimal overhead.

## Architecture

The XmlRpcRouter implements the following interfaces:
- `INonSharedRegionModule` - Per-region module lifecycle management
- `IXmlRpcRouter` - XMLRPC routing functionality interface

### Key Components

1. **Script Event Management**
   - **Event Dispatch**: Generates xmlrpc_uri events for script notification
   - **Script Engine Integration**: Direct integration with script engine event system
   - **Channel Registration**: Associates XMLRPC channels with scripts and objects
   - **URI Notification**: Notifies scripts of assigned XMLRPC URIs

2. **Router Interface Implementation**
   - **IXmlRpcRouter Interface**: Implements standard XMLRPC router functionality
   - **Registration Management**: Handles receiver registration and unregistration
   - **Script Lifecycle**: Tracks script and object lifecycle events
   - **Channel Management**: Manages XMLRPC channel assignments

3. **Lightweight Architecture**
   - **Minimal Overhead**: Simplified implementation with minimal resource usage
   - **Optional Loading**: Only loads when explicitly configured
   - **Per-Region Operation**: Independent operation per region
   - **Event-Driven Design**: Responds to script engine events

4. **Configuration Management**
   - **XMLRPC Configuration**: Uses XMLRPC configuration section
   - **Module Enablement**: Configurable module activation
   - **Selective Loading**: Loads only when specifically requested
   - **Backward Compatibility**: Maintains compatibility with existing configurations

## Configuration

### Module Activation

The module loads when explicitly configured:

```ini
[Modules]
XmlRpcRouterModule = true  ; Enable XmlRpcRouter (default: false)

[XMLRPC]
XmlRpcRouterModule = XmlRpcRouterModule  ; Activate XmlRpcRouter
```

### Configuration Requirements

Both configuration sections are required for activation:
- **[Modules]** section must enable XmlRpcRouterModule
- **[XMLRPC]** section must specify XmlRpcRouterModule

### Default Behavior

- **Disabled by Default**: Module is disabled unless explicitly configured
- **Per-Region Operation**: Operates independently in each region
- **Optional Functionality**: Supplements but doesn't replace XMLRPCModule
- **Lightweight Operation**: Minimal resource consumption when active

### Dependencies

- **Script Engine**: Requires active script engine for event posting
- **IXmlRpcRouter Interface**: Implements standardized router interface
- **Scene Management**: Requires scene context for module registration
- **Configuration System**: Depends on Nini configuration system

## Features

### XMLRPC Router Interface

The module implements the complete IXmlRpcRouter interface:

#### Registration Management

**RegisterNewReceiver()**
- Registers new XMLRPC receiver with script association
- Generates xmlrpc_uri event for script notification
- Associates channel, object, and script identifiers
- Provides URI callback to scripts

**UnRegisterReceiver()**
- Removes XMLRPC receiver registration
- Cleans up channel associations
- Currently implemented as stub (no-op)

#### Lifecycle Management

**ScriptRemoved()**
- Handles script removal lifecycle events
- Cleans up script-specific resources
- Currently implemented as stub (no-op)

**ObjectRemoved()**
- Handles object removal lifecycle events
- Cleans up object-specific resources
- Currently implemented as stub (no-op)

### Script Event Integration

#### Event Generation

**xmlrpc_uri Event**
```csharp
public void RegisterNewReceiver(IScriptModule scriptEngine, UUID channel, UUID objectID, UUID itemID, string uri)
{
    if (m_Enabled)
    {
        scriptEngine.PostScriptEvent(itemID, "xmlrpc_uri", new Object[] { uri });
    }
}
```

#### Script Engine Integration

- **Direct Event Posting**: Posts events directly to script engine
- **Event Parameters**: Provides URI as event parameter
- **Script Identification**: Uses itemID for targeted event delivery
- **Conditional Operation**: Only operates when module is enabled

### Module Interface Registration

#### Scene Integration

**Interface Registration**
```csharp
public void AddRegion(Scene scene)
{
    if (!m_Enabled)
        return;

    scene.RegisterModuleInterface<IXmlRpcRouter>(this);
}
```

**Interface Cleanup**
```csharp
public void RemoveRegion(Scene scene)
{
    if (!m_Enabled)
        return;

    scene.UnregisterModuleInterface<IXmlRpcRouter>(this);
}
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

    if (startupConfig.GetString("XmlRpcRouterModule", "XmlRpcRouterModule") == "XmlRpcRouterModule")
        m_Enabled = true;
}
```

#### Region Management

```csharp
public void AddRegion(Scene scene)
{
    if (!m_Enabled)
        return;

    scene.RegisterModuleInterface<IXmlRpcRouter>(this);
}

public void RemoveRegion(Scene scene)
{
    if (!m_Enabled)
        return;

    scene.UnregisterModuleInterface<IXmlRpcRouter>(this);
}
```

### Router Interface Implementation

#### Complete Interface Implementation

```csharp
public class XmlRpcRouter : INonSharedRegionModule, IXmlRpcRouter
{
    public void RegisterNewReceiver(IScriptModule scriptEngine, UUID channel, UUID objectID, UUID itemID, string uri)
    {
        if (m_Enabled)
        {
            scriptEngine.PostScriptEvent(itemID, "xmlrpc_uri", new Object[] { uri });
        }
    }

    public void UnRegisterReceiver(string channelID, UUID itemID)
    {
        // Stub implementation - cleanup would go here
    }

    public void ScriptRemoved(UUID itemID)
    {
        // Stub implementation - script cleanup would go here
    }

    public void ObjectRemoved(UUID objectID)
    {
        // Stub implementation - object cleanup would go here
    }
}
```

### Configuration Processing

#### Configuration Validation

```csharp
// Check for XMLRPC configuration section
IConfig startupConfig = config.Configs["XMLRPC"];
if (startupConfig == null)
    return;  // Module remains disabled

// Check for specific module configuration
if (startupConfig.GetString("XmlRpcRouterModule", "XmlRpcRouterModule") == "XmlRpcRouterModule")
    m_Enabled = true;
```

#### Module Properties

```csharp
public string Name
{
    get { return "XmlRpcRouterModule"; }
}

public Type ReplaceableInterface
{
    get { return null; }  // No interface replacement
}
```

### Event System Integration

#### Script Event Generation

```csharp
// Generate xmlrpc_uri event for script notification
scriptEngine.PostScriptEvent(itemID, "xmlrpc_uri", new Object[] { uri });
```

#### Event Parameters

- **Event Name**: "xmlrpc_uri"
- **Parameters**: Array containing URI string
- **Target**: Specific script identified by itemID
- **Timing**: Immediate event posting upon registration

## Performance Characteristics

### Resource Usage

- **Memory Footprint**: Minimal memory usage - only module state and interface registration
- **CPU Impact**: Negligible CPU overhead - only event posting when active
- **Network Usage**: No network usage - operates locally within region
- **Storage Impact**: No persistent storage requirements

### Scalability Features

- **Per-Region Independence**: Each region operates independently
- **Event-Driven Operation**: Only active during registration events
- **Minimal State**: No persistent state management required
- **Lightweight Design**: Minimal overhead for basic routing functionality

### Performance Optimization

- **Conditional Operation**: All operations gated by m_Enabled flag
- **Direct Event Posting**: Direct script engine integration without queuing
- **Stub Implementations**: Placeholder methods ready for future enhancement
- **Simple Architecture**: Straightforward implementation without complex logic

## Usage Examples

### Basic XMLRPC Router Configuration

```ini
[Modules]
XmlRpcRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcRouterModule
```

### LSL Script Integration

```lsl
// LSL Script using XmlRpcRouter
string xmlrpc_uri;

default
{
    state_entry()
    {
        // Open XMLRPC channel - will trigger xmlrpc_uri event via XmlRpcRouter
        llOpenRemoteDataChannel();
    }

    xmlrpc_uri(string uri)
    {
        // Event generated by XmlRpcRouter when URI is assigned
        xmlrpc_uri = uri;
        llOwnerSay("XMLRPC URI assigned: " + uri);

        // Use the URI for external communication
        llOwnerSay("Send XMLRPC requests to: " + uri);
    }

    remote_data(integer request_type, key channel, key message_id, string sender, integer idata, string sdata)
    {
        // Handle incoming XMLRPC requests
        if (request_type == REMOTE_DATA_REQUEST)
        {
            llOwnerSay("XMLRPC request received:");
            llOwnerSay("Data: " + sdata);

            // Send response
            llRemoteDataReply(channel, message_id, "Response: " + sdata, idata + 1);
        }
    }
}
```

### Multi-Script XMLRPC System

```lsl
// Master Script - Coordinates XMLRPC across multiple scripts
list script_uris;
list script_names;

default
{
    state_entry()
    {
        // Initialize XMLRPC for coordination
        llOpenRemoteDataChannel();

        // Tell other scripts to initialize their XMLRPC
        llMessageLinked(LINK_SET, 1000, "init_xmlrpc", "");
    }

    xmlrpc_uri(string uri)
    {
        llOwnerSay("Master XMLRPC URI: " + uri);

        // Store master URI
        script_uris = [uri] + script_uris;
        script_names = ["master"] + script_names;
    }

    link_message(integer sender_num, integer num, string str, key id)
    {
        if (num == 1001)  // URI registration from other scripts
        {
            script_uris = script_uris + [str];
            script_names = script_names + [(string)id];

            llOwnerSay("Registered script URI: " + str + " for " + (string)id);
        }
        else if (num == 1000)  // Initialize XMLRPC
        {
            llOpenRemoteDataChannel();
        }
    }

    remote_data(integer request_type, key channel, key message_id, string sender, integer idata, string sdata)
    {
        if (request_type == REMOTE_DATA_REQUEST)
        {
            // Route request based on content or distribute to other scripts
            llOwnerSay("Master received: " + sdata);

            // Forward to appropriate script or handle directly
            llMessageLinked(LINK_SET, 2000, sdata, channel);

            llRemoteDataReply(channel, message_id, "Processed by master", 200);
        }
    }
}
```

### External Integration Example

```python
# Python script integrating with XmlRpcRouter-enabled OpenSim
import xmlrpc.client
import json

class OpenSimXMLRPCClient:
    def __init__(self, base_url):
        self.base_url = base_url

    def send_request(self, uri, data):
        """Send XMLRPC request to OpenSim script"""
        try:
            # Connect to the URI provided by XmlRpcRouter
            client = xmlrpc.client.ServerProxy(uri)

            # Send request to llRemoteData handler
            response = client.llRemoteData({
                "Channel": "",  # Will be filled by OpenSim
                "IntValue": data.get("int_value", 0),
                "StringValue": json.dumps(data)
            })

            return response
        except Exception as e:
            print(f"XMLRPC Error: {e}")
            return None

# Usage example
client = OpenSimXMLRPCClient("http://opensim-server")

# Send data to script
data = {
    "command": "update_status",
    "value": "active",
    "timestamp": "2025-01-01T00:00:00Z"
}

# URI would be obtained from script logs or database
script_uri = "http://opensim-server:20800/xmlrpc/12345678-1234-1234-1234-123456789abc"
response = client.send_request(script_uri, data)

if response:
    print(f"Script response: {response}")
```

### Configuration Examples

#### Basic Configuration

```ini
[Modules]
XmlRpcRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcRouterModule
```

#### With Full XMLRPC System

```ini
[Modules]
XMLRPCModule = true           ; Full XMLRPC functionality
XmlRpcRouterModule = true     ; Router for event handling

[XMLRPC]
XmlRpcPort = 20800
XmlRpcRouterModule = XmlRpcRouterModule
```

#### Development Configuration

```ini
[Modules]
XmlRpcRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcRouterModule

[Logging]
LogLevel = DEBUG              ; Enable detailed logging
```

## Integration Points

### With XMLRPCModule

- **Complementary Functionality**: Works alongside XMLRPCModule for enhanced XMLRPC support
- **Shared Configuration**: Uses same [XMLRPC] configuration section
- **Event Coordination**: Provides additional event routing for XMLRPC operations
- **Interface Compatibility**: Implements standard IXmlRpcRouter interface

### With Script Engine

- **Event Generation**: Generates xmlrpc_uri events for script notification
- **Direct Integration**: Direct event posting to script engine
- **Script Lifecycle**: Participates in script lifecycle management
- **Engine Independence**: Works with any IScriptModule implementation

### With Scene Management

- **Interface Registration**: Registers IXmlRpcRouter interface with scene
- **Per-Region Operation**: Independent operation in each region
- **Lifecycle Participation**: Proper integration with scene lifecycle
- **Resource Management**: Clean registration and unregistration

### With Configuration System

- **Nini Integration**: Uses Nini configuration system
- **Multi-Section Configuration**: Requires both [Modules] and [XMLRPC] sections
- **Conditional Loading**: Only loads when properly configured
- **Backward Compatibility**: Maintains compatibility with existing configurations

## Security Features

### Access Control

- **Configuration-Based Access**: Only loads when explicitly configured
- **Module-Level Security**: Disabled by default for security
- **Event Isolation**: Events only sent to specified scripts
- **Interface Protection**: Protected through scene interface registration

### Operational Security

- **Minimal Attack Surface**: Simple implementation with minimal functionality
- **Event Validation**: Events only generated for enabled module
- **Resource Protection**: No resource exhaustion possibilities
- **Error Isolation**: Errors don't affect other modules or scripts

### Configuration Security

- **Explicit Enablement**: Must be explicitly enabled to function
- **Configuration Validation**: Validates configuration before activation
- **Safe Defaults**: Defaults to disabled state
- **Clean Failure**: Fails safely when misconfigured

## Debugging and Troubleshooting

### Common Issues

1. **Module Not Loading**: Check both [Modules] and [XMLRPC] configuration sections
2. **No Events Generated**: Verify module is enabled and properly configured
3. **URI Events Missing**: Check script engine integration and event handling
4. **Interface Not Found**: Verify scene registration and module lifecycle

### Diagnostic Procedures

1. **Configuration Check**: Verify both required configuration sections
2. **Module Status**: Check logs for module loading and enablement
3. **Interface Registration**: Verify IXmlRpcRouter interface registration
4. **Event Flow**: Trace event generation and script delivery

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[Modules]
XmlRpcRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcRouterModule
```

### Debug Output

```bash
# Expected log messages for successful loading
[DEBUG] Loading XmlRpcRouter for XMLRPC channel routing and script integration
[INFO] XmlRpcRouter loaded for XMLRPC channel routing, script event handling, and external API integration

# Configuration issues
[DEBUG] XmlRpcRouter disabled - set XmlRpcRouterModule = true in [Modules] to enable XMLRPC routing functionality
```

## Use Cases

### Script Event Notification

- **URI Assignment**: Notify scripts when XMLRPC URIs are assigned
- **Channel Management**: Manage XMLRPC channel associations
- **Event Coordination**: Coordinate XMLRPC events across scripts
- **Status Updates**: Provide status updates to scripts

### Lightweight XMLRPC Integration

- **Minimal Overhead**: Provide XMLRPC functionality with minimal resources
- **Event-Driven Architecture**: Event-based XMLRPC integration
- **Supplemental Functionality**: Supplement full XMLRPCModule with additional features
- **Development Support**: Support development and testing scenarios

### External API Integration

- **API Gateway**: Serve as lightweight API gateway for external integration
- **Event Broadcasting**: Broadcast XMLRPC events to multiple scripts
- **Service Discovery**: Help external services discover XMLRPC endpoints
- **Integration Testing**: Support integration testing and development

### System Architecture

- **Microservice Support**: Support microservice architectures with lightweight routing
- **Event Mesh**: Participate in event mesh architectures
- **Service Composition**: Enable service composition through event routing
- **System Decoupling**: Decouple systems through event-driven integration

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the OptionalModulesFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via OptionalModulesFactory configuration
- Logging provides visibility into module loading decisions
- All XMLRPC router functionality remains fully compatible

### Configuration Migration

When upgrading from previous versions:

- Verify XmlRpcRouterModule is enabled in OptionalModulesFactory
- Test XMLRPC router functionality after deployment
- Update any scripts that depend on xmlrpc_uri events
- Validate integration with external systems

### Deployment Considerations

- **Optional Functionality**: Module is optional and disabled by default
- **Configuration Requirements**: Requires specific configuration to activate
- **Performance Impact**: Minimal performance impact when enabled
- **Compatibility**: Compatible with existing XMLRPC systems

## Configuration Examples

### Basic Configuration

```ini
[Modules]
XmlRpcRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcRouterModule
```

### Development Configuration

```ini
[Modules]
XmlRpcRouterModule = true

[XMLRPC]
XmlRpcRouterModule = XmlRpcRouterModule

[Logging]
LogLevel = DEBUG
```

### Production Configuration

```ini
[Modules]
XMLRPCModule = true           ; Full XMLRPC functionality
XmlRpcRouterModule = false    ; Disabled in production for security

[XMLRPC]
XmlRpcPort = 20800
# XmlRpcRouterModule not configured - disabled
```

### Testing Configuration

```ini
[Modules]
XMLRPCModule = true
XmlRpcRouterModule = true

[XMLRPC]
XmlRpcPort = 20800
XmlRpcRouterModule = XmlRpcRouterModule

[Logging]
LogLevel = DEBUG
```

## Best Practices

### Configuration Management

1. **Explicit Enablement**: Only enable when specifically needed
2. **Security Consideration**: Disable in production unless required
3. **Documentation**: Document configuration requirements clearly
4. **Testing**: Test configuration changes thoroughly

### Script Development

1. **Event Handling**: Implement proper xmlrpc_uri event handling
2. **Error Handling**: Handle missing events gracefully
3. **State Management**: Manage XMLRPC state properly in scripts
4. **Resource Cleanup**: Clean up XMLRPC resources when scripts reset

### System Integration

1. **Minimal Usage**: Use only when lightweight routing is needed
2. **Complement XMLRPCModule**: Use to supplement, not replace, full XMLRPC functionality
3. **Event Architecture**: Design event-driven architectures appropriately
4. **Performance Monitoring**: Monitor performance impact when enabled

## Future Enhancements

### Potential Improvements

1. **Enhanced Routing**: More sophisticated routing and filtering capabilities
2. **State Management**: Persistent state management for channels and registrations
3. **Monitoring**: Enhanced monitoring and statistics collection
4. **Security**: Additional security features and access controls

### Compatibility Considerations

1. **Interface Evolution**: Adapt to IXmlRpcRouter interface updates
2. **Script Engine Changes**: Maintain compatibility with script engine evolution
3. **Configuration Evolution**: Evolve with configuration system changes
4. **Integration Requirements**: Adapt to changing integration requirements

### Integration Opportunities

1. **Monitoring Systems**: Enhanced integration with monitoring tools
2. **Management Interfaces**: Web-based management interfaces
3. **API Gateway**: Full API gateway functionality
4. **Service Mesh**: Integration with service mesh architectures