# Unity Remote Tuning

Unity Remote Tuning is a runtime tuning tool for Unity that lets you adjust game variables from a mobile device (Android/iOS) in real time, without recompiling. The game runs a WebSocket server (host) that exposes registered variables; a companion app running on a phone (client) connects to it, receives the variable schema and renders an auto-generated UI to modify values live.

## How it works

```
[Unity Game - PC/Console]          [Mobile Device - Android/iOS]
  RemoteTuningHost                    RemoteTuningClient
  RTWebSocketServer   <-- WebSocket --> NativeWebSocket
  RemoteTuningRegistry                DynamicUIBuilder
  QR Code generation                  QR Code scanning
```

1. The game registers variables via `RemoteTuningRegistry`.
2. `RemoteTuningHost` starts an embedded WebSocket server and serializes the variable schema.
3. The host optionally generates a QR code with the connection info (IP + port).
4. The client app scans the QR code (or enters the IP manually) and connects.
5. The client receives the schema and builds a UI automatically (sliders, toggles, dropdowns, buttons, input fields).
6. Value changes made in the UI are sent back to the game in real time through the registered setters.

## Installation via Unity Package Manager (Git URL)

Open your Unity project, go to **Window > Package Manager**, click the **+** button and choose **Add package from git URL**, then paste:

```
https://github.com/Kalimist/Unity-Remote-Tuning.git
```

To lock to a specific version, append the tag:

```
https://github.com/Kalimist/Unity-Remote-Tuning.git#v1.0.0
```

Alternatively, add the entry directly to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.kalimist.unity-remote-tuning": "https://github.com/Kalimist/Unity-Remote-Tuning.git",
    ...
  }
}
```

> NativeWebSocket is **not** installed automatically. See the Requirements section below for manual setup.

## Requirements

- Unity 2020.3 or later
- NativeWebSocket package (for Android/iOS client builds)
- Included plugins:
  - `websocket-sharp.dll` — WebSocket server embedded in the game
  - `zxing.unity.dll` — QR code generation and scanning

## Installation

1. Copy the `Runtime`, `Plugins` and `Prefabs` folders into your Unity project under `Assets/`.
2. Install the NativeWebSocket package via the Unity Package Manager (Git URL: `https://github.com/endel/NativeWebSocket.git#upm`).
3. Add the `RemoteTuningHost` component (or use the provided `Online Remote Tuning.prefab`) to a GameObject in your scene.
4. Register your variables in code (see Usage section below).

## Project structure

```
Runtime/
  Core/
    Registry/       - RemoteTuningRegistry singleton, variable registration
    Models/         - ControlDefinition, ControlType, ValueType, Schema
    Protocol/       - WebSocket message types (schema, values, set, ping...)
    Presets/        - PresetManager, save/load named configurations
    Persistence/    - RemoteTuningPersistence, auto-save via PlayerPrefs
    History/        - HistoryManager, per-variable change history
    Utils/          - Network utilities
  Host/
    Server/         - RemoteTuningHost, RTWebSocketServer, ConnectionInfo
    QRGeneration/   - QRCodeGenerator
    UI/             - Host-side debug UI
  Client/
    Connection/     - RemoteTuningClient (NativeWebSocket)
    QRScanning/     - QRCodeScanner
    UI/             - DynamicUIBuilder, PresetUIManager, ConnectionStatusUI
Plugins/            - websocket-sharp.dll, zxing.unity.dll
Prefabs/            - Ready-to-use prefabs for host and client
Examples/           - Complete usage examples
```

## Usage

### 1. Register variables (Host / Game side)

Call `RemoteTuningRegistry.Instance.Register*` from any MonoBehaviour, typically in `Start` or `Awake`:

```csharp
var registry = RemoteTuningRegistry.Instance;

// Float slider (min, max, step)
registry.RegisterFloat(
    id: "player.speed",
    label: "Player Speed",
    getter: () => playerSpeed,
    setter: val => playerSpeed = val,
    min: 0f, max: 20f, step: 0.5f
);

// Integer slider
registry.RegisterInt(
    id: "player.maxHealth",
    label: "Max Health",
    getter: () => maxHealth,
    setter: val => maxHealth = val,
    min: 10, max: 200, step: 10
);

// Boolean toggle
registry.RegisterBool(
    id: "game.enableParticles",
    label: "Enable Particles",
    getter: () => enableParticles,
    setter: val => enableParticles = val
);

// Enum dropdown
registry.RegisterEnum(
    id: "game.difficulty",
    label: "Difficulty",
    getter: () => difficulty,
    setter: val => difficulty = val,
    options: new[] { "Easy", "Normal", "Hard", "Extreme" }
);
```

Unregister variables when the owning object is destroyed:

```csharp
void OnDestroy()
{
    RemoteTuningRegistry.Instance.Unregister("player.speed");
}
```

### 2. Start the host

Add `RemoteTuningHost` to a GameObject (or use the supplied prefab). Configure `gameId`, `gameName` and `port` in the Inspector. Enable `Auto Start` to start automatically on `Start()`, or call `StartHost()` manually.

```csharp
_host = GetComponent<RemoteTuningHost>();
_host.StartHost();

// Get the WebSocket URL and QR connection info
string url = _host.ConnectionInfo.GetWebSocketUrl(); // ws://192.168.x.x:8080
```

### 3. Connect from the client (mobile)

Add `RemoteTuningClient` to a GameObject in the client scene (Android/iOS build). Point it to the server IP and port, or use `QRCodeScanner` to read the connection info automatically:

```csharp
// Manual connection
_client.Connect("192.168.1.100", 8080);

// Connection from scanned QR
_client.Connect(scannedConnectionInfo);

// Events
_client.OnSchemaReceived += schema => Debug.Log("Schema received");
_client.OnValuesUpdated  += () => Debug.Log("Values updated");
```

`DynamicUIBuilder` listens to `OnSchemaReceived` and builds the control UI automatically from the schema received.

## Control types

| Control type | Used for            | Value types       |
|--------------|---------------------|-------------------|
| Slider       | Float / Int ranges  | Float, Int        |
| Toggle       | On/Off flags        | Bool              |
| Dropdown     | Enumerated options  | String, Enum      |
| InputField   | Free text input     | String            |
| Button       | Trigger actions     | None              |

## Presets

Save and restore named snapshots of all registered variable values:

```csharp
// Save current values as a preset
PresetManager.SavePreset("SpeedRun Config", "Optimized for speed runs");

// Load a preset (applies all saved values back to the registry)
PresetManager.LoadPreset("SpeedRun Config");

// List available presets
List<string> names = PresetManager.GetPresetNames();

// Delete a preset
PresetManager.DeletePreset("SpeedRun Config");
```

Presets are stored in `PlayerPrefs` and survive play sessions.

## Persistence

Automatically persist variable values across sessions without using presets:

```csharp
// Save a single value
RemoteTuningPersistence.SaveValue("player.speed", playerSpeed, ValueType.Float);

// Load a value
if (RemoteTuningPersistence.LoadValue("player.speed", ValueType.Float, out object saved))
    playerSpeed = (float)saved;

// Save / load all registered variables at once
RemoteTuningPersistence.SaveAll();
RemoteTuningPersistence.LoadAll();
```

## Change history

`HistoryManager` records every value change made through the registry:

```csharp
var history = HistoryManager.Instance;
history.SetMaxHistorySize(200);

// Subscribe to new entries
history.OnHistoryEntryAdded += entry =>
    Debug.Log($"[{entry.timestamp}] {entry.variableId}: {entry.previousValue} -> {entry.newValue}");

// Query history for a specific variable
var entries = history.GetHistory("player.speed");

// Clear history
history.Clear();
```

## QR code connection

In the Editor or on desktop, generate a QR PNG with the connection info so a phone can scan it:

```csharp
string json = _host.ConnectionInfo.ToJson();
Texture2D qrTexture = QRCodeGenerator.GenerateQRManual(json, 512);
QRCodeGenerator.SaveQRAsPNG(qrTexture, Application.dataPath + "/../connection_qr.png");
```

On the client, attach `QRCodeScanner` and call `StartScanning()`. When a valid QR is detected it fires `OnConnectionInfoScanned` with the decoded `ConnectionInfo`.

## Prefabs

| Prefab                         | Description                                                      |
|--------------------------------|------------------------------------------------------------------|
| `Online Remote Tuning.prefab`  | Full online setup: host, server, QR generation, status UI       |
| `Offline Remote Tuning.prefab` | Offline/Editor-only setup for testing without a mobile device   |
| `CategoryHeader.prefab`        | UI header used by DynamicUIBuilder to group controls             |
| `Slider.prefab`                | Slider control used by DynamicUIBuilder                          |
| `Toggle.prefab`                | Toggle control used by DynamicUIBuilder                          |
| `Dropdown.prefab`              | Dropdown control used by DynamicUIBuilder                        |

## Examples

All examples are in the `Examples/` folder:

| File                               | Description                                                   |
|------------------------------------|---------------------------------------------------------------|
| `CompleteRemoteTuningDemo.cs`      | Complete demo: host + variable registration + QR generation   |
| `PresetUsageExample.cs`            | How to save, load and delete presets                          |
| `PersistentRemoteTuningExample.cs` | Auto-persistence of variables across sessions                 |
| `PresetHistorySearchResetTest.cs`  | History tracking, search and reset                            |
| `SoccerGameExample.cs`             | Practical example with soccer-game variables                  |
| `ProfileUISetupExample.cs`         | Dynamic profile/category UI setup                             |

## License

See [LICENSE](LICENSE) for details.
