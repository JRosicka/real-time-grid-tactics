using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwnerInteractBehavior : IInteractBehavior {
    public void Select(GridEntity entity) {
        GameManager.Instance.GridController.SelectedEntity = entity;
    }

    public void TargetWithSelectedUnit(GridEntity targetEntity) {
        GridEntity selectedEntity = GameManager.Instance.GridController.SelectedEntity;
        if (selectedEntity == null || !selectedEntity.CanTargetEntities())
            return;

        targetEntity.ReceiveAttackFromEntity(selectedEntity);
    }
}
