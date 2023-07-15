using System.Collections.Generic;
using Gameplay.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Exists to indicate which tile is currently being moused over
    /// </summary>
    public class SelectionReticle : MonoBehaviour {
        public Color AllySelectionColor;
        public Color EnemySelectionColor;
        public Color NeutralSelectionColor;
        
        public List<Image> ReticleComponents;
        [SerializeField] private CanvasGroup _canvasGroup;
        
        private Vector2Int _currentLocation;
        private bool _hidden;
        
        private void Start() {
            GridEntityCollection.EntityUpdatedEvent += OnTileUpdated;
        }

        public void SelectTile(Vector2Int location, GridEntity entityAtLocation) {
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
            _currentLocation = new Vector2Int(10000, 10000);
        }

        private void UpdateColor(GridEntity entityAtLocation) {
            GridEntity.Team localTeam = GameManager.Instance.LocalPlayer.Data.Team;
            Color selectionColor = NeutralSelectionColor;
            if (entityAtLocation && entityAtLocation.MyTeam == localTeam) {
                selectionColor = AllySelectionColor;
            } else if (entityAtLocation && entityAtLocation.MyTeam != GridEntity.Team.Neutral
                                        && entityAtLocation.MyTeam != localTeam) {
                selectionColor = EnemySelectionColor;
            }
            ReticleComponents.ForEach(r => r.color = selectionColor);
        }

        private void OnTileUpdated(Vector2Int location, GridEntity entity) {
            if (_hidden) return;
            // We only care about the current location getting updated
            if (_currentLocation != location) return;
            
            UpdateColor(entity);
        }
    }
}