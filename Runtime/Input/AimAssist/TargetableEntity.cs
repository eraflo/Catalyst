using UnityEngine;

namespace Eraflo.Catalyst.Input.AimAssist
{
    /// <summary>
    /// Component that marks an entity as a target for the Aim Assist system.
    /// </summary>
    [AddComponentMenu("Catalyst/Input/Targetable Entity")]
    public class TargetableEntity : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Collider _centerCollider;
        [SerializeField] private int _teamID;

        public Collider CenterCollider => _centerCollider;
        public int TeamID => _teamID;

        /// <summary>
        /// Gets the current evaluation position (usually the center of the collider).
        /// </summary>
        public Vector3 Position => _centerCollider ? _centerCollider.bounds.center : transform.position;

        private void OnEnable()
        {
            App.Get<IAimAssistService>()?.Register(this);
        }

        private void OnDisable()
        {
            App.Get<IAimAssistService>()?.Unregister(this);
        }
    }
}
