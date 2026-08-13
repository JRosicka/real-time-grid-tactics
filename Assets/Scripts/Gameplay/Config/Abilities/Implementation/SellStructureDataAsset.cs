using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/SellStructureAbilityData")]
    public class SellStructureDataAsset : BaseAbilityDataAsset<SellStructureAbilityData, NullAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the selling a structure for a refund and a peasant
    /// </summary>
    [Serializable]
    public class SellStructureAbilityData : AbilityDataBase<NullAbilityParameters> {
        public EntityData Peasant;
        
        private GridEntityCollection EntitiesOnGrid => GameManager.Instance.CommandManager.EntitiesOnGrid;
        
        public override bool CancelableWhileOnCooldown => true;
        public override bool CancelableWhileInProgress => false;
        public override bool Cancelable => false;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selector, this, new NullAbilityParameters(), true, false, false, true); 
        }
        
        protected override AbilityLegality AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity, GameTeam team, out string failureReason) {
            failureReason = null;
            if (entity == null || entity.DeadOrDying || entity.Location == null) {
                return AbilityLegality.IndefinitelyIllegal;
            }

            // Not legal if the structure is producing anything
            if (entity.BuildQueue.Queue(team).Count > 0) {
                failureReason = "Can not sell while producing anything.";
                return AbilityLegality.IndefinitelyIllegal;
            }
            
            // Not legal if the structure has any units on it
            if (OccupiedByOtherEntities(entity)) {
                failureReason = "Can not sell while occupied by units.";
                return AbilityLegality.IndefinitelyIllegal;
            }
            
            // Otherwise legal
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new SellStructureAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new NullAbilityParameters();
        }

        public bool OccupiedByOtherEntities(GridEntity performer) {
            if (performer.Location == null) return false;
            
            List<GridEntity> entitiesAtLocation = EntitiesOnGrid.EntitiesAtLocation(performer.Location.Value).Entities.Select(e => e.Entity).ToList();
            return entitiesAtLocation.Any(e => e != performer && !e.Tags.Contains(EntityTag.Resource));
        }
    }
}