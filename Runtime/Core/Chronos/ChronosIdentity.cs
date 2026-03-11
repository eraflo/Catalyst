using UnityEngine;

namespace Eraflo.Catalyst.Core.Chronos
{
    [AddComponentMenu("Catalyst/Chronos/Chronos Identity")]
    public class ChronosIdentity : MonoBehaviour
    {
        [SerializeField] private string _channel = "World";
        
        [Inject] private ChronosManager _chronos;

        public string Channel
        {
            get => _channel;
            set => _channel = value;
        }

        private void Start()
        {
            if (_chronos == null)
            {
                Debug.LogWarning($"[ChronosIdentity] ChronosManager not found on {gameObject.name}. Make sure it is registered as a service.");
            }
        }

        /// <summary>
        /// Returns the delta time specific to this object's time channel.
        /// Formula: Time.deltaTime * ChannelScale
        /// </summary>
        public float DeltaTime
        {
            get
            {
                if (_chronos == null) return Time.deltaTime;
                return Time.deltaTime * _chronos.GetChannelScale(_channel);
            }
        }

        /// <summary>
        /// Returns the fixed delta time specific to this object's time channel.
        /// </summary>
        public float FixedDeltaTime
        {
            get
            {
                if (_chronos == null) return Time.fixedDeltaTime;
                return Time.fixedDeltaTime * _chronos.GetChannelScale(_channel);
            }
        }
    }
}
