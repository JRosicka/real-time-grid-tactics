using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Stores the maps 
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/MapsConfiguration", fileName = "MapsConfiguration", order = 0)]
    public class MapsConfiguration : ScriptableObject {
        public List<MapData> PlayableMaps;
        public List<MapData> PreviewMaps;
        public List<ReplayData> PreviewReplays;
        
        public MapData this[string mapID] => PlayableMaps.Concat(PreviewMaps).FirstOrDefault(m => m.mapID == mapID);

        public void RemoveMap(string mapID) {
            PlayableMaps.RemoveAll(m => m.mapID == mapID);
            PreviewMaps.RemoveAll(m => m.mapID == mapID);
        }
        
        public ReplayData GetReplay(string replayID) {
            return PreviewReplays.FirstOrDefault(r => r.replayID == replayID);
        }
        
        public void AddReplay(ReplayData replayData) {
            ReplayData existingReplay = GetReplay(replayData.replayID);
            if (existingReplay != null) {
                PreviewReplays.Remove(existingReplay);
            }
            PreviewReplays.Add(replayData);
        }
    }
}