using UnityEngine;
using Eraflo.Catalyst.Core.Blackboard;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Service that finds the closest object with a specific tag.
    /// </summary>
    [BehaviourTreeNode("Services", "Find Closest By Tag")]
    public class FindClosestByTagService : ServiceNode
    {
        public string Tag = "Enemy";

        [BlackboardKey]
        public string ResultKey = "ClosestEnemy";

        public float MaxRange = 100f;

        private GameObject[] _cachedObjects;
        private float _cacheTime = -999f;
        private const float CacheInterval = 1f;

        protected override void OnServiceUpdate()
        {
            if (Owner == null || Blackboard == null) return;

            // Refresh the tag search only once per CacheInterval seconds
            if (Time.time - _cacheTime > CacheInterval)
            {
                _cachedObjects = GameObject.FindGameObjectsWithTag(Tag);
                _cacheTime = Time.time;
            }

            var objects = _cachedObjects;
            GameObject closest = null;
            float closestDist = MaxRange;

            foreach (var obj in objects)
            {
                float dist = Vector3.Distance(Owner.transform.position, obj.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = obj;
                }
            }

            if (closest != null)
            {
                Blackboard.Set(ResultKey, closest);
                DebugMessage = $"Found: {closest.name} ({closestDist:F1}m)";
            }
            else
            {
                Blackboard.Set<GameObject>(ResultKey, null);
                DebugMessage = $"No {Tag} in range";
            }
        }
    }
}
