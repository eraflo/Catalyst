using System.Collections.Generic;

namespace Eraflo.Catalyst.Networking.Features.Spawn
{
    /// <summary>
    /// Interface for spawn point selection strategies.
    /// </summary>
    public interface ISpawnStrategy
    {
        /// <summary>
        /// Selects a spawn point from the available points.
        /// </summary>
        /// <param name="points">Available spawn points.</param>
        /// <param name="clientId">Client ID requesting spawn.</param>
        /// <param name="teamId">Optional team ID to filter by. -1 = any team.</param>
        /// <param name="spawnTag">Optional tag to filter by. Empty = any tag.</param>
        /// <returns>Selected spawn point, or null if none available.</returns>
        NetworkSpawnPoint SelectSpawnPoint(
            IReadOnlyList<NetworkSpawnPoint> points,
            ulong clientId,
            int teamId = -1,
            string spawnTag = "");
    }
}
