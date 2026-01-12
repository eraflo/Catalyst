namespace Eraflo.Catalyst.HFSM
{
    /// <summary>
    /// Event fired when a state machine changes its active state.
    /// </summary>
    public struct HfsmStateChangedEvent
    {
        public StateMachine Machine;
        public StateBase PreviousState;
        public StateBase NewState;

        public HfsmStateChangedEvent(StateMachine machine, StateBase previous, StateBase next)
        {
            Machine = machine;
            PreviousState = previous;
            NewState = next;
        }
    }
}
