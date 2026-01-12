using UnityEngine;

namespace Eraflo.Catalyst.Command
{
    /// <summary>
    /// Interface for commands that support rebinding to a different target.
    /// Essential for replaying actions on 'Ghost' objects or redirected actors.
    /// </summary>
    public interface IRebindableCommand : ICommand
    {
        /// <summary>
        /// Rebinds the command's primary target to a new GameObject.
        /// </summary>
        void Rebind(GameObject newTarget);
    }
}
