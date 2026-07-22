using System.Linq;
using Gameplay.Config;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles setting up and emitting particles from <see cref="ParticleSystem"/>s overlayed on the map
    /// </summary>
    public class WorldParticlesManager : MonoBehaviour {
        [Header("References")]
        [SerializeField] private ParticleSystem _amberForgeOrangeParticles;

        [Header("Adjustments")] 
        [SerializeField] private float _particleTransformShiftPerHorizontalHex = .433f;
        [SerializeField] private int _particleShapeRadiusPerVerticalHex = 40;
        [SerializeField] private int _particleShapeRadiusAddition = 80;
        [SerializeField] private float _particleRateOverTimePerVerticalHex = 3.5f;

        private MapData _mapData;
        
        public void Initialize(MapData mapData) {
            _mapData = mapData;
            SetUpParticles(mapData);
        }
        
        #if UNITY_EDITOR
        [Button]
        public void ReInitialize() {
            if (_mapData == null) return;
            Initialize(_mapData);
        }
        #endif

        private void SetUpParticles(MapData mapData) {
            int minX = mapData.cells.Min(c => c.location.x);
            int maxX = mapData.cells.Max(c => c.location.x);
            int minY = mapData.cells.Min(c => c.location.y);
            int maxY = mapData.cells.Max(c => c.location.y);
            int horizontalHexCount = maxX - minX;
            int verticalHexCount = maxY - minY;
            
            // Adjust parent transform
            Vector2 upperRightWorldPos = GameManager.Instance.GridController.GetWorldPosition(new Vector2Int(maxX, maxY));
            Vector2 lowerLeftWorldPos = GameManager.Instance.GridController.GetWorldPosition(new Vector2Int(minX, minY));
            transform.localPosition = new Vector3((upperRightWorldPos.x + lowerLeftWorldPos.x) / 2f, (upperRightWorldPos.y + lowerLeftWorldPos.y) / 2f, 0);
            
            // Set position to right off the right side of the map
            Vector3 localPosition = _amberForgeOrangeParticles.transform.localPosition;
            localPosition.x = _particleTransformShiftPerHorizontalHex * horizontalHexCount;
            _amberForgeOrangeParticles.transform.localPosition = localPosition;
            
            // Set the shape radius and emission rate to scale with vertical map size
            ParticleSystem.ShapeModule shape = _amberForgeOrangeParticles.shape;
            shape.radius = _particleShapeRadiusPerVerticalHex * verticalHexCount + _particleShapeRadiusAddition;
            ParticleSystem.EmissionModule emission = _amberForgeOrangeParticles.emission;
            emission.rateOverTime = _particleRateOverTimePerVerticalHex * verticalHexCount;
        }
    }
}