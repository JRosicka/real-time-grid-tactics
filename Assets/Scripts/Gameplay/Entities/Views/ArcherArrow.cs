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
        public float Speed;
        public AnimationCurve AttackPathCurve;

        private GridEntity _attacker;
        private GridEntity _target;
        private GridController _gridController;
        private bool _shouldPerformUpdates;
        private Vector3 _originalLocation;
        private Vector2Int _lastTargetLocation;
        private Vector3 _lastLocation;
        private Vector3 _lastTargetWorldPosition;
        private float _currentProgress;
        private float _startingDistance;
        private bool _stopLobbing;
        private int _signInt;

        /// <summary>
        /// Should only be called on the server/SP. Sets up logic for moving the arrow to the target. 
        /// </summary>
        public void Initialize(GridEntity attacker, GridEntity target) {
            _attacker = attacker;
            _target = target;
            _gridController = GameManager.Instance.GridController;
            _shouldPerformUpdates = true;
            _originalLocation = transform.position;
            _lastTargetLocation = target.Location;
            _lastLocation = _originalLocation;
            _lastTargetWorldPosition = _gridController.GetWorldPosition(target.Location);
            _startingDistance = Vector2.Distance(_originalLocation, _lastTargetWorldPosition);
            int xDif = _lastTargetLocation.x - attacker.Location.x;
            _signInt = xDif < 0 ? -1 : 1;
        }

        public void Update() {
            if (!_shouldPerformUpdates) return;
            if (_target == null || _target.DeadOrDying()) {
                Destroy(gameObject);
                return;
            }

            Vector3 currentPosition = transform.position;
            
            if (_lastTargetLocation != _target.Location) {
                // The target moved, so record changes and from now on just travel in a straight line towards the target
                _lastTargetLocation = _target.Location;
                _lastTargetWorldPosition = _gridController.GetWorldPosition(_target.Location);
                _stopLobbing = true;

                float degreeRotation = Mathf.Rad2Deg * Mathf.Atan2(_lastTargetWorldPosition.y - currentPosition.y, _lastTargetWorldPosition.x - currentPosition.x);
                transform.rotation = Quaternion.Euler(0, 0, degreeRotation);
            }

            if (_stopLobbing) {
                // Progress in a straight line
                Vector3 progressVector = Time.deltaTime * Speed * (_lastTargetWorldPosition - _lastLocation);
                transform.position += progressVector;
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
            _target.ReceiveAttackFromEntity(_attacker);
            Destroy(gameObject);
        }
    }
}