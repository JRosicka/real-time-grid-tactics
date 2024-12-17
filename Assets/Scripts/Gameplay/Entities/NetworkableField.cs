using System.Diagnostics.CodeAnalysis;
using Mirror;

namespace Gameplay.Entities {
    /// <summary>
    /// Represents a field that is able to be networked, but does not necessarily need to be. Updated through
    /// <see cref="ICommandManager"/> which handles both SP and MP. 
    ///
    /// <see cref="T"/> must be networkable, and the class that contains this field must itself be networkable,
    /// i.e. a NetworkBehaviour or serializable through the network layer.
    ///
    /// Similar to SyncVar, except it fixes two issues with SyncVar:
    /// - Allows for tracking state correctly in both SP and MP instead of just MP
    /// - Allows for any client to update the value, not just the host
    /// </summary>
    public class NetworkableField<T> {
        private readonly NetworkBehaviour _parent;
        private readonly string _fieldName;
        public T Value { get; private set; }

        public delegate void ValueChangedDelegate(T oldValue, T newValue);
        public ValueChangedDelegate ValueChanged;

        public NetworkableField(NetworkBehaviour parent, string fieldName) {
            _parent = parent;
            _fieldName = fieldName;
        }

        /// <summary>
        /// Update the value and pass to the server/clients for MP. Note that if this is a MP client (not the host), then
        /// the value will not update locally until we receive the update from the host. 
        /// </summary>
        public void UpdateValue(T newValue) {
            GameManager.Instance.CommandManager.UpdateNetworkableField(_parent, _fieldName, newValue);
        }

        /// <summary>
        /// Only call this via <see cref="ICommandManager"/>. Called on the client.
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]   // Called via reflection
        public void DoUpdateValue(object newValueObject) {
            T newValue = (T)newValueObject;
            T oldValue = Value;
            Value = newValue;
            ValueChanged?.Invoke(oldValue, newValue);
        }
    }
}