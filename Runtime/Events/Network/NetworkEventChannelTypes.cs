using UnityEngine;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Network-aware event channel for int values.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNetworkIntChannel", menuName = "Events/Network/Int Channel", order = 101)]
    public class NetworkIntEventChannel : NetworkEventChannel<int> { }

    /// <summary>
    /// Network-aware event channel for float values.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNetworkFloatChannel", menuName = "Events/Network/Float Channel", order = 102)]
    public class NetworkFloatEventChannel : NetworkEventChannel<float> { }

    /// <summary>
    /// Network-aware event channel for string values.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNetworkStringChannel", menuName = "Events/Network/String Channel", order = 103)]
    public class NetworkStringEventChannel : NetworkEventChannel<string>
    {
        protected override byte[] SerializeValue(string value)
        {
            return System.Text.Encoding.UTF8.GetBytes(value ?? "");
        }

        public override string DeserializeValue(byte[] data)
        {
            return System.Text.Encoding.UTF8.GetString(data);
        }
    }

    /// <summary>
    /// Network-aware event channel for bool values.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNetworkBoolChannel", menuName = "Events/Network/Bool Channel", order = 104)]
    public class NetworkBoolEventChannel : NetworkEventChannel<bool>
    {
        protected override byte[] SerializeValue(bool value)
        {
            return new byte[] { (byte)(value ? 1 : 0) };
        }

        public override bool DeserializeValue(byte[] data)
        {
            return data != null && data.Length > 0 && data[0] != 0;
        }
    }

    /// <summary>
    /// Network-aware event channel for Vector3 values.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNetworkVector3Channel", menuName = "Events/Network/Vector3 Channel", order = 105)]
    public class NetworkVector3EventChannel : NetworkEventChannel<Vector3>
    {
        protected override byte[] SerializeValue(Vector3 value)
        {
            byte[] data = new byte[12];
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(value.x), 0, data, 0, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(value.y), 0, data, 4, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(value.z), 0, data, 8, 4);
            return data;
        }

        public override Vector3 DeserializeValue(byte[] data)
        {
            if (data == null || data.Length < 12) return Vector3.zero;
            return new Vector3(
                System.BitConverter.ToSingle(data, 0),
                System.BitConverter.ToSingle(data, 4),
                System.BitConverter.ToSingle(data, 8)
            );
        }
    }
}
