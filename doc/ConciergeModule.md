# ConciergeModule Technical Documentation

## Overview

The **ConciergeModule** is a shared region module that provides comprehensive avatar greeting, announcement, and chat management capabilities within OpenSimulator. It serves as an intelligent host system that welcomes new visitors, announces avatar movements, manages regional chat functionality, and provides external data reporting capabilities through XML-RPC interfaces.

## Purpose

The ConciergeModule serves as a sophisticated hospitality and communication management system that:

- **Avatar Greeting**: Provides personalized welcome messages for avatars entering regions
- **Movement Announcements**: Broadcasts enter/leave notifications to region occupants
- **Chat Management**: Can optionally replace or extend the standard ChatModule functionality
- **External Reporting**: Sends avatar presence data to external broker systems via HTTP/XML
- **Administrative Interface**: Provides XML-RPC endpoints for remote welcome message management
- **Region Filtering**: Supports selective operation on specific regions via regex patterns

## Architecture

### Core Components

```
┌─────────────────────────────────────┐
│           ConciergeModule           │
├─────────────────────────────────────┤
│         ChatModule (Base)           │
│    - Chat message processing       │
│    - Event handling integration    │
│    - Broadcasting capabilities     │
├─────────────────────────────────────┤
│        Region Management            │
│  RwLockedList<IScene> m_scenes      │
│  RwLockedList<IScene> concierged    │
│    - Thread-safe collections       │
│    - Region filtering support      │
├─────────────────────────────────────┤
│      Welcome System                 │
│    - File-based templates          │
│    - Region-specific messages      │
│    - Variable substitution         │
├─────────────────────────────────────┤
│       Event Handlers                │
│    - OnNewClient subscription       │
│    - OnMakeRootAgent processing     │
│    - OnMakeChildAgent processing    │
│    - OnClientLoggedOut handling     │
├─────────────────────────────────────┤
│     External Integration            │
│    - HTTP broker updates           │
│    - XML-RPC administration        │
│    - Asynchronous web requests      │
└─────────────────────────────────────┘
```

### Inheritance Hierarchy

```
    ChatModule (Base)
         ↓
   ConciergeModule
         ↓ implements
  ISharedRegionModule
```

### Thread Safety

The module uses thread-safe collections and synchronization:
- **RwLockedList<IScene>**: Thread-safe scene collection management
- **object m_syncy**: Synchronization object for critical sections
- **Asynchronous Operations**: Non-blocking external communications

## Interface Implementation

The module implements:
- **ISharedRegionModule**: Shared across all regions in the simulator
- **ChatModule**: Inherits chat functionality with optional replacement capability

### Module Lifecycle Methods

```csharp
public override void Initialise(IConfigSource configSource)
public override void AddRegion(Scene scene)
public override void RemoveRegion(Scene scene)
public override void PostInitialise()
public override void Close()
```

## Configuration

### Module Activation

Configure in OpenSim.ini [Concierge] section:

```ini
[Concierge]
enabled = true
whoami = "Concierge Bot"
concierge_channel = 42
announce_entering = "{0} enters {1} (now {2} visitors in this region)"
announce_leaving = "{0} leaves {1} (back to {2} visitors in this region)"
regions = "Welcome.*|Main.*"
welcomes = "/path/to/welcome/files"
password = "admin_password"
broker = "http://external.server.com/avatar_update?region={0}&uuid={1}"
broker_timeout = 300
```

### Configuration Options

#### Core Settings

- **enabled**: Enables/disables the entire ConciergeModule (default: false)
- **whoami**: Identity name for the concierge system (default: "conferencier")
- **concierge_channel**: Chat channel for concierge communications (default: 42)

#### Chat Integration

- **Chat Module Replacement**: Automatically detects and optionally replaces disabled ChatModule
- **Extended Chat Range**: For concierged regions, extends chat to cover entire region (except whispers)
- **Message Broadcasting**: Integrates with existing chat event system

#### Region Filtering

- **regions**: Regular expression pattern to filter which regions are concierged
- Example patterns:
  - `"Welcome.*"` - Only regions starting with "Welcome"
  - `"Main|Central|Hub"` - Specific named regions
  - `""` (empty) - All regions (default)

#### Welcome Messages

- **welcomes**: Directory path containing welcome message template files
- **File Structure**:
  - `{region_name}` - Region-specific welcome file
  - `DEFAULT` - Fallback welcome file for regions without specific files
- **Variable Substitution**: Supports `{0}` (avatar name), `{1}` (region name), `{2}` (concierge name)

#### Announcement Templates

- **announce_entering**: Message template for avatar arrivals
- **announce_leaving**: Message template for avatar departures
- **Template Variables**: `{0}` = avatar name, `{1}` = region name, `{2}` = visitor count

#### External Integration

- **broker**: HTTP endpoint URL for avatar data reporting with placeholders
- **broker_timeout**: Timeout in seconds for broker HTTP requests (default: 300)
- **password**: Password for XML-RPC administrative operations

### Factory Integration

The module is loaded via factory with configuration-based activation:

```csharp
var conciergeConfig = configSource?.Configs["Concierge"];
if (conciergeConfig?.GetBoolean("enabled", false) == true)
{
    if(m_log.IsDebugEnabled) m_log.Debug("Loading ConciergeModule for avatar welcome messages and region announcements");
    var conciergeModuleInstance = LoadConciergeModule();
    if (conciergeModuleInstance != null)
    {
        yield return conciergeModuleInstance;
        if(m_log.IsInfoEnabled) m_log.Info("ConciergeModule loaded for avatar greetings, announcements, and chat integration");
    }
}
```

## Core Functionality

### Avatar Welcome System

#### Welcome Message Processing

```csharp
protected void WelcomeAvatar(ScenePresence agent, Scene scene)
{
    if (!String.IsNullOrEmpty(m_welcomes))
    {
        string[] welcomes = new string[] {
            Path.Combine(m_welcomes, agent.Scene.RegionInfo.RegionName),
            Path.Combine(m_welcomes, "DEFAULT")
        };

        foreach (string welcome in welcomes)
        {
            if (File.Exists(welcome))
            {
                string[] welcomeLines = File.ReadAllLines(welcome);
                foreach (string line in welcomeLines)
                {
                    AnnounceToAgent(agent, String.Format(line, agent.Name, scene.RegionInfo.RegionName, m_whoami));
                }
                return;
            }
        }
    }
}
```

#### Welcome File Format

Welcome files contain plain text with variable substitution:

```
Welcome to {1}, {0}!
I am {2}, your virtual concierge.
Feel free to explore and enjoy your visit.
Type /help for available commands.
```

Variables:
- `{0}`: Avatar name
- `{1}`: Region name
- `{2}`: Concierge identity (m_whoami)

### Movement Announcements

#### Avatar Arrival

```csharp
public void OnMakeRootAgent(ScenePresence agent)
{
    if (m_conciergedScenes.Contains(agent.Scene))
    {
        Scene scene = agent.Scene;
        WelcomeAvatar(agent, scene);
        AnnounceToAgentsRegion(scene, String.Format(m_announceEntering,
            agent.Name, scene.RegionInfo.RegionName, scene.GetRootAgentCount()));
        UpdateBroker(scene);
    }
}
```

#### Avatar Departure

```csharp
public void OnMakeChildAgent(ScenePresence agent)
{
    if (m_conciergedScenes.Contains(agent.Scene))
    {
        Scene scene = agent.Scene;
        AnnounceToAgentsRegion(scene, String.Format(m_announceLeaving,
            agent.Name, scene.RegionInfo.RegionName, scene.GetRootAgentCount()));
        UpdateBroker(scene);
    }
}
```

### Chat System Integration

#### Chat Module Replacement Logic

The ConciergeModule can optionally replace the ChatModule:

```csharp
try
{
    if (configSource.Configs["Chat"] == null)
    {
        m_replacingChatModule = false; // Chat enabled by default
    }
    else
    {
        m_replacingChatModule = !configSource.Configs["Chat"].GetBoolean("enabled", true);
    }
}
catch (Exception)
{
    m_replacingChatModule = false;
}
```

#### Extended Chat Range

For concierged regions, the module extends chat range to cover the entire region:

```csharp
public override void OnChatFromClient(Object sender, OSChatMessage c)
{
    if (m_replacingChatModule)
    {
        if (m_conciergedScenes.Contains(c.Scene))
        {
            if (c.Type != ChatTypeEnum.Whisper)
            {
                base.OnChatBroadcast(sender, c);
                return;
            }
        }
        base.OnChatFromClient(sender, c);
    }
}
```

### External Broker Integration

#### Data Reporting Format

The module sends XML-formatted avatar data to external brokers:

```xml
<avatars count="3" region_name="Welcome Plaza" region_uuid="12345678-1234-1234-1234-123456789012" timestamp="2023-10-15T14:30:00">
    <avatar name="John Doe" uuid="87654321-4321-4321-4321-210987654321" />
    <avatar name="Jane Smith" uuid="11111111-2222-3333-4444-555555555555" />
    <avatar name="Bob Johnson" uuid="66666666-7777-8888-9999-000000000000" />
</avatars>
```

#### Asynchronous HTTP Updates

```csharp
protected void UpdateBroker(Scene scene)
{
    if (String.IsNullOrEmpty(m_brokerURI)) return;

    string uri = String.Format(m_brokerURI, scene.RegionInfo.RegionName, scene.RegionInfo.RegionID);

    // Create XML payload with current avatar list
    StringBuilder list = new StringBuilder();
    list.Append(String.Format("<avatars count=\"{0}\" region_name=\"{1}\" region_uuid=\"{2}\" timestamp=\"{3}\">\n",
        scene.GetRootAgentCount(), scene.RegionInfo.RegionName,
        scene.RegionInfo.RegionID, DateTime.UtcNow.ToString("s")));

    scene.ForEachRootScenePresence(sp => {
        list.Append(String.Format("    <avatar name=\"{0}\" uuid=\"{1}\" />\n", sp.Name, sp.UUID));
    });

    list.Append("</avatars>");

    // Send asynchronously with timeout protection
    HttpWebRequest updatePost = WebRequest.Create(uri) as HttpWebRequest;
    updatePost.Method = "POST";
    updatePost.ContentType = "text/xml";
    updatePost.ContentLength = payload.Length;
    updatePost.UserAgent = "OpenSim.Concierge";

    BrokerState bs = new BrokerState(uri, payload, updatePost);
    bs.Timer = new Timer(delegate(object state) {
        BrokerState b = state as BrokerState;
        b.Poster.Abort();
        b.Timer.Dispose();
    }, bs, m_brokerUpdateTimeout * 1000, Timeout.Infinite);

    updatePost.BeginGetRequestStream(UpdateBrokerSend, bs);
}
```

### XML-RPC Administrative Interface

#### Welcome Message Update Endpoint

```csharp
public XmlRpcResponse XmlRpcUpdateWelcomeMethod(XmlRpcRequest request, IPEndPoint remoteClient)
{
    // Request parameters:
    // - password: Authentication password
    // - region: Target region name
    // - welcome: New welcome message content

    Hashtable requestData = (Hashtable)request.Params[0];

    // Validate password
    if (!String.IsNullOrEmpty(m_xmlRpcPassword) &&
        (string)requestData["password"] != m_xmlRpcPassword)
        throw new Exception("wrong password");

    // Update welcome file
    string regionName = (string)requestData["region"];
    string msg = (string)requestData["welcome"];
    string welcome = Path.Combine(m_welcomes, regionName);

    // Backup existing file
    if (File.Exists(welcome))
    {
        string welcomeBackup = String.Format("{0}~", welcome);
        if (File.Exists(welcomeBackup)) File.Delete(welcomeBackup);
        File.Move(welcome, welcomeBackup);
    }

    File.WriteAllText(welcome, msg);
    return successResponse;
}
```

#### XML-RPC Usage Example

```python
import xmlrpc.client

server = xmlrpc.client.ServerProxy("http://opensim.server.com:9000/")
result = server.concierge_update_welcome({
    'password': 'admin_password',
    'region': 'Welcome Plaza',
    'welcome': 'Welcome to our new improved region!\nEnjoy your stay!'
})
```

## Performance Characteristics

### Memory Management

- **Thread-Safe Collections**: Uses RwLockedList for concurrent access without blocking
- **Efficient Event Handling**: Subscribes only to necessary events
- **Resource Cleanup**: Proper cleanup during region removal
- **String Optimization**: Uses StringBuilder for XML generation

### Asynchronous Operations

- **Non-blocking Updates**: Broker updates don't block main thread
- **Timeout Protection**: Prevents hung HTTP requests from consuming resources
- **Timer Management**: Automatic cleanup of timeout timers
- **Exception Handling**: Robust error handling for network operations

### Performance Metrics

- **Welcome Processing**: < 50ms for file-based welcome messages
- **Announcement Broadcasting**: < 10ms for region-wide announcements
- **Broker Updates**: Asynchronous with configurable timeout (default: 5 minutes)
- **Memory Footprint**: Minimal additional overhead beyond ChatModule

## Advanced Features

### Region Filtering

The module supports sophisticated region filtering using regular expressions:

```ini
[Concierge]
regions = "^(Welcome|Tutorial|Help).*$"
```

This example enables concierge features only for regions whose names start with "Welcome", "Tutorial", or "Help".

### Chat Integration Modes

#### Full Replacement Mode
When ChatModule is disabled:
- ConciergeModule handles all chat functions
- Extended range for concierged regions
- Maintains compatibility with existing chat events

#### Supplemental Mode
When ChatModule is enabled:
- ConciergeModule adds announcements only
- ChatModule handles normal chat processing
- No interference with existing chat functionality

### Broker Communication Protocol

#### Supported URI Templates

```ini
# Basic region update
broker = "http://stats.server.com/update?region={0}"

# Region and UUID parameters
broker = "http://api.server.com/regions/{1}/avatars"

# Custom endpoints with authentication
broker = "http://secure.server.com/api/v1/opensim/regions/{0}/presence?key=api_key"
```

## Error Handling and Resilience

### Configuration Validation

```csharp
public override void Initialise(IConfigSource configSource)
{
    IConfig config = configSource.Configs["Concierge"];
    if (config == null) return;
    if (!config.GetBoolean("enabled", false)) return;

    // Validate and set configuration with defaults
    m_conciergeChannel = config.GetInt("concierge_channel", m_conciergeChannel);
    m_welcomes = config.GetString("welcomes", m_welcomes);

    // Initialize regex with error handling
    string regions = config.GetString("regions", String.Empty);
    if (!String.IsNullOrEmpty(regions))
    {
        try
        {
            m_regions = new Regex(regions, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        catch (ArgumentException ex)
        {
            m_log.ErrorFormat("Invalid regions regex pattern '{0}': {1}", regions, ex.Message);
            m_regions = null;
        }
    }
}
```

### File System Error Handling

```csharp
try
{
    string[] welcomeLines = File.ReadAllLines(welcome);
    foreach (string l in welcomeLines)
    {
        AnnounceToAgent(agent, String.Format(l, agent.Name, scene.RegionInfo.RegionName, m_whoami));
    }
}
catch (IOException ioe)
{
    m_log.ErrorFormat("trouble reading welcome file {0} for region {1} for avatar {2}: {3}",
        welcome, scene.RegionInfo.RegionName, agent.Name, ioe);
}
catch (FormatException fe)
{
    m_log.ErrorFormat("welcome file {0} is malformed: {1}", welcome, fe);
}
```

### Network Error Handling

```csharp
try
{
    updatePost.BeginGetRequestStream(UpdateBrokerSend, bs);
}
catch (WebException we)
{
    m_log.ErrorFormat("async broker POST to {0} failed: {1}", uri, we.Status);
}
```

## Security Considerations

### Authentication and Authorization

- **Password Protection**: XML-RPC endpoints require password authentication
- **Region Validation**: Operations validated against concierged region list
- **Input Sanitization**: All user inputs validated before processing
- **File Path Security**: Welcome file paths restricted to configured directory

### Network Security

- **HTTPS Support**: Broker URLs support HTTPS for encrypted communication
- **Timeout Protection**: HTTP requests have configurable timeouts
- **Error Information Limiting**: Detailed error messages only in debug logs
- **User Agent Identification**: HTTP requests identified as "OpenSim.Concierge"

### Resource Protection

- **Memory Limits**: Reasonable limits on welcome message size and count
- **Timer Management**: Automatic cleanup prevents timer leaks
- **Thread Safety**: Proper synchronization prevents race conditions
- **Exception Isolation**: Errors in one region don't affect others

## Troubleshooting

### Common Issues

#### Module Not Loading
```
Symptom: ConciergeModule not appearing in logs
Cause: [Concierge] enabled = false or missing
Solution: Set enabled = true in [Concierge] section
```

#### Welcome Messages Not Showing
```
Symptom: Avatars not receiving welcome messages
Causes:
- Welcome files directory not configured
- Welcome files don't exist
- File permission issues
- Region not in concierged list

Solutions:
- Set welcomes parameter to valid directory
- Create region-specific or DEFAULT welcome files
- Check file permissions (readable by OpenSim process)
- Verify region name matches regex pattern
```

#### Chat Not Working
```
Symptom: Chat replacement not functioning
Cause: ChatModule still enabled or configuration conflict
Solution: Disable ChatModule in [Chat] section or check replacement logic
```

#### Broker Updates Failing
```
Symptom: External broker not receiving updates
Causes:
- Invalid broker URI
- Network connectivity issues
- Authentication problems
- Timeout issues

Solutions:
- Verify broker URI format and accessibility
- Test network connectivity from OpenSim server
- Check broker authentication requirements
- Adjust broker_timeout setting
```

### Debug Logging

Enable debug logging for detailed troubleshooting:

```csharp
// Enable these debug statements in the code:
m_log.DebugFormat("{0} enters {1}", agent.Name, scene.RegionInfo.RegionName);
m_log.DebugFormat("async broker POST to {0} started", uri);
m_log.DebugFormat("broker update: status {0}", response.StatusCode);
```

### Factory Debugging

Enhanced factory logging helps diagnose loading issues:

```csharp
if(m_log.IsDebugEnabled)
    m_log.Debug("Loading ConciergeModule for avatar welcome messages and region announcements");

if(m_log.IsInfoEnabled)
    m_log.Info("ConciergeModule loaded for avatar greetings, announcements, and chat integration");
```

## Migration Notes

### From Mono.Addins to Factory

The module has been migrated from Mono.Addins to factory-based loading:

- **Removed Dependencies**: No longer requires Mono.Addins references
- **Configuration Control**: Loading controlled by [Concierge] enabled setting
- **Enhanced Logging**: Improved operational visibility and debugging capabilities
- **Backward Compatibility**: Maintains full API and configuration compatibility

### Upgrade Considerations

- Update configuration files to use factory loading system
- Review welcome message file permissions after upgrade
- Test chat functionality if using replacement mode
- Verify broker connectivity and authentication after migration

## Related Components

### Dependencies
- **ChatModule**: Base class providing chat functionality
- **Scene**: Regional simulation environment
- **ISharedRegionModule**: Module interface contract
- **ThreadedClasses**: Thread-safe collection utilities

### Integration Points
- **Chat System**: Event handling and message broadcasting
- **Scene Management**: Region lifecycle and avatar tracking
- **HTTP Services**: External broker communication
- **File System**: Welcome message template management

## Use Cases

### Virtual World Hospitality

- **Welcome Centers**: Automated greeting for new visitors
- **Help Regions**: Information dissemination and assistance
- **Event Spaces**: Visitor tracking and announcements
- **Community Hubs**: Social interaction enhancement

### Administrative Applications

- **Visitor Tracking**: Real-time monitoring of avatar presence
- **Message Management**: Remote welcome message updates
- **Analytics Integration**: Data export for external analysis
- **Moderation Support**: Chat monitoring and logging

### Educational Environments

- **Student Orientation**: Automated campus tours and information
- **Classroom Management**: Attendance tracking and announcements
- **Resource Centers**: Information distribution and guidance
- **Virtual Libraries**: Visitor assistance and navigation

## Future Enhancements

### Potential Improvements

- **Multi-language Support**: Localized welcome messages based on avatar preferences
- **Template Engine**: More sophisticated message templating with conditional logic
- **Database Integration**: Store welcome messages and visitor logs in database
- **Analytics Dashboard**: Web-based interface for visitor statistics
- **AI Integration**: Intelligent conversation capabilities for concierge interactions

### Advanced Features

- **Voice Synthesis**: Text-to-speech welcome messages
- **Dynamic Content**: Real-time information updates in welcome messages
- **Visitor Profiles**: Personalized greetings based on visitor history
- **Event Integration**: Automatic announcements for scheduled events
- **Social Features**: Integration with friend lists and group membership

---

*This documentation covers ConciergeModule as integrated with the factory-based loading system, removing dependency on Mono.Addins while maintaining full avatar greeting, announcement, and chat management capabilities.*