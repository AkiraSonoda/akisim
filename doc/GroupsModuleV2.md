# Groups Module V2

## Overview

The **Groups Module V2** is the primary groups functionality implementation for OpenSimulator. It provides comprehensive group management features including group creation, membership management, role-based permissions, group notices, invitations, and integration with the viewer's groups interface. This module serves as the main entry point for all groups-related operations in a region.

## Location

- **File**: `src/OpenSim.Addons.Groups/GroupsModule.cs`
- **Namespace**: `OpenSim.Groups`
- **Assembly**: `OpenSim.Addons.Groups.dll`

## Module Type

- **Interface**: `ISharedRegionModule`, `IGroupsModule`
- **Scope**: Shared across all regions in an OpenSim instance
- **Loading**: Loaded via ModuleFactory (no Mono.Addins dependency)
- **Dependencies**: IGroupsServicesConnector, IMessageTransferModule, IUserManagement

## Configuration

### Basic Configuration

To enable Groups Module V2, configure the following in your `OpenSim.ini`:

```ini
[Groups]
Enabled = true
Module = "Groups Module V2"
NoticesEnabled = true
DebugEnabled = false
LevelGroupCreate = 0
```

### Configuration Parameters

| Parameter | Description | Default | Required |
|-----------|-------------|---------|----------|
| `Enabled` | Enable/disable groups functionality | false | Yes |
| `Module` | Must be set to "Groups Module V2" | - | Yes |
| `NoticesEnabled` | Enable/disable group notices | true | No |
| `DebugEnabled` | Enable verbose debug logging | false | No |
| `LevelGroupCreate` | Minimum user level to create groups | 0 | No |

### Additional Requirements

Groups Module V2 requires a groups services connector to be configured. Common options include:

```ini
[Groups]
ServicesConnectorModule = "Groups HG Service Connector"  ; recommended for all setups
; or  
ServicesConnectorModule = "Groups Remote Service Connector"  ; for distributed setups
```

## Core Features

### 1. Group Management
- **Group Creation**: Users can create new groups with configurable membership fees
- **Group Information**: View and update group profiles, charters, and settings
- **Group Search**: Find groups by name or other criteria
- **Membership Fees**: Optional payment system integration for group creation and joining

### 2. Membership Management
- **Join/Leave Groups**: Users can join open groups or leave groups they belong to
- **Invitations**: Send and receive group invitations with role assignments
- **Member Ejection**: Group officers can eject members from groups
- **Membership Lists**: View all members of groups with appropriate permissions

### 3. Role-Based Permissions
- **Role Creation/Management**: Create, modify, and delete group roles
- **Permission Assignment**: Assign specific powers to roles (e.g., invite members, send notices)
- **Role Membership**: Assign users to multiple roles within a group
- **Active Role Selection**: Users can choose which role title to display

### 4. Group Notices
- **Notice Creation**: Send notices to all group members
- **Notice Attachments**: Attach inventory items to group notices
- **Notice History**: View past group notices
- **Notice Acceptance/Decline**: Handle attached inventory items

### 5. Communication Integration
- **Instant Messaging**: Integration with IM system for group communications
- **Group Chat**: Support for group messaging sessions
- **Offline Delivery**: Queue messages for offline members

## Architecture

### Service Dependencies

```
┌─────────────────────────────────────┐
│        Groups Module V2             │
│     (IGroupsModule Interface)       │
├─────────────────────────────────────┤
│  ┌─────────────────┐ ┌─────────────┐│
│  │ Groups Services │ │  Message    ││
│  │   Connector     │ │ Transfer    ││
│  │                 │ │  Module     ││
│  └─────────────────┘ └─────────────┘│
├─────────────────────────────────────┤
│  ┌─────────────────┐ ┌─────────────┐│
│  │ User Management │ │   Money     ││
│  │     Module      │ │  Module     ││
│  │                 │ │ (Optional)  ││
│  └─────────────────┘ └─────────────┘│
└─────────────────────────────────────┘
```

### Event Handling

The module registers for several scene events:
- **OnNewClient**: Set up client event handlers
- **OnMakeRootAgent**: Configure root agent for groups functionality
- **OnMakeChildAgent**: Clean up child agent handlers
- **OnIncomingInstantMessage**: Process group-related instant messages
- **OnClientClosed**: Clean up client resources

## Key Methods and Functionality

### Group Operations

#### CreateGroup()
Creates a new group with the specified parameters:
- Validates group name uniqueness
- Checks user level permissions (`LevelGroupCreate`)
- Processes membership fee through money module
- Registers group in services backend

#### JoinGroupRequest() / LeaveGroupRequest()
Handles group membership changes:
- Validates membership fees for joining
- Processes payments through money module
- Updates member's group data and UI
- Sends appropriate notifications

### Role Management

#### GroupRoleUpdate()
Manages group roles:
- **Create**: Add new roles with specified permissions
- **Delete**: Remove existing roles
- **Update**: Modify role properties and powers
- Validates permissions through services layer

#### GroupRoleChanges()
Handles role membership:
- Add/remove users from specific roles
- Updates member permissions and titles
- Refreshes client UI with new data

### Notice System

#### OnInstantMessage() - Group Notices
Processes group notice creation:
- Validates sender permissions (`GroupPowers.SendNotices`)
- Handles inventory attachments with permission checks
- Distributes notices to all accepting members
- Creates appropriate instant messages for delivery

#### GroupNoticeRequest()
Handles individual notice retrieval:
- Creates instant message with notice content
- Includes attachment information if present
- Delivers through message transfer system

### Communication Features

#### Group Invitations
- **InviteGroup()**: Send group invitations with role assignments
- **OnInstantMessage()**: Process invitation responses (accept/decline)
- Handles cross-grid invitations for hypergrid setups

#### Member Management
- **EjectGroupMember()**: Remove members from groups
- Sends notification messages to ejected users
- Updates group membership across all regions

## User Interface Integration

### Client Updates

The module provides several methods to keep the viewer synchronized:

#### SendAgentGroupDataUpdate()
- Updates the viewer's group membership information
- Refreshes group powers and active group status
- Updates group title display in the viewer

#### SendDataUpdate()
- Sends agent data updates including active group
- Updates scene presence with current group title
- Maintains consistency across all regions

### Debug Commands

The module provides console commands for troubleshooting:

```
debug groups verbose <true|false>
```

Enables detailed logging of all groups operations for debugging purposes.

## Permission System

### Group Powers

Groups Module V2 implements a comprehensive permission system based on `GroupPowers` enumeration:

- **SendNotices**: Send group notices to members
- **InviteMember**: Invite new members to the group
- **EjectMember**: Remove members from the group
- **ChangeOptions**: Modify group settings and charter
- **CreateRole**: Create new group roles
- **DeleteRole**: Delete existing group roles
- **RoleProperties**: Modify role properties and permissions
- **AssignMember**: Assign members to roles
- **RemoveMember**: Remove members from roles
- **VoteOnProposal**: Participate in group voting
- **OwnerPowers**: Full ownership powers

### Permission Validation

All group operations include server-side permission validation through the groups services layer, ensuring security and preventing unauthorized actions.

## Error Handling and Logging

### Logging Levels

- **Info**: Basic operational messages
- **Debug**: Detailed operation tracking (when `DebugEnabled = true`)
- **Warn**: Non-critical issues and warnings
- **Error**: Critical failures and exceptions

### Common Error Scenarios

- **Missing Groups Service Connector**: Module disables itself if no connector is available
- **Permission Denied**: Operations fail with appropriate user notifications
- **Network Failures**: Graceful handling of remote service unavailability
- **Invalid Data**: Validation prevents processing of malformed requests

## Money Integration

When a money module is present, Groups Module V2 integrates payment processing:

- **Group Creation Fees**: Charges users for creating new groups
- **Membership Fees**: Processes payments for joining groups with fees
- **Transaction Types**: Uses `MoneyTransactionType.GroupCreate` and `MoneyTransactionType.GroupJoin`

## Cross-Grid Compatibility

Groups Module V2 is designed to work with hypergrid setups:

- Works with `GroupsServiceHGConnectorModule` for cross-grid functionality
- Handles foreign user identification through UUIDs
- Supports cross-grid group memberships and communications

## Performance Considerations

- **Caching**: Relies on groups services for caching remote data
- **Asynchronous Operations**: Notice distribution uses background threads
- **Scene Management**: Efficient handling of multiple regions
- **Client Updates**: Batched updates to minimize network traffic

## Troubleshooting

### Common Issues

1. **Groups Not Working**: Verify `[Groups] Enabled = true` and `Module = "Groups Module V2"`
2. **Cannot Create Groups**: Check `LevelGroupCreate` setting and user permissions
3. **Missing Notices**: Verify `NoticesEnabled = true` and message transfer module
4. **Cross-Grid Issues**: Ensure hypergrid connector is properly configured

### Debug Mode

Enable debug logging for detailed troubleshooting:

```ini
[Groups]
DebugEnabled = true
```

Or use the console command:
```
debug groups verbose true
```

## Version History

This module has been updated to remove Mono.Addins dependency and uses reflection-based loading through the ModuleFactory system for .NET 8 compatibility.