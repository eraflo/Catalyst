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
    /// Burst-compiled FABRIK IK solver job.
    /// Solves multiple IK chains in parallel.
    /// </summary>
    [BurstCompile]
    public struct FABRIKJob : IJobFor
    {
        // Per-chain data (indices into joint arrays)
        [ReadOnly] public NativeArray<int2> ChainRanges;        // start, length per chain
        [ReadOnly] public NativeArray<float3> RootPositions;    // root position per chain
        [ReadOnly] public NativeArray<float3> TargetPositions;  // target per chain
        [ReadOnly] public NativeArray<float> BoneLengths;       // all bone lengths flattened
        
        // Joint data (flattened for all chains)
        public NativeArray<float3> JointPositions;
        
        // Solver config
        [ReadOnly] public int MaxIterations;
        [ReadOnly] public float Tolerance;
        
        public void Execute(int chainIndex)
        {
            int2 range = ChainRanges[chainIndex];
            int start = range.x;
            int count = range.y;
            
            if (count < 2) return;
            
            float3 root = RootPositions[chainIndex];
            float3 target = TargetPositions[chainIndex];
            
            // Calculate total length
            float totalLength = 0f;
            for (int i = 0; i < count - 1; i++)
            {
                totalLength += BoneLengths[start + i];
            }
            
            float distToTarget = math.distance(root, target);
            
            // If target unreachable, stretch toward it
            if (distToTarget > totalLength)
            {
                float3 dir = math.normalizesafe(target - root);
                JointPositions[start] = root;
                for (int i = 1; i < count; i++)
                {
                    JointPositions[start + i] = JointPositions[start + i - 1] + dir * BoneLengths[start + i - 1];
                }
                return;
            }
            
            // FABRIK iterations
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // Check if close enough
                float error = math.distance(JointPositions[start + count - 1], target);
                if (error < Tolerance) break;
                
                // Forward reaching (end to root)
                JointPositions[start + count - 1] = target;
                for (int i = count - 2; i >= 0; i--)
                {
                    float boneLen = BoneLengths[start + i];
                    float3 dir = math.normalizesafe(JointPositions[start + i] - JointPositions[start + i + 1]);
                    JointPositions[start + i] = JointPositions[start + i + 1] + dir * boneLen;
                }
                
                // Backward reaching (root to end)
                JointPositions[start] = root;
                for (int i = 1; i < count; i++)
                {
                    float boneLen = BoneLengths[start + i - 1];
                    float3 dir = math.normalizesafe(JointPositions[start + i] - JointPositions[start + i - 1]);
                    JointPositions[start + i] = JointPositions[start + i - 1] + dir * boneLen;
                }
            }
        }
    }
    
    /// <summary>
    /// Converts joint positions to rotations.
    /// </summary>
    [BurstCompile]
    public struct PositionToRotationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int2> ChainRanges;
        [ReadOnly] public NativeArray<float3> JointPositions;
        [ReadOnly] public NativeArray<float3> UpVectors;
        
        public NativeArray<quaternion> Rotations;
        
        public void Execute(int jointIndex)
        {
            // Find which chain this joint belongs to
            int chainIndex = 0;
            int offsetInChain = jointIndex;
            
            for (int c = 0; c < ChainRanges.Length; c++)
            {
                int2 range = ChainRanges[c];
                if (jointIndex >= range.x && jointIndex < range.x + range.y)
                {
                    chainIndex = c;
                    offsetInChain = jointIndex - range.x;
                    break;
                }
            }
            
            int2 myRange = ChainRanges[chainIndex];
            int lastInChain = myRange.x + myRange.y - 1;
            
            // Calculate rotation to look at next joint
            float3 pos = JointPositions[jointIndex];
            float3 up = UpVectors[chainIndex];
            
            if (jointIndex < lastInChain)
            {
                float3 nextPos = JointPositions[jointIndex + 1];
                float3 forward = math.normalizesafe(nextPos - pos);
                
                if (math.lengthsq(forward) > 0.001f)
                {
                    Rotations[jointIndex] = quaternion.LookRotation(forward, up);
                }
            }
            else
            {
                // Last joint uses previous joint's direction
                if (jointIndex > myRange.x)
                {
                    Rotations[jointIndex] = Rotations[jointIndex - 1];
                }
            }
        }
    }
    
    /// <summary>
    /// Applies solved rotations to transforms.
    /// </summary>
    [BurstCompile]
    public struct ApplyRotationsJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<quaternion> Rotations;
        [ReadOnly] public NativeArray<quaternion> OriginalRotations;
        [ReadOnly] public float BlendWeight;
        
        public void Execute(int index, TransformAccess transform)
        {
            quaternion targetRot = Rotations[index];
            quaternion originalRot = OriginalRotations[index];
            
            transform.rotation = math.slerp(originalRot, targetRot, BlendWeight);
        }
    }
    
    /// <summary>
    /// Locomotion foot update job.
    /// </summary>
    [BurstCompile]
    public struct FootPlacementJob : IJobParallelFor
    {
        // Per-foot input
        [ReadOnly] public NativeArray<float3> TargetPositions;
        [ReadOnly] public NativeArray<bool> IsInSwing;
        [ReadOnly] public NativeArray<float> SwingProgress;
        [ReadOnly] public NativeArray<float3> SwingStartPositions;
        [ReadOnly] public NativeArray<float> StepHeights;
        
        // Spring state
        public NativeArray<float3> Positions;
        public NativeArray<float3> Velocities;
        
        // Config
        [ReadOnly] public SpringCoefficients SpringConfig;
        [ReadOnly] public float DeltaTime;
        
        public void Execute(int index)
        {
            float3 target;
            
            if (IsInSwing[index])
            {
                // Calculate arc position
                float t = SwingProgress[index];
                float3 start = SwingStartPositions[index];
                float3 end = TargetPositions[index];
                
                float3 horizontal = math.lerp(start, end, t);
                float arcHeight = math.sin(t * math.PI) * StepHeights[index];
                float baseHeight = math.lerp(start.y, end.y, t);
                
                target = new float3(horizontal.x, baseHeight + arcHeight, horizontal.z);
            }
            else
            {
                target = TargetPositions[index];
            }
            
            // Spring update
            float3 pos = Positions[index];
            float3 vel = Velocities[index];
            
            float k2Stable = SpringMath.ComputeStableK2(SpringConfig.K1, SpringConfig.K2, DeltaTime);
            
            pos += DeltaTime * vel;
            vel += DeltaTime * (target - pos - SpringConfig.K1 * vel) / k2Stable;
            
            Positions[index] = pos;
            Velocities[index] = vel;
        }
    }
    
    /// <summary>
    /// Body balance job - computes body position/rotation from foot positions.
    /// </summary>
    [BurstCompile]
    public struct BodyBalanceJob : IJob
    {
        [ReadOnly] public NativeArray<float3> FootPositions;
        [ReadOnly] public NativeArray<bool> FootGrounded;
        [ReadOnly] public float3 Velocity;
        [ReadOnly] public float GaitPhase;
        [ReadOnly] public float TargetHeight;
        [ReadOnly] public float BobbingAmplitude;
        [ReadOnly] public float MaxTiltAngle;
        [ReadOnly] public float MovementLeanAngle;
        
        public NativeArray<float3> OutputPosition;       // length 1
        public NativeArray<quaternion> OutputRotation;   // length 1
        
        public void Execute()
        {
            // Calculate support center
            float3 center = float3.zero;
            int groundedCount = 0;
            
            for (int i = 0; i < FootPositions.Length; i++)
            {
                if (i < FootGrounded.Length && FootGrounded[i])
                {
                    center += FootPositions[i];
                    groundedCount++;
                }
            }
            
            if (groundedCount == 0)
            {
                for (int i = 0; i < FootPositions.Length; i++)
                    center += FootPositions[i];
                groundedCount = FootPositions.Length;
            }
            
            if (groundedCount > 0)
                center /= groundedCount;
            
            // Calculate ground height
            float groundHeight = 0f;
            for (int i = 0; i < FootPositions.Length && i < FootGrounded.Length; i++)
            {
                if (FootGrounded[i])
                    groundHeight += FootPositions[i].y;
            }
            if (groundedCount > 0)
                groundHeight /= groundedCount;
            
            // Bobbing
            float bob = math.cos(GaitPhase * math.PI * 4f) * BobbingAmplitude;
            
            // Output position
            OutputPosition[0] = new float3(center.x, groundHeight + TargetHeight + bob, center.z);
            
            // Calculate rotation
            float speed = math.length(Velocity);
            float3 forward = speed > 0.1f 
                ? math.normalizesafe(new float3(Velocity.x, 0, Velocity.z))
                : new float3(0, 0, 1);
            
            // Lean into movement
            float3 leanAxis = math.cross(new float3(0, 1, 0), forward);
            float leanAngle = speed * math.radians(MovementLeanAngle) * 0.1f;
            
            quaternion baseRot = quaternion.LookRotation(forward, new float3(0, 1, 0));
            quaternion leanRot = quaternion.AxisAngle(leanAxis, leanAngle);
            
            OutputRotation[0] = math.mul(leanRot, baseRot);
        }
    }
    
    /// <summary>
    /// Computes muscle target rotations in parallel.
    /// </summary>
    [BurstCompile]
    public struct MuscleComputeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<quaternion> AnimatedRotations;
        [ReadOnly] public NativeArray<quaternion> CurrentRotations;
        [ReadOnly] public float Strength;
        
        public NativeArray<quaternion> TargetRotations;
        public NativeArray<float3> AngularErrors;
        
        public void Execute(int index)
        {
            quaternion animated = AnimatedRotations[index];
            quaternion current = CurrentRotations[index];
            
            // Blend target based on strength
            quaternion target = math.slerp(current, animated, Strength);
            TargetRotations[index] = target;
            
            // Calculate angular error for PD controller
            quaternion error = math.mul(target, math.conjugate(current));
            
            // Ensure shortest path
            if (error.value.w < 0)
                error = new quaternion(-error.value);
            
            // Convert to axis-angle
            float sinHalf = math.length(error.value.xyz);
            if (sinHalf > 0.0001f)
            {
                float angle = 2f * math.asin(math.clamp(sinHalf, -1f, 1f));
                float3 axis = error.value.xyz / sinHalf;
                AngularErrors[index] = axis * angle;
            }
            else
            {
                AngularErrors[index] = float3.zero;
            }
        }
    }
    
    /// <summary>
    /// Verlet spine simulation job for tails, tentacles, snakes.
    /// </summary>
    [BurstCompile]
    public struct VerletSpineJob : IJob
    {
        public NativeArray<float3> Positions;
        public NativeArray<float3> PreviousPositions;
        [ReadOnly] public NativeArray<float> BoneLengths;
        public NativeArray<float3> OutputPositions;
        
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float Damping;
        [ReadOnly] public float Gravity;
        [ReadOnly] public int ConstraintIterations;
        [ReadOnly] public float FollowStrength;
        [ReadOnly] public float FollowDelay;
        [ReadOnly] public bool EnableNoise;
        [ReadOnly] public float NoiseFrequency;
        [ReadOnly] public float NoiseAmplitude;
        [ReadOnly] public float Time;
        
        public void Execute()
        {
            int count = Positions.Length;
            if (count < 2) return;
            
            float dt2 = DeltaTime * DeltaTime;
            float dampFactor = 1f - Damping;
            
            // First bone is fixed to leader - already set in Prepare
            OutputPositions[0] = Positions[0];
            
            // Verlet integration for remaining bones
            for (int i = 1; i < count; i++)
            {
                float3 pos = Positions[i];
                float3 prevPos = PreviousPositions[i];
                
                // Velocity from position difference
                float3 velocity = (pos - prevPos) * dampFactor;
                
                // Gravity
                float3 acceleration = new float3(0, Gravity, 0);
                
                // Noise wiggle using BurstNoise from Noise module
                if (EnableNoise)
                {
                    float noiseOffset = i * 0.5f;
                    // Use 4D Simplex noise with time for smooth animation
                    float nx = BurstNoise.Sample4D(pos.x * NoiseFrequency, pos.y * NoiseFrequency, pos.z * NoiseFrequency + noiseOffset, Time);
                    float ny = BurstNoise.Sample4D(pos.x * NoiseFrequency + 100f, pos.y * NoiseFrequency, pos.z * NoiseFrequency, Time * 1.3f);
                    float nz = BurstNoise.Sample4D(pos.x * NoiseFrequency, pos.y * NoiseFrequency + 200f, pos.z * NoiseFrequency, Time * 0.8f);
                    
                    acceleration += new float3(nx, ny * 0.5f, nz) * NoiseAmplitude;
                }
                
                // New position
                float3 newPos = pos + velocity + acceleration * dt2;
                OutputPositions[i] = newPos;
            }
            
            // Distance constraint solving
            for (int iter = 0; iter < ConstraintIterations; iter++)
            {
                for (int i = 0; i < count - 1; i++)
                {
                    float3 p1 = OutputPositions[i];
                    float3 p2 = OutputPositions[i + 1];
                    
                    float3 delta = p2 - p1;
                    float dist = math.length(delta);
                    float targetDist = BoneLengths[i];
                    
                    if (dist > 0.0001f)
                    {
                        float error = dist - targetDist;
                        float3 correction = math.normalize(delta) * error * 0.5f;
                        
                        // First bone doesn't move (anchored)
                        if (i > 0)
                        {
                            OutputPositions[i] = p1 + correction * FollowStrength;
                        }
                        OutputPositions[i + 1] = p2 - correction;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Comfort pose optimization job - blends rotations toward rest pose.
    /// </summary>
    [BurstCompile]
    public struct ComfortOptimizeJob : IJobParallelFor
    {
        public NativeArray<quaternion> Rotations;
        [ReadOnly] public NativeArray<quaternion> RestRotations;
        [ReadOnly] public NativeArray<float> JointWeights;
        
        [ReadOnly] public float ComfortWeight;
        [ReadOnly] public float MaxDeviationRadians;
        [ReadOnly] public bool UseSoftLimits;
        [ReadOnly] public float LimitSoftness;
        
        public void Execute(int index)
        {
            if (index >= RestRotations.Length) return;
            
            quaternion current = Rotations[index];
            quaternion rest = RestRotations[index];
            float jointWeight = index < JointWeights.Length ? JointWeights[index] : 1f;
            
            // Blend toward rest pose
            quaternion blended = math.slerp(current, rest, ComfortWeight * jointWeight);
            
            // Apply angular limits
            quaternion diff = math.mul(blended, math.conjugate(rest));
            
            if (diff.value.w < 0)
                diff = new quaternion(-diff.value);
            
            float sinHalf = math.length(diff.value.xyz);
            if (sinHalf > 0.0001f)
            {
                float angle = 2f * math.asin(math.clamp(sinHalf, 0f, 1f));
                
                if (angle > MaxDeviationRadians)
                {
                    float3 axis = diff.value.xyz / sinHalf;
                    float clampedAngle = MaxDeviationRadians;
                    
                    if (UseSoftLimits)
                    {
                        float excess = angle - MaxDeviationRadians;
                        clampedAngle = MaxDeviationRadians + excess * LimitSoftness;
                        clampedAngle = math.min(clampedAngle, MaxDeviationRadians * 1.5f);
                    }
                    
                    quaternion clampedDiff = quaternion.AxisAngle(axis, clampedAngle);
                    blended = math.mul(clampedDiff, rest);
                }
            }
            
            Rotations[index] = blended;
        }
    }
}
