using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Eraflo.Catalyst.Noise;

namespace Eraflo.Catalyst.ProceduralAnimation.Jobs
{
    /// <summary>
    /// Job that applies noise displacement to positions.
    /// Useful for breathing, idle motion, wind effects.
    /// </summary>
    [BurstCompile]
    public struct NoiseDisplacementJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> OriginalPositions;
        public NativeArray<float3> Positions;
        
        [ReadOnly] public float Time;
        [ReadOnly] public float Frequency;
        [ReadOnly] public float Amplitude;
        [ReadOnly] public float3 NoiseOffset;
        
        public void Execute(int index)
        {
            float3 pos = OriginalPositions[index];
            float3 noiseCoord = (pos + NoiseOffset) * Frequency;
            
            // Sample noise for each axis with different offsets
            float nx = BurstNoise.Sample4D(noiseCoord.x, noiseCoord.y, noiseCoord.z, Time);
            float ny = BurstNoise.Sample4D(noiseCoord.x + 100f, noiseCoord.y + 100f, noiseCoord.z, Time);
            float nz = BurstNoise.Sample4D(noiseCoord.x, noiseCoord.y + 200f, noiseCoord.z + 200f, Time);
            
            Positions[index] = pos + new float3(nx, ny, nz) * Amplitude;
        }
    }
    
    /// <summary>
    /// Job that applies exponential decay to velocities.
    /// Used for damping motion.
    /// </summary>
    [BurstCompile]
    public struct VelocityDecayJob : IJobParallelFor
    {
        public NativeArray<float3> Velocities;
        
        [ReadOnly] public float DecayFactor;
        
        public void Execute(int index)
        {
            Velocities[index] *= DecayFactor;
        }
    }
    
    /// <summary>
    /// Job that blends between two pose arrays.
    /// </summary>
    [BurstCompile]
    public struct PoseBlendJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> PositionsA;
        [ReadOnly] public NativeArray<quaternion> RotationsA;
        [ReadOnly] public NativeArray<float3> PositionsB;
        [ReadOnly] public NativeArray<quaternion> RotationsB;
        
        public NativeArray<float3> OutputPositions;
        public NativeArray<quaternion> OutputRotations;
        
        [ReadOnly] public float BlendFactor;
        
        public void Execute(int index)
        {
            OutputPositions[index] = math.lerp(PositionsA[index], PositionsB[index], BlendFactor);
            OutputRotations[index] = math.slerp(RotationsA[index], RotationsB[index], BlendFactor);
        }
    }
    
    /// <summary>
    /// Job that clamps positions within bounds.
    /// </summary>
    [BurstCompile]
    public struct BoundsClampJob : IJobParallelFor
    {
        public NativeArray<float3> Positions;
        
        [ReadOnly] public float3 MinBounds;
        [ReadOnly] public float3 MaxBounds;
        
        public void Execute(int index)
        {
            Positions[index] = math.clamp(Positions[index], MinBounds, MaxBounds);
        }
    }
    
    /// <summary>
    /// Job that applies inertial blending for smooth transitions.
    /// Implementation of inertialization algorithm.
    /// </summary>
    [BurstCompile]
    public struct InertializationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> TargetPositions;
        [ReadOnly] public NativeArray<float3> OffsetPositions;
        [ReadOnly] public NativeArray<float3> OffsetVelocities;
        
        public NativeArray<float3> OutputPositions;
        public NativeArray<float3> OutputVelocities;
        
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float HalfLife;
        
        public void Execute(int index)
        {
            float3 target = TargetPositions[index];
            float3 offset = OffsetPositions[index];
            float3 offsetVel = OffsetVelocities[index];
            
            // Exponential decay of offset
            float decay = math.pow(0.5f, DeltaTime / math.max(HalfLife, 0.001f));
            
            float3 newOffset = offset * decay;
            float3 newOffsetVel = offsetVel * decay;
            
            OutputPositions[index] = target + newOffset;
            OutputVelocities[index] = newOffsetVel;
        }
    }
    
    /// <summary>
    /// Job that computes distance constraints (for Verlet integration).
    /// </summary>
    [BurstCompile]
    public struct DistanceConstraintJob : IJobParallelFor
    {
        public NativeArray<float3> Positions;
        
        [ReadOnly] public NativeArray<int> ParentIndices;
        [ReadOnly] public NativeArray<float> TargetDistances;
        [ReadOnly] public float Stiffness;
        
        public void Execute(int index)
        {
            int parentIndex = ParentIndices[index];
            if (parentIndex < 0) return;
            
            float3 pos = Positions[index];
            float3 parentPos = Positions[parentIndex];
            float targetDist = TargetDistances[index];
            
            float3 delta = pos - parentPos;
            float currentDist = math.length(delta);
            
            if (currentDist < 0.0001f) return;
            
            float error = currentDist - targetDist;
            float3 correction = math.normalize(delta) * error * Stiffness * 0.5f;
            
            Positions[index] = pos - correction;
        }
    }
}
