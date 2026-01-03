using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Studio
{
    /// <summary>
    /// Fluent API for defining procedural animation moves.
    /// Enables non-animators to create expressive animations through code.
    /// </summary>
    /// <example>
    /// var punch = new ProceduralMove()
    ///     .Prepare(0.15f, -0.3f)
    ///     .Strike(targetPosition, Curves.Snap)
    ///     .Recover(0.8f);
    ///     
    /// transform.position = punch.Evaluate(time).Position;
    /// </example>
    [Serializable]
    public class ProceduralMove
    {
        private List<MovePhase> _phases = new List<MovePhase>();
        private float _totalDuration;
        private float3 _startPosition;
        private quaternion _startRotation;
        private bool _isRelative = true;
        
        /// <summary>
        /// Total duration of all phases.
        /// </summary>
        public float Duration => _totalDuration;
        
        /// <summary>
        /// Number of phases in this move.
        /// </summary>
        public int PhaseCount => _phases.Count;
        
        #region Fluent Builder Methods
        
        /// <summary>
        /// Sets the move to use absolute world coordinates.
        /// </summary>
        public ProceduralMove Absolute()
        {
            _isRelative = false;
            return this;
        }
        
        /// <summary>
        /// Sets the starting pose for the move.
        /// </summary>
        public ProceduralMove From(float3 position, quaternion rotation = default)
        {
            _startPosition = position;
            _startRotation = rotation.Equals(default(quaternion)) ? quaternion.identity : rotation;
            return this;
        }
        
        /// <summary>
        /// Adds a preparation/anticipation phase (wind-up before action).
        /// </summary>
        /// <param name="duration">Duration of the anticipation.</param>
        /// <param name="recoil">How far to pull back (negative = anticipation).</param>
        /// <param name="curve">Optional easing curve.</param>
        public ProceduralMove Prepare(float duration, float recoil, AnimationCurve curve = null)
        {
            _phases.Add(new MovePhase
            {
                Type = PhaseType.Anticipation,
                Duration = duration,
                Magnitude = recoil,
                Curve = curve ?? Curves.EaseOut,
                StartTime = _totalDuration
            });
            _totalDuration += duration;
            return this;
        }
        
        /// <summary>
        /// Adds a strike/action phase toward a target.
        /// </summary>
        /// <param name="target">Target position to move toward.</param>
        /// <param name="curve">Easing curve for the movement.</param>
        /// <param name="duration">Optional explicit duration (auto-calculated if not set).</param>
        public ProceduralMove Strike(float3 target, AnimationCurve curve = null, float duration = 0.2f)
        {
            _phases.Add(new MovePhase
            {
                Type = PhaseType.Strike,
                Duration = duration,
                TargetPosition = target,
                Curve = curve ?? Curves.Snap,
                StartTime = _totalDuration
            });
            _totalDuration += duration;
            return this;
        }
        
        /// <summary>
        /// Adds a recovery phase (return to rest).
        /// </summary>
        /// <param name="damping">Damping factor for spring-like recovery.</param>
        /// <param name="duration">Duration of recovery.</param>
        public ProceduralMove Recover(float damping = 0.8f, float duration = 0.3f)
        {
            _phases.Add(new MovePhase
            {
                Type = PhaseType.Recovery,
                Duration = duration,
                Magnitude = damping,
                Curve = Curves.EaseInOut,
                StartTime = _totalDuration
            });
            _totalDuration += duration;
            return this;
        }
        
        /// <summary>
        /// Adds a hold/sustain phase at current position.
        /// </summary>
        public ProceduralMove Hold(float duration)
        {
            _phases.Add(new MovePhase
            {
                Type = PhaseType.Hold,
                Duration = duration,
                StartTime = _totalDuration
            });
            _totalDuration += duration;
            return this;
        }
        
        /// <summary>
        /// Adds an arc movement to a target (curved path).
        /// </summary>
        /// <param name="target">Target position.</param>
        /// <param name="arcHeight">Height of the arc.</param>
        /// <param name="duration">Duration of the arc motion.</param>
        public ProceduralMove Arc(float3 target, float arcHeight, float duration = 0.3f)
        {
            _phases.Add(new MovePhase
            {
                Type = PhaseType.Arc,
                Duration = duration,
                TargetPosition = target,
                Magnitude = arcHeight,
                Curve = Curves.Smooth,
                StartTime = _totalDuration
            });
            _totalDuration += duration;
            return this;
        }
        
        /// <summary>
        /// Adds a swing phase (pendulum-like motion).
        /// </summary>
        public ProceduralMove Swing(float3 axis, float angle, float duration = 0.4f)
        {
            _phases.Add(new MovePhase
            {
                Type = PhaseType.Swing,
                Duration = duration,
                TargetPosition = axis,  // Using as axis storage
                Magnitude = angle,
                Curve = Curves.Bounce,
                StartTime = _totalDuration
            });
            _totalDuration += duration;
            return this;
        }
        
        /// <summary>
        /// Adds a follow-through phase with overshoot.
        /// </summary>
        public ProceduralMove FollowThrough(float overshoot = 0.1f, float duration = 0.15f)
        {
            _phases.Add(new MovePhase
            {
                Type = PhaseType.FollowThrough,
                Duration = duration,
                Magnitude = overshoot,
                Curve = Curves.Overshoot,
                StartTime = _totalDuration
            });
            _totalDuration += duration;
            return this;
        }
        
        #endregion
        
        #region Evaluation
        
        /// <summary>
        /// Evaluates the move at a given time.
        /// </summary>
        /// <param name="time">Time in seconds (0 to Duration).</param>
        /// <returns>The pose at that time.</returns>
        public AnimationPose Evaluate(float time)
        {
            if (_phases.Count == 0)
            {
                return new AnimationPose
                {
                    Position = _startPosition,
                    Rotation = _startRotation
                };
            }
            
            // Clamp time to valid range
            time = math.clamp(time, 0f, _totalDuration);
            
            // Find current and previous phase
            MovePhase currentPhase = _phases[0];
            MovePhase previousPhase = default;
            int phaseIndex = 0;
            
            for (int i = 0; i < _phases.Count; i++)
            {
                if (time >= _phases[i].StartTime)
                {
                    currentPhase = _phases[i];
                    phaseIndex = i;
                    previousPhase = i > 0 ? _phases[i - 1] : default;
                }
                else
                {
                    break;
                }
            }
            
            // Calculate local time within phase
            float localTime = time - currentPhase.StartTime;
            float normalizedTime = currentPhase.Duration > 0 
                ? math.saturate(localTime / currentPhase.Duration) 
                : 1f;
            
            // Apply curve
            float curvedTime = currentPhase.Curve?.Evaluate(normalizedTime) ?? normalizedTime;
            
            // Get start position for this phase
            float3 phaseStart = GetPhaseStartPosition(phaseIndex);
            
            // Evaluate based on phase type
            return EvaluatePhase(currentPhase, phaseStart, curvedTime);
        }
        
        private float3 GetPhaseStartPosition(int phaseIndex)
        {
            if (phaseIndex == 0) return _startPosition;
            
            // End position of previous phase is start of current
            var prevPhase = _phases[phaseIndex - 1];
            float3 prevStart = GetPhaseStartPosition(phaseIndex - 1);
            
            return EvaluatePhase(prevPhase, prevStart, 1f).Position;
        }
        
        private AnimationPose EvaluatePhase(MovePhase phase, float3 startPos, float t)
        {
            float3 position = startPos;
            quaternion rotation = _startRotation;
            
            switch (phase.Type)
            {
                case PhaseType.Anticipation:
                    // Pull back in opposite direction
                    float3 strikeDir = GetNextStrikeDirection(phase);
                    position = startPos - strikeDir * phase.Magnitude * t;
                    break;
                    
                case PhaseType.Strike:
                    position = math.lerp(startPos, phase.TargetPosition, t);
                    break;
                    
                case PhaseType.Recovery:
                    // Spring-like return to start
                    float damped = 1f - math.exp(-t * 5f * phase.Magnitude);
                    position = math.lerp(startPos, _startPosition, damped);
                    break;
                    
                case PhaseType.Hold:
                    position = startPos;
                    break;
                    
                case PhaseType.Arc:
                    // Parabolic arc
                    float3 horizontal = math.lerp(startPos, phase.TargetPosition, t);
                    float arcOffset = math.sin(t * math.PI) * phase.Magnitude;
                    position = horizontal + new float3(0, arcOffset, 0);
                    break;
                    
                case PhaseType.Swing:
                    // Pendulum motion
                    float angle = math.sin(t * math.PI) * math.radians(phase.Magnitude);
                    rotation = math.mul(quaternion.AxisAngle(phase.TargetPosition, angle), _startRotation);
                    position = startPos;
                    break;
                    
                case PhaseType.FollowThrough:
                    // Overshoot then settle
                    float overshootCurve = math.sin(t * math.PI * 2f) * (1f - t);
                    position = startPos + GetPreviousDirection() * phase.Magnitude * overshootCurve;
                    break;
            }
            
            return new AnimationPose
            {
                Position = position,
                Rotation = rotation
            };
        }
        
        private float3 GetNextStrikeDirection(MovePhase fromPhase)
        {
            int index = _phases.IndexOf(fromPhase);
            for (int i = index + 1; i < _phases.Count; i++)
            {
                if (_phases[i].Type == PhaseType.Strike)
                {
                    return math.normalizesafe(_phases[i].TargetPosition - _startPosition);
                }
            }
            return new float3(0, 0, 1);
        }
        
        private float3 GetPreviousDirection()
        {
            for (int i = _phases.Count - 1; i >= 0; i--)
            {
                if (_phases[i].Type == PhaseType.Strike)
                {
                    return math.normalizesafe(_phases[i].TargetPosition - _startPosition);
                }
            }
            return new float3(0, 0, 1);
        }
        
        #endregion
        
        /// <summary>
        /// Creates a deep copy of this move.
        /// </summary>
        public ProceduralMove Clone()
        {
            var clone = new ProceduralMove
            {
                _startPosition = _startPosition,
                _startRotation = _startRotation,
                _totalDuration = _totalDuration,
                _isRelative = _isRelative,
                _phases = new List<MovePhase>(_phases)
            };
            return clone;
        }
    }
    
    /// <summary>
    /// A single phase within a procedural move.
    /// </summary>
    [Serializable]
    public struct MovePhase
    {
        public PhaseType Type;
        public float Duration;
        public float StartTime;
        public float Magnitude;
        public float3 TargetPosition;
        public AnimationCurve Curve;
    }
    
    /// <summary>
    /// Types of phases in a procedural move.
    /// </summary>
    public enum PhaseType
    {
        Anticipation,
        Strike,
        Recovery,
        Hold,
        Arc,
        Swing,
        FollowThrough
    }
    
    /// <summary>
    /// Common animation curves for procedural moves.
    /// </summary>
    public static class Curves
    {
        private static AnimationCurve _easeIn;
        private static AnimationCurve _easeOut;
        private static AnimationCurve _easeInOut;
        private static AnimationCurve _snap;
        private static AnimationCurve _smooth;
        private static AnimationCurve _bounce;
        private static AnimationCurve _overshoot;
        
        public static AnimationCurve EaseIn => _easeIn ??= AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        public static AnimationCurve EaseOut => _easeOut ??= new AnimationCurve(
            new Keyframe(0, 0, 2, 2),
            new Keyframe(1, 1, 0, 0));
        
        public static AnimationCurve EaseInOut => _easeInOut ??= AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        public static AnimationCurve Snap => _snap ??= new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(0.3f, 0.95f, 2, 0.5f),
            new Keyframe(1, 1, 0, 0));
        
        public static AnimationCurve Smooth => _smooth ??= new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(0.5f, 0.5f, 1, 1),
            new Keyframe(1, 1, 0, 0));
        
        public static AnimationCurve Bounce => _bounce ??= new AnimationCurve(
            new Keyframe(0, 0, 0, 2),
            new Keyframe(0.6f, 1.1f, 0, 0),
            new Keyframe(0.8f, 0.95f, 0, 0),
            new Keyframe(1, 1, 0, 0));
        
        public static AnimationCurve Overshoot => _overshoot ??= new AnimationCurve(
            new Keyframe(0, 0, 0, 3),
            new Keyframe(0.4f, 1.2f, 0, 0),
            new Keyframe(0.7f, 0.9f, 0, 0),
            new Keyframe(1, 1, 0, 0));
        
        public static AnimationCurve Linear => AnimationCurve.Linear(0, 0, 1, 1);
    }
}
