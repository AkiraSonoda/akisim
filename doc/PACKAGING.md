# Packaging Guide

## Available Package Types

### 1. Source Package (`make package`)
Creates a source distribution with all files needed to build Akisim.

**Contents:**
- Complete source code
- Project files (*.csproj)
- Build scripts (compile.sh, Makefile, etc.)
- Configuration examples
- bin/ directory with all runtime dependencies
- Documentation

**Excludes:**
- Build artifacts (obj/, bin/Release/, bin/Debug/)
- Publish directories
- IDE user files (*.user, *.suo)
- Hidden files

**Output:**
- `akisim-X.X.X.zip` (~49MB)
- Versioned automatically (increments patch number)
- Located in: `~/opensim/packaging/`

**Usage:**
```bash
make package
```

### 2. Binary Package (`make package-binary`)
Creates a ready-to-run binary distribution identical to `make deploy`.

**Contents:**
- bin/ directory with all executables and DLLs
- Complete runtime environment
- All configuration files (including delta overrides)
- Asset libraries (lib64/, openmetaverse_data/)
- Config directories (config-include/, Regions/, etc.)

**Total files:** 913 files, 132 DLLs

**Critical components included:**
- ✓ OpenSim.dll (main executable)
- ✓ C5.dll, CSJ2K.dll, DotNetOpenId.dll, Ionic.Zip.dll
- ✓ LukeSkywalker.IPNetwork.dll
- ✓ System.Configuration.ConfigurationManager.dll
- ✓ All OpenTelemetry DLLs (6 files)
- ✓ Configuration files with binding redirects
- ✓ Delta overrides applied

**Output:**
- `akisim-bin-X.X.X.zip` (~42MB)
- Versioned automatically (separate from source packages)
- Located in: `~/opensim/packaging/`

**Usage:**
```bash
make package-binary
```

**Deployment:**
Just extract the zip and the bin/ directory is ready to run:
```bash
unzip akisim-bin-X.X.X.zip
cd akisim-bin-X.X.X/bin
dotnet OpenSim.dll
```

## Package Comparison

| Feature | Source Package | Binary Package |
|---------|---------------|----------------|
| Size | ~49MB | ~42MB |
| Purpose | Build from source | Ready to run |
| Contains source | ✓ | ✗ |
| Contains binaries | ✓ (in bin/) | ✓ |
| Build required | ✓ | ✗ |
| .csproj files | ✓ | ✗ |
| obj/ directories | ✗ | ✗ |
| Delta overrides | ✗ | ✓ (applied) |
| Use case | Distribution, development | Deployment, production |

## Versioning

Both package types maintain independent version numbers:
- Source: `akisim-X.X.X.zip`
- Binary: `akisim-bin-X.X.X.zip`

Versions auto-increment based on the latest package in `~/opensim/packaging/`.

## Location

All packages are stored in:
```
~/opensim/packaging/
  ├── akisim-3.1.6/          (source)
  ├── akisim-3.1.6.zip       (source)
  ├── akisim-bin-0.1.0/      (binary)
  └── akisim-bin-0.1.0.zip   (binary)
```

## Notes

- Binary packages include OpenTelemetry instrumentation configured for Grafana Cloud
- All necessary DLL dependencies are included and tested
- Configuration files include binding redirects for version conflicts
- Delta overrides are pre-applied in binary packages
