# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **Akisim**, a fork/customization of OpenSimulator - an open-source virtual world server platform written in C# targeting .NET 8.0. The project enables creation of 3D virtual worlds compatible with Second Life viewers.

## Build System & Common Commands

### Prerequisites
- .NET 8.0 SDK
- libgdiplus (Linux/Mac)

### Building from Source
```bash
# Generate project files
./runprebuild.sh

# Build the solution
./compile.sh
# OR
dotnet build -c Release Akisim.sln

# Clean build artifacts
make clean

# Build and deploy to configured location
make deploy
```

### Makefile Targets
- `make build` - Build in Release configuration (default)
- `make clean` - Remove all build artifacts and project files
- `make rebuild` - Clean then build
- `make deploy` - Build and deploy to configured destination
- `make package` - Create versioned package

### Project Generation
The project uses Prebuild system (prebuild.xml) to generate Visual Studio/.NET project files:
- `./runprebuild.sh` - Generate .csproj files for current platform
- `./runprebuild.sh clean` - Remove generated project files

## Architecture Overview

### Core Components Structure

**OpenSim.Framework** - Core framework providing fundamental types, interfaces, and utilities

**OpenSim.Region.Application** - Main simulator application entry point (OpenSim.exe)
- `OpenSim.cs` - Primary application class
- `OpenSimBase.cs` - Base application functionality

**OpenSim.Server** - Robust server for grid services
- `ServerMain.cs` - Entry point for grid services (Robust.exe)

**OpenSim.Region.Framework** - Region/simulator framework and interfaces

**OpenSim.Region.CoreModules** - Essential simulator modules (avatar, assets, world, scripting)

**OpenSim.Services** - Grid service implementations (authentication, assets, inventory, etc.)

**OpenSim.Data** - Database abstraction layer supporting MySQL, PostgreSQL, SQLite

### Key Executables
- **OpenSim.exe** - Region/simulator server
- **Robust.exe** - Grid services server  

### Configuration System
- **OpenSim.ini** - Main configuration (copy from OpenSim.ini.example)
- **config-include/** - Modular configuration files
  - `StandaloneCommon.ini` - Standalone mode configuration
  - `GridCommon.ini` - Grid mode configuration

### Operating Modes
- **Standalone** - Single server with embedded services
- **Grid** - Distributed architecture with separate grid services

### Third-Party Dependencies
Located in `ThirdParty/`:
- **SmartThreadPool** - Thread pool implementation
- **ThreadedClasses** - Thread-safe collection classes

### Add-on Modules
- **addon-modules/** - External/community modules
  - `akkimoney_module/` - Currency system module with transaction handling
  - Module integration via prebuild.xml configuration

### Database Support
Multi-database support via `OpenSim.Data.*`:
- MySQL (production recommended)
- PostgreSQL  
- SQLite (development/testing)

### Testing Framework
- Comprehensive test suite using **NUnit** framework
- Test structure:
  - `OpenSim/Tests/Common/` - Base test classes and utilities (`OpenSimTestCase`, `TestHelpers`)
  - Component-specific test directories throughout modules
  - `OpenSim/Tests/Performance/` - NPCPerformanceTests, ObjectPerformanceTests, ScriptPerformanceTests
  - `OpenSim/Tests/Stress/` - VectorRenderModuleStressTests
  - `OpenSim/Tests/Permissions/` - DirectTransferTests, IndirectTransferTests
- Key test areas:
  - **Scene Framework**: SceneObjectTests, ScenePresenceTests, EntityManagerTests
  - **Physics**: BulletS physics engine tests, basic physics tests
  - **Scripting**: LSL API tests (LSL_ApiTest, LSL_TypesTest series)
  - **Networking**: UDP client stack tests, packet handling tests
  - **Data Layer**: Database-agnostic tests for MySQL, PostgreSQL, SQLite
  - **Services**: Asset, inventory, authentication service tests
  - **World Features**: Terrain, archiver, land management tests
- Test execution: `dotnet test` (requires built solution)
- Test configuration via `bin/OpenSim.ini.example` and `OpenSim/Data/Tests/Resources/TestDataConnections.ini`

### Script Execution Commands
- `./runprebuild.sh` - Generate project files (Linux/Mac)
- `./runprebuild.bat` - Generate project files (Windows)
- `./compile.sh` - Build solution (Linux/Mac)
- `./deploy.sh` - Deploy built binaries to configured location
- `./package.sh` - Create versioned package archive

## Development Workflow

1. **Project Generation**: Use `./runprebuild.sh` to generate project files after changes to prebuild.xml
2. **Build**: Use `./compile.sh`, `make build`, or `dotnet build -c Release Akisim.sln`
3. **Testing**: Run `dotnet test` to execute comprehensive test suite
4. **Configuration**: Test configurations in `bin/` directory with `.ini.example` files
5. **Deployment**: Use `make deploy` for deployment to configured environments
6. **Development Tools**:
   - `make clean` - Remove all build artifacts and generated project files  
   - `make rebuild` - Clean then build from scratch
   - `make package` - Create versioned distribution packages

### Important Development Notes
- The build system uses **Prebuild** to generate platform-specific project files
- All output goes to common `bin/` directory for easy testing
- Configuration is highly modular via `config-include/` system
- Physics engines are pluggable (BulletS, ubOde, BasicPhysics, POS)
- Script engines support both LSL (Linden Scripting Language) and YEngine
- Multi-database support requires appropriate connection string configuration