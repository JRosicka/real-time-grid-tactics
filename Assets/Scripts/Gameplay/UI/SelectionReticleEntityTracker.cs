using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Handles moving a <see cref="SelectionReticle"/> around to different <see cref="GridEntity"/>s and following
    /// their movements.
    /// </summary>
    public class SelectionReticleEntityTracker {
        private SelectionReticle _reticle;
        private GridEntity _entity;
        private Vector2Int? _lastTrackedLocation;
        private bool _targeted;

        public void Initialize(SelectionReticle reticle, bool targeted) {
            _reticle = reticle;
            _targeted = targeted;
        }

        public void TrackEntity(GridEntity entity) {
            // Unregister previous entity events
            if (_entity) {
                _entity.AbilityPerformedEvent -= OnAbilityPerformed;
                _entity.UnregisteredEvent -= OnEntityUnregistered;
                if (_targeted) {
                    _entity.DisplayTargeted(false);
                }
            }

            _entity = entity;
            // Register new entity events
            if (_entity) {
                _entity.AbilityPerformedEvent += OnAbilityPerformed;
                _entity.UnregisteredEvent += OnEntityUnregistered;
                if (_targeted) {
                    _entity.DisplayTargeted(true);
                }
            }
            UpdateReticle(entity);
        }

        /// <summary>
        /// Move the reticle to the location of the tracked entity (or hide if it does not exist or is not interactable)
        /// </summary>
        private void UpdateReticle(Vector2Int newLocation) {
            if (_entity == null || !_entity.Interactable) {
                _reticle.Hide();
            } else {
                _reticle.SelectTile(newLocation, _entity); 
            }
            _lastTrackedLocation = newLocation;
        }

        private void UpdateReticle(GridEntity gridEntity) {
            Vector2Int? entityLocation = gridEntity == null ? null : gridEntity.Location;
            if (entityLocation == null) {
                _reticle.Hide();
                _lastTrackedLocation = null;
                return;
            }
            UpdateReticle(entityLocation.Value);
        }

        private void OnAbilityPerformed(IAbility ability, AbilityCooldownTimer timer) {
            // Special move ability handling
            if (ability is MoveAbility moveAbility) {
                UpdateReticle(moveAbility.AbilityParameters.NextMoveCell);
                return;
            }

            if (_entity != null && _entity.Location != null && _entity.Location != _lastTrackedLocation) {
                UpdateReticle(_entity.Location.Value);
            }
        }

        private void OnEntityUnregistered() {
            UpdateReticle(_entity);
        }
    }
}