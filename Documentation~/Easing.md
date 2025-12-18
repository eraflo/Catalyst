# Easing Functions

A comprehensive library of standard easing functions (Linear, Quad, Cubic, Elastic, Bounce, etc.) based on Robert Penner's equations. While integrated with the Timer system, this module is standalone and can be used for any interpolation needs.

## Quick Start

```csharp
using Eraflo.UnityImportPackage.Easing;

// Interpolate between 0 and 1
float t = 0.5f; // 50% progress
float easedT = Easing.Evaluate(t, EasingType.QuadOut);

// Use in Vector3.Lerp
transform.position = Vector3.Lerp(start, end, easedT);
```

## Available Easing Types

All standard Penner easings are supported, with `In`, `Out`, and `InOut` variants:

| Group | Types | Description |
|-------|-------|-------------|
| **Standard** | `Linear` | No easing (constant speed) |
| **Power** | `Quad`, `Cubic`, `Quart`, `Quint` | Accelerating/decelerating power curves |
| **Trig/Exp** | `Sine`, `Expo`, `Circ` | Smooth natural curves |
| **Special** | `Elastic` | Springs past the target and wobbles |
| **Special** | `Back` | Overshoots slightly then returns |
| **Special** | `Bounce` | Bounces against the target |

## Integration

### With Timer System

The `Timer` class has a built-in helper:

```csharp
timer.GetProgress(EasingType.QuadInOut);
```

### With Custom Systems

You can use `Easing.Evaluate()` in your own update loops, coroutines, or tweening systems. 

```csharp
public class CustomMover : MonoBehaviour
{
    public EasingType moveCurve = EasingType.SineInOut;
    
    void Update()
    {
        float progress = Mathf.PingPong(Time.time, 1f);
        float curve = Easing.Evaluate(progress, moveCurve);
        // ... apply curve
    }
}
```
