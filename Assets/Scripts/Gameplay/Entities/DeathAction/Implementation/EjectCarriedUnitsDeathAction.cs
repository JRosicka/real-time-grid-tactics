using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Entities.DeathAction {
    /// <summary>
    /// <see cref="IDeathAction"/> for putting carried units back on the grid when the carrier dies
    /// </summary>
    public class EjectCarriedUnitsDeathAction : IDeathAction {
        
        public void DoDeathAction(GridEntity dyingEntity, GridEntity attacker) {
            List<GridEntity> carriedEntities = new List<GridEntity>(dyingEntity.CarriedEntities);
            foreach (GridEntity carriedEntity in carriedEntities) {
                TryPlaceCarriedEntity(dyingEntity, carriedEntity, attacker);
            }
        }

        private void TryPlaceCarriedEntity(GridEntity dyingEntity, GridEntity carriedEntity, GridEntity attacker) {
            if (!EntityPickUpLogic.CanDropOff(dyingEntity, carriedEntity, false) || dyingEntity.Location == null) {
                DestroyCarriedEntity(carriedEntity, attacker);
                return;
            }
            
            // Try to place on the dying entity's location
            if (EntityPickUpLogic.TryDropOff(dyingEntity, carriedEntity, dyingEntity.Location!.Value, true, false)) {
                carriedEntity.View.DoGenericDroppedOffAnimation(dyingEntity.Location!.Value, dyingEntity.Location!.Value);
                return;
            }
            
            // Otherwise try to place on some adjacent space
            foreach (Vector2Int adjacentCell in GameManager.Instance.GridController.GridData.GetAdjacentCells(dyingEntity.Location!.Value).Select(c => c.Location)) {
                if (EntityPickUpLogic.TryDropOff(dyingEntity, carriedEntity, adjacentCell, false, false)) {
                    carriedEntity.View.DoGenericDroppedOffAnimation(dyingEntity.Location!.Value, adjacentCell);
                    return;
                }
            }
            
            // Otherwise can't place, so destroy it
            DestroyCarriedEntity(carriedEntity, attacker);
        }

        private void DestroyCarriedEntity(GridEntity carriedEntity, GridEntity attacker) {
            carriedEntity.HPHandler.Kill(attacker);
        }
    }
}