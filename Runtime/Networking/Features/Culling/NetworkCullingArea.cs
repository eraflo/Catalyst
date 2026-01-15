using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Culling
{
    /// <summary>
    /// Defines a culling area for interest management.
    /// Objects within this radius of a player will be visible to them.
    /// </summary>
    [AddComponentMenu("Catalyst/Networking/Network Culling Area")]
    public class NetworkCullingArea : MonoBehaviour
    {
        [Header("Culling Settings")]
        [Tooltip("Radius within which objects are visible to this player.")]
        [SerializeField] private float _radius = 50f;
        
        [Tooltip("Hysteresis buffer to prevent flickering at boundaries.")]
        [SerializeField] private float _hysteresis = 5f;
        
        [Tooltip("Layers to include in culling checks.")]
        [SerializeField] private LayerMask _cullingLayers = -1; // All layers
        
        [Tooltip("If true, only cull objects with ICullable component.")]
        [SerializeField] private bool _requireCullableComponent = false;
        
        /// <summary>Visibility radius.</summary>
        public float Radius { get => _radius; set => _radius = value; }
        
        /// <summary>Outer radius including hysteresis (for hiding).</summary>
        public float OuterRadius => _radius + _hysteresis;
        
        /// <summary>Hysteresis buffer distance.</summary>
        public float Hysteresis => _hysteresis;
        
        /// <summary>Layer mask for cullable objects.</summary>
        public LayerMask CullingLayers => _cullingLayers;
        
        /// <summary>Whether to require ICullable component.</summary>
        public bool RequireCullableComponent => _requireCullableComponent;
        
        /// <summary>Current position of this culling area.</summary>
        public Vector3 Position => transform.position;
        
        /// <summary>
        /// Checks if a world position is within the visibility radius.
        /// </summary>
        public bool IsInRange(Vector3 position)
        {
            return (position - transform.position).sqrMagnitude <= _radius * _radius;
        }
        
        /// <summary>
        /// Checks if a world position is within the outer radius (including hysteresis).
        /// </summary>
        public bool IsInOuterRange(Vector3 position)
        {
            float outerRadius = _radius + _hysteresis;
            return (position - transform.position).sqrMagnitude <= outerRadius * outerRadius;
        }
        
        /// <summary>
        /// Gets the squared distance to a position.
        /// </summary>
        public float GetSqrDistance(Vector3 position)
        {
            return (position - transform.position).sqrMagnitude;
        }
        
#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [SerializeField] private Color _innerColor = new Color(0f, 1f, 0f, 0.2f);
        [SerializeField] private Color _outerColor = new Color(1f, 1f, 0f, 0.1f);
        
        private void OnDrawGizmosSelected()
        {
            // Draw inner radius (visibility)
            Gizmos.color = _innerColor;
            Gizmos.DrawWireSphere(transform.position, _radius);
            
            // Draw outer radius (hysteresis)
            Gizmos.color = _outerColor;
            Gizmos.DrawWireSphere(transform.position, _radius + _hysteresis);
            
            // Label
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * (_radius + 2f), 
                $"Culling: {_radius}m (+{_hysteresis}m)");
        }
#endif
    }
}
