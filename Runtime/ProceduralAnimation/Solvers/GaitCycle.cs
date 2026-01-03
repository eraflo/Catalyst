using System;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Solvers
{
    /// <summary>
    /// Represents a gait cycle for locomotion.
    /// Controls timing and phase of leg movements.
    /// </summary>
    [Serializable]
    public class GaitCycle
    {
        [Header("Timing")]
        [Tooltip("Duration of one complete gait cycle in seconds.")]
        [SerializeField] private float _cycleDuration = 1f;
        
        [Tooltip("Current phase of the cycle (0-1).")]
        [SerializeField, Range(0f, 1f)] private float _phase;
        
        [Tooltip("Speed multiplier for the cycle.")]
        [SerializeField] private float _speed = 1f;
        
        [Header("Stance/Swing")]
        [Tooltip("Fraction of cycle spent in stance (foot on ground).")]
        [SerializeField, Range(0.1f, 0.9f)] private float _stanceDutyFactor = 0.6f;
        
        [Tooltip("Height of foot during swing phase.")]
        [SerializeField] private float _stepHeight = 0.15f;
        
        [Tooltip("Forward overshoot at end of swing.")]
        [SerializeField] private float _stepOvershoot = 0.1f;
        
        private float _normalizedSpeed;
        private bool _isMoving;
        
        /// <summary>
        /// Current phase of the cycle (0-1).
        /// </summary>
        public float Phase => _phase;
        
        /// <summary>
        /// Duration of one complete cycle.
        /// </summary>
        public float CycleDuration
        {
            get => _cycleDuration;
            set => _cycleDuration = math.max(0.1f, value);
        }
        
        /// <summary>
        /// Speed multiplier.
        /// </summary>
        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }
        
        /// <summary>
        /// Fraction of cycle in stance phase.
        /// </summary>
        public float StanceDutyFactor
        {
            get => _stanceDutyFactor;
            set => _stanceDutyFactor = math.clamp(value, 0.1f, 0.9f);
        }
        
        /// <summary>
        /// Height of step during swing.
        /// </summary>
        public float StepHeight
        {
            get => _stepHeight;
            set => _stepHeight = math.max(0f, value);
        }
        
        /// <summary>
        /// Whether the cycle is currently moving.
        /// </summary>
        public bool IsMoving => _isMoving;
        
        /// <summary>
        /// Updates the gait cycle.
        /// </summary>
        public void Update(float deltaTime, float movementSpeed)
        {
            _normalizedSpeed = movementSpeed;
            _isMoving = movementSpeed > 0.01f;
            
            if (_isMoving)
            {
                // Advance phase based on speed
                float phaseAdvance = (deltaTime * _speed) / _cycleDuration;
                _phase = (_phase + phaseAdvance) % 1f;
            }
        }
        
        /// <summary>
        /// Gets the phase for a specific leg.
        /// </summary>
        /// <param name="legPhaseOffset">Phase offset for this leg (0-1).</param>
        /// <returns>The leg's current phase in the cycle.</returns>
        public float GetLegPhase(float legPhaseOffset)
        {
            return (_phase + legPhaseOffset) % 1f;
        }
        
        /// <summary>
        /// Checks if a leg is in stance phase (foot on ground).
        /// </summary>
        /// <param name="legPhaseOffset">Phase offset for this leg.</param>
        public bool IsInStance(float legPhaseOffset)
        {
            float legPhase = GetLegPhase(legPhaseOffset);
            return legPhase < _stanceDutyFactor;
        }
        
        /// <summary>
        /// Checks if a leg is in swing phase (foot in air).
        /// </summary>
        /// <param name="legPhaseOffset">Phase offset for this leg.</param>
        public bool IsInSwing(float legPhaseOffset)
        {
            return !IsInStance(legPhaseOffset);
        }
        
        /// <summary>
        /// Gets the normalized swing progress (0-1) for a leg in swing phase.
        /// Returns 0 if in stance.
        /// </summary>
        public float GetSwingProgress(float legPhaseOffset)
        {
            float legPhase = GetLegPhase(legPhaseOffset);
            
            if (legPhase < _stanceDutyFactor)
                return 0f;
            
            return (legPhase - _stanceDutyFactor) / (1f - _stanceDutyFactor);
        }
        
        /// <summary>
        /// Gets the foot height for a leg based on its phase.
        /// Uses a smooth arc during swing.
        /// </summary>
        public float GetFootHeight(float legPhaseOffset)
        {
            if (!IsInSwing(legPhaseOffset))
                return 0f;
            
            float swingProgress = GetSwingProgress(legPhaseOffset);
            
            // Smooth arc using sine
            return math.sin(swingProgress * math.PI) * _stepHeight;
        }
        
        /// <summary>
        /// Gets the step position interpolation factor for a leg.
        /// 0 = at lift-off position, 1 = at target position.
        /// </summary>
        public float GetStepProgress(float legPhaseOffset)
        {
            float legPhase = GetLegPhase(legPhaseOffset);
            
            if (legPhase < _stanceDutyFactor)
            {
                // In stance: interpolate from 1 (just landed) to 0 (about to lift)
                float stanceProgress = legPhase / _stanceDutyFactor;
                return 1f - stanceProgress;
            }
            else
            {
                // In swing: interpolate from 0 (just lifted) to 1 (about to land)
                return GetSwingProgress(legPhaseOffset);
            }
        }
        
        /// <summary>
        /// Resets the cycle to a specific phase.
        /// </summary>
        public void Reset(float startPhase = 0f)
        {
            _phase = startPhase % 1f;
        }
        
        /// <summary>
        /// Synchronizes this cycle to another.
        /// </summary>
        public void SyncTo(GaitCycle other)
        {
            _phase = other._phase;
        }
    }
    
    /// <summary>
    /// Predefined gait patterns.
    /// </summary>
    public static class GaitPatterns
    {
        /// <summary>
        /// Biped walking gait (alternating legs).
        /// </summary>
        public static float[] Biped => new[] { 0f, 0.5f };
        
        /// <summary>
        /// Quadruped walk gait (diagonal pairs).
        /// FL, FR, BL, BR
        /// </summary>
        public static float[] QuadrupedWalk => new[] { 0f, 0.5f, 0.5f, 0f };
        
        /// <summary>
        /// Quadruped trot gait (diagonal pairs in sync).
        /// </summary>
        public static float[] QuadrupedTrot => new[] { 0f, 0.5f, 0.5f, 0f };
        
        /// <summary>
        /// Quadruped pace gait (same-side pairs).
        /// </summary>
        public static float[] QuadrupedPace => new[] { 0f, 0.5f, 0f, 0.5f };
        
        /// <summary>
        /// Quadruped gallop gait (front together, back together).
        /// </summary>
        public static float[] QuadrupedGallop => new[] { 0f, 0.1f, 0.5f, 0.6f };
        
        /// <summary>
        /// Hexapod tripod gait (alternating triangles).
        /// </summary>
        public static float[] HexapodTripod => new[] { 0f, 0.5f, 0f, 0.5f, 0f, 0.5f };
        
        /// <summary>
        /// Octopod wave gait (sequential).
        /// </summary>
        public static float[] OctopodWave
        {
            get
            {
                var phases = new float[8];
                for (int i = 0; i < 8; i++)
                    phases[i] = (float)i / 8f;
                return phases;
            }
        }
        
        /// <summary>
        /// Gets the appropriate gait pattern for a morphology.
        /// </summary>
        public static float[] GetDefaultPattern(Perception.MorphologyType morphology)
        {
            return morphology switch
            {
                Perception.MorphologyType.Biped => Biped,
                Perception.MorphologyType.Quadruped => QuadrupedWalk,
                Perception.MorphologyType.Hexapod => HexapodTripod,
                Perception.MorphologyType.Octopod => OctopodWave,
                _ => Biped
            };
        }
    }
}
