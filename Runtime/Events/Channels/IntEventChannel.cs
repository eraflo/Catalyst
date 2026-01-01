using UnityEngine;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Event channel that carries an int value.
    /// Create via Assets > Create > Events > Int Channel.
    /// </summary>
    [CreateAssetMenu(fileName = "NewIntChannel", menuName = "Events/Int Channel", order = 1)]
    public class IntEventChannel : EventChannel<int> { }
}
