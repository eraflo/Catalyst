namespace Eraflo.Catalyst.Command
{
    /// <summary>
    /// Fired when a command is successfully executed.
    /// </summary>
    public struct CommandExecutedEvent
    {
        public ICommand Command;
        public float Timestamp;
    }

    /// <summary>
    /// Fired when a command is undone.
    /// </summary>
    public struct CommandUndoneEvent
    {
        public ICommand Command;
        public float Timestamp;
    }

    /// <summary>
    /// Fired when a command is redone.
    /// </summary>
    public struct CommandRedoneEvent
    {
        public ICommand Command;
        public float Timestamp;
    }
}
