using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Upgrades;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using Gameplay.UI;
using JetBrains.Annotations;
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
        
        private GridReticleVisualizer GridReticleVisualizer => GameManager.Instance.GridReticleVisualizer;
        private GridController GridController => GameManager.Instance.GridController;
        private GridData GridData => GridController.GridData;
        
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
                TargetCells = ((string)json["TargetCells"]).ToVector2IntSet()
            };
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            return true;
        }
        
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            HashSet<Vector2Int> closestVertex = ClosestLegalAimVertex(selectedEntity, GameManager.Instance.GridInputController.CurrentClosestVertex);
            if (closestVertex != null) {
                GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, new CatapultAttackAbilityParameters() {
                    TargetCells = closestVertex,
                }, true, true, true, true);
            
                selectedEntity.SetTargetLocation(cellPosition, null, PathVisualizer.PathType.TargetAttack);
            }

            UpdateAimVisual(selectedEntity, null);
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector, object targetData) {
            // Nothing to do
        }

        public void UpdateHoveredCell(GridEntity selector, Vector2Int? cell) {
            UpdateAimVisual(selector, GameManager.Instance.GridInputController.CurrentClosestVertex);
        }
        
        public void UpdateHoveredVertex(GridEntity selector, HashSet<Vector2Int> cellsAroundHoveredVertex) {
            UpdateAimVisual(selector, cellsAroundHoveredVertex);
        }
        
        public void OwnedPurchasablesChanged(GridEntity selector) {
            UpdateAimVisual(selector, GameManager.Instance.GridInputController.CurrentClosestVertex);
        }

        public void Deselect(GridEntity selector) {
            UpdateAimVisual(selector, null);
        }

        public bool MoveToTargetCellFirst => false;
        public GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData) {
            return null;
        }

        public string AbilityVerb => "attack";
        public bool ShowIconOnGridWhenSelected => false;
        public bool DeselectAfterAttempt => true;
        public List<MouseClick> DefaultAllowableClicks => new() { MouseClick.Left };

        public HashSet<Vector2Int> GetCellsInRange(GridEntity performer) {
            if (!performer.IsActive()) return new HashSet<Vector2Int>();
            
            return GridData.GetCellsInRange(performer.Location!.Value, Range(performer))
                .Select(c => c.Location)
                .Where(l => l != performer.Location!.Value)
                .ToHashSet();
        }
        
        public int Range(GridEntity performer) => performer.Range;
        
        #region Visuals and Aiming

        private void UpdateAimVisual(GridEntity selector, HashSet<Vector2Int> cellsAroundHoveredVertex) {
            if (cellsAroundHoveredVertex == null) {
                GridReticleVisualizer.HideCatapultAimReticle();
                return;
            }

            HashSet<Vector2Int> closestVertex = ClosestLegalAimVertex(selector, cellsAroundHoveredVertex);
            if (closestVertex == null) {
                GridReticleVisualizer.HideCatapultAimReticle();
                return;
            }
            
            GridReticleVisualizer.UpdateCatapultAimReticle(closestVertex);
        }

        [CanBeNull] private HashSet<Vector2Int> ClosestLegalAimVertex(GridEntity selector, HashSet<Vector2Int> cellsAroundHoveredVertex) {
            HashSet<Vector2Int> cellsInRange = GetCellsInRange(selector);
            if (cellsAroundHoveredVertex.All(l => cellsInRange.Contains(l))) {
                return cellsAroundHoveredVertex;
            }
            
            // Pick the closest in-range vertex to display the aim visual
            // First, get the distance of the closest legal cell to the hovered cell. Track the in-range cells that are closest to any of the hovered cells (probably a tie for multiple cells)
            int minDistance = int.MaxValue;
            List<Vector2Int> closestInRangeCells = new List<Vector2Int>();
            foreach (Vector2Int cell in cellsInRange) {
                foreach (Vector2Int hoveredCell in cellsAroundHoveredVertex) {
                    int distance = CellDistanceLogic.DistanceBetweenCells(cell, hoveredCell);
                    if (distance < minDistance) {
                        minDistance = distance;
                        closestInRangeCells.Clear();
                        closestInRangeCells.Add(cell);
                    } else if (distance == minDistance && !closestInRangeCells.Contains(cell)) {
                        closestInRangeCells.Add(cell);
                    }
                }
            }
            
            // Get all the in-range vertices that all of those closest cells are a part of
            List<HashSet<Vector2Int>> eligibleVertices = new List<HashSet<Vector2Int>>();
            foreach (Vector2Int cell in closestInRangeCells) {
                // Get the set of in-range vertices for this cell
                List<Vector2Int> eligibleAdjacentCells = GridData.GetAdjacentCells(cell)
                    .Select(c => c.Location)
                    .Where(c => cellsInRange.Contains(c))
                    .ToList();
                // Get the vertices that include eligible cells
                foreach (Vector2Int adjacentCell in eligibleAdjacentCells) {
                    foreach (Vector2Int otherAdjacentCell in eligibleAdjacentCells.Where(c => c.AreCellsAdjacent(adjacentCell))) {
                        HashSet<Vector2Int> newVertex = new HashSet<Vector2Int> { cell, adjacentCell, otherAdjacentCell };
                        if (!eligibleVertices.Contains(newVertex)) {
                            eligibleVertices.Add(newVertex);
                        }
                    }
                }
            }
            
            // Determine which one of those vertices is closest in world space to the current mouse position
            Vector2 currentMousePos = GameManager.Instance.GridInputController.CurrentMousePosition;
            if (eligibleVertices.Count == 0) {
                // Not legal, probably because the player is aiming at the corner of the map
                return null;
            }
            return eligibleVertices.OrderBy(v => (GridController.GetWorldPosition(v) - currentMousePos).magnitude).First();
        }

        #endregion
    }
}