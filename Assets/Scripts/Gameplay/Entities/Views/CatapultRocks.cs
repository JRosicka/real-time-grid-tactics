using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using Gameplay.Managers;
using Mirror;
using Scenes;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Controls rocks launched via <see cref="CatapultAttackAbility"/> that moves towards a target over time, any enemy
    /// entities at the location once reached. 
    /// </summary>
    public class CatapultRocks : NetworkBehaviour {
        public float Speed;
        [Tooltip("This is a fake 'distance' that gets added onto the actual distance for the purpose of determining how long it takes for the projectile to reach its target")]
        public float BaseDistance = .5f;
        public float DistanceFromTargetToPlayRocksLandingAnimation;
        public Animator RocksLaunchAnimator;
        public RocksLandingAnimation RocksLandingAnimationPrefabNarrowTop;
        public RocksLandingAnimation RocksLandingAnimationPrefabWideTop;
        public Transform RocksToLaunch;

        private GridEntity _attacker;
        private HashSet<Vector2Int> _targetCells;
        private GridController _gridController;
        private bool _actuallyDamageTarget;
        private Vector2Int _originalLocation;
        private Vector3 _originalWorldPosition;
        private Vector3 _targetWorldPosition;
        private float _currentProgress;
        private float _startingDistance;
        private bool _facingRight;
        private bool _startedInitializing;
        private bool _doneInitializing;
        private int _bonusDamage;
        private bool _playedRocksLandingAnimation;
        private CatapultTargetReticle _catapultTargetReticle;
        private bool _alreadyDestroyed;
        private bool _targetHit;

        private TeamFogOfWarTracker LocalFoWTracker => GameManager.Instance.FogOfWarManager?.GetLocalTeamTracker();

        /// <summary>
        /// Should only be called on the server/SP. Sets up logic for moving catapult rocks to the target.
        /// Unlike <see cref="ArcherArrow"/>, this does not animate projectiles continuously to the target. Instead,
        /// it handles them in phases:
        /// - A launch animation is played where the rocks shoot out at an upward angle and quickly disappear
        /// - A <see cref="CatapultTargetReticle"/> gets shown to all players for the target location
        /// - This quietly tracks the distance the projectile travel without associated visuals. The farther the target,
        ///   the longer it takes to reach the destination.
        /// - When the invisible projectile is near the destination, an animation plays for the rocks landing on all the
        ///   targets.
        /// FoW is considered for all the visuals.
        /// </summary>
        public void Initialize(GridEntity attacker, HashSet<Vector2Int> targetCells, int bonusDamage) {
            _startedInitializing = true;
            _bonusDamage = bonusDamage;
            DoInitialize(attacker, targetCells, true);
            if (GameTypeTracker.Instance.GameIsNetworked) {
                RpcInitialize(attacker, targetCells.ToList());
            }
        }

        [ClientRpc]
        private void RpcInitialize(GridEntity attacker, List<Vector2Int> targetCells) {
            if (_startedInitializing) return;
            DoInitialize(attacker, targetCells.ToHashSet(), false);
        }

        private void DoInitialize(GridEntity attacker, HashSet<Vector2Int> targetCells, bool actuallyDamageTarget) {
            Vector2Int? attackerLocation = attacker.Location;
            if (attackerLocation == null) {
                Destroy(gameObject);
                return;
            }

            _attacker = attacker;
            _targetCells = targetCells;
            _gridController = GameManager.Instance.GridController;
            _actuallyDamageTarget = actuallyDamageTarget;
            _originalLocation = attackerLocation.Value;
            _originalWorldPosition = transform.position;
            _targetWorldPosition = _gridController.GetWorldPosition(targetCells);
            _startingDistance = Vector2.Distance(_originalWorldPosition, _targetWorldPosition) + BaseDistance;
            float xDif = _targetWorldPosition.x - _originalWorldPosition.x;
            _facingRight = xDif > 0;
            
            SetLaunchableRocksFacingDirection();
            SetCatapultFacingDirection();
            PlayLaunchAnimation();

            _catapultTargetReticle = GameManager.Instance.GridReticleVisualizer.ShowIncomingCatapultTarget(targetCells, attacker);

            GameAudio.Instance.EntityAttackSound(_attacker);
            _doneInitializing = true;
        }

        public void Update() {
            // Move towards the target
            _currentProgress += Time.deltaTime * Speed;
            
            // See if we should start the rocks landing animation
            if (!_playedRocksLandingAnimation && _startingDistance - _currentProgress <= DistanceFromTargetToPlayRocksLandingAnimation) {
                PlayRocksLandingAnimation();
            }
            
            // See if we should hit the target
            if (_currentProgress >= _startingDistance) {
                HitTargets();
            }
        }

        private void OnDestroy() {
            if (!_doneInitializing) return;
            if (_targetHit) return;
            
            // This is the client getting destroyed by the server, but the target was not visually hit on this client. Do that now. 
            _alreadyDestroyed = true;
            HitTargets();
        }

        private void HitTargets() {
            _targetHit = true;
            if (_actuallyDamageTarget) {
                foreach (Vector2Int cell in _targetCells) {
                    GridEntity entityAtCell = GameManager.Instance.CommandManager.GetEntitiesAtCell(cell)
                        ?.GetTopEntity()?.Entity;
                    // Only apply damage to opponents
                    if (entityAtCell != null && entityAtCell.Team == _attacker.Team.OpponentTeam()) {
                        GameManager.Instance.AttackManager.DealDamage(_attacker, entityAtCell, _bonusDamage);
                    }
                }
            }

            GameAudio.Instance.CatapultRocksLandSound(_attacker);
            
            _catapultTargetReticle.Hide(true);
            if (!_alreadyDestroyed) {
                Destroy(gameObject);
            }
        }

        private void SetCatapultFacingDirection() {
            bool faceRight = (_originalWorldPosition - _targetWorldPosition).x < 0;
            _attacker.View.SetFacingDirection(faceRight);
        }

        private void PlayLaunchAnimation() {
            if (LocalFoWTracker?.IsLocationHidden(_originalLocation) ?? false) return;
            
            RocksLaunchAnimator.Play("LaunchRocks");
        }

        private void PlayRocksLandingAnimation() {
            _playedRocksLandingAnimation = true;
            
            // Need at least one target cell to be visible
            if (_targetCells.All(cell => LocalFoWTracker?.IsLocationHidden(cell) ?? false)) return;
            
            // Play the animation since at least one target is visible
            int yMax = _targetCells.Select(c => c.y).Max();
            bool wideTop = _targetCells.Count(c => c.y == yMax) == 2;

            RocksLandingAnimation prefab = wideTop ? RocksLandingAnimationPrefabWideTop : RocksLandingAnimationPrefabNarrowTop;
            RocksLandingAnimation rocksLanding = Instantiate(prefab, GameManager.Instance.CommandManager.SpawnBucket);
            rocksLanding.transform.position = GameManager.Instance.GridController.GetWorldPosition(_targetCells);
            rocksLanding.SetFacingDirection(_facingRight);
        }

        private void SetLaunchableRocksFacingDirection() {
            Vector3 localScale = RocksToLaunch.localScale;
            float scaleX = localScale.x;
            
            if ((_facingRight && scaleX > 0) || (!_facingRight && scaleX < 0)) return;
            
            localScale = new Vector3(scaleX * -1, localScale.y, localScale.z);
            RocksToLaunch.localScale = localScale;
        }
    }
}