namespace Eraflo.Catalyst
{
    /// <summary>
    /// Global thread safety mode for all package systems.
    /// </summary>
    public enum PackageThreadMode
    {
        /// <summary>
        /// Fast mode - optimized for single-threaded main thread access only.
        /// Best performance but not thread-safe.
        /// </summary>
        SingleThread,
        
        /// <summary>
        /// Thread-safe mode - allows operations from any thread.
        /// Slightly slower but safe for async/multi-threaded scenarios.
        /// </summary>
        ThreadSafe
    }

    /// <summary>
    /// Global runtime configuration for the package.
    /// Provides centralized access to thread mode and other global settings.
    /// </summary>
    public static class PackageRuntime
    {
        private static PackageThreadMode _threadMode = PackageThreadMode.SingleThread;
        private static bool _initialized;

        private static int _mainThreadId;

        /// <summary>
        /// Global thread mode for all package systems.
        /// </summary>
        public static PackageThreadMode ThreadMode
        {
            get => _threadMode;
            set => _threadMode = value;
        }

        /// <summary>
        /// Whether thread-safe mode is enabled.
        /// </summary>
        public static bool IsThreadSafe => _threadMode == PackageThreadMode.ThreadSafe;

        /// <summary>
        /// Whether the current thread is the main Unity thread.
        /// </summary>
        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>
        /// Initializes from PackageSettings. Called automatically.
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            try
            {
                _threadMode = (PackageThreadMode)(int)PackageSettings.Instance.ThreadMode;
            }
            catch
            {
                _threadMode = PackageThreadMode.SingleThread;
            }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    _initialized = false;
                    _threadMode = PackageThreadMode.SingleThread;
                }
            };
        }
#endif
    }
}
