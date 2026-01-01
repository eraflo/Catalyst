using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    [System.Serializable]
    public class StickyNote : ScriptableObject
    {
        public string Title = "Note";
        public string Content = "Write something...";
        public Rect Position = new Rect(0, 0, 200, 160);
        public Color Color = new Color(1f, 0.9f, 0.4f, 1f); // Classic yellow
    }
}
