using System;
using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Entities;
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

    public List<StartingEntitySet> UnitSpawns;
    
    public void LoadMap() {
        // Nothing to do yet, heehee
    }
}