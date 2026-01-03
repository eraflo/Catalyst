using UnityEngine;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Listener for StringEventChannel.
    /// </summary>
    [AddComponentMenu("Catalyst/Events/String Channel Listener")]
    public class StringEventChannelListener : EventChannelListener<StringEventChannel, string> { }
}
