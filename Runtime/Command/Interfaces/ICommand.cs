using System.Threading.Tasks;

namespace Eraflo.Catalyst.Command
{
    /// <summary>
    /// Base interface for all commands in the Command System.
    /// Commands must be serializable by the Save module to support Replay and Sync.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Logic for executing the command.
        /// </summary>
        Task Execute();

        /// <summary>
        /// Logic for reverting the command.
        /// </summary>
        Task Undo();

        /// <summary>
        /// Optional validation before execution.
        /// </summary>
        bool CanExecute() => true;
    }
}
