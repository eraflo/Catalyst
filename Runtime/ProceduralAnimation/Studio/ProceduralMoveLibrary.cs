using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Studio
{
    /// <summary>
    /// Library of preset procedural moves for common actions.
    /// </summary>
    public static class ProceduralMoveLibrary
    {
        #region Combat Moves
        
        /// <summary>
        /// Quick jab punch.
        /// </summary>
        public static ProceduralMove Jab(float3 target, float speed = 1f)
        {
            float duration = 0.15f / speed;
            return new ProceduralMove()
                .Prepare(duration * 0.5f, -0.15f)
                .Strike(target, Curves.Snap, duration)
                .Recover(0.9f, duration * 1.5f);
        }
        
        /// <summary>
        /// Heavy punch with wind-up.
        /// </summary>
        public static ProceduralMove HeavyPunch(float3 target, float speed = 1f)
        {
            float baseTime = 0.25f / speed;
            return new ProceduralMove()
                .Prepare(baseTime, -0.4f)
                .Strike(target, Curves.Snap, baseTime * 0.6f)
                .FollowThrough(0.15f, baseTime * 0.3f)
                .Recover(0.7f, baseTime * 2f);
        }
        
        /// <summary>
        /// Kick motion.
        /// </summary>
        public static ProceduralMove Kick(float3 target, float height = 0.3f, float speed = 1f)
        {
            float baseTime = 0.3f / speed;
            return new ProceduralMove()
                .Prepare(baseTime * 0.5f, -0.2f)
                .Arc(target, height, baseTime)
                .Recover(0.6f, baseTime * 1.5f);
        }
        
        /// <summary>
        /// Sword slash.
        /// </summary>
        public static ProceduralMove Slash(float3 startPos, float3 endPos, float speed = 1f)
        {
            float baseTime = 0.2f / speed;
            return new ProceduralMove()
                .From(startPos, quaternion.identity)
                .Prepare(baseTime * 0.8f, -0.3f)
                .Strike(endPos, Curves.Snap, baseTime * 0.5f)
                .FollowThrough(0.2f, baseTime * 0.4f)
                .Recover(0.5f, baseTime * 2f);
        }
        
        #endregion
        
        #region Locomotion Moves
        
        /// <summary>
        /// Step forward motion for feet.
        /// </summary>
        public static ProceduralMove Step(float3 target, float stepHeight = 0.1f, float duration = 0.3f)
        {
            return new ProceduralMove()
                .Arc(target, stepHeight, duration);
        }
        
        /// <summary>
        /// Jump motion.
        /// </summary>
        public static ProceduralMove Jump(float height = 1f, float duration = 0.5f)
        {
            return new ProceduralMove()
                .Prepare(duration * 0.2f, -0.15f)
                .Arc(new float3(0, height, 0), height * 0.2f, duration * 0.4f)
                .Recover(0.5f, duration * 0.4f);
        }
        
        /// <summary>
        /// Dash forward.
        /// </summary>
        public static ProceduralMove Dash(float3 direction, float distance = 2f, float duration = 0.2f)
        {
            float3 target = math.normalizesafe(direction) * distance;
            return new ProceduralMove()
                .Prepare(duration * 0.15f, -0.1f)
                .Strike(target, Curves.EaseOut, duration * 0.6f)
                .Recover(0.8f, duration * 0.25f);
        }
        
        #endregion
        
        #region Gesture Moves
        
        /// <summary>
        /// Wave hand gesture.
        /// </summary>
        public static ProceduralMove Wave(float amplitude = 0.2f, int waves = 3, float duration = 1f)
        {
            var move = new ProceduralMove();
            float waveTime = duration / waves;
            
            for (int i = 0; i < waves; i++)
            {
                float side = (i % 2 == 0) ? 1f : -1f;
                move.Strike(new float3(amplitude * side, 0, 0), Curves.Smooth, waveTime * 0.5f);
            }
            
            return move.Recover(0.7f, waveTime);
        }
        
        /// <summary>
        /// Nod head motion.
        /// </summary>
        public static ProceduralMove Nod(float angle = 15f, int nods = 2, float duration = 0.6f)
        {
            var move = new ProceduralMove();
            float nodTime = duration / nods;
            
            for (int i = 0; i < nods; i++)
            {
                move.Swing(new float3(1, 0, 0), angle, nodTime * 0.5f);
                move.Swing(new float3(1, 0, 0), -angle * 0.3f, nodTime * 0.5f);
            }
            
            return move;
        }
        
        /// <summary>
        /// Shake head motion.
        /// </summary>
        public static ProceduralMove HeadShake(float angle = 20f, int shakes = 2, float duration = 0.5f)
        {
            var move = new ProceduralMove();
            float shakeTime = duration / (shakes * 2);
            
            for (int i = 0; i < shakes; i++)
            {
                move.Swing(new float3(0, 1, 0), angle, shakeTime);
                move.Swing(new float3(0, 1, 0), -angle, shakeTime);
            }
            
            return move.Recover(0.8f, shakeTime);
        }
        
        /// <summary>
        /// Point at target.
        /// </summary>
        public static ProceduralMove Point(float3 target, float duration = 0.3f)
        {
            return new ProceduralMove()
                .Strike(target, Curves.EaseOut, duration * 0.7f)
                .Hold(duration * 0.6f)
                .Recover(0.6f, duration * 0.7f);
        }
        
        #endregion
        
        #region Reactive Moves
        
        /// <summary>
        /// Flinch/startle reaction.
        /// </summary>
        public static ProceduralMove Flinch(float3 awayFrom, float intensity = 0.3f, float duration = 0.2f)
        {
            float3 recoil = math.normalizesafe(awayFrom) * -intensity;
            return new ProceduralMove()
                .Strike(recoil, Curves.Snap, duration * 0.3f)
                .Recover(0.9f, duration * 0.7f);
        }
        
        /// <summary>
        /// Recoil from impact.
        /// </summary>
        public static ProceduralMove Recoil(float3 impactDir, float force = 0.5f, float duration = 0.4f)
        {
            float3 knockback = math.normalizesafe(impactDir) * -force;
            return new ProceduralMove()
                .Strike(knockback, Curves.EaseOut, duration * 0.2f)
                .Recover(0.5f, duration * 0.8f);
        }
        
        /// <summary>
        /// Stagger backwards.
        /// </summary>
        public static ProceduralMove Stagger(float3 direction, float distance = 0.5f, float duration = 0.6f)
        {
            float3 target = math.normalizesafe(direction) * distance;
            return new ProceduralMove()
                .Strike(target * 0.7f, Curves.Snap, duration * 0.15f)
                .Recover(0.3f, duration * 0.2f)
                .Strike(target, Curves.Smooth, duration * 0.25f)
                .Recover(0.6f, duration * 0.4f);
        }
        
        #endregion
        
        #region Idle Moves
        
        /// <summary>
        /// Subtle breathing motion.
        /// </summary>
        public static ProceduralMove Breathe(float amplitude = 0.02f, float duration = 3f)
        {
            return new ProceduralMove()
                .Arc(new float3(0, amplitude, 0), amplitude * 0.5f, duration * 0.45f)
                .Hold(duration * 0.1f)
                .Arc(float3.zero, amplitude * 0.3f, duration * 0.45f);
        }
        
        /// <summary>
        /// Weight shift for idle stance.
        /// </summary>
        public static ProceduralMove WeightShift(float3 direction, float amount = 0.05f, float duration = 2f)
        {
            float3 offset = math.normalizesafe(direction) * amount;
            return new ProceduralMove()
                .Strike(offset, Curves.EaseInOut, duration * 0.4f)
                .Hold(duration * 0.2f)
                .Recover(0.5f, duration * 0.4f);
        }
        
        #endregion
        
        #region Tool Interactions
        
        /// <summary>
        /// Grab/pick up motion.
        /// </summary>
        public static ProceduralMove Grab(float3 target, float duration = 0.4f)
        {
            return new ProceduralMove()
                .Strike(target, Curves.Smooth, duration * 0.6f)
                .Hold(duration * 0.15f)
                .Recover(0.7f, duration * 0.25f);
        }
        
        /// <summary>
        /// Throw motion.
        /// </summary>
        public static ProceduralMove Throw(float3 direction, float power = 1f, float duration = 0.35f)
        {
            float3 windUp = -math.normalizesafe(direction) * 0.3f * power;
            float3 release = math.normalizesafe(direction) * power;
            
            return new ProceduralMove()
                .Prepare(duration * 0.4f, -0.3f * power)
                .Strike(release, Curves.Snap, duration * 0.25f)
                .FollowThrough(0.25f * power, duration * 0.15f)
                .Recover(0.6f, duration * 0.2f);
        }
        
        /// <summary>
        /// Push motion.
        /// </summary>
        public static ProceduralMove Push(float3 direction, float distance = 0.4f, float duration = 0.25f)
        {
            float3 target = math.normalizesafe(direction) * distance;
            return new ProceduralMove()
                .Prepare(duration * 0.3f, -0.1f)
                .Strike(target, Curves.EaseOut, duration * 0.4f)
                .Recover(0.8f, duration * 0.3f);
        }
        
        #endregion
    }
}
