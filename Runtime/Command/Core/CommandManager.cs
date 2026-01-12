using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Eraflo.Catalyst.Core.Chronos;
using Eraflo.Catalyst.Events;

namespace Eraflo.Catalyst.Command
{
    /// <summary>
    /// Manages execution, history, and synchronization of commands.
    /// </summary>
    [Service(Priority = 55)]
    public class CommandManager : IGameService
    {
        private readonly LinkedList<ICommand> _undoStack = new LinkedList<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
        
        private ChronosManager _chronos;
        private EventBus _eventBus;

        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        public int MaxHistorySize { get; set; } = 50;

        public void Initialize()
        {
            _chronos = App.Get<ChronosManager>();
            _eventBus = App.Get<EventBus>();
        }

        public void Shutdown()
        {
            ClearHistory();
        }

        /// <summary>
        /// Executes a command and adds it to the undo history.
        /// </summary>
        public async Task Execute(ICommand command)
        {
            if (command == null || !command.CanExecute()) return;

            await command.Execute();

            _undoStack.AddLast(command);
            _redoStack.Clear(); // New action invalidates redo stack

            if (_undoStack.Count > MaxHistorySize)
            {
                _undoStack.RemoveFirst(); // Remove oldest
            }

            NotifyExecuted(command);
        }

        /// <summary>
        /// Executes a command without adding it to history or triggering events.
        /// Primarily used for Network synchronization and Replays.
        /// </summary>
        public async Task ExecuteDirect(ICommand command)
        {
            if (command == null || !command.CanExecute()) return;
            await command.Execute();
        }

        private void NotifyExecuted(ICommand command)
        {
            float time = _chronos != null ? _chronos.AppTime : Time.time;
            _eventBus?.Publish(new CommandExecutedEvent { Command = command, Timestamp = time });
        }

        /// <summary>
        /// Undoes the last command in the history.
        /// </summary>
        public async Task Undo()
        {
            if (_undoStack.Count == 0) return;

            ICommand command = _undoStack.Last.Value;
            _undoStack.RemoveLast();
            
            await command.Undo();
            _redoStack.Push(command);

            float time = _chronos != null ? _chronos.AppTime : Time.time;
            _eventBus?.Publish(new CommandUndoneEvent { Command = command, Timestamp = time });
        }

        /// <summary>
        /// Redoes the last undone command.
        /// </summary>
        public async Task Redo()
        {
            if (_redoStack.Count == 0) return;

            ICommand command = _redoStack.Pop();
            await command.Execute();
            _undoStack.AddLast(command);

            float time = _chronos != null ? _chronos.AppTime : Time.time;
            _eventBus?.Publish(new CommandRedoneEvent { Command = command, Timestamp = time });
        }

        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
