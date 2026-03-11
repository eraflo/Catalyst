using System;
using System.Collections.Generic;
using System.Threading;
using Eraflo.Catalyst.Core.Chronos;
using Eraflo.Catalyst.Core.Blackboard;

namespace Eraflo.Catalyst.HFSM
{
    /// <summary>
    /// Base class for all states in the Hierarchical Finite State Machine.
    /// </summary>
    public abstract class StateBase
    {
        protected StateMachine _fsm;
        protected StateBase _parent;
        protected BlackboardManager _blackboard;
        protected List<StateBase> _children = new List<StateBase>();
        protected List<Transition> _transitions = new List<Transition>();
        private List<StateBase> _fullPath;
        private CancellationTokenSource _cts;

        public string Name { get; protected set; }
        public float StateDuration { get; internal set; }
        public string TimeChannel { get; set; } = "World";
        
        public StateMachine Machine => _fsm;
        public StateBase Parent => _parent;
        public IReadOnlyList<StateBase> Children => _children;
        public IReadOnlyList<Transition> Transitions => _transitions;
        public IReadOnlyList<StateBase> FullPath => _fullPath;
        
        /// <summary>
        /// A cancellation token that is cancelled when the state exits.
        /// Use this for safe async operations started in OnEnter.
        /// </summary>
        public CancellationToken ExitToken => _cts?.Token ?? CancellationToken.None;

        protected StateBase(string name = null)
        {
            Name = name ?? GetType().Name;
        }

        internal void Setup(StateMachine fsm, StateBase parent = null)
        {
            _fsm = fsm;
            _parent = parent;
            _blackboard = App.Get<BlackboardManager>();
            
            // Pre-calculate path
            _fullPath = new List<StateBase>();
            var current = this;
            while (current != null)
            {
                _fullPath.Add(current);
                current = current._parent;
            }
            _fullPath.Reverse();
        }

        public virtual void OnEnter() 
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }
        public virtual void OnLogic(float dt) { }
        public virtual void OnFixedLogic(float dt) { }
        public virtual void OnExit() 
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void AddTransition(Transition transition)
        {
            _transitions.Add(transition);
        }

        public void AddChild(StateBase child)
        {
            child.Setup(_fsm, this);
            _children.Add(child);
        }

        internal Transition CheckTransitions()
        {
            for (int i = 0; i < _transitions.Count; i++)
            {
                var t = _transitions[i];
                if (t.Condition != null && t.Condition())
                    return t;
            }
            return null;
        }

        protected T GetBlackboardValue<T>(string key, T defaultValue = default)
        {
            if (_blackboard?.Global == null) return defaultValue;
            return _blackboard.Global.TryGet<T>(key, out var val) ? val : defaultValue;
        }

        protected void SetBlackboardValue<T>(string key, T value)
        {
            _blackboard?.Global?.Set(key, value);
        }
    }
}
