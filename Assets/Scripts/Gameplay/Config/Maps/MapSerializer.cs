using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Handles saving/loading/retrieving map data to/from <see cref="MapsConfiguration"/>
    /// </summary>
    public static class MapSerializer {
        private static MapsConfiguration _mapsConfiguration;
        private static MapsConfiguration MapsConfiguration {
            get {
                // Load the configuration if not already loaded
                if (_mapsConfiguration == null) {
                    _mapsConfiguration = GameConfigurationLocator.GameConfiguration.MapsConfiguration;
                }
            
                return _mapsConfiguration;
            }
        }

        /// <summary>
        /// Saves a map in the static maps configuration. If a map exists with the same ID, overwrites that one. 
        /// </summary>
        public static void SaveMap(string mapID, MapType mapType, string displayName, string description, int index, 
                            Vector2Int lowerLeftCell, Vector2Int upperRightCell, bool wideLeftSide, bool wideRightSide, 
                            List<MapData.Cell> cells, List<StartingEntitySet> entities) {
            MapData newMapData = new MapData {
                mapID = mapID,
                mapType = mapType,
                displayName = displayName,
                description = description,
                index = index,
                lowerLeftCell = lowerLeftCell,
                upperRightCell = upperRightCell,
                wideLeftSide = wideLeftSide,
                wideRightSide = wideRightSide,
                cells = cells,
                entities = entities,
            };

            MapData currentMapData = MapsConfiguration[mapID];
            if (currentMapData != null) {
                MapsConfiguration.RemoveMap(mapID);
            }

            switch (mapType) {
                case MapType.Playable:
                    MapsConfiguration.PlayableMaps.Add(newMapData);
                    break;
                case MapType.Preview:
                    MapsConfiguration.PreviewMaps.Add(newMapData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapType), mapType, "Tried to save unknown map type");
            }
            
#if UNITY_EDITOR
            EditorUtility.SetDirty(MapsConfiguration);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        [CanBeNull]
        public static MapData GetMap(string mapID) {
            return MapsConfiguration[mapID];
        }

        public static List<MapData> GetMaps(MapType mapType) {
            return mapType switch {
                MapType.Playable => MapsConfiguration.PlayableMaps,
                MapType.Preview => MapsConfiguration.PreviewMaps,
                _ => throw new ArgumentOutOfRangeException(nameof(mapType), mapType,
                    "Tried to load maps for an unknown map type")
            };
        }

        public static List<MapData> GetAllMaps() {
            return GetMaps(MapType.Playable).Concat(GetMaps(MapType.Preview)).ToList();
        }
    }
}