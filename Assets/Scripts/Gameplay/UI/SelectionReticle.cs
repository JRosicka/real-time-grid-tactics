using System.Collections.Generic;
using Gameplay.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Exists to indicate which tile is currently being moused over
    /// </summary>
    public class SelectionReticle : MonoBehaviour {
        private static readonly Vector2Int DefaultLocation = new Vector2Int(10000, 10000);

        public Color AllySelectionColor;
        public Color EnemySelectionColor;
        public Color NeutralSelectionColor;
        
        public List<Image> ReticleComponents;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private bool _listenForEntityUpdates = true;
        
        private Vector2Int _currentLocation;
        private bool _hidden;
        
        private void Start() {
            if (_listenForEntityUpdates) {
                GridEntityCollection.EntityUpdatedEvent += OnTileUpdated;
            }
        }

        public void SelectTile(Vector2Int location, GridEntity entityAtLocation) {
            if (!GameManager.Instance.GameSetupManager.GameInitialized) return;
            
            _hidden = false; 
            _canvasGroup.alpha = 1;

            // Set position
            _currentLocation = location;
            transform.position = GameManager.Instance.GridController.GetWorldPosition(location);

            UpdateColor(entityAtLocation);
        }

        public void Hide() {
            _hidden = true;
            _canvasGroup.alpha = 0;
            // Set the location to be very far away
            _currentLocation = DefaultLocation;
        }

        private void UpdateColor(GridEntity entityAtLocation) {
            if (!GameManager.Instance.GameSetupManager.GameInitialized) return;

            GameTeam localTeam = GameManager.Instance.LocalTeam;
            Color selectionColor = NeutralSelectionColor;
            if (entityAtLocation && entityAtLocation.Team == localTeam) {
                selectionColor = AllySelectionColor;
            } else if (entityAtLocation && entityAtLocation.Team != GameTeam.Neutral
                                        && entityAtLocation.Team != localTeam) {
                selectionColor = EnemySelectionColor;
            }
            ReticleComponents.ForEach(r => r.color = selectionColor);
        }

        private void OnTileUpdated(Vector2Int location, GridEntity entity) {
            if (!this || _hidden) return;
            // We only care about the current location getting updated
            if (_currentLocation != location) return;
            
            UpdateColor(entity);
        }
    }
}