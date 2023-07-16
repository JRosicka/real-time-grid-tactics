using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Grid;
using Gameplay.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles input for interacting with the grid and the tilemaps that live on it
/// TODO this class has a lot of different responsibilities now, would be good to extract some of those to new classes
/// </summary>
public class GridController : MonoBehaviour {
    public const float CellWidth = 0.8659766f;
    
    [SerializeField] private Grid _grid;
    [SerializeField] private Tilemap _gameplayTilemap;
    [SerializeField] private PathVisualizer _pathVisualizer;

    // The overlay tilemap for highlighting particular tiles
    private OverlayTilemap _overlayTilemap;
    [SerializeField] private Tilemap _overlayMap;
    [SerializeField] private Tile _inaccessibleTile;
    [SerializeField] private Tile _slowMovementTile;
    
    // Reticle for where the mouse is currently hovering
    [FormerlySerializedAs("_reticle")] 
    [SerializeField] private SelectionReticle _mouseReticle;
    private Vector2Int _previousMousePos = new Vector2Int();
    // Reticle for the selected unit
    [SerializeField] private SelectionReticle _selectedUnitReticle;
    private SelectionReticleEntityTracker _selectedUnitTracker = new SelectionReticleEntityTracker();
    // Reticle for the target unit (The place where the selected unit is moving towards or attacking)
    [SerializeField] private SelectionReticle _targetUnitReticle;    // TODO not currently doing anything with this. Call associated method in this class when we move or attack.
    private SelectionReticleEntityTracker _targetUnitTracker = new SelectionReticleEntityTracker();
    
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

    private GridData _gridData;

    public void Initialize() {
        _pathVisualizer.Initialize();
        _selectedUnitTracker.Initialize(_selectedUnitReticle);
        _targetUnitTracker.Initialize(_targetUnitReticle);
        _gridData = new GridData(_gameplayTilemap);
        _overlayTilemap = new OverlayTilemap(_overlayMap, _gridData, _inaccessibleTile, _slowMovementTile);
    }
    
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
        _mouseReticle.SelectTile(mousePos, GameManager.Instance.GetTopEntityAtLocation(mousePos));
    }

    public void ProcessMouseExit(PointerEventData eventData) {
        _mouseReticle.Hide();
    }

    public void TrackEntity(GridEntity entity) {
        _selectedUnitTracker.TrackEntity(entity);
        _overlayTilemap.SelectEntity(entity);
    }

    public void TargetEntity(GridEntity entity) {
        _targetUnitTracker.TrackEntity(entity);
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
                    _selectedUnitTracker.TrackEntity(null);
                    _overlayTilemap.SelectEntity(null);
                    // TODO combine these all into a method since they are all used in a few places. Maybe repurpose this class into a GridSelectionController or something.
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

    [Button]
    private void VisualizePath1() {
        _pathVisualizer.Visualize(new PathVisualizer.GridPath {
            Cells = new List<Vector2Int> {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
                new Vector2Int(2, 1),
                new Vector2Int(2, 0),
            }
        });
    }
    
    [Button]
    private void VisualizePath2() {
        _pathVisualizer.Visualize(new PathVisualizer.GridPath {
            Cells = new List<Vector2Int> {
                new Vector2Int(-4, -4),
                new Vector2Int(-4, -5),
                new Vector2Int(-3, -5),
            }
        });
    }

    [Button]
    private void VisualizePath3() {
        _pathVisualizer.Visualize(new PathVisualizer.GridPath {
            Cells = new List<Vector2Int> {
                new Vector2Int(0, 4),
                new Vector2Int(0, 3),
                new Vector2Int(-1, 3),
                new Vector2Int(-1, 4),
                new Vector2Int(-1, 5),
                new Vector2Int(0, 5),
                new Vector2Int(1, 5),
                new Vector2Int(2, 4),
                new Vector2Int(2, 3),
                new Vector2Int(2, 2),

            }
        });
    }
}
