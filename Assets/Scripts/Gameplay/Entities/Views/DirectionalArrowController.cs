using System;
using Gameplay.Grid;
using Gameplay.UI;
using UnityEngine;
using UnityEngine.UI;
using Task = System.Threading.Tasks.Task;

namespace Gameplay.Entities {
    /// <summary>
    /// Controls position/rotation of a directional arrow view on a <see cref="GridEntityView"/>
    /// </summary>
    public class DirectionalArrowController : MonoBehaviour {
        [Header("References")]
        [SerializeField] private Image _arrowFill;
        [SerializeField] private Transform _arrowContainer;
        [SerializeField] private Transform _arrow;
        
        [Header("Config")]
        [SerializeField] private Color _moveColor;
        [SerializeField] private Color _attackColor;
        [SerializeField] private Color _targetAttackColor;
        [SerializeField] private float _maxEdgeDistance;
        [SerializeField] private float _extraEdgeDistance;
        [SerializeField] private float _arrowMoveSeconds = .15f;
        
        private GridEntity _entity;
        private bool _active;
        private float _previousAngle;
        private float _previousDistance;
        private float _targetAngle;
        private float _targetDistance;
        private float _updateTime;
        
        public void Initialize(GridEntity entity) {
            _entity = entity;
            ToggleArrow(false);

            if (!entity.InteractBehavior!.AllowedToSeeMiscInfo) return;

            entity.TargetLocationLogic.ValueChanged += TargetLocationChanged;
            entity.EntityMovedClientEvent += EntityMoved;
        }

        private void TargetLocationChanged(INetworkableFieldValue oldValue, INetworkableFieldValue newValue, string metadata) {
            TryPointArrow((TargetLocationLogic) newValue);
        }

        private async void EntityMoved() {
            // Need to delay since this move might have happened before updating the target location. When using move/attack abilities, the effect happens before the target location gets updated. 
            await Task.Delay(100);
            if (!this || _entity.DeadOrDying || _entity.Location == null) return;
            
            TryPointArrow(_entity.TargetLocationLogicValue);
        }

        private void TryPointArrow(TargetLocationLogic targetLocationLogic) {
            PathVisualizer.PathType pathType = !targetLocationLogic.Attacking
                ? PathVisualizer.PathType.Move
                : targetLocationLogic.TargetEntity != null
                    ? PathVisualizer.PathType.TargetAttack
                    : PathVisualizer.PathType.AttackMove;
            DoPointArrow(targetLocationLogic.CurrentTarget, pathType);
        }

        private void DoPointArrow(Vector2Int destination, PathVisualizer.PathType targetType) {
            if (_entity.DeadOrDying || _entity.Location == null) return;

            bool wasActive = _active;
            ToggleArrow(destination != _entity.Location.Value);
            
            // Arrow color
            _arrowFill.color = targetType switch {
                PathVisualizer.PathType.Move => _moveColor,
                PathVisualizer.PathType.AttackMove => _attackColor,
                PathVisualizer.PathType.TargetAttack => _targetAttackColor,
                _ => throw new ArgumentOutOfRangeException(nameof(targetType), targetType, null)
            };

            // Angle
            GridController gridController = GameManager.Instance.GridController;
            Vector2 line = gridController.GetWorldPosition(destination) - gridController.GetWorldPosition(_entity.Location.Value);
            float newTargetAngle = Vector2.SignedAngle(Vector2.up, line);
            
            // Distance
            float newTargetDistance = GetHexagonEdgeDistance(newTargetAngle);

            SetTargets(newTargetAngle, newTargetDistance, wasActive);
        }

        private void SetTargets(float newTargetAngle, float newTargetDistance, bool wasActive) {
            _targetAngle = newTargetAngle;
            _targetDistance = newTargetDistance;
            if (!wasActive) {
                // Just move there immediately since we just un-hid the arrow
                UpdateArrow(newTargetAngle, newTargetDistance, true);
            } else {
                _updateTime = 0;
            }
        }

        private float GetHexagonEdgeDistance(float angle) {
            float reducedAngle = (angle + 360) % 60;
            float reducedAngleRads = reducedAngle * Mathf.Deg2Rad;
            return (Mathf.Sqrt(3) * _maxEdgeDistance) / (Mathf.Sqrt(3) * Mathf.Cos(reducedAngleRads) + Mathf.Sin(reducedAngleRads)) + _extraEdgeDistance;
        }
        
        #region View/animation
        
        private void Update() {
            if (_updateTime < 0) return;
            _updateTime += Time.deltaTime;
            bool finalUpdate = _updateTime >= _arrowMoveSeconds;
            
            float newAngle = Mathf.LerpAngle(_previousAngle, _targetAngle, _updateTime / _arrowMoveSeconds);
            float newDistance = Mathf.Lerp(_previousDistance, _targetDistance, _updateTime / _arrowMoveSeconds);
            UpdateArrow(newAngle, newDistance, finalUpdate);
        }

        private void UpdateArrow(float newAngle, float newDistance, bool done) {
            _arrowContainer.localRotation = Quaternion.Euler(0, 0, newAngle);
            Vector2 position = _arrow.localPosition;
            position.y = newDistance;
            _arrow.localPosition = position;
            
            if (done) {
                _previousAngle = newAngle;
                _previousDistance = newDistance;
                _updateTime = -1;
            }
        }

        private void ToggleArrow(bool active) {
            _active = active;
            _arrowContainer.gameObject.SetActive(active);
        }
        
        #endregion
    }
}