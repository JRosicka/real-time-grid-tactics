using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Entities {
    public class EnemyInteractBehavior : IInteractBehavior {
        public void Select(GridEntity entity) {
            // Do nothing - can't select enemy units
        }

        public void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell) {
            // Do nothing - can't use enemy units to do stuff
        }
    }
}