using UnityEngine;

namespace Eraflo.Catalyst.Input.AimAssist
{
    /// <summary>
    /// Service responsible for handling aim assist logic (Friction and Magnetism).
    /// </summary>
    public interface IAimAssistService : IGameService
    {
        /// <summary>
        /// Registers a targetable entity into the system.
        /// </summary>
        void Register(TargetableEntity entity);

        /// <summary>
        /// Unregisters a targetable entity from the system.
        /// </summary>
        void Unregister(TargetableEntity entity);

        /// <summary>
        /// Modifies the raw input to apply aim assist effects.
        /// </summary>
        /// <param name="rawInput">The raw 2D axis input from the player.</param>
        /// <param name="sourcePosition">World position of the aim origin.</param>
        /// <param name="forward">Current aim forward vector.</param>
        /// <param name="cam">The camera used for viewport calculations.</param>
        /// <param name="deltaTime">Time since last frame.</param>
        /// <param name="sourceTeamID">Team ID to ignore (e.g. your own team).</param>
        /// <returns>The assisted input vector.</returns>
        Vector2 ApplyAssist(Vector2 rawInput, Vector3 sourcePosition, Vector3 forward, Camera cam, float deltaTime, int sourceTeamID = -1);
    }
}
