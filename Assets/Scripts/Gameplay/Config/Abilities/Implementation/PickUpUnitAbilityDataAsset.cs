using System;
using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/PickUpUnitAbilityData")]
    public class PickUpUnitAbilityDataAsset : BaseAbilityDataAsset<PickUpUnitAbilityData, PickUpUnitAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for picking up a unit.
    /// </summary>
    [Serializable]
    public class PickUpUnitAbilityData : AbilityDataBase<PickUpUnitAbilityParameters>, ITargetableAbilityData {
        public override bool CancelableWhileOnCooldown => true;
        public override bool CancelableWhileInProgress => true;
        public override bool ManuallyCancelable => true;
        public override IAbilityParameters OnStartParameters => new PickUpUnitAbilityParameters { Target = null };

        public override void SelectAbility(GridEntity selector, object selectData) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }
        
        protected override AbilityLegality AbilityLegalImpl(PickUpUnitAbilityParameters parameters, GridEntity performer, GameTeam team, out string failureReason) {
            failureReason = null;
            return EntityPickUpLogic.CanPickUp(performer, parameters.Target) ? AbilityLegality.Legal : AbilityLegality.NotCurrentlyLegal;
        }
        
        protected override IAbility CreateAbilityImpl(PickUpUnitAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new PickUpUnitAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new PickUpUnitAbilityParameters {
                Target = GameManager.Instance.CommandManager.EntitiesOnGrid.GetEntityByID((long)json["Target"])
            };
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(cellPosition);
            if (target == null) return false;
            
            return EntityPickUpLogic.CanPickUp(selectedEntity, target);
        }
        
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            GridEntity entity = GameManager.Instance.GetTopEntityAtLocation(cellPosition);
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, new PickUpUnitAbilityParameters {
                Target = entity
            }, true, true, false, true);
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector, object targetData) {
            // Nothing to do
        }

        public void UpdateHoveredCell(GridEntity selector, Vector2Int? cell) {
            GameManager.Instance.GridIconDisplayer.DisplayOverHoveredCell(this, cell);
        }

        public void OwnedPurchasablesChanged(GridEntity selector) {
            // Nothing to do
        }

        public void Deselect(GridEntity selector) {
            // Nothing to do
        }

        public bool MoveToTargetCellFirst => false;
        public GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData) {
            return null;
        }

        public string AbilityVerb => "pick up";
        public bool ShowIconOnGridWhenSelected => false;
        public bool DeselectAfterAttempt => true;
        public List<MouseClick> DefaultAllowableClicks => new() { MouseClick.Left };
    }
}