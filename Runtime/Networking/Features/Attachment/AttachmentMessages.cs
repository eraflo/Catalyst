using System.IO;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Attachment
{
    /// <summary>
    /// Request to attach a network object to a parent.
    /// </summary>
    public struct AttachRequestMessage : INetworkMessage
    {
        public uint ChildId;
        public uint ParentId;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ChildId);
            writer.Write(ParentId);
            writer.Write(LocalPosition.x);
            writer.Write(LocalPosition.y);
            writer.Write(LocalPosition.z);
            writer.Write(LocalRotation.x);
            writer.Write(LocalRotation.y);
            writer.Write(LocalRotation.z);
            writer.Write(LocalRotation.w);
        }
        
        public void Deserialize(BinaryReader reader)
        {
            ChildId = reader.ReadUInt32();
            ParentId = reader.ReadUInt32();
            LocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            LocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
    
    /// <summary>
    /// Confirmation of attachment from server.
    /// </summary>
    public struct AttachConfirmMessage : INetworkMessage
    {
        public uint ChildId;
        public uint ParentId;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public bool WasKinematic;
        public bool WasUsingGravity;
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ChildId);
            writer.Write(ParentId);
            writer.Write(LocalPosition.x);
            writer.Write(LocalPosition.y);
            writer.Write(LocalPosition.z);
            writer.Write(LocalRotation.x);
            writer.Write(LocalRotation.y);
            writer.Write(LocalRotation.z);
            writer.Write(LocalRotation.w);
            writer.Write(WasKinematic);
            writer.Write(WasUsingGravity);
        }
        
        public void Deserialize(BinaryReader reader)
        {
            ChildId = reader.ReadUInt32();
            ParentId = reader.ReadUInt32();
            LocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            LocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            WasKinematic = reader.ReadBoolean();
            WasUsingGravity = reader.ReadBoolean();
        }
    }
    
    /// <summary>
    /// Request to detach a network object.
    /// </summary>
    public struct DetachRequestMessage : INetworkMessage
    {
        public uint ChildId;
        public bool InheritVelocity;
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ChildId);
            writer.Write(InheritVelocity);
        }
        
        public void Deserialize(BinaryReader reader)
        {
            ChildId = reader.ReadUInt32();
            InheritVelocity = reader.ReadBoolean();
        }
    }
    
    /// <summary>
    /// Confirmation of detachment from server.
    /// </summary>
    public struct DetachConfirmMessage : INetworkMessage
    {
        public uint ChildId;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public Vector3 InheritedVelocity;
        public Vector3 InheritedAngularVelocity;
        public bool RestoreKinematic;
        public bool RestoreGravity;
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ChildId);
            writer.Write(WorldPosition.x);
            writer.Write(WorldPosition.y);
            writer.Write(WorldPosition.z);
            writer.Write(WorldRotation.x);
            writer.Write(WorldRotation.y);
            writer.Write(WorldRotation.z);
            writer.Write(WorldRotation.w);
            writer.Write(InheritedVelocity.x);
            writer.Write(InheritedVelocity.y);
            writer.Write(InheritedVelocity.z);
            writer.Write(InheritedAngularVelocity.x);
            writer.Write(InheritedAngularVelocity.y);
            writer.Write(InheritedAngularVelocity.z);
            writer.Write(RestoreKinematic);
            writer.Write(RestoreGravity);
        }
        
        public void Deserialize(BinaryReader reader)
        {
            ChildId = reader.ReadUInt32();
            WorldPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            WorldRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            InheritedVelocity = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            InheritedAngularVelocity = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            RestoreKinematic = reader.ReadBoolean();
            RestoreGravity = reader.ReadBoolean();
        }
    }
}
