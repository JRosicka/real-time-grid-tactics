using System;
using Mirror;

namespace Gameplay.Entities {
    /// <summary>
    /// Handles reconstructing a <see cref="INetworkableFieldValue"/> after deserialization
    /// </summary>
    public static class NetworkableFieldValueDeserializer {
        public static INetworkableFieldValue Deserialize(NetworkReader reader) {
            string id = reader.ReadString();
            return id switch {
                nameof(NetworkableIntegerValue) => NetworkableIntegerValue.Deserialize(reader),
                nameof(NetworkableVector2IntegerValue) => NetworkableVector2IntegerValue.Deserialize(reader),
                nameof(ResourceAmount) => ResourceAmount.Deserialize(reader),
                nameof(TargetLocationLogic) => TargetLocationLogic.Deserialize(reader),
                _ => throw new Exception($"No deserializer found for {nameof(INetworkableFieldValue)} with ID {id}")
            };
        }
    }
}