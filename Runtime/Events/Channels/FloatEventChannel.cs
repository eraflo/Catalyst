using UnityEngine;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Event channel that carries a float value.
    /// Create via Assets > Create > Events > Float Channel.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFloatChannel", menuName = "Catalyst/Events/Float Channel", order = 2)]
    public class FloatEventChannel : EventChannel<float> { }
}
