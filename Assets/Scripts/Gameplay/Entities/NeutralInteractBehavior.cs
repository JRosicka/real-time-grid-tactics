using UnityEngine;

namespace GamePlay.Entities {
    public class NeutralInteractBehavior : IInteractBehavior {
        public void Select(GridEntity entity) {
            // Do nothing
        }

        public void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell) {
            // Do nothing
        }
    }
}