# ServerReleaseNotesModule

## Overview

The **ServerReleaseNotesModule** is a shared region module that provides client capability support for displaying server release notes to users. It implements the "ServerReleaseNotes" capability that allows viewer clients to access release notes and documentation about the OpenSim server instance. The module works by redirecting capability requests to a configured external URL containing the release notes.

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`
- **Namespace**: `OpenSim.Region.ClientStack.LindenCaps`
- **Location**: `src/OpenSim.Region.ClientStack.LindenCaps/ServerReleaseNotesModule.cs`

### Dependencies
- **Capabilities Framework**: OpenSim Capabilities (Caps) system for client-server communication
- **HTTP Framework**: HTTP server infrastructure for handling capability requests
- **Configuration System**: Nini configuration framework for module settings

## Functionality

### Core Features

#### 1. Capability Registration
- **ServerReleaseNotes Capability**: Registers the "ServerReleaseNotes" capability with the viewer
- **Per-Agent Registration**: Creates unique capability endpoints for each connecting agent
- **Automatic Cleanup**: Properly unregisters capabilities when agents disconnect

#### 2. HTTP Redirect Functionality
- **External URL Redirection**: Redirects capability requests to configured external documentation
- **Client Integration**: Seamlessly integrates with viewer's "Help" → "Server Release Notes" menu
- **URL Validation**: Validates configured URLs to ensure they are well-formed absolute URIs

#### 3. Configuration-Driven Operation
- **Conditional Activation**: Only activates when properly configured
- **Flexible URL Configuration**: Supports any external URL for release notes hosting
- **Capability Control**: Can be enabled/disabled via capability configuration

### Client Interaction Flow

1. **Client Connection**: Viewer connects to region and requests capabilities
2. **Capability Registration**: Module registers ServerReleaseNotes capability if enabled
3. **User Action**: User selects "Help" → "Server Release Notes" in viewer
4. **HTTP Request**: Viewer makes HTTP GET request to capability endpoint
5. **Redirect Response**: Module sends HTTP redirect to configured release notes URL
6. **External Access**: Viewer opens external URL with release notes content

## Configuration

### Required Configuration Sections

#### Section: [ClientStack.LindenCaps]
```ini
[ClientStack.LindenCaps]
    ; Enable ServerReleaseNotes capability
    ; Must be set to "localhost" to enable the module
    ; Default: empty (disabled)
    Cap_ServerReleaseNotes = localhost
```

#### Section: [ServerReleaseNotes]
```ini
[ServerReleaseNotes]
    ; URL to redirect users to for server release notes
    ; Must be a valid absolute URL (http:// or https://)
    ; Default: empty (module disabled)
    ServerReleaseNotesURL = https://example.com/opensim-release-notes.html
```

### Factory Integration
The module is loaded through the `CoreModuleFactory` with the following behavior:
- **Capability-Based Loading**: Loaded as part of the Linden Caps modules collection
- **Configuration-Driven**: Only activates when both configuration sections are properly set
- **Automatic Instantiation**: Created via reflection with other LindenCaps modules

## Implementation Details

### Initialization Process
1. **Configuration Validation**: Checks for required configuration sections
2. **Capability Check**: Verifies `Cap_ServerReleaseNotes = localhost` is set
3. **URL Validation**: Validates that ServerReleaseNotesURL is a well-formed absolute URI
4. **Module Activation**: Enables module only if all configuration requirements are met

### Capability Registration
1. **Event Subscription**: Subscribes to Scene.EventManager.OnRegisterCaps events
2. **Per-Agent Registration**: Registers unique capability endpoint for each agent
3. **Random Path Generation**: Creates unique capability paths using UUID.Random()
4. **Handler Registration**: Registers ProcessServerReleaseNotes as the capability handler

### HTTP Request Processing
1. **Request Reception**: Receives HTTP GET requests to capability endpoints
2. **Redirect Generation**: Creates HTTP 302 redirect response to configured URL
3. **Response Completion**: Sends redirect response back to viewer client
4. **Resource Cleanup**: Properly handles request/response lifecycle

### Module Lifecycle
1. **AddRegion**: Subscribes to capability registration events for each region
2. **RemoveRegion**: Unsubscribes from events and cleans up region-specific resources
3. **Close**: Performs final cleanup when module is shut down

## Usage Examples

### Basic Configuration
```ini
[ClientStack.LindenCaps]
Cap_ServerReleaseNotes = localhost

[ServerReleaseNotes]
ServerReleaseNotesURL = https://wiki.example.com/opensim-release-notes
```

### GitHub Integration
```ini
[ClientStack.LindenCaps]
Cap_ServerReleaseNotes = localhost

[ServerReleaseNotes]
ServerReleaseNotesURL = https://github.com/yourgrid/opensim-fork/releases
```

### Local Documentation Server
```ini
[ClientStack.LindenCaps]
Cap_ServerReleaseNotes = localhost

[ServerReleaseNotes]
ServerReleaseNotesURL = http://docs.local/opensim/release-notes.html
```

### Release Notes Website Examples

#### Simple HTML Page
```html
<!DOCTYPE html>
<html>
<head>
    <title>OpenSim Release Notes</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .version { background: #f0f0f0; padding: 10px; margin: 20px 0; }
        .date { color: #666; font-style: italic; }
    </style>
</head>
<body>
    <h1>OpenSim Server Release Notes</h1>

    <div class="version">
        <h2>Version 0.9.3.0</h2>
        <p class="date">Released: 2024-01-15</p>
        <ul>
            <li>Improved physics engine performance</li>
            <li>Fixed avatar appearance issues</li>
            <li>Enhanced mesh support</li>
            <li>Security updates and bug fixes</li>
        </ul>
    </div>

    <div class="version">
        <h2>Version 0.9.2.0</h2>
        <p class="date">Released: 2023-12-01</p>
        <ul>
            <li>New LSL functions added</li>
            <li>Improved script compilation</li>
            <li>Better HyperGrid support</li>
        </ul>
    </div>
</body>
</html>
```

#### Dynamic PHP-Based Notes
```php
<?php
// Dynamic release notes with version detection
$versions = [
    '0.9.3.0' => [
        'date' => '2024-01-15',
        'features' => [
            'Improved physics engine performance',
            'Fixed avatar appearance issues',
            'Enhanced mesh support',
            'Security updates and bug fixes'
        ]
    ],
    '0.9.2.0' => [
        'date' => '2023-12-01',
        'features' => [
            'New LSL functions added',
            'Improved script compilation',
            'Better HyperGrid support'
        ]
    ]
];
?>
<!DOCTYPE html>
<html>
<head>
    <title>OpenSim Release Notes</title>
    <link rel="stylesheet" href="style.css">
</head>
<body>
    <h1>OpenSim Server Release Notes</h1>

    <?php foreach ($versions as $version => $info): ?>
    <div class="version">
        <h2>Version <?= htmlspecialchars($version) ?></h2>
        <p class="date">Released: <?= htmlspecialchars($info['date']) ?></p>
        <ul>
            <?php foreach ($info['features'] as $feature): ?>
            <li><?= htmlspecialchars($feature) ?></li>
            <?php endforeach; ?>
        </ul>
    </div>
    <?php endforeach; ?>
</body>
</html>
```

### Client-Side Access
Users can access release notes through the viewer:
1. **Menu Access**: Help → Server Release Notes
2. **Automatic Opening**: Viewer opens external URL in default browser
3. **Fallback Handling**: If capability not available, menu option is disabled

## Performance Considerations

### Memory Usage
- **Minimal Footprint**: Very low memory usage per region
- **Event Handlers**: Single event handler registration per region
- **Capability Storage**: Minimal capability registration overhead
- **No Persistent Storage**: No database or file system usage

### Network Impact
- **Redirect Only**: Only sends HTTP redirect responses, no content serving
- **External Hosting**: Actual content served by external servers
- **Bandwidth Efficient**: Minimal bandwidth usage for redirects
- **No Caching**: No server-side caching required

### Capability Performance
- **Fast Registration**: Quick capability registration per agent
- **Efficient Routing**: Direct HTTP redirect without processing overhead
- **Minimal Latency**: Low latency for redirect responses
- **Scalable**: Scales well with number of concurrent users

## Troubleshooting

### Common Issues

#### 1. Capability Not Available
**Symptoms**: "Server Release Notes" option not available in viewer help menu
**Solutions**:
- Verify `Cap_ServerReleaseNotes = localhost` in [ClientStack.LindenCaps]
- Check that [ServerReleaseNotes] section exists with valid URL
- Confirm module loaded successfully in log files
- Restart simulator after configuration changes

#### 2. Invalid URL Configuration
**Symptoms**: Module initialization fails, error messages in logs
**Solutions**:
- Ensure ServerReleaseNotesURL is a complete, absolute URL
- Include protocol (http:// or https://) in URL
- Verify URL format is valid and accessible
- Test URL manually in web browser

#### 3. Redirect Not Working
**Symptoms**: Capability endpoint accessed but no redirect occurs
**Solutions**:
- Check HTTP response handling in viewer
- Verify external URL is accessible from client machines
- Monitor network connectivity between client and external server
- Check for firewall or proxy interference

#### 4. External URL Not Loading
**Symptoms**: Redirect occurs but external page doesn't load
**Solutions**:
- Verify external server is running and accessible
- Check DNS resolution for external hostname
- Ensure external server supports HTTP/HTTPS as configured
- Test direct access to external URL

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization and configuration validation
- Capability registration for each agent
- HTTP request processing and redirect operations
- URL validation and error details

### Configuration Validation
Use these steps to validate configuration:

1. **Check Configuration Syntax**:
```bash
# Verify INI file syntax
opensim-config-validator OpenSim.ini
```

2. **Test URL Accessibility**:
```bash
# Test external URL accessibility
curl -I "https://your-release-notes-url.com"
```

3. **Monitor Capability Registration**:
```bash
# Check capability registration in logs
grep "ServerReleaseNotesModule" OpenSim.log
```

## Integration Notes

### Factory Loading
- Loaded via `CoreModuleFactory.CreateSharedModules()` as part of LindenCaps modules
- Uses reflection-based instantiation for flexibility
- No hard dependencies on OptionalModules assembly

### Capabilities Framework Integration
- Uses standard OpenSim Capabilities infrastructure
- Integrates with Scene.EventManager for capability registration
- Follows standard capability naming conventions

### HTTP Server Integration
- Uses OpenSim's HTTP server infrastructure
- Integrates with IOSHttpRequest/IOSHttpResponse interfaces
- Supports standard HTTP redirect mechanisms

### Viewer Compatibility
- Compatible with Second Life viewers and OpenSim-compatible viewers
- Supports standard "Help" menu integration
- Works with viewers that implement ServerReleaseNotes capability

## Security Considerations

### URL Security
- **External URLs**: Ensure external release notes URLs use HTTPS when possible
- **URL Validation**: Module validates URLs to prevent invalid configurations
- **Content Security**: Release notes content security depends on external hosting

### Access Control
- **Public Access**: Capability provides public access to release notes
- **No Authentication**: No built-in authentication for accessing release notes
- **External Security**: Security depends on external server configuration

### Information Disclosure
- **Server Information**: Release notes may contain server version and configuration details
- **Grid Information**: Consider what information to include in public release notes
- **Operational Security**: Balance transparency with operational security

## See Also
- [Capabilities Framework](../docs/Capabilities.md) - OpenSim capabilities system
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [HTTP Server Configuration](../docs/HttpServer.md) - HTTP infrastructure setup
- [Client Integration](../docs/ClientIntegration.md) - Viewer integration patterns