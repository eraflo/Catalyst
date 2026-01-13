using System;
using System.Collections.Generic;
using Eraflo.Catalyst.Core.Save;
using UnityEngine;

namespace Eraflo.Catalyst.Core.Blackboard
{
    /// <summary>
    /// Service managing global and scoped blackboards.
    /// </summary>
    [Service(Priority = -5)]
    public class BlackboardManager : IGameService, ISaveable
    {
        private Blackboard _global;

        /// <summary>
        /// The global blackboard instance.
        /// </summary>
        public Blackboard Global => _global;

        public void Initialize()
        {
            _global = new Blackboard();
        }

        public void Shutdown()
        {
            _global = null;
        }

        /// <summary>
        /// Creates a new blackboard scoped to the global one.
        /// </summary>
        public Blackboard CreateScoped()
        {
            var scoped = new Blackboard();
            scoped.SetParent(_global);
            return scoped;
        }

        #region ISaveable Implementation

        public string SaveId => "GlobalBlackboard";

        public object SaveState()
        {
            // Capture entries from global blackboard
            return _global?.GetEntries();
        }

        public void LoadState(object state)
        {
            if (state is List<Blackboard.BlackboardEntry> entries)
            {
                _global.RestoreEntries(entries);
            }
        }

        #endregion
    }
}
