using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
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
        
        public override bool CanBeCanceled => true;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileQueued => true;

        public override void SelectAbility(GridEntity selector) {
            List<Vector2Int> viableTargets = GetViableTargets(selector);
            GridController.UpdateSelectableCells(viableTargets, selector);
            EntitySelectionManager.SelectTargetableAbility(this, null);
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
            selectedEntity.SetTargetLocation(cellPosition, null);
            GameManager.Instance.AbilityAssignmentManager.QueueAbility(selectedEntity, this, new ChargeAbilityParameters {
                Destination = cellPosition,
                MoveDestination = cellPosition,
                SelectorTeam = selectorTeam
            }, true, true, false);
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector) {
            // TODO Really only need to invalidate the cache and recalculate if anything moves/spawns/dies/finishes building *in range* of the charge.
            List<Vector2Int> viableTargets = GetViableTargets(selector);
            GridController.UpdateSelectableCells(viableTargets, selector);
        }

        public bool MoveToTargetCellFirst => false;

        // TODO find some way to cache this, probably
        private List<Vector2Int> GetViableTargets(GridEntity selector) {
            Vector2Int? selectorLocation = selector.Location;
            if (selectorLocation == null) return new List<Vector2Int>();
            
            List<GridData.CellData> cells = GetViableCellsWithoutConsideringTerrainOrEntities(selector);
            
            // Caches and helper function for blocker calculations
            List<GridEntityCollection.PositionedGridEntityCollection> locationsWithEntities = cells
                .Select(c => CommandManager.EntitiesOnGrid.EntitiesAtLocation(c.Location))
                .Where(p => p != null)
                .ToList();
            
            // For viable cells, leave out adjacent cells
            cells = cells.Except(GridController.GridData.GetAdjacentCells(selectorLocation.Value)).ToList();

            List<GridData.CellData> cachedBlockerCells = new List<GridData.CellData>();
            List<GridData.CellData> cachedNotBlockerCells = new List<GridData.CellData>();
            bool DoesCellBlock(GridData.CellData cell) {
                if (cachedBlockerCells.Contains(cell)) return true;
                if (cachedNotBlockerCells.Contains(cell)) return false;
                
                // Can not go through a cell if it hinders movement 
                if (selector.InaccessibleTiles.Contains(cell.Tile) || selector.SlowTiles.Contains(cell.Tile)) {
                    cachedBlockerCells.Add(cell);
                    return true;
                }
                
                // Can not go through a cell if it contains any unit or any enemy structure (friendly structures are fine)
                GridEntity entityAtCell = locationsWithEntities
                    .FirstOrDefault(l => l.Location == cell.Location)
                    ?.GetTopEntity()?.Entity;
                if (entityAtCell != null && (entityAtCell.Team != selector.Team || !entityAtCell.EntityData.IsStructure)) {
                    cachedBlockerCells.Add(cell);
                    return true;
                }

                cachedNotBlockerCells.Add(cell);
                return false;
            }

            // Leave out cells that do not have clear straight paths
            List<GridData.CellData> viableCells = new List<GridData.CellData>();
            foreach (GridData.CellData cell in cells) {
                List<Vector2Int> cellsInPath = CellDistanceLogic.GetCellsInStraightLine(selectorLocation.Value, cell.Location);
                // Only look for blockers in the cells leading up to the target, not the target itself
                if (cellsInPath.SkipLast(1)
                    .Any(c => DoesCellBlock(GridController.GridData.GetCell(c)))) {
                    continue;
                }
                
                GridEntity entityAtCell = locationsWithEntities
                    .FirstOrDefault(l => l.Location == cell.Location)
                    ?.GetTopEntity()?.Entity;
                if (entityAtCell != null) {
                    // If the target has an entity in it, it had better be an enemy
                    if (entityAtCell.Team == selector.Team) {
                        continue;
                    }
                } else if (selector.InaccessibleTiles.Contains(cell.Tile) || selector.SlowTiles.Contains(cell.Tile)) {
                    // Otherwise can not charge to an empty cell if it hinders movement
                    cachedBlockerCells.Add(cell);
                    continue;
                }
                viableCells.Add(cell);
            }

            return viableCells.Select(c => c.Location).ToList();
        }
        
        private List<GridData.CellData> GetViableCellsWithoutConsideringTerrainOrEntities(GridEntity selector) {
            Vector2Int? selectorLocation = selector.Location;
            if (selectorLocation == null) return new List<GridData.CellData>();

            // Get all cells in range
            List<GridData.CellData> cells = GridController.GridData.GetCellsInRange(selectorLocation.Value, ChargeRange);
            
            // Leave out the current cell
            cells = cells.Where(c => c.Location != selector.Location).ToList();
            
            // Leave out any cells that are not in a straight line from the selector
            return cells.Where(c => CellDistanceLogic.AreCellsInStraightLine(selectorLocation.Value, c.Location)).ToList();
        }

        private bool CanChargeToCell(GridEntity selector, Vector2Int cell) {
            // TODO if we move this viability calculation logic to a business logic class with a more proper cache, then we 
            // can break GetViableTargets up a bit and allow us to forego checking every single cell in range. That would 
            // be nice. 
            return GetViableTargets(selector).Contains(cell);
        }
    }
}