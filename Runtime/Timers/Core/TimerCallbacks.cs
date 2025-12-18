using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Timers
{
    #region Callback Marker Interface

    /// <summary>
    /// Marker interface for timer callback types.
    /// Implement this with an empty struct to create custom callback types.
    /// </summary>
    public interface ITimerCallback { }

    /// <summary>
    /// Marker interface indicating a timer type supports a specific callback.
    /// </summary>
    public interface ISupportsCallback<T> where T : struct, ITimerCallback { }

    #endregion

    #region Built-in Callback Types

    /// <summary>Fired when a timer completes.</summary>
    public struct OnComplete : ITimerCallback { }

    /// <summary>Fired every frame while timer is running.</summary>
    public struct OnTick : ITimerCallback { }

    /// <summary>Fired when a timer is paused.</summary>
    public struct OnPause : ITimerCallback { }

    /// <summary>Fired when a timer is resumed.</summary>
    public struct OnResume : ITimerCallback { }

    /// <summary>Fired when a timer is reset.</summary>
    public struct OnReset : ITimerCallback { }

    /// <summary>Fired when a timer is cancelled.</summary>
    public struct OnCancel : ITimerCallback { }

    /// <summary>Fired when a repeating timer completes one interval.</summary>
    public struct OnRepeat : ITimerCallback { }

    #endregion

    #region Composite Callback Interfaces

    /// <summary>
    /// Standard lifecycle callbacks: OnComplete, OnTick, OnPause, OnResume, OnReset, OnCancel.
    /// </summary>
    public interface ISupportsStandardCallbacks :
        ISupportsCallback<OnComplete>,
        ISupportsCallback<OnTick>,
        ISupportsCallback<OnPause>,
        ISupportsCallback<OnResume>,
        ISupportsCallback<OnReset>,
        ISupportsCallback<OnCancel>
    { }

    /// <summary>
    /// Callbacks for indefinite timers (no OnComplete): OnTick, OnPause, OnResume, OnReset, OnCancel.
    /// </summary>
    public interface ISupportsIndefiniteCallbacks :
        ISupportsCallback<OnTick>,
        ISupportsCallback<OnPause>,
        ISupportsCallback<OnResume>,
        ISupportsCallback<OnReset>,
        ISupportsCallback<OnCancel>
    { }

    /// <summary>
    /// Minimal callbacks for one-shot timers: OnComplete, OnTick, OnCancel.
    /// </summary>
    public interface ISupportsOneShotCallbacks :
        ISupportsCallback<OnComplete>,
        ISupportsCallback<OnTick>,
        ISupportsCallback<OnCancel>
    { }

    /// <summary>
    /// Callbacks for repeating timers: Standard + OnRepeat.
    /// </summary>
    public interface ISupportsRepeatingCallbacks :
        ISupportsStandardCallbacks,
        ISupportsCallback<OnRepeat>
    { }

    #endregion

    #region Callback Registry

    /// <summary>
    /// Extensible callback registry for timers.
    /// Supports any parameter type via generics.
    /// </summary>
    public static class TimerCallbacks
    {
        private static readonly Dictionary<Type, Dictionary<uint, Delegate>> _callbacks 
            = new Dictionary<Type, Dictionary<uint, Delegate>>();

        #region Registration

        /// <summary>Registers a callback with no parameters.</summary>
        public static void Register<TCallback>(TimerHandle handle, Action callback) 
            where TCallback : struct, ITimerCallback
        {
            if (!handle.IsValid || callback == null) return;
            GetOrCreate<TCallback>()[handle.Id] = callback;
        }

        /// <summary>Registers a callback with any parameter type.</summary>
        public static void Register<TCallback, TArg>(TimerHandle handle, Action<TArg> callback) 
            where TCallback : struct, ITimerCallback
        {
            if (!handle.IsValid || callback == null) return;
            GetOrCreate<TCallback>()[handle.Id] = callback;
        }

        #endregion

        #region Invocation

        /// <summary>Invokes a callback with no parameters.</summary>
        public static void Invoke<TCallback>(uint id) where TCallback : struct, ITimerCallback
        {
            if (!TryGetCallback<TCallback>(id, out var del)) return;
            SafeInvoke(() => (del as Action)?.Invoke());
        }

        /// <summary>Invokes a callback with any parameter type.</summary>
        public static void Invoke<TCallback, TArg>(uint id, TArg value) where TCallback : struct, ITimerCallback
        {
            if (!TryGetCallback<TCallback>(id, out var del)) return;
            SafeInvoke(() =>
            {
                if (del is Action<TArg> typedAction) 
                    typedAction.Invoke(value);
                else 
                    (del as Action)?.Invoke();
            });
        }

        #endregion

        #region Cleanup

        public static void Unregister<TCallback>(TimerHandle handle) where TCallback : struct, ITimerCallback
        {
            if (_callbacks.TryGetValue(typeof(TCallback), out var dict))
                dict.Remove(handle.Id);
        }

        public static void Remove(uint id)
        {
            foreach (var dict in _callbacks.Values)
                dict.Remove(id);
        }

        public static void Clear()
        {
            foreach (var dict in _callbacks.Values)
                dict.Clear();
            _callbacks.Clear();
        }

        #endregion

        #region Helpers

        private static Dictionary<uint, Delegate> GetOrCreate<TCallback>()
        {
            var type = typeof(TCallback);
            if (!_callbacks.TryGetValue(type, out var dict))
            {
                dict = new Dictionary<uint, Delegate>();
                _callbacks[type] = dict;
            }
            return dict;
        }

        private static bool TryGetCallback<TCallback>(uint id, out Delegate del)
        {
            del = null;
            return _callbacks.TryGetValue(typeof(TCallback), out var dict) && dict.TryGetValue(id, out del);
        }

        private static void SafeInvoke(Action action)
        {
            try { action(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        #endregion
    }

    #endregion

    #region Callback Collector Implementation

    /// <summary>
    /// Default implementation of ICallbackCollector used by backends.
    /// </summary>
    public struct CallbackCollector : ICallbackCollector
    {
        private uint _timerId;

        public CallbackCollector(uint timerId) => _timerId = timerId;

        public void Trigger<TCallback>() where TCallback : struct, ITimerCallback
            => TimerCallbacks.Invoke<TCallback>(_timerId);

        public void Trigger<TCallback, TArg>(TArg value) where TCallback : struct, ITimerCallback
            => TimerCallbacks.Invoke<TCallback, TArg>(_timerId, value);
    }

    #endregion
}
