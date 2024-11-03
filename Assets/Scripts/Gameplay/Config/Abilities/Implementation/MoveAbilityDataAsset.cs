using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/MoveAbilityData")]
    public class MoveAbilityDataAsset : BaseAbilityDataAsset<MoveAbilityData, MoveAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to move an entity
    /// </summary>
    [Serializable]
    public class MoveAbilityData : AbilityDataBase<MoveAbilityParameters>, ITargetableAbilityData {
        public override bool CanBeCanceled => true;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileQueued => true;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, null);
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override bool AbilityLegalImpl(MoveAbilityParameters parameters, GridEntity entity) {
            return CanTargetCell(parameters.Destination, entity, parameters.SelectorTeam, null);
        }

        protected override IAbility CreateAbilityImpl(MoveAbilityParameters parameters, GridEntity performer) {
            return new MoveAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GridEntity.Team selectorTeam, System.Object targetData) {
            if (selectedEntity == null || selectedEntity.MyTeam != selectorTeam) return false;

            if (selectedEntity.Location == cellPosition) {
                // Bro you're already here
                return false;
            }

            return true;
        }
        
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GridEntity.Team selectorTeam, System.Object targetData) {
            // Nothing to do here - the caller is responsible for moving the entity to the destination first anyway, which 
            // is the only thing we're trying to do with this ability
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector) {
            // Nothing to do
        }

        public bool MoveToTargetCellFirst => true;
    }
}