using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Eraflo.Catalyst.ProceduralAnimation.Jobs
{
    /// <summary>
    /// Base interface for all procedural animation jobs.
    /// Provides a standard contract for job execution.
    /// </summary>
    public interface IProceduralAnimationJob
    {
        /// <summary>
        /// Whether this job needs to run this frame.
        /// </summary>
        bool NeedsUpdate { get; }
        
        /// <summary>
        /// Prepares the job data before scheduling.
        /// Called on the main thread.
        /// </summary>
        void Prepare(float deltaTime);
        
        /// <summary>
        /// Schedules the job and returns the handle.
        /// </summary>
        JobHandle Schedule(JobHandle dependency = default);
        
        /// <summary>
        /// Applies the results after job completion.
        /// Called on the main thread.
        /// </summary>
        void Apply();
        
        /// <summary>
        /// Cleans up any native resources.
        /// </summary>
        void Dispose();
    }
    
    /// <summary>
    /// Job that updates multiple SpringMotion instances in parallel.
    /// </summary>
    [BurstCompile]
    public struct SpringUpdateJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> Targets;
        public NativeArray<float3> Positions;
        public NativeArray<float3> Velocities;
        
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float K1;
        [ReadOnly] public float K2;
        [ReadOnly] public float K3;
        
        public void Execute(int index)
        {
            float3 target = Targets[index];
            float3 position = Positions[index];
            float3 velocity = Velocities[index];
            
            float k2Stable = SpringMath.ComputeStableK2(K1, K2, DeltaTime);
            
            position += DeltaTime * velocity;
            velocity += DeltaTime * (target - position - K1 * velocity) / k2Stable;
            
            Positions[index] = position;
            Velocities[index] = velocity;
        }
    }
    
    /// <summary>
    /// Job that updates transform positions using SpringMotion dynamics.
    /// </summary>
    [BurstCompile]
    public struct SpringTransformJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float3> Targets;
        public NativeArray<float3> Velocities;
        
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float K1;
        [ReadOnly] public float K2;
        [ReadOnly] public float K3;
        
        public void Execute(int index, TransformAccess transform)
        {
            float3 target = Targets[index];
            float3 position = transform.localPosition;
            float3 velocity = Velocities[index];
            
            float k2Stable = SpringMath.ComputeStableK2(K1, K2, DeltaTime);
            
            position += DeltaTime * velocity;
            velocity += DeltaTime * (target - position - K1 * velocity) / k2Stable;
            
            transform.localPosition = position;
            Velocities[index] = velocity;
        }
    }
}

