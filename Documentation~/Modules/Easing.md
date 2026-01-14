# Easing Functions

A comprehensive library of standard easing functions based on Robert Penner's equations. Standalone module that can be used with any interpolation—timers, animations, UI, or custom systems.

---

## Table of Contents

1. [Features](#1-features)
2. [Quick Start](#2-quick-start)
3. [Available Easing Types](#3-available-easing-types)
4. [Usage Examples](#4-usage-examples)
5. [Integration](#5-integration)
6. [API Reference](#6-api-reference)

---

## 1. Features

- **31 Easing Types**: All standard Penner curves with In, Out, and InOut variants
- **Standalone**: No dependencies on other Catalyst modules
- **Zero Allocation**: Pure static functions
- **Timer Integration**: Works seamlessly with Catalyst Timer system
- **Chronos Compatible**: Used by ChronosManager for time scale transitions

---

## 2. Quick Start

```csharp
using UnityEngine;
using Eraflo.Catalyst.EasingSystem;

public class BasicEasingExample : MonoBehaviour
{
    public Transform target;
    public Vector3 startPos;
    public Vector3 endPos;
    
    void Update()
    {
        // Progress from 0 to 1 based on time
        float t = Mathf.PingPong(Time.time, 1f);
        
        // Apply easing
        float easedT = Easing.Evaluate(t, EasingType.QuadOut);
        
        // Use in interpolation
        target.position = Vector3.Lerp(startPos, endPos, easedT);
    }
}
```

---

## 3. Available Easing Types

### 3.1 Linear
| Type | Description |
|------|-------------|
| `Linear` | No easing, constant speed |

### 3.2 Power Curves
| Type | In | Out | InOut |
|------|:--:|:---:|:-----:|
| **Quad** (t²) | `QuadIn` | `QuadOut` | `QuadInOut` |
| **Cubic** (t³) | `CubicIn` | `CubicOut` | `CubicInOut` |
| **Quart** (t⁴) | `QuartIn` | `QuartOut` | `QuartInOut` |
| **Quint** (t⁵) | `QuintIn` | `QuintOut` | `QuintInOut` |

### 3.3 Trigonometric & Exponential
| Type | In | Out | InOut |
|------|:--:|:---:|:-----:|
| **Sine** | `SineIn` | `SineOut` | `SineInOut` |
| **Expo** | `ExpoIn` | `ExpoOut` | `ExpoInOut` |
| **Circ** | `CircIn` | `CircOut` | `CircInOut` |

### 3.4 Special Effects
| Type | In | Out | InOut | Effect |
|------|:--:|:---:|:-----:|--------|
| **Elastic** | `ElasticIn` | `ElasticOut` | `ElasticInOut` | Springs past target and wobbles |
| **Back** | `BackIn` | `BackOut` | `BackInOut` | Overshoots then returns |
| **Bounce** | `BounceIn` | `BounceOut` | `BounceInOut` | Bouncing ball effect |

---

## 4. Usage Examples

### 4.1 UI Animation

```csharp
using UnityEngine;
using UnityEngine.UI;
using Eraflo.Catalyst.EasingSystem;
using System.Collections;

public class UIPopup : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private EasingType _easeIn = EasingType.BackOut;
    [SerializeField] private EasingType _easeOut = EasingType.QuadIn;
    
    public IEnumerator ShowPopup()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        
        _panel.gameObject.SetActive(true);
        
        while (elapsed < _duration)
        {
            float t = elapsed / _duration;
            float easedT = Easing.Evaluate(t, _easeIn);
            _panel.localScale = Vector3.Lerp(startScale, endScale, easedT);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _panel.localScale = endScale;
    }
    
    public IEnumerator HidePopup()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.zero;
        
        while (elapsed < _duration)
        {
            float t = elapsed / _duration;
            float easedT = Easing.Evaluate(t, _easeOut);
            _panel.localScale = Vector3.Lerp(startScale, endScale, easedT);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _panel.gameObject.SetActive(false);
    }
}
```

### 4.2 Color Transition

```csharp
using UnityEngine;
using Eraflo.Catalyst.EasingSystem;

public class ColorPulse : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Color _colorA = Color.red;
    [SerializeField] private Color _colorB = Color.blue;
    [SerializeField] private EasingType _curve = EasingType.SineInOut;
    
    void Update()
    {
        float t = Mathf.PingPong(Time.time * 0.5f, 1f);
        float easedT = Easing.Evaluate(t, _curve);
        _renderer.color = Color.Lerp(_colorA, _colorB, easedT);
    }
}
```

### 4.3 Camera Shake with Falloff

```csharp
using UnityEngine;
using Eraflo.Catalyst.EasingSystem;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Progress 0 → 1
            float t = elapsed / duration;
            
            // Falloff: intensity decreases over time
            float intensity = Easing.Evaluate(1f - t, EasingType.QuadOut);
            
            float x = Random.Range(-1f, 1f) * magnitude * intensity;
            float y = Random.Range(-1f, 1f) * magnitude * intensity;
            
            transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = originalPos;
    }
}
```

---

## 5. Integration

### 5.1 With Timer System

```csharp
using UnityEngine;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Timers;
using Eraflo.Catalyst.EasingSystem;

public class TimerEasingExample : MonoBehaviour
{
    void Start()
    {
        Timer timer = App.Get<Timer>();
        
        timer.CreateTimer<CallbackTimer>(2f)
            .SetEasing(EasingType.ElasticOut)
            .OnUpdate(t => 
            {
                // t is already eased!
                transform.localScale = Vector3.one * t;
            })
            .OnComplete(() => Debug.Log("Done!"))
            .Start();
    }
}
```

### 5.2 With ChronosManager

```csharp
using UnityEngine;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Core.Chronos;
using Eraflo.Catalyst.EasingSystem;

public class SlowMotionExample : MonoBehaviour
{
    void TriggerSlowMo()
    {
        ChronosManager chronos = App.Get<ChronosManager>();
        
        // Time scale transition uses easing
        chronos.SetTimeScale(
            ChronosManager.DefaultChannel,
            targetScale: 0.2f,
            duration: 0.5f,
            ease: EasingType.QuadOut  // Smooth slow-down
        );
    }
}
```

---

## 6. API Reference

### Easing (Static Class)

| Method | Description |
|--------|-------------|
| `float Evaluate(float t, EasingType type)` | Returns eased value for progress `t` (0-1) |

### EasingType (Enum)

| Category | Values |
|----------|--------|
| **Linear** | `Linear` |
| **Quad** | `QuadIn`, `QuadOut`, `QuadInOut` |
| **Cubic** | `CubicIn`, `CubicOut`, `CubicInOut` |
| **Quart** | `QuartIn`, `QuartOut`, `QuartInOut` |
| **Quint** | `QuintIn`, `QuintOut`, `QuintInOut` |
| **Sine** | `SineIn`, `SineOut`, `SineInOut` |
| **Expo** | `ExpoIn`, `ExpoOut`, `ExpoInOut` |
| **Circ** | `CircIn`, `CircOut`, `CircInOut` |
| **Elastic** | `ElasticIn`, `ElasticOut`, `ElasticInOut` |
| **Back** | `BackIn`, `BackOut`, `BackInOut` |
| **Bounce** | `BounceIn`, `BounceOut`, `BounceInOut` |

---

## See Also

- [Timers](Timers.md): Timer system with easing support
- [ChronosManager](../Core/ChronosManager.md): Time management using easing
