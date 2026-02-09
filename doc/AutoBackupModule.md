# AutoBackupModule Technical Documentation

## Overview

The AutoBackupModule is a shared region module that provides automated backup functionality for OpenSimulator regions. It creates scheduled OpenSimulator Archive (OAR) backups of regions with configurable intervals, naming schemes, file retention policies, and post-backup script execution capabilities.

## Module Classification

- **Type**: ISharedRegionModule
- **Namespace**: OpenSim.Region.OptionalModules.World.AutoBackup
- **Assembly**: OpenSim.Region.OptionalModules
- **Factory Integration**: ✅ Integrated in ModuleFactory.cs with configuration-based loading

## Core Functionality

### Primary Purpose

The AutoBackupModule enables administrators to configure automated, scheduled backups of OpenSimulator regions using the OAR (OpenSimulator Archive) format. It provides comprehensive backup management including scheduling, naming, retention, and post-processing capabilities.

### Key Features

1. **Scheduled Backups**: Automated backup execution at configurable intervals
2. **Multiple Naming Schemes**: Time-based, sequential, or overwrite naming options
3. **File Retention Management**: Automatic cleanup of old backup files
4. **Asset Control**: Option to include or exclude assets from backups
5. **Post-Backup Scripts**: Execute custom scripts after backup completion
6. **Per-Region Configuration**: Individual backup settings for each region
7. **Manual Backup Commands**: Console commands for immediate backup execution
8. **Concurrent Safety**: Thread-safe operation with busy state management

## Technical Architecture

### Module Lifecycle

```csharp
// Module initialization sequence for shared modules
1. Initialise(IConfigSource) - Global configuration and timer setup
2. AddRegion(Scene) - Add region to backup tracking
3. RegionLoaded(Scene) - Configure per-region backup settings
4. PostInitialise() - Final setup (currently no-op)
5. Close() - Cleanup timers and resources
```

### State Management

The module uses `AutoBackupModuleState` objects to track per-region configuration:

```csharp
public class AutoBackupModuleState
{
    public bool Enabled { get; set; }        // Enable backup for this region
    public bool SkipAssets { get; set; }     // Skip assets in backup
    public NamingType NamingType { get; set; } // File naming scheme
    public string Script { get; set; }       // Post-backup script path
}
```

### Timer-Based Architecture

The module uses a master timer for coordinated backup execution:

```csharp
private Timer m_masterTimer;
private bool m_busy;  // Prevents concurrent backup operations
private double m_baseInterval;  // Backup interval in milliseconds
```

## Configuration System

### Global Configuration ([AutoBackupModule] section)

#### Required Settings
- **AutoBackupModuleEnabled**: `boolean` - Enables the entire module (default: false)
- **AutoBackupInterval**: `double` - Backup interval in minutes (default: 720 = 12 hours)
- **AutoBackupDir**: `string` - Directory for backup files (default: "." = current directory)

#### Optional Global Settings
- **AutoBackupKeepFilesForDays**: `integer` - Days to retain backup files, 0 disables cleanup (default: -1)
- **AutoBackup**: `boolean` - Default per-region backup enable flag (default: false)
- **AutoBackupSkipAssets**: `boolean` - Default asset inclusion setting (default: false)
- **AutoBackupNaming**: `string` - Default naming scheme: "Time", "Sequential", or "Overwrite" (default: "Time")
- **AutoBackupScript**: `string` - Default post-backup script path (default: none)

### Per-Region Configuration (in Regions.ini or region-specific config)

#### Region-Specific Overrides
- **AutoBackup**: `boolean` - Enable backup for this specific region
- **AutoBackupSkipAssets**: `boolean` - Skip assets for this region
- **AutoBackupNaming**: `string` - Naming scheme for this region
- **AutoBackupScript**: `string` - Post-backup script for this region
- **AutoBackupKeepFilesForDays**: `integer` - File retention period for this region

### Configuration Example

```ini
[AutoBackupModule]
AutoBackupModuleEnabled = true
AutoBackupInterval = 360              ; 6 hours
AutoBackupDir = /opt/opensim/backups
AutoBackupKeepFilesForDays = 30
AutoBackup = true                     ; Default for all regions
AutoBackupSkipAssets = false
AutoBackupNaming = Time
AutoBackupScript = /opt/opensim/scripts/post-backup.sh

; Per-region settings in Regions.ini
[RegionName]
; Standard region configuration...
AutoBackup = true
AutoBackupNaming = Sequential
AutoBackupKeepFilesForDays = 14
```

## Naming Schemes

### Time-Based Naming (Default)
- **Format**: `RegionName_YYYY-m-DD-h-MM-s.oar`
- **Example**: `MyRegion_2024y_3M_15d_14h_30m_45s.oar`
- **Behavior**: Never overwrites existing files, uses timestamp for uniqueness

### Sequential Naming
- **Format**: `RegionName_N.oar` where N is incrementing number
- **Example**: `MyRegion_1.oar`, `MyRegion_2.oar`, `MyRegion_3.oar`
- **Behavior**: Finds highest existing number and creates next sequential file

### Overwrite Naming
- **Format**: `RegionName.oar`
- **Example**: `MyRegion.oar`
- **Behavior**: Always overwrites the same file, only keeps latest backup

## Backup Process Workflow

### 1. Timer Trigger
```csharp
private void HandleElapsed(object sender, ElapsedEventArgs e)
{
    if (!m_enabled || m_busy) return;

    m_busy = true;

    // Clean up old files if configured
    if(m_doneFirst && m_KeepFilesForDays > 0)
        RemoveOldFiles();

    // Process all regions
    foreach (IScene scene in m_Scenes)
        DoRegionBackup(scene);

    m_busy = false;
    m_masterTimer.Start(); // Restart timer
}
```

### 2. Region Backup Execution
```csharp
private void DoRegionBackup(IScene scene)
{
    // Verify region is ready
    if (!scene.Ready) return;

    // Get region configuration
    AutoBackupModuleState state = m_states[scene];
    if (!state.Enabled) return;

    // Generate backup file path
    string savePath = BuildOarPath(scene.RegionInfo.RegionName,
                                   m_backupDir,
                                   state.NamingType);

    // Execute backup
    IRegionArchiverModule archiver = scene.RequestModuleInterface<IRegionArchiverModule>();
    Dictionary<string, object> options = new Dictionary<string, object>();
    if (state.SkipAssets) options["noassets"] = true;

    archiver.ArchiveRegion(savePath, Guid.NewGuid(), options);

    // Execute post-backup script
    ExecuteScript(state.Script, savePath);
}
```

### 3. File Path Generation
The module supports complex path generation logic for different naming schemes:

```csharp
private static string BuildOarPath(string regionName, string baseDir, NamingType naming)
{
    switch (naming)
    {
        case NamingType.Overwrite:
            return Path.Combine(baseDir, regionName + ".oar");

        case NamingType.Time:
            return Path.Combine(baseDir, regionName + GetTimeString() + ".oar");

        case NamingType.Sequential:
            return GetNextFile(baseDir, regionName);
    }
}
```

## File Management

### Automatic File Cleanup
```csharp
private void RemoveOldFiles()
{
    string[] files = Directory.GetFiles(m_backupDir, "*.oar");
    DateTime cutoffDate = DateTime.Now.AddDays(-m_KeepFilesForDays);

    foreach (string file in files)
    {
        FileInfo fi = new FileInfo(file);
        if (fi.CreationTime < cutoffDate)
            fi.Delete();
    }
}
```

### Sequential Number Detection
The module uses regex pattern matching to detect existing sequential backup files:

```csharp
private static long GetNextOarFileNumber(string dirName, string regionName)
{
    DirectoryInfo di = new DirectoryInfo(dirName);
    FileInfo[] files = di.GetFiles(regionName, SearchOption.TopDirectoryOnly);

    Regex regex = new Regex(regionName + "_([0-9])+" + ".oar");
    // Find highest existing number and increment
}
```

## Console Commands

### Manual Backup Command

**Command**: `dooarbackup <regionName> | ALL`

**Usage Examples**:
```bash
# Backup specific region
dooarbackup MyRegion

# Backup all regions
dooarbackup ALL
```

**Implementation**:
```csharp
private void DoBackup(string module, string[] args)
{
    if (m_busy)
    {
        MainConsole.Instance.Output("Already doing a backup, please try later");
        return;
    }

    m_masterTimer.Stop();
    m_busy = true;

    try
    {
        if (args[1] == "ALL")
        {
            foreach (Scene scene in m_Scenes)
                DoRegionBackup(scene);
        }
        else
        {
            Scene targetScene = FindSceneByName(args[1]);
            if (targetScene != null)
                DoRegionBackup(targetScene);
        }
    }
    finally
    {
        m_busy = false;
        m_masterTimer.Start();
    }
}
```

## Post-Backup Script Execution

### Script Execution
```csharp
private static void ExecuteScript(string scriptName, string savePath)
{
    if (string.IsNullOrEmpty(scriptName)) return;

    try
    {
        ProcessStartInfo psi = new ProcessStartInfo(scriptName);
        psi.Arguments = savePath;  // OAR file path passed as argument
        psi.CreateNoWindow = true;

        Process proc = Process.Start(psi);
        proc.ErrorDataReceived += HandleProcErrorDataReceived;
    }
    catch (Exception e)
    {
        m_log.Warn("Exception encountered when trying to run script for oar backup " + savePath, e);
    }
}
```

### Script Interface
Post-backup scripts receive the backup file path as their first argument (`argv[1]`):

```bash
#!/bin/bash
# post-backup.sh
OAR_FILE="$1"
echo "Processing backup: $OAR_FILE"

# Example: Compress the backup
gzip "$OAR_FILE"

# Example: Upload to remote storage
rsync "$OAR_FILE" user@backup-server:/backups/

# Example: Send notification
echo "Backup completed: $OAR_FILE" | mail -s "OpenSim Backup" admin@example.com
```

## Error Handling and Logging

### Comprehensive Error Handling
```csharp
// Directory access validation
try
{
    DirectoryInfo dirinfo = new DirectoryInfo(backupDir);
    if (!dirinfo.Exists) dirinfo.Create();
}
catch (Exception e)
{
    m_enabled = false;
    m_log.WarnFormat("[AUTO BACKUP]: Error accessing backup folder {0}. Module disabled. {1}",
                     backupDir, e);
    return;
}

// Region state validation
if (!scene.Ready)
{
    m_log.Warn("[AUTO BACKUP]: Not backing up region " + scene.RegionInfo.RegionName +
               " because its status is " + scene.RegionStatus);
    return;
}
```

### Logging Strategy
- **Debug Level**: Configuration details, state changes, process flow
- **Info Level**: Module lifecycle events, backup completion
- **Warn Level**: Configuration issues, script execution problems
- **Error Level**: File system errors, critical failures

### Example Log Output
```
[AUTO BACKUP]: AutoBackupModule enabled
[AUTO BACKUP]: Default config:
[AUTO BACKUP]: AutoBackup: ENABLED
[AUTO BACKUP]: Naming Type: Time
[AUTO BACKUP]: Script: /opt/opensim/scripts/post-backup.sh
[AUTO BACKUP]: Config for RegionName
[AUTO BACKUP]: AutoBackup: ENABLED
[AUTO BACKUP]: Naming Type: Sequential
[AUTO BACKUP]: Script:
[AUTO BACKUP]: Backing up region RegionName
```

## Performance Considerations

### Thread Safety
- Uses `lock(m_Scenes)` for thread-safe region list access
- Implements busy state management to prevent concurrent operations
- Timer-based coordination prevents overlapping backup processes

### Resource Management
- Stops timer during module shutdown to prevent callbacks
- Proper disposal of timer resources in `Close()` method
- Efficient file system operations with minimal memory allocation

### Backup Size Optimization
- **Asset Exclusion**: `SkipAssets` option can significantly reduce backup size
- **Selective Backups**: Per-region enable/disable for resource management
- **Retention Policies**: Automatic cleanup prevents disk space exhaustion

## Security Considerations

### File System Security
- Validates backup directory accessibility during initialization
- Handles file system exceptions gracefully
- Creates directories with appropriate permissions

### Script Execution Security
- **Warning**: Script execution poses security risks if configuration is untrusted
- Scripts run with OpenSimulator process privileges
- Validates script file existence before execution
- Captures and logs script error output

### Configuration Security
- Module can be completely disabled via configuration
- Per-region settings allow granular control
- File retention policies prevent uncontrolled disk usage

## Dependencies

### Core Framework Dependencies
- `OpenSim.Framework` - Core data structures and utilities
- `OpenSim.Region.Framework` - Scene and region management interfaces
- `System.Timers` - Timer-based scheduling functionality

### Archive System Dependencies
- `IRegionArchiverModule` - OAR creation and management interface
- Scene archiver subsystem for region serialization

### File System Dependencies
- .NET file system APIs for backup file management
- Directory and file manipulation capabilities
- Process execution for post-backup scripts

## Integration Points

### Scene Manager Integration
- Registers with multiple scenes as a shared module
- Uses scene readiness status for backup validation
- Integrates with scene lifecycle events

### Configuration System Integration
- Reads both global and per-region configuration settings
- Supports configuration inheritance and override patterns
- Validates configuration consistency

### Archive System Integration
- Leverages existing OAR creation infrastructure
- Supports OAR creation options (asset inclusion, etc.)
- Uses GUID-based archive operation tracking

## Troubleshooting

### Common Configuration Issues

1. **Module Not Loading**
   - Verify `AutoBackupModuleEnabled = true` in `[AutoBackupModule]` section
   - Check that OpenSim.Region.OptionalModules.dll is available
   - Review startup logs for configuration errors

2. **Directory Access Problems**
   - Ensure backup directory exists and is writable
   - Check file system permissions for OpenSimulator process
   - Verify disk space availability

3. **Timer Not Executing**
   - Confirm `AutoBackupInterval` is set to reasonable value (> 0)
   - Check that at least one region has `AutoBackup = true`
   - Verify module initialization completed successfully

### Common Runtime Issues

1. **Backup Files Not Created**
   - Check region readiness status in logs
   - Verify `IRegionArchiverModule` is available
   - Review file path generation logic for naming conflicts

2. **Old Files Not Cleaned Up**
   - Ensure `AutoBackupKeepFilesForDays > 0`
   - Verify at least one backup cycle has completed (`m_doneFirst = true`)
   - Check file creation time vs. current date calculation

3. **Post-Backup Scripts Not Executing**
   - Verify script file exists and is executable
   - Check script path configuration
   - Review process execution logs for error details

### Debug Configuration

```ini
[AutoBackupModule]
AutoBackupModuleEnabled = true
AutoBackupInterval = 5              ; Short interval for testing
AutoBackupDir = ./test-backups
AutoBackupKeepFilesForDays = 1      ; Short retention for testing
AutoBackup = true
AutoBackupNaming = Time             ; Easy to verify timing
; AutoBackupScript = ./debug-script.sh  ; Test script execution
```

### Log Analysis

Enable debug logging to trace module behavior:
```
[AUTO BACKUP]: AutoBackupModule enabled
[AUTO BACKUP]: Default config: ...
[AUTO BACKUP]: Config for [RegionName]: ...
[AUTO BACKUP]: Backing up region [RegionName]
```

## Future Enhancement Opportunities

### Advanced Scheduling
- Cron-like scheduling expressions for complex backup timing
- Per-region custom scheduling intervals
- Backup windows and blackout periods

### Enhanced File Management
- Compression options for backup files
- Remote storage integration (S3, FTP, etc.)
- Backup integrity verification and checksums

### Monitoring and Alerting
- Health check endpoints for backup status
- Email notifications for backup success/failure
- Metrics integration for backup monitoring

### Performance Optimizations
- Incremental backup support
- Parallel backup processing for multiple regions
- Background backup processing with lower priority

## Conclusion

The AutoBackupModule provides essential automated backup functionality for OpenSimulator deployments. Its comprehensive configuration system, multiple naming schemes, file retention management, and post-processing capabilities make it suitable for both development and production environments. The module's robust error handling and logging ensure reliable operation while maintaining security and performance standards.