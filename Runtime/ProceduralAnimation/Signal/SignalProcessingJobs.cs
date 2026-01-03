using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Eraflo.Catalyst.ProceduralAnimation.SignalProcessing
{    
    /// <summary>
    /// Burst-compiled job for decaying inertialization offsets.
    /// </summary>
    [BurstCompile]
    public struct InertializationDecayJob : IJobParallelFor
    {
        public NativeArray<float3> PositionOffsets;
        public NativeArray<float3> PositionVelocities;
        public NativeArray<float3> RotationOffsets;
        public NativeArray<float3> RotationVelocities;
        
        [ReadOnly] public float DecayFactor;
        
        public void Execute(int index)
        {
            PositionOffsets[index] *= DecayFactor;
            PositionVelocities[index] *= DecayFactor;
            RotationOffsets[index] *= DecayFactor;
            RotationVelocities[index] *= DecayFactor;
        }
    }
    
    /// <summary>
    /// Burst-compiled job for exponential smoothing.
    /// </summary>
    [BurstCompile]
    public struct ExponentialSmoothingJob : IJobParallelFor
    {
        public NativeArray<float3> CurrentPositions;
        public NativeArray<quaternion> CurrentRotations;
        
        [ReadOnly] public NativeArray<float3> TargetPositions;
        [ReadOnly] public NativeArray<quaternion> TargetRotations;
        
        [ReadOnly] public float SmoothingFactor;
        [ReadOnly] public float DeltaTime;
        
        public void Execute(int index)
        {
            float factor = 1f - math.pow(SmoothingFactor, DeltaTime);
            
            CurrentPositions[index] = math.lerp(CurrentPositions[index], TargetPositions[index], factor);
            CurrentRotations[index] = math.slerp(CurrentRotations[index], TargetRotations[index], factor);
        }
    }
    
    /// <summary>
    /// Burst-compiled job for additive pose blending.
    /// </summary>
    [BurstCompile]
    public struct AdditiveBlendJob : IJobParallelFor
    {
        public NativeArray<float3> BasePositions;
        public NativeArray<quaternion> BaseRotations;
        
        [ReadOnly] public NativeArray<float3> AdditivePositions;
        [ReadOnly] public NativeArray<quaternion> AdditiveRotations;
        
        [ReadOnly] public float AdditiveWeight;
        
        public void Execute(int index)
        {
            // Add scaled position offset
            BasePositions[index] += AdditivePositions[index] * AdditiveWeight;
            
            // Blend rotation additively (slerp from identity scaled by weight)
            quaternion addRot = AdditiveRotations[index];
            quaternion scaledAdd = math.slerp(quaternion.identity, addRot, AdditiveWeight);
            BaseRotations[index] = math.mul(scaledAdd, BaseRotations[index]);
        }
    }
    
    /// <summary>
    /// Burst-compiled job for computing pose velocities from position deltas.
    /// </summary>
    [BurstCompile]
    public struct ComputeVelocityJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> CurrentPositions;
        [ReadOnly] public NativeArray<float3> PreviousPositions;
        [ReadOnly] public NativeArray<quaternion> CurrentRotations;
        [ReadOnly] public NativeArray<quaternion> PreviousRotations;
        
        public NativeArray<float3> Velocities;
        public NativeArray<float3> AngularVelocities;
        
        [ReadOnly] public float InverseDeltaTime;
        
        public void Execute(int index)
        {
            // Linear velocity
            Velocities[index] = (CurrentPositions[index] - PreviousPositions[index]) * InverseDeltaTime;
            
            // Angular velocity (convert rotation difference to axis-angle per second)
            quaternion delta = math.mul(CurrentRotations[index], math.conjugate(PreviousRotations[index]));
            
            // Ensure shortest path
            if (delta.value.w < 0)
                delta = new quaternion(-delta.value);
            
            delta = math.normalizesafe(delta);
            
            float sinHalfAngle = math.length(delta.value.xyz);
            if (sinHalfAngle > 0.0001f)
            {
                float halfAngle = math.asin(math.clamp(sinHalfAngle, -1f, 1f));
                float3 axis = delta.value.xyz / sinHalfAngle;
                AngularVelocities[index] = axis * (2f * halfAngle * InverseDeltaTime);
            }
            else
            {
                AngularVelocities[index] = float3.zero;
            }
        }
    }
}
