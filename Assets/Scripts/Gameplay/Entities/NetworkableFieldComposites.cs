using System;
using System.Collections.Generic;
using Mirror;

namespace Gameplay.Entities {
    [Serializable]
    public class NetworkableBoostRegistrationsValue : INetworkableFieldValue {
        public BoostRegistrations Value;
        public NetworkableBoostRegistrationsValue(BoostRegistrations value) {
            Value = value;
        }

        public string ID => nameof(NetworkableBoostRegistrationsValue);

        public void SerializeValue(NetworkWriter writer) {
            writer.WriteInt(Value.Boosts.Count);
            foreach ((GridEntity, float) boost in Value.Boosts) {
                writer.Write(boost.Item1);
                writer.WriteFloat(boost.Item2);
            }
        }

        public static NetworkableBoostRegistrationsValue Deserialize(NetworkReader reader) {
            List<(GridEntity, float)> boosts = new();
            int length = reader.ReadInt();
            for (int i = 0; i < length; i++) {
                boosts.Add((reader.Read<GridEntity>(), reader.ReadFloat()));
            }
            return new NetworkableBoostRegistrationsValue(new BoostRegistrations(boosts));
        }
    }
}