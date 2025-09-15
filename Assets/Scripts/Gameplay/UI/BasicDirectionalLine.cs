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

        public override void ShowDestinationIcon(PathVisualizer.PathType pathType) {
            _destination = true;
            ToggleEndDot(false);
            
            _moveDestinationIcon.gameObject.SetActive(pathType == PathVisualizer.PathType.Move);
            _attackMoveDestinationIcon.gameObject.SetActive(pathType == PathVisualizer.PathType.AttackMove);
            _targetAttackDestinationIcon.gameObject.SetActive(pathType == PathVisualizer.PathType.TargetAttack);
        }
    }
}
