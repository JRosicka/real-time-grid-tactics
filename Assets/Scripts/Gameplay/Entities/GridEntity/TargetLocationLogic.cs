using Gameplay.UI;
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
        
        public PathVisualizer.PathType PathType { get; }
        
        public bool HidePathDestination { get; }
        
        /// <summary>
        /// Values only set on the server!
        /// </summary>
        public Vector2Int CurrentTarget { get; }
        
        /// <summary>
        /// The current entity that we are targeting with an ability, otherwise null
        /// </summary>
        public GridEntity TargetEntity { get; }

        public TargetLocationLogic() : this(false, new Vector2Int(0, 0), null, PathVisualizer.PathType.Move, false) { }
        public TargetLocationLogic(bool canRally, Vector2Int initialTargetLocation, GridEntity targetEntity, PathVisualizer.PathType pathType, bool hidePathDestination) {
            CanRally = canRally;
            CurrentTarget = initialTargetLocation;
            TargetEntity = targetEntity;
            PathType = pathType;
            HidePathDestination = hidePathDestination;
        }

        public void SerializeValue(NetworkWriter writer) {
            writer.WriteBool(CanRally);
            writer.WriteVector2Int(CurrentTarget);
            writer.Write(TargetEntity);
            writer.WriteInt((int)PathType);
            writer.WriteBool(HidePathDestination);
        }

        public static TargetLocationLogic Deserialize(NetworkReader reader) {
            return new TargetLocationLogic(reader.ReadBool(), reader.ReadVector2Int(),
                reader.Read<GridEntity>(), (PathVisualizer.PathType)reader.ReadInt(), reader.ReadBool());
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
            writer.WriteInt((int)logic.PathType);
            writer.WriteBool(logic.HidePathDestination);
        }

        public static TargetLocationLogic ReadTargetLocationLogic(this NetworkReader reader) {
            if (reader.ReadBool()) {
                return new TargetLocationLogic(reader.ReadBool(),
                    reader.ReadVector2Int(),
                    reader.Read<GridEntity>(), (PathVisualizer.PathType)reader.ReadInt(), reader.ReadBool());
            }
            
            return null;
        }
    }
}