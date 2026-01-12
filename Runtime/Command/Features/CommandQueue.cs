using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Eraflo.Catalyst.Command.Features
{
    /// <summary>
    /// Allows queuing multiple commands to be executed sequentially, 
    /// optionally with delays between them.
    /// </summary>
    public class CommandQueue
    {
        private readonly Queue<(ICommand command, float delay)> _queue = new Queue<(ICommand, float)>();
        private bool _isRunning;

        /// <summary>
        /// Adds a command to the queue.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="delayBefore">Delay in seconds before this command starts.</param>
        public void Enqueue(ICommand command, float delayBefore = 0f)
        {
            _queue.Enqueue((command, delayBefore));
            if (!_isRunning) _ = ProcessQueue();
        }

        private async Task ProcessQueue()
        {
            _isRunning = true;

            while (_queue.Count > 0)
            {
                var item = _queue.Dequeue();
                
                if (item.delay > 0)
                {
                    await Task.Delay((int)(item.delay * 1000));
                }

                await App.Get<CommandManager>().Execute(item.command);
            }

            _isRunning = false;
        }

        public void Clear()
        {
            _queue.Clear();
        }
    }
}
