using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable InconsistentNaming     If we ever serialize this to JSON, then it will be convenient for the fields to be lowercase

namespace Gameplay.Config {
    /// <summary>
    /// Configuration for a particular map
    /// </summary>
    [Serializable]
    public class MapData {
        [Header("Info")]
        public string mapID;
        public MapType mapType;
        public string displayName;
        public string description;
        public int index;

        [Header("Config")]
        public Vector2Int lowerLeftCell;
        public Vector2Int upperRightCell;
        public bool wideLeftSide;
        public bool wideRightSide;
        public List<Cell> cells;
        public List<StartingEntitySet> entities;

        [Serializable]
        public class Cell {
            public Vector2Int location;
            public string cellType;
        }
        
        [Serializable]
        public class Entity {
            public Vector2Int location;
            public string entityType;
        }
    }
}