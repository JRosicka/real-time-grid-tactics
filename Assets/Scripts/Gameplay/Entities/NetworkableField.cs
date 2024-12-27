using System.Diagnostics.CodeAnalysis;
using Mirror;

namespace Gameplay.Entities {
    /// <summary>
    /// Wrapper class for a field that is able to be networked, but does not necessarily need to be. Updated through
    /// <see cref="ICommandManager"/> which handles both SP and MP. 
    ///
    /// The <see cref="INetworkableFieldValue"/> object passed into the constructor must be networkable, and the class
    /// that contains this field must itself be networkable, i.e. a NetworkBehaviour or serializable through the network
    /// layer.
    ///
    /// This was originally a generic class (NetworkableField<T>), but this did not work well with Mirror, and referencing
    /// the DoUpdateValue method using the dynamic keyword did not work in the build. TODO it would be good to look into
    /// some sort of client-side wrapper for conveniently casting to the INetworkableFieldValue implementation.
    /// 
    /// Similar to SyncVar, except it fixes two issues with SyncVar:
    /// - Allows for tracking state correctly in both SP and MP instead of just MP
    /// - Allows for any client to update the value, not just the host
    /// </summary>
    public class NetworkableField {
        private readonly NetworkBehaviour _parent;
        private readonly string _fieldName;
        public INetworkableFieldValue Value { get; private set; }

        public delegate void ValueChangedDelegate(INetworkableFieldValue oldValue, INetworkableFieldValue newValue, string metadata);
        
        /// <summary>
        /// Triggered on both the server and client
        /// </summary>
        public ValueChangedDelegate ValueChanged;

        public NetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue startingValue) {
            _parent = parent;
            _fieldName = fieldName;
            Value = startingValue;
        }

        /// <summary>
        /// Update the value and pass to the server/clients for MP. Note that if this is a MP client (not the host), then
        /// the value will not update locally until we receive the update from the host. 
        /// </summary>
        public void UpdateValue(INetworkableFieldValue newValue, string metadata = null) {
            GameManager.Instance.CommandManager.UpdateNetworkableField(_parent, _fieldName, newValue, metadata);
        }

        /// <summary>
        /// Only call this via <see cref="ICommandManager"/>. Called on the client.
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]   // Called via reflection
        public void DoUpdateValue(INetworkableFieldValue newValue, string metaData) {
            INetworkableFieldValue oldValue = Value;
            Value = newValue;
            ValueChanged?.Invoke(oldValue, newValue, metaData);
        }
    }
}