using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Controls an arrow from an <see cref="AttackAbility"/> that moves towards a target over time, damaging the target
    /// upon reaching it. 
    /// </summary>
    public class ArcherArrow : NetworkBehaviour {
        public float CloseEnoughAmount = .05f;
        public float Speed;
        public AnimationCurve AttackPathCurve;

        private GridEntity _attacker;
        private GridEntity _target;
        private GridController _gridController;
        private bool _actuallyDamageTarget;
        private Vector3 _originalLocation;
        private Vector2Int _lastTargetLocation;
        private Vector3 _lastLocation;
        private Vector3 _lastTargetWorldPosition;
        private float _currentProgress;
        private float _startingDistance;
        private bool _stopLobbing;
        private int _signInt;
        private bool _initialized;
        private int _bonusDamage;

        /// <summary>
        /// Should only be called on the server/SP. Sets up logic for moving the arrow to the target. 
        /// </summary>
        public void Initialize(GridEntity attacker, GridEntity target, int bonusDamage) {
            _initialized = true;
            _bonusDamage = bonusDamage;
            DoInitialize(attacker, target, true);
            if (NetworkClient.active) {
                RpcInitialize(attacker, target);
            }
        }

        [ClientRpc]
        private void RpcInitialize(GridEntity attacker, GridEntity target) {
            if (_initialized) return;
            DoInitialize(attacker, target, false);
        }

        private void DoInitialize(GridEntity attacker, GridEntity target, bool actuallyDamageTarget) {
            Vector2Int? targetLocation = target.Location;
            Vector2Int? attackerLocation = attacker.Location;
            if (targetLocation == null || attackerLocation == null) {
                Destroy(gameObject);
                return;
            }
            
            _attacker = attacker;
            _target = target;
            _gridController = GameManager.Instance.GridController;
            _actuallyDamageTarget = actuallyDamageTarget;
            _originalLocation = transform.position;
            _lastTargetLocation = targetLocation.Value;
            _lastLocation = _originalLocation;
            _lastTargetWorldPosition = _gridController.GetWorldPosition(targetLocation.Value);
            _startingDistance = Vector2.Distance(_originalLocation, _lastTargetWorldPosition);
            int xDif = _lastTargetLocation.x - attackerLocation.Value.x;
            _signInt = xDif < 0 ? -1 : 1;
        }

        public void Update() {
            Vector2Int? targetLocation = _target == null ? null : _target.Location;
            if (targetLocation == null || _target.DeadOrDying) {
                Destroy(gameObject);
                return;
            }

            Vector3 currentPosition = transform.position;
            
            if (_lastTargetLocation != _target.Location) {
                // The target moved, so record changes and from now on just travel in a straight line towards the target
                _lastTargetLocation = targetLocation.Value;
                _lastTargetWorldPosition = _gridController.GetWorldPosition(targetLocation.Value);
                _stopLobbing = true;

                float degreeRotation = Mathf.Rad2Deg * Mathf.Atan2(_lastTargetWorldPosition.y - currentPosition.y, _lastTargetWorldPosition.x - currentPosition.x);
                transform.rotation = Quaternion.Euler(0, 0, degreeRotation);
            }

            if (_stopLobbing) {
                // Progress in a straight line
                Vector3 progressVector = Time.deltaTime * Speed * (_lastTargetWorldPosition - _lastLocation).normalized;
                transform.position += progressVector;
                if (Vector2.Distance(_lastTargetWorldPosition, transform.position) < CloseEnoughAmount) {
                    HitTarget();
                    return;
                }
            } else {
                // Lob towards the target
                _currentProgress += Time.deltaTime * Speed / _startingDistance;
                if (_currentProgress >= 1) {
                    HitTarget();
                    return;
                }
            
                currentPosition = Vector3.Lerp(_originalLocation, _lastTargetWorldPosition, _currentProgress);
                Vector2 perpendicular = Vector2.Perpendicular(_lastTargetWorldPosition - _originalLocation);
                currentPosition += _signInt * AttackPathCurve.Evaluate(_currentProgress) * (Vector3)perpendicular;
                transform.position = currentPosition;
            
                float degreeRotation = Mathf.Rad2Deg * Mathf.Atan2(currentPosition.y - _lastLocation.y, currentPosition.x - _lastLocation.x);
                transform.rotation = Quaternion.Euler(0, 0, degreeRotation);
            }
            
            _lastLocation = transform.position;
        }
        
        private void HitTarget() {
            if (_actuallyDamageTarget) {
                GameManager.Instance.AttackManager.DealDamage(_attacker, _target, _bonusDamage);
            }
            Destroy(gameObject);
        }
    }
}