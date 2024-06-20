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

    private GridController GridController => GameManager.Instance.GridController;
    
    public void LoadMap(GridEntity.Team team) {
        Vector2 lowerLeftWorldPosition = GridController.GetWorldPosition(LowerLeftCell);
        Vector2 upperRightWorldPosition = GridController.GetWorldPosition(UpperRightCell);
        CameraManager.SetBoundaries(lowerLeftWorldPosition.x, upperRightWorldPosition.x, upperRightWorldPosition.y, lowerLeftWorldPosition.y);
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