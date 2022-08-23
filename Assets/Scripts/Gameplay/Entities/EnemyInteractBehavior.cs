using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInteractBehavior : IInteractBehavior {
    public void Select(GridEntity entity) {
        // Do nothing - can't select enemy units
    }

    public void TargetWithSelectedUnit(GridEntity targetEntity) {
        GridEntity selectedEntity = GameManager.Instance.GridController.SelectedEntity;
        if (selectedEntity == null || !selectedEntity.CanTargetEntities())
            return;

        targetEntity.ReceiveAttackFromEntity(selectedEntity);
    }
}
