using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles input for interacting with the grid and the tilemaps that live on it
/// </summary>
public class GridController : MonoBehaviour {
    [SerializeField] private Grid _grid;
    [SerializeField] private Tilemap _gameplayTilemap;

    // If I ever want to do anything with mousing over - a selecting reticule, simulating attacking an enemy, etc
    // [SerializeField] private Tilemap _interactiveTilemap;
    // [SerializeField] private Tile _hoverTile;
    // private Vector3Int _previousMousePos = new Vector3Int();

    public enum MouseClick {
        None = -1,
        Left = 0,
        Middle = 1,
        Right = 2
    }

    private ITargetableAbilityData _selectedTargetableAbility;
    public void SelectTargetableAbility(ITargetableAbilityData abilityData) {
        _selectedTargetableAbility = abilityData;
    }

    public void ProcessClick(PointerEventData eventData) {
        Vector2Int mousePos = GetMousePosition(eventData);
        
        // If I ever want to do anything with mousing over - a selecting reticule, simulating attacking an enemy, etc
        // if (!mousePos.Equals(_previousMousePos)) {
        //     _interactiveTilemap.SetTile(_previousMousePos, null);    // Remove old hovertile
        //     _interactiveTilemap.SetTile(mousePos, _hoverTile);
        //     _previousMousePos = mousePos;
        // }

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

    private void TryClickOnCell(MouseClick clickType, Vector2Int clickPosition) {
        GridEntity selectedEntity = GameManager.Instance.SelectionInterface.SelectedEntity;
        
        // Always do the "Mouse hovering over" action
        // TODO mouse hovering over action. Depends on which unit is there, what the player is selecting, if they can move there, etc

        switch (clickType) {
            case MouseClick.Left:
                // See if we have a targetable ability we want to use. If so, use it.
                if (_selectedTargetableAbility != null) {
                    if (_selectedTargetableAbility.CanTargetCell(clickPosition, selectedEntity, GameManager.Instance.LocalPlayer.Data.Team)) {
                        _selectedTargetableAbility.DoTargetableAbility(clickPosition, selectedEntity, GameManager.Instance.LocalPlayer.Data.Team);
                        GameManager.Instance.SelectionInterface.DeselectActiveAbility();
                        SelectTargetableAbility(null);
                        return;
                    } else {
                        // We clicked on a cell that the ability cannot be used on. Deselect the ability and click on the cell normally. 
                        GameManager.Instance.SelectionInterface.DeselectActiveAbility();
                        SelectTargetableAbility(null);
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
                SelectTargetableAbility(null);
                break;
            case MouseClick.None:
                // We have already done the hover action, so nothing else to do
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(clickType), clickType, null);
        }
    }

    public bool CanEntityEnterCell(Vector2Int cellPosition, EntityData entityData, GridEntity.Team entityTeam) {
        List<GridEntity> entitiesAtLocation = GameManager.Instance.GetEntitiesAtLocation(cellPosition)?.Entities
            .Select(o => o.Entity).ToList();
        if (entitiesAtLocation == null) {
            // No other entities are here
            return true;
        }
        if (entitiesAtLocation.Any(e => e.MyTeam != entityTeam && e.MyTeam != GridEntity.Team.Neutral)) {
            // There are enemies here
            return false;
        }
        if (entitiesAtLocation.Any(e => !e.EntityData.FriendlyUnitsCanShareCell)) {
            // Can only enter a friendly entity's cell if they are specifically configured to allow for that. 
            // Note that this means that structures can not be built on cells that contain units! This is intentional. 
            return false;
        }
        // So the only entities here do indeed allow for non-structures to share space with them. Still need to check if this is a structure. Can't put a structure on a structure!
        if (entityData.Tags.Contains(EntityData.EntityTag.Structure)) {
            return false;
        }

        return true;
    }

    public Vector2 GetWorldPosition(Vector2Int cellPosition) {
        return _grid.CellToWorld((Vector3Int) cellPosition);
    }

    private Vector2Int GetMousePosition(PointerEventData eventData) {
        Vector3 mouseWorldPosition = eventData.pointerPressRaycast.worldPosition;
        return GetCellPosition(mouseWorldPosition);
    }
    
    private Vector2Int GetCellPosition(Vector2 worldPosition) {
        Vector2Int cellPosition = (Vector2Int) _grid.WorldToCell(worldPosition);
        return cellPosition;
    }
}
