using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Spawn
{
    /// <summary>
    /// Selects a random available spawn point.
    /// </summary>
    public class RandomSpawnStrategy : ISpawnStrategy
    {
        private readonly List<NetworkSpawnPoint> _filtered = new();
        
        public NetworkSpawnPoint SelectSpawnPoint(
            IReadOnlyList<NetworkSpawnPoint> points,
            ulong clientId,
            int teamId = -1,
            string spawnTag = "")
        {
            _filtered.Clear();
            
            foreach (var point in points)
            {
                if (!point.IsOccupied && 
                    point.MatchesTeam(teamId) && 
                    point.MatchesTag(spawnTag))
                {
                    _filtered.Add(point);
                }
            }
            
            if (_filtered.Count == 0)
                return null;
            
            int index = Random.Range(0, _filtered.Count);
            return _filtered[index];
        }
    }
    
    /// <summary>
    /// Cycles through spawn points in order (round-robin).
    /// </summary>
    public class RoundRobinSpawnStrategy : ISpawnStrategy
    {
        private int _currentIndex = 0;
        private readonly List<NetworkSpawnPoint> _filtered = new();
        
        public NetworkSpawnPoint SelectSpawnPoint(
            IReadOnlyList<NetworkSpawnPoint> points,
            ulong clientId,
            int teamId = -1,
            string spawnTag = "")
        {
            _filtered.Clear();
            
            foreach (var point in points)
            {
                if (!point.IsOccupied && 
                    point.MatchesTeam(teamId) && 
                    point.MatchesTag(spawnTag))
                {
                    _filtered.Add(point);
                }
            }
            
            if (_filtered.Count == 0)
                return null;
            
            // Wrap index
            _currentIndex = _currentIndex % _filtered.Count;
            var selected = _filtered[_currentIndex];
            _currentIndex++;
            
            return selected;
        }
        
        /// <summary>
        /// Resets the round-robin counter.
        /// </summary>
        public void Reset()
        {
            _currentIndex = 0;
        }
    }
    
    /// <summary>
    /// Selects spawn points based on team, preferring higher priority points.
    /// </summary>
    public class TeamBasedSpawnStrategy : ISpawnStrategy
    {
        private readonly List<NetworkSpawnPoint> _filtered = new();
        
        public NetworkSpawnPoint SelectSpawnPoint(
            IReadOnlyList<NetworkSpawnPoint> points,
            ulong clientId,
            int teamId = -1,
            string spawnTag = "")
        {
            _filtered.Clear();
            
            // First pass: find matching points
            foreach (var point in points)
            {
                if (!point.IsOccupied && 
                    point.MatchesTeam(teamId) && 
                    point.MatchesTag(spawnTag))
                {
                    _filtered.Add(point);
                }
            }
            
            if (_filtered.Count == 0)
                return null;
            
            // Sort by priority (descending)
            _filtered.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            // Get highest priority value
            int highestPriority = _filtered[0].Priority;
            
            // Find all points with highest priority
            int highPriorityCount = 0;
            foreach (var point in _filtered)
            {
                if (point.Priority == highestPriority)
                    highPriorityCount++;
                else
                    break;
            }
            
            // Random among highest priority
            int index = Random.Range(0, highPriorityCount);
            return _filtered[index];
        }
    }
    
    /// <summary>
    /// Selects the spawn point furthest from all enemies.
    /// </summary>
    public class FurthestFromEnemiesStrategy : ISpawnStrategy
    {
        private readonly List<NetworkSpawnPoint> _filtered = new();
        private readonly System.Func<ulong, IEnumerable<Vector3>> _getEnemyPositions;
        
        /// <summary>
        /// Creates a new furthest-from-enemies strategy.
        /// </summary>
        /// <param name="getEnemyPositions">Function that returns enemy positions for a given client.</param>
        public FurthestFromEnemiesStrategy(System.Func<ulong, IEnumerable<Vector3>> getEnemyPositions)
        {
            _getEnemyPositions = getEnemyPositions;
        }
        
        public NetworkSpawnPoint SelectSpawnPoint(
            IReadOnlyList<NetworkSpawnPoint> points,
            ulong clientId,
            int teamId = -1,
            string spawnTag = "")
        {
            _filtered.Clear();
            
            foreach (var point in points)
            {
                if (!point.IsOccupied && 
                    point.MatchesTeam(teamId) && 
                    point.MatchesTag(spawnTag))
                {
                    _filtered.Add(point);
                }
            }
            
            if (_filtered.Count == 0)
                return null;
            
            if (_filtered.Count == 1)
                return _filtered[0];
            
            // Get enemy positions
            var enemies = _getEnemyPositions?.Invoke(clientId);
            if (enemies == null)
                return _filtered[Random.Range(0, _filtered.Count)];
            
            // Calculate minimum distance to any enemy for each spawn point
            NetworkSpawnPoint best = null;
            float bestMinDistance = float.MinValue;
            
            foreach (var point in _filtered)
            {
                float minDistToEnemy = float.MaxValue;
                
                foreach (var enemyPos in enemies)
                {
                    float dist = Vector3.Distance(point.Position, enemyPos);
                    if (dist < minDistToEnemy)
                        minDistToEnemy = dist;
                }
                
                if (minDistToEnemy > bestMinDistance)
                {
                    bestMinDistance = minDistToEnemy;
                    best = point;
                }
            }
            
            return best ?? _filtered[0];
        }
    }
}
