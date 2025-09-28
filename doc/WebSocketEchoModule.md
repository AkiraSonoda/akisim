# WebSocketEchoModule Technical Documentation

## Overview

The WebSocketEchoModule is a shared region module that provides WebSocket echo functionality for OpenSimulator. It serves as an example implementation and testing tool for WebSocket communication, demonstrating real-time bidirectional communication between clients and the OpenSimulator server through WebSocket protocols.

## Module Classification

- **Type**: ISharedRegionModule (Example/Testing Module)
- **Namespace**: OpenSim.Region.OptionalModules.WebSocketEchoModule
- **Assembly**: OpenSim.Region.OptionalModules
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs with configuration-based loading
- **Purpose**: Example and testing module for WebSocket functionality

## Core Functionality

### Primary Purpose

The WebSocketEchoModule demonstrates WebSocket implementation in OpenSimulator by providing a simple echo service. It accepts WebSocket connections at the `/echo` endpoint and echoes back any text or binary data received from clients, serving as both a testing tool and an example for WebSocket development.

### Key Features

1. **WebSocket Echo Service**: Real-time echo of text and binary messages
2. **Connection Management**: Tracking and management of active WebSocket connections
3. **Event Handling**: Comprehensive WebSocket event processing (connect, disconnect, message, ping/pong)
4. **Ping/Pong Support**: Built-in keepalive and latency measurement functionality
5. **Binary Data Support**: Handling of both text and binary WebSocket frames
6. **Connection Cleanup**: Proper resource management and connection cleanup
7. **HTTP Upgrade Handling**: WebSocket handshake and protocol upgrade management

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for shared modules
1. Initialise(IConfigSource) - Configuration loading and feature enablement
2. PostInitialise() - WebSocket handler registration with HTTP server
3. AddRegion(Scene) - Region registration (minimal functionality)
4. RegionLoaded(Scene) - Final region setup (minimal functionality)
5. RemoveRegion(Scene) - Region cleanup (minimal functionality)
6. Close() - WebSocket handler cleanup and connection termination
```

### WebSocket Handler Architecture

The module uses a callback-based architecture for WebSocket connection management:

```csharp
// Handler registration
MainServer.Instance.AddWebSocketHandler("/echo", WebSocketHandlerCallback);

// Handler configuration callback
public void WebSocketHandlerCallback(string path, WebSocketHttpServerHandler handler)
{
    SubscribeToEvents(handler);
    handler.SetChunksize(8192);
    handler.NoDelay_TCP_Nagle = true;
    handler.HandshakeAndUpgrade();
}
```

### Connection Management

The module maintains an active connection registry:

```csharp
private HashSet<WebSocketHttpServerHandler> _activeHandlers = new HashSet<WebSocketHttpServerHandler>();
```

## Configuration System

### Module Configuration

#### Basic Configuration ([WebSocketEcho] section)
- **Existence Check**: Module is enabled if the `[WebSocketEcho]` section exists in configuration
- **No Parameters**: Module does not require specific configuration parameters

#### Module Loading ([Modules] section)
- **Automatic Loading**: Module loads when `[WebSocketEcho]` section is present

### Configuration Example

```ini
[WebSocketEcho]
# The presence of this section enables the WebSocketEchoModule
# No additional parameters are required

[Modules]
# Module loads automatically when [WebSocketEcho] section exists
```

### Minimal Configuration
```ini
[WebSocketEcho]
```

## WebSocket Event Handling

### Event Subscription

The module subscribes to comprehensive WebSocket events:

```csharp
public void SubscribeToEvents(WebSocketHttpServerHandler handler)
{
    handler.OnClose += HandlerOnOnClose;
    handler.OnText += HandlerOnOnText;
    handler.OnUpgradeCompleted += HandlerOnOnUpgradeCompleted;
    handler.OnData += HandlerOnOnData;
    handler.OnPong += HandlerOnOnPong;
}
```

### Event Handlers

#### Connection Upgrade Completion
```csharp
private void HandlerOnOnUpgradeCompleted(object sender, UpgradeCompletedEventArgs completeddata)
{
    WebSocketHttpServerHandler obj = sender as WebSocketHttpServerHandler;
    _activeHandlers.Add(obj);
}
```

**Purpose**: Tracks successfully established WebSocket connections.

#### Text Message Handling
```csharp
private void HandlerOnOnText(object sender, WebsocketTextEventArgs text)
{
    WebSocketHttpServerHandler obj = sender as WebSocketHttpServerHandler;
    obj.SendMessage(text.Data);
    m_log.Info("[WebSocketEchoModule]: We received this: " + text.Data);
}
```

**Purpose**: Echoes received text messages back to the sender with logging.

#### Binary Data Handling
```csharp
private void HandlerOnOnData(object sender, WebsocketDataEventArgs data)
{
    WebSocketHttpServerHandler obj = sender as WebSocketHttpServerHandler;
    obj.SendData(data.Data);
    m_log.Info("[WebSocketEchoModule]: We received a bunch of ugly non-printable bytes");
    obj.SendPingCheck();
}
```

**Purpose**: Echoes binary data and initiates ping check for connection validation.

#### Pong Response Handling
```csharp
private void HandlerOnOnPong(object sender, PongEventArgs pongdata)
{
    m_log.Info("[WebSocketEchoModule]: Got a pong..  ping time: " + pongdata.PingResponseMS);
}
```

**Purpose**: Logs ping response times for latency measurement.

#### Connection Closure Handling
```csharp
private void HandlerOnOnClose(object sender, CloseEventArgs closedata)
{
    WebSocketHttpServerHandler obj = sender as WebSocketHttpServerHandler;
    UnSubscribeToEvents(obj);

    lock (_activeHandlers)
        _activeHandlers.Remove(obj);
    obj.Dispose();
}
```

**Purpose**: Cleans up resources and removes connection from active registry.

## WebSocket Protocol Implementation

### Connection Establishment

1. **HTTP Upgrade Request**: Client sends WebSocket upgrade request to `/echo`
2. **Handler Callback**: `WebSocketHandlerCallback` is invoked
3. **Event Subscription**: Event handlers are registered for the connection
4. **Configuration**: Connection parameters are set (chunk size, TCP no-delay)
5. **Handshake**: WebSocket handshake and protocol upgrade is completed
6. **Registration**: Connection is added to active handlers registry

### Message Flow

#### Text Message Echo
```
Client → [TEXT MESSAGE] → WebSocketEchoModule → [ECHO TEXT] → Client
```

#### Binary Data Echo
```
Client → [BINARY DATA] → WebSocketEchoModule → [ECHO BINARY] → Client
                                            → [PING CHECK] → Client
```

### Connection Parameters

#### Chunk Size Configuration
```csharp
handler.SetChunksize(8192);
```
**Purpose**: Sets WebSocket frame chunk size to 8KB for efficient data transfer.

#### TCP Optimization
```csharp
handler.NoDelay_TCP_Nagle = true;
```
**Purpose**: Disables Nagle's algorithm for reduced latency in real-time communication.

## Resource Management

### Connection Cleanup

#### Individual Connection Cleanup
Automatic cleanup when connections close through event handling.

#### Module Shutdown Cleanup
```csharp
public void Close()
{
    if (!enabled) return;

    // Convert to array to avoid enumeration modification issues
    WebSocketHttpServerHandler[] items = new WebSocketHttpServerHandler[_activeHandlers.Count];
    _activeHandlers.CopyTo(items);

    // Close all active connections
    for (int i = 0; i < items.Length; i++)
    {
        items[i].Close(string.Empty);
        items[i].Dispose();
    }

    _activeHandlers.Clear();
    MainServer.Instance.RemoveWebSocketHandler("/echo");
}
```

**Features**:
- Array conversion prevents enumeration modification during cleanup
- Explicit connection closure and disposal
- Handler removal from HTTP server
- Complete resource cleanup

### Thread Safety

#### Connection Registry Protection
```csharp
lock (_activeHandlers)
    _activeHandlers.Remove(obj);
```

**Purpose**: Thread-safe modification of the active connections registry.

## Logging and Monitoring

### Connection Events
- WebSocket upgrade completion
- Message reception and echoing
- Ping/pong latency measurements
- Connection closure events

### Log Messages
```
[WebSocketEchoModule]: We received this: [message content]
[WebSocketEchoModule]: We received a bunch of ugly non-printable bytes
[WebSocketEchoModule]: Got a pong.. ping time: [ms]
```

### Debug Logging
Commented debug logging statements are available for development:
```csharp
// m_log.DebugFormat("[WebSocketEchoModule]: INITIALIZED MODULE");
// m_log.DebugFormat("[WebSocketEchoModule]: REGION {0} ADDED", scene.RegionInfo.RegionName);
```

## Client Integration

### WebSocket Connection

#### JavaScript Example
```javascript
const socket = new WebSocket('ws://opensim-server:9000/echo');

socket.onopen = function(event) {
    console.log('WebSocket connection established');
    socket.send('Hello OpenSimulator!');
};

socket.onmessage = function(event) {
    console.log('Echo received:', event.data);
};

socket.onclose = function(event) {
    console.log('WebSocket connection closed');
};

socket.onerror = function(error) {
    console.error('WebSocket error:', error);
};
```

#### Binary Data Example
```javascript
// Send binary data
const buffer = new ArrayBuffer(1024);
const view = new Uint8Array(buffer);
for (let i = 0; i < view.length; i++) {
    view[i] = i % 256;
}
socket.send(buffer);

// Handle binary response
socket.addEventListener('message', function(event) {
    if (event.data instanceof ArrayBuffer) {
        console.log('Binary echo received:', event.data.byteLength, 'bytes');
    }
});
```

### Testing and Validation

#### Connection Testing
1. **Establish Connection**: Connect to `ws://server:port/echo`
2. **Send Text Message**: Verify echo response matches sent data
3. **Send Binary Data**: Verify binary echo and ping response
4. **Latency Testing**: Monitor ping/pong response times
5. **Connection Cleanup**: Verify proper connection closure

#### Load Testing
Multiple concurrent connections can be established to test server capacity and connection management.

## Use Cases and Applications

### Development and Testing
- **WebSocket Implementation Testing**: Validate WebSocket server functionality
- **Real-time Communication Development**: Example for building WebSocket-based features
- **Network Latency Testing**: Ping/pong functionality for latency measurement
- **Connection Stability Testing**: Long-running connection validation

### Educational Purposes
- **WebSocket Protocol Learning**: Demonstrates WebSocket implementation patterns
- **Event-Driven Programming**: Example of event-based WebSocket handling
- **Resource Management**: Proper connection cleanup and resource disposal
- **OpenSimulator Integration**: Shows how to integrate WebSocket services

### Prototyping
- **Real-time Features**: Foundation for chat, notifications, or live updates
- **Bidirectional Communication**: Base for client-server interactive features
- **Custom Protocol Development**: Starting point for specialized WebSocket protocols

## Performance Considerations

### Connection Management
- **Efficient Registry**: HashSet for O(1) connection lookup and removal
- **Event-Driven Architecture**: Non-blocking event handling for scalability
- **Resource Cleanup**: Proper disposal prevents memory leaks

### Network Optimization
- **TCP No-Delay**: Reduced latency for real-time communication
- **Chunk Size Optimization**: 8KB chunks for efficient data transfer
- **Ping/Pong**: Built-in keepalive and connectivity validation

### Memory Management
- **Proper Disposal**: Explicit disposal of WebSocket handlers
- **Event Unsubscription**: Prevents memory leaks from event handlers
- **Collection Management**: Efficient connection tracking with minimal overhead

## Security Considerations

### Access Control
- **No Authentication**: Module provides open access to echo functionality
- **Endpoint Exposure**: `/echo` endpoint is publicly accessible when enabled
- **Data Echo**: All received data is echoed back without filtering

### Production Deployment
**Warning**: This is an example/testing module not intended for production use.

**Security Recommendations**:
- Disable in production environments
- Add authentication if WebSocket functionality is needed
- Implement input validation and filtering
- Add rate limiting and connection throttling

### Network Security
- Consider firewall rules for WebSocket port access
- Monitor connection counts and usage patterns
- Implement proper logging for security auditing

## Dependencies

### Core Framework Dependencies
- `OpenSim.Framework.Servers` - HTTP server infrastructure
- `OpenSim.Framework.Servers.HttpServer` - WebSocket handler support
- `OpenSim.Region.Framework.Interfaces` - Module interface contracts

### System Dependencies
- `System.Collections.Generic` - Collection types for connection management
- `System.Reflection` - Logging infrastructure support

### HTTP Server Dependencies
- `MainServer.Instance` - Access to main HTTP server instance
- `WebSocketHttpServerHandler` - WebSocket connection handling
- WebSocket event types and argument classes

## Integration Points

### HTTP Server Integration
- Registers WebSocket handler with main HTTP server
- Uses server's WebSocket upgrade capabilities
- Integrates with server's connection management

### Module System Integration
- Implements ISharedRegionModule for multi-region support
- Follows standard module lifecycle patterns
- Provides proper cleanup and resource management

### Logging Integration
- Uses OpenSimulator's logging infrastructure
- Provides informational logging for monitoring
- Supports debug logging for development

## Troubleshooting

### Common Configuration Issues

1. **Module Not Loading**
   - Verify `[WebSocketEcho]` section exists in configuration
   - Check that OpenSim.Region.OptionalModules.dll is available
   - Review startup logs for module loading messages

2. **WebSocket Connection Failures**
   - Verify HTTP server is running and accessible
   - Check firewall settings for WebSocket port access
   - Ensure `/echo` endpoint is registered successfully

3. **Connection Drops**
   - Monitor ping/pong response times for network issues
   - Check server logs for connection closure events
   - Verify client-side WebSocket implementation

### Common Runtime Issues

1. **Echo Not Working**
   - Verify WebSocket connection is established
   - Check that upgrade completed successfully
   - Review message event handler logs

2. **Memory Issues**
   - Monitor connection count in active handlers registry
   - Verify proper connection cleanup on close events
   - Check for event handler memory leaks

3. **Performance Problems**
   - Monitor connection count and message frequency
   - Check TCP configuration and chunk size settings
   - Review logging output for performance bottlenecks

### Debug Configuration

```ini
[WebSocketEcho]
# Enable module with minimal configuration

# Optional: Enable debug logging in OpenSim configuration
```

### Log Analysis

Monitor module operation through log messages:
```
[WebSocketEchoModule]: We received this: test message
[WebSocketEchoModule]: Got a pong.. ping time: 45
[WebSocketEchoModule]: We received a bunch of ugly non-printable bytes
```

## Limitations and Considerations

### Module Limitations
- **Example Module**: Not intended for production use
- **No Authentication**: Open access without security controls
- **Simple Echo**: Basic functionality without advanced features
- **No Persistence**: No message storage or history

### Scalability Considerations
- **Connection Limits**: Limited by server resources and configuration
- **Memory Usage**: Grows with number of active connections
- **Processing Overhead**: Event handling overhead per connection

### Development Considerations
- **Thread Safety**: Careful handling of connection registry modifications
- **Resource Management**: Proper cleanup to prevent memory leaks
- **Error Handling**: Basic error handling may need enhancement for production

## Future Enhancement Opportunities

### Feature Enhancements
- **Authentication Support**: Add user authentication for WebSocket connections
- **Message Filtering**: Input validation and content filtering capabilities
- **Connection Limits**: Configurable connection count restrictions
- **Protocol Extensions**: Support for custom WebSocket subprotocols

### Performance Improvements
- **Connection Pooling**: Efficient connection reuse mechanisms
- **Message Queuing**: Asynchronous message processing capabilities
- **Compression Support**: WebSocket compression for large messages
- **Load Balancing**: Multi-instance connection distribution

### Monitoring and Management
- **Connection Statistics**: Detailed connection and usage metrics
- **Health Endpoints**: HTTP endpoints for monitoring connection status
- **Administrative Controls**: Runtime connection management capabilities
- **Logging Enhancements**: Structured logging and metrics collection

## Conclusion

The WebSocketEchoModule serves as an excellent example and testing tool for WebSocket functionality in OpenSimulator. Its comprehensive event handling, proper resource management, and clear implementation patterns make it valuable for both learning WebSocket development and testing real-time communication features. While designed as an example module, it provides a solid foundation for developing production WebSocket services within the OpenSimulator platform.