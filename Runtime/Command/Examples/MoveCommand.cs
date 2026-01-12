using System.Threading.Tasks;
using UnityEngine;

namespace Eraflo.Catalyst.Command.Examples
{
    /// <summary>
    /// A simple command to move an object to a specific position.
    /// Supports Undo.
    /// </summary>
    public class MoveCommand : IRebindableCommand
    {
        public GameObject Target;
        public Vector3 NewPosition;
        
        [Newtonsoft.Json.JsonIgnore]
        private Vector3 _oldPosition;

        public void Rebind(GameObject newTarget) => Target = newTarget;

        // Required for deserialization
        public MoveCommand() { }

        public MoveCommand(GameObject target, Vector3 newPosition)
        {
            Target = target;
            NewPosition = newPosition;
        }

        public Task Execute()
        {
            if (Target != null)
            {
                _oldPosition = Target.transform.position;
                Target.transform.position = NewPosition;
            }
            return Task.CompletedTask;
        }

        public Task Undo()
        {
            if (Target != null)
            {
                Target.transform.position = _oldPosition;
            }
            return Task.CompletedTask;
        }

        public bool CanExecute() => Target != null;
    }
}
