using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Base MonoBehaviour that automatically manages event subscriptions.
    /// Methods marked with [SubscribeTo] are auto-subscribed on enable and unsubscribed on disable.
    /// </summary>
    public abstract class EventSubscriber : MonoBehaviour
    {
        private readonly List<SubscriptionInfo> _subscriptions = new List<SubscriptionInfo>();

        protected virtual void OnEnable()
        {
            RegisterSubscriptions();
        }

        protected virtual void OnDisable()
        {
            UnregisterSubscriptions();
        }

        private void RegisterSubscriptions()
        {
            var type = GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes<SubscribeToAttribute>();
                
                foreach (var attr in attributes)
                {
                    var field = type.GetField(attr.ChannelFieldName, 
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (field == null)
                    {
                        Debug.LogError($"[EventSubscriber] Field '{attr.ChannelFieldName}' not found on {type.Name}");
                        continue;
                    }

                    var channel = field.GetValue(this);
                    if (channel == null)
                    {
                        Debug.LogWarning($"[EventSubscriber] Channel '{attr.ChannelFieldName}' is null on {type.Name}");
                        continue;
                    }

                    var subscription = CreateSubscription(channel, method);
                    if (subscription != null)
                    {
                        subscription.Subscribe();
                        _subscriptions.Add(subscription);
                    }
                }
            }
        }

        private void UnregisterSubscriptions()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Unsubscribe();
            }
            _subscriptions.Clear();
        }

        private SubscriptionInfo CreateSubscription(object channel, MethodInfo method)
        {
            var channelType = channel.GetType();

            // Handle void EventChannel
            if (channelType == typeof(EventChannel))
            {
                var action = (Action)Delegate.CreateDelegate(typeof(Action), this, method);
                return new VoidSubscription((EventChannel)channel, action);
            }

            // Handle typed EventChannel<T>
            var baseType = channelType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(EventChannel<>))
                {
                    var valueType = baseType.GetGenericArguments()[0];
                    var subscriptionType = typeof(TypedSubscription<>).MakeGenericType(valueType);
                    var actionType = typeof(Action<>).MakeGenericType(valueType);
                    
                    try
                    {
                        var action = Delegate.CreateDelegate(actionType, this, method);
                        return (SubscriptionInfo)Activator.CreateInstance(subscriptionType, channel, action);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventSubscriber] Failed to create subscription for {method.Name}: {e.Message}");
                        return null;
                    }
                }
                baseType = baseType.BaseType;
            }

            Debug.LogError($"[EventSubscriber] Unknown channel type: {channelType.Name}");
            return null;
        }

        #region Subscription Info Classes

        private abstract class SubscriptionInfo
        {
            public abstract void Subscribe();
            public abstract void Unsubscribe();
        }

        private class VoidSubscription : SubscriptionInfo
        {
            private readonly EventChannel _channel;
            private readonly Action _callback;

            public VoidSubscription(EventChannel channel, Action callback)
            {
                _channel = channel;
                _callback = callback;
            }

            public override void Subscribe() => _channel.Subscribe(_callback);
            public override void Unsubscribe() => _channel.Unsubscribe(_callback);
        }

        private class TypedSubscription<T> : SubscriptionInfo
        {
            private readonly EventChannel<T> _channel;
            private readonly Action<T> _callback;

            public TypedSubscription(object channel, object callback)
            {
                _channel = (EventChannel<T>)channel;
                _callback = (Action<T>)callback;
            }

            public override void Subscribe() => _channel.Subscribe(_callback);
            public override void Unsubscribe() => _channel.Unsubscribe(_callback);
        }

        #endregion
    }
}
