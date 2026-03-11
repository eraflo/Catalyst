using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Service that manages LOD-based ticking of all registered <see cref="BehaviourTreeRunner"/>
    /// instances. Runners are sorted into three distance-based tiers and ticked at decreasing
    /// frequencies to keep CPU usage proportional to visual relevance.
    ///
    /// Tier 0 (distance &lt; <see cref="Tier1Distance"/>): ticked every frame.
    /// Tier 1 (distance &lt; <see cref="Tier2Distance"/>): ticked every 3 frames.
    /// Tier 2 (distance &gt;= <see cref="Tier2Distance"/>): ticked every 10 frames.
    ///
    /// A per-frame time budget (<see cref="MaxMsPerFrame"/>) is enforced via a
    /// <see cref="System.Diagnostics.Stopwatch"/>. Round-robin ordering within each tier
    /// ensures runners that were skipped due to budget pressure get priority next frame.
    /// </summary>
    [Service(Priority = 52)]
    public class BTSchedulerService : IGameService, IUpdatable
    {
        // ── LOD distance thresholds ───────────────────────────────────────────────

        /// <summary>
        /// Runners closer than this distance to the reference camera are placed in Tier 0
        /// and ticked every frame. Default: 15.
        /// </summary>
        public float Tier1Distance { get; set; } = 15f;

        /// <summary>
        /// Runners between <see cref="Tier1Distance"/> and this distance are placed in Tier 1
        /// and ticked every 3 frames. Default: 50.
        /// </summary>
        public float Tier2Distance { get; set; } = 50f;

        // ── Tier tick intervals ───────────────────────────────────────────────────

        private const int Tier1Interval = 3;
        private const int Tier2Interval = 10;

        // ── Per-frame budget ──────────────────────────────────────────────────────

        /// <summary>
        /// Maximum wall-clock milliseconds to spend ticking BehaviourTree runners per frame.
        /// Ticking stops early if this budget is exceeded. Default: 2 ms.
        /// </summary>
        public float MaxMsPerFrame { get; set; } = 2f;

        // ── Per-tier runner lists ─────────────────────────────────────────────────

        private readonly List<BehaviourTreeRunner> _tier0 = new();
        private readonly List<BehaviourTreeRunner> _tier1 = new();
        private readonly List<BehaviourTreeRunner> _tier2 = new();

        // ── Round-robin offsets ───────────────────────────────────────────────────
        // Each offset tracks where ticking stopped last frame so that runners skipped
        // due to budget pressure receive priority on the next eligible frame.

        private int _tier0Index;
        private int _tier1Index;
        private int _tier2Index;

        // ── Original update modes (restored on Unregister) ───────────────────────

        private readonly Dictionary<BehaviourTreeRunner, BehaviourTreeRunner.UpdateMode> _originalModes = new();

        // ── Internal state ────────────────────────────────────────────────────────

        private Camera _camera;
        private int _frameCount;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        // ─────────────────────────────────────────────────────────────────────────
        //  IGameService
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Called by <see cref="ServiceLocator"/> after the service is created.</summary>
        public void Initialize()
        {
            _frameCount   = 0;
            _tier0Index   = 0;
            _tier1Index   = 0;
            _tier2Index   = 0;
        }

        /// <summary>
        /// Called on application quit. Restores the original <see cref="BehaviourTreeRunner.UpdateMode"/>
        /// of every registered runner before clearing internal state.
        /// </summary>
        public void Shutdown()
        {
            foreach (var pair in _originalModes)
            {
                if (pair.Key != null)
                    pair.Key.Mode = pair.Value;
            }

            _originalModes.Clear();
            _tier0.Clear();
            _tier1.Clear();
            _tier2.Clear();
            _tier0Index = 0;
            _tier1Index = 0;
            _tier2Index = 0;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Registration
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a <see cref="BehaviourTreeRunner"/> with the scheduler.
        /// The runner's <see cref="BehaviourTreeRunner.UpdateMode"/> is saved and switched to
        /// <see cref="BehaviourTreeRunner.UpdateMode.Manual"/> so this service drives all ticking.
        /// </summary>
        /// <param name="runner">The runner to manage. Null-safe — no-op if null.</param>
        public void Register(BehaviourTreeRunner runner)
        {
            if (runner == null) return;
            if (_originalModes.ContainsKey(runner)) return; // Already registered.

            _originalModes[runner] = runner.Mode;
            runner.Mode = BehaviourTreeRunner.UpdateMode.Manual;

            // Place in Tier 0 initially; RebalanceTiers() will move it if needed.
            _tier0.Add(runner);
        }

        /// <summary>
        /// Unregisters a runner and restores its original <see cref="BehaviourTreeRunner.UpdateMode"/>.
        /// </summary>
        /// <param name="runner">The runner to release. Null-safe — no-op if null.</param>
        public void Unregister(BehaviourTreeRunner runner)
        {
            if (runner == null) return;

            if (_originalModes.TryGetValue(runner, out var originalMode))
            {
                runner.Mode = originalMode;
                _originalModes.Remove(runner);
            }

            RemoveFromAllTiers(runner);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  IUpdatable
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called every frame by the <see cref="ServiceLocator"/> player-loop hook.
        /// Rebalances tier membership, then ticks each tier according to its interval,
        /// stopping early if <see cref="MaxMsPerFrame"/> is exceeded.
        /// </summary>
        public void OnUpdate()
        {
            _frameCount++;

            // Refresh camera reference if the previous one has been destroyed.
            if (_camera == null)
                _camera = Camera.main;

            _stopwatch.Restart();

            // Redistribute runners among tiers based on current camera distance.
            RebalanceTiers();

            // Tier 0 — tick every frame.
            TickTier(_tier0, ref _tier0Index);
            if (_stopwatch.Elapsed.TotalMilliseconds >= MaxMsPerFrame) return;

            // Tier 1 — tick every 3 frames.
            if (_frameCount % Tier1Interval == 0)
            {
                TickTier(_tier1, ref _tier1Index);
                if (_stopwatch.Elapsed.TotalMilliseconds >= MaxMsPerFrame) return;
            }

            // Tier 2 — tick every 10 frames.
            if (_frameCount % Tier2Interval == 0)
            {
                TickTier(_tier2, ref _tier2Index);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Tier rebalancing
        // ─────────────────────────────────────────────────────────────────────────

        private void RebalanceTiers()
        {
            if (_camera == null)
            {
                // No camera available — fall back to ticking everything every frame.
                MoveListToTier0(_tier1);
                MoveListToTier0(_tier2);
                return;
            }

            Vector3 referencePos = _camera.transform.position;

            // Iterate each tier backwards so safe in-place removal is possible.
            Redistribute(_tier0, 0, referencePos);
            Redistribute(_tier1, 1, referencePos);
            Redistribute(_tier2, 2, referencePos);
        }

        /// <summary>
        /// Moves every non-null entry from <paramref name="source"/> into <see cref="_tier0"/>
        /// and clears <paramref name="source"/>.
        /// </summary>
        private void MoveListToTier0(List<BehaviourTreeRunner> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                    _tier0.Add(source[i]);
            }
            source.Clear();
        }

        /// <summary>
        /// Scans <paramref name="tier"/> (identified by <paramref name="tierIndex"/>),
        /// prunes null entries (runners destroyed without calling <see cref="Unregister"/>),
        /// and moves any runner whose distance no longer matches this tier to the correct one.
        /// </summary>
        private void Redistribute(List<BehaviourTreeRunner> tier, int tierIndex, Vector3 referencePos)
        {
            for (int i = tier.Count - 1; i >= 0; i--)
            {
                var runner = tier[i];

                if (runner == null)
                {
                    // Runner GameObject was destroyed without calling Unregister.
                    // Prune the stale entry; the _originalModes entry will be ignored
                    // since its destroyed-object key will never match future lookups.
                    tier.RemoveAt(i);
                    continue;
                }

                int targetTier = GetTierIndex(runner, referencePos);
                if (targetTier == tierIndex) continue;

                tier.RemoveAt(i);
                GetTierList(targetTier).Add(runner);
            }
        }

        private int GetTierIndex(BehaviourTreeRunner runner, Vector3 referencePos)
        {
            float dist = Vector3.Distance(runner.transform.position, referencePos);
            if (dist < Tier1Distance) return 0;
            if (dist < Tier2Distance) return 1;
            return 2;
        }

        private List<BehaviourTreeRunner> GetTierList(int tierIndex) => tierIndex switch
        {
            0 => _tier0,
            1 => _tier1,
            _ => _tier2
        };

        private void RemoveFromAllTiers(BehaviourTreeRunner runner)
        {
            _tier0.Remove(runner);
            _tier1.Remove(runner);
            _tier2.Remove(runner);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Round-robin ticking
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Ticks runners in <paramref name="runners"/>, beginning at <paramref name="index"/>
        /// (round-robin continuation from the previous eligible frame).
        /// <para>
        /// If the per-frame budget is exceeded mid-tier, <paramref name="index"/> is advanced
        /// past the last-ticked runner so the next call resumes where this one stopped.
        /// If the full tier is completed, <paramref name="index"/> is reset to 0.
        /// </para>
        /// </summary>
        private void TickTier(List<BehaviourTreeRunner> runners, ref int index)
        {
            int count = runners.Count;
            if (count == 0) return;

            // Clamp saved index after list mutations (removal / rebalancing).
            if (index >= count) index = 0;

            int startIndex = index;

            for (int i = 0; i < count; i++)
            {
                int current = (startIndex + i) % count;
                var runner  = runners[current];

                if (runner == null) continue;

                runner.Tick();

                if (_stopwatch.Elapsed.TotalMilliseconds >= MaxMsPerFrame)
                {
                    // Persist position so the next frame starts after this runner.
                    index = (current + 1) % count;
                    return;
                }
            }

            // Full tier completed — reset so next frame starts from the beginning.
            index = 0;
        }
    }
}
