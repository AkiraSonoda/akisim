# WebRTC Voice Addon (os-webrtc-janus)

## Übersicht

Das `Opensim.Addons.os-webrtc-janus`-Addon implementiert WebRTC-basierte Sprachkommunikation für Akisim-Regionen unter Verwendung des [Janus WebRTC Gateway](https://janus.conf.meetecho.com/). Es ersetzt das klassische Vivox/Freeswitch-basierte Sprachsystem durch einen modernen WebRTC-Stack, der direkt im Viewer (z.B. Firestorm mit WebRTC-Support) und im Browser genutzt werden kann.

Das Addon besteht aus vier Projekten mit klarer Schichtentrennung zwischen Region-Server und Robust-Server.

## Projektstruktur

```
src/Opensim.Addons.os-webrtc-janus/
├── Opensim.Addons.os-webrtc-janus.WebRtcVoice/               # Gemeinsame Interfaces & Basisklassen
├── Opensim.Addons.os-webrtc-janus.WebRtcVoiceRegionModule/   # Region-seitiges Modul (Caps)
├── Opensim.Addons.os-webrtc-janus.WebRtcVoiceServiceModule/  # Robust-seitiger Service
└── Opensim.Addons.os-webrtc-janus.WebRtcJanusService/        # Janus-spezifische Implementierung
```

## Architektur

```
Viewer (WebRTC)
     │  HTTP Caps (LLSD/XML)
     ▼
WebRtcVoiceRegionModule          ← Region-Server
  (ISharedRegionModule)
  Caps: ProvisionVoiceAccountRequest
        VoiceSignalingRequest
        ChatSessionRequest
     │  JSON-RPC über HTTP
     ▼
WebRtcVoiceServerConnector       ← Robust-Server
  (IServiceConnector)
  Endpunkte: provision_voice_account_request
             voice_signaling_request
     │  ServerUtils.LoadPlugin
     ▼
WebRtcVoiceServiceModule         ← Robust-Server (Service-Modul)
  (ISharedRegionModule + IWebRtcVoiceService)
  Verteilt auf spatial / non-spatial Voice-Service
     │  ServerUtils.LoadPlugin
     ▼
WebRtcJanusService               ← Janus-Implementierung
  (IWebRtcVoiceService)
  Kommuniziert mit Janus WebRTC Gateway
     │  WebSocket / HTTP
     ▼
Janus WebRTC Gateway
```

## Komponenten

### 1. WebRtcVoice (Gemeinsame Bibliothek)

Enthält alle gemeinsam genutzten Interfaces und Basisklassen:

**`IWebRtcVoiceService`** — Haupt-Interface für Voice-Dienste:
```csharp
OSDMap ProvisionVoiceAccountRequest(OSDMap request, UUID userID, UUID sceneID);
OSDMap VoiceSignalingRequest(OSDMap request, UUID userID, UUID sceneID);
OSDMap ProvisionVoiceAccountRequest(IVoiceViewerSession session, OSDMap request, UUID userID, UUID sceneID);
OSDMap VoiceSignalingRequest(IVoiceViewerSession session, OSDMap request, UUID userID, UUID sceneID);
IVoiceViewerSession CreateViewerSession(OSDMap request, UUID userID, UUID sceneID);
```

**`IVoiceViewerSession`** — Repräsentiert eine aktive Viewer-Verbindung:
```csharp
string ViewerSessionID { get; set; }       // ID die mit dem Viewer ausgetauscht wird
string VoiceServiceSessionId { get; set; } // ID gegenüber dem Janus-Dienst
UUID RegionId { get; set; }
UUID AgentId { get; set; }
Task Shutdown();
```

**`VoiceViewerSession`** — Implementierung mit statischem Session-Store:
- Thread-sicheres Dictionary aller aktiven Viewer-Sessions (simulator-weit)
- Lookup nach `ViewerSessionID`, `AgentId` oder `VoiceServiceSessionId`
- Session-ID-Synchronisation zwischen Viewer und Janus-Dienst via `UpdateViewerSessionId()`

**`WebRtcVoiceServiceConnector`** — Region-seitiger RPC-Client:
- Implementiert `IWebRtcVoiceService` auf der Regionsseite
- Leitet Anfragen per JSON-RPC an den Robust-Server weiter (`WebUtil.PostToService`)
- Synchronisiert `ViewerSessionID` nach der ersten `ProvisionVoiceAccountRequest`-Antwort

### 2. WebRtcVoiceRegionModule

**Typ:** `ISharedRegionModule` — läuft im Region-Server
**Ladung:** Über `OptionalModulesFactory` (noch ausstehend, derzeit Mono.Addins)

Registriert drei HTTP-Capabilities für jeden Viewer-Login:

| Capability | Funktion |
|---|---|
| `ProvisionVoiceAccountRequest` | Verbindungsaufbau, SDP-Offer/Answer |
| `VoiceSignalingRequest` | ICE-Kandidaten-Trickle |
| `ChatSessionRequest` | P2P-Sprach-/Text-Sessions |

**Sicherheitsprüfungen in `ProvisionVoiceAccountRequest`:**
- Estate-Einstellung `AllowVoice` muss aktiv sein
- Parcel-Flag `AllowVoiceChat` wird geprüft
- Bann/Einschränkungen auf dem Parcel werden berücksichtigt
- Parcel mit `UseEstateVoiceChan` verwendet den Estate-Kanal

**SimulatorFeature:** Setzt `VoiceServerType = "webrtc"` damit Viewer WebRTC aktivieren.

### 3. WebRtcVoiceServiceModule

**Typ:** `ISharedRegionModule` + `IWebRtcVoiceService` — läuft im Robust-Server
**Ladung:** Über `ServerUtils.LoadPlugin<IWebRtcVoiceService>` durch `WebRtcVoiceServerConnector`

Verteilt eingehende Voice-Anfragen auf zwei konfigurierbare Voice-Dienste:

- **Spatial Voice** (`channel_type = "local"`) → z.B. für räumliche Sprache pro Region
- **Non-Spatial Voice** (alle anderen) → z.B. für Gruppen-Chat

Beide Dienste werden ihrerseits via `ServerUtils.LoadPlugin<IWebRtcVoiceService>` geladen.

> **Hinweis zur Mono.Addins-Migration:** Die Attribute `[assembly: Addin]`, `[assembly: AddinDependency]` und `[Extension]` wurden entfernt. Das Modul wird nicht über die Akisim-Factory registriert, sondern direkt vom `WebRtcVoiceServerConnector` per `ServerUtils.LoadPlugin` instantiiert — das ist das korrekte Muster für Robust-Service-Module.

### 4. WebRtcJanusService

**Typ:** `IWebRtcVoiceService` — Janus-spezifische Implementierung
**Ladung:** Über `ServerUtils.LoadPlugin<IWebRtcVoiceService>` durch `WebRtcVoiceServiceModule`

Kommuniziert mit dem Janus WebRTC Gateway über dessen REST/WebSocket-API:

- `JanusSession` — Verwaltung einer Janus-Server-Session
- `JanusPlugin` — Verbindung zum `janus.plugin.audiobridge`-Plugin
- `JanusRoom` — Verwaltung eines Audio-Raums (pro Region/Parcel)
- `JanusAudioBridge` — Hauptklasse für die Audio-Bridge-Verwaltung
- `JanusViewerSession` — Verknüpft `IVoiceViewerSession` mit Janus-Session
- `JanusMessages` — OSD-basierte Nachrichtentypen für die Janus-API
- `BHasher` — Hilfsfunktion für konsistente Room-ID-Generierung

## Konfiguration

### Region-Server (`OpenSim.ini`)

```ini
[Opensim.Addons.os-webrtc-janus.WebRtcVoice]
Enabled = true

; URI des Robust-Servers der den Voice-Service bereitstellt
WebRtcVoiceServerURI = http://robust.example.com:8004

; Detailliertes Message-Logging (nur für Debugging)
MessageDetails = false
```

### Robust-Server (`Robust.ini`)

```ini
[Opensim.Addons.os-webrtc-janus.WebRtcVoice]
Enabled = true

; Welche DLL+Klasse das eigentliche Service-Modul enthält
LocalServiceModule = Opensim.Addons.os-webrtc-janus.WebRtcVoiceServiceModule.dll:osWebRtcVoice.WebRtcVoiceServiceModule

; Spatial Voice (räumlich, pro Region)
SpatialVoiceService = Opensim.Addons.os-webrtc-janus.WebRtcJanusService.dll:WebRtcJanusService.WebRtcJanusService

; Non-Spatial Voice (Gruppen-Chat etc.), Standard = SpatialVoiceService
; NonSpatialVoiceService = ...

; Detailliertes Message-Logging
MessageDetails = false
```

### Janus Gateway (Akisim-Konfigurationsabschnitt)

```ini
[Opensim.Addons.os-webrtc-janus.WebRtcVoice]
JanusServerURI = http://janus.example.com:8088/janus
JanusApiSecret  = geheimesToken
```

## Janus mit Docker

### docker-compose.yml

```yaml
services:
  janus:
    image: meetecho/janus-gateway
    restart: unless-stopped
    ports:
      - "8088:8088"           # HTTP REST API  (→ JanusServerURI)
      - "8089:8089"           # HTTPS REST API
      - "8188:8188"           # WebSocket
      - "8989:8989"           # Secure WebSocket
      - "20000-20100:20000-20100/udp"  # RTP/SRTP Media-Ports
    volumes:
      - ./janus/janus.jcfg:/usr/local/etc/janus/janus.jcfg
      - ./janus/janus.plugin.audiobridge.jcfg:/usr/local/etc/janus/janus.plugin.audiobridge.jcfg
    environment:
      - JANUS_LOG_LEVEL=4
```

### janus/janus.jcfg

Minimale Hauptkonfiguration:

```
general: {
    configs_folder = "/usr/local/etc/janus"
    plugins_folder = "/usr/local/lib/janus/plugins"
    transports_folder = "/usr/local/lib/janus/transports"

    api_secret = "geheimesToken"   # muss mit JanusApiSecret übereinstimmen
    admin_secret = "adminToken"

    # Logging
    log_to_stdout = true
    debug_level = 4
}

media: {
    # RTP-Port-Bereich muss mit docker-compose ports übereinstimmen
    rtp_port_range = "20000-20100"
}

nat: {
    # --- Localhost/internes Netz (kein NAT) ---
    # Nichts nötig.

    # --- Hinter NAT (z.B. Cloud-VM mit öffentlicher IP) ---
    # nat_1_1_mapping = "1.2.3.4"   # öffentliche IP des Hosts

    # --- STUN (für Viewer hinter NAT) ---
    stun_server = "stun.l.google.com"
    stun_port = 19302

    # --- TURN (wenn STUN nicht reicht) ---
    # turn_server = "turn.example.com"
    # turn_port = 3478
    # turn_type = "udp"
    # turn_user = "user"
    # turn_pwd = "password"
}

plugins: {
    # Nur audiobridge laden, Rest deaktivieren
    disable = "libjanus_videoroom.so,libjanus_videocall.so,libjanus_echotest.so,libjanus_recordplay.so,libjanus_sip.so,libjanus_textroom.so,libjanus_nosip.so"
}

transports: {
    disable = "libjanus_pfunix.so"
}
```

### janus/janus.plugin.audiobridge.jcfg

```
general: {
    # Standardmäßig leer — Räume werden dynamisch von Akisim erstellt
    # admin_key = "adminKey"   # optional: schützt Room-Erstellung
}
```

### NAT-Szenarien

| Szenario | Konfiguration |
|---|---|
| Alles lokal (Dev) | Nichts — Standard reicht |
| Cloud-VM (öffentliche IP) | `nat_1_1_mapping = "öffentliche.IP"` |
| Viewer hinter NAT | `stun_server` setzen |
| Strenger NAT / Firewall | TURN-Server zusätzlich konfigurieren |

> **Wichtigste Falle:** Die UDP-Ports `20000-20100` müssen in der Firewall/Security-Group des Hosts offen sein — sonst scheitert der ICE-Handshake lautlos und es kommt kein Audio.

## Ladekette beim Start

### Region-Server

1. `RegionModulesController.LoadCoreModulesFromFactory()` lädt `WebRtcVoiceRegionModule` (sobald in Factory eingetragen)
2. `Initialise()` liest Konfiguration, erstellt `WebRtcVoiceServiceConnector` als lokalen `IWebRtcVoiceService`
3. `RegionLoaded()` registriert Caps und setzt `VoiceServerType = "webrtc"`

### Robust-Server

1. `WebRtcVoiceServerConnector` (als `IServiceConnector`) wird vom Robust-Server geladen
2. Er ruft `ServerUtils.LoadPlugin<IWebRtcVoiceService>("...WebRtcVoiceServiceModule.dll:...")` auf
3. Das geladene Modul wird als `ISharedRegionModule` gecastet und `Initialise()` aufgerufen
4. `WebRtcVoiceServiceModule.Initialise()` lädt seinerseits `WebRtcJanusService` per `ServerUtils.LoadPlugin`
5. JSON-RPC-Handler werden am HTTP-Server registriert

## Request-Flow (Viewer → Janus)

```
1. Viewer sendet POST /caps/ProvisionVoiceAccountRequest  (LLSD/XML)
2. WebRtcVoiceRegionModule.ProvisionVoiceAccountRequest()
   → Sicherheitsprüfungen (Estate, Parcel)
   → voiceService.ProvisionVoiceAccountRequest(map, agentID, sceneID)
3. WebRtcVoiceServiceConnector.ProvisionVoiceAccountRequest()
   → CreateViewerSession() → neue VoiceViewerSession
   → VoiceViewerSession.AddViewerSession()
   → JsonRpcRequest("provision_voice_account_request", robustURI, ...)
4. WebRtcVoiceServerConnector.Handle_ProvisionVoiceAccountRequest()
   → m_WebRtcVoiceService.ProvisionVoiceAccountRequest(request, userID, sceneID)
5. WebRtcVoiceServiceModule.ProvisionVoiceAccountRequest()
   → channel_type == "local" → m_spatialVoiceService.CreateViewerSession()
   → vSession.VoiceService.ProvisionVoiceAccountRequest(vSession, ...)
6. WebRtcJanusService → Janus API (WebSocket/HTTP)
   → JanusRoom erstellen/beitreten
   → SDP-Answer zurückgeben
7. Antwort rückwärts durch alle Schichten zum Viewer
```

## Session-ID-Synchronisation

Ein Besonderheit der Implementierung: Die `ViewerSessionID` die initial vom `WebRtcVoiceServiceConnector` erzeugt wird (zufällige UUID), wird nach der ersten `ProvisionVoiceAccountRequest`-Antwort mit der Session-ID des Janus-Diensts synchronisiert:

```csharp
// WebRtcVoiceServiceConnector.ProvisionVoiceAccountRequest()
if (resp.TryGetString("viewer_session", out string otherViewerSessionId))
    VoiceViewerSession.UpdateViewerSessionId(pVSession, otherViewerSessionId);
```

Danach verwenden Viewer und Janus-Dienst dieselbe Session-ID für alle Folge-Requests (`VoiceSignalingRequest`).

## Abhängigkeiten

| Projekt | Abhängigkeiten |
|---|---|
| `WebRtcVoice` | OpenMetaverse, OpenSim.Framework, OpenSim.Server.Handlers |
| `WebRtcVoiceRegionModule` | WebRtcVoice, OpenSim.Region.Framework |
| `WebRtcVoiceServiceModule` | WebRtcVoice, OpenSim.Server.Base |
| `WebRtcJanusService` | WebRtcVoice, OpenSim.Region.Framework |

Alle Projekte targetten `.NET 8.0`. Externe DLL-Referenzen (OpenMetaverse) werden über `../../../bin/` bezogen.

## Build

```bash
# Alle WebRTC-Projekte bauen
dotnet build src/Opensim.Addons.os-webrtc-janus/Opensim.Addons.os-webrtc-janus.WebRtcJanusService/Opensim.Addons.os-webrtc-janus.WebRtcJanusService.csproj --configuration Debug

# Oder über die Solution
dotnet build Akisim.sln --configuration Debug
```

## Mono.Addins-Migration (Akisim-spezifisch)

Im Gegensatz zum ursprünglichen OpenSim-Code verwendet Akisim kein Mono.Addins. Die Attribute wurden wie folgt entfernt:

| Attribut | Status |
|---|---|
| `[assembly: Addin(...)]` | Entfernt |
| `[assembly: AddinDependency(...)]` | Entfernt |
| `[Extension(Path = "/OpenSim/RegionModules", ...)]` | Entfernt |

**`WebRtcVoiceServiceModule`** — wird über `ServerUtils.LoadPlugin` geladen (kein Factory-Eintrag nötig).

**`WebRtcVoiceRegionModule`** — ist in `OptionalModulesFactory.CreateOptionalSharedModules()` eingetragen. Aktivierung über `Enabled = true` im Abschnitt `[Opensim.Addons.os-webrtc-janus.WebRtcVoice]`.

## Debugging

```ini
[Opensim.Addons.os-webrtc-janus.WebRtcVoice]
; Alle JSON-RPC Request/Response Bodies loggen
MessageDetails = true
```

Log-Header für grep:
- `[REGION WEBRTC VOICE]` — Region-seitige Caps
- `[WEBRTC VOICE SERVICE CONNECTOR]` — Region→Robust RPC
- `[WEBRTC VOICE SERVER CONNECTOR]` — Robust-seitiger HTTP-Empfang
- `[WEBRTC VOICE SERVICE MODULE]` — Service-Verteilung
