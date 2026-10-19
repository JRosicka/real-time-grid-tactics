using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using UnityEngine;
using Util;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/DropOffUnitAbilityData")]
    public class DropOffUnitAbilityDataAsset : BaseAbilityDataAsset<DropOffUnitAbilityData, DropOffUnitAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for dropping off a unit.
    /// </summary>
    [Serializable]
    public class DropOffUnitAbilityData : AbilityDataBase<DropOffUnitAbilityParameters>, ITargetableAbilityData {
        public override bool CancelableWhileOnCooldown => true;
        public override bool CancelableWhileInProgress => true;
        public override bool ManuallyCancelable => true;
        public override IAbilityParameters OnStartParameters => new DropOffUnitAbilityParameters { Target = null, DropOffLocation = default };

        public override void SelectAbility(GridEntity selector, object selectData) {
            GridEntity carriedEntity = selectData == null 
                ? selector.CarriedEntities.FirstOrDefault() 
                : (GridEntity)selectData;
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, carriedEntity);
        }
        
        protected override AbilityLegality AbilityLegalImpl(DropOffUnitAbilityParameters parameters, GridEntity performer, GameTeam team, out string failureReason) {
            failureReason = null;
            return EntityPickUpLogic.CanDropOff(performer, parameters.Target) ? AbilityLegality.Legal : AbilityLegality.NotCurrentlyLegal;
        }
        
        protected override IAbility CreateAbilityImpl(DropOffUnitAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new DropOffUnitAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new DropOffUnitAbilityParameters {
                Target = GameManager.Instance.CommandManager.EntitiesOnGrid.GetEntityByID((long)json["Target"]),
                DropOffLocation = ((string)json["DropOffLocation"]).ToVector2Int()
            };
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            GridEntity target = (GridEntity)targetData;
            if (target == null) return false;
            
            if (!EntityPickUpLogic.CanDropOff(selectedEntity, target)) return false;
            if (!PathfinderService.CanEntityEnterCell(cellPosition, target.EntityData, target.Team)) return false;
            if (!target.CanPathFindToTile(GameManager.Instance.GridController.GridData.GetCell(cellPosition).Tile)) return false;
            return true;
        }
        
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            GridEntity entity = (GridEntity)targetData;
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, new DropOffUnitAbilityParameters {
                Target = entity,
                DropOffLocation = cellPosition
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

        public string AbilityVerb => "drop off";
        public bool ShowIconOnGridWhenSelected => false;
        public bool DeselectAfterAttempt => true;
        public List<MouseClick> DefaultAllowableClicks => new() { MouseClick.Left };
    }
}