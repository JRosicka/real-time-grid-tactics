using System.Linq;
using Gameplay.Config;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Logic for picking up, dropping off, and assessing capacity for entities.
    /// </summary>
    public static class EntityPickUpLogic {
        public static bool CanPickUp(GridEntity carrier, GridEntity passenger) {
            if (carrier == null || carrier.Location == null || carrier.DeadOrDying) return false;
            if (passenger == null || passenger.Location == null || passenger.DeadOrDying) return false;

            if (!EntityCanEverBePickedUp(passenger, carrier.Team)) return false;
            
            // Must have space
            int size = EntitySize(passenger);
            if (size > AvailableCapacity(carrier)) return false;
            
            return true;
        }

        // Server method
        public static bool TryPickUp(GridEntity carrier, GridEntity passenger) {
            if (!CanPickUp(carrier, passenger)) return false;

            // Must be adjacent
            if (!CellDistanceLogic.Neighbors(carrier.Location!.Value).Contains(passenger.Location!.Value)) return false;

            carrier.PickUpEntity(passenger);

            GameManager.Instance.CommandManager.RemoveEntityFromGrid(passenger);

            return true;
        }

        public static AbilityLegality CanDropOff(GridEntity carrier, GridEntity passenger) {
            if (carrier == null || carrier.Location == null || carrier.DeadOrDying) return AbilityLegality.IndefinitelyIllegal;
            if (passenger == null || passenger.Location != null || passenger.DeadOrDying) return AbilityLegality.IndefinitelyIllegal;   // passenger does not have a location
            
            // Carrier must be carrying the passenger
            if (!carrier.CarriedEntities.Contains(passenger)) return AbilityLegality.IndefinitelyIllegal;
            
            return AbilityLegality.Legal;
        }

        // Server method
        public static bool TryDropOff(GridEntity carrier, GridEntity passenger, Vector2Int location) {
            if (CanDropOff(carrier, passenger) != AbilityLegality.Legal) return false;

            // The location must be open
            if (!PathfinderService.CanEntityEnterCell(location, passenger.EntityData, passenger.Team)) return false;
            
            // Carrier must be adjacent
            if (!CellDistanceLogic.Neighbors(carrier.Location!.Value).Contains(location)) return false;

            carrier.DropEntity(passenger);
            
            // TODO

            return true;
        }

        private static bool EntityCanEverBePickedUp(GridEntity entity, GameTeam playerTeam) {
            if (entity == null) return false;
            if (playerTeam != entity.Team) return false;
            if (entity.EntityData.IsStructure) return false;
            
            return true;
        }

        private static int EntitySize(GridEntity entity) {
            if (!EntityCanEverBePickedUp(entity, entity.Team)) return 9999;
            if (entity.Tags.Contains(EntityTag.Massive)) return 2;
            return 1;
        }

        private static int AvailableCapacity(GridEntity entity) {
            int capacity = entity.CarryCapacity;
            int currentEncumbrance = entity.CarriedEntities.Sum(EntitySize);
            return capacity - currentEncumbrance;
        }
    }
}