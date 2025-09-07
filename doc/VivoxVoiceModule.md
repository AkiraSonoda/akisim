# VivoxVoiceModule Documentation

## Overview

The VivoxVoiceModule is a **comprehensive voice communication system** that integrates OpenSimulator with Vivox's voice services. It provides **3D positional voice chat** capabilities, enabling avatars to communicate using spatial audio with distance-based attenuation, parcel-based voice channels, and enterprise-grade voice infrastructure through Vivox's cloud services.

## Purpose

**Primary Functions:**
- **3D Positional Voice** - Spatial audio with distance-based attenuation and directional audio
- **Parcel-Based Channels** - Automatic voice channel creation for land parcels and regions
- **Voice Account Management** - Integration with Vivox's user account system
- **Channel Management** - Dynamic creation and cleanup of voice channels
- **Estate Integration** - Respects estate and parcel voice settings and permissions
- **Enterprise Integration** - Professional voice infrastructure via Vivox cloud services

## Architecture

### Module Structure

The VivoxVoiceModule implements the `ISharedRegionModule` interface and integrates with Vivox voice services:

```
VivoxVoiceModule (ISharedRegionModule)
├── Voice Account Management
│   ├── User Authentication - Vivox account creation and login
│   ├── Credential Management - Secure token-based authentication
│   └── Account Provisioning - Dynamic user account setup
├── Channel Management
│   ├── Region Channels - Top-level voice channels per region
│   ├── Parcel Channels - Automatic channel creation for land parcels
│   ├── Directory Management - Hierarchical channel organization
│   └── Channel Cleanup - Automatic cleanup of unused channels
├── Spatial Audio Processing
│   ├── Distance Attenuation - Configurable distance models and roll-off
│   ├── Channel Modes - Open, lecture, presentation, auditorium modes
│   ├── Maximum Range - Configurable voice range limits
│   └── Clamping Distance - Near-field voice clarity settings
└── API Integration
    ├── Vivox REST API - HTTP-based communication with Vivox services
    ├── Capability Handlers - ProvisionVoiceAccountRequest, ParcelVoiceInfoRequest
    └── Administrative API - Channel creation, deletion, and management
```

### Core Components

**Distance Models:**
- **None (0)** - No attenuation, full volume at any distance
- **Inverse (1)** - Inverse distance attenuation (1/distance)
- **Linear (2)** - Linear attenuation (default)
- **Exponential (3)** - Exponential distance attenuation

**Channel Types:**
- **Positional** - 3D spatial audio with position awareness (default)
- **Channel** - Non-positional conference-style audio

**Channel Modes:**
- **Open** - All users can speak freely (default)
- **Lecture** - Moderated, specific speakers only
- **Presentation** - Single presenter, others listen
- **Auditorium** - Large-scale events with controlled speaking

## Configuration

### Basic Configuration

Configure the VivoxVoiceModule in the `[VivoxVoice]` section:

```ini
[VivoxVoice]
enabled = true
vivox_server = voice.example.com
vivox_sip_uri = voice.example.com:5060
vivox_admin_user = admin_username  
vivox_admin_password = admin_password
```

### Configuration Parameters

**Core Settings:**
- `enabled` - Enable/disable the voice module (default: false)
- `vivox_server` - Hostname/IP of the Vivox voice server (required)
- `vivox_sip_uri` - SIP URI for Vivox voice services (required)
- `vivox_admin_user` - Administrative username for Vivox API (required)
- `vivox_admin_password` - Administrative password for Vivox API (required)

**Channel Audio Settings:**
- `vivox_channel_distance_model` - Distance attenuation model (0-3, default: 2)
- `vivox_channel_roll_off` - Rate of volume attenuation (1.0-4.0, default: 2.0)
- `vivox_channel_max_range` - Maximum voice range in meters (0-160, default: 60)
- `vivox_channel_clamping_distance` - Distance before attenuation starts (0-160, default: 10)

**Channel Behavior Settings:**
- `vivox_channel_mode` - Channel interaction mode (open/lecture/presentation/auditorium, default: open)
- `vivox_channel_type` - Audio processing type (positional/channel, default: positional)
- `dump_xml` - Enable XML request/response debugging (default: false)

### Module Loading

The VivoxVoiceModule is loaded automatically via ModuleFactory:

```csharp
// ModuleFactory.cs automatically loads VivoxVoiceModule based on configuration
if (vivoxConfig?.GetBoolean("enabled", false) == true)
{
    var vivoxModuleInstance = LoadVivoxVoiceModule();
    yield return vivoxModuleInstance;
}
```

## API Integration

### Vivox Service Integration

**Voice Account API:**
```csharp
// Automatic account provisioning
public void ProvisionVoiceAccountRequest(IOSHttpRequest request, IOSHttpResponse response, 
    UUID agentID, Scene scene)
{
    // Creates or retrieves Vivox user account
    // Generates authentication tokens
    // Returns voice credentials to viewer
}
```

**Parcel Voice API:**
```csharp
// Dynamic channel assignment
public void ParcelVoiceInfoRequest(IOSHttpRequest request, IOSHttpResponse response, 
    UUID agentID, Scene scene)
{
    // Determines appropriate voice channel based on avatar location
    // Respects estate and parcel voice settings
    // Returns channel URI and spatial audio parameters
}
```

### Capability Registration

**Client Capabilities:**
```csharp
public void OnRegisterCaps(Scene scene, UUID agentID, Caps caps)
{
    caps.RegisterSimpleHandler("ProvisionVoiceAccountRequest", ...);
    caps.RegisterSimpleHandler("ParcelVoiceInfoRequest", ...);
    // Registers voice-related capabilities for viewer integration
}
```

## Spatial Audio System

### Distance Attenuation

**Linear Distance Model (Default):**
```csharp
// Volume calculation based on distance
// Full volume within clamping distance (default: 10m)
// Linear decrease from clamping distance to maximum range (default: 60m)
// Silent beyond maximum range
```

**Configuration Example:**
```ini
[VivoxVoice]
vivox_channel_distance_model = 2        # Linear attenuation
vivox_channel_roll_off = 2.0           # Standard attenuation rate
vivox_channel_max_range = 60           # 60 meter voice range
vivox_channel_clamping_distance = 10   # Full volume within 10 meters
```

### Channel Management

**Automatic Channel Creation:**
- **Region Channels** - Created automatically for each region
- **Parcel Channels** - Dynamic creation based on land parcel boundaries
- **Hierarchical Structure** - Parent-child relationship between region and parcel channels
- **Cleanup Management** - Automatic removal of unused channels

**Channel Naming Convention:**
```csharp
// Region channels: "{RegionUUID}D"
// Parcel channels: "{ParcelGlobalID}"
// Channel names: "{RegionName}:{ParcelName}"
```

## Estate and Parcel Integration

### Voice Permission System

**Estate-Level Controls:**
```csharp
if (!scene.RegionInfo.EstateSettings.AllowVoice)
{
    // Voice disabled at estate level
    channel_uri = String.Empty;
}
```

**Parcel-Level Controls:**
```csharp
if ((land.Flags & (uint)ParcelFlags.AllowVoiceChat) == 0)
{
    // Voice disabled for this parcel
    channel_uri = String.Empty;
}
```

### Permission Hierarchy

1. **Estate Settings** - Global voice enable/disable for entire estate
2. **Parcel Settings** - Individual parcel voice permissions
3. **Avatar Presence** - User must be present in region/parcel
4. **Vivox Authentication** - Valid voice account required

## Administrative Features

### Console Commands

**Debug Control:**
```bash
# Enable XML debugging
vivox debug on

# Disable XML debugging
vivox debug off
```

**Channel Management:**
- Automatic cleanup of residual channels on region restart
- Administrative login to Vivox services on module initialization
- Error handling and reconnection logic for service interruptions

### Logging and Monitoring

**Comprehensive Logging:**
```csharp
// Initialization and configuration validation
m_log.InfoFormat("[VivoxVoice] using vivox server {0}", m_vivoxServer);

// Channel creation and management
m_log.DebugFormat("[VivoxVoice] Found existing channel at {0}", channelUri);

// Error conditions and validation failures
m_log.WarnFormat("[VivoxVoice] Invalid value for roll off ({0}), reset to {1}", 
    rollOff, DEFAULT_VALUE);
```

## Security Considerations

### Authentication and Authorization

**Vivox Account Security:**
- Secure token-based authentication with Vivox services
- Administrative credentials stored in configuration files
- Dynamic user account provisioning with unique identifiers

**API Security:**
- HTTPS communication with Vivox services (when supported)
- Request validation and input sanitization
- Administrative privilege separation

### Privacy Controls

**Spatial Privacy:**
- Voice limited by distance and parcel boundaries
- Respect for estate and parcel voice settings
- Automatic channel isolation per region/parcel

**Access Controls:**
- Estate manager voice permissions
- Parcel owner voice control settings
- User-level voice enable/disable options

## Performance Considerations

### Scalability Features

**Connection Management:**
- Persistent connection to Vivox services
- Connection pooling and reuse
- Automatic reconnection on service interruption

**Channel Optimization:**
- Dynamic channel creation only when needed
- Automatic cleanup of unused channels
- Efficient channel lookup and caching

### Resource Management

**Memory Management:**
- Lightweight channel state tracking
- Efficient XML parsing and response handling
- Minimal object allocation in voice processing paths

**Network Efficiency:**
- RESTful API communication with Vivox
- Compressed audio streaming through Vivox infrastructure
- Client-side audio processing reduces server load

## Usage Scenarios

### Enterprise Virtual Meetings

**Configuration for Business Use:**
```ini
[VivoxVoice]
enabled = true
vivox_channel_mode = presentation      # Single presenter mode
vivox_channel_max_range = 100         # Extended range for large venues
vivox_channel_type = channel          # Non-positional for clarity
```

**Use Cases:**
- **Corporate Meetings** - Professional audio quality with presentation modes
- **Training Sessions** - Lecture mode with instructor control
- **Large Events** - Auditorium mode for conferences and presentations

### Social Virtual Worlds

**Configuration for Social Interaction:**
```ini
[VivoxVoice]
enabled = true
vivox_channel_mode = open             # Free conversation
vivox_channel_type = positional       # 3D spatial audio
vivox_channel_max_range = 60          # Natural conversation range
vivox_channel_clamping_distance = 10  # Intimate conversation zone
```

**Use Cases:**
- **Social Gatherings** - Natural conversation with spatial audio
- **Virtual Parties** - Multiple conversation groups with distance separation
- **Roleplaying** - Immersive voice interaction with spatial awareness

### Educational Environments

**Configuration for Education:**
```ini
[VivoxVoice]
enabled = true
vivox_channel_mode = lecture          # Teacher-controlled environment
vivox_channel_max_range = 80          # Classroom-appropriate range
vivox_channel_roll_off = 1.5          # Gentle attenuation for comfort
```

**Use Cases:**
- **Virtual Classrooms** - Structured learning environments
- **Distance Learning** - Professional educational delivery
- **Interactive Workshops** - Collaborative learning with voice interaction

## Troubleshooting

### Common Issues

**Voice Not Working:**
- **Cause**: Module disabled or misconfigured
- **Solution**: Check `enabled = true` and all required server settings
- **Debug**: Enable `dump_xml = true` to see API communication

**Cannot Connect to Vivox Services:**
- **Cause**: Network connectivity or server configuration issues
- **Solution**: Verify `vivox_server` and `vivox_sip_uri` settings
- **Debug**: Check server logs for connection errors

**Voice Quality Issues:**
- **Cause**: Inappropriate distance model or range settings
- **Solution**: Adjust `vivox_channel_roll_off` and `vivox_channel_max_range`
- **Debug**: Test different distance models and attenuation settings

### Configuration Validation

**Parameter Validation:**
- Roll-off values automatically constrained to 1.0-4.0 range
- Maximum range values constrained to 0-160 meter range  
- Channel mode and type values validated against supported options
- Missing required parameters cause module to disable with clear error messages

**Server Connectivity:**
- Automatic validation of server URLs on startup
- Connection testing during administrative login
- Clear error messages for connectivity issues

### Performance Issues

**High Latency:**
- **Cause**: Network connectivity to Vivox services
- **Solution**: Ensure reliable internet connection to Vivox infrastructure
- **Debug**: Monitor network connectivity and server response times

**Memory Usage:**
- **Cause**: Channel proliferation or connection leaks
- **Solution**: Verify automatic channel cleanup is functioning
- **Debug**: Monitor channel creation and deletion patterns

## Technical Specifications

### System Requirements

- **.NET 8.0** - Runtime environment
- **Vivox Account** - Commercial voice service subscription
- **Network Access** - Reliable internet connectivity to Vivox services
- **Audio Hardware** - Client-side microphone and speakers/headphones

### Vivox Service Integration

**API Compatibility:**
- Vivox REST API v2 integration
- HTTP-based communication with JSON/XML payloads
- Token-based authentication system
- SIP protocol integration for audio streaming

**Audio Specifications:**
- Multiple codec support through Vivox infrastructure
- Adaptive bitrate based on network conditions
- Cross-platform client compatibility
- Low-latency audio streaming optimized for virtual worlds

### Network Requirements

**Bandwidth Considerations:**
- Voice data handled by Vivox infrastructure
- OpenSim server requires minimal bandwidth for control messages
- Client audio streaming direct to/from Vivox services
- Typical usage: <100 kbps per concurrent voice user

**Port Requirements:**
- Outbound HTTP/HTTPS to Vivox services
- Client connections to Vivox SIP services
- Standard firewall-friendly protocols

The VivoxVoiceModule provides enterprise-grade voice communication infrastructure for OpenSimulator, enabling natural voice interaction in virtual environments with professional audio quality and scalable cloud-based delivery through Vivox's proven voice technology platform.