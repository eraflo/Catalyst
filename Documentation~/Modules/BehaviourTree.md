# Behaviour Tree System

A flexible, ScriptableObject-based Behaviour Tree system for AI agents with visual graph editing.

## Quick Start

1. **Create Tree:** Right-click in Project → Create → Behaviour Tree → Behaviour Tree
2. **Open Editor:** Click "Open Behaviour Tree Editor" button in the inspector
3. **Add Nodes:** Right-click in graph or press Space for search window
4. **Connect Nodes:** Drag from output (bottom) to input (top)
5. **Run:** Add `BehaviourTreeRunner` to your agent and assign the tree

---

## Execution Flow

The tree is evaluated top-to-bottom, left-to-right. Each node returns a `NodeState` (Success, Failure, or Running) to its parent.

```mermaid
sequenceDiagram
    participant R as Runner
    participant S as Sequence
    participant C1 as Condition 1
    participant A1 as Action 1

    R->>S: Evaluate()
    S->>C1: Evaluate()
    C1-->>S: Success
    S->>A1: Evaluate()
    A1-->>S: Running
    S-->>R: Running
    
    Note over R,A1: Next Tick...
    
    R->>S: Evaluate()
    S->>A1: Evaluate()
    A1-->>S: Success
    S-->>R: Success
```

---

## Visual Editor

Open via: `Tools > Eraflo Catalyst > Behaviour Tree Editor`

| Feature | Description |
|---------|-------------|
| **Graph View** | Top-to-bottom node layout with drag & zoom |
| **Node Search** | Press Space to search and create nodes |
| **Inspector Panel** | Edit selected node properties |
| **Blackboard Panel** | View/edit shared data |
| **Context Menu** | Right-click to create or set root |

---

## Node Types

```mermaid
graph TD
    Node["Node (Base)"]
    Root["Root Node"]
    Composite["Composite Node"]
    Decorator["Decorator Node"]
    Action["Action Node"]
    Condition["Condition Node"]

    Node --> Root
    Node --> Composite
    Node --> Decorator
    Node --> Action
    Node --> Condition

    Composite --> Selector["Selector (OR)"]
    Composite --> Sequence["Sequence (AND)"]
    Composite --> Parallel["Parallel"]

    Decorator --> Inverter["Inverter"]
    Decorator --> Repeater["Repeater"]
    Decorator --> Cooldown["Cooldown"]

    Action --> Wait["Wait"]
    Action --> MoveTo["MoveTo"]
    Action --> RaiseEvent["RaiseEvent"]

    Condition --> BlackboardCondition["BlackboardCondition"]
    Condition --> IsInRange["IsInRange"]
```

### Composites (Blue)
| Node | Description |
|------|-------------|
| `Selector` | OR - succeeds on first child success |
| `Sequence` | AND - fails on first child failure |
| `Parallel` | Runs all children simultaneously |
| `RandomSelector` | Shuffles order before selection |

### Decorators (Purple)
| Node | Description |
|------|-------------|
| `Inverter` | Inverts child result |
| `Repeater` | Repeats N times (0 = infinite) |
| `Succeeder` | Always returns Success |
| `Failer` | Always returns Failure |
| `UntilFail` | Repeats until child fails |
| `Cooldown` | Rate-limits execution (uses Timer) |
| `TimeLimit` | Aborts child after timeout |
| `Probability` | Executes child with X% chance |
| `SubTree` | Executes another BT asset |
| `BlackboardConditional` | Guards child with Blackboard condition |

### Actions (Green)

**General:**
| Node | Description |
|------|-------------|
| `Wait` | Waits N seconds. Supports dynamic duration via `Duration` port. |
| `RaiseEvent` | Raises EventChannel |
| `WaitForEvent` | Waits for EventChannel to fire |
| `RunUnityEvent` | Invokes a UnityEvent |
| `ExecuteCommand` | Executes a Command via the Command System. Supports Undo/Replay and parameter injection via Blackboard. |

**Navigation:** (requires NavMeshAgent)
| Node | Description |
|------|-------------|
| `MoveTo` | NavMeshAgent pathfinding. Supports dynamic target via `InputTarget` port. |
| `RotateTo` | Smooth rotation towards target |

**Animation:**
| Node | Description |
|------|-------------|
| `PlayAnimation` | Plays animation with crossfade |
| `SetAnimatorParameter` | Sets Bool/Int/Float/Trigger |

**Blackboard:**
| Node | Description |
|------|-------------|
| `SetBlackboardValue` | Sets blackboard data |

**Debug:**
| Node | Description |
|------|-------------|
| `Log` | Debug.Log message |

### Conditions (Yellow)
| Node | Description |
|------|-------------|
| `BlackboardCondition` | Checks blackboard value |
| `IsInRange` | Distance check to target |
| `HasLineOfSight` | Raycast visibility check. Supports dynamic target via `InputTarget` port. |

---

## TargetProvider System

ScriptableObject-based targeting without tags/layers:

| Provider | Description |
|----------|-------------|
| `BlackboardTargetProvider` | Reads Transform/GameObject/Vector3 from Blackboard |

Create via: Right-click → Create → Eraflo → BehaviourTree → Target Providers

---

## Services

Services are background tasks attached to nodes that run at intervals while the node is active.

### Built-in Services
| Service | Description |
|---------|-------------|
| `FindTargetService` | Finds GameObject by tag |
| `FindClosestByTagService` | Finds nearest object with tag |
| `UpdateDistanceService` | Calculates distance to target |
| `CheckRangeService` | Checks if target is within range |
| `UpdateSelfPositionService` | Stores owner position |
| `DebugLogService` | Logs debug messages |

### Adding Services in Editor
1. Right-click on any node → **Add Service**
2. Select service from list
3. Configure properties in Inspector
4. ⚙️ badge appears on nodes with services

### Custom Service
```csharp
[BehaviourTreeNode("Services", "My Service")]
public class MyService : ServiceNode
{
    public float Interval = 0.5f; // Tick rate
    
    protected override void OnServiceUpdate()
    {
        // Runs every Interval seconds while parent is active
    }
}
```

---

## Blackboard

Shared data container for nodes, now powered by the **Core Blackboard System**. 

### Scoping & Scoped AI
By default, each `BehaviourTree` instance has its own independent blackboard. However, you can use the **Hierarchical Scoping** feature to let your AI inherit data from a global context (e.g., world temperature, game state) while keeping local overrides (e.g., unit health, current target).

```csharp
// Set values locally on the tree's blackboard
Blackboard.Set("target", transform.position);
Blackboard.Set("health", 100);

// Get values (will search parent if not found locally)
Vector3 pos = Blackboard.Get<Vector3>("target");

// Reactive: Listen to changes
Blackboard.RegisterListener("health", (oldVal, newVal) => {
    Debug.Log($"Health changed: {oldVal} → {newVal}");
});
```

> [!TIP]
> To link an AI's blackboard to the global state, you can set `BlackboardManager.Global` as the parent of the tree's blackboard during initialization.

---

## Data Flow / Blackboard

```mermaid
graph LR
    subgraph "Blackboard (Shared Data)"
        B[(Data Storage)]
    end

    subgraph "Logic Nodes"
        N1[Action A]
        N2[Condition B]
    end

    subgraph "Data Ports"
        P1((Output Port))
        P2((Input Port))
    end

    N1 --> P1
    P1 -.-> P2
    P2 --> N2
    
    N1 <--> B
    N2 <--> B
```

Directly connect nodes to pass data values (e.g., Calculate Position → MoveTo).

1. **Ports**: Nodes can have Input (Left) and Output (Right) ports.
2. **Connections**: Drag from Output to Input to link data.
3. **Hybrid Mode**: Standard nodes (Wait, MoveTo) use data if connected, or fallback to Inspector values if not.

### Supported Types
- `float`, `int`, `bool`, `string`, `Vector3`, `GameObject`, `Transform`
- System validates connections (prevents type mismatch).

---

## Custom Nodes

### Action Node
```csharp
[BehaviourTreeNode("Actions/MyCategory", "My Action")]
public class MyAction : ActionNode
{
    protected override void OnStart() { }
    
    protected override NodeState OnUpdate()
    {
        // Do work, return Running/Success/Failure
        return NodeState.Success;
    }
    
    protected override void OnStop() { }
}
```

### Data Flow Action
```csharp
[BehaviourTreeNode("MyCategory", "Generator")]
public class NumberGenerator : ActionNode
{
    [NodeOutput] public float OutputValue; // Creates Output Port

    protected override NodeState OnUpdate()
    {
        OutputValue = Random.Range(0f, 100f);
        return NodeState.Success;
    }
}

[BehaviourTreeNode("MyCategory", "Consumer")]
public class NumberPrinter : ActionNode
{
    [NodeInput] public float InputValue; // Creates Input Port

    protected override NodeState OnUpdate()
    {
        // Get data from port (or use local variable as fallback)
        float val = GetData("InputValue", InputValue); 
        Debug.Log($"Value: {val}");
        return NodeState.Success;
    }
}
```

### Condition Node
```csharp
[BehaviourTreeNode("Conditions", "My Condition")]
public class MyCondition : ConditionNode
{
    protected override bool CheckCondition()
    {
        return Owner.GetComponent<Health>().Value > 0;
    }
}
```

### Decorator Node
```csharp
[BehaviourTreeNode("Decorators", "My Decorator")]
public class MyDecorator : DecoratorNode
{
    protected override NodeState OnUpdate()
    {
        if (Child == null) return NodeState.Failure;
        return Child.Evaluate();
    }
}
```

---

## File Structure

```
Runtime/BehaviourTree/
├── Actions/
│   ├── ActionNode.cs (base class)
│   ├── Wait.cs, RaiseEvent.cs, WaitForEvent.cs, RunUnityEvent.cs
│   ├── Navigation/ → MoveTo.cs, RotateTo.cs
│   ├── Animation/ → PlayAnimation.cs, SetAnimatorParameter.cs
│   ├── Blackboard/ → SetBlackboardValue.cs
│   └── Debug/ → Log.cs
├── Composites/
│   ├── CompositeNode.cs (base class)
│   └── Selector.cs, Sequence.cs, Parallel.cs, RandomSelector.cs
├── Decorators/
│   ├── DecoratorNode.cs (base class)
│   ├── Inverter.cs, Succeeder.cs, Failer.cs, Repeater.cs, etc.
│   └── Blackboard/ → BlackboardConditional.cs
├── Conditions/
│   ├── ConditionNode.cs (base class)
│   ├── IsInRange.cs, HasLineOfSight.cs
│   └── Blackboard/ → BlackboardCondition.cs
└── Core/
    ├── Node.cs, BehaviourTree.cs
    └── TargetProvider.cs, BlackboardTargetProvider.cs
```

---

## Integration

```mermaid
graph TB
    subgraph "Service Locator"
        SL[App]
    end

    subgraph "Core Services"
        T[Timer]
        EB[EventBus]
        NM[NetworkManager]
        BM[BlackboardManager]
    end

    subgraph "Behaviour Tree System"
        Runner[BehaviourTreeRunner]
        Tree[BehaviourTree Asset]
        BB[Blackboard Instance]
    end

    SL -- "Get<T>" --> T
    SL -- "Get<T>" --> EB
    SL -- "Get<T>" --> NM
    SL -- "Get<T>" --> BM

    Runner --> Tree
    Runner --> BB
    BB -- "Parent (Optional)" --> BM
    
    Tree -- "Evaluate" --> Nodes[Nodes]
    Nodes -- "Uses" --> T
    Nodes -- "Uses" --> EB
    Nodes -- "Uses" --> NM
```

| System | Usage |
|--------|-------|
| **Service Locator** | Core modules are accessed via `App.Get<T>()`. |
| **Blackboard** | Core blackboard system handles variable storage, scoping, and persistence. |
| **Timer** | `Wait`, `Cooldown` use `App.Get<Timer>()` internally. |
| **EventBus** | `RaiseEvent`, `WaitForEvent` nodes use `App.Get<EventBus>()`. |
| **Command System** | `ExecuteCommand` node uses `App.Get<CommandManager>()` to perform recordable/syncable actions. |
| **AI Navigation** | `MoveTo` uses `NavMeshAgent`. |
| **Networking** | `NetworkBehaviourTreeSync` for multiplayer, uses `App.Get<NetworkManager>()`. |

---

## Multiplayer

Add `NetworkBehaviourTreeSync` for server-authoritative AI:
- Server evaluates tree, clients receive state
- Blackboard values can be synchronized
- Works with Netcode for GameObjects
