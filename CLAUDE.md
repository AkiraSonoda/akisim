# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Akisim is a fork of OpenSimulator (OpenSim), a BSD-licensed virtual world server platform that implements the Second Life protocol. It's a .NET-based application written in C# that supports multiple clients and servers in a heterogeneous grid structure.

## Build Commands

### Prerequisites
- .NET 8.0 SDK
- On Linux/Mac: libgdiplus (`apt-get install libgdiplus` on Debian/Ubuntu)

### Primary Build Commands
```bash
# Build the solution (uses NUKE build system)
make build
# or directly:
nuke

# Clean build artifacts
make clean
# or:
nuke clean

# Deploy to configured location
make deploy

# Package for distribution
make package

# Rebuild (clean + build)
make rebuild
```

### Build Notes
- The project uses `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` in `Directory.Build.props` to avoid conflicts between auto-generated and manual assembly attributes
- Build system copies required dependencies from NuGet packages to the bin directory automatically
- Parallel builds are disabled (`DisableParallel=true`) for build reliability
- Mono.Addins configuration files use version `0.9.3.1` to match the actual assembly version in `VersionInfo.cs`
- MySQL support requires `MySqlConnector.dll` (from NuGet package `mysqlconnector`) in the bin directory

### Alternative Build Methods
```bash
# Using dotnet CLI directly
dotnet build --configuration Release Akisim.sln

# Build specific project
dotnet build src/OpenSim.Framework/OpenSim.Framework.csproj
```

### Running the Application
```bash
# From the bin directory
cd bin
./opensim.sh        # Linux/Mac
# or
OpenSim.exe         # Windows
```

## Architecture Overview

### Core Components

**Main Executables:**
- `OpenSim.exe` - Primary region simulator server
- `Robust.exe` - Services backend server (distributed architecture)
- `OpenSim.ConsoleClient.exe` - Remote administration console

**Key Architectural Layers:**
1. **Foundation Layer** - Core utilities and data structures (`OpenSim.Framework`)
2. **Data Layer** - Database abstraction (`OpenSim.Data.*`) with MySQL/SQLite/PostgreSQL support
3. **Services Layer** - Backend services for assets, inventory, users (`OpenSim.Services.*`)
4. **Region Layer** - World simulation and scene management (`OpenSim.Region.*`)
5. **Client Stack** - Protocol implementation (`OpenSim.Region.ClientStack.*`)

### Scene Management
- **Scene** class - Central world state manager in `OpenSim.Region.Framework.Scenes`
- **SceneObjectGroup/SceneObjectPart** - Represents 3D objects (prims) in the world
- **ScenePresence** - Represents avatars/users in the scene
- Uses event-driven architecture with extensive C# events

### Module System
- Plugin-based architecture using Mono.Addins
- **IRegionModuleBase** - Base interface for region functionality
- **ISharedRegionModule** - Modules shared across regions
- Modules are discovered and loaded dynamically from assemblies

### Physics Engines (Pluggable)
- **BasicPhysics** - Simple physics for basic operations
- **BulletS** - Advanced Bullet physics engine
- **ubOde** - ODE physics engine integration
- **POS** - Position-based physics

### Scripting Engine
- **YEngine** - Primary LSL (Linden Scripting Language) implementation
- **LSL_Api** - Core scripting API in `OpenSim.Region.ScriptEngine.Shared.Api`
- **OSSL_Api** - OpenSim-specific scripting extensions
- Scripts compiled to .NET bytecode for performance

## Development Patterns

### Adding New Region Modules
1. Implement `IRegionModuleBase` or `ISharedRegionModule`
2. Use `[Extension(Path = "/OpenSim/RegionModules")]` attribute
3. Handle `Initialize()`, `AddRegion()`, `RegionLoaded()` lifecycle events
4. Place in appropriate project under `OpenSim.Region.*`

### Database Integration
1. Define interfaces in `OpenSim.Data` (e.g., `IAssetData`)
2. Implement for specific databases in `OpenSim.Data.MySQL`, etc.
3. Use Migration classes for schema versioning
4. Follow existing patterns for connection management

### Service Development
1. Define service contracts in `OpenSim.Services.Interfaces`
2. Implement services in `OpenSim.Services.*`
3. Create connectors in `OpenSim.Services.Connectors` for distributed access
4. Register in dependency injection container

### Client Protocol Extensions
1. Extend `IClientAPI` interface for new functionality
2. Implement in `LLClientView` for UDP protocol support
3. Add capabilities in `OpenSim.Region.ClientStack.LindenCaps` for HTTP-based features
4. Handle both inbound and outbound protocol messages

## Configuration Structure

### Main Configuration Files (in `bin/`)
- `OpenSim.ini` - Primary simulator configuration
- `Regions/Regions.ini` - Region definitions
- `config-include/` - Modular configuration includes
  - `Standalone.ini` vs `Grid.ini` - Deployment mode selection
  - `StandaloneCommon.ini` / `GridCommon.ini` - Common settings

### Development vs Production
- **Standalone Mode** - Single-server development setup
- **Grid Mode** - Distributed production architecture with separate Robust services
- Database selection via configuration (SQLite for dev, MySQL for production)

## Testing

### Available Test Projects
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test src/OpenSim.Tests/OpenSim.Tests.csproj
```

### Test Categories
- Unit tests for core framework components
- Integration tests for service interactions
- Performance tests for critical paths
- Bot framework (`pCampBot`) for load testing

## Key Files for Common Tasks

**Region/World Development:**
- `src/OpenSim.Region.Framework/Scenes/Scene.cs` - Core world logic
- `src/OpenSim.Region.CoreModules/` - Standard region modules

**Database Schema:**
- `OpenSim/Data/*/Resources/` - SQL migration scripts
- Follow naming pattern: `XXX_StoreName.sql` where XXX is version number

**Protocol Implementation:**
- `src/OpenSim.Region.ClientStack.LindenUDP/LLClientView.cs` - Main client handler
- `src/OpenSim.Region.ClientStack.LindenCaps/` - HTTP capabilities

**Scripting Extensions:**
- `src/OpenSim.Region.ScriptEngine.Shared.Api/LSL_Api.cs` - Core LSL functions
- `src/OpenSim.Region.ScriptEngine.Shared.Api/OSSL_Api.cs` - OpenSim extensions

## Currency Module Integration

The project includes currency module extensions in `src/OpenSim.Region.OptionalModules.Currency/`:
- **DTLNSLMoneyModule** - Distributed Transaction Layer money module
- **NSLXmlRpc** - XML-RPC communication for currency operations  
- **MySQL.MoneyData** - Database layer for currency transactions
- PHP helper scripts in `helper/` directory for web integration

## Performance Considerations

- Uses **SmartThreadPool** for efficient thread management
- Implements object pooling for frequently allocated objects (packets, etc.)
- Event-driven architecture minimizes blocking operations
- Database connection pooling and prepared statements
- Asset caching with configurable cache sizes
- Physics and scripting run on separate threads from main simulation loop