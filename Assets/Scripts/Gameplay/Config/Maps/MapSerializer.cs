using System;
using System.Collections.Generic;
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
#if UNITY_EDITOR
                    // If in the editor, then load the file directly since GameConfigurationLocator only operates in play mode
                    string[] guids = AssetDatabase.FindAssets("t:MapsConfiguration");
                    switch (guids.Length) {
                        case 0:
                            throw new Exception("No maps configuration found");
                        case > 1:
                            throw new Exception("More than one maps configuration found");
                    }

                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _mapsConfiguration = AssetDatabase.LoadAssetAtPath<MapsConfiguration>(assetPath);

                    if (_mapsConfiguration == null) {
                        throw new Exception($"Failed to find cheat configuration: {assetPath}");
                    }
#else
                    _mapsConfiguration = GameConfigurationLocator.GameConfiguration.MapsConfiguration;
#endif
                }
            
                return _mapsConfiguration;
            }
        }

        /// <summary>
        /// Saves a map in the static maps configuration. If a map exists with the same ID, overwrites that one. 
        /// </summary>
        public static void SaveMap(string mapID, string displayName, string description, int index, 
                            Vector2Int lowerLeftCell, Vector2Int upperRightCell, List<MapData.Cell> cells, 
                            List<MapData.Entity> neutralEntities, List<MapData.Entity> player1Entities, 
                            List<MapData.Entity> player2Entities, MapType mapType) {
            MapData newMapData = new MapData {
                mapID = mapID,
                displayName = displayName,
                description = description,
                index = index,
                lowerLeftCell = lowerLeftCell,
                upperRightCell = upperRightCell,
                cells = cells,
                neutralEntities = neutralEntities,
                player1Entities = player1Entities,
                player2Entities = player2Entities,
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
        }

        [CanBeNull]
        public static MapData GetMap(string mapID) {
            return MapsConfiguration[mapID];
        }

        public static List<MapData> GetAllMaps(MapType mapType) {
            return mapType switch {
                MapType.Playable => MapsConfiguration.PlayableMaps,
                MapType.Preview => MapsConfiguration.PreviewMaps,
                _ => throw new ArgumentOutOfRangeException(nameof(mapType), mapType,
                    "Tried to load maps for an unknown map type")
            };
        }
    }
}