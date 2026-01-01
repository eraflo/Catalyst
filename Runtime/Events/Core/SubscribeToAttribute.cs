using System;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Attribute to automatically subscribe a method to an EventChannel field.
    /// Use with EventSubscriber base class for automatic subscription management.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyScript : EventSubscriber
    /// {
    ///     [SerializeField] IntEventChannel onScoreChanged;
    ///     
    ///     [SubscribeTo(nameof(onScoreChanged))]
    ///     void OnScoreChanged(int score)
    ///     {
    ///         Debug.Log($"Score: {score}");
    ///     }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SubscribeToAttribute : Attribute
    {
        /// <summary>
        /// Name of the EventChannel field to subscribe to.
        /// </summary>
        public string ChannelFieldName { get; }

        /// <summary>
        /// Creates a new SubscribeTo attribute.
        /// </summary>
        /// <param name="channelFieldName">Name of the EventChannel field (use nameof()).</param>
        public SubscribeToAttribute(string channelFieldName)
        {
            ChannelFieldName = channelFieldName;
        }
    }
}
