using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eraflo.Catalyst
{
    /// <summary>
    /// Performs field injection of <see cref="IGameService"/> instances into any object
    /// whose fields are marked with <see cref="InjectAttribute"/>.
    /// <para>
    /// For <c>MonoBehaviour</c> components placed in a scene, injection is triggered
    /// automatically via <see cref="SceneManager.sceneLoaded"/> (before <c>Start</c>)
    /// through the hidden <see cref="ServiceInjectorBootstrap"/> component — no base class required.
    /// </para>
    /// <para>
    /// For any other object (plain C# class, ScriptableObject, runtime-instantiated component),
    /// call <see cref="Inject"/> once after construction.
    /// </para>
    /// </summary>
    public static class ServiceInjector
    {
        // Cache: Type → injectable fields discovered via reflection (built once per type).
        private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new Dictionary<Type, FieldInfo[]>();
        private static readonly FieldInfo[] _empty = Array.Empty<FieldInfo>();

        private static readonly BindingFlags _flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        // Reusable traversal buffers — injection always runs on the Unity main thread.
        private static readonly List<GameObject>  _rootBuffer      = new List<GameObject>();
        private static readonly List<MonoBehaviour> _behaviourBuffer = new List<MonoBehaviour>();

        // Reusable scratch list for building the per-type field array on cache miss.
        // Avoids one transient List<FieldInfo> allocation per type during startup scan.
        private static readonly List<FieldInfo> _fieldScratch = new List<FieldInfo>();

        // Reentrancy guard: prevents corruption of the shared buffers if InjectHierarchy
        // is invoked recursively (e.g. from a service Initialize or a MonoBehaviour Awake).
        private static bool _injecting;

        // Idempotency guard: ensures Bootstrap creates exactly one instance even if
        // RuntimeInitializeOnLoadMethod fires more than once (e.g. on domain reload in Editor).
        private static bool _bootstrapped;

        // ──────────────────────────────────────────────────────────────
        //  Bootstrap — runs AFTER services are initialized
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates the hidden bootstrap object that drives automatic scene injection.
        /// Guaranteed to run after <see cref="ServiceLocator.Initialize"/>
        /// (AfterAssembliesLoaded precedes BeforeSceneLoad).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_bootstrapped) return;
            _bootstrapped = true;

            var go = new GameObject("[ServiceInjector]")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ServiceInjectorBootstrap>();
        }

        // ──────────────────────────────────────────────────────────────
        //  Public API
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> and injects all
        /// <see cref="InjectAttribute"/> fields before returning it.
        /// Use this instead of <c>new T()</c> for classes marked with <see cref="InjectableAttribute"/>.
        /// </summary>
        /// <typeparam name="T">A class with a public parameterless constructor.</typeparam>
        public static T Create<T>() where T : class, new()
        {
            var instance = new T();
            Inject(instance);
            return instance;
        }

        /// <summary>
        /// Injects all <see cref="InjectAttribute"/> fields on <paramref name="target"/>
        /// with matching services from the <see cref="ServiceLocator"/>.
        /// Call this manually for objects created at runtime (e.g. via <c>new</c> or <c>Instantiate</c>).
        /// </summary>
        public static void Inject(object target)
        {
            if (target == null) return;

            var type   = target.GetType();
            var fields = GetInjectableFields(type);
            if (fields.Length == 0) return;

            InjectFields(target, type, fields);
        }

        // ──────────────────────────────────────────────────────────────
        //  Internal scene traversal
        // ──────────────────────────────────────────────────────────────

        internal static void InjectScene(Scene scene)
        {
            if (!scene.IsValid()) return;

            if (_injecting)
            {
                Debug.LogWarning("[ServiceInjector] Reentrant InjectScene call detected and skipped.");
                return;
            }

            _injecting = true;
            try
            {
                // GetRootGameObjects(List<>) avoids allocating a new array on every scene load.
                _rootBuffer.Clear();
                scene.GetRootGameObjects(_rootBuffer);

                foreach (var root in _rootBuffer)
                    InjectHierarchyInternal(root);
            }
            finally
            {
                _injecting = false;
            }
        }

        internal static void InjectHierarchy(GameObject root)
        {
            if (root == null) return;

            if (_injecting)
            {
                Debug.LogWarning("[ServiceInjector] Reentrant InjectHierarchy call detected and skipped.");
                return;
            }

            _injecting = true;
            try
            {
                InjectHierarchyInternal(root);
            }
            finally
            {
                _injecting = false;
            }
        }

        // Internal variant — called from within an already-guarded scope.
        private static void InjectHierarchyInternal(GameObject root)
        {
            if (root == null) return;

            // GetComponentsInChildren(bool, List<>) avoids per-root array allocation.
            _behaviourBuffer.Clear();
            root.GetComponentsInChildren(true, _behaviourBuffer);

            foreach (var behaviour in _behaviourBuffer)
            {
                if (behaviour == null) continue;  // missing script guard

                var type   = behaviour.GetType();
                var fields = GetInjectableFields(type);
                if (fields.Length > 0)
                    InjectFields(behaviour, type, fields);
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Core injection + reflection cache
        // ──────────────────────────────────────────────────────────────

        // `declaringType` is passed in so the warning path avoids a redundant target.GetType() call.
        private static void InjectFields(object target, Type declaringType, FieldInfo[] fields)
        {
            foreach (var field in fields)
            {
                var service = ServiceLocator.Get(field.FieldType);
                if (service != null)
                {
                    field.SetValue(target, service);
                }
                else
                {
                    Debug.LogWarning(
                        $"[ServiceInjector] No service registered for '{field.FieldType.Name}' " +
                        $"on '{declaringType.Name}.{field.Name}'.");
                }
            }
        }

        private static FieldInfo[] GetInjectableFields(Type type)
        {
            if (_fieldCache.TryGetValue(type, out var cached))
                return cached;

            // Reuse scratch list to avoid allocating a new List<FieldInfo> per type.
            _fieldScratch.Clear();

            var current = type;
            // For MonoBehaviour subclasses, stop at MonoBehaviour to skip Unity engine internals.
            // For plain C# classes, walk all the way up to (but not including) object.
            var isMono = typeof(MonoBehaviour).IsAssignableFrom(type);

            while (current != null && current != typeof(object))
            {
                if (isMono && current == typeof(MonoBehaviour))
                    break;

                foreach (var field in current.GetFields(_flags))
                {
                    if (field.GetCustomAttribute<InjectAttribute>(false) != null)
                        _fieldScratch.Add(field);
                }
                current = current.BaseType;
            }

            var result = _fieldScratch.Count > 0 ? _fieldScratch.ToArray() : _empty;
            _fieldCache[type] = result;
            return result;
        }
    }
}
