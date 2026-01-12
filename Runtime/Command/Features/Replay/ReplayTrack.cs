using System;
using System.Collections.Generic;

namespace Eraflo.Catalyst.Command
{
    /// <summary>
    /// Represents a single command executed at a specific point in time.
    /// </summary>
    [Serializable]
    public class ReplayFrame
    {
        public float Timestamp;
        public string CommandType;
        public byte[] CommandData;
    }

    /// <summary>
    /// A collection of recorded frames representing a full replay or ghost track.
    /// </summary>
    [Serializable]
    public class ReplayTrack
    {
        public string Name;
        public List<ReplayFrame> Frames = new List<ReplayFrame>();
    }
}
