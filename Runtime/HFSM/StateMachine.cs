using System.Collections.Generic;
using Eraflo.Catalyst.Core.Chronos;
using Eraflo.Catalyst.Events;
using Eraflo.Catalyst.Networking;
using UnityEngine;

namespace Eraflo.Catalyst.HFSM
{
    /// <summary>
    /// The Hierarchical Finite State Machine controller.
    /// </summary>
    public class StateMachine
    {
        private readonly List<StateBase> _activePath = new List<StateBase>();
        private StateBase _rootState;
        private bool _isRunning;
        private AuthorityMode _authority = AuthorityMode.ServerAuthoritative;

        private ChronosManager _chronos;
        private EventBus _events;

        public StateBase ActiveState => _activePath.Count > 0 ? _activePath[_activePath.Count - 1] : null;
        public IReadOnlyList<StateBase> ActivePath => _activePath;
        
        public AuthorityMode Authority
        {
            get => _authority;
            set => _authority = value;
        }

        public void SetRootState(StateBase root)
        {
            _rootState = root;
            _rootState.Setup(this);
        }

        public void Start()
        {
            if (_rootState == null)
            {
                Debug.LogError("[HFSM] Cannot start StateMachine without a root state.");
                return;
            }

            _chronos = App.Get<ChronosManager>();
            _events = App.Get<EventBus>();

            _isRunning = true;
            ChangeState(_rootState);
        }

        public void Stop()
        {
            _isRunning = false;
            while (_activePath.Count > 0)
            {
                ExitState(_activePath[_activePath.Count - 1]);
            }
        }

        public void Update()
        {
            if (!_isRunning) return;

            // 1. Calculate time based on active state's channel
            var active = ActiveState;
            if (active == null) return;

            float dt = _chronos != null ? _chronos.GetDeltaTime(active.TimeChannel) : Time.deltaTime;
            
            // Increment duration for all active states
            foreach (var state in _activePath)
            {
                state.StateDuration += dt;
            }

            // 2. Logic (Bottom-Up)
            for (int i = _activePath.Count - 1; i >= 0; i--)
            {
                _activePath[i].OnLogic(dt);
            }
 
            // 3. Transitions (Bottom-Up Priority)
            for (int i = _activePath.Count - 1; i >= 0; i--)
            {
                var transition = _activePath[i].CheckTransitions();
                if (transition != null)
                {
                    ChangeState(transition.TargetState);
                    break;
                }
            }
        }

        public void FixedUpdate()
        {
            if (!_isRunning) return;
 
            var active = ActiveState;
            if (active == null) return;
 
            float dt = _chronos != null ? _chronos.GetDeltaTime(active.TimeChannel) : Time.fixedDeltaTime;
            for (int i = _activePath.Count - 1; i >= 0; i--)
            {
                _activePath[i].OnFixedLogic(dt);
            }
        }

        private void ChangeState(StateBase nextState)
        {
            if (nextState == null) return;

            // Drill down to the first leaf state
            while (nextState.Children.Count > 0)
            {
                nextState = nextState.Children[0];
            }

            // Find lowest common ancestor
            StateBase commonAncestor = null;
            var nextPath = nextState.FullPath;

            int minLength = Mathf.Min(_activePath.Count, nextPath.Count);
            for (int i = 0; i < minLength; i++)
            {
                if (_activePath[i] == nextPath[i])
                    commonAncestor = _activePath[i];
                else
                    break;
            }

            var previousActive = ActiveState;

            // Exit states down to the common ancestor (exclusive)
            while (ActiveState != commonAncestor)
            {
                ExitState(ActiveState);
            }

            // Enter states from the ancestor down to the target
            int startIndex = 0;
            for (int i = 0; i < nextPath.Count; i++)
            {
                if (nextPath[i] == commonAncestor)
                {
                    startIndex = i + 1;
                    break;
                }
            }

            for (int i = startIndex; i < nextPath.Count; i++)
            {
                EnterState(nextPath[i]);
            }

            // Publish event
            _events?.Publish(new HfsmStateChangedEvent(this, previousActive, ActiveState));
        }

        private void EnterState(StateBase state)
        {
            _activePath.Add(state);
            state.StateDuration = 0f;
            state.OnEnter();
        }

        private void ExitState(StateBase state)
        {
            state.OnExit();
            _activePath.Remove(state);
        }

        public void ChangeStateByPath(string path)
        {
            var target = FindStateByPath(path);
            if (target != null) ChangeState(target);
        }

        public StateBase FindStateByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var parts = path.Split('/');
            
            // Start from root's children if path doesn't start with root name
            StateBase current = _rootState;
            int startIndex = 0;

            if (parts[0] == _rootState.Name)
            {
                startIndex = 1;
            }

            for (int i = startIndex; i < parts.Length; i++)
            {
                bool found = false;
                foreach (var child in current.Children)
                {
                    if (child.Name == parts[i])
                    {
                        current = child;
                        found = true;
                        break;
                    }
                }
                if (!found) return null;
            }

            return current;
        }
    }
}
