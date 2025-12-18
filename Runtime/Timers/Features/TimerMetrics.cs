using System.Diagnostics;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Tracks timer system metrics for debugging and profiling.
    /// Access via Timer.Metrics.
    /// </summary>
    public class TimerMetrics
    {
        private readonly Stopwatch _updateStopwatch = new Stopwatch();
        
        /// <summary>Total number of timers created since startup or last reset.</summary>
        public int TotalCreated { get; private set; }
        
        /// <summary>Current number of active timers.</summary>
        public int ActiveCount => Timer.Count;
        
        /// <summary>Total number of timers that completed naturally.</summary>
        public int TotalCompleted { get; private set; }
        
        /// <summary>Total number of timers cancelled.</summary>
        public int TotalCancelled { get; private set; }
        
        /// <summary>Total number of times Reset was called.</summary>
        public int TotalResets { get; private set; }
        
        /// <summary>Peak number of simultaneous active timers.</summary>
        public int PeakActiveCount { get; private set; }
        
        /// <summary>Average initial duration of created timers.</summary>
        public float AverageDuration => TotalCreated > 0 ? _totalDuration / TotalCreated : 0f;
        
        /// <summary>Last Update() execution time in milliseconds.</summary>
        public double LastUpdateMs { get; private set; }
        
        /// <summary>Average Update() execution time in milliseconds.</summary>
        public double AverageUpdateMs => _updateCount > 0 ? _totalUpdateMs / _updateCount : 0;

        private float _totalDuration;
        private double _totalUpdateMs;
        private long _updateCount;

        /// <summary>Records a timer creation.</summary>
        internal void RecordCreation(float duration)
        {
            TotalCreated++;
            _totalDuration += duration;
            
            int current = ActiveCount;
            if (current > PeakActiveCount)
                PeakActiveCount = current;
        }

        /// <summary>Records a timer completion.</summary>
        internal void RecordCompletion()
        {
            TotalCompleted++;
        }

        /// <summary>Records a timer cancellation.</summary>
        internal void RecordCancellation()
        {
            TotalCancelled++;
        }

        /// <summary>Records a timer reset.</summary>
        internal void RecordReset()
        {
            TotalResets++;
        }

        /// <summary>Starts measuring update time.</summary>
        internal void BeginUpdate()
        {
            _updateStopwatch.Restart();
        }

        /// <summary>Stops measuring update time.</summary>
        internal void EndUpdate()
        {
            _updateStopwatch.Stop();
            LastUpdateMs = _updateStopwatch.Elapsed.TotalMilliseconds;
            _totalUpdateMs += LastUpdateMs;
            _updateCount++;
        }

        /// <summary>Resets all metrics to zero.</summary>
        public void Reset()
        {
            TotalCreated = 0;
            TotalCompleted = 0;
            TotalCancelled = 0;
            TotalResets = 0;
            PeakActiveCount = 0;
            _totalDuration = 0f;
            _totalUpdateMs = 0;
            _updateCount = 0;
            LastUpdateMs = 0;
        }

        /// <summary>Returns a formatted string of all metrics.</summary>
        public override string ToString()
        {
            return $"Created: {TotalCreated}, Active: {ActiveCount}, Completed: {TotalCompleted}, " +
                   $"Cancelled: {TotalCancelled}, Resets: {TotalResets}, Peak: {PeakActiveCount}, " +
                   $"AvgDuration: {AverageDuration:F2}s, UpdateMs: {LastUpdateMs:F3}ms (avg: {AverageUpdateMs:F3}ms)";
        }
    }
}
