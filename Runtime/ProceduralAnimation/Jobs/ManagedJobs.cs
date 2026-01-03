using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Eraflo.Catalyst.Noise;

namespace Eraflo.Catalyst.ProceduralAnimation.Jobs
{
    /// <summary>
    /// A managed job wrapper that handles native array lifecycle.
    /// Provides a convenient way to run spring animation on a set of transforms.
    /// </summary>
    public class ManagedSpringJob : IProceduralAnimationJob, IDisposable
    {
        private TransformAccessArray _transforms;
        private NativeArray<float3> _targets;
        private NativeArray<float3> _velocities;
        
        private SpringCoefficients _coefficients;
        private float _deltaTime;
        private bool _isDisposed;
        private bool _needsUpdate = true;
        
        /// <summary>
        /// Number of transforms in this job.
        /// </summary>
        public int Count => _transforms.isCreated ? _transforms.length : 0;
        
        /// <inheritdoc/>
        public bool NeedsUpdate => _needsUpdate && !_isDisposed && _transforms.isCreated;
        
        /// <summary>
        /// Creates a new managed spring job for the given transforms.
        /// </summary>
        public ManagedSpringJob(Transform[] transforms, SpringPreset preset = SpringPreset.Smooth)
        {
            _transforms = new TransformAccessArray(transforms);
            _targets = new NativeArray<float3>(transforms.Length, Allocator.Persistent);
            _velocities = new NativeArray<float3>(transforms.Length, Allocator.Persistent);
            _coefficients = SpringCoefficients.FromPreset(preset);
            
            // Initialize targets with current positions
            for (int i = 0; i < transforms.Length; i++)
            {
                _targets[i] = transforms[i].localPosition;
            }
        }
        
        /// <summary>
        /// Sets the spring parameters.
        /// </summary>
        public void SetSpringParameters(float frequency, float damping, float response)
        {
            _coefficients = SpringCoefficients.Create(frequency, damping, response);
        }
        
        /// <summary>
        /// Sets the spring parameters from a preset.
        /// </summary>
        public void SetSpringPreset(SpringPreset preset)
        {
            _coefficients = SpringCoefficients.FromPreset(preset);
        }
        
        /// <summary>
        /// Sets the target position for a specific transform.
        /// </summary>
        public void SetTarget(int index, float3 target)
        {
            if (index >= 0 && index < _targets.Length)
            {
                _targets[index] = target;
            }
        }
        
        /// <summary>
        /// Sets target positions for all transforms.
        /// </summary>
        public void SetTargets(float3[] targets)
        {
            for (int i = 0; i < math.min(targets.Length, _targets.Length); i++)
            {
                _targets[i] = targets[i];
            }
        }
        
        /// <summary>
        /// Enables or disables updates.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _needsUpdate = enabled;
        }
        
        /// <inheritdoc/>
        public void Prepare(float deltaTime)
        {
            _deltaTime = deltaTime;
        }
        
        /// <inheritdoc/>
        public JobHandle Schedule(JobHandle dependency = default)
        {
            if (!_transforms.isCreated) return dependency;
            
            var job = new SpringTransformJob
            {
                Targets = _targets,
                Velocities = _velocities,
                DeltaTime = _deltaTime,
                K1 = _coefficients.K1,
                K2 = _coefficients.K2,
                K3 = _coefficients.K3
            };
            
            return job.Schedule(_transforms, dependency);
        }
        
        /// <inheritdoc/>
        public void Apply()
        {
            // TransformAccessArray jobs apply directly via IJobParallelForTransform
            // No additional work needed here
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            if (_transforms.isCreated) _transforms.Dispose();
            if (_targets.IsCreated) _targets.Dispose();
            if (_velocities.IsCreated) _velocities.Dispose();
        }
    }
    
    /// <summary>
    /// Job that applies noise displacement directly to transforms.
    /// </summary>
    [BurstCompile]
    public struct NoiseTransformJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float3> OriginalPositions;
        
        [ReadOnly] public float Time;
        [ReadOnly] public float Frequency;
        [ReadOnly] public float Amplitude;
        [ReadOnly] public float3 NoiseOffset;
        [ReadOnly] public bool3 AxisMask;
        
        public void Execute(int index, TransformAccess transform)
        {
            float3 originalPos = OriginalPositions[index];
            float3 noiseCoord = (originalPos + NoiseOffset) * Frequency;
            
            // Sample noise for each axis with different offsets for independence
            float nx = BurstNoise.Sample4D(noiseCoord.x, noiseCoord.y, noiseCoord.z, Time);
            float ny = BurstNoise.Sample4D(noiseCoord.x + 100f, noiseCoord.y + 100f, noiseCoord.z, Time);
            float nz = BurstNoise.Sample4D(noiseCoord.x, noiseCoord.y + 200f, noiseCoord.z + 200f, Time);
            
            // Apply axis mask
            float3 displacement = new float3(
                AxisMask.x ? nx : 0f,
                AxisMask.y ? ny : 0f,
                AxisMask.z ? nz : 0f
            ) * Amplitude;
            
            transform.localPosition = originalPos + displacement;
        }
    }
    
    /// <summary>
    /// A managed job wrapper for noise-based displacement.
    /// Applies noise directly to transforms using IJobParallelForTransform.
    /// </summary>
    public class ManagedNoiseJob : IProceduralAnimationJob, IDisposable
    {
        private TransformAccessArray _transforms;
        private NativeArray<float3> _originalPositions;
        
        private float _frequency = 1f;
        private float _amplitude = 0.1f;
        private float _timeScale = 1f;
        private float _time;
        private float3 _offset;
        private bool3 _axisMask = new bool3(true, true, false);
        
        private bool _isDisposed;
        private bool _needsUpdate = true;
        
        /// <summary>
        /// Number of transforms in this job.
        /// </summary>
        public int Count => _transforms.isCreated ? _transforms.length : 0;
        
        /// <inheritdoc/>
        public bool NeedsUpdate => _needsUpdate && !_isDisposed && _transforms.isCreated;
        
        /// <summary>
        /// Current time value used for noise animation.
        /// </summary>
        public float Time => _time;
        
        /// <summary>
        /// Creates a new managed noise job for the given transforms.
        /// </summary>
        public ManagedNoiseJob(Transform[] transforms)
        {
            _transforms = new TransformAccessArray(transforms);
            _originalPositions = new NativeArray<float3>(transforms.Length, Allocator.Persistent);
            
            for (int i = 0; i < transforms.Length; i++)
            {
                _originalPositions[i] = transforms[i].localPosition;
            }
            
            // Generate a random offset based on instance
            RandomizeOffset();
        }
        
        /// <summary>
        /// Sets noise parameters.
        /// </summary>
        /// <param name="frequency">Noise spatial frequency (lower = larger patterns)</param>
        /// <param name="amplitude">Displacement amplitude</param>
        /// <param name="timeScale">Speed of noise evolution</param>
        public void SetParameters(float frequency, float amplitude, float timeScale)
        {
            _frequency = frequency;
            _amplitude = amplitude;
            _timeScale = timeScale;
        }
        
        /// <summary>
        /// Sets which axes receive noise displacement.
        /// </summary>
        public void SetAxisMask(bool x, bool y, bool z)
        {
            _axisMask = new bool3(x, y, z);
        }
        
        /// <summary>
        /// Sets a random offset for the noise field.
        /// </summary>
        public void RandomizeOffset()
        {
            var random = new Unity.Mathematics.Random((uint)(DateTime.Now.Ticks & 0xFFFFFFFF));
            _offset = random.NextFloat3() * 1000f;
        }
        
        /// <summary>
        /// Sets a specific offset for the noise field (for deterministic behavior).
        /// </summary>
        public void SetOffset(float3 offset)
        {
            _offset = offset;
        }
        
        /// <summary>
        /// Resets the time accumulator.
        /// </summary>
        public void ResetTime()
        {
            _time = 0f;
        }
        
        /// <summary>
        /// Updates the original positions from current transform positions.
        /// Call this if the transforms have been moved externally.
        /// </summary>
        public void RefreshOriginalPositions(Transform[] transforms)
        {
            for (int i = 0; i < math.min(transforms.Length, _originalPositions.Length); i++)
            {
                _originalPositions[i] = transforms[i].localPosition;
            }
        }
        
        /// <summary>
        /// Enables or disables updates.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _needsUpdate = enabled;
        }
        
        /// <inheritdoc/>
        public void Prepare(float deltaTime)
        {
            _time += deltaTime * _timeScale;
        }
        
        /// <inheritdoc/>
        public JobHandle Schedule(JobHandle dependency = default)
        {
            if (!_transforms.isCreated) return dependency;
            
            var job = new NoiseTransformJob
            {
                OriginalPositions = _originalPositions,
                Time = _time,
                Frequency = _frequency,
                Amplitude = _amplitude,
                NoiseOffset = _offset,
                AxisMask = _axisMask
            };
            
            return job.Schedule(_transforms, dependency);
        }
        
        /// <inheritdoc/>
        public void Apply()
        {
            // NoiseTransformJob applies directly via IJobParallelForTransform
            // No additional work needed here
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            if (_transforms.isCreated) _transforms.Dispose();
            if (_originalPositions.IsCreated) _originalPositions.Dispose();
        }
    }
    
    /// <summary>
    /// Job that applies combined spring + noise motion to transforms.
    /// </summary>
    [BurstCompile]
    public struct SpringNoiseTransformJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float3> Targets;
        public NativeArray<float3> Velocities;
        
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float K1;
        [ReadOnly] public float K2;
        [ReadOnly] public float K3;
        
        [ReadOnly] public float NoiseTime;
        [ReadOnly] public float NoiseFrequency;
        [ReadOnly] public float NoiseAmplitude;
        [ReadOnly] public float3 NoiseOffset;
        
        public void Execute(int index, TransformAccess transform)
        {
            float3 target = Targets[index];
            float3 position = transform.localPosition;
            float3 velocity = Velocities[index];
            
            // Spring dynamics
            float k2Stable = SpringMath.ComputeStableK2(K1, K2, DeltaTime);
            
            position += DeltaTime * velocity;
            velocity += DeltaTime * (target - position - K1 * velocity) / k2Stable;
            
            // Add noise on top
            float3 noiseCoord = (position + NoiseOffset) * NoiseFrequency;
            float nx = BurstNoise.Sample4D(noiseCoord.x, noiseCoord.y, noiseCoord.z, NoiseTime);
            float ny = BurstNoise.Sample4D(noiseCoord.x + 100f, noiseCoord.y + 100f, noiseCoord.z, NoiseTime);
            float nz = BurstNoise.Sample4D(noiseCoord.x, noiseCoord.y + 200f, noiseCoord.z + 200f, NoiseTime);
            
            float3 noiseDisplacement = new float3(nx, ny, nz) * NoiseAmplitude;
            
            transform.localPosition = position + noiseDisplacement;
            Velocities[index] = velocity;
        }
    }
    
    /// <summary>
    /// A managed job that combines spring motion with noise overlay.
    /// Useful for following a target with added organic movement.
    /// </summary>
    public class ManagedSpringNoiseJob : IProceduralAnimationJob, IDisposable
    {
        private TransformAccessArray _transforms;
        private NativeArray<float3> _targets;
        private NativeArray<float3> _velocities;
        
        private SpringCoefficients _coefficients;
        private float _deltaTime;
        
        private float _noiseFrequency = 0.5f;
        private float _noiseAmplitude = 0.05f;
        private float _noiseTimeScale = 0.5f;
        private float _noiseTime;
        private float3 _noiseOffset;
        
        private bool _isDisposed;
        private bool _needsUpdate = true;
        
        /// <summary>
        /// Number of transforms in this job.
        /// </summary>
        public int Count => _transforms.isCreated ? _transforms.length : 0;
        
        /// <inheritdoc/>
        public bool NeedsUpdate => _needsUpdate && !_isDisposed && _transforms.isCreated;
        
        /// <summary>
        /// Creates a new managed spring+noise job for the given transforms.
        /// </summary>
        public ManagedSpringNoiseJob(Transform[] transforms, SpringPreset preset = SpringPreset.Smooth)
        {
            _transforms = new TransformAccessArray(transforms);
            _targets = new NativeArray<float3>(transforms.Length, Allocator.Persistent);
            _velocities = new NativeArray<float3>(transforms.Length, Allocator.Persistent);
            _coefficients = SpringCoefficients.FromPreset(preset);
            
            for (int i = 0; i < transforms.Length; i++)
            {
                _targets[i] = transforms[i].localPosition;
            }
            
            var random = new Unity.Mathematics.Random((uint)(DateTime.Now.Ticks & 0xFFFFFFFF));
            _noiseOffset = random.NextFloat3() * 1000f;
        }
        
        /// <summary>
        /// Sets the spring parameters.
        /// </summary>
        public void SetSpringParameters(float frequency, float damping, float response)
        {
            _coefficients = SpringCoefficients.Create(frequency, damping, response);
        }
        
        /// <summary>
        /// Sets the noise parameters.
        /// </summary>
        public void SetNoiseParameters(float frequency, float amplitude, float timeScale)
        {
            _noiseFrequency = frequency;
            _noiseAmplitude = amplitude;
            _noiseTimeScale = timeScale;
        }
        
        /// <summary>
        /// Sets the target position for a specific transform.
        /// </summary>
        public void SetTarget(int index, float3 target)
        {
            if (index >= 0 && index < _targets.Length)
            {
                _targets[index] = target;
            }
        }
        
        /// <summary>
        /// Sets target positions for all transforms.
        /// </summary>
        public void SetTargets(float3[] targets)
        {
            for (int i = 0; i < math.min(targets.Length, _targets.Length); i++)
            {
                _targets[i] = targets[i];
            }
        }
        
        /// <summary>
        /// Enables or disables updates.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _needsUpdate = enabled;
        }
        
        /// <inheritdoc/>
        public void Prepare(float deltaTime)
        {
            _deltaTime = deltaTime;
            _noiseTime += deltaTime * _noiseTimeScale;
        }
        
        /// <inheritdoc/>
        public JobHandle Schedule(JobHandle dependency = default)
        {
            if (!_transforms.isCreated) return dependency;
            
            var job = new SpringNoiseTransformJob
            {
                Targets = _targets,
                Velocities = _velocities,
                DeltaTime = _deltaTime,
                K1 = _coefficients.K1,
                K2 = _coefficients.K2,
                K3 = _coefficients.K3,
                NoiseTime = _noiseTime,
                NoiseFrequency = _noiseFrequency,
                NoiseAmplitude = _noiseAmplitude,
                NoiseOffset = _noiseOffset
            };
            
            return job.Schedule(_transforms, dependency);
        }
        
        /// <inheritdoc/>
        public void Apply()
        {
            // Job applies directly via IJobParallelForTransform
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            if (_transforms.isCreated) _transforms.Dispose();
            if (_targets.IsCreated) _targets.Dispose();
            if (_velocities.IsCreated) _velocities.Dispose();
        }
    }
}
