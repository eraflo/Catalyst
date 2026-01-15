using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Attachment
{
    /// <summary>
    /// Cached Rigidbody state for restoration on detach.
    /// </summary>
    internal struct RigidbodyState
    {
        public bool WasKinematic;
        public bool UseGravity;
        public RigidbodyInterpolation Interpolation;
    }
    
    /// <summary>
    /// Service for network-synchronized object parenting.
    /// Handles attach/detach with proper Rigidbody state management.
    /// </summary>
    [Service(Priority = 9)]
    public class NetworkAttachmentManager : IGameService, INetworkMessageHandler
    {
        private NetworkManager _networkManager;
        private NetworkIdManager _idManager;
        
        // Cache original Rigidbody states for restoration
        private readonly Dictionary<uint, RigidbodyState> _originalStates = new();
        
        // Track current attachments
        private readonly Dictionary<uint, uint> _attachments = new(); // childId -> parentId
        
        // Pending attach/detach operations (for batching)
        private readonly List<AttachRequestMessage> _pendingAttaches = new();
        private readonly List<DetachRequestMessage> _pendingDetaches = new();
        private bool _hasPendingOperations;
        
        #region Events
        
        /// <summary>Fired when an object is attached.</summary>
        public event Action<uint, uint> OnAttached; // childId, parentId
        
        /// <summary>Fired when an object is detached.</summary>
        public event Action<uint> OnDetached; // childId
        
        #endregion
        
        #region IGameService
        
        public void Initialize()
        {
            _networkManager = App.Get<NetworkManager>();
            _idManager = App.Get<NetworkIdManager>();
        }
        
        public void Shutdown()
        {
            _originalStates.Clear();
            _attachments.Clear();
            _pendingAttaches.Clear();
            _pendingDetaches.Clear();
        }
        
        #endregion
        
        #region INetworkMessageHandler
        
        public void OnRegistered()
        {
            _networkManager.On<AttachRequestMessage>(HandleAttachRequest);
            _networkManager.On<AttachConfirmMessage>(HandleAttachConfirm);
            _networkManager.On<DetachRequestMessage>(HandleDetachRequest);
            _networkManager.On<DetachConfirmMessage>(HandleDetachConfirm);
        }
        
        public void OnUnregistered()
        {
            _networkManager.Off<AttachRequestMessage>(HandleAttachRequest);
            _networkManager.Off<AttachConfirmMessage>(HandleAttachConfirm);
            _networkManager.Off<DetachRequestMessage>(HandleDetachRequest);
            _networkManager.Off<DetachConfirmMessage>(HandleDetachConfirm);
        }
        
        public void OnNetworkConnected() { }
        public void OnNetworkDisconnected()
        {
            _originalStates.Clear();
            _attachments.Clear();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Requests to attach a child object to a parent.
        /// </summary>
        /// <param name="childId">Network ID of the child object.</param>
        /// <param name="parentId">Network ID of the parent object.</param>
        /// <param name="localPosition">Optional local position offset.</param>
        /// <param name="localRotation">Optional local rotation.</param>
        /// <param name="authorityMode">Override authority mode, or null to use PackageSettings default.</param>
        public void RequestAttach(
            uint childId, 
            uint parentId, 
            Vector3? localPosition = null, 
            Quaternion? localRotation = null,
            AuthorityMode? authorityMode = null)
        {
            var child = _idManager?.GetObject<GameObject>(childId);
            var parent = _idManager?.GetObject<GameObject>(parentId);
            
            if (child == null || parent == null)
            {
                Debug.LogWarning($"[NetworkAttachment] Invalid IDs: child={childId}, parent={parentId}");
                return;
            }
            
            var request = new AttachRequestMessage
            {
                ChildId = childId,
                ParentId = parentId,
                LocalPosition = localPosition ?? Vector3.zero,
                LocalRotation = localRotation ?? Quaternion.identity
            };
            
            // Use provided authority mode or fall back to PackageSettings default
            var effectiveAuthority = authorityMode ?? PackageSettings.Instance.DefaultAuthorityMode;
            
            if (effectiveAuthority == AuthorityMode.ServerAuthoritative && _networkManager.IsServer)
            {
                // Server: execute immediately and broadcast
                ExecuteAttach(request);
                BroadcastAttachConfirm(request, child);
            }
            else if (effectiveAuthority == AuthorityMode.ServerAuthoritative)
            {
                // Client in ServerAuthoritative: send request to server
                _networkManager.SendToServer(request);
            }
            else
            {
                // OwnerAuthoritative: execute locally and broadcast
                ExecuteAttach(request);
                BroadcastAttachConfirm(request, child);
            }
        }
        
        /// <summary>
        /// Requests to detach a child object.
        /// </summary>
        /// <param name="childId">Network ID of the child object.</param>
        /// <param name="inheritVelocity">Whether to inherit parent's velocity.</param>
        /// <param name="authorityMode">Override authority mode, or null to use PackageSettings default.</param>
        public void RequestDetach(uint childId, bool inheritVelocity = false, AuthorityMode? authorityMode = null)
        {
            var request = new DetachRequestMessage
            {
                ChildId = childId,
                InheritVelocity = inheritVelocity
            };
            
            var effectiveAuthority = authorityMode ?? PackageSettings.Instance.DefaultAuthorityMode;
            
            if (effectiveAuthority == AuthorityMode.ServerAuthoritative && _networkManager.IsServer)
            {
                // Server: execute immediately and broadcast
                var confirm = ExecuteDetach(request);
                _networkManager.Send(confirm, NetworkTarget.Clients);
            }
            else if (effectiveAuthority == AuthorityMode.ServerAuthoritative)
            {
                // Client in ServerAuthoritative: send request to server
                _networkManager.SendToServer(request);
            }
            else
            {
                // OwnerAuthoritative: execute locally and broadcast
                var confirm = ExecuteDetach(request);
                _networkManager.Send(confirm, NetworkTarget.Clients);
            }
        }
        
        /// <summary>
        /// Checks if an object is currently attached.
        /// </summary>
        public bool IsAttached(uint childId)
        {
            return _attachments.ContainsKey(childId);
        }
        
        /// <summary>
        /// Gets the parent ID of an attached object.
        /// </summary>
        public bool TryGetParent(uint childId, out uint parentId)
        {
            return _attachments.TryGetValue(childId, out parentId);
        }
        
        #endregion
        
        #region Message Handlers
        
        private void HandleAttachRequest(AttachRequestMessage request)
        {
            if (!_networkManager.IsServer)
                return;
            
            var child = _idManager?.GetObject<GameObject>(request.ChildId);
            if (child == null)
                return;
            
            ExecuteAttach(request);
            BroadcastAttachConfirm(request, child);
        }
        
        private void HandleAttachConfirm(AttachConfirmMessage confirm)
        {
            if (_networkManager.IsServer)
                return; // Server already applied
            
            var child = _idManager?.GetObject<GameObject>(confirm.ChildId);
            var parent = _idManager?.GetObject<GameObject>(confirm.ParentId);
            
            if (child == null || parent == null)
                return;
            
            // Store original state
            _originalStates[confirm.ChildId] = new RigidbodyState
            {
                WasKinematic = confirm.WasKinematic,
                UseGravity = confirm.WasUsingGravity
            };
            
            // Apply attachment
            ApplyAttach(child, parent.transform, confirm.LocalPosition, confirm.LocalRotation);
            _attachments[confirm.ChildId] = confirm.ParentId;
            
            OnAttached?.Invoke(confirm.ChildId, confirm.ParentId);
        }
        
        private void HandleDetachRequest(DetachRequestMessage request)
        {
            if (!_networkManager.IsServer)
                return;
            
            var confirm = ExecuteDetach(request);
            _networkManager.Send(confirm, NetworkTarget.Clients);
        }
        
        private void HandleDetachConfirm(DetachConfirmMessage confirm)
        {
            if (_networkManager.IsServer)
                return; // Server already applied
            
            var child = _idManager?.GetObject<GameObject>(confirm.ChildId);
            if (child == null)
                return;
            
            ApplyDetach(child, confirm);
            _attachments.Remove(confirm.ChildId);
            _originalStates.Remove(confirm.ChildId);
            
            OnDetached?.Invoke(confirm.ChildId);
        }
        
        #endregion
        
        #region Execution
        
        private void ExecuteAttach(AttachRequestMessage request)
        {
            var child = _idManager?.GetObject<GameObject>(request.ChildId);
            var parent = _idManager?.GetObject<GameObject>(request.ParentId);
            
            if (child == null || parent == null)
                return;
            
            // Cache Rigidbody state before modifying
            var rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                _originalStates[request.ChildId] = new RigidbodyState
                {
                    WasKinematic = rb.isKinematic,
                    UseGravity = rb.useGravity,
                    Interpolation = rb.interpolation
                };
            }
            
            ApplyAttach(child, parent.transform, request.LocalPosition, request.LocalRotation);
            _attachments[request.ChildId] = request.ParentId;
            
            OnAttached?.Invoke(request.ChildId, request.ParentId);
            
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkAttachment] Attached {request.ChildId} to {request.ParentId}");
            }
        }
        
        private void ApplyAttach(GameObject child, Transform parent, Vector3 localPos, Quaternion localRot)
        {
            // Freeze Rigidbody
            var rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Reparent
            child.transform.SetParent(parent);
            child.transform.localPosition = localPos;
            child.transform.localRotation = localRot;
        }
        
        private DetachConfirmMessage ExecuteDetach(DetachRequestMessage request)
        {
            var child = _idManager?.GetObject<GameObject>(request.ChildId);
            
            var confirm = new DetachConfirmMessage
            {
                ChildId = request.ChildId
            };
            
            if (child == null)
                return confirm;
            
            // Capture velocity from parent before detaching
            Vector3 inheritedVelocity = Vector3.zero;
            Vector3 inheritedAngularVelocity = Vector3.zero;
            
            if (request.InheritVelocity && child.transform.parent != null)
            {
                var parentRb = child.transform.parent.GetComponent<Rigidbody>();
                if (parentRb != null)
                {
                    inheritedVelocity = parentRb.velocity;
                    inheritedAngularVelocity = parentRb.angularVelocity;
                }
            }
            
            // Get original state
            bool restoreKinematic = false;
            bool restoreGravity = true;
            
            if (_originalStates.TryGetValue(request.ChildId, out var state))
            {
                restoreKinematic = state.WasKinematic;
                restoreGravity = state.UseGravity;
            }
            
            // Unparent
            child.transform.SetParent(null);
            
            // Build confirm message
            confirm.WorldPosition = child.transform.position;
            confirm.WorldRotation = child.transform.rotation;
            confirm.InheritedVelocity = inheritedVelocity;
            confirm.InheritedAngularVelocity = inheritedAngularVelocity;
            confirm.RestoreKinematic = restoreKinematic;
            confirm.RestoreGravity = restoreGravity;
            
            // Restore Rigidbody
            var rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = restoreKinematic;
                rb.useGravity = restoreGravity;
                
                if (!restoreKinematic && request.InheritVelocity)
                {
                    rb.velocity = inheritedVelocity;
                    rb.angularVelocity = inheritedAngularVelocity;
                }
            }
            
            _attachments.Remove(request.ChildId);
            _originalStates.Remove(request.ChildId);
            
            OnDetached?.Invoke(request.ChildId);
            
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkAttachment] Detached {request.ChildId}");
            }
            
            return confirm;
        }
        
        private void ApplyDetach(GameObject child, DetachConfirmMessage confirm)
        {
            child.transform.SetParent(null);
            child.transform.position = confirm.WorldPosition;
            child.transform.rotation = confirm.WorldRotation;
            
            var rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = confirm.RestoreKinematic;
                rb.useGravity = confirm.RestoreGravity;
                
                if (!confirm.RestoreKinematic)
                {
                    rb.velocity = confirm.InheritedVelocity;
                    rb.angularVelocity = confirm.InheritedAngularVelocity;
                }
            }
        }
        
        private void BroadcastAttachConfirm(AttachRequestMessage request, GameObject child)
        {
            var rb = child.GetComponent<Rigidbody>();
            bool wasKinematic = false;
            bool wasUsingGravity = true;
            
            if (_originalStates.TryGetValue(request.ChildId, out var state))
            {
                wasKinematic = state.WasKinematic;
                wasUsingGravity = state.UseGravity;
            }
            
            var confirm = new AttachConfirmMessage
            {
                ChildId = request.ChildId,
                ParentId = request.ParentId,
                LocalPosition = request.LocalPosition,
                LocalRotation = request.LocalRotation,
                WasKinematic = wasKinematic,
                WasUsingGravity = wasUsingGravity
            };
            
            _networkManager.Send(confirm, NetworkTarget.Clients);
        }
        
        #endregion
    }
}
