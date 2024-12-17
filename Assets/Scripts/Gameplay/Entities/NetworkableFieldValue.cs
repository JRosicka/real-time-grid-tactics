using Mirror;

namespace Gameplay.Entities {
    /// <summary>
    /// Wrapper around the value tracked in a <see cref="NetworkableField{T}"/>. Necessary in order to provide a base
    /// class to Mirror for networking operations. 
    /// </summary>
    public interface INetworkableFieldValue {
        string ID { get; }
        void SerializeValue(NetworkWriter writer);
    }
 
    public static class AbilitySerializer {
        public static void WriteNetworkableFieldValue(this NetworkWriter writer, INetworkableFieldValue networkableFieldValue) {
            writer.WriteString(networkableFieldValue.ID);
            networkableFieldValue.SerializeValue(writer);
        }

        public static INetworkableFieldValue ReadNetworkableFieldValue(this NetworkReader reader) {
            return NetworkableFieldValueDeserializer.Deserialize(reader);
        }
    }
}