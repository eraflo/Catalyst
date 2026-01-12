using System;

namespace Eraflo.Catalyst.HFSM
{
    /// <summary>
    /// A transition between two states based on a condition.
    /// </summary>
    public class Transition
    {
        public StateBase TargetState { get; }
        public Func<bool> Condition { get; }

        public Transition(StateBase targetState, Func<bool> condition)
        {
            TargetState = targetState;
            Condition = condition;
        }
    }
}
