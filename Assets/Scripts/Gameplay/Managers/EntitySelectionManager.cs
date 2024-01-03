using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Grid;
using Gameplay.Pathfinding;
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
            SelectedEntity.UnregisteredEvent -= DeselectEntity;
        }
        
        SelectedEntity = entity;
        _selectedEntityCurrentLocation = entity == null ? (Vector2Int?) null : entity.Location;
        SelectionInterface.UpdateSelectedEntity(entity);
        GridController.TrackEntity(entity);

        if (entity != null) {
            entity.UnregisteredEvent += DeselectEntity;
        }
    }

    /// <summary>
    /// Select whatever entity is at the given cell, if any. Cycle through the multiple entities on the cell with
    /// subsequent calls.
    /// </summary>
    public void SelectEntityAtCell(Vector2Int cell) {
        GridEntityCollection.PositionedGridEntityCollection entitiesAtLocation = _gameManager.GetEntitiesAtLocation(cell);
        if (entitiesAtLocation != null) {
            // Select the top entity, or the next entity if we are already selecting an entity at this location
            GridEntityCollection.OrderedGridEntity orderedEntity = entitiesAtLocation.Entities.FirstOrDefault(c => c.Entity == SelectedEntity);
            if (orderedEntity == null) {
                entitiesAtLocation.GetTopEntity().Entity.Select();
            } else {
                entitiesAtLocation.GetEntityAfter(orderedEntity).Entity.Select();
            }
        } else {
            // The cell is empty - deselect whatever we selected previously
            DeselectEntity();
        }
    }

    public void TryInteractWithCell(Vector2Int cell) {
        if (SelectedEntity == null) return;
        SelectedEntity.InteractWithCell(cell);
    }

    private void DeselectEntity() {
        if (SelectedEntity == null) return;
        SelectEntity(null);
        GridController.ClearPath();
    }

    private void EntityCollectionChanged() {
        // We only care about whether the selected entity moved
        if (SelectedEntity == null) return;
        if (!_gameManager.CommandManager.EntitiesOnGrid.AllEntities().Contains(SelectedEntity)) return;
        if (SelectedEntity.Location == _selectedEntityCurrentLocation) return;

        SelectedEntityMoved?.Invoke(); 
    }

    #endregion
    
    #region Targetable Abilities
    
    public void SelectTargetableAbility(ITargetableAbilityData abilityData, object data) {
        _selectedTargetableAbility = abilityData;
        _targetData = data;
    }

    public void DeselectTargetableAbility() {
        _selectedTargetableAbility = null;
        _targetData = null;
        SelectionInterface.DeselectActiveAbility();
    }

    /// <summary>
    /// Use the selected targetable ability if we have one and can use it at the selected cell.
    /// <returns>True if an ability was successfully used, otherwise false.</returns>
    /// </summary>
    public bool TryUseTargetableAbility(Vector2Int clickedCell) {
        if (_selectedTargetableAbility == null) return false;

        if (!_selectedTargetableAbility.CanTargetCell(clickedCell, SelectedEntity, _gameManager.LocalPlayer.Data.Team, _targetData)) {
            // We clicked on a cell that the ability cannot be used on. Deselect the ability. 
            DeselectTargetableAbility();
            return false;
        }
        
        SelectedEntity.ClearAbilityQueue();

        // TODO maybe move everything under here in this method to some other class, if things get more complicated
        if (_selectedTargetableAbility.MoveToTargetCellFirst && SelectedEntity.Location != clickedCell) {
            // We need to move to the clicked cell first
            // TODO if an ability has a range, like an attack, find a path to the nearest place in range. We only want to 
            // refrain from doing the targetable ability if it actually matters if the entity can move to the target. Not 
            // sure how best to handle that. 
            if (!SelectedEntity.TryMoveToCell(clickedCell)) {
                // We failed to move to the destination, so don't do the targetable ability
                return false;
            }
        }

        // This targetable ability will get queued
        _selectedTargetableAbility.DoTargetableAbility(clickedCell, SelectedEntity, _gameManager.LocalPlayer.Data.Team, _targetData);
        DeselectTargetableAbility();
        return true;
    }
    
    #endregion

    public void TryFindPath(Vector2Int cell) {
        if (SelectedEntity == null) return;
        if (!SelectedEntity.CanMove) return;
        if (SelectedEntity.MyTeam != _gameManager.LocalPlayer.Data.Team) return;

        List<GridNode> path = PathfinderService.FindPath(SelectedEntity, cell);
        GridController.VisualizePath(path);
    }
}