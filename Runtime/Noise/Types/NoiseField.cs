using Unity.Burst;
using Unity.Mathematics;

namespace Eraflo.Catalyst.Noise
{
    /// <summary>
    /// Noise field that provides smooth, animated noise values at any 3D position.
    /// Useful for wind simulation, breathing effects, trembling, etc.
    /// </summary>
    public struct NoiseField
    {
        /// <summary>
        /// Base frequency of the noise field.
        /// </summary>
        public float Frequency;
        
        /// <summary>
        /// Speed at which the noise evolves over time.
        /// </summary>
        public float TimeScale;
        
        /// <summary>
        /// Amplitude of the noise output.
        /// </summary>
        public float Amplitude;
        
        /// <summary>
        /// Number of octaves for fractal noise.
        /// </summary>
        public int Octaves;
        
        /// <summary>
        /// Offset added to all coordinates.
        /// </summary>
        public float3 Offset;
        
        /// <summary>
        /// Current time for animation.
        /// </summary>
        public float Time;
        
        /// <summary>
        /// Creates a noise field with default settings.
        /// </summary>
        public static NoiseField Default => new NoiseField
        {
            Frequency = 1f,
            TimeScale = 0.5f,
            Amplitude = 1f,
            Octaves = 3,
            Offset = float3.zero,
            Time = 0f
        };
        
        /// <summary>
        /// Creates a noise field for wind simulation.
        /// </summary>
        public static NoiseField Wind => new NoiseField
        {
            Frequency = 0.5f,
            TimeScale = 0.3f,
            Amplitude = 1f,
            Octaves = 4,
            Offset = float3.zero,
            Time = 0f
        };
        
        /// <summary>
        /// Creates a noise field for breathing/idle animation.
        /// </summary>
        public static NoiseField Breathing => new NoiseField
        {
            Frequency = 0.1f,
            TimeScale = 0.8f,
            Amplitude = 0.5f,
            Octaves = 2,
            Offset = float3.zero,
            Time = 0f
        };
        
        /// <summary>
        /// Creates a noise field for trembling/shaking.
        /// </summary>
        public static NoiseField Trembling => new NoiseField
        {
            Frequency = 5f,
            TimeScale = 3f,
            Amplitude = 0.2f,
            Octaves = 2,
            Offset = float3.zero,
            Time = 0f
        };
        
        /// <summary>
        /// Samples a scalar noise value at the given position.
        /// </summary>
        /// <param name="position">World position to sample</param>
        /// <returns>Noise value in range [-Amplitude, Amplitude]</returns>
        [BurstCompile]
        public float Sample(float3 position)
        {
            float3 coord = (position + Offset) * Frequency;
            float4 coord4d = new float4(coord, Time * TimeScale);
            
            var settings = new FractalSettings
            {
                Octaves = Octaves,
                Lacunarity = 2f,
                Persistence = 0.5f,
                Amplitude = 1f,
                Frequency = 1f
            };
            
            return FractalNoise.Sample4D(coord4d, settings) * Amplitude;
        }
        
        /// <summary>
        /// Samples a 3D noise vector at the given position.
        /// Each component uses a different offset to ensure independence.
        /// </summary>
        /// <param name="position">World position to sample</param>
        /// <returns>3D noise vector with each component in range [-Amplitude, Amplitude]</returns>
        [BurstCompile]
        public float3 Sample3D(float3 position)
        {
            float3 coord = (position + Offset) * Frequency;
            
            var settings = new FractalSettings
            {
                Octaves = Octaves,
                Lacunarity = 2f,
                Persistence = 0.5f,
                Amplitude = 1f,
                Frequency = 1f
            };
            
            // Use different offsets for each axis to get independent noise
            float x = FractalNoise.Sample4D(new float4(coord, Time * TimeScale), settings);
            float y = FractalNoise.Sample4D(new float4(coord + new float3(100f, 0f, 0f), Time * TimeScale), settings);
            float z = FractalNoise.Sample4D(new float4(coord + new float3(0f, 100f, 0f), Time * TimeScale), settings);
            
            return new float3(x, y, z) * Amplitude;
        }
        
        /// <summary>
        /// Updates the time of the noise field.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public void Update(float deltaTime)
        {
            Time += deltaTime;
        }
        
        /// <summary>
        /// Samples the noise field as a directional force (e.g., for wind).
        /// </summary>
        /// <param name="position">World position to sample</param>
        /// <param name="mainDirection">Main direction of the force</param>
        /// <returns>Force vector with noise-based variation</returns>
        [BurstCompile]
        public float3 SampleAsForce(float3 position, float3 mainDirection)
        {
            float3 noise = Sample3D(position);
            float mainNoise = 0.5f + 0.5f * Sample(position); // 0 to 1
            
            return math.normalizesafe(mainDirection) * mainNoise * Amplitude + noise * 0.3f;
        }
    }
}
