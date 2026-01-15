using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Attachment
{
    /// <summary>
    /// Extension methods for easy network parenting.
    /// </summary>
    public static class NetworkAttachmentExtensions
    {
        /// <summary>
        /// Attaches this object to a parent over the network.
        /// </summary>
        /// <param name="child">The object to attach.</param>
        /// <param name="parent">The parent to attach to.</param>
        /// <param name="localPosition">Optional local position offset.</param>
        /// <param name="localRotation">Optional local rotation.</param>
        public static void NetworkParentTo(this GameObject child, Transform parent, Vector3? localPosition = null, Quaternion? localRotation = null)
        {
            if (child == null || parent == null)
            {
                Debug.LogWarning("[NetworkAttachment] Cannot attach: null child or parent.");
                return;
            }
            
            var manager = App.Get<NetworkAttachmentManager>();
            if (manager == null)
            {
                Debug.LogWarning("[NetworkAttachment] NetworkAttachmentManager not available.");
                return;
            }
            
            uint childId = child.GetNetworkId();
            uint parentId = parent.gameObject.GetNetworkId();
            
            if (childId == 0 || parentId == 0)
            {
                Debug.LogWarning("[NetworkAttachment] Objects must have network IDs.");
                return;
            }
            
            manager.RequestAttach(childId, parentId, localPosition, localRotation);
        }
        
        /// <summary>
        /// Attaches this object to a parent over the network.
        /// </summary>
        public static void NetworkParentTo(this GameObject child, GameObject parent, Vector3? localPosition = null, Quaternion? localRotation = null)
        {
            if (parent == null)
            {
                Debug.LogWarning("[NetworkAttachment] Cannot attach: null parent.");
                return;
            }
            
            child.NetworkParentTo(parent.transform, localPosition, localRotation);
        }
        
        /// <summary>
        /// Attaches this component's object to a parent over the network.
        /// </summary>
        public static void NetworkParentTo(this Component child, Transform parent, Vector3? localPosition = null, Quaternion? localRotation = null)
        {
            child.gameObject.NetworkParentTo(parent, localPosition, localRotation);
        }
        
        /// <summary>
        /// Detaches this object from its parent over the network.
        /// </summary>
        /// <param name="child">The object to detach.</param>
        /// <param name="inheritVelocity">Whether to inherit the parent's velocity (for throwing).</param>
        public static void NetworkUnparent(this GameObject child, bool inheritVelocity = false)
        {
            if (child == null)
            {
                Debug.LogWarning("[NetworkAttachment] Cannot detach: null child.");
                return;
            }
            
            var manager = App.Get<NetworkAttachmentManager>();
            if (manager == null)
            {
                Debug.LogWarning("[NetworkAttachment] NetworkAttachmentManager not available.");
                return;
            }
            
            uint childId = child.GetNetworkId();
            
            if (childId == 0)
            {
                Debug.LogWarning("[NetworkAttachment] Object must have a network ID.");
                return;
            }
            
            manager.RequestDetach(childId, inheritVelocity);
        }
        
        /// <summary>
        /// Detaches this component's object from its parent over the network.
        /// </summary>
        public static void NetworkUnparent(this Component child, bool inheritVelocity = false)
        {
            child.gameObject.NetworkUnparent(inheritVelocity);
        }
        
        /// <summary>
        /// Checks if this object is currently network-attached to a parent.
        /// </summary>
        public static bool IsNetworkAttached(this GameObject obj)
        {
            var manager = App.Get<NetworkAttachmentManager>();
            if (manager == null)
                return false;
            
            uint id = obj.GetNetworkId();
            return id != 0 && manager.IsAttached(id);
        }
    }
}
