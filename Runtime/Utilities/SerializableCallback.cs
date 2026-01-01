using System;
using System.Reflection;
using UnityEngine;

namespace Eraflo.Catalyst.Utilities
{
    /// <summary>
    /// Stores callback metadata for serialization/persistence.
    /// Uses reflection to store and restore method references.
    /// Can be used for any delegate serialization needs.
    /// </summary>
    [Serializable]
    public class SerializableCallback
    {
        /// <summary>Type name of the callback (optional context).</summary>
        public string CallbackTypeName;
        
        /// <summary>Instance ID of the target Unity Object.</summary>
        public int TargetInstanceId;
        
        /// <summary>Assembly-qualified type name of the declaring class.</summary>
        public string DeclaringTypeName;
        
        /// <summary>Name of the method.</summary>
        public string MethodName;
        
        /// <summary>Whether this callback has a parameter.</summary>
        public bool HasParameter;
        
        /// <summary>Assembly-qualified type name of the parameter (if any).</summary>
        public string ParameterTypeName;

        /// <summary>
        /// Creates a SerializableCallback from a delegate.
        /// </summary>
        /// <param name="callback">The delegate to serialize.</param>
        /// <param name="callbackTypeName">Optional type name for context.</param>
        /// <returns>Serializable callback, or null if delegate cannot be serialized.</returns>
        public static SerializableCallback FromDelegate(Delegate callback, string callbackTypeName = null)
        {
            if (callback == null) return null;
            
            var target = callback.Target;
            var method = callback.Method;

            // Only Unity Objects can be properly serialized
            if (target != null && !(target is UnityEngine.Object))
            {
                Debug.LogWarning($"[SerializableCallback] Target is not a UnityEngine.Object, cannot serialize: {target.GetType().Name}");
                return null;
            }

            // Anonymous methods/lambdas cannot be serialized
            if (method.Name.Contains("<") || method.Name.Contains(">"))
            {
                Debug.LogWarning($"[SerializableCallback] Anonymous method cannot be serialized: {method.Name}");
                return null;
            }

            var unityObj = target as UnityEngine.Object;
            var parameters = method.GetParameters();

            return new SerializableCallback
            {
                CallbackTypeName = callbackTypeName ?? "",
                TargetInstanceId = unityObj != null ? unityObj.GetInstanceID() : 0,
                DeclaringTypeName = method.DeclaringType?.AssemblyQualifiedName,
                MethodName = method.Name,
                HasParameter = parameters.Length > 0,
                ParameterTypeName = parameters.Length > 0 ? parameters[0].ParameterType.AssemblyQualifiedName : null
            };
        }

        /// <summary>
        /// Reconstructs the delegate from stored metadata.
        /// </summary>
        /// <returns>The restored delegate, or null if restoration failed.</returns>
        public Delegate ToDelegate()
        {
            if (string.IsNullOrEmpty(DeclaringTypeName) || string.IsNullOrEmpty(MethodName))
                return null;

            try
            {
                // Find the target object by instance ID
                UnityEngine.Object target = null;
                if (TargetInstanceId != 0)
                {
                    var allObjects = UnityEngine.Object.FindObjectsOfType<UnityEngine.Object>();
                    foreach (var obj in allObjects)
                    {
                        if (obj.GetInstanceID() == TargetInstanceId)
                        {
                            target = obj;
                            break;
                        }
                    }

                    if (target == null)
                    {
                        Debug.LogWarning($"[SerializableCallback] Could not find object with instance ID {TargetInstanceId}");
                        return null;
                    }
                }

                // Get the declaring type
                var declaringType = Type.GetType(DeclaringTypeName);
                if (declaringType == null)
                {
                    Debug.LogWarning($"[SerializableCallback] Could not find type: {DeclaringTypeName}");
                    return null;
                }

                // Get the method
                MethodInfo method;
                if (HasParameter && !string.IsNullOrEmpty(ParameterTypeName))
                {
                    var paramType = Type.GetType(ParameterTypeName);
                    method = declaringType.GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new[] { paramType }, null);
                }
                else
                {
                    method = declaringType.GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, Type.EmptyTypes, null);
                }

                if (method == null)
                {
                    Debug.LogWarning($"[SerializableCallback] Could not find method: {MethodName} on {DeclaringTypeName}");
                    return null;
                }

                // Create the delegate
                Type delegateType;
                if (HasParameter && !string.IsNullOrEmpty(ParameterTypeName))
                {
                    var paramType = Type.GetType(ParameterTypeName);
                    delegateType = typeof(Action<>).MakeGenericType(paramType);
                }
                else
                {
                    delegateType = typeof(Action);
                }

                return method.CreateDelegate(delegateType, target);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializableCallback] Failed to restore callback: {ex.Message}");
                return null;
            }
        }
    }
}
