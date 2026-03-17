using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configuration object for map preview images. Kept separate from <see cref="MapData"/> so that can be json-ified. 
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/MapImagesConfiguration", fileName = "MapImagesConfiguration", order = 0)]
    public class MapImagesConfiguration : ScriptableObject {
        [Serializable]
        public class MapPreviewImage {
            public string MapID;
            public Sprite PreviewImage;
        }
        
        [SerializeField] private List<MapPreviewImage> _mapPreviewImages;
        
        public Sprite GetMapPreviewImage(string mapID) {
            return _mapPreviewImages.FirstOrDefault(m => m.MapID == mapID)?.PreviewImage;
        }
    }
}