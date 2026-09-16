using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Config.Upgrades;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Entities {
    public class KingView : GridEntityParticularView {
        [SerializeField] private int _minSecondsBetweenUnderAttackAlerts = 30;
        [SerializeField] private ParadeAnimationBehavior _paradeAnimationPrefab;
        
        [SerializeField] private InspiringPresenceUpgradeData _inspiringPresenceUpgrade;
        [SerializeField] private List<InspiringPresencePositionedParticles> _inspiringPresenceParticles;

        private GridEntity _entity;
        private float _timeOfLastDamageReceived;
        private bool _inspiringPresenceActive;
        private List<Vector2Int> _cachedAdjacentAndEntityPositions;
        private Vector2Int? _cachedEntityPosition;

        public override void Initialize(GridEntity entity) {
            _entity = entity;
            SetParticleColors();
        }

        public override void InitializeFoW() {
            ReEvaluateInspiringPresenceFoW();
            _entity.EntityMovedClientEvent += ReEvaluateInspiringPresenceFoW;
            TeamFogOfWarTracker tracker = GameManager.Instance.FogOfWarManager!.GetLocalTeamTracker();
            if (tracker != null) {
                tracker.FoWUpdated += FoWUpdated;
            }
        }

        public override void LethalDamageReceived() {
            DoInspiringPresenceAnimation(false);
        }
        
        public override void NonLethalDamageReceived() {
            if (_entity.Team != GameManager.Instance.LocalTeam) return;
            if (Time.time - _timeOfLastDamageReceived < _minSecondsBetweenUnderAttackAlerts) return;
            
            _timeOfLastDamageReceived = Time.time;
            GameManager.Instance.AlertTextDisplayer.DisplayAlert("Your King is under attack!");
        }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability.AbilityData) {
                case ParadeAbilityData _:
                    DoParadeAnimation();
                    return false;
                default:
                    return true;
            }
        }

        public override void UpgradeApplied(IUpgrade upgrade) {
            if (upgrade.UpgradeData == _inspiringPresenceUpgrade) {
                DoInspiringPresenceAnimation(true);
            }
        }

        private void DoParadeAnimation() {
            ParadeAnimationBehavior animationBehavior = Instantiate(_paradeAnimationPrefab, GameManager.Instance.CommandManager.SpawnBucket);
            animationBehavior.transform.position = GameManager.Instance.GridController.GetWorldPosition(_entity.Location!.Value);
            animationBehavior.Initialize(_entity);
        }

        private void SetParticleColors() {
            PlayerColorData colorData = GameManager.Instance.GetPlayerForTeam(_entity).ColorData;

            foreach (InspiringPresencePositionedParticles particles in _inspiringPresenceParticles) {
                particles.Initialize(_entity, colorData);
            }
        }
        
        private void DoInspiringPresenceAnimation(bool enable) {
            _inspiringPresenceActive = enable;
            _inspiringPresenceParticles.ForEach(particle => particle.ToggleActive(enable));
            ReEvaluateInspiringPresenceFoW();
        }

        private void FoWUpdated(List<TeamFogOfWarTracker.FoWCell> updatedCells) {
            if (!_inspiringPresenceActive) return;
            if (_entity.Location == null) return;

            List<Vector2Int> adjacentAndEntityCells = GetAdjacentAndEntityCells();
            List<TeamFogOfWarTracker.FoWCell> cellsOfInterest = updatedCells.Where(c => adjacentAndEntityCells.Contains(c.Position)).ToList();
            if (cellsOfInterest.Any()) {
                UpdateInspiringPresenceFoW(cellsOfInterest);
            }
        }

        private void ReEvaluateInspiringPresenceFoW() {
            if (!_inspiringPresenceActive) return;
            if (GameManager.Instance.FogOfWarManager == null) return;
            TeamFogOfWarTracker tracker = GameManager.Instance.FogOfWarManager.GetLocalTeamTracker();
            if (tracker == null) return;
            
            List<TeamFogOfWarTracker.FoWCell> cells = new();
            foreach (Vector2Int location in GetAdjacentAndEntityCells()) {
                cells.Add(new TeamFogOfWarTracker.FoWCell {
                    Position = location,
                    Hidden = tracker.IsLocationHidden(location)
                });
            }
            
            UpdateInspiringPresenceFoW(cells);
        }

        /// <summary>
        /// Actually update the individual inspiring presence particles
        /// </summary>
        /// <param name="updatedCells"></param>
        private void UpdateInspiringPresenceFoW(List<TeamFogOfWarTracker.FoWCell> updatedCells) {
            if (!_inspiringPresenceActive) return;

            foreach (InspiringPresencePositionedParticles particles in _inspiringPresenceParticles) {
                particles.UpdateFoW(updatedCells);
            }
        }

        private List<Vector2Int> GetAdjacentAndEntityCells() {
            if (_cachedAdjacentAndEntityPositions == null || _cachedEntityPosition == null || _cachedEntityPosition != _entity.Location!.Value) {
                _cachedEntityPosition = _entity.Location!.Value;
                _cachedAdjacentAndEntityPositions = GameManager.Instance.GridController.GridData.GetAdjacentCells(_cachedEntityPosition.Value)
                    .Select(c => c.Location)
                    .Append(_entity.Location.Value)
                    .ToList();
            }
            
            return _cachedAdjacentAndEntityPositions;
        }
    }
}