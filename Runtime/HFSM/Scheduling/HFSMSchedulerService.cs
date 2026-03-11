using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Eraflo.Catalyst.HFSM.Scheduling
{
    /// <summary>
    /// Service that drives <see cref="StateMachine"/> instances with distance-based LOD ticking
    /// and a per-frame millisecond budget, preventing AI state machines from overwhelming the CPU.
    ///
    /// <para><b>Usage — register your state machine in your own MonoBehaviour:</b></para>
    /// <code>
    /// private StateMachine _stateMachine;
    ///
    /// private void OnEnable()
    /// {
    ///     // Build and start _stateMachine here...
    ///     App.Get&lt;HFSMSchedulerService&gt;()?.Register(_stateMachine, transform);
    /// }
    ///
    /// private void OnDisable()
    /// {
    ///     App.Get&lt;HFSMSchedulerService&gt;()?.Unregister(_stateMachine);
    /// }
    /// </code>
    ///
    /// <para><b>LOD tiers (distance from Camera.main):</b></para>
    /// <list type="bullet">
    ///   <item>Tier 0 — distance &lt; <see cref="Tier0MaxDistance"/> (default 15 m): ticked every frame.</item>
    ///   <item>Tier 1 — distance &lt; <see cref="Tier1MaxDistance"/> (default 50 m): ticked every <see cref="Tier1Interval"/> frames (default 3).</item>
    ///   <item>Tier 2 — distance &gt;= <see cref="Tier1MaxDistance"/>: ticked every <see cref="Tier2Interval"/> frames (default 10).</item>
    ///   <item>Fallback — no camera or null owner: ticked every frame.</item>
    /// </list>
    ///
    /// <para>
    /// <b>Timing:</b> The scheduler calls <see cref="StateMachine.Update()"/> without passing a
    /// delta-time argument. The state machine resolves its own dt internally via
    /// <c>ChronosManager.GetDeltaTime</c>, so channel-specific time scaling is preserved even for
    /// throttled tiers.
    /// </para>
    /// </summary>
    [Service(Priority = 51)]
    public class HFSMSchedulerService : IGameService, IUpdatable
    {
        // ── LOD thresholds ─────────────────────────────────────────────────────────

        /// <summary>
        /// Distance threshold below which a state machine is ticked every frame (Tier 0).
        /// Default: 15 m.
        /// </summary>
        public float Tier0MaxDistance { get; set; } = 15f;

        /// <summary>
        /// Distance threshold below which a state machine is ticked every
        /// <see cref="Tier1Interval"/> frames (Tier 1). Default: 50 m.
        /// </summary>
        public float Tier1MaxDistance { get; set; } = 50f;

        /// <summary>Tick interval (in frames) for Tier 1 state machines. Default: 3.</summary>
        public int Tier1Interval { get; set; } = 3;

        /// <summary>Tick interval (in frames) for Tier 2 state machines. Default: 10.</summary>
        public int Tier2Interval { get; set; } = 10;

        // ── Frame budget ───────────────────────────────────────────────────────────

        /// <summary>
        /// Maximum milliseconds the scheduler may spend ticking state machines in a single frame.
        /// Once exceeded, remaining machines are deferred to the next eligible frame.
        /// Default: 2 ms.
        /// </summary>
        public float MaxMsPerFrame { get; set; } = 2f;

        // ── Internal state ─────────────────────────────────────────────────────────

        private struct Entry
        {
            public StateMachine StateMachine;
            public Transform Owner;
            /// <summary>
            /// Per-entry frame offset used for round-robin distribution so that all
            /// machines at the same tier do not tick on the same frame.
            /// </summary>
            public int FrameOffset;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private Camera _camera;
        private int _frameCount;

        // ── IGameService ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            _camera = Camera.main;
        }

        public void Shutdown()
        {
            _entries.Clear();
        }

        // ── Registration ───────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a <see cref="StateMachine"/> to be driven by the scheduler.
        /// </summary>
        /// <param name="stateMachine">The state machine to tick. Must not be null.</param>
        /// <param name="owner">
        /// The <see cref="Transform"/> used to measure distance for LOD tier selection.
        /// May be null — in that case the machine falls back to Tier 0 (every frame).
        /// </param>
        public void Register(StateMachine stateMachine, Transform owner)
        {
            if (stateMachine == null)
            {
                UnityEngine.Debug.LogWarning("[HFSMSchedulerService] Register: stateMachine is null. Skipping.");
                return;
            }

            // Prevent duplicate registrations.
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].StateMachine == stateMachine)
                    return;
            }

            _entries.Add(new Entry
            {
                StateMachine = stateMachine,
                Owner        = owner,
                // Spread registrations across frames so all newly registered machines
                // do not coincide on the same frame offset.
                FrameOffset  = _entries.Count
            });
        }

        /// <summary>
        /// Removes a <see cref="StateMachine"/> from the scheduler.
        /// Safe to call even if the machine was never registered.
        /// </summary>
        public void Unregister(StateMachine stateMachine)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].StateMachine == stateMachine)
                {
                    _entries.RemoveAt(i);
                    return;
                }
            }
        }

        // ── IUpdatable ─────────────────────────────────────────────────────────────

        public void OnUpdate()
        {
            _frameCount++;

            // Re-acquire Camera.main if it has been destroyed or not yet available.
            if (_camera == null)
                _camera = Camera.main;

            // Purge entries whose owner Transform has been destroyed by Unity.
            // Distinction:
            //   ReferenceEquals(owner, null) == true  → intentionally registered as null; keep (Tier-0 fallback).
            //   ReferenceEquals(owner, null) == false AND owner == null (Unity check) → Destroyed(); remove.
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var ownerRef = _entries[i].Owner;
                bool isCSharpNull = ReferenceEquals(ownerRef, null);
                bool isUnityNull  = (ownerRef == null); // Unity's overloaded == catches Destroyed objects

                if (!isCSharpNull && isUnityNull)
                    _entries.RemoveAt(i);
            }

            _stopwatch.Restart();

            for (int i = 0; i < _entries.Count; i++)
            {
                // Per-frame budget guard — stop processing once the budget is spent.
                if (_stopwatch.Elapsed.TotalMilliseconds >= MaxMsPerFrame)
                    break;

                var entry = _entries[i];

                // Determine tick interval based on distance to camera.
                int interval = GetTickInterval(entry.Owner);

                // Round-robin: each entry's FrameOffset shifts its phase within the interval
                // window so that machines in the same tier are spread across frames.
                if ((_frameCount + entry.FrameOffset) % interval != 0)
                    continue;

                // StateMachine.Update() resolves dt internally via ChronosManager so that
                // channel-specific time scaling is honoured, regardless of how many frames
                // were skipped due to LOD throttling.
                entry.StateMachine.Update();
            }

            _stopwatch.Stop();
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private int GetTickInterval(Transform owner)
        {
            // Fallback: no camera or no valid owner → tick every frame (Tier 0 behaviour).
            if (_camera == null || owner == null)
                return 1;

            // Use squared distances to avoid a square-root per entry per frame.
            float sqrDist = (owner.position - _camera.transform.position).sqrMagnitude;
            float tier0SqrMax = Tier0MaxDistance * Tier0MaxDistance;
            float tier1SqrMax = Tier1MaxDistance * Tier1MaxDistance;

            if (sqrDist < tier0SqrMax)  return 1;             // Tier 0 — every frame
            if (sqrDist < tier1SqrMax)  return Tier1Interval; // Tier 1 — every N frames
            return Tier2Interval;                              // Tier 2 — every M frames
        }
    }
}
