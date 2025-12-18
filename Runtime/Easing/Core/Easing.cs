using UnityEngine;


namespace Eraflo.UnityImportPackage.EasingSystem
{
    /// <summary>
    /// Static class containing standard easing functions.
    /// Based on Robert Penner's easing equations.
    /// </summary>
    public static class Easing
    {
        private const float PI = Mathf.PI;
        private const float HALF_PI = Mathf.PI / 2f;

        /// <summary>
        /// Calculates the interpolated value for the given easing type.
        /// </summary>
        /// <param name="t">Current progress (0 to 1).</param>
        /// <param name="type">Type of easing to apply.</param>
        /// <returns>Eased value.</returns>
        public static float Evaluate(float t, EasingType type)
        {
            switch (type)
            {
                case EasingType.Linear:      return t;
                
                case EasingType.QuadIn:      return t * t;
                case EasingType.QuadOut:     return t * (2f - t);
                case EasingType.QuadInOut:   return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
                
                case EasingType.CubicIn:     return t * t * t;
                case EasingType.CubicOut:    return (--t) * t * t + 1f;
                case EasingType.CubicInOut:  return t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;
                
                case EasingType.QuartIn:     return t * t * t * t;
                case EasingType.QuartOut:    return 1f - (--t) * t * t * t;
                case EasingType.QuartInOut:  return t < 0.5f ? 8f * t * t * t * t : 1f - 8f * (--t) * t * t * t;
                
                case EasingType.QuintIn:     return t * t * t * t * t;
                case EasingType.QuintOut:    return 1f + (--t) * t * t * t * t;
                case EasingType.QuintInOut:  return t < 0.5f ? 16f * t * t * t * t * t : 1f + 16f * (--t) * t * t * t * t;
                
                case EasingType.SineIn:      return 1f - Mathf.Cos(t * HALF_PI);
                case EasingType.SineOut:     return Mathf.Sin(t * HALF_PI);
                case EasingType.SineInOut:   return 0.5f * (1f - Mathf.Cos(PI * t));
                
                case EasingType.ExpoIn:      return t == 0f ? 0f : Mathf.Pow(2f, 10f * (t - 1f));
                case EasingType.ExpoOut:     return t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case EasingType.ExpoInOut:
                    if (t == 0f) return 0f;
                    if (t == 1f) return 1f;
                    return t < 0.5f ? 0.5f * Mathf.Pow(2f, 20f * t - 10f) : 0.5f * (2f - Mathf.Pow(2f, -20f * t + 10f));
                
                case EasingType.CircIn:      return 1f - Mathf.Sqrt(1f - t * t);
                case EasingType.CircOut:     return Mathf.Sqrt(1f - (--t) * t);
                case EasingType.CircInOut:   return t < 0.5f ? (1f - Mathf.Sqrt(1f - 4f * t * t)) * 0.5f : (Mathf.Sqrt(1f - (2f * t - 2f) * (2f * t - 2f)) + 1f) * 0.5f;
                
                case EasingType.ElasticIn:
                    if (t == 0f) return 0f;
                    if (t == 1f) return 1f;
                    return -Mathf.Pow(2f, 10f * (t - 1f)) * Mathf.Sin((t - 1.1f) * 5f * PI);
                
                case EasingType.ElasticOut:
                    if (t == 0f) return 0f;
                    if (t == 1f) return 1f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - 0.1f) * 5f * PI) + 1f;
                
                case EasingType.ElasticInOut:
                    if (t == 0f) return 0f;
                    if (t == 1f) return 1f;
                    return t < 0.5f ? 0.5f * Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * ((2f * PI) / 4.5f))
                                    : -(0.5f * Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * ((2f * PI) / 4.5f))) + 1f; // Simplified logic, usually separate In/Out logic needed
                
                case EasingType.BackIn:      return t * t * (2.70158f * t - 1.70158f);
                case EasingType.BackOut:     return (--t) * t * (2.70158f * t + 1.70158f) + 1f;
                case EasingType.BackInOut:   return t < 0.5f ? (t * t * (7.189819f * t - 2.5949095f)) * 2f : ((t -= 2f) * t * (3.5949095f * t + 2.5949095f) + 2f) * 0.5f;

                case EasingType.BounceIn:    return 1f - Evaluate(1f - t, EasingType.BounceOut);
                case EasingType.BounceOut:
                    if (t < 1f / 2.75f) return 7.5625f * t * t;
                    if (t < 2f / 2.75f) return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
                    if (t < 2.5f / 2.75f) return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
                    return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
                case EasingType.BounceInOut: return t < 0.5f ? Evaluate(t * 2f, EasingType.BounceIn) * 0.5f : Evaluate(t * 2f - 1f, EasingType.BounceOut) * 0.5f + 0.5f;
                
                default: return t;
            }
        }
    }
}
