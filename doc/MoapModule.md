# MoapModule

## Overview

The MoapModule (Media on a Prim) is a comprehensive media management system for OpenSimulator/Akisim that enables rich multimedia experiences within virtual worlds. It allows users to display web content, videos, images, and interactive media directly on object surfaces, transforming simple 3D objects into dynamic multimedia displays and interactive interfaces.

## Architecture

The MoapModule implements multiple interfaces:
- `INonSharedRegionModule` - Per-region module instance management
- `IMoapModule` - Media management interface for external access

### Key Components

1. **Media Entry Management**
   - **Per-face Media**: Independent media streams on each object face
   - **Media Properties**: Comprehensive control over media behavior and appearance
   - **URL Management**: Dynamic URL updating and navigation control
   - **Permission System**: Fine-grained access control for media manipulation

2. **Capability System**
   - **ObjectMedia Capability**: HTTP endpoint for media data exchange
   - **ObjectMediaNavigate Capability**: Real-time URL navigation control
   - **Streaming Protocol**: LLSD-based communication with viewers
   - **Version Management**: Media versioning for synchronization

3. **Security Framework**
   - **Whitelist System**: URL filtering and access control
   - **Permission Integration**: Integration with OpenSim's permission system
   - **Agent Validation**: Secure agent authentication for media operations
   - **Content Filtering**: Protection against malicious or inappropriate content

## Configuration

### Module Activation

Set in `[MediaOnAPrim]` section:
```ini
[MediaOnAPrim]
Enabled = true
```

### Configuration Options

- **Enabled**: Master switch for Media on a Prim functionality (default: false)

The module is disabled by default and must be explicitly enabled in configuration. This ensures that media functionality is only available when intentionally configured.

## Features

### Advanced Media Properties

The module supports comprehensive media configuration:

1. **Basic Properties**
   - **Home URL**: Default media URL for the face
   - **Current URL**: Currently displayed URL
   - **Auto Loop**: Automatic media looping behavior
   - **Auto Play**: Automatic media playback on load
   - **Auto Scale**: Automatic scaling to fit face dimensions

2. **Display Control**
   - **Width/Height**: Media dimensions in pixels
   - **Media Type**: Content type specification (HTML, video, image)
   - **Controls**: Viewer control visibility (play, pause, etc.)
   - **Alt Image**: Alternative image for non-media capable viewers

3. **Security Features**
   - **Enable Whitelist**: URL restriction enforcement
   - **Whitelist**: Array of allowed URL patterns
   - **Wildcard Support**: Flexible URL pattern matching
   - **Permission Checks**: Per-face permission validation

### Real-time Interaction

1. **Dynamic Navigation**: Users can navigate to different URLs in real-time
2. **Interactive Content**: Support for interactive web applications
3. **Multi-user Sync**: Synchronized media viewing across multiple users
4. **Script Integration**: LSL script control over media properties

## Technical Implementation

### Media Storage and Management

#### Media Entry Structure

Each face can have an independent MediaEntry with properties:
- **HomeURL**: Base URL for the media content
- **CurrentURL**: Currently active URL (may differ from HomeURL)
- **AutoLoop**: Whether media should loop automatically
- **AutoPlay**: Whether media should start playing automatically
- **AutoScale**: Whether media should scale to fit the face
- **Width/Height**: Media dimensions
- **EnableWhiteList**: Whether URL filtering is active
- **WhiteList**: Array of allowed URL patterns
- **Controls**: Control interface visibility
- **PermsControl**: Permission level for media control
- **PermsInteract**: Permission level for media interaction

#### Media Versioning

The module implements a sophisticated versioning system:
- **Version Format**: `x-mv:NNNNNNNNNN/agent-uuid`
- **Incremental Versioning**: Automatic version increment on changes
- **Change Tracking**: Attribution of changes to specific agents
- **Synchronization**: Ensures viewer synchronization with server state

### Capability Endpoints

#### ObjectMedia Capability

**GET Request**: Retrieve media configuration for an object
- Returns complete media configuration for all faces
- Includes version information for synchronization
- Respects permission system for access control

**POST Request**: Request specific media data
- Accepts object UUID and optional face specifications
- Returns filtered media data based on permissions
- Handles non-existent objects gracefully

**PUT Request**: Update media configuration
- Accepts media configuration updates from viewers
- Validates permissions for each face modification
- Updates media properties and triggers appropriate events
- Handles partial updates and face-specific changes

#### ObjectMediaNavigate Capability

**POST Request**: Navigate media to new URL
- Accepts navigation requests from viewers
- Validates permissions for media interaction
- Checks URL against whitelist if enabled
- Updates current URL and triggers change events

### Permission Integration

The module integrates comprehensively with OpenSim's permission system:

#### Permission Types

1. **CanControlPrimMedia**: Ability to modify media properties
2. **CanInteractWithPrimMedia**: Ability to navigate and interact with media
3. **Object Ownership**: Automatic permissions for object owners
4. **Group Permissions**: Group-based media access control

#### Permission Validation

- **Per-face Validation**: Separate permissions for each object face
- **Agent Authentication**: Verification of agent identity and session
- **Dynamic Checking**: Real-time permission validation for all operations
- **Graceful Degradation**: Appropriate handling of permission failures

### Whitelist System

The module implements a flexible URL whitelist system:

#### Pattern Matching

- **Exact Matching**: Precise URL matching for strict control
- **Wildcard Support**: `*` wildcards for flexible patterns
- **Domain Matching**: Host-based filtering with subdomain support
- **Path Matching**: URL path pattern matching

#### Pattern Examples

```ini
# Exact domain matching
example.com

# Subdomain wildcard
*.example.com

# Path pattern matching
example.com/videos/*

# Protocol-agnostic matching
//trusted-site.com/*
```

## API Methods

### Core Media Operations

- `GetMediaEntry(SceneObjectPart part, int face)` - Retrieve media entry for specific face
- `SetMediaEntry(SceneObjectPart part, int face, MediaEntry me)` - Set media entry for face
- `ClearMediaEntry(SceneObjectPart part, int face)` - Remove media from face

### Media Properties

MediaEntry supports the following key properties:

- **HomeURL**: Default URL for the media
- **CurrentURL**: Currently displayed URL
- **AutoLoop**: Automatic looping behavior
- **AutoPlay**: Automatic playback on load
- **AutoScale**: Automatic scaling to face size
- **Width/Height**: Media dimensions in pixels
- **EnableWhiteList**: URL filtering activation
- **WhiteList**: Array of allowed URL patterns
- **Controls**: Control interface visibility
- **MediaType**: MIME type specification
- **Description**: Human-readable description

### Utility Methods

- `CheckUrlAgainstWhitelist(string url, string[] whitelist)` - Validate URL against whitelist
- `UpdateMediaUrl(SceneObjectPart part, UUID updateId)` - Update media version tracking
- `CheckFaceParam(SceneObjectPart part, int face)` - Validate face parameter

## Usage Examples

### Basic Media Setup

```csharp
// Create media entry
MediaEntry media = new MediaEntry();
media.HomeURL = "https://example.com/video.mp4";
media.AutoPlay = true;
media.AutoLoop = true;
media.Width = 512;
media.Height = 288;

// Apply to object face
moapModule.SetMediaEntry(part, 0, media);
```

### Whitelist Configuration

```csharp
// Create media with whitelist
MediaEntry media = new MediaEntry();
media.HomeURL = "https://trusted-site.com/content/";
media.EnableWhiteList = true;
media.WhiteList = new string[] {
    "trusted-site.com/*",
    "*.approved-domain.com",
    "specific-page.net/safe-content"
};

moapModule.SetMediaEntry(part, 0, media);
```

### Dynamic URL Navigation

```csharp
// Get current media entry
MediaEntry media = moapModule.GetMediaEntry(part, 0);

// Update current URL (if whitelist allows)
if (media != null)
{
    media.CurrentURL = "https://trusted-site.com/new-content";
    moapModule.SetMediaEntry(part, 0, media);
}
```

## Performance Characteristics

### Memory Management

- **Per-face Storage**: Efficient storage of media data per object face
- **Lazy Loading**: Media entries created only when needed
- **Automatic Cleanup**: Removal of empty media entries
- **Copy-on-Write**: Efficient copying during object duplication

### Network Optimization

- **Capability Caching**: Efficient capability endpoint management
- **Delta Updates**: Only changed media properties are transmitted
- **Version Synchronization**: Prevents unnecessary data transfers
- **Compression**: LLSD compression for large media configurations

### Viewer Integration

- **Protocol Compliance**: Full compatibility with Second Life media protocols
- **Feature Detection**: Graceful handling of viewer capability differences
- **Performance Optimization**: Efficient media data serialization
- **Error Handling**: Robust error handling for network issues

## Security Features

### Access Control

- **Permission Integration**: Complete integration with OpenSim permissions
- **Agent Validation**: Secure agent authentication for all operations
- **Session Verification**: Validation of agent sessions and capabilities
- **Multi-level Security**: Object, face, and operation-level security

### Content Protection

- **URL Validation**: Comprehensive URL validation and sanitization
- **Whitelist Enforcement**: Strict enforcement of URL whitelists
- **Pattern Matching**: Flexible but secure pattern matching
- **Malicious Content Protection**: Protection against harmful URLs

### Privacy Features

- **User Consent**: Respect for user media preferences
- **Opt-out Support**: Support for users who disable media
- **Data Minimization**: Minimal data collection and storage
- **Audit Logging**: Comprehensive logging of media operations

## Integration Points

### With Object System

- **Shape Integration**: Seamless integration with object shape data
- **Texture System**: Coordination with texture entry management
- **Copy Handling**: Proper media copying during object duplication
- **Serialization**: Media data persistence in object serialization

### With Permission System

- **Dynamic Permissions**: Real-time permission checking
- **Granular Control**: Per-face permission granularity
- **Owner Rights**: Automatic permissions for object owners
- **Group Integration**: Support for group-based permissions

### With Scripting System

- **LSL Integration**: Script access to media properties
- **Event Generation**: Script events for media changes
- **Function Exposure**: LSL functions for media manipulation
- **State Synchronization**: Coordination between scripts and viewers

## Debugging and Troubleshooting

### Common Issues

1. **Media Not Loading**: Check URL accessibility and whitelist configuration
2. **Permission Denied**: Verify user permissions and object ownership
3. **Navigation Blocked**: Check whitelist patterns and URL validation
4. **Performance Issues**: Monitor media complexity and viewer capabilities

### Diagnostic Tools

1. **Debug Logging**: Comprehensive debug output for troubleshooting
2. **Permission Analysis**: Tools for analyzing permission configurations
3. **Whitelist Testing**: Validation tools for whitelist patterns
4. **Version Tracking**: Monitoring of media version changes

### Debug Configuration

Enable detailed logging for troubleshooting:

```ini
[Logging]
LogLevel = DEBUG

[MediaOnAPrim]
Enabled = true
```

## Use Cases

### Educational Applications

- **Interactive Lessons**: Web-based educational content on classroom objects
- **Virtual Museums**: Rich multimedia exhibits and interactive displays
- **Training Simulations**: Instructional videos and interactive tutorials
- **Research Presentations**: Dynamic data visualization and presentation tools

### Business Applications

- **Digital Signage**: Dynamic advertising and information displays
- **Product Demos**: Interactive product demonstrations and catalogs
- **Meeting Rooms**: Shared presentations and collaborative displays
- **Customer Service**: Interactive help systems and support interfaces

### Entertainment

- **Media Walls**: Large-scale video displays and entertainment systems
- **Interactive Games**: Web-based games embedded in virtual objects
- **Social Media**: Live social media feeds and interactive content
- **Virtual Theaters**: Movie screenings and live performance streaming

### Creative Applications

- **Digital Art**: Interactive art installations and multimedia experiences
- **Portfolio Displays**: Dynamic artist portfolios and gallery exhibitions
- **Music Systems**: Streaming music and interactive audio players
- **Virtual Galleries**: Immersive art and media gallery experiences

## Migration and Deployment

### From Mono.Addins

The module has been migrated from Mono.Addins to the CoreModuleFactory system:

- Remove any `[Extension]` attributes in configuration
- Module loading is now controlled via configuration
- Logging provides visibility into module loading decisions

### Configuration Migration

When upgrading from previous versions:

- Verify `[MediaOnAPrim]` configuration section exists
- Test media functionality after deployment
- Update any custom media configurations
- Validate whitelist patterns and permissions

### Deployment Considerations

- **Viewer Compatibility**: Ensure target viewers support media on a prim
- **Network Security**: Configure appropriate firewall rules for media URLs
- **Content Policies**: Establish media content policies and guidelines
- **Performance Planning**: Plan for media bandwidth and processing requirements

## Configuration Examples

### Basic Media Configuration

```ini
[MediaOnAPrim]
Enabled = true
```

### Production Configuration

```ini
[MediaOnAPrim]
Enabled = true

[Logging]
LogLevel = INFO
```

### Development Configuration

```ini
[MediaOnAPrim]
Enabled = true

[Logging]
LogLevel = DEBUG
```

## Best Practices

### Security Guidelines

1. **Whitelist Management**: Maintain strict whitelists for media URLs
2. **Permission Auditing**: Regular auditing of media permissions
3. **Content Monitoring**: Monitor media content for appropriateness
4. **Update Management**: Keep media URLs and content current

### Performance Optimization

1. **Media Sizing**: Use appropriate media dimensions for performance
2. **Content Optimization**: Optimize media content for virtual world viewing
3. **Bandwidth Management**: Monitor and manage media bandwidth usage
4. **Viewer Compatibility**: Test media with different viewer types

### Operational Practices

1. **Monitoring**: Monitor media usage and performance metrics
2. **Backup**: Regular backup of media configurations
3. **Documentation**: Document media policies and procedures
4. **Training**: Provide user training for media functionality

## Future Enhancements

### Potential Improvements

1. **Enhanced Formats**: Support for additional media formats and protocols
2. **Performance Optimization**: Improved caching and streaming performance
3. **Security Enhancements**: Advanced security features and validation
4. **Integration Features**: Enhanced integration with other OpenSim systems

### Compatibility Considerations

1. **Protocol Evolution**: Stay current with media protocol developments
2. **Viewer Updates**: Adapt to new viewer media capabilities
3. **Web Standards**: Support for evolving web media standards
4. **Security Standards**: Implementation of new security best practices