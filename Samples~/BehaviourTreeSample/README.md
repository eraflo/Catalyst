# Behaviour Tree Sample

This sample demonstrates the core functionality of the Behaviour Tree system, including visual debugging, blackboard management, services, and navigation.

## Overview

The sample scene contains:
- **Agent**: A capsule with a `NavMeshAgent` and a `BehaviourTreeRunner`.
- **Target**: A simple sphere that acts as the destination for the agent.
- **Environment**: A plane with a baked NavMesh.

## How to use

1.  **Open the Scene**: Navigate to `Samples/BehaviourTree Sample/BehaviourTreeScene.unity`.
2.  **Inspect the Agent**: Select the **Agent** object in the hierarchy. You will see the `BehaviourTreeRunner` component.
3.  **Open the Editor**: Go to **Tools > Unity Import Package > Behaviour Tree Editor**.
4.  **Observe Execution**:
    - Press **Play** in Unity.
    - The Editor window will automatically detect the agent and show the live execution path.
    - Nodes will highlight in **Green** (Success), **Red** (Failure), or **Yellow** (Running).
5.  **Interact with the Blackboard**:
    - In the Editor window's **Blackboard Panel**, you can see the live values.
    - Drag the **Target** object into the `MoveToTarget` slot in the Blackboard to see the agent move.
    - Alternatively, use the **Runner Inspector** to edit values at runtime.

## Services

Services are background tasks that run at intervals while their parent node is active. They are ideal for continuous logic like finding targets or updating distances.

### Available Services:
| Service | Description |
|---------|-------------|
| **Find Target** | Finds a GameObject by tag |
| **Find Closest By Tag** | Finds the nearest object with a specific tag |
| **Update Distance** | Calculates and stores distance to a target |
| **Check Range** | Checks if target is within a specified range |
| **Update Self Position** | Stores the agent's position in the Blackboard |
| **Debug Log** | Logs debug messages for testing |

### Adding a Service:
1. **Right-click** on any node in the graph
2. Select **"Add Service"**
3. Choose a service from the list
4. Configure its properties in the **Inspector**

Services are displayed with a **⚙️ badge** on nodes that have them attached.

## Sample Scripts

The sample includes a `BehaviourTreeDemo.cs` script (not attached by default) which shows how to:
- Access the `BehaviourTreeRunner`.
- Set blackboard values dynamically from other scripts.

```csharp
// Example: Setting a target from code
var runner = GetComponent<BehaviourTreeRunner>();
runner.Blackboard.Set("MoveToTarget", someTransform);
```

## Blackboard Types

Supported types: `bool`, `int`, `float`, `string`, `Vector3`, `GameObject`, `Transform`
