using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Spawn
{
    /// <summary>
    /// Defines a spawn point in the scene with team assignment and priority.
    /// Use multiple NetworkSpawnPoints to create a spawn system.
    /// </summary>
    [AddComponentMenu("Catalyst/Networking/Network Spawn Point")]
    public class NetworkSpawnPoint : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Team ID this spawn point belongs to. -1 = all teams.")]
        [SerializeField] private int _teamId = -1;
        
        [Tooltip("Priority of this spawn point. Higher = more likely to be selected.")]
        [SerializeField] private int _priority = 0;
        
        [Tooltip("Tag for filtering spawn points (e.g., 'Initial', 'Respawn', 'VIP').")]
        [SerializeField] private string _spawnTag = "";
        
        [Header("State")]
        [Tooltip("Whether this spawn point is currently occupied.")]
        [SerializeField] private bool _isOccupied = false;
        
        [Tooltip("Duration in seconds before an occupied point becomes available again.")]
        [SerializeField] private float _occupiedCooldown = 2f;
        
        private float _occupiedUntilTime;
        
        #region Properties
        
        /// <summary>Team ID this spawn point belongs to. -1 = all teams.</summary>
        public int TeamId => _teamId;
        
        /// <summary>Priority of this spawn point. Higher values = preferred.</summary>
        public int Priority => _priority;
        
        /// <summary>Tag for filtering spawn points.</summary>
        public string SpawnTag => _spawnTag;
        
        /// <summary>Whether this spawn point is currently occupied or on cooldown.</summary>
        public bool IsOccupied
        {
            get
            {
                if (_isOccupied && Time.time >= _occupiedUntilTime)
                {
                    _isOccupied = false;
                }
                return _isOccupied;
            }
            set
            {
                _isOccupied = value;
                if (value)
                {
                    _occupiedUntilTime = Time.time + _occupiedCooldown;
                }
            }
        }
        
        /// <summary>Gets the spawn position (transform position).</summary>
        public Vector3 Position => transform.position;
        
        /// <summary>Gets the spawn rotation (transform rotation).</summary>
        public Quaternion Rotation => transform.rotation;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Marks this spawn point as occupied for the cooldown duration.
        /// </summary>
        public void MarkOccupied()
        {
            IsOccupied = true;
        }
        
        /// <summary>
        /// Checks if this spawn point matches the given team.
        /// </summary>
        /// <param name="teamId">Team to check. -1 matches any team.</param>
        public bool MatchesTeam(int teamId)
        {
            if (_teamId == -1 || teamId == -1) return true;
            return _teamId == teamId;
        }
        
        /// <summary>
        /// Checks if this spawn point matches the given tag.
        /// </summary>
        /// <param name="tag">Tag to match. Empty string matches any tag.</param>
        public bool MatchesTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(_spawnTag))
                return true;
            return _spawnTag == tag;
        }
        
        #endregion
        
        #region Editor Gizmos
        
#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [SerializeField] private float _gizmoRadius = 0.5f;
        [SerializeField] private Color _availableColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color _occupiedColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private Color _teamColor = new Color(0f, 0.5f, 1f, 0.5f);
        
        private void OnDrawGizmos()
        {
            DrawSpawnPointGizmo(false);
        }
        
        private void OnDrawGizmosSelected()
        {
            DrawSpawnPointGizmo(true);
        }
        
        private void DrawSpawnPointGizmo(bool selected)
        {
            // Choose color based on state
            Color color;
            if (IsOccupied)
                color = _occupiedColor;
            else if (_teamId >= 0)
                color = Color.Lerp(_availableColor, _teamColor, 0.5f);
            else
                color = _availableColor;
            
            Gizmos.color = color;
            
            // Draw sphere
            float radius = selected ? _gizmoRadius * 1.2f : _gizmoRadius;
            Gizmos.DrawSphere(transform.position, radius);
            
            // Draw direction arrow
            Gizmos.color = Color.yellow;
            Vector3 forward = transform.forward * (radius * 2f);
            Gizmos.DrawLine(transform.position, transform.position + forward);
            
            // Draw arrowhead
            Vector3 right = transform.right * (radius * 0.5f);
            Gizmos.DrawLine(transform.position + forward, transform.position + forward * 0.7f + right);
            Gizmos.DrawLine(transform.position + forward, transform.position + forward * 0.7f - right);
            
            // Draw label
            if (selected)
            {
                UnityEditor.Handles.color = Color.white;
                string label = $"Spawn Point\nTeam: {(_teamId == -1 ? "Any" : _teamId.ToString())}\nPriority: {_priority}";
                if (!string.IsNullOrEmpty(_spawnTag))
                    label += $"\nTag: {_spawnTag}";
                UnityEditor.Handles.Label(transform.position + Vector3.up * (radius + 0.5f), label);
            }
        }
#endif
        
        #endregion
    }
}
