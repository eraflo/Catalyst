using System;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.SignalProcessing
{
    /// <summary>
    /// Streams pose data over time, with support for buffering, interpolation, and extrapolation.
    /// </summary>
    public class PoseStream
    {
        private AnimationPose[] _buffer;
        private float[] _timestamps;
        private int _head;
        private int _count;
        private int _capacity;
        
        /// <summary>
        /// Maximum number of frames stored.
        /// </summary>
        public int Capacity => _capacity;
        
        /// <summary>
        /// Current number of frames in the buffer.
        /// </summary>
        public int Count => _count;
        
        /// <summary>
        /// Whether the stream has any data.
        /// </summary>
        public bool HasData => _count > 0;
        
        /// <summary>
        /// Timestamp of the oldest frame.
        /// </summary>
        public float OldestTime => _count > 0 ? _timestamps[GetIndex(0)] : 0f;
        
        /// <summary>
        /// Timestamp of the newest frame.
        /// </summary>
        public float NewestTime => _count > 0 ? _timestamps[GetIndex(_count - 1)] : 0f;
        
        /// <summary>
        /// Creates a pose stream with the given capacity.
        /// </summary>
        public PoseStream(int capacity = 10)
        {
            _capacity = math.max(2, capacity);
            _buffer = new AnimationPose[_capacity];
            _timestamps = new float[_capacity];
            _head = 0;
            _count = 0;
        }
        
        /// <summary>
        /// Adds a pose to the stream.
        /// </summary>
        public void Push(AnimationPose pose, float timestamp)
        {
            // Move head forward (circular buffer)
            if (_count >= _capacity)
            {
                _head = (_head + 1) % _capacity;
            }
            else
            {
                _count++;
            }
            
            int index = GetIndex(_count - 1);
            _buffer[index] = pose;
            _timestamps[index] = timestamp;
        }
        
        /// <summary>
        /// Gets the most recent pose.
        /// </summary>
        public AnimationPose GetLatest()
        {
            if (_count == 0) return AnimationPose.Identity;
            return _buffer[GetIndex(_count - 1)];
        }
        
        /// <summary>
        /// Gets a pose at the specified time via interpolation.
        /// </summary>
        public AnimationPose Sample(float timestamp)
        {
            if (_count == 0) return AnimationPose.Identity;
            if (_count == 1) return _buffer[_head];
            
            // Find surrounding frames
            int beforeIdx = -1;
            int afterIdx = -1;
            
            for (int i = 0; i < _count; i++)
            {
                int bufIdx = GetIndex(i);
                float loopT = _timestamps[bufIdx];
                
                if (loopT <= timestamp)
                    beforeIdx = i;
                else if (afterIdx == -1)
                    afterIdx = i;
            }
            
            // Handle edge cases
            if (beforeIdx == -1)
            {
                // Before all frames - extrapolate backward
                return ExtrapolateBackward(timestamp);
            }
            if (afterIdx == -1)
            {
                // After all frames - extrapolate forward
                return ExtrapolateForward(timestamp);
            }
            
            // Interpolate between frames
            int beforeBufIdx = GetIndex(beforeIdx);
            int afterBufIdx = GetIndex(afterIdx);
            
            float beforeTime = _timestamps[beforeBufIdx];
            float afterTime = _timestamps[afterBufIdx];
            
            float t = math.saturate((timestamp - beforeTime) / (afterTime - beforeTime));
            
            return AnimationPose.Lerp(_buffer[beforeBufIdx], _buffer[afterBufIdx], t);
        }
        
        private AnimationPose ExtrapolateForward(float timestamp)
        {
            if (_count < 2)
                return GetLatest();
            
            // Use last two frames to extrapolate
            int idx1 = GetIndex(_count - 2);
            int idx2 = GetIndex(_count - 1);
            
            float t1 = _timestamps[idx1];
            float t2 = _timestamps[idx2];
            float dt = t2 - t1;
            
            if (dt <= 0.0001f)
                return _buffer[idx2];
            
            AnimationPose p1 = _buffer[idx1];
            AnimationPose p2 = _buffer[idx2];
            
            // Calculate velocity
            float3 velocity = (p2.Position - p1.Position) / dt;
            
            // Extrapolate
            float extraTime = timestamp - t2;
            return new AnimationPose
            {
                Position = p2.Position + velocity * extraTime,
                Rotation = p2.Rotation, // Don't extrapolate rotation (can cause issues)
                Velocity = velocity,
                AngularVelocity = p2.AngularVelocity
            };
        }
        
        private AnimationPose ExtrapolateBackward(float timestamp)
        {
            // For backward extrapolation, just return the oldest frame
            // (extrapolating backward is usually not needed)
            return _buffer[GetIndex(0)];
        }
        
        /// <summary>
        /// Clears all buffered poses.
        /// </summary>
        public void Clear()
        {
            _count = 0;
            _head = 0;
        }
        
        private int GetIndex(int offset)
        {
            return (_head + offset) % _capacity;
        }
    }
    
    /// <summary>
    /// Various signal smoothing/filtering utilities.
    /// </summary>
    public static class SignalSmoothing
    {
        /// <summary>
        /// One-Euro Filter for adaptive smoothing.
        /// Good for low-latency tracking with noise reduction.
        /// </summary>
        public struct OneEuroFilter
        {
            private float _minCutoff;
            private float _beta;
            private float _dCutoff;
            
            private float _x;
            private float _dx;
            private float _lastTime;
            private bool _initialized;
            
            /// <summary>
            /// Creates a One-Euro filter.
            /// </summary>
            /// <param name="minCutoff">Minimum cutoff frequency (Hz). Lower = more smoothing.</param>
            /// <param name="beta">Speed coefficient. Higher = less lag during fast movements.</param>
            /// <param name="dCutoff">Cutoff for derivative filter.</param>
            public static OneEuroFilter Create(float minCutoff = 1f, float beta = 0.5f, float dCutoff = 1f)
            {
                return new OneEuroFilter
                {
                    _minCutoff = minCutoff,
                    _beta = beta,
                    _dCutoff = dCutoff,
                    _initialized = false
                };
            }
            
            /// <summary>
            /// Filters a value.
            /// </summary>
            public float Filter(float x, float time)
            {
                if (!_initialized)
                {
                    _x = x;
                    _dx = 0f;
                    _lastTime = time;
                    _initialized = true;
                    return x;
                }
                
                float dt = time - _lastTime;
                if (dt <= 0f) return _x;
                
                _lastTime = time;
                
                // Estimate derivative
                float edx = (x - _x) / dt;
                
                // Filter derivative
                float alphaDx = SmoothingFactor(dt, _dCutoff);
                _dx = ExponentialSmooth(alphaDx, edx, _dx);
                
                // Adaptive cutoff
                float cutoff = _minCutoff + _beta * math.abs(_dx);
                
                // Filter value
                float alpha = SmoothingFactor(dt, cutoff);
                _x = ExponentialSmooth(alpha, x, _x);
                
                return _x;
            }
            
            /// <summary>
            /// Resets the filter state.
            /// </summary>
            public void Reset()
            {
                _initialized = false;
            }
            
            private static float SmoothingFactor(float dt, float cutoff)
            {
                float tau = 1f / (2f * math.PI * cutoff);
                return 1f / (1f + tau / dt);
            }
            
            private static float ExponentialSmooth(float alpha, float x, float prev)
            {
                return alpha * x + (1f - alpha) * prev;
            }
        }
        
        /// <summary>
        /// One-Euro Filter for float3 values.
        /// </summary>
        public struct OneEuroFilter3
        {
            private OneEuroFilter _x;
            private OneEuroFilter _y;
            private OneEuroFilter _z;
            
            public static OneEuroFilter3 Create(float minCutoff = 1f, float beta = 0.5f, float dCutoff = 1f)
            {
                return new OneEuroFilter3
                {
                    _x = OneEuroFilter.Create(minCutoff, beta, dCutoff),
                    _y = OneEuroFilter.Create(minCutoff, beta, dCutoff),
                    _z = OneEuroFilter.Create(minCutoff, beta, dCutoff)
                };
            }
            
            public float3 Filter(float3 v, float time)
            {
                return new float3(
                    _x.Filter(v.x, time),
                    _y.Filter(v.y, time),
                    _z.Filter(v.z, time)
                );
            }
            
            public void Reset()
            {
                _x.Reset();
                _y.Reset();
                _z.Reset();
            }
        }
        
        /// <summary>
        /// Simple moving average filter.
        /// </summary>
        public class MovingAverageFilter
        {
            private float[] _buffer;
            private int _head;
            private int _count;
            private float _sum;
            
            public int WindowSize => _buffer.Length;
            public float Average => _count > 0 ? _sum / _count : 0f;
            
            public MovingAverageFilter(int windowSize)
            {
                _buffer = new float[math.max(1, windowSize)];
            }
            
            public float Add(float value)
            {
                // Remove old value from sum
                if (_count >= _buffer.Length)
                {
                    _sum -= _buffer[_head];
                }
                else
                {
                    _count++;
                }
                
                // Add new value
                _buffer[_head] = value;
                _sum += value;
                _head = (_head + 1) % _buffer.Length;
                
                return Average;
            }
            
            public void Reset()
            {
                _head = 0;
                _count = 0;
                _sum = 0f;
            }
        }
        
        /// <summary>
        /// Moving average filter for float3.
        /// </summary>
        public class MovingAverageFilter3
        {
            private MovingAverageFilter _x;
            private MovingAverageFilter _y;
            private MovingAverageFilter _z;
            
            public MovingAverageFilter3(int windowSize)
            {
                _x = new MovingAverageFilter(windowSize);
                _y = new MovingAverageFilter(windowSize);
                _z = new MovingAverageFilter(windowSize);
            }
            
            public float3 Add(float3 value)
            {
                return new float3(
                    _x.Add(value.x),
                    _y.Add(value.y),
                    _z.Add(value.z)
                );
            }
            
            public float3 Average => new float3(_x.Average, _y.Average, _z.Average);
            
            public void Reset()
            {
                _x.Reset();
                _y.Reset();
                _z.Reset();
            }
        }
        
        /// <summary>
        /// Applies exponential smoothing to a value.
        /// </summary>
        public static float ExpSmooth(float current, float target, float smoothing, float dt)
        {
            float factor = 1f - math.pow(smoothing, dt);
            return math.lerp(current, target, factor);
        }
        
        /// <summary>
        /// Applies exponential smoothing to a float3.
        /// </summary>
        public static float3 ExpSmooth(float3 current, float3 target, float smoothing, float dt)
        {
            float factor = 1f - math.pow(smoothing, dt);
            return math.lerp(current, target, factor);
        }
        
        /// <summary>
        /// Applies exponential smoothing to a quaternion.
        /// </summary>
        public static quaternion ExpSmooth(quaternion current, quaternion target, float smoothing, float dt)
        {
            float factor = 1f - math.pow(smoothing, dt);
            return math.slerp(current, target, factor);
        }
    }
}
