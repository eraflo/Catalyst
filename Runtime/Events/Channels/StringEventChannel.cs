using UnityEngine;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Event channel that carries a string value.
    /// Create via Assets > Create > Events > String Channel.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStringChannel", menuName = "Events/String Channel", order = 3)]
    public class StringEventChannel : EventChannel<string> { }
}
