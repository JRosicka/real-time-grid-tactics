using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Grid;
using UnityEngine;

/// <summary>
/// Handles loading the map during game setup. Entirely client-side. 
/// </summary>
public class MapLoader : MonoBehaviour {
    public GridController GridController;
    public CameraManager CameraManager;
    
    
    public string CurrentMapID;

    private MapData _currentMap;
    public Vector2Int LowerLeftCell { get; private set; }
    public Vector2Int UpperRightCell { get; private set; }
    public bool WideLeftSide { get; private set; }
    public bool WideRightSide { get; private set; }
    public List<StartingEntitySet> UnitSpawns { get; private set; }
    
    // TODO Repurpose to camera setup which only gets called in-game (right after loading map)
    public void LoadMapOld(GameTeam team) {
        Vector2 lowerLeftWorldPosition = GridController.GetWorldPosition(LowerLeftCell);
        Vector2 upperRightWorldPosition = GridController.GetWorldPosition(UpperRightCell);
        
        // TODO move all camera stuff to a separate method
        bool needAdditionalHalfCellAtLeft = WideLeftSide;
        float xMin = lowerLeftWorldPosition.x - (needAdditionalHalfCellAtLeft ? GridController.CellWidth / 2 : 0);
        bool needAdditionalHalfCellAtRight = WideRightSide; 
        float xMax = upperRightWorldPosition.x + (needAdditionalHalfCellAtRight ? GridController.CellWidth / 2 : 0);
        CameraManager.Initialize(GridController.GetWorldPosition(GetPlayerStartLocation(team)), 
            xMin, xMax, upperRightWorldPosition.y, lowerLeftWorldPosition.y);
    }

    public void LoadMap(MapData mapData) {
        _currentMap = mapData;
        CurrentMapID = mapData.mapID;
        
        LowerLeftCell = mapData.lowerLeftCell;
        UpperRightCell = mapData.upperRightCell;
        WideLeftSide = mapData.wideLeftSide;
        WideRightSide = mapData.wideRightSide;
        UnitSpawns = mapData.entities;
        
        GridController.LoadMap(mapData);
    }

    public Vector2Int GetPlayerStartLocation(GameTeam team) {
        // If the player is a spectator, just focus them on player 1's home base
        team = team == GameTeam.Spectator ? GameTeam.Player1 : team;
        return UnitSpawns
            .First(u => u.Team == team)
            .Entities
            .First(e => e.Data.Tags.Contains(EntityTag.Leader))
            .SpawnLocation.Location;
    }
}