using System;
using System.Collections.Generic;
using UnityEngine;
using Eraflo.Catalyst.EasingSystem;

namespace Eraflo.Catalyst.Core.Chronos
{
    [Service(Priority = 41)]
    public class ChronosManager : IGameService, IUpdatable
    {
        private class TimeChannel
        {
            public string Id;
            public float Scale = 1f;
            public float TargetScale = 1f;
            public float StartScale = 1f;
            public float TransitionDuration;
            public float TransitionElapsed;
            public EasingType EaseType;
            public bool IsTransitioning;
            public bool IsUnscaled; 

            public TimeChannel(string id, bool isUnscaled = false)
            {
                Id = id;
                IsUnscaled = isUnscaled;
            }
        }

        private readonly Dictionary<string, TimeChannel> _channels = new Dictionary<string, TimeChannel>();
        public const string DefaultChannel = "World";
        public const string UIChannel = "UI";

        private float _globalScale = 1f;
        private float _appTime;

        public float AppTime => _appTime;

        public float GlobalScale
        {
            get => _globalScale;
            set
            {
                _globalScale = value;
                Time.timeScale = value;
                Time.fixedDeltaTime = 0.02f * value;
            }
        }

        public event Action<string, float, float, EasingType> OnChannelTransitionStarted;

        public void Initialize()
        {
            _globalScale = Time.timeScale;
            RegisterChannel(DefaultChannel, false);
            RegisterChannel(UIChannel, true);
        }

        public void Shutdown() { }

        public void RegisterChannel(string id, bool isUnscaled = false)
        {
            if (!_channels.ContainsKey(id))
            {
                _channels.Add(id, new TimeChannel(id, isUnscaled));
            }
        }

        public float GetChannelScale(string id)
        {
            return _channels.TryGetValue(id, out var channel) ? channel.Scale : 1f;
        }

        public float GetDeltaTime(string id)
        {
            if (!_channels.TryGetValue(id, out var channel)) 
                return Time.unscaledDeltaTime * _globalScale;
            
            if (channel.IsUnscaled)
            {
                return Time.unscaledDeltaTime * channel.Scale;
            }
            
            return Time.unscaledDeltaTime * _globalScale * channel.Scale;
        }

        public float GetFixedDeltaTime(string id)
        {
            if (!_channels.TryGetValue(id, out var channel)) 
                return Time.fixedUnscaledDeltaTime * _globalScale;

            if (channel.IsUnscaled)
            {
                return Time.fixedUnscaledDeltaTime * channel.Scale;
            }

            return Time.fixedUnscaledDeltaTime * _globalScale * channel.Scale;
        }

        public void SetTimeScale(string id, float targetScale, float duration = 0f, EasingType ease = EasingType.Linear)
        {
            if (!_channels.TryGetValue(id, out var channel))
            {
                channel = new TimeChannel(id);
                _channels.Add(id, channel);
            }

            if (duration <= 0f)
            {
                channel.Scale = targetScale;
                channel.TargetScale = targetScale;
                channel.IsTransitioning = false;
                return;
            }

            channel.StartScale = channel.Scale;
            channel.TargetScale = targetScale;
            channel.TransitionDuration = duration;
            channel.TransitionElapsed = 0f;
            channel.EaseType = ease;
            channel.IsTransitioning = true;

            OnChannelTransitionStarted?.Invoke(id, targetScale, duration, ease);
        }

        public void OnUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            foreach (var channel in _channels.Values)
            {
                if (!channel.IsTransitioning) continue;

                channel.TransitionElapsed += dt;
                float t = Mathf.Clamp01(channel.TransitionElapsed / channel.TransitionDuration);
                float easedT = Easing.Evaluate(t, channel.EaseType);

                channel.Scale = Mathf.LerpUnclamped(channel.StartScale, channel.TargetScale, easedT);

                if (t >= 1f)
                {
                    channel.Scale = channel.TargetScale;
                    channel.IsTransitioning = false;
                }
            }

            _appTime += dt * _globalScale;
        }

        public void PauseGame() => GlobalScale = 0f;
        public void ResumeGame() => GlobalScale = 1f;
    }
}
