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

        // Reusable buffers — injection always runs on the Unity main thread, no reentrancy risk.
        private static readonly List<GameObject> _rootBuffer = new List<GameObject>();
        private static readonly List<MonoBehaviour> _behaviourBuffer = new List<MonoBehaviour>();

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

            var fields = GetInjectableFields(target.GetType());
            if (fields.Length == 0) return;

            InjectFields(target, fields);
        }

        // ──────────────────────────────────────────────────────────────
        //  Internal scene traversal
        // ──────────────────────────────────────────────────────────────

        internal static void InjectScene(Scene scene)
        {
            if (!scene.IsValid()) return;

            // GetRootGameObjects(List<>) avoids allocating a new array on every scene load.
            _rootBuffer.Clear();
            scene.GetRootGameObjects(_rootBuffer);

            foreach (var root in _rootBuffer)
                InjectHierarchy(root);
        }

        internal static void InjectHierarchy(GameObject root)
        {
            // GetComponentsInChildren(bool, List<>) avoids per-root array allocation.
            _behaviourBuffer.Clear();
            root.GetComponentsInChildren(true, _behaviourBuffer);

            foreach (var behaviour in _behaviourBuffer)
            {
                if (behaviour == null) continue;  // missing script guard

                var fields = GetInjectableFields(behaviour.GetType());
                if (fields.Length > 0)
                    InjectFields(behaviour, fields);  // reuses already-retrieved fields, no second lookup
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Core injection + reflection cache
        // ──────────────────────────────────────────────────────────────

        private static void InjectFields(object target, FieldInfo[] fields)
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
                        $"on '{target.GetType().Name}.{field.Name}'.");
                }
            }
        }

        private static FieldInfo[] GetInjectableFields(Type type)
        {
            if (_fieldCache.TryGetValue(type, out var cached))
                return cached;

            var fields = new List<FieldInfo>();
            var current = type;
            // For MonoBehaviour subclasses, stop at MonoBehaviour itself to skip Unity engine internals.
            // For plain C# classes, walk all the way up to (but not including) object.
            var isMono = typeof(MonoBehaviour).IsAssignableFrom(type);

            while (current != null && current != typeof(object))
            {
                if (isMono && current == typeof(MonoBehaviour))
                    break;

                foreach (var field in current.GetFields(_flags))
                {
                    if (field.GetCustomAttribute<InjectAttribute>(false) != null)
                        fields.Add(field);
                }
                current = current.BaseType;
            }

            var result = fields.Count > 0 ? fields.ToArray() : _empty;
            _fieldCache[type] = result;
            return result;
        }
    }
}
