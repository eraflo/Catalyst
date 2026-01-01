namespace Eraflo.Catalyst.EasingSystem
{
    /// <summary>
    /// Type of easing function to apply.
    /// Supports In, Out, and InOut variations for standard curves.
    /// </summary>
    public enum EasingType
    {
        /// <summary>No easing, no acceleration.</summary>
        Linear,
        
        /// <summary>Accelerates from zero velocity (t^2).</summary>
        QuadIn,
        /// <summary>Decelerates to zero velocity (t^2).</summary>
        QuadOut,
        /// <summary>Accelerates until halfway, then decelerates (t^2).</summary>
        QuadInOut,
        
        /// <summary>Accelerates from zero velocity (t^3).</summary>
        CubicIn,
        /// <summary>Decelerates to zero velocity (t^3).</summary>
        CubicOut,
        /// <summary>Accelerates until halfway, then decelerates (t^3).</summary>
        CubicInOut,
        
        /// <summary>Accelerates from zero velocity (t^4).</summary>
        QuartIn,
        /// <summary>Decelerates to zero velocity (t^4).</summary>
        QuartOut,
        /// <summary>Accelerates until halfway, then decelerates (t^4).</summary>
        QuartInOut,
        
        /// <summary>Accelerates from zero velocity (t^5).</summary>
        QuintIn,
        /// <summary>Decelerates to zero velocity (t^5).</summary>
        QuintOut,
        /// <summary>Accelerates until halfway, then decelerates (t^5).</summary>
        QuintInOut,
        
        /// <summary>Accelerates from zero velocity (sinusoidal).</summary>
        SineIn,
        /// <summary>Decelerates to zero velocity (sinusoidal).</summary>
        SineOut,
        /// <summary>Accelerates until halfway, then decelerates (sinusoidal).</summary>
        SineInOut,
        
        /// <summary>Accelerates from zero velocity (exponential).</summary>
        ExpoIn,
        /// <summary>Decelerates to zero velocity (exponential).</summary>
        ExpoOut,
        /// <summary>Accelerates until halfway, then decelerates (exponential).</summary>
        ExpoInOut,
        
        /// <summary>Accelerates from zero velocity (circular).</summary>
        CircIn,
        /// <summary>Decelerates to zero velocity (circular).</summary>
        CircOut,
        /// <summary>Accelerates until halfway, then decelerates (circular).</summary>
        CircInOut,
        
        /// <summary>Overshoots the destination like a rubber band.</summary>
        ElasticIn,
        /// <summary>Overshoots the destination like a rubber band.</summary>
        ElasticOut,
        /// <summary>Overshoots the destination like a rubber band.</summary>
        ElasticInOut,
        
        /// <summary>Pulls back before accelerating.</summary>
        BackIn,
        /// <summary>Overshoots then returns to destination.</summary>
        BackOut,
        /// <summary>Pulls back, then overshoots.</summary>
        BackInOut,
        
        /// <summary>Bounces at the start.</summary>
        BounceIn,
        /// <summary>Bounces at the end (classic bouncing ball).</summary>
        BounceOut,
        /// <summary>Bounces at start and end.</summary>
        BounceInOut
    }
}
