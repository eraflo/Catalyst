using System;

namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Extensions for Chronos integration in the Timer system.
    /// </summary>
    public static class TimerChronosExtensions
    {
        /// <summary>
        /// Sets the time channel for a timer.
        /// </summary>
        /// <param name="handle">Timer handle.</param>
        /// <param name="channel">Channel name (e.g., "Enemies", "SlowMo").</param>
        /// <returns>The same handle for chaining.</returns>
        public static TimerHandle SetChannel(this TimerHandle handle, string channel)
        {
            if (!handle.IsValid) return handle;

            var timerService = App.Get<Timer>();
            if (timerService != null)
            {
                timerService.SetChannel(handle, channel);
            }

            return handle;
        }
    }
}
