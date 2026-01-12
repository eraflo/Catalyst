using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Eraflo.Catalyst.Command
{
    /// <summary>
    /// A composite command that groups multiple commands into a single execution/undo step.
    /// Useful for complex atomic actions.
    /// </summary>
    public class CompositeCommand : IRebindableCommand
    {
        [Newtonsoft.Json.JsonProperty]
        private readonly List<ICommand> _commands = new List<ICommand>();

        [Newtonsoft.Json.JsonIgnore]
        public IEnumerable<ICommand> Commands => _commands;

        public void Rebind(GameObject newTarget)
        {
            foreach (var cmd in _commands)
            {
                if (cmd is IRebindableCommand rebindable)
                    rebindable.Rebind(newTarget);
            }
        }

        public void Add(ICommand command)
        {
            if (command != null) _commands.Add(command);
        }

        public async Task Execute()
        {
            foreach (var cmd in _commands)
            {
                await cmd.Execute();
            }
        }

        public async Task Undo()
        {
            // Undo in reverse order
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                await _commands[i].Undo();
            }
        }

        public bool CanExecute()
        {
            foreach (var cmd in _commands)
            {
                if (!cmd.CanExecute()) return false;
            }
            return true;
        }
    }
}
