using System;

namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Timer event registration methods.
    /// </summary>
    public static partial class Timer
    {
        /// <summary>
        /// Registers a callback with no parameters.
        /// </summary>
        /// <typeparam name="TCallback">Callback type implementing ITimerCallback.</typeparam>
        /// <param name="handle">Timer handle.</param>
        /// <param name="callback">Callback action.</param>
        public static void On<TCallback>(TimerHandle handle, Action callback) 
            where TCallback : struct, ITimerCallback
        {
            TimerCallbacks.Register<TCallback>(handle, callback);
        }

        /// <summary>
        /// Registers a callback with any parameter type.
        /// </summary>
        /// <typeparam name="TCallback">Callback type implementing ITimerCallback.</typeparam>
        /// <typeparam name="TArg">Parameter type (e.g., float, int, custom struct).</typeparam>
        /// <param name="handle">Timer handle.</param>
        /// <param name="callback">Callback action with parameter.</param>
        public static void On<TCallback, TArg>(TimerHandle handle, Action<TArg> callback) 
            where TCallback : struct, ITimerCallback
        {
            TimerCallbacks.Register<TCallback, TArg>(handle, callback);
        }

        /// <summary>
        /// Unregisters a specific callback type from a timer.
        /// </summary>
        /// <typeparam name="TCallback">Callback type to unregister.</typeparam>
        /// <param name="handle">Timer handle.</param>
        public static void Off<TCallback>(TimerHandle handle) 
            where TCallback : struct, ITimerCallback
        {
            TimerCallbacks.Unregister<TCallback>(handle);
        }
    }
}
