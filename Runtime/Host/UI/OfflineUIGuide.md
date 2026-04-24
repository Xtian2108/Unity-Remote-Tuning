# Offline Tuning UI - Guia de uso

Permite modificar variables expuestas directamente desde el host (el juego) sin necesitar un cliente remoto conectado. Los cambios se aplican en tiempo real a través del `RemoteTuningRegistry` y, si hay clientes conectados, también se les envían por WebSocket.

---

## Componentes

| Componente | Descripcion |
|---|---|
| `OfflineUIBuilder` | Construye los controles (sliders, toggles, dropdowns) a partir del `RemoteTuningRegistry`. |
| `OfflinePresetUIManager` | Gestiona 3 slots de perfiles (guardar / cargar) usando el registry directamente. |

---

## Setup en la escena del host

### 1. Crear el GameObject de UI

Crea un Canvas (o usa uno existente) y dentro de el crea un GameObject llamado `OfflineTuningUI`.

### 2. Agregar OfflineUIBuilder

Agrega el componente `OfflineUIBuilder` al GameObject y configura sus campos:

| Campo | Descripcion |
|---|---|
| `Content Parent` | Transform donde se instancian los controles generados (un ScrollView Content, por ejemplo). |
| `Scroll Rect` | Opcional. El ScrollRect del panel para desplazamiento. |
| `Host` | Opcional. Referencia al `RemoteTuningHost`. Si se asigna, los cambios offline se envian a clientes remotos conectados. |
| `Slider Prefab` | Prefab de slider (el mismo que usa `DynamicUIBuilder` en el cliente). |
| `Toggle Prefab` | Prefab de toggle. |
| `Dropdown Prefab` | Prefab de dropdown. |
| `Build On Start` | Si esta activo, construye la UI automaticamente al iniciar. Recomendado: activado. |
| `Rebuild On Registry Change` | Si esta activo, reconstruye la UI cada vez que se registra o desregistra una variable. Util si las variables se registran tarde. |

### 3. Agregar OfflinePresetUIManager

Agrega el componente `OfflinePresetUIManager` al mismo GameObject (o a uno separado) y configura sus campos:

| Campo | Descripcion |
|---|---|
| `Ui Builder` | Referencia al `OfflineUIBuilder` creado en el paso anterior. |
| `Profile 1/2/3 Button` | Botones de UI para cargar cada perfil. |
| `Profile 1/2/3 Text` | Etiquetas que muestran el estado de cada perfil. |
| `Save Current Button` | Boton para guardar el perfil activo. |
| `Default Color` | Color del boton cuando el slot esta vacio. |
| `Active Color` | Color cuando ese perfil esta activo. |
| `Saved Color` | Color cuando el slot tiene datos guardados pero no esta activo. |

---

## Orden de ejecucion importante

Las variables deben estar registradas en el `RemoteTuningRegistry` **antes** de que `OfflineUIBuilder.Start()` se ejecute (o antes de llamar `BuildUI()` manualmente).

Si tus variables se registran en `Awake()` y el `OfflineUIBuilder` llama `BuildUI()` en `Start()`, el orden natural de Unity garantiza que funcione correctamente.

Si las variables se registran de forma asincrona o tardia, activa `Rebuild On Registry Change` en el inspector.

---

## Comportamiento de los perfiles

Los perfiles se almacenan en `PlayerPrefs` con claves independientes de los perfiles del cliente remoto:

| Slot | Clave en PlayerPrefs |
|---|---|
| Perfil 1 | `OfflineProfile_1` |
| Perfil 2 | `OfflineProfile_2` |
| Perfil 3 | `OfflineProfile_3` |
| Perfil activo | `OfflineActiveProfile` |

### Flujo al cargar un perfil

1. Se guarda automaticamente el perfil activo actual (auto-save).
2. Se leen los valores guardados del perfil seleccionado desde `PlayerPrefs`.
3. Se aplican los valores al `RemoteTuningRegistry` (llama a los setters reales).
4. Se espera un frame para que los setters propaguen.
5. Se llama a `OfflineUIBuilder.RefreshUIValues()` para actualizar los widgets.
6. Si hay un `RemoteTuningHost` con clientes conectados, cada cambio se transmite.

### Flujo al guardar un perfil

1. Se leen los valores actuales de todas las variables via `variable.GetValue()`.
2. Se serializa el preset como JSON y se guarda en `PlayerPrefs`.
3. Se actualiza el estado visual de los botones.

---

## API publica

### OfflineUIBuilder

```csharp
// Reconstruye toda la UI desde cero
offlineUIBuilder.BuildUI();

// Solo actualiza los valores de los widgets existentes (sin destruir ni recrear)
// Usar despues de aplicar un preset
offlineUIBuilder.RefreshUIValues();

// Destruye todos los widgets generados
offlineUIBuilder.ClearUI();
```

### OfflinePresetUIManager

```csharp
// Carga el perfil 1, 2 o 3 (con auto-save del actual)
offlinePresetUIManager.LoadProfile(1);

// Guarda en el perfil activo (o en el 1 si ninguno esta activo)
offlinePresetUIManager.SaveCurrentProfile();

// Guarda en un slot especifico
offlinePresetUIManager.SaveToProfile(2);

// Borra el slot de un perfil
offlinePresetUIManager.ResetProfile(3);

// Borra todos los slots
offlinePresetUIManager.ResetAllProfiles();
```

---

## Diferencias con el cliente remoto (PresetUIManager)

| Aspecto | Cliente remoto (PresetUIManager) | Host offline (OfflinePresetUIManager) |
|---|---|---|
| Fuente de datos | `client.Schema.controls` | `RemoteTuningRegistry` |
| Aplicacion de valores | `control.SetValue()` + envia por WebSocket | `variable.SetValue()` (setter directo) |
| Claves PlayerPrefs | `ClientProfile_1/2/3` | `OfflineProfile_1/2/3` |
| Requiere conexion | Si (para recibir schema) | No |
| Broadcast a clientes | Siempre (es el cliente) | Opcional (si se asigna `Host`) |

---

## Ejemplo de jerarquia en la escena

```
Canvas
└── OfflineTuningPanel
    ├── OfflineUIBuilder (componente)
    ├── OfflinePresetUIManager (componente)
    ├── ScrollView
    │   └── Viewport
    │       └── Content  <-- asignar como Content Parent en OfflineUIBuilder
    └── ProfileButtons
        ├── Profile1Button
        ├── Profile2Button
        ├── Profile3Button
        └── SaveButton
```

