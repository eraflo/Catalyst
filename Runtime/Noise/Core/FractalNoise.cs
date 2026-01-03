using Unity.Burst;
using Unity.Mathematics;

namespace Eraflo.Catalyst.Noise
{
    /// <summary>
    /// Fractal Brownian Motion (FBM) noise generation.
    /// Layers multiple octaves of noise for natural-looking patterns.
    /// </summary>
    public static class FractalNoise
    {
        /// <summary>
        /// Default FBM settings.
        /// </summary>
        public static readonly FractalSettings Default = new FractalSettings
        {
            Octaves = 4,
            Lacunarity = 2.0f,
            Persistence = 0.5f,
            Amplitude = 1.0f,
            Frequency = 1.0f
        };
        
        /// <summary>
        /// Samples 2D Fractal Brownian Motion noise.
        /// </summary>
        /// <param name="coord">2D coordinate</param>
        /// <param name="settings">FBM settings</param>
        /// <returns>Noise value (range depends on octaves and persistence)</returns>
            public static float Sample2D(float2 coord, FractalSettings settings)
        {
            float value = 0f;
            float amplitude = settings.Amplitude;
            float frequency = settings.Frequency;
            float maxValue = 0f;
            
            for (int i = 0; i < settings.Octaves; i++)
            {
                value += BurstNoise.Sample2D(coord * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= settings.Persistence;
                frequency *= settings.Lacunarity;
            }
            
            return value / maxValue;
        }
        
        /// <summary>
        /// Samples 2D FBM with default settings.
        /// </summary>
            public static float Sample2D(float2 coord, int octaves = 4)
        {
            var settings = Default;
            settings.Octaves = octaves;
            return Sample2D(coord, settings);
        }
        
        /// <summary>
        /// Samples 3D Fractal Brownian Motion noise.
        /// </summary>
            public static float Sample3D(float3 coord, FractalSettings settings)
        {
            float value = 0f;
            float amplitude = settings.Amplitude;
            float frequency = settings.Frequency;
            float maxValue = 0f;
            
            for (int i = 0; i < settings.Octaves; i++)
            {
                value += BurstNoise.Sample3D(coord * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= settings.Persistence;
                frequency *= settings.Lacunarity;
            }
            
            return value / maxValue;
        }
        
        /// <summary>
        /// Samples 3D FBM with default settings.
        /// </summary>
            public static float Sample3D(float3 coord, int octaves = 4)
        {
            var settings = Default;
            settings.Octaves = octaves;
            return Sample3D(coord, settings);
        }
        
        /// <summary>
        /// Samples 4D Fractal Brownian Motion noise.
        /// </summary>
            public static float Sample4D(float4 coord, FractalSettings settings)
        {
            float value = 0f;
            float amplitude = settings.Amplitude;
            float frequency = settings.Frequency;
            float maxValue = 0f;
            
            for (int i = 0; i < settings.Octaves; i++)
            {
                value += BurstNoise.Sample4D(coord * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= settings.Persistence;
                frequency *= settings.Lacunarity;
            }
            
            return value / maxValue;
        }
        
        /// <summary>
        /// Samples 4D FBM with default settings.
        /// </summary>
            public static float Sample4D(float4 coord, int octaves = 4)
        {
            var settings = Default;
            settings.Octaves = octaves;
            return Sample4D(coord, settings);
        }
        
        /// <summary>
        /// Samples time-animated 3D FBM noise.
        /// Convenience method that uses time as the 4th dimension.
        /// </summary>
        /// <param name="coord">3D coordinate</param>
        /// <param name="time">Time value for animation</param>
        /// <param name="timeScale">How fast the noise changes over time</param>
        /// <param name="settings">FBM settings</param>
            public static float SampleAnimated3D(float3 coord, float time, float timeScale, FractalSettings settings)
        {
            return Sample4D(new float4(coord, time * timeScale), settings);
        }
        
        /// <summary>
        /// Samples time-animated 3D FBM with default settings.
        /// </summary>
            public static float SampleAnimated3D(float3 coord, float time, float timeScale = 1f, int octaves = 4)
        {
            var settings = Default;
            settings.Octaves = octaves;
            return SampleAnimated3D(coord, time, timeScale, settings);
        }
    }
    
    /// <summary>
    /// Settings for Fractal Brownian Motion noise.
    /// </summary>
    public struct FractalSettings
    {
        /// <summary>
        /// Number of noise layers to sum.
        /// More octaves = more detail, but more expensive.
        /// </summary>
        public int Octaves;
        
        /// <summary>
        /// Frequency multiplier per octave.
        /// Higher values = more high-frequency detail.
        /// Typical value: 2.0
        /// </summary>
        public float Lacunarity;
        
        /// <summary>
        /// Amplitude multiplier per octave.
        /// Higher values = stronger contribution from high-frequency octaves.
        /// Typical value: 0.5
        /// </summary>
        public float Persistence;
        
        /// <summary>
        /// Base amplitude of the noise.
        /// </summary>
        public float Amplitude;
        
        /// <summary>
        /// Base frequency of the noise.
        /// </summary>
        public float Frequency;
        
        /// <summary>
        /// Creates a new FractalSettings with common defaults.
        /// </summary>
        public static FractalSettings Create(int octaves = 4, float lacunarity = 2f, float persistence = 0.5f)
        {
            return new FractalSettings
            {
                Octaves = octaves,
                Lacunarity = lacunarity,
                Persistence = persistence,
                Amplitude = 1f,
                Frequency = 1f
            };
        }
    }
}
