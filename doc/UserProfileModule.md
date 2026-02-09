# UserProfileModule Documentation

## Overview

The UserProfileModule is OpenSimulator's advanced profile system that provides comprehensive avatar profile functionality. It enables rich user profile features including profile editing, classifieds, picks, notes, and user preferences. This module requires a dedicated profile service backend and supports both local and hypergrid users.

## Module Type and Interface

**Module Type**: `INonSharedRegionModule`, `IProfileModule`  
**Assembly**: `OpenSim.Region.CoreModules.dll`  
**Namespace**: `OpenSim.Region.CoreModules.Avatar.UserProfiles`  

## Features

### Profile Management
- **Editable Profiles**: Users can update their profile text, interests, and images
- **Profile Images**: Support for profile pictures and first life images
- **Partner System**: Users can set and display partner relationships
- **Web URLs**: Configurable support for profile web URLs
- **Interest System**: Skills, wants, and language preferences

### Classifieds System
- **Classified Ads**: Users can create, edit, and delete classified advertisements
- **Cost System**: Integration with economy modules for classified pricing
- **Category System**: Organized classified categories
- **Search Integration**: Classifieds appear in viewer search results

### Picks System
- **Location Picks**: Users can create picks for favorite locations
- **Pick Management**: Create, edit, and delete picks with descriptions and images
- **Teleport Integration**: Direct teleportation from picks
- **Top Picks**: Priority picks system

### Notes System
- **Private Notes**: Users can write private notes about other users
- **Per-User Storage**: Notes are stored per-user and private
- **Cross-Grid Support**: Works with hypergrid users

### User Preferences
- **IM Preferences**: Email notification settings for instant messages
- **Visibility Settings**: Profile visibility controls
- **Email Integration**: Email address management

## Configuration

### Required Configuration

The module requires a `[UserProfiles]` configuration section with a valid ProfileServiceURL:

```ini
[UserProfiles]
    ; URL to the profile service
    ProfileServiceURL = http://localhost:8002/user

    ; Whether to allow users to set web URLs in profiles
    ; Default: true
    AllowUserProfileWebURLs = true
```

### Service Backend

The module requires a separate UserProfiles service backend, typically running as part of Robust services:

```ini
; In Robust.HG.ini or Robust.ini
[UserProfilesService]
    LocalServiceModule = "OpenSim.Services.UserProfilesService.dll:UserProfilesService"
    
    ; Database connection
    ConnectionString = "Data Source=localhost;Database=opensim;User ID=opensim;Password=***;Old Guids=true;"
```

## ModuleFactory Integration

### Loading Logic

The module is loaded as a non-shared module with the following logic:

```csharp
// Load UserProfiles module based on configuration
if (configSource != null)
{
    var userProfilesConfig = configSource.Configs["UserProfiles"];
    if (userProfilesConfig != null)
    {
        string profileServiceURL = userProfilesConfig.GetString("ProfileServiceURL", "");
        if (!string.IsNullOrEmpty(profileServiceURL))
        {
            yield return new UserProfileModule();
            m_log.Info("UserProfileModule loaded (advanced profile system)");
        }
        else
        {
            m_log.Info("UserProfileModule not loaded - ProfileServiceURL not configured");
        }
    }
}
```

### Profile Module Architecture

- **Single Profile System**: UserProfileModule is the primary and only profile module
- **Configuration Required**: Must have proper `[UserProfiles]` configuration to function
- **Comprehensive Features**: Provides full profile functionality for all use cases

## Architecture

### Communication Protocol
- **JSON-RPC**: Uses JSON-RPC for communication with profile service
- **Caching System**: Aggressive caching to reduce service calls
- **Async Processing**: Background processing for profile requests
- **Error Handling**: Graceful degradation when service is unavailable

### Hypergrid Support
- **Cross-Grid Profiles**: Fetches profiles from foreign grids
- **Asset Caching**: Caches foreign profile images locally
- **OpenProfile Support**: Fallback to OpenProfile protocol for compatibility
- **Local vs Foreign**: Intelligent routing based on user origin

### Profile Data Storage
- **Database Backend**: Persistent storage in UserProfiles service database
- **Cache Management**: Memory cache with configurable expiration
- **Image Handling**: Asset service integration for profile images
- **Data Validation**: Input validation and sanitization

## Key Components

### Core Methods

#### Profile Properties
- `RequestAvatarProperties()`: Handles profile property requests
- `AvatarPropertiesUpdate()`: Updates user profile information
- `AvatarInterestsUpdate()`: Updates user interests and skills

#### Classifieds Management
- `ClassifiedsRequest()`: Lists user's classified ads
- `ClassifiedInfoRequest()`: Retrieves specific classified details
- `ClassifiedInfoUpdate()`: Creates/updates classified ads
- `ClassifiedDelete()`: Removes classified ads

#### Picks Management
- `PicksRequest()`: Lists user's picks
- `PickInfoRequest()`: Retrieves specific pick details
- `PickInfoUpdate()`: Creates/updates picks
- `PickDelete()`: Removes picks

#### Notes System
- `NotesRequest()`: Retrieves notes about a user
- `NotesUpdate()`: Updates notes about a user

### Data Structures

#### UserProfileProperties
```csharp
public class UserProfileProperties
{
    public UUID UserId;
    public string AboutText;
    public string FirstLifeText;
    public UUID ImageId;
    public UUID FirstLifeImageId;
    public UUID PartnerId;
    public string WebUrl;
    public int WantToMask;
    public string WantToText;
    public int SkillsMask;
    public string SkillsText;
    public string Language;
}
```

#### UserProfileCacheEntry
```csharp
class UserProfileCacheEntry
{
    public UserProfileProperties props;
    public string born;
    public Byte[] membershipType;
    public uint flags;
    public Dictionary<UUID, UserClassifiedAdd> classifieds;
    public Dictionary<UUID, string> classifiedsLists;
    public Dictionary<UUID, UserProfilePick> picks;
    public Dictionary<UUID, string> picksList;
    public GroupMembershipData[] avatarGroups;
    public HashSet<IClientAPI> ClientsWaitingProps;
}
```

## Database Schema

The UserProfiles service uses several database tables:

### Core Tables
- **userprofile**: Main profile data (about text, images, partner, etc.)
- **userpicks**: Location picks created by users
- **userclassifieds**: Classified advertisements
- **usernotes**: Private notes about other users
- **userpreferences**: User preference settings

### Key Relationships
- All tables link to users via UUID primary keys
- Foreign key relationships maintain data integrity
- Indexes optimize common query patterns

## Performance Considerations

### Caching Strategy
- **Memory Cache**: 60-second expiration for profile data
- **Request Batching**: Async processing reduces latency
- **Cache Coherence**: Cache invalidation on updates
- **Foreign Asset Caching**: Local caching of hypergrid profile images

### Network Optimization
- **JSON-RPC Batching**: Multiple requests in single calls where possible
- **Selective Updates**: Only transmit changed data
- **Compression**: HTTP compression for large profile data
- **Timeout Handling**: Graceful timeout management

### Database Performance
- **Connection Pooling**: Efficient database connection management
- **Prepared Statements**: SQL injection protection and performance
- **Index Usage**: Optimized queries using proper indexes
- **Bulk Operations**: Efficient handling of multiple records

## Security Features

### Input Validation
- **SQL Injection Protection**: Parameterized queries
- **XSS Prevention**: HTML sanitization in profile text
- **Length Limits**: Enforced limits on text fields
- **UUID Validation**: Proper UUID format checking

### Access Control
- **User Ownership**: Users can only edit their own profiles
- **Privacy Controls**: Visibility settings respected
- **NPC Handling**: Special handling for NPC entities
- **Admin Override**: God users can view all profiles

### Cross-Grid Security
- **URI Validation**: Proper validation of hypergrid URIs
- **Certificate Validation**: SSL/TLS certificate checking
- **Rate Limiting**: Protection against DoS attacks
- **Timeout Protection**: Prevents hanging connections

## Error Handling

### Service Unavailability
```csharp
if (!rpc.JsonRpcRequest(ref parameters, "avatar_properties_request", serverURI, UUID.Random().ToString()))
{
    properties.AboutText = "Profile not available at this time. User may still be unknown to this grid";
    return false;
}
```

### Network Failures
- **Graceful Degradation**: Shows appropriate error messages
- **Retry Logic**: Automatic retry for transient failures
- **Fallback Modes**: OpenProfile fallback for compatibility
- **User Notification**: Clear error messages to users

## Integration Points

### Required Dependencies
- **UserAccountService**: For user account information
- **AssetService**: For profile image storage
- **UserManagement**: For local vs hypergrid user detection
- **GroupsModule**: For group membership display
- **MoneyModule**: For classified ad payments

### Optional Dependencies
- **FriendsModule**: For online status detection
- **PresenceService**: For god user presence checks
- **GridService**: For region location services

## Use Cases

### Personal Profiles
```ini
[UserProfiles]
    ProfileServiceURL = http://profiles.mygrid.com:8002/user
    AllowUserProfileWebURLs = true
```

### Commercial Grid
```ini
[UserProfiles]
    ProfileServiceURL = http://profiles.commercialgrid.com:8002/user
    AllowUserProfileWebURLs = false  ; Restrict external URLs
```

### Development Environment
```ini
; UserProfiles configuration required for profile functionality
[UserProfiles]
    ProfileServiceURL = http://localhost:8002/user
    AllowUserProfileWebURLs = true
```

## Migration Notes

### From Mono.Addins

The module was modernized to remove Mono.Addins dependencies:

**Before**:
```csharp
[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "UserProfilesModule")]
public class UserProfileModule : IProfileModule, INonSharedRegionModule
```

**After**:
```csharp
// No attributes needed - loaded by ModuleFactory
public class UserProfileModule : IProfileModule, INonSharedRegionModule
```

### Configuration-Based Loading

The module is now loaded based on configuration presence and validity rather than attribute discovery.

## Troubleshooting

### Common Issues

1. **Profile Service Unreachable**
   - Check ProfileServiceURL configuration
   - Verify network connectivity to profile service
   - Check firewall settings

2. **Database Connection Failures**
   - Verify database connection string in Robust configuration
   - Check database server availability
   - Ensure database schema is up to date

3. **Hypergrid Profile Issues**
   - Verify hypergrid connectivity
   - Check asset service configuration
   - Review network timeout settings

### Debug Configuration

```ini
[Startup]
    ; Enable profile module debugging
    LogLevel = DEBUG

[UserProfiles]
    ProfileServiceURL = http://localhost:8002/user
    AllowUserProfileWebURLs = true
    ; Add debug logging for profile requests
```

## Performance Tuning

### Cache Settings
- Increase cache expiration time for stable environments
- Reduce cache size in memory-constrained environments
- Monitor cache hit rates

### Database Optimization
- Ensure proper indexing on frequently queried columns
- Use connection pooling for better performance
- Consider read replicas for high-load environments

### Network Optimization
- Use HTTP compression
- Implement connection keep-alive
- Configure appropriate timeout values

## Future Enhancements

Potential improvements could include:
1. **Enhanced Caching**: Redis-based distributed caching
2. **Real-time Updates**: WebSocket-based real-time profile updates
3. **Advanced Search**: Full-text search capabilities
4. **Profile Analytics**: Usage statistics and analytics
5. **Social Features**: Enhanced social networking features
6. **Mobile API**: REST API for mobile applications

## See Also

- **UserAccountService**: User account management
- **UserProfilesService**: Backend profile service
- **ModuleFactory**: Module loading and dependency management
- **HypergridService**: Cross-grid functionality