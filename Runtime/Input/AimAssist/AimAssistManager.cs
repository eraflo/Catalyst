using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using Eraflo.Catalyst.Spatial.Native;

namespace Eraflo.Catalyst.Input.AimAssist
{
    /// <summary>
    /// Highly optimized manager for Aim Assist logic using Burst and Jobs.
    /// </summary>
    [Service(Priority = 45)]
    public class AimAssistManager : IAimAssistService, IUpdatable
    {
        private AimAssistConfig _config;
        private readonly List<TargetableEntity> _entities = new();

        // Native data for Jobs
        private NativeList<TargetData> _targetData;
        private NativeArray<float> _frictionLUT;
        private NativeArray<float> _magnetismLUT;
        private NativeSpatialHash _spatialHash;

        private const int DefaultCapacity = 1000;

        public void Initialize()
        {
            _targetData = new NativeList<TargetData>(Allocator.Persistent);
            _spatialHash = new NativeSpatialHash(DefaultCapacity, 10f, Allocator.Persistent);
        }

        public void Shutdown()
        {
            if (_targetData.IsCreated) _targetData.Dispose();
            if (_frictionLUT.IsCreated) _frictionLUT.Dispose();
            if (_magnetismLUT.IsCreated) _magnetismLUT.Dispose();
            if (_spatialHash.IsCreated) _spatialHash.Dispose();
        }

        /// <summary>
        /// Sets the configuration and bakes the curves.
        /// </summary>
        public void SetConfig(AimAssistConfig config)
        {
            _config = config;
            if (_frictionLUT.IsCreated) _frictionLUT.Dispose();
            if (_magnetismLUT.IsCreated) _magnetismLUT.Dispose();

            if (_config != null)
            {
                _config.BakeCurves(out _frictionLUT, out _magnetismLUT, Allocator.Persistent);
            }
        }

        public void Register(TargetableEntity entity)
        {
            if (!_entities.Contains(entity))
                _entities.Add(entity);
        }

        public void Unregister(TargetableEntity entity)
        {
            _entities.Remove(entity);
        }

        public void OnUpdate()
        {
            if (_entities.Count == 0) return;

            _targetData.Clear();
            _spatialHash.Clear();

            for (int i = 0; i < _entities.Count; i++)
            {
                var entity = _entities[i];
                if (entity == null) continue;

                var data = new TargetData
                {
                    Position = entity.Position,
                    TeamID = entity.TeamID
                };

                _targetData.Add(data);
                _spatialHash.Add(i, data.Position);
            }
        }

        public Vector2 ApplyAssist(Vector2 rawInput, Vector3 sourcePosition, Vector3 forward, Camera cam, float deltaTime, int sourceTeamID = -1)
        {
            if (_config == null || _targetData.Length == 0) return rawInput;

            // 1. Spatial Culling on main thread (very fast query)
            var candidates = new NativeList<int>(Allocator.TempJob);
            _spatialHash.QueryRadius(sourcePosition, _config.MaxDistance, candidates);

            if (candidates.Length == 0)
            {
                candidates.Dispose();
                return rawInput;
            }

            // 2. Schedule Burst Job for precise math
            var resultRef = new NativeReference<float2>(Allocator.TempJob);

            var job = new AimAssistJob
            {
                Targets = _targetData.AsArray(),
                Candidates = candidates.AsArray(),
                FrictionLUT = _frictionLUT,
                MagnetismLUT = _magnetismLUT,
                
                SourcePosition = sourcePosition,
                Forward = forward,
                MaxDistance = _config.MaxDistance,
                ConeAngle = _config.ConeAngle,
                MagnetismStrength = _config.MagnetismStrength,
                MagnetismSmoothing = _config.MagnetismSmoothing,
                DeltaTime = deltaTime,
                RawInput = rawInput,
                ViewProjectionMatrix = cam.projectionMatrix * cam.worldToCameraMatrix,
                SourceTeamID = sourceTeamID,
                
                ResultInput = resultRef
            };

            job.Run();

            Vector2 assistedInput = resultRef.Value;
            
            resultRef.Dispose();
            candidates.Dispose();

            return assistedInput;
        }

    }


    /// <summary>
    /// DOD-friendly target data.
    /// </summary>
    public struct TargetData
    {
        public float3 Position;
        public int TeamID;
    }

    /// <summary>
    /// Burst-compiled job for calculating aim assist.
    /// </summary>
    [BurstCompile]
    public struct AimAssistJob : IJob
    {
        [ReadOnly] public NativeArray<TargetData> Targets;
        [ReadOnly] public NativeArray<int> Candidates;
        [ReadOnly] public NativeArray<float> FrictionLUT;
        [ReadOnly] public NativeArray<float> MagnetismLUT;

        public float3 SourcePosition;
        public float3 Forward;
        public float MaxDistance;
        public float ConeAngle;
        public float MagnetismStrength;
        public float MagnetismSmoothing;
        public float DeltaTime;
        public float2 RawInput;
        public float4x4 ViewProjectionMatrix;
        public int SourceTeamID;

        public NativeReference<float2> ResultInput;

        public void Execute()
        {
            float2 modifiedInput = RawInput;
            float bestMagnetismScore = -1f;
            float3 bestTargetPos = float3.zero;

            float maxDistSq = MaxDistance * MaxDistance;

            for (int j = 0; j < Candidates.Length; j++)
            {
                int i = Candidates[j];
                TargetData target = Targets[i];

                // Filter out teammates
                if (SourceTeamID != -1 && target.TeamID == SourceTeamID) continue;

                float3 toTarget = target.Position - SourcePosition;
                float distSq = math.lengthsq(toTarget);

                if (distSq > maxDistSq) continue;

                float dist = math.sqrt(distSq);
                float3 dir = toTarget / dist;
                float dot = math.dot(Forward, dir);
                
                // Angle in degrees
                float angle = math.acos(math.clamp(dot, -1f, 1f)) * 57.29578f;

                // 1. Friction Logic: Slow down if near center
                // Friction is stronger when Closer to center of target
                if (angle < 5.0f) 
                {
                    float angleNormalized = math.clamp(angle / 5.0f, 0f, 1f);
                    float distNormalized = math.clamp(dist / MaxDistance, 0f, 1f);
                    
                    // Sample friction from LUT based on distance
                    float frictionBase = SampleLUT(FrictionLUT, distNormalized);
                    
                    // Modulate friction by how centered the reticle is (stronger at center)
                    float frictionMod = math.lerp(frictionBase, 1.0f, angleNormalized);
                    
                    modifiedInput *= frictionMod;
                }

                // 2. Magnetism (Adhesion) Logic: Find best target in cone
                if (angle < ConeAngle)
                {
                    // Viewport check
                    float4 clipPos = math.mul(ViewProjectionMatrix, new float4(target.Position, 1f));
                    if (clipPos.w > 0)
                    {
                        float3 ndc = clipPos.xyz / clipPos.w;
                        if (ndc.x >= -1 && ndc.x <= 1 && ndc.y >= -1 && ndc.y <= 1)
                        {
                            // Stick Adhesion: Only assist if the player is moving the stick
                            // Reward players who are already trying to track
                            float2 targetNDC = ndc.xy;
                            float2 inputDir = math.normalize(RawInput);
                            float2 toTargetNDC = math.normalize(targetNDC);
                            float stickDot = math.dot(inputDir, toTargetNDC);

                            // Score: Combination of distance from center and stick direction alignment
                            float angleScore = 1f - (angle / ConeAngle);
                            float combinedScore = angleScore * math.clamp(stickDot, 0.1f, 1f);

                            if (combinedScore > bestMagnetismScore)
                            {
                                bestMagnetismScore = combinedScore;
                                bestTargetPos = target.Position;
                            }
                        }
                    }
                }
            }

            // Apply Magnetism pull
            float inputMag = math.length(RawInput);
            if (bestMagnetismScore > 0 && inputMag > 0.05f)
            {
                float t = math.clamp(inputMag, 0f, 1f);
                float strengthMultiplier = SampleLUT(MagnetismLUT, t);

                // Project target center to screen (approximate joystick coordinates)
                float4 targetClip = math.mul(ViewProjectionMatrix, new float4(bestTargetPos, 1f));
                float2 targetNDC = targetClip.xy / targetClip.w;
                
                // Calculate pull vector in NDC space
                float2 pullDir = targetNDC;
                float pullMag = math.length(pullDir);
                
                if (pullMag > 0.001f)
                {
                    pullDir /= pullMag; // Normalize
                    
                    // Apply pull with smoothing and strength
                    float finalStrength = MagnetismStrength * bestMagnetismScore * strengthMultiplier;
                    float2 pullVector = pullDir * finalStrength * DeltaTime;
                    
                    // Rotate input towards target instead of just adding to it
                    // This creates a smoother "adhesion" feel
                    modifiedInput = math.lerp(modifiedInput, pullVector + modifiedInput, math.clamp(MagnetismSmoothing * DeltaTime, 0, 1));
                }
            }

            ResultInput.Value = modifiedInput;
        }

        /// <summary>
        /// Samples a Look-Up Table (LUT) for a given value.
        /// </summary>
        private static float SampleLUT(NativeArray<float> lut, float t)
        {
            if (!lut.IsCreated || lut.Length == 0) return 1f;
            int index = (int)math.clamp(t * (lut.Length - 1), 0, lut.Length - 1);
            return lut[index];
        }
    }
}
