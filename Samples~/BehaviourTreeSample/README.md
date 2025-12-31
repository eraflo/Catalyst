# Behaviour Tree Sample

This sample demonstrates the core functionality of the Behaviour Tree system, including visual debugging, blackboard management, and navigation.

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

## Sample Scripts

The sample includes a `BehaviourTreeDemo.cs` script (not attached by default) which shows how to:
- Access the `BehaviourTreeRunner`.
- Set blackboard values dynamically from other scripts.
- Listen to tree events (planned for future updates).

```csharp
// Example: Setting a target from code
var runner = GetComponent<BehaviourTreeRunner>();
runner.Blackboard.Set("MoveToTarget", someTransform);
```
