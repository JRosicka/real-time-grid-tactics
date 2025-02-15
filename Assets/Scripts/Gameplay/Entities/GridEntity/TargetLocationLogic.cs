using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Logic for tracking an entity's current target
    /// </summary>
    public class TargetLocationLogic : INetworkableFieldValue {
        public string ID => nameof(TargetLocationLogic);

        /// <summary>
        /// Whether the target is a rally point rather than a destination to move to
        /// </summary>
        public bool CanRally { get; }
        
        public bool Attacking { get; }
        
        public bool HidePathDestination { get; }
        
        /// <summary>
        /// Values only set on the server!
        /// </summary>
        public Vector2Int CurrentTarget { get; set; }
        
        /// <summary>
        /// The current entity that we are targeting with an ability, otherwise null
        /// </summary>
        public GridEntity TargetEntity { get; set; }

        public TargetLocationLogic() : this(false, new Vector2Int(0, 0), null, false, false) { }
        public TargetLocationLogic(bool canRally, Vector2Int initialTargetLocation, GridEntity targetEntity, bool attacking, bool hidePathDestination) {
            CanRally = canRally;
            CurrentTarget = initialTargetLocation;
            TargetEntity = targetEntity;
            Attacking = attacking;
            HidePathDestination = hidePathDestination;
        }

        public void SerializeValue(NetworkWriter writer) {
            writer.WriteBool(CanRally);
            writer.WriteVector2Int(CurrentTarget);
            writer.Write(TargetEntity);
            writer.WriteBool(Attacking);
            writer.WriteBool(HidePathDestination);
        }

        public static TargetLocationLogic Deserialize(NetworkReader reader) {
            return new TargetLocationLogic(reader.ReadBool(), reader.ReadVector2Int(),
                reader.Read<GridEntity>(), reader.ReadBool(), reader.ReadBool());
        }
    }
    
    public static class TargetLocationLogicSerializer {
        public static void WriteTargetLocationLogic(this NetworkWriter writer, TargetLocationLogic logic) {
            if (logic == null) {
                writer.WriteBool(false);    // Indicates null
                return;
            }
            writer.WriteBool(true);    // Indicates not null
            writer.WriteBool(logic.CanRally);
            writer.WriteVector2Int(logic.CurrentTarget);
            writer.Write(logic.TargetEntity);
            writer.WriteBool(logic.Attacking);
            writer.WriteBool(logic.HidePathDestination);
        }

        public static TargetLocationLogic ReadTargetLocationLogic(this NetworkReader reader) {
            if (reader.ReadBool()) {
                return new TargetLocationLogic(reader.ReadBool(),
                    reader.ReadVector2Int(),
                    reader.Read<GridEntity>(), reader.ReadBool(), reader.ReadBool());
            }
            
            return null;
        }
    }
}