using Gameplay.Config;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Logic for tracking an entity's current target
    /// </summary>
    public class TargetLocationLogic {
        /// <summary>
        /// Whether the target is a rally point rather than a destination to move to
        /// </summary>
        public bool CanRally { get; }
        
        /// <summary>
        /// Values only set on the server!
        /// </summary>
        public Vector2Int CurrentTarget { get; set; }
        
        /// <summary>
        /// The current entity that we are targeting with an ability, otherwise null
        /// </summary>
        public GridEntity TargetEntity { get; set; }

        public TargetLocationLogic() : this(false, new Vector2Int(0, 0), null) { }
        public TargetLocationLogic(bool canRally, Vector2Int initialTargetLocation, GridEntity targetEntity) {
            CanRally = canRally;
            CurrentTarget = initialTargetLocation;
            TargetEntity = targetEntity;
        }
    }
    
    public static class TargetLocationLogicSerializer {
        public static void WriteTargetLocationLogic(this NetworkWriter writer, TargetLocationLogic logic) {
            writer.WriteBool(logic.CanRally);
            writer.WriteVector2(logic.CurrentTarget);
            writer.Write(logic.TargetEntity);
        }

        public static TargetLocationLogic ReadTargetLocationLogic(this NetworkReader reader) {
            return new TargetLocationLogic(reader.ReadBool(), 
                reader.ReadVector2Int(), 
                reader.Read<GridEntity>());
        }
    }
}