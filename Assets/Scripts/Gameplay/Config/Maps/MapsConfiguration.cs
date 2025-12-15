using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Stores the maps 
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/MapsConfiguration", fileName = "MapsConfiguration", order = 0)]
    public class MapsConfiguration : ScriptableObject {
        public List<MapData> PlayableMaps;
        public List<MapData> PreviewMaps;
        public MapData this[string mapID] => PlayableMaps.Concat(PreviewMaps).FirstOrDefault(m => m.mapID == mapID);

        public void RemoveMap(string mapID) {
            PlayableMaps.RemoveAll(m => m.mapID == mapID);
            PreviewMaps.RemoveAll(m => m.mapID == mapID);
        }
    }
}