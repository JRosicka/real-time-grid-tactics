using System.Collections.Generic;
using System.Linq;
using Audio;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
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
        public float MaxLifetimeSeconds;
        public AnimationCurve AttackPathCurve;

        private GridEntity _attacker;
        private HashSet<Vector2Int> _targetCells;
        private GridController _gridController;
        private bool _actuallyDamageTarget;
        private Vector3 _originalLocation;
        private Vector3 _lastLocation;
        private Vector3 _targetWorldPosition;
        private float _currentProgress;
        private float _startingDistance;
        private int _signInt;
        private bool _initialized;
        private int _bonusDamage;
        private float _lifetimeSeconds;

        /// <summary>
        /// Should only be called on the server/SP. Sets up logic for moving the arrow to the target. 
        /// </summary>
        public void Initialize(GridEntity attacker, HashSet<Vector2Int> targetCells, int bonusDamage) {
            _initialized = true;
            _bonusDamage = bonusDamage;
            DoInitialize(attacker, targetCells, true);
            if (GameTypeTracker.Instance.GameIsNetworked) {
                RpcInitialize(attacker, targetCells.ToList());
            }
        }

        [ClientRpc]
        private void RpcInitialize(GridEntity attacker, List<Vector2Int> targetCells) {
            if (_initialized) return;
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
            _originalLocation = transform.position;
            _lastLocation = _originalLocation;
            _targetWorldPosition = _gridController.GetWorldPosition(targetCells);
            _startingDistance = Vector2.Distance(_originalLocation, _targetWorldPosition);
            float xDif = _targetWorldPosition.x - _gridController.GetWorldPosition(attackerLocation.Value).x;
            _signInt = xDif < 0 ? -1 : 1;
        }

        public void Update() {
            _lifetimeSeconds += Time.deltaTime;
            if (_lifetimeSeconds >= MaxLifetimeSeconds) {
                Debug.LogWarning("Took too long for the arrow to reach its target! Hitting now.");
                HitTargets();
                return;
            }
            
            // Lob towards the target
            _currentProgress += Time.deltaTime * Speed / _startingDistance;
            if (_currentProgress >= 1) {
                HitTargets();
                return;
            }
        
            Vector3 currentPosition = Vector3.Lerp(_originalLocation, _targetWorldPosition, _currentProgress);
            Vector2 perpendicular = Vector2.Perpendicular(_targetWorldPosition - _originalLocation);
            currentPosition += _signInt * AttackPathCurve.Evaluate(_currentProgress) * (Vector3)perpendicular;
            transform.position = currentPosition;
        
            float degreeRotation = Mathf.Rad2Deg * Mathf.Atan2(currentPosition.y - _lastLocation.y, currentPosition.x - _lastLocation.x);
            transform.rotation = Quaternion.Euler(0, 0, degreeRotation);
            
            _lastLocation = transform.position;
        }
        
        private void HitTargets() {
            if (_actuallyDamageTarget) {
                foreach (Vector2Int cell in _targetCells) {
                    GridEntity entityAtCell = GameManager.Instance.CommandManager.GetEntitiesAtCell(cell)?.GetTopEntity()?.Entity;
                    // Only apply damage to opponents
                    if (entityAtCell != null && entityAtCell.Team == _attacker.Team.OpponentTeam()) {
                        GameManager.Instance.AttackManager.DealDamage(_attacker, entityAtCell, _bonusDamage);
                    }
                }
            }
            
            GameAudio.Instance.CatapultRocksLandSound(_attacker);
            Destroy(gameObject);
        }
    }
}