# Akisim

Akisim is a fork of [OpenSimulator](http://opensimulator.org/) — a BSD-licensed open source virtual world server platform implementing the Second Life protocol. It is written in C# targeting **.NET 8** and supports multiple clients and servers in a heterogeneous grid structure.

## Key Differences from Upstream OpenSim

| Area | Upstream OpenSim | Akisim |
|---|---|---|
| Module loading | Mono.Addins | Factory pattern (`OptionalModulesFactory`) |
| Image processing | System.Drawing (GDI+) | SkiaSharp |
| Voice | Vivox / FreeSwitch | WebRTC via Janus Gateway |
| Target framework | .NET 8 | .NET 8 |
| Grid services (Robust) | Included | Separate project in future ([goAki](https://bitbucket.org/AkiraSonoda/goaki/src/main/)) |

### Mono.Addins Removed

Akisim removes Mono.Addins entirely. Region modules are loaded via `OptionalModulesFactory.CreateOptionalSharedModules()` and Robust service modules via `ServerUtils.LoadPlugin<T>()`. No `[Extension]`, `[assembly: Addin]`, or `[assembly: AddinDependency]` attributes are used.

### WebRTC Voice (os-webrtc-janus)

Akisim includes the `os-webrtc-janus` addon as a built-in source module under `src/Opensim.Addons.os-webrtc-janus/`. It provides WebRTC-based spatial and non-spatial voice using [Janus WebRTC Gateway](https://janus.conf.meetecho.com/). See [`doc/WebRtcJanus.md`](doc/WebRtcJanus.md) for architecture and configuration details.

---

## Requirements

- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- On Linux/macOS: `libgdiplus`
  ```bash
  # Debian/Ubuntu
  apt-get install libgdiplus libc6-dev

  # Arch/CachyOS
  pacman -S libgdiplus
  ```

---

## Building

```bash
# Restore NuGet packages
make restore

# Build (Release)
make build

# or directly
dotnet build Akisim.sln --configuration Release

# Clean
make clean

# Rebuild (clean + build)
make rebuild
```

Build output goes to `bin/`.

---

## Running

TODO Create a better implementation 

On first start you will be prompted for region name, estate name, and owner credentials.

---

## Configuration

Main configuration files are in `bin/`:

| File | Purpose |
|---|---|
| `OpenSim.ini` | Primary region simulator configuration |
| `Regions/Regions.ini` | Region definitions |
| `config-include/` | Modular configuration includes |

### WebRTC Voice

Add to `OpenSim.ini`:

```ini
[Opensim.Addons.os-webrtc-janus.WebRtcVoice]
Enabled = true
WebRtcVoiceServerURI = http://grid-voice-service.example.com:8004
```

See [`doc/WebRtcJanus.md`](doc/WebRtcJanus.md) for the full setup including Docker-based Janus configuration.

---

## Project Structure

```
Akisim.sln
├── src/
│   ├── OpenSim.Framework/              # Core types, utilities
│   ├── OpenSim.Region.Framework/       # Scene management, module interfaces
│   ├── OpenSim.Region.CoreModules/     # Standard region modules
│   ├── OpenSim.Region.OptionalModules/ # Optional modules + factory
│   ├── OpenSim.Region.ClientStack.*/   # UDP and HTTP client protocol
│   ├── OpenSim.Region.ScriptEngine.*/  # LSL / YEngine scripting
│   ├── OpenSim.Region.PhysicsModules.*/# BulletS, ubODE physics
│   ├── OpenSim.Data.*/                 # Database layer (MySQL, SQLite, PostgreSQL)
│   ├── OpenSim.Services.*/             # Service connectors (client-side only)
│   └── Opensim.Addons.os-webrtc-janus/ # WebRTC voice addon
│       ├── WebRtcVoice/                # Shared interfaces & session management
│       ├── WebRtcVoiceRegionModule/    # Region-side caps module
│       ├── WebRtcVoiceServiceModule/   # Voice service dispatcher
│       └── WebRtcJanusService/         # Janus Gateway implementation
├── artifacts/                          # Libraries (not nuget), Configurations
├── bin/                                # Build output
├── doc/                                # Documentation
│   └── WebRtcJanus.md                  # WebRTC voice architecture & config
└── docker/                             # Docker support files
```

---

## Database Support

| Database | Usage |
|---|---|
| SQLite | Development / local (default) will be removed |
| MySQL | Production |
| PostgreSQL | Alternative production option |

Configure in `config-include/`.

---

## Connecting a Viewer

By default the region listens on port `9000`. Add the login URI to your viewer:

```
http://<host>:9000
```

For WebRTC voice, use a viewer with WebRTC voice support (e.g. Firestorm with WebRTC enabled).

---

## Upstream

Akisim tracks [OpenSimulator](http://opensimulator.org/) upstream. For questions about base OpenSim functionality see the [OpenSim wiki](http://opensimulator.org/wiki/Main_Page).
