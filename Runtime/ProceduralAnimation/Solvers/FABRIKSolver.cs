using System;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Solvers
{
    /// <summary>
    /// FABRIK (Forward And Backward Reaching Inverse Kinematics) solver.
    /// Efficient iterative IK algorithm suitable for chains of any length.
    /// </summary>
    public static class FABRIKSolver
    {
        /// <summary>
        /// Configuration for the FABRIK solver.
        /// </summary>
        public struct SolverConfig
        {
            /// <summary>
            /// Maximum number of iterations.
            /// </summary>
            public int MaxIterations;
            
            /// <summary>
            /// Tolerance for target distance (stops early if within tolerance).
            /// </summary>
            public float Tolerance;
            
            /// <summary>
            /// Whether to constrain the root position.
            /// </summary>
            public bool ConstrainRoot;
            
            /// <summary>
            /// Default configuration.
            /// </summary>
            public static SolverConfig Default => new SolverConfig
            {
                MaxIterations = 10,
                Tolerance = 0.001f,
                ConstrainRoot = true
            };
        }
        
        /// <summary>
        /// Result of a FABRIK solve operation.
        /// </summary>
        public struct SolverResult
        {
            /// <summary>
            /// Solved joint positions.
            /// </summary>
            public float3[] Positions;
            
            /// <summary>
            /// Distance from end effector to target.
            /// </summary>
            public float Error;
            
            /// <summary>
            /// Whether the target was reached (within tolerance).
            /// </summary>
            public bool Reached;
            
            /// <summary>
            /// Number of iterations performed.
            /// </summary>
            public int Iterations;
        }
        
        /// <summary>
        /// Solves IK for a chain of joints to reach a target position.
        /// </summary>
        /// <param name="joints">Current joint positions (will be modified).</param>
        /// <param name="boneLengths">Lengths of each bone segment.</param>
        /// <param name="target">Target position for the end effector.</param>
        /// <param name="config">Solver configuration.</param>
        /// <returns>Solver result with new joint positions.</returns>
        public static SolverResult Solve(float3[] joints, float[] boneLengths, 
                                          float3 target, SolverConfig config = default)
        {
            if (config.MaxIterations == 0)
                config = SolverConfig.Default;
            
            if (joints == null || joints.Length < 2)
            {
                return new SolverResult
                {
                    Positions = joints,
                    Error = float.MaxValue,
                    Reached = false,
                    Iterations = 0
                };
            }
            
            int jointCount = joints.Length;
            var positions = new float3[jointCount];
            Array.Copy(joints, positions, jointCount);
            
            // Calculate total chain length
            float totalLength = 0f;
            foreach (var length in boneLengths)
                totalLength += length;
            
            // Store original root position
            float3 rootPos = positions[0];
            
            // Check if target is reachable
            float distanceToTarget = math.length(target - rootPos);
            bool targetReachable = distanceToTarget <= totalLength;
            
            if (!targetReachable && config.ConstrainRoot)
            {
                // Target is too far - stretch toward it
                float3 direction = math.normalizesafe(target - rootPos);
                for (int i = 1; i < jointCount; i++)
                {
                    positions[i] = positions[i - 1] + direction * boneLengths[i - 1];
                }
                
                return new SolverResult
                {
                    Positions = positions,
                    Error = distanceToTarget - totalLength,
                    Reached = false,
                    Iterations = 1
                };
            }
            
            // FABRIK iterations
            int iteration = 0;
            float error = math.length(positions[jointCount - 1] - target);
            
            while (iteration < config.MaxIterations && error > config.Tolerance)
            {
                // Forward reaching (from end effector to root)
                positions[jointCount - 1] = target;
                for (int i = jointCount - 2; i >= 0; i--)
                {
                    float3 direction = math.normalizesafe(positions[i] - positions[i + 1]);
                    positions[i] = positions[i + 1] + direction * boneLengths[i];
                }
                
                // Backward reaching (from root to end effector)
                if (config.ConstrainRoot)
                {
                    positions[0] = rootPos;
                }
                
                for (int i = 1; i < jointCount; i++)
                {
                    float3 direction = math.normalizesafe(positions[i] - positions[i - 1]);
                    positions[i] = positions[i - 1] + direction * boneLengths[i - 1];
                }
                
                error = math.length(positions[jointCount - 1] - target);
                iteration++;
            }
            
            return new SolverResult
            {
                Positions = positions,
                Error = error,
                Reached = error <= config.Tolerance,
                Iterations = iteration
            };
        }
        
        /// <summary>
        /// Solves IK with pole vector constraint (for elbow/knee orientation).
        /// </summary>
        public static SolverResult SolveWithPole(float3[] joints, float[] boneLengths,
                                                  float3 target, float3 poleTarget,
                                                  SolverConfig config = default)
        {
            // First solve without pole
            var result = Solve(joints, boneLengths, target, config);
            
            if (result.Positions.Length < 3)
                return result;
            
            // Apply pole constraint to middle joints
            ApplyPoleConstraint(result.Positions, poleTarget);
            
            // One more forward-backward pass to maintain bone lengths
            int jointCount = result.Positions.Length;
            float3 rootPos = result.Positions[0];
            
            // Forward
            result.Positions[jointCount - 1] = target;
            for (int i = jointCount - 2; i >= 0; i--)
            {
                float3 direction = math.normalizesafe(result.Positions[i] - result.Positions[i + 1]);
                result.Positions[i] = result.Positions[i + 1] + direction * boneLengths[i];
            }
            
            // Backward
            if (config.ConstrainRoot)
            {
                result.Positions[0] = rootPos;
            }
            for (int i = 1; i < jointCount; i++)
            {
                float3 direction = math.normalizesafe(result.Positions[i] - result.Positions[i - 1]);
                result.Positions[i] = result.Positions[i - 1] + direction * boneLengths[i - 1];
            }
            
            result.Error = math.length(result.Positions[jointCount - 1] - target);
            result.Reached = result.Error <= config.Tolerance;
            
            return result;
        }
        
        /// <summary>
        /// Applies pole constraint to bend middle joints toward the pole target.
        /// </summary>
        private static void ApplyPoleConstraint(float3[] positions, float3 poleTarget)
        {
            if (positions.Length < 3) return;
            
            float3 root = positions[0];
            float3 effector = positions[positions.Length - 1];
            
            // Line from root to effector
            float3 chainAxis = math.normalizesafe(effector - root);
            
            // For each middle joint
            for (int i = 1; i < positions.Length - 1; i++)
            {
                float3 joint = positions[i];
                
                // Project joint and pole onto plane perpendicular to chain axis
                float3 jointOnAxis = root + chainAxis * math.dot(joint - root, chainAxis);
                float3 poleOnAxis = root + chainAxis * math.dot(poleTarget - root, chainAxis);
                
                // Direction from axis to current joint
                float3 jointDir = math.normalizesafe(joint - jointOnAxis);
                float jointDist = math.length(joint - jointOnAxis);
                
                // Direction from axis to pole
                float3 poleDir = math.normalizesafe(poleTarget - poleOnAxis);
                
                // New joint position (same distance from axis, but toward pole)
                if (jointDist > 0.001f)
                {
                    positions[i] = jointOnAxis + poleDir * jointDist;
                }
            }
        }
        
        /// <summary>
        /// Converts solved joint positions to rotations.
        /// </summary>
        /// <param name="positions">Joint positions from solver.</param>
        /// <param name="upVector">Up vector for orientation.</param>
        /// <returns>Rotations for each joint.</returns>
        public static quaternion[] PositionsToRotations(float3[] positions, float3 upVector = default)
        {
            if (positions == null || positions.Length < 2)
                return new quaternion[0];
            
            if (math.lengthsq(upVector) < 0.01f)
                upVector = new float3(0, 1, 0);
            
            var rotations = new quaternion[positions.Length];
            
            for (int i = 0; i < positions.Length - 1; i++)
            {
                float3 direction = math.normalizesafe(positions[i + 1] - positions[i]);
                
                if (math.lengthsq(direction) > 0.01f)
                {
                    rotations[i] = quaternion.LookRotation(direction, upVector);
                }
                else
                {
                    rotations[i] = quaternion.identity;
                }
            }
            
            // Last joint uses same rotation as previous
            rotations[positions.Length - 1] = rotations[positions.Length - 2];
            
            return rotations;
        }
    }
}
