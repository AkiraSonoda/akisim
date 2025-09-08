# GroupsServiceHGConnectorModule

## Overview

The `GroupsServiceHGConnectorModule` is a hypergrid-aware connector module that enables groups functionality across multiple grids in OpenSimulator. It acts as a bridge between local and remote groups services, allowing users from different grids to participate in groups that originate from other grids.

## Location

- **File**: `src/OpenSim.Addons.Groups/Hypergrid/GroupsServiceHGConnectorModule.cs`
- **Namespace**: `OpenSim.Groups`
- **Assembly**: `OpenSim.Addons.Groups.dll`

## Module Type

- **Interface**: `ISharedRegionModule`, `IGroupsServicesConnector`
- **Scope**: Shared across all regions in an OpenSim instance
- **Dependencies**: Groups Module V2, UserManagement, OfflineIM service

## Configuration

### Prerequisites

The module requires Groups Module V2 to be enabled and configured:

```ini
[Groups]
Enabled = true
Module = "Groups Module V2"
```

### Module Activation

To enable the GroupsServiceHGConnectorModule, configure the following in your OpenSim.ini:

```ini
[Groups]
Enabled = true
Module = "Groups Module V2"
ServicesConnectorModule = "Groups HG Service Connector"
LocalService = "local"              ; or "remote" for distributed setup
GroupsExternalURI = "http://127.0.0.1:8003"  ; URI for remote groups service
```

### Configuration Parameters

| Parameter | Description | Default | Required |
|-----------|-------------|---------|----------|
| `ServicesConnectorModule` | Must be set to "Groups HG Service Connector" | - | Yes |
| `LocalService` | "local" or "remote" service mode | "local" | Yes |
| `GroupsExternalURI` | External URI for groups service | "http://127.0.0.1" | Yes |

## Functionality

### Core Features

1. **Cross-Grid Group Management**: Enables users from different grids to join and participate in groups
2. **Local/Remote Group Handling**: Automatically routes group operations to appropriate local or remote services
3. **Access Token Management**: Manages authentication tokens for cross-grid group access
4. **Member Synchronization**: Keeps group memberships synchronized across grids
5. **Notice Distribution**: Distributes group notices to members across multiple grids

### Key Operations

#### Group Creation
- Only local grid users can create new groups
- Groups are created on the local grid's groups service

#### Cross-Grid Membership
- Local users can join remote groups with appropriate access tokens
- Remote users can join local groups with proxy creation on their home grid
- Membership tokens are managed automatically

#### Group Information Access
- Local groups: Direct access through local connector
- Remote groups: Cached access through network connectors with access tokens

#### Notice Distribution
- Local group notices are distributed to all participating grids
- Uses background threading to avoid blocking operations
- Maintains list of target grid URLs for efficient distribution

## Architecture

### Service Layers

```
┌─────────────────────────────────────┐
│    GroupsServiceHGConnectorModule   │
│         (Hypergrid Bridge)          │
├─────────────────────────────────────┤
│  Local Groups    │  Remote Groups   │
│   Connector      │   Connectors     │
├──────────────────┼──────────────────┤
│ GroupsService    │  Network Grid    │
│   (Local)        │   Services       │
└─────────────────────────────────────┘
```

### Internal Components

1. **Local Groups Connector**: Handles operations for groups that originate on this grid
2. **Network Connectors**: Manages connections to remote grids for cross-grid operations
3. **Cache Wrapper**: Provides caching for remote group information to improve performance
4. **Foreign Importer**: Handles importing user information from remote grids

## Methods

### Group Management

- `CreateGroup()`: Creates new groups (local users only)
- `UpdateGroup()`: Updates group information (origin grid only)
- `GetGroupRecord()`: Retrieves group information with caching for remote groups

### Membership Management

- `AddAgentToGroup()`: Handles both local and cross-grid membership additions
- `RemoveAgentFromGroup()`: Removes members from groups with cross-grid cleanup
- `GetAgentGroupMembership()`: Gets membership information for agents

### Role Management

- `AddGroupRole()`, `UpdateGroupRole()`, `RemoveGroupRole()`: Group role management
- `GetGroupRoles()`: Retrieves group roles with remote grid support
- `AddAgentToGroupRole()`, `RemoveAgentFromGroupRole()`: Member role assignments

### Notice System

- `AddGroupNotice()`: Creates and distributes notices across grids
- `GetGroupNotice()`, `GetGroupNotices()`: Retrieves group notices

## Cross-Grid Authentication

The module uses a token-based authentication system for cross-grid operations:

1. **Access Tokens**: Generated for group membership to authenticate remote operations
2. **User Universal Identifiers (UUI)**: Used to identify users across grids
3. **Service Location Tracking**: Maintains information about which grid a group originates from

## Threading and Performance

- **Background Operations**: Notice distribution uses `WorkManager.RunInThread()` to avoid blocking
- **Connection Pooling**: Network connectors are cached and reused for efficiency
- **Cached Remote Data**: Remote group information is cached to reduce network overhead

## Dependencies

### Required Services

- Groups Module V2 (`OpenSim.Groups.GroupsModule`)
- User Management Service (`IUserManagement`)
- Offline IM Service (`IOfflineIMService`)
- Message Transfer Module (`IMessageTransferModule`)

### Required Assemblies

- `OpenSim.Addons.Groups.dll`
- `OpenSim.Framework.dll`
- `OpenSim.Services.Interfaces.dll`

## Logging

The module provides extensive debug logging when `IsDebugEnabled` is true:

- Group operation debugging
- Cross-grid communication logging
- User authentication tracking
- Network connector management

## Error Handling

- Graceful fallback for unavailable remote services
- User-friendly error messages for unsupported operations
- Comprehensive exception handling for network operations

## Security Considerations

- Access tokens provide secure cross-grid authentication
- Operations restricted based on group origin and user permissions
- Remote user verification through User Management service

## Version History

The module has been updated to remove Mono.Addins dependency and uses reflection-based loading through the ModuleFactory system for .NET 8 compatibility.