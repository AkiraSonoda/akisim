# GodNamesModule Technical Documentation

## Overview

The **GodNamesModule** is an optional OpenSimulator module that provides viewer support for displaying special god names and identifiers. It integrates with viewer features to highlight god users with distinctive name displays, helping users identify administrators and staff members in virtual worlds. The module is essential for grid administration visibility and user community management.

## Architecture and Interfaces

### Core Interfaces
- **ISharedRegionModule**: Shared across regions module lifecycle
- **SimulatorFeatures Integration**: Provides god name data to viewer features

### Key Components
- **God Name Lists**: Configure full names and surnames for god identification
- **Viewer Integration**: Simulator features support for viewer god name display
- **Configuration Management**: Flexible configuration of god name patterns
- **Feature Broadcasting**: Automatic god name data distribution to connected viewers

## God Name System

### Name Recognition Types

#### Full Names
Complete names that identify god users:
- **Exact Matches**: Full "Firstname Lastname" combinations
- **Administrative Accounts**: Special service account names
- **Staff Identifiers**: Grid staff and support personnel names
- **System Accounts**: Automated system and bot account names

#### Surnames (Last Names)
Last name patterns that identify god users:
- **Family Names**: Administrative family surnames
- **Organization Names**: Grid organization identifiers
- **Role-Based Names**: Names indicating administrative roles
- **Special Suffixes**: Distinctive surname patterns for gods

### Configuration System

#### Module Enablement
```ini
[GodNames]
; Enable GodNamesModule for viewer god name display
Enabled = true
```

#### God Name Configuration
```ini
[GodNames]
Enabled = true

; Full names for god identification (comma-separated)
FullNames = "Grid Administrator, System Manager, Support Staff"

; Surnames for god identification (comma-separated)
Surnames = "Linden, Admin, Staff, Manager, Support"
```

#### Configuration Processing
```csharp
public void Initialise(IConfigSource config)
{
    IConfig moduleConfig = config.Configs["GodNames"];

    if (moduleConfig == null) {
        return;
    }

    if (!moduleConfig.GetBoolean("Enabled", false)) {
        m_log.Info("[GODNAMES]: Addon is disabled");
        return;
    }

    m_enabled = true;
    string conf_str = moduleConfig.GetString("FullNames", String.Empty);
    if (conf_str != String.Empty)
    {
        foreach (string strl in conf_str.Split(',')) {
            string strlan = strl.Trim(" \t".ToCharArray());
            m_log.DebugFormat("[GODNAMES]: Adding {0} as a God name", strlan);
            m_fullNames.Add(strlan);
        }
    }
}
```

## Viewer Integration

### Simulator Features Interface

#### Feature Registration
```csharp
public virtual void RegionLoaded(Scene scene)
{
    if (!m_enabled)
        return;

    ISimulatorFeaturesModule featuresModule = scene.RequestModuleInterface<ISimulatorFeaturesModule>();

    if (featuresModule != null)
        featuresModule.OnSimulatorFeaturesRequest += OnSimulatorFeaturesRequest;
}
```

#### Feature Data Provision
```csharp
private void OnSimulatorFeaturesRequest(UUID agentID, ref OSDMap features)
{
    OSD namesmap = new OSDMap();
    if (features.ContainsKey("god_names"))
        namesmap = features["god_names"];
    else
        features["god_names"] = namesmap;

    OSDArray fnames = new OSDArray();
    foreach (string name in m_fullNames) {
        fnames.Add(name);
    }
    ((OSDMap)namesmap)["full_names"] = fnames;

    OSDArray lnames = new OSDArray();
    foreach (string name in m_lastNames) {
        lnames.Add(name);
    }
    ((OSDMap)namesmap)["last_names"] = lnames;
}
```

### Data Format

#### God Names Feature Structure
```json
{
    "god_names": {
        "full_names": [
            "Grid Administrator",
            "System Manager",
            "Support Staff"
        ],
        "last_names": [
            "Linden",
            "Admin",
            "Staff",
            "Manager"
        ]
    }
}
```

#### OSD Data Types
- **OSDMap**: Container for god names feature data
- **OSDArray**: Lists of full names and surnames
- **String Values**: Individual name entries for viewer processing

## Name Matching Logic

### Full Name Matching
```csharp
// Example viewer-side logic (conceptual)
bool IsGodFullName(string fullName)
{
    foreach (string godName in godNamesFeature["full_names"])
    {
        if (fullName.Equals(godName, StringComparison.InvariantCultureIgnoreCase))
            return true;
    }
    return false;
}
```

### Surname Matching
```csharp
// Example viewer-side logic (conceptual)
bool IsGodSurname(string lastName)
{
    foreach (string godSurname in godNamesFeature["last_names"])
    {
        if (lastName.Equals(godSurname, StringComparison.InvariantCultureIgnoreCase))
            return true;
    }
    return false;
}
```

### Viewer Display Enhancement
- **Name Highlighting**: Special colors or formatting for god names
- **Icon Display**: Administrative badges or symbols next to names
- **Tooltip Information**: Additional context for god users
- **UI Differentiation**: Distinctive appearance in user lists and chat

## Administrative Use Cases

### Grid Management
- **Staff Identification**: Clearly identify grid administrators and staff
- **Support Recognition**: Help users identify official support personnel
- **Authority Display**: Visual indication of administrative authority
- **Trust Building**: Build user confidence through clear staff identification

### Community Management
- **Moderation Visibility**: Make moderators easily identifiable
- **Event Management**: Identify event organizers and coordinators
- **Help Desk**: Clear identification of help desk personnel
- **Official Communications**: Distinguish official announcements

### Security Benefits
- **Impersonation Prevention**: Help users identify legitimate administrators
- **Social Engineering Protection**: Reduce risks from fake authority figures
- **Trust Verification**: Enable users to verify administrative claims
- **Fraud Reduction**: Minimize social manipulation through false authority

## Configuration Examples

### Basic Grid Setup
```ini
[GodNames]
Enabled = true
FullNames = "Grid Manager, System Admin"
Surnames = "Admin, Staff"
```

### Large Grid Configuration
```ini
[GodNames]
Enabled = true
FullNames = "Grid Administrator, Head of Support, Community Manager, Event Coordinator, Technical Support"
Surnames = "Linden, Admin, Staff, Support, Manager, Coordinator, Developer, Moderator"
```

### Role-Based Configuration
```ini
[GodNames]
Enabled = true
FullNames = "Estate Manager, Region Owner, Grid Owner"
Surnames = "Owner, Manager, Admin, Developer, Support, Moderator, Coordinator"
```

## Performance Considerations

### Efficient Operations
- **Static Data**: God names loaded once during initialization
- **Memory Caching**: Names stored in memory for fast access
- **Event-Driven**: Only processes data when requested by viewers
- **Minimal Overhead**: Lightweight feature data transmission

### Scalability Features
- **Shared Module**: Single instance serves all regions efficiently
- **On-Demand Delivery**: Feature data sent only when requested
- **Configurable Lists**: Flexible name list management
- **Low Bandwidth**: Minimal network overhead for feature data

### Optimization Strategies
- **List Trimming**: Automatic whitespace trimming for clean data
- **Lazy Loading**: Feature data prepared only when needed
- **Efficient Formats**: Optimized OSD data structures for transmission
- **Cache-Friendly**: Static configuration data ideal for caching

## Module Lifecycle

### Initialization
```csharp
public void Initialise(IConfigSource config)
```
- **Configuration Loading**: Read [GodNames] section from configuration
- **Name List Processing**: Parse and store full names and surnames
- **Enable/Disable Logic**: Set module enabled state based on configuration
- **Debug Logging**: Log all configured god names for verification

### Region Integration
```csharp
public void AddRegion(Scene scene) { /*no op*/ }
public void RegionLoaded(Scene scene)
```
- **Feature Module Access**: Request ISimulatorFeaturesModule interface
- **Event Registration**: Subscribe to OnSimulatorFeaturesRequest events
- **Service Availability**: Make god name data available to viewers
- **Conditional Loading**: Only activate if module is enabled

### Event Handling
- **Feature Requests**: Respond to viewer simulator features requests
- **Data Provision**: Supply god names data in proper OSD format
- **Dynamic Updates**: Handle multiple feature requests efficiently
- **Error Handling**: Graceful handling of missing feature modules

### Cleanup
```csharp
public void Close() { /*no op*/ }
```
- **Minimal Cleanup**: Module uses static data with automatic cleanup
- **Event Unsubscription**: Automatic cleanup through scene lifecycle
- **Memory Management**: Static lists cleaned up with module disposal

## Security Considerations

### Configuration Security
- **Admin-Only Config**: God names configuration requires server access
- **Validation**: Input validation and sanitization for name entries
- **Case Sensitivity**: Consistent case handling for name matching
- **Update Control**: Changes require server restart for security

### Data Integrity
- **Read-Only Operation**: Module only reads configuration data
- **Static Data**: God names cannot be modified at runtime
- **Transmission Security**: Feature data sent through secure viewer channels
- **Access Control**: Only authenticated viewers receive feature data

### Privacy Protection
- **Public Information**: God names are intentionally public data
- **No Personal Data**: Module doesn't expose personal information
- **Administrative Transparency**: Promotes transparency in administration
- **User Awareness**: Helps users identify legitimate authority figures

## Error Handling and Validation

### Configuration Validation
```csharp
if (moduleConfig == null) {
    return;
}

if (!moduleConfig.GetBoolean("Enabled", false)) {
    m_log.Info("[GODNAMES]: Addon is disabled");
    return;
}
```

### Input Processing
```csharp
string strlan = strl.Trim(" \t".ToCharArray());
if (!string.IsNullOrEmpty(strlan))
{
    m_log.DebugFormat("[GODNAMES]: Adding {0} as a God name", strlan);
    m_fullNames.Add(strlan);
}
```

### Safe Operation
- **Null Checking**: Comprehensive null checking for all operations
- **Empty String Handling**: Graceful handling of empty configuration values
- **Module Availability**: Safe handling of missing simulator features module
- **Feature Conflicts**: Proper handling of existing god_names features

## Integration Examples

### Basic Module Setup
```ini
[GodNames]
Enabled = true
FullNames = "Grid Admin"
Surnames = "Admin"
```

### Programmatic Integration
```csharp
// Access god names feature data
ISimulatorFeaturesModule features = scene.RequestModuleInterface<ISimulatorFeaturesModule>();
if (features != null)
{
    // God names automatically included in simulator features
    // when GodNamesModule is enabled and configured
}
```

### Viewer Feature Access
```csharp
// Conceptual viewer-side code
bool CheckIfGodUser(string firstName, string lastName)
{
    var godNames = simulatorFeatures["god_names"];
    string fullName = $"{firstName} {lastName}";

    // Check full names
    if (godNames["full_names"].Contains(fullName))
        return true;

    // Check surnames
    if (godNames["last_names"].Contains(lastName))
        return true;

    return false;
}
```

## Migration Notes

### Factory Integration
- **Mono.Addins Removal**: Migrated from plugin-based to factory-based loading
- **Configuration-based Loading**: Controlled via [GodNames] section configuration
- **Default Behavior**: Disabled by default, requires explicit configuration
- **Logging Integration**: Comprehensive debug and info logging for operations

### Configuration Migration
Previous versions may have used different configuration approaches:
```ini
# New explicit configuration required
[GodNames]
Enabled = true
FullNames = "Admin Names"
Surnames = "Admin Surnames"
```

### Dependencies
- **Simulator Features**: Requires ISimulatorFeaturesModule for viewer integration
- **Scene Management**: Integration with scene and region lifecycle
- **Viewer Support**: Compatible viewers required for god name display
- **Configuration System**: Depends on Nini configuration framework

## Troubleshooting

### Common Issues

#### Module Not Loading
- **Check Configuration**: Ensure [GodNames] section exists with Enabled = true
- **Name Lists**: Verify FullNames and/or Surnames are configured
- **Log Messages**: Check for loading debug messages in server logs
- **Case Sensitivity**: Configuration values are case-sensitive

#### God Names Not Displaying
- **Viewer Support**: Ensure viewer supports god names feature
- **Feature Module**: Verify ISimulatorFeaturesModule is available
- **Configuration Syntax**: Check comma-separated name list syntax
- **Name Matching**: Verify exact name matching in configuration

#### Performance Issues
- **List Size**: Large name lists may impact memory usage
- **Feature Requests**: Monitor frequency of simulator features requests
- **Network Usage**: Large name lists increase feature data transmission
- **Caching**: Ensure viewer properly caches simulator features

## Usage Examples

### Standard Grid Configuration
```ini
[GodNames]
Enabled = true
FullNames = "Grid Administrator, Support Manager"
Surnames = "Linden, Admin, Support, Staff"
```

### Educational Grid Setup
```ini
[GodNames]
Enabled = true
FullNames = "Professor Smith, Dean Johnson, Campus Security"
Surnames = "Faculty, Staff, Security, Admin"
```

### Corporate Virtual World
```ini
[GodNames]
Enabled = true
FullNames = "IT Administrator, HR Manager, Facilities Coordinator"
Surnames = "Admin, Manager, Coordinator, Executive, IT"
```

### Event-Specific Configuration
```ini
[GodNames]
Enabled = true
FullNames = "Event Host, Technical Support, Registration Desk"
Surnames = "Host, Support, Staff, Organizer, Volunteer"
```

This documentation reflects the GodNamesModule implementation in `src/OpenSim.Region.OptionalModules/ViewerSupport/GodNamesModule.cs` and its integration with the factory-based module loading system.