namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// A timer that counts down from a specified duration to zero.
    /// </summary>
    public class CountdownTimer : Timer
    {
        /// <summary>
        /// Creates a new countdown timer.
        /// </summary>
        /// <param name="duration">The duration in seconds to count down from.</param>
        public CountdownTimer(float duration) : base(duration) { }

        /// <summary>
        /// Returns true when the timer has reached zero.
        /// </summary>
        public override bool IsFinished => CurrentTime <= 0f;

        /// <summary>
        /// Decrements the current time by deltaTime.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            if (IsFinished) return;
            
            CurrentTime -= deltaTime;
            
            if (CurrentTime < 0f)
                CurrentTime = 0f;
        }
    }
}
