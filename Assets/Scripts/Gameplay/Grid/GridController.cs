using System;
using Gameplay.Entities;
using UnityEngine;
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
    
    void Update() {
        Vector2Int mousePos = GetMousePosition();
        
        // If I ever want to do anything with mousing over - a selecting reticule, simulating attacking an enemy, etc
        // if (!mousePos.Equals(_previousMousePos)) {
        //     _interactiveTilemap.SetTile(_previousMousePos, null);    // Remove old hovertile
        //     _interactiveTilemap.SetTile(mousePos, _hoverTile);
        //     _previousMousePos = mousePos;
        // }

        MouseClick click = MouseClick.None;
        if (Input.GetMouseButtonUp(0)) {
            // Left mouse button click
            Debug.Log("Click on grid at " + mousePos);
            click = MouseClick.Left;
        } else if (Input.GetMouseButtonUp(1)) {
            Debug.Log("Right click at " + mousePos);
            click = MouseClick.Right;
        } else if (Input.GetMouseButtonUp(2)) {
            Debug.Log("Middle mouse click at " + mousePos);
            click = MouseClick.Middle;
        }

        TryClickOnCell(click, mousePos);
    }

    private void TryClickOnCell(MouseClick clickType, Vector2Int clickPosition) {
        GridEntity selectedEntity = GameManager.Instance.SelectionInterface.SelectedEntity;
        GridEntity entityAtMouseLocation = GameManager.Instance.GetEntityAtLocation(clickPosition);
        
        // Always do the "Mouse hovering over" action
        // TODO mouse hovering over action. Depends on which unit is there, what the player is selecting, if they can move there, etc

        switch (clickType) {
            case MouseClick.Left:
                if (entityAtMouseLocation != null) {
                    entityAtMouseLocation.Select();
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

    public Vector2Int GetCellPosition(Vector2 worldPosition) {
        Vector2Int cellPosition = (Vector2Int) _grid.WorldToCell(worldPosition);
        return cellPosition;
    }

    public Vector2 GetWorldPosition(Vector2Int cellPosition) {
        return _grid.CellToWorld((Vector3Int) cellPosition);
    }

    private Vector2Int GetMousePosition() {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return GetCellPosition(mouseWorldPosition);
    }
}
