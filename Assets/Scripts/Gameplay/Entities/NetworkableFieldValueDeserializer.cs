using System;
using Mirror;

namespace Gameplay.Entities {
    /// <summary>
    /// Handles reconstructing a <see cref="INetworkableFieldValue"/> after deserialization
    /// </summary>
    public static class NetworkableFieldValueDeserializer {
        public static INetworkableFieldValue Deserialize(NetworkReader reader) {
            string id = reader.ReadString();
            switch (id) {
                case nameof(NetworkableIntegerValue):
                    return NetworkableIntegerValue.Deserialize(reader);
                case nameof(ResourceAmount):
                    return ResourceAmount.Deserialize(reader);
                case nameof(TargetLocationLogic):
                    return TargetLocationLogic.Deserialize(reader);
                default:
                    throw new Exception($"No deserializer found for {nameof(INetworkableFieldValue)} with ID {id}");
            }
        }
    }
}