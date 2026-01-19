using UnityEngine;
using Unity.Collections;
using Eraflo.Catalyst.EasingSystem;

namespace Eraflo.Catalyst.Input.AimAssist
{
    /// <summary>
    /// Configuration for the Aim Assist system.
    /// </summary>
    [CreateAssetMenu(fileName = "AimAssistConfig", menuName = "Catalyst/Input/AimAssist Config")]
    public class AimAssistConfig : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Maximum distance at which assist is active.")]
        public float MaxDistance = 100f;

        [Tooltip("The cone angle (in degrees) for magnetism detection.")]
        public float ConeAngle = 15f;

        [Header("Friction (Slowdown)")]
        [Tooltip("Maximum friction multiplier (e.g., 0.5 means half sensitivity).")]
        public float MaxFriction = 0.5f;
        
        [Tooltip("Easing type for friction falloff based on distance.")]
        public EasingType FrictionEase = EasingType.SineInOut;

        [Header("Magnetism (Adhesion)")]
        [Tooltip("Maximum pull strength multiplier.")]
        public float MaxMagnetism = 1.0f;

        [Tooltip("Easing type for magnetism strength based on stick deflection.")]
        public EasingType MagnetismEase = EasingType.QuadOut;

        [Tooltip("How strongly the magnetism pulls towards the target center.")]
        public float MagnetismStrength = 5f;

        [Tooltip("Rotation speed of the magnetism 'pull' (smoothing).")]
        public float MagnetismSmoothing = 15f;

        public const int LUTSize = 256;

        /// <summary>
        /// Bakes the easing functions into NativeArrays for use in Burst jobs.
        /// </summary>
        public void BakeCurves(out NativeArray<float> frictionLUT, out NativeArray<float> magnetismLUT, Allocator allocator)
        {
            frictionLUT = new NativeArray<float>(LUTSize, allocator);
            magnetismLUT = new NativeArray<float>(LUTSize, allocator);

            for (int i = 0; i < LUTSize; i++)
            {
                float t = i / (float)(LUTSize - 1);
                
                // Friction usually goes from MaxFriction (at 0m) to 1.0 (at MaxDistance)
                // We'll evaluate Easing and lerp
                float easedFriction = Easing.Evaluate(t, FrictionEase);
                frictionLUT[i] = Mathf.Lerp(MaxFriction, 1.0f, easedFriction);

                // Magnetism usually goes from 0 (no stick) to MaxMagnetism (full stick)
                float easedMagnetism = Easing.Evaluate(t, MagnetismEase);
                magnetismLUT[i] = easedMagnetism * MaxMagnetism;
            }
        }
    }
}

