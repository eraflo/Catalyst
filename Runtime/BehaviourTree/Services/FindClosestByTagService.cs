using UnityEngine;

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

        protected override void OnServiceUpdate()
        {
            if (Owner == null || Blackboard == null) return;

            var objects = GameObject.FindGameObjectsWithTag(Tag);
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
