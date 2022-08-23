using System.Collections;
using System.Collections.Generic;
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
    
    public GridEntity SelectedEntity;    // TODO move this to some manager
    
    void Update() {
        Vector3Int mousePos = GetMousePosition();
        
        // If I ever want to do anything with mousing over - a selecting reticule, simulating attacking an enemy, etc
        // if (!mousePos.Equals(_previousMousePos)) {
        //     _interactiveTilemap.SetTile(_previousMousePos, null);    // Remove old hovertile
        //     _interactiveTilemap.SetTile(mousePos, _hoverTile);
        //     _previousMousePos = mousePos;
        // }

        if (Input.GetMouseButtonUp(0)) {
            // Left mouse button click
            Debug.Log("Click on grid at " + mousePos);
        }
    }

    public Vector3Int GetCellPosition(Vector3 worldPosition) {
        Vector3Int cellPosition = _grid.WorldToCell(worldPosition);
        cellPosition.z = 0;
        return cellPosition;
    }

    public Vector3 GetWorldPosition(Vector3Int cellPosition) {
        return _grid.CellToWorld(cellPosition);
    }

    private Vector3Int GetMousePosition() {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return GetCellPosition(mouseWorldPosition);
    }
}
