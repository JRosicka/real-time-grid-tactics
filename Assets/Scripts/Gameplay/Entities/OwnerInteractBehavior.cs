using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwnerInteractBehavior : IInteractBehavior {
    public void Select(GridEntity entity) {
        GameManager.Instance.EntityManager.SelectedEntity = entity;
    }

    public void TargetCellWithUnit(GridEntity thisEntity, Vector3Int targetCell) {
        if (thisEntity == null) {
            return;
        } 
        
        GridEntity targetEntity = GameManager.Instance.EntityManager.GetEntityAtLocation(targetCell);
        
        // See if we should move this entity
        if (targetEntity == null) {
            if (thisEntity.CanMove()) {
                thisEntity.MoveToCell(targetCell);
            }
        } else if (thisEntity.CanTargetThings()) {
            targetEntity.ReceiveAttackFromEntity(thisEntity);
        }
    }
}
