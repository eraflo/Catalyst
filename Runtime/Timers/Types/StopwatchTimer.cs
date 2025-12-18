namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// A timer that counts up from zero indefinitely (like a stopwatch).
    /// </summary>
    public class StopwatchTimer : Timer
    {
        /// <summary>
        /// Creates a new stopwatch timer starting from zero.
        /// </summary>
        public StopwatchTimer() : base(0f) { }

        /// <summary>
        /// Stopwatch timers never finish automatically.
        /// </summary>
        public override bool IsFinished => false;

        /// <summary>
        /// Gets the elapsed time since the stopwatch started.
        /// </summary>
        public float ElapsedTime => CurrentTime;

        /// <summary>
        /// Increments the current time by deltaTime.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            CurrentTime += deltaTime;
        }

        /// <summary>
        /// Resets the stopwatch to zero.
        /// </summary>
        public override void Reset()
        {
            CurrentTime = 0f;
        }
    }
}
