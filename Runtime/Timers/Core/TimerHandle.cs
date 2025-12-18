using System;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Opaque handle to a timer. Used to interact with timers without knowing
    /// the underlying implementation (Standard or Burst).
    /// </summary>
    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        /// <summary>Unique identifier for this timer.</summary>
        public readonly uint Id;
        
        /// <summary>Generation to detect stale handles.</summary>
        public readonly byte Generation;
        
        /// <summary>Type identifier for the timer type.</summary>
        public readonly ushort TypeId;

        internal TimerHandle(uint id, byte generation, ushort typeId)
        {
            Id = id;
            Generation = generation;
            TypeId = typeId;
        }

        /// <summary>Whether this handle is valid (non-zero ID).</summary>
        public bool IsValid => Id != 0;

        /// <summary>Invalid/null handle constant.</summary>
        public static readonly TimerHandle None = default;

        public bool Equals(TimerHandle other) => Id == other.Id && Generation == other.Generation;
        public override bool Equals(object obj) => obj is TimerHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Generation);
        public static bool operator ==(TimerHandle left, TimerHandle right) => left.Equals(right);
        public static bool operator !=(TimerHandle left, TimerHandle right) => !left.Equals(right);
        public override string ToString() => $"Timer({Id}:{Generation})";
    }
}
