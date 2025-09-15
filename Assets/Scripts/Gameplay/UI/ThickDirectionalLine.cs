using Gameplay.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Controls and animates a directional line of thick arrows for a single straight line
    /// </summary>
    public class ThickDirectionalLine : AbstractDirectionalLine {
        [SerializeField] private Image _chargeLineSegment;
        [SerializeField] private Image _chargeDestinationSegment;
        
        protected override float CellWidth => GridController.CellWidthWithBuffer;

        protected override void SetRotationImpl(float angle) {
            // Nothing to do
        }

        protected override void SetUpEndHalfImpl() {
            ShowDestinationIcon(PathVisualizer.PathType.Move);
        }
        
        public override void Discard() {
            SetRotation(0);
            
            Color color = _chargeLineSegment.color;
            color.a = 1;
            _chargeLineSegment.color = color;
            
            color = _chargeDestinationSegment.color;
            color.a = 0;
            _chargeDestinationSegment.color = color;
        }
        
        public override void ToggleEndDot(bool toggle) {
            // Nothing to do
        }

        public override void ShowDestinationIcon(PathVisualizer.PathType pathType) {
            Color color = _chargeLineSegment.color;
            color.a = 0;
            _chargeLineSegment.color = color;
            
            color = _chargeDestinationSegment.color;
            color.a = 1;
            _chargeDestinationSegment.color = color;
        }
    }
}
