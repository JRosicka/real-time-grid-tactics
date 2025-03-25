using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/ChargeAbilityData")]
    public class ChargeAbilityDataAsset : BaseAbilityDataAsset<ChargeAbilityData, ChargeAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to charge through multiple cells in a straight line
    /// </summary>
    [Serializable]
    public class ChargeAbilityData : AbilityDataBase<ChargeAbilityParameters>, ITargetableAbilityData {
        [Header("Charge configuration")]
        public int ChargeRange;

        public GridController GridController => GameManager.Instance.GridController;
        public EntitySelectionManager EntitySelectionManager => GameManager.Instance.EntitySelectionManager;
        public ICommandManager CommandManager => GameManager.Instance.CommandManager;
        
        public override bool CanBeCanceled => false;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileQueued => false;

        public override void SelectAbility(GridEntity selector) {
            EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
            Vector2Int? currentlyHoveredCell = GameManager.Instance.GridInputController.CurrentHoveredCell;
            UpdateChargePathVisual(selector, currentlyHoveredCell);
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override bool AbilityLegalImpl(ChargeAbilityParameters parameters, GridEntity entity) {
            return CanChargeToCell(entity, parameters.Destination);
        }

        protected override IAbility CreateAbilityImpl(ChargeAbilityParameters parameters, GridEntity performer) {
            return new ChargeAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            return CanChargeToCell(selectedEntity, cellPosition);
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            Vector2Int? destination = DetermineChargeDestination(selectedEntity, cellPosition);
            if (destination == null) {
                Debug.LogWarning("No viable charge location found, that's unexpected.");
                return;
            }
            selectedEntity.SetTargetLocation(cellPosition, null, true);
            GameManager.Instance.AbilityAssignmentManager.PerformAbility(selectedEntity, this, new ChargeAbilityParameters {
                Destination = destination.Value,
                MoveDestination = destination.Value,
                ClickLocation = cellPosition,
                SelectorTeam = selectorTeam
            }, true, false);
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector, object targetData) {
            Vector2Int? currentlyHoveredCell = GameManager.Instance.GridInputController.CurrentHoveredCell;
            UpdateChargePathVisual(selector, currentlyHoveredCell);
        }

        public void UpdateHoveredCell(GridEntity selector, Vector2Int? cell) {
            UpdateChargePathVisual(selector, cell);
        }

        public void Deselect() {
            HideChargePathVisual();
        }

        public bool MoveToTargetCellFirst => false;
        public GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData) {
            return null;
        }
        
        #region Charge logic
        
        private bool CanChargeToCell(GridEntity selector, Vector2Int cell) {
            return DetermineChargeDestination(selector, cell) != null;
        }
        
        /// <summary>
        /// Given a target cell, determine the destination that a charge would land on.
        /// </summary>
        /// <returns>The destination cell, or null if no viable destination exists</returns>
        private Vector2Int? DetermineChargeDestination(GridEntity selector, Vector2Int targetCell) {
            Vector2Int? selectorLocation = selector.Location;
            if (selectorLocation == null) return null;

            (List<Vector2Int> line1, List<Vector2Int> line2, bool equidistant) = CellDistanceLogic.GetCellsInClosestStraightLines(selectorLocation.Value, targetCell, ChargeRange);
            List<GridData.CellData> line1Cells = line1.Select(c => GridController.GridData.GetCell(c)).NotNull().ToList();
            List<GridData.CellData> line2Cells = line2 == null 
                ? new List<GridData.CellData>() 
                : line2.Select(c => GridController.GridData.GetCell(c)).NotNull().ToList();

            if (line1Cells.Count == 0 && line2Cells.Count == 0) return null;
            (Vector2Int? closestCellFromLine1, int distance1) = GetClosestLegalCell(targetCell, line1Cells, selector);
            (Vector2Int? closestCellFromLine2, int distance2) = GetClosestLegalCell(targetCell, line2Cells, selector);

            if (closestCellFromLine1 == null && closestCellFromLine2 == null) return null;
            if (closestCellFromLine1 == null) return closestCellFromLine2;
            if (closestCellFromLine2 == null) return closestCellFromLine1;
            // Pick the closer line regardless of distance if one of the lines has a closer angle
            if (!equidistant) return closestCellFromLine1;
            if (distance1 < distance2) return closestCellFromLine1;
            if (distance1 > distance2) return closestCellFromLine2;
            // The two closest cells are the same distance, so just pick the first one
            return closestCellFromLine1;
        }

        /// <summary>
        /// Given a set of a straight line of cells, get the closest cell (that can be legally traveled to) to the given
        /// origin, along with the distance. Ties are broken by whichever cell is father along the path. 
        /// </summary>
        private (Vector2Int?, int) GetClosestLegalCell(Vector2Int origin, List<GridData.CellData> cells, GridEntity selector) {
            List<Vector2Int> viableCells = GetViableCells(cells, selector);
            if (viableCells.Count == 0) return (null, 0);

            Vector2Int? closestCell = null;
            int closestDistance = int.MaxValue;
            foreach (Vector2Int cell in viableCells) {
                int distance = CellDistanceLogic.DistanceBetweenCells(origin, cell);
                // Use <= instead of < so that the further cell along the path breaks any ties
                if (distance <= closestDistance) {
                    closestCell = cell;
                    closestDistance = distance;
                }
            }
            return (closestCell, closestDistance);
        }

        /// <summary>
        /// Given a set of cells in a line away from the origin, return the subset of cells that the selected
        /// <see cref="GridEntity"/> can legally travel through. 
        /// </summary>
        private List<Vector2Int> GetViableCells(List<GridData.CellData> cells, GridEntity selector) {
            // Caches and helper function for blocker calculations
            List<GridEntityCollection.PositionedGridEntityCollection> locationsWithEntities = cells
                .Select(c => CommandManager.EntitiesOnGrid.EntitiesAtLocation(c.Location))
                .Where(p => p != null)
                .ToList();
            
            bool DoesCellBlock(GridData.CellData cell) {
                // Can not go through a cell if it hinders movement 
                if (selector.InaccessibleTiles.Contains(cell.Tile) || selector.SlowTiles.Contains(cell.Tile)) {
                    return true;
                }
                
                // Can not go through a cell if it contains any unit or any enemy structure (friendly structures are fine)
                GridEntity entityAtCell = locationsWithEntities
                    .FirstOrDefault(l => l.Location == cell.Location)
                    ?.GetTopEntity()?.Entity;
                if (entityAtCell == null) {
                    return false;
                }
                if (entityAtCell.Team == selector.Team.OpponentTeam()) {
                    return true;
                } 
                if (entityAtCell.EntityData.IsStructure || entityAtCell.EntityData.Tags.Contains(EntityData.EntityTag.Resource)) {
                    return false;
                }

                return true;
            }

            // Iterate through each cell until we find a blocker
            List<GridData.CellData> viableCells = new List<GridData.CellData>();
            for (int i = 0; i < cells.Count; i++) {
                GridData.CellData cell = cells[i];
                bool stopIterating = DoesCellBlock(cell);
                if (stopIterating && i == 0) {
                    // The first cell in the path is a blocker, so we can't charge down this line. Need to be able to move at least one cell. 
                    break;
                }
                
                GridEntity entityAtCell = locationsWithEntities
                    .FirstOrDefault(l => l.Location == cell.Location)
                    ?.GetTopEntity()?.Entity;
                if (entityAtCell != null) {
                    // Can not target a cell with a friendly unit that we can't share the cell with
                    if (entityAtCell.Team == selector.Team && !entityAtCell.EntityData.FriendlyUnitsCanShareCell) {
                        break;
                    }
                } else if (selector.InaccessibleTiles.Contains(cell.Tile) || selector.SlowTiles.Contains(cell.Tile)) {
                    // Otherwise can not charge to an empty cell if it hinders movement
                    break;
                }
                viableCells.Add(cell);
                
                if (stopIterating) {
                    break;
                }
            }

            return viableCells.Select(c => c.Location).ToList();
        }
        
        #endregion
        #region Visuals

        private void UpdateChargePathVisual(GridEntity selector, Vector2Int? currentlyHoveredCell) {
            if (currentlyHoveredCell == null) {
                HideChargePathVisual();
            } else {
                Vector2Int? destination = DetermineChargeDestination(selector, currentlyHoveredCell.Value);
                if (destination == null) {
                    HideChargePathVisual();
                } else {
                    ShowChargePathVisual(selector, destination.Value);
                }
            }
        }

        private void HideChargePathVisual() {
            GameManager.Instance.GridController.ClearPath(true);
        }
        
        private void ShowChargePathVisual(GridEntity selector, Vector2Int destination) {
            GridEntity entityAtDestination = GameManager.Instance.GetTopEntityAtLocation(destination);
            bool enemyEntityPresent = selector.GetTargetType(entityAtDestination) == GridEntity.TargetType.Enemy;
            
            PathfinderService.Path path = GameManager.Instance.PathfinderService.GetPathInStraightLine(selector, destination);
            GameManager.Instance.GridController.VisualizePath(path, enemyEntityPresent, enemyEntityPresent, true);
        }

        #endregion
    }
}