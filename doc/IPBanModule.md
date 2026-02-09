# IPBanModule

## Overview

The **IPBanModule** is a shared region module that provides IP address and hostname-based access control for OpenSim virtual world environments. It enforces bans at the network level by blocking connections from specified IP addresses, IP ranges, and domain names before users can fully connect to regions. This module serves as a fundamental security layer, protecting virtual worlds from unwanted access and providing administrators with tools to maintain safe and controlled environments.

## Architecture

### Module Type
- **Interface**: `ISharedRegionModule`
- **Namespace**: `OpenSim.Region.CoreModules.Agent.IPBan`
- **Location**: `src/OpenSim.Region.CoreModules/Agent/IPBan/IPBanModule.cs`

### Dependencies
- **SceneBanner Class**: Internal helper class for client connection monitoring
- **Estate System**: Integrates with estate ban settings for automatic ban loading
- **File System**: Reads global ban lists from `bans.txt` file
- **DNS Services**: Performs reverse DNS lookups for hostname-based bans
- **Network Framework**: Uses System.Net for IP address processing

### Related Components
- **SceneBanner**: `src/OpenSim.Region.CoreModules/Agent/IPBan/SceneBanner.cs`
  - Handles per-scene client connection monitoring
  - Performs actual IP and hostname checking
  - Manages client disconnection for banned addresses

## Functionality

### Core Features

#### 1. IP Address Banning
- **Exact IP Matching**: Blocks specific IP addresses (e.g., "192.168.1.100")
- **Subnet Banning**: Supports partial IP matching for network ranges
- **Flexible Patterns**: Allows various IP matching patterns based on string prefixes
- **IPv4 Support**: Full support for IPv4 address blocking

#### 2. Hostname-Based Banning
- **Domain Blocking**: Blocks connections from specific domains
- **Subdomain Matching**: Blocks all subdomains of specified domains
- **Reverse DNS Integration**: Performs reverse DNS lookups for hostname verification
- **Pattern Matching**: Uses string containment for flexible hostname blocking

#### 3. Estate Integration
- **Automatic Loading**: Automatically loads bans from estate settings
- **IP Mask Support**: Supports estate ban IP masks
- **Hostname Mask Support**: Supports estate ban hostname masks
- **Multi-Region Sharing**: Shares ban lists across all regions in the same estate

#### 4. Global Ban Management
- **File-Based Bans**: Loads additional bans from `bans.txt` file
- **Runtime Management**: Supports adding bans during runtime
- **Comment Support**: Ignores lines starting with "#" in ban files
- **Persistent Storage**: Maintains ban list across server restarts

#### 5. Real-Time Enforcement
- **Connection Monitoring**: Monitors all incoming client connections
- **Immediate Blocking**: Disconnects banned clients immediately upon connection
- **Graceful Disconnection**: Provides clear disconnect messages to banned users
- **Performance Optimized**: Efficient checking with minimal impact on legitimate connections

### Ban Enforcement Process

#### Client Connection Flow
1. **Client Connection**: New client attempts to connect to region
2. **IP Resolution**: SceneBanner extracts client IP address
3. **DNS Lookup**: Attempts reverse DNS lookup for hostname (if possible)
4. **Ban Check**: Checks both IP and hostname against ban lists
5. **Enforcement**: Disconnects client if banned, otherwise allows connection
6. **Logging**: Records ban enforcement actions for administrative review

#### Ban Matching Logic

##### IP Address Matching
- **Exact Match**: "192.168.1.100" matches only "192.168.1.100"
- **Prefix Match**: "192.168.1" matches "192.168.10.x", "192.168.100.x", etc.
- **Network Range**: "192.168.1." matches only "192.168.1.x" network
- **Flexible Patterns**: Supports any prefix-based matching pattern

##### Hostname Matching
- **Domain Containment**: "example.com" blocks any hostname containing "example.com"
- **Subdomain Blocking**: "example.com" blocks "sub.example.com", "beta.example.com", etc.
- **Partial Matching**: "evil" would block "evil.com", "evilsite.net", "dangerous-evil.org"
- **Case Insensitive**: Hostname matching is case-insensitive

#### Error Handling
- **DNS Failures**: Gracefully handles DNS lookup failures
- **Network Errors**: Continues operation despite network connectivity issues
- **Invalid Bans**: Validates ban entries and warns about invalid formats
- **File Errors**: Handles missing or corrupted ban files gracefully

## Configuration

### Factory Integration
The module is automatically loaded by the `CoreModuleFactory` as a core security module:
- **Always Enabled**: Loaded automatically without configuration requirements
- **Security Priority**: Loaded early in the module initialization process
- **Essential Module**: Considered essential for server security

### Estate Ban Configuration
Estate bans are configured through the OpenSim estate management interface:

#### Estate Settings Example
```ini
# Estate bans are managed through estate tools, but can be configured directly in database:
# EstateBans table columns:
# - EstateID: ID of the estate
# - BannedHostIPMask: IP address or IP range to ban
# - BannedHostNameMask: Hostname or domain to ban
# - BanUser: UUID of banned user (not used by IPBanModule)
```

### Global Ban File Configuration
Create a `bans.txt` file in the OpenSim root directory:

```txt
# Global IP and hostname bans for OpenSim
# Lines starting with # are comments and will be ignored

# Ban specific IP addresses
192.168.100.50
10.0.0.100

# Ban IP ranges/subnets
192.168.100.
10.0.0.

# Ban domains and hostnames
evil.com
badactor.net
spam-source.org

# Ban partial hostnames (be careful with these)
botnet
malware
```

### No Configuration Required
The module operates without explicit configuration:
- **Zero Configuration**: Works out-of-the-box without setup
- **Automatic Integration**: Integrates with existing estate ban systems
- **Optional Enhancement**: `bans.txt` file provides additional ban capabilities
- **Runtime Management**: Bans can be added programmatically

## Implementation Details

### Initialization Process
1. **Module Registration**: Registers as shared region module
2. **Ban List Initialization**: Creates empty ban list structure
3. **Post-Initialization**: Loads global bans from `bans.txt` if present
4. **Region Integration**: Sets up SceneBanner for each added region

### Ban List Management

#### Data Structure
```csharp
private List<string> m_bans = new List<string>();
```

#### Thread Safety
- **Lock Protection**: All ban list access is protected by locks
- **Concurrent Access**: Safe for multiple threads to access simultaneously
- **Atomic Operations**: Ban additions and checks are atomic

#### Memory Management
- **Efficient Storage**: Simple string list for minimal memory overhead
- **No Persistence**: Ban list is rebuilt on each server restart
- **Cleanup**: Proper cleanup on module shutdown

### SceneBanner Integration

#### Per-Scene Monitoring
```csharp
new SceneBanner(scene, m_bans);
```

Each region gets its own SceneBanner instance that:
- **Shares Ban List**: References the same global ban list
- **Monitors Connections**: Handles OnNewClient events for that scene
- **Enforces Bans**: Performs actual disconnection of banned clients

#### Event Integration
```csharp
scene.EventManager.OnNewClient += EventManager_OnClientConnect;
```

### DNS Resolution Process

#### Reverse DNS Lookup
```csharp
try
{
    IPHostEntry rDNS = Dns.GetHostEntry(end);
    hostName = rDNS.HostName;
}
catch (System.Net.Sockets.SocketException)
{
    hostName = null; // DNS lookup failed
}
```

#### Fallback Strategy
- **DNS Success**: Check both IP and hostname against bans
- **DNS Failure**: Check only IP address against bans
- **Performance**: DNS lookups are performed asynchronously
- **Reliability**: DNS failures don't prevent IP-based banning

### Ban Checking Algorithm

#### Dual-Check Process
```csharp
foreach (string ban in bans)
{
    if (hostName.Contains(ban) || end.ToString().StartsWith(ban))
    {
        // Client is banned
        client.Disconnect("Banned - network \"" + ban + "\" is not allowed to connect to this server.");
        return;
    }
}
```

#### Performance Optimization
- **Early Exit**: Stops checking on first ban match
- **Minimal Checks**: Only checks if ban list contains entries
- **Efficient Matching**: Uses fast string operations for matching

## Usage Examples

### Basic Estate Ban Configuration
Configure through estate management tools or database:
```sql
INSERT INTO EstateBans (EstateID, BannedHostIPMask, BannedHostNameMask)
VALUES (1, '192.168.100.50', NULL);

INSERT INTO EstateBans (EstateID, BannedHostIPMask, BannedHostNameMask)
VALUES (1, NULL, 'evil.com');
```

### Global Ban File Example
Create `bans.txt` in OpenSim root directory:
```txt
# Comprehensive ban list example

# Known problematic IP ranges
192.168.100.
10.0.0.50
172.16.1.

# Malicious domains
evil-site.com
spam-source.net
botnet-command.org

# Educational bans (be specific to avoid false positives)
testbot
automated-client
griefing-tool
```

### Runtime Ban Management
```csharp
// Add a ban programmatically (from another module or console command)
IPBanModule banModule = scene.RequestModuleInterface<IPBanModule>();
if (banModule != null)
{
    banModule.Ban("192.168.1.100");  // Ban specific IP
    banModule.Ban("badactor.com");   // Ban domain
}
```

### Advanced IP Range Banning
```txt
# Various IP range examples in bans.txt

# Ban entire Class C network
192.168.1.

# Ban specific range
10.0.0.5
10.0.0.
10.0.1.

# Ban cloud provider ranges (example)
52.84.
54.230.
```

## Performance Considerations

### Connection Performance
- **Minimal Overhead**: Ban checking adds minimal latency to connections
- **Early Termination**: Banned connections are terminated quickly
- **DNS Caching**: System DNS caching improves hostname lookup performance
- **Efficient Algorithms**: String operations are optimized for performance

### Memory Usage
- **Lightweight Structure**: Simple string list with minimal memory footprint
- **Shared Data**: Ban list is shared across all regions
- **No Caching**: Doesn't cache DNS results to prevent stale data
- **Cleanup**: Proper memory cleanup on shutdown

### Network Impact
- **Reduced Load**: Prevents banned clients from consuming server resources
- **DNS Queries**: May generate reverse DNS lookup traffic
- **Quick Disconnection**: Minimizes bandwidth usage for banned connections
- **Logging**: Minimal network impact from logging operations

### Scalability Factors
- **Ban List Size**: Performance scales linearly with number of bans
- **Connection Volume**: Handles high connection volumes efficiently
- **DNS Performance**: Dependent on DNS server response times
- **Thread Safety**: Supports concurrent access from multiple regions

## Troubleshooting

### Common Issues

#### 1. Module Not Loading
**Symptoms**: IP bans not working, no ban enforcement
**Solutions**:
- Verify module is automatically loaded by CoreModuleFactory
- Check logs for IPBanModule loading messages
- Ensure no configuration conflicts preventing module loading
- Verify estate settings are properly configured

#### 2. Bans Not Working
**Symptoms**: Banned IPs can still connect
**Solutions**:
- Verify ban entries are correctly formatted
- Check estate ban settings in database
- Confirm SceneBanner is properly initialized
- Monitor logs for ban checking activity

#### 3. DNS Lookup Issues
**Symptoms**: Hostname bans not working, DNS-related errors
**Solutions**:
- Check DNS server configuration and connectivity
- Verify reverse DNS is working for test IPs
- Consider using IP-based bans instead of hostname bans
- Monitor DNS lookup failures in logs

#### 4. Global Bans Not Loading
**Symptoms**: bans.txt entries not effective
**Solutions**:
- Verify bans.txt exists in OpenSim root directory
- Check file permissions and accessibility
- Ensure proper file format (one ban per line)
- Monitor post-initialization logs for file loading

#### 5. Performance Issues
**Symptoms**: Slow connection times, high CPU usage
**Solutions**:
- Reduce number of bans if possible
- Use IP ranges instead of individual IPs
- Optimize DNS server performance
- Consider disabling reverse DNS lookups for hostname bans

### Debug Information
Enable debug logging to see detailed module operations:
```ini
[Startup]
LogLevel = DEBUG
```

This will show:
- Module initialization and ban loading
- Region registration and SceneBanner setup
- Individual ban checks and enforcement actions
- DNS lookup attempts and failures
- Ban file loading and parsing

### Performance Monitoring
Monitor these metrics for optimal performance:
- **Ban Check Frequency**: Track number of ban checks performed
- **DNS Lookup Time**: Monitor reverse DNS lookup performance
- **Ban Enforcement Rate**: Track percentage of connections banned
- **Memory Usage**: Monitor ban list memory consumption
- **Connection Performance**: Track impact on legitimate connection times

### Configuration Validation
Use these steps to validate configuration:

1. **Check Module Loading**:
```bash
# Search for IPBanModule in logs
grep "IPBanModule" OpenSim.log
```

2. **Verify Ban Loading**:
```bash
# Check ban loading messages
grep "ban" OpenSim.log | grep -i load
```

3. **Monitor Ban Enforcement**:
```bash
# Track ban enforcement actions
grep "Disconnected.*ban" OpenSim.log
```

## Ban Management Best Practices

### Effective Ban Strategies
- **Be Specific**: Use specific IP addresses when possible to avoid false positives
- **Use Ranges Carefully**: IP range bans can affect legitimate users
- **Document Bans**: Keep records of why specific bans were added
- **Regular Review**: Periodically review and clean up ban lists

### Security Considerations
- **False Positives**: Overly broad bans can block legitimate users
- **Ban Evasion**: Sophisticated attackers may use different IPs/proxies
- **Complementary Security**: Use IPBanModule alongside other security measures
- **Monitoring**: Regularly monitor ban effectiveness and adjust as needed

### Maintenance Guidelines
- **Backup Ban Lists**: Keep backups of important ban configurations
- **Test Changes**: Test ban changes in development environment first
- **Monitor Impact**: Watch for impact on legitimate user connections
- **Documentation**: Document ban reasons and review schedules

## Integration Notes

### Factory Loading
- Loaded automatically by `CoreModuleFactory.CreateSharedModules()`
- No configuration required for basic operation
- Loaded early in initialization process for security priority

### Estate System Integration
- **Seamless Integration**: Automatically reads estate ban settings
- **Database Compatibility**: Works with existing estate ban database structure
- **Management Tools**: Compatible with existing estate management interfaces
- **Multi-Estate Support**: Supports different ban lists per estate

### Security Framework Integration
- **First Line Defense**: Provides network-level access control
- **Complementary Security**: Works alongside other security modules
- **Event Integration**: Uses scene event system for connection monitoring
- **Logging Integration**: Provides detailed security logging

### SceneBanner Architecture
- **Per-Scene Instances**: Each region gets its own SceneBanner
- **Shared Ban Lists**: All SceneBanners reference the same global ban list
- **Event-Driven**: Uses scene events for efficient connection monitoring
- **Stateless Design**: No per-client state maintained

## Security Considerations

### Network Security
- **IP Spoofing**: IPBanModule cannot prevent IP address spoofing
- **Proxy Evasion**: Banned users may use proxies or VPNs to bypass bans
- **DNS Manipulation**: Hostname bans can be evaded through DNS manipulation
- **Complementary Measures**: Should be used with other security systems

### Privacy Implications
- **IP Logging**: Client IP addresses are logged for ban enforcement
- **DNS Queries**: Reverse DNS lookups may expose connection patterns
- **Ban Records**: Ban enforcement actions are logged with IP addresses
- **Data Retention**: Consider privacy policies for ban log retention

### Performance vs Security
- **Trade-offs**: More comprehensive bans may impact performance
- **Resource Usage**: DNS lookups consume CPU and network resources
- **False Positives**: Overly broad bans can impact legitimate users
- **Monitoring**: Balance security effectiveness with operational impact

## See Also
- [CoreModuleFactory](./CoreModuleFactory.md) - Module loading system
- [Estate Management](../docs/EstateManagement.md) - Estate administration and ban management
- [Security Architecture](../docs/SecurityArchitecture.md) - Overall OpenSim security framework
- [Access Control](./AccessModule.md) - Region-level access control module