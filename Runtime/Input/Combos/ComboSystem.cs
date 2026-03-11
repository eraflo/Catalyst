using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Eraflo.Catalyst.Timers;
using Timer = Eraflo.Catalyst.Timers.Timer;

namespace Eraflo.Catalyst.InputSystem.Combos
{
    /// <summary>
    /// Handles combo detection using a Trie (Prefix Tree).
    /// </summary>
    public class ComboSystem
    {
        private class TrieNode
        {
            public Dictionary<string, TrieNode> Children = new Dictionary<string, TrieNode>();
            public ComboDefinition Combo;
            public bool IsLeaf => Combo != null;
        }

        private readonly TrieNode _root = new TrieNode();
        private TrieNode _currentNode;
        private float _resetTimeout = 1.0f; 
        private TimerHandle _resetTimer;
        [Inject] private Timer _timerManager;

        public event Action<ComboDefinition> OnComboExecuted;

        public float ResetTimeout
        {
            get => _resetTimeout;
            set => _resetTimeout = value;
        }

        public ComboSystem(ComboDatabase database)
        {
            ServiceInjector.Inject(this);
            _currentNode = _root;
            BuildTrie(database);
        }

        private void BuildTrie(ComboDatabase database)
        {
            if (database == null || database.Combos == null) return;

            foreach (var combo in database.Combos)
            {
                var current = _root;
                foreach (var actionId in combo.Sequence)
                {
                    if (!current.Children.TryGetValue(actionId, out var next))
                    {
                        next = new TrieNode();
                        current.Children[actionId] = next;
                    }
                    current = next;
                }
                
                // If multiple combos have the same sequence, priority wins
                if (current.Combo == null || combo.Priority > current.Combo.Priority)
                {
                    current.Combo = combo;
                }
            }
        }

        /// <summary>
        /// Advances the Trie based on the provided action ID.
        /// </summary>
        public void ProcessInput(string actionId, float timestamp)
        {
            // Try to find child
            if (_currentNode.Children.TryGetValue(actionId, out var nextNode))
            {
                _currentNode = nextNode;
            }
            else
            {
                // If not found, check if the action can start a new sequence from root
                if (_root.Children.TryGetValue(actionId, out var rootStartNode))
                {
                    _currentNode = rootStartNode;
                }
                else
                {
                    _currentNode = _root;
                }
            }

            // Start or restart reset timer
            if (_timerManager != null)
            {
                if (_resetTimer.IsValid) _timerManager.CancelTimer(_resetTimer);
                _resetTimer = _timerManager.CreateDelay(_resetTimeout, Reset, true);
            }

            // Check for leaf
            if (_currentNode.IsLeaf)
            {
                OnComboExecuted?.Invoke(_currentNode.Combo);
                Reset();
            }
        }

        /// <summary>
        /// Asynchronously waits for a specific combo to be executed.
        /// </summary>
        public async Task<ComboDefinition> WaitForComboAsync(string comboId, CancellationToken token = default)
        {
            var tcs = new TaskCompletionSource<ComboDefinition>();

            Action<ComboDefinition> handler = null;
            handler = (combo) =>
            {
                if (combo.ComboId == comboId)
                {
                    OnComboExecuted -= handler;
                    tcs.TrySetResult(combo);
                }
            };

            OnComboExecuted += handler;

            if (token.CanBeCanceled)
            {
                token.Register(() => 
                {
                    OnComboExecuted -= handler;
                    tcs.TrySetCanceled();
                });
            }

            return await tcs.Task;
        }
        
        public void Reset()
        {
            _currentNode = _root;
            if (_timerManager != null && _resetTimer.IsValid)
            {
                _timerManager.CancelTimer(_resetTimer);
                _resetTimer = TimerHandle.None;
            }
        }
    }
}
