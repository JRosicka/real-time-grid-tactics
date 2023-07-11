using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles input for interacting with the grid and the tilemaps that live on it
/// </summary>
public class GridController : MonoBehaviour {
    [SerializeField] private Grid _grid;
    [SerializeField] private Tilemap _gameplayTilemap;

    // The overlay tilemap for highlighting particular tiles
    [SerializeField] private Tilemap _overlayTilemap;
    [SerializeField] private Tile _inaccessibleTile;
    [SerializeField] private Tile _slowMovementTile;
    
    // Selection reticle stuff
    [SerializeField] private SelectionReticle _reticle;
    private Vector2Int _previousMousePos = new Vector2Int();

    private enum MouseClick {
        None = -1,
        Left = 0,
        Middle = 1,
        Right = 2
    }

    private ITargetableAbilityData _selectedTargetableAbility;
    /// <summary>
    /// Arbitrary data passed along when an <see cref="ITargetableAbilityData"/> is selected
    /// </summary>
    private System.Object _targetData;
    public void SelectTargetableAbility(ITargetableAbilityData abilityData, System.Object data) {
        _selectedTargetableAbility = abilityData;
        _targetData = data;
    }

    public void ProcessClick(PointerEventData eventData) {
        ProcessMouseMove(eventData);
        Vector2Int mousePos = GetCellPosition(eventData.pointerPressRaycast.worldPosition);
        
        MouseClick click = MouseClick.None;
        if (eventData.button == PointerEventData.InputButton.Left) {
            // Left mouse button click
            Debug.Log("Click on grid at " + mousePos);
            click = MouseClick.Left;
        } else if (eventData.button == PointerEventData.InputButton.Right) {
            Debug.Log("Right click at " + mousePos);
            click = MouseClick.Right;
        } else if (eventData.button == PointerEventData.InputButton.Middle) {
            Debug.Log("Middle mouse click at " + mousePos);
            click = MouseClick.Middle;
        }

        TryClickOnCell(click, mousePos);
    }

    public void ProcessMouseMove(PointerEventData eventData) {
        // TODO I probably don't need this anymore, BUT I will want it for highlighting tiles based on whether a selected unit can go there
        // If I ever want to do anything with mousing over - particularly with showing how tiles affect movement
        // if (!mousePos.Equals(_previousMousePos)) {
        //     _interactiveTilemap.SetTile(_previousMousePos, null);    // Remove old hovertile
        //     _interactiveTilemap.SetTile(mousePos, _hoverTile);
        //     _previousMousePos = mousePos;
        // }

        Vector2Int mousePos = GetCellPosition(eventData.pointerCurrentRaycast.worldPosition);
        if (mousePos == _previousMousePos) return;

        _previousMousePos = mousePos;
        _reticle.SelectTile(mousePos, GameManager.Instance.GetTopEntityAtLocation(mousePos));
    }

    public void ProcessMouseExit(PointerEventData eventData) {
        _reticle.Hide();
    }

    private void TryClickOnCell(MouseClick clickType, Vector2Int clickPosition) {
        GridEntity selectedEntity = GameManager.Instance.SelectionInterface.SelectedEntity;
        
        switch (clickType) {
            case MouseClick.Left:
                // See if we have a targetable ability we want to use. If so, use it.
                if (_selectedTargetableAbility != null) {
                    if (_selectedTargetableAbility.CanTargetCell(clickPosition, selectedEntity, GameManager.Instance.LocalPlayer.Data.Team, _targetData)) {
                        _selectedTargetableAbility.DoTargetableAbility(clickPosition, selectedEntity, GameManager.Instance.LocalPlayer.Data.Team, _targetData);
                        GameManager.Instance.SelectionInterface.DeselectActiveAbility();
                        return;
                    } else {
                        // We clicked on a cell that the ability cannot be used on. Deselect the ability and click on the cell normally. 
                        GameManager.Instance.SelectionInterface.DeselectActiveAbility();
                    }
                } 
                
                GridEntityCollection.PositionedGridEntityCollection entitiesAtLocation = GameManager.Instance.GetEntitiesAtLocation(clickPosition);
                if (entitiesAtLocation != null) {
                    // Select the top entity, or the next entity if we are already selecting an entity at this location
                    GridEntityCollection.OrderedGridEntity orderedEntity = entitiesAtLocation.Entities.FirstOrDefault(c => c.Entity == selectedEntity);
                    if (orderedEntity == null) {
                        entitiesAtLocation.GetTopEntity().Entity.Select();
                    } else {
                        entitiesAtLocation.GetEntityAfter(orderedEntity).Entity.Select();
                    }
                } else {
                    // We clicked on an empty cell - deselect whatever we selected previously
                    GameManager.Instance.SelectionInterface.SelectEntity(null);
                }
                break;
            case MouseClick.Middle:
                // Don't do anything with this
                break;
            case MouseClick.Right:
                if (selectedEntity == null)
                    break;
                
                selectedEntity.InteractWithCell(clickPosition);
                break;
            case MouseClick.None:
                // We have already done the hover action, so nothing else to do
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(clickType), clickType, null);
        }
    }

    public bool CanEntityEnterCell(Vector2Int cellPosition, EntityData entityData, GridEntity.Team entityTeam, List<GridEntity> entitiesToIgnore = null) {
        entitiesToIgnore ??= new List<GridEntity>();
        List<GridEntity> entitiesAtLocation = GameManager.Instance.GetEntitiesAtLocation(cellPosition)?.Entities
            .Select(o => o.Entity).ToList();
        if (entitiesAtLocation == null) {
            // No other entities are here
            // TODO Check to see if tile type allows for this entity
            return true;
        }
        if (entitiesAtLocation.Any(e => e.MyTeam != entityTeam && e.MyTeam != GridEntity.Team.Neutral)) {
            // There are enemies here
            return false;
        }
        if (entitiesAtLocation.Any(e => !e.EntityData.FriendlyUnitsCanShareCell && !entitiesToIgnore.Contains(e))) {
            // Can only enter a friendly entity's cell if they are specifically configured to allow for that
            // or if we are set to ignore that entity.
            // Note that this means that structures can not be built on cells that contain units! This is intentional. 
            return false;
        }
        // So the only entities here do indeed allow for non-structures to share space with them. Still need to check if this is a structure. Can't put a structure on a structure!
        if (entityData.IsStructure && entitiesAtLocation.Any(e => e.EntityData.IsStructure)) {
            return false;
        }
        
        // TODO Check to see if tile type allows for this entity

        return true;
    }

    public Vector2 GetWorldPosition(Vector2Int cellPosition) {
        return _grid.CellToWorld((Vector3Int) cellPosition);
    }
    
    private Vector2Int GetCellPosition(Vector2 worldPosition) {
        Vector2Int cellPosition = (Vector2Int) _grid.WorldToCell(worldPosition);
        return cellPosition;
    }
}
