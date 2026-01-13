# Scene Flow Manager

The **Scene Flow Manager** is a robust system for handling complex scene transitions in Unity. It supports additive scene groups, automated loading screen management, memory cleanup, and transition events.

## Features

- **Scene Groups**: Define sets of scenes that belong together (e.g., Level 1 + HUD + Persistent UI).
- **Automated Flow**: Orchestrates the entire transition: Events -> Fade In -> Unload -> GC -> Load -> Set Active -> Fade Out.
- **UI Abstraction**: Seamlessly integrates with any loading screen UI via the `ILoadingScreen` interface.
- **Memory Management**: Automatically calls `Resources.UnloadUnusedAssets()` and `GC.Collect()` between loads to prevent fragmentation and memory spikes.
- **Testable Architecture**: Uses an abstracted `ISceneManager` to allow unit testing of scene flows without physical scene assets.

## The Transition Flow

The following diagram illustrates the sequence of operations during a scene transition:

```mermaid
sequenceDiagram
    participant App
    participant SL as SceneLoaderService
    participant EB as EventBus
    participant UI as ILoadingScreen
    participant SM as ISceneManager
    participant Res as Resources/GC

    App->>SL: LoadGroupAsync("Level1", ...)
    SL->>EB: Raise(OnTransitionStarted)
    SL->>UI: Show() (Fade In)
    UI-->>SL: Completed
    
    rect rgb(240, 240, 240)
        Note right of SL: Scene Cleanup
        SL->>SM: Unload current scenes
        SL->>Res: UnloadUnusedAssets & GC.Collect
    end

    rect rgb(230, 250, 230)
        Note right of SL: Scene Loading
        loop For each Scene in Group
            SL->>SM: LoadSceneAsync(scene, Additive)
            SM-->>SL: Progress update
            SL->>UI: UpdateProgress(float)
        end
    end

    SL->>SM: SetActiveScene(activeScene)
    
    opt If waitForInput is true
        SL->>SL: Wait for AnyKey/Click
    end

    SL->>UI: Hide() (Fade Out)
    UI-->>SL: Completed
    SL->>EB: Raise(OnTransitionCompleted)
    SL-->>App: Task Completed
```

## How to Use

### 1. Registering Scene Groups

You can define and register scene groups during your initialization phase:

```csharp
var group = new SceneGroup {
    Name = "Level_Desert",
    Scenes = new List<string> { "Environment_Desert", "UI_HUD", "Gameplay_Core" },
    ActiveScene = "Gameplay_Core"
};

App.Get<SceneLoaderService>().RegisterGroup(group);
```

### 2. Triggering a Load

To transition to a group:

```csharp
// Simple load
await App.Get<SceneLoaderService>().LoadGroupAsync("Level_Desert");

// Load with "Press any key to continue"
await App.Get<SceneLoaderService>().LoadGroupAsync("Level_Desert", waitForInput: true);
```

### 3. Implementing a Loading Screen

Simply implement the `ILoadingScreen` interface on a MonoBehaviour:

```csharp
public class MyLoadingUI : MonoBehaviour, ILoadingScreen {
    public CanvasGroup fadeGroup;
    public Image progressBar;

    public void Initialize() { /* Register yourself or setup */ }
    public void Shutdown() { }

    public async Task Show() {
        await fadeGroup.DOFade(1, 0.5f).AsyncWaitForCompletion();
    }

    public async Task Hide() {
        await fadeGroup.DOFade(0, 0.5f).AsyncWaitForCompletion();
    }

    public void UpdateProgress(float value) {
        progressBar.fillAmount = value;
    }
}
```

## Architecture

The system is built on a decoupled architecture to ensure testability and flexibility.

```mermaid
classDiagram
    class SceneLoaderService {
        +RegisterGroup(SceneGroup group)
        +LoadGroupAsync(string name, bool showUI, bool wait) Task
        -ISceneManager _sceneManager
        -ILoadingScreen _loadingScreen
    }

    class ILoadingScreen {
        <<interface>>
        +Show() Task
        +Hide() Task
        +UpdateProgress(float value)
    }

    class ISceneManager {
        <<interface>>
        +LoadSceneAsync(string name, mode) Task
        +UnloadSceneAsync(scene) Task
    }

    class SceneGroup {
        +string Name
        +List<string> Scenes
        +string ActiveScene
    }

    SceneLoaderService --> ILoadingScreen : uses
    SceneLoaderService --> ISceneManager : uses
    SceneLoaderService --> SceneGroup : manages
    ISceneManager <|-- UnitySceneManager : implementation
    ISceneManager <|-- MockSceneManager : tests
```

## Networked Scene Loading
Scene transitions can be synchronized across the network using the `SceneNetworkHandler`.

### Features
- **Server-Driven**: The server initiates scene transitions for all clients.
- **Progress Synchronization**: Clients report their loading progress, allowing the server to wait for everyone before proceeding.
- **ISceneNetworkBackend**: A specialized interface for networking backends to implement scene loading primitives.

### Flow
1. **Server** calls `LoadGroupAsync`.
2. **SceneNetworkHandler** broadcasts a `SceneLoadMessage` to clients.
3. **Clients** receive the message and start loading locally.
4. **Clients** poll the `SceneManager` and send progress updates back to the server.
5. **Server** proceeds once all clients are synchronized.

```mermaid
sequenceDiagram
    participant S_Code as Server Gameplay Code
    participant S_SL as SceneLoaderService (Server)
    participant S_NH as SceneNetworkHandler (Server)
    participant C_NH as SceneNetworkHandler (Client)
    participant C_SL as SceneLoaderService (Client)

    S_Code->>S_SL: LoadGroupAsync("Zone_A")
    S_SL->>S_NH: OnTransitionStarted
    S_NH->>C_NH: SceneLoadMessage("Zone_A")
    
    par Server Loading
        S_SL->>S_SL: Local Loading Workflow
    and Client Loading
        C_NH->>C_SL: LoadGroupAsync("Zone_A")
        loop Every Tick
            C_SL-->>C_NH: Progress Updates
            C_NH->>S_NH: SceneProgressMessage(float)
        end
    end

    S_NH->>S_SL: All Clients Ready
    S_SL-->>S_Code: Task Completed
```
