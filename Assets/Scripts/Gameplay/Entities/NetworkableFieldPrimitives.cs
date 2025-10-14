using System;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    [Serializable]
    public class NetworkableIntegerValue : INetworkableFieldValue {
        public int Value;
        public NetworkableIntegerValue(int value) {
            Value = value;
        }

        public string ID => nameof(NetworkableIntegerValue);

        public void SerializeValue(NetworkWriter writer) {
            writer.WriteInt(Value);
        }

        public static NetworkableIntegerValue Deserialize(NetworkReader reader) {
            return new NetworkableIntegerValue(reader.ReadInt());
        }
    }

    [Serializable]
    public class NetworkableFloatValue : INetworkableFieldValue {
        public float Value;
        public NetworkableFloatValue(float value) {
            Value = value;
        }

        public string ID => nameof(NetworkableFloatValue);

        public void SerializeValue(NetworkWriter writer) {
            writer.WriteFloat(Value);
        }

        public static NetworkableFloatValue Deserialize(NetworkReader reader) {
            return new NetworkableFloatValue(reader.ReadFloat());
        }
    }

    [Serializable]
    public class NetworkableVector2IntegerValue : INetworkableFieldValue {
        public Vector2Int Value;
        public NetworkableVector2IntegerValue(Vector2Int value) {
            Value = value;
        }

        public string ID => nameof(NetworkableVector2IntegerValue);

        public void SerializeValue(NetworkWriter writer) {
            writer.WriteVector2Int(Value);
        }

        public static NetworkableVector2IntegerValue Deserialize(NetworkReader reader) {
            return new NetworkableVector2IntegerValue(reader.ReadVector2Int());
        }
    }
    
    [Serializable]
    public class NetworkableGridEntityValue : INetworkableFieldValue {
        public GridEntity Value;
        public NetworkableGridEntityValue(GridEntity value) {
            Value = value;
        }

        public string ID => nameof(NetworkableGridEntityValue);

        public void SerializeValue(NetworkWriter writer) {
            writer.Write(Value);
        }

        public static NetworkableGridEntityValue Deserialize(NetworkReader reader) {
            return new NetworkableGridEntityValue(reader.Read<GridEntity>());
        }
    }
}