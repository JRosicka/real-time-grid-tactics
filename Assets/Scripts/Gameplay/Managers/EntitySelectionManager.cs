using System;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Grid;
using Gameplay.UI;
using UnityEngine;

/// <summary>
/// Handles selecting and keeping track of entities and abilities
/// </summary>
public class EntitySelectionManager {
    /// <summary>
    /// The central location where we store which entity is selected
    /// </summary>
    public GridEntity SelectedEntity { get; private set; }
    public event Action SelectedEntityMoved;
    private Vector2Int? _selectedEntityCurrentLocation;
    
    private ITargetableAbilityData _selectedTargetableAbility;
    /// <summary>
    /// Arbitrary data passed along when an <see cref="ITargetableAbilityData"/> is selected
    /// </summary>
    private System.Object _targetData;

    private readonly GameManager _gameManager;
    private SelectionInterface SelectionInterface => _gameManager.SelectionInterface;
    private GridController GridController => _gameManager.GridController;
    private PathfinderService PathfinderService => _gameManager.PathfinderService;

    public EntitySelectionManager(GameManager gameManager) {
        _gameManager = gameManager;
        if (_gameManager.GameSetupManager.GameInitialized) {
            RegisterEvents();
        } else {
            gameManager.GameSetupManager.GameInitializedEvent += RegisterEvents;
        }
    }

    private void RegisterEvents() {
        _gameManager.CommandManager.EntityCollectionChangedEvent += EntityCollectionChanged;
    }

    #region Entity Selection
    
    public void SelectEntity(GridEntity entity) {
        if (SelectedEntity != null) {
            // Unregister the un-registration event for the previously selected entity
            SelectedEntity.TargetLocationLogic.ValueChanged -= TryFindPath;
            SelectedEntity.UnregisteredEvent -= DeselectEntity;
        }
        GridController.UnTrackEntity(SelectedEntity);
        
        SelectedEntity = entity;
        _selectedEntityCurrentLocation = entity == null ? null : entity.Location;
        SelectionInterface.UpdateSelectedEntity(entity);
        GridController.TrackEntity(entity);

        if (entity != null) {
            TryFindPath(null, entity.TargetLocationLogic.Value, null);
            entity.TargetLocationLogic.ValueChanged += TryFindPath;
            entity.UnregisteredEvent += DeselectEntity;
        } else {
            GridController.ClearPath();
        }
    }

    /// <summary>
    /// Select whatever entity is at the given cell, otherwise select the cell itself. Cycle through the multiple
    /// choices with subsequent calls.
    /// </summary>
    public void SelectCell(Vector2Int cell) {
        GridEntity lastSelectedEntity = SelectedEntity;
        DeselectEntity();

        GridEntityCollection.PositionedGridEntityCollection entitiesAtLocation = _gameManager.GetEntitiesAtLocation(cell);
        if (entitiesAtLocation == null) {
            // There are no entities at this location. Select the cell itself
            SelectCellTerrain(cell);
        } else {
            // Select the top entity, or the next entity if we are already selecting an entity at this location
            GridEntityCollection.OrderedGridEntity orderedEntity = entitiesAtLocation.Entities.FirstOrDefault(c => c.Entity == lastSelectedEntity);
            if (orderedEntity == null) {
                entitiesAtLocation.GetTopEntity().Entity.Select();
            } else {
                GridEntity nextEntity = entitiesAtLocation.GetEntityAfter(orderedEntity)?.Entity;
                if (nextEntity != null) {
                    nextEntity.Select();
                } else if (!SelectCellTerrain(cell)) {  // There is no next entity. Try selecting the cell itself.
                    // The cell is not selectable. Loop back around and select the first entity in the stack. 
                    entitiesAtLocation.GetTopEntity().Entity.Select();
                }
            }
        }
    }

    /// <summary>
    /// Try selecting the cell
    /// </summary>
    /// <returns>True if the cell was selected, otherwise false if it is not a cell that can be selected</returns>
    private bool SelectCellTerrain(Vector2Int cell) {
        GameplayTile tile = GridController.GridData.GetCell(cell).Tile;
        
        if (!tile.Selectable) return false;
        
        SelectionInterface.UpdateSelectedTerrain(tile);
        return true;
    }

    public void TryInteractWithCell(Vector2Int cell) {
        if (SelectedEntity == null) return;
        SelectedEntity.InteractWithCell(cell);
    }

    private void DeselectEntity() {
        if (SelectedEntity == null) return;
        SelectEntity(null);
    }

    private void EntityCollectionChanged() {
        UpdateSelectedEntity();
        if (_selectedTargetableAbility != null && SelectedEntity != null) {
            // TODO I don't like that we do this every time an entity spawns/despawns/moves. That's a lot of calculating,
            // especially for expensive calculations like the charge ability selection. 
            _selectedTargetableAbility.RecalculateTargetableAbilitySelection(SelectedEntity, _targetData);
        }
    }

    private void UpdateSelectedEntity() {
        if (SelectedEntity == null) return;
        if (!_gameManager.CommandManager.EntitiesOnGrid.IsEntityOnGrid(SelectedEntity)) return;
        // Update the path if necessary
        TryFindPath(null, SelectedEntity.TargetLocationLogic.Value, null);

        // Update the location if necessary
        if (SelectedEntity.Location == _selectedEntityCurrentLocation) return; 
        _selectedEntityCurrentLocation = SelectedEntity.Location;
        SelectedEntityMoved?.Invoke();
    }

    #endregion
    
    #region Targetable Abilities
    
    public void SelectTargetableAbility(ITargetableAbilityData abilityData, GameTeam selectorTeam, object data) {
        _selectedTargetableAbility = abilityData;
        _targetData = data;
        GridController.SetTargetedIcon(abilityData.CreateIconForTargetedCell(selectorTeam, data));
    }

    /// <returns>True if there was actually a selected targetable ability that gets canceled, otherwise false</returns>
    public bool DeselectTargetableAbility() {
        bool targetableAbilityWasSelected = _selectedTargetableAbility != null;
        _selectedTargetableAbility = null;
        _targetData = null;
        GridController.ClearTargetedIcon();
        SelectionInterface.DeselectActiveAbility();
        ClearSelectableTiles();
        return targetableAbilityWasSelected;
    }

    public bool IsTargetableAbilitySelected() {
        return _selectedTargetableAbility != null;
    }

    private void ClearSelectableTiles() {
        GridController.UpdateSelectableCells(null, SelectedEntity);
    }

    /// <summary>
    /// Use the selected targetable ability if we have one and can use it at the selected cell.
    /// <returns>True if an ability was successfully used, otherwise false.</returns>
    /// </summary>
    public bool TryUseTargetableAbility(Vector2Int clickedCell) {
        if (_selectedTargetableAbility == null) return false;

        if (!_selectedTargetableAbility.CanTargetCell(clickedCell, SelectedEntity, _gameManager.LocalTeam, _targetData)) {
            // We clicked on a cell that the ability cannot be used on. Deselect the ability. 
            DeselectTargetableAbility();
            return false;
        }

        _gameManager.CommandManager.ClearAbilityQueue(SelectedEntity);

        // TODO maybe move everything under here in this method to some other class, if things get more complicated
        if (_selectedTargetableAbility.MoveToTargetCellFirst && SelectedEntity.Location != clickedCell) {
            // We need to move to the clicked cell first
            // TODO if an ability has a range, like an attack, find a path to the nearest place in range. We only want to 
            // refrain from doing the targetable ability if it actually matters if the entity can move to the target. Not 
            // sure how best to handle that. 
            if (!SelectedEntity.TryMoveToCell(clickedCell, true)) {
                // We failed to move to the destination, so don't do the targetable ability
                return false;
            }
        }

        // This targetable ability will get queued
        _selectedTargetableAbility.DoTargetableAbility(clickedCell, SelectedEntity, _gameManager.LocalTeam, _targetData);
        DeselectTargetableAbility();
        return true;
    }
    
    #endregion

    private void TryFindPath(INetworkableFieldValue oldValue, INetworkableFieldValue newValue, object metadata) {
        if (SelectedEntity == null) return;
        if (!_gameManager.CommandManager.EntitiesOnGrid.IsEntityOnGrid(SelectedEntity)) return; // May be in the middle of getting unregistered
        if (!SelectedEntity.CanMoveOrRally && !SelectedEntity.TargetLocationLogicValue.CanRally) return;
        if (SelectedEntity.InteractBehavior is not { AllowedToSeeTargetLocation: true }) return;

        TargetLocationLogic targetLocationLogic = (TargetLocationLogic)newValue;
        PathfinderService.Path path = PathfinderService.FindPath(SelectedEntity, targetLocationLogic.CurrentTarget);
        GridController.VisualizePath(path, targetLocationLogic.Attacking, targetLocationLogic.HidePathDestination, false);
    }
}