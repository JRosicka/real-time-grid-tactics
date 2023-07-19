using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using UnityEngine;

/// <summary>
/// Handles selecting and keeping track of entities and abilities
/// </summary>
public class EntitySelectionManager {
    /// <summary>
    /// The central location where we store which entity is selected
    /// </summary>
    private GridEntity SelectedEntity { get; set; }
    
    private ITargetableAbilityData _selectedTargetableAbility;
    /// <summary>
    /// Arbitrary data passed along when an <see cref="ITargetableAbilityData"/> is selected
    /// </summary>
    private System.Object _targetData;

    public void SelectEntity(GridEntity entity) {
        if (SelectedEntity != null) {
            // Unregister the un-registration event for the previously selected entity
            SelectedEntity.UnregisteredEvent -= DeselectEntity;
        }
        
        SelectedEntity = entity;
        GameManager.Instance.SelectionInterface.UpdateSelectedEntity(entity);
        GameManager.Instance.GridController.TrackEntity(entity);

        if (entity != null) {
            entity.UnregisteredEvent += DeselectEntity;
        }
    }

    /// <summary>
    /// Select whatever entity is at the given cell, if any. Cycle through the multiple entities on the cell with
    /// subsequent calls.
    /// </summary>
    public void SelectEntityAtCell(Vector2Int cell) {
        GridEntityCollection.PositionedGridEntityCollection entitiesAtLocation = GameManager.Instance.GetEntitiesAtLocation(cell);
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
    }

    #region Targetable Abilities
    
    public void SelectTargetableAbility(ITargetableAbilityData abilityData, object data) {
        _selectedTargetableAbility = abilityData;
        _targetData = data;
    }

    public void DeselectTargetableAbility() {
        _selectedTargetableAbility = null;
        _targetData = null;
        GameManager.Instance.SelectionInterface.DeselectActiveAbility();
    }

    /// <summary>
    /// Use the selected targetable ability if we have one and can use it at the selected cell.
    /// <returns>True if an ability was successfully used, otherwise false.</returns>
    /// </summary>
    public bool TryUseTargetableAbility(Vector2Int clickedCell) {
        if (_selectedTargetableAbility != null) {
            if (_selectedTargetableAbility.CanTargetCell(clickedCell, SelectedEntity, GameManager.Instance.LocalPlayer.Data.Team, _targetData)) {
                _selectedTargetableAbility.DoTargetableAbility(clickedCell, SelectedEntity, GameManager.Instance.LocalPlayer.Data.Team, _targetData);
                DeselectTargetableAbility();
                return true;
            }
        
            // We clicked on a cell that the ability cannot be used on. Deselect the ability. 
            DeselectTargetableAbility();
            return false;
        }

        // No targetable ability selected
        return false;
    }
    
    #endregion
}