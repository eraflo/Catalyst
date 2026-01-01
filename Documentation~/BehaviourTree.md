# Behaviour Tree System

A flexible, ScriptableObject-based Behaviour Tree system for AI agents with visual graph editing.

## Quick Start

1. **Create Tree:** Right-click in Project → Create → Behaviour Tree → Behaviour Tree
2. **Open Editor:** Click "Open Behaviour Tree Editor" button in the inspector
3. **Add Nodes:** Right-click in graph or press Space for search window
4. **Connect Nodes:** Drag from output (bottom) to input (top)
5. **Run:** Add `BehaviourTreeRunner` to your agent and assign the tree

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

Shared data container for nodes. Supports reactive callbacks.

**Supported Types:** `bool`, `int`, `float`, `string`, `Vector3`, `GameObject`, `Transform`

```csharp
// Set values
Blackboard.Set("target", transform.position);
Blackboard.Set("health", 100);

// Get values
Vector3 pos = Blackboard.Get<Vector3>("target");

// Check existence
if (Blackboard.Contains("key")) { ... }

// Reactive: Listen to changes
Blackboard.RegisterListener("health", (oldVal, newVal) => {
    Debug.Log($"Health changed: {oldVal} → {newVal}");
});
```

---

## Data Flow

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
    ├── Node.cs, BehaviourTree.cs, Blackboard.cs
    └── TargetProvider.cs, BlackboardTargetProvider.cs
```

---

## Integration

| System | Usage |
|--------|-------|
| **Timer** | `Wait`, `Cooldown` use Timer.Delay |
| **EventBus** | `RaiseEvent`, `WaitForEvent` nodes |
| **AI Navigation** | `MoveTo` uses NavMeshAgent |
| **Networking** | `NetworkBehaviourTreeSync` for multiplayer |

---

## Multiplayer

Add `NetworkBehaviourTreeSync` for server-authoritative AI:
- Server evaluates tree, clients receive state
- Blackboard values can be synchronized
- Works with Netcode for GameObjects
