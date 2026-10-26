using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using Gameplay.UI;
using UnityEngine;
using Util;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/CatapultAttackAbilityData")]
    public class CatapultAttackAbilityDataAsset : BaseAbilityDataAsset<CatapultAttackAbilityData, CatapultAttackAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for doing an area-of-effect attack that targets multiple cells.
    /// Acts as a sort of hybrid targetable attack and default attack, since the player can target particular cells with
    /// it or otherwise just have it auto-attack cells with enemies. No concept of attack-moving here since the
    /// unit needs to be sieged which immobilizes it. 
    /// </summary>
    [Serializable]
    public class CatapultAttackAbilityData : AbilityDataBase<CatapultAttackAbilityParameters>, ITargetableAbilityData, IAttackAbilityData {
        [SerializeField] private AttackAbilityLogicType _attackType;
        public AttackAbilityLogicType AttackType => _attackType;

        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => true;
        public override bool ManuallyCancelable => true;

        public override void SelectAbility(GridEntity selector, object selectData) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
            HashSet<Vector2Int> cellsAroundHoveredVertex = GameManager.Instance.GridInputController.CurrentClosestVertex;
            UpdateAimVisual(selector, cellsAroundHoveredVertex);
        }

        public override bool TryUseAbilityAutomatically(GridEntity selector, GridEntity target, GridEntity.TargetType targetType, Vector2Int targetCell) {
            // TODO
            throw new NotImplementedException();
        }

        protected override AbilityLegality AbilityLegalImpl(CatapultAttackAbilityParameters parameters, GridEntity entity, GameTeam team, out string failureReason) {
            failureReason = null;
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(CatapultAttackAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new CatapultAttackAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new CatapultAttackAbilityParameters {
                TargetCells = ((string)json["TargetCells"]).ToVector2Ints()
            };
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            return true;
        }
        
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(cellPosition);    // Only able to target the top entity!
            if (target != null && target.Team == selectedEntity.Team) {
                target = null;
            }
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, new AttackAbilityParameters {
                    Target = target, 
                    Destination = cellPosition
                }, true, true, true, true);
            selectedEntity.SetTargetLocation(cellPosition, null, PathVisualizer.PathType.AttackMove);
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector, object targetData) {
            // Nothing to do
        }

        public void UpdateHoveredCell(GridEntity selector, Vector2Int? cell) {
            // Nothing to do
        }
        
        public void UpdateHoveredVertex(GridEntity selector, HashSet<Vector2Int> cellsAroundHoveredVertex) {
            UpdateAimVisual(selector, cellsAroundHoveredVertex);
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

        public string AbilityVerb => "attack";
        public bool ShowIconOnGridWhenSelected => true;
        public bool DeselectAfterAttempt => true;
        public List<MouseClick> DefaultAllowableClicks => new() { MouseClick.Left };

        #region Visuals and Aiming

        private void UpdateAimVisual(GridEntity selector, HashSet<Vector2Int> cellsAroundHoveredVertex) {
            if (cellsAroundHoveredVertex == null) {
                // TODO hide visual
                return;
            }

            int yMax = cellsAroundHoveredVertex.Select(c => c.y).Max();
            if (cellsAroundHoveredVertex.Count(c => c.y == yMax) == 2) {
                // TODO Use wide-topped visual
            } else {
                // TODO Use wide-bottomed visual
            }
        }

        #endregion
    }
}