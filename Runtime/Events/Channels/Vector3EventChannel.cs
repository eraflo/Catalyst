using UnityEngine;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Event channel that carries a Vector3 value.
    /// Create via Assets > Create > Events > Vector3 Channel.
    /// </summary>
    [CreateAssetMenu(fileName = "NewVector3Channel", menuName = "Events/Vector3 Channel", order = 5)]
    public class Vector3EventChannel : EventChannel<Vector3> { }
}
