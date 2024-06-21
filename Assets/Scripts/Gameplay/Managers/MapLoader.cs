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
    /// <summary>
    /// Represents the set of starting units and their spawn locations for a single player
    /// </summary>
    [Serializable]
    public struct StartingEntitySet {
        public GridEntity.Team Team;
        public List<EntitySpawn> Entities;
    }

    /// <summary>
    /// Represents a single entity and its spawn location
    /// </summary>
    [Serializable]
    public struct EntitySpawn {
        public EntityData Entity;
        public Vector2Int SpawnLocation;
    }

    public CameraManager CameraManager;
    
    [Header("Config")]
    public List<StartingEntitySet> UnitSpawns;
    
    [Header("The farthest enter-able areas in the map, in grid-space")]
    public Vector2Int LowerLeftCell;
    public Vector2Int UpperRightCell;

    [Header("Whether either side should be 'wide' meaning an extra column beyond the corner cell's column")]
    public bool WideLeftSide;
    public bool WideRightSide;

    private GridController GridController => GameManager.Instance.GridController;
    
    public void LoadMap(GridEntity.Team team) {
        Vector2 lowerLeftWorldPosition = GridController.GetWorldPosition(LowerLeftCell);
        Vector2 upperRightWorldPosition = GridController.GetWorldPosition(UpperRightCell);
        
        bool needAdditionalHalfCellAtLeft = WideLeftSide;
        float xMin = lowerLeftWorldPosition.x - (needAdditionalHalfCellAtLeft ? GridController.CellWidth / 2 : 0);
        bool needAdditionalHalfCellAtRight = WideRightSide;
        float xMax = upperRightWorldPosition.x + (needAdditionalHalfCellAtRight ? GridController.CellWidth / 2 : 0);
        CameraManager.SetBoundaries(xMin, xMax, upperRightWorldPosition.y, lowerLeftWorldPosition.y);
        CameraManager.SetCameraStartPosition(GridController.GetWorldPosition(GetHomeBaseLocation(team)));
    }

    private Vector2Int GetHomeBaseLocation(GridEntity.Team team) {
        return UnitSpawns
            .First(u => u.Team == team)
            .Entities
            .First(e => e.Entity.Tags.Contains(EntityData.EntityTag.HomeBase))
            .SpawnLocation;
    }
}