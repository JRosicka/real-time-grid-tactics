using System;
using Gameplay.Grid;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Controls and animates a directional line of arrows that can be used to indicate movement
    /// </summary>
    public class BasicDirectionalLine : AbstractDirectionalLine {
        [SerializeField] private Image _endDot;
        [SerializeField] private Image _moveDestinationIcon;
        [FormerlySerializedAs("_attackDestinationIcon")] [SerializeField] private Image _attackMoveDestinationIcon;
        [SerializeField] private Image _targetAttackDestinationIcon;

        private Image _destinationIcon;
        private Sprite _originalSprite;
        
        private bool _destination;

        protected override float CellWidth => GridController.CellWidth;
        
        protected override void SetRotationImpl(float angle) {            
            // Rotate the icons back - we don't want their global rotation to change
            _endDot.transform.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        protected override void SetUpEndHalfImpl() {
            ToggleEndDot(false);
        }

        public override void Discard() {
            _destination = false;
            ToggleEndDot(true);
            if (_originalSprite != null) {
                _destinationIcon.sprite = _originalSprite;
                _originalSprite = null;
            }
            _attackMoveDestinationIcon.gameObject.SetActive(false);
            _moveDestinationIcon.gameObject.SetActive(false);
            _targetAttackDestinationIcon.gameObject.SetActive(false);
            SetRotation(0);
            UpdateColor(DefaultColor);
        }
        
        public override void ToggleEndDot(bool toggle) {
            bool actuallyToggle = toggle && !_destination;
            Color color = _endDot.color;
            color.a = actuallyToggle ? 1 : 0;
            _endDot.color = color;
        }

        public override void ShowDestinationIcon(PathVisualizer.PathType pathType, Sprite overrideSprite = null) {
            _destination = true;
            ToggleEndDot(false);
            
            _destinationIcon = pathType switch {
                PathVisualizer.PathType.Move => _moveDestinationIcon,
                PathVisualizer.PathType.AttackMove => _attackMoveDestinationIcon,
                PathVisualizer.PathType.TargetAttack => _targetAttackDestinationIcon,
                _ => throw new NotImplementedException(),
            };

            if (overrideSprite != null) {
                _originalSprite = _destinationIcon.sprite;
                _destinationIcon.sprite = overrideSprite;
            }

            _destinationIcon.gameObject.SetActive(true);
        }
    }
}
