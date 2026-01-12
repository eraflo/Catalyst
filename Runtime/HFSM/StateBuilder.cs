using System;
using System.Collections.Generic;

namespace Eraflo.Catalyst.HFSM
{
    /// <summary>
    /// Builder used to configure a StateBase with transitions and hierarchy in a fluent way.
    /// </summary>
    public class StateBuilder
    {
        private readonly StateBase _state;

        public StateBuilder(StateBase state)
        {
            _state = state;
        }

        public StateBuilder AddTransition(StateBase target, Func<bool> condition)
        {
            _state.AddTransition(new Transition(target, condition));
            return this;
        }

        public StateBuilder AddChild(StateBase child)
        {
            _state.AddChild(child);
            return this;
        }

        public StateBase Build() => _state;
    }

    /// <summary>
    /// A simple state that executes callbacks. Ideal for the Builder API.
    /// </summary>
    public class HfsmCallbackState : StateBase
    {
        private Action _onEnter;
        private Action<float> _onLogic;
        private Action _onExit;

        public HfsmCallbackState(string name = null, Action onEnter = null, Action<float> onLogic = null, Action onExit = null) 
            : base(name)
        {
            _onEnter = onEnter;
            _onLogic = onLogic;
            _onExit = onExit;
        }

        public override void OnEnter() => _onEnter?.Invoke();
        public override void OnLogic(float dt) => _onLogic?.Invoke(dt);
        public override void OnExit() => _onExit?.Invoke();
    }
}
