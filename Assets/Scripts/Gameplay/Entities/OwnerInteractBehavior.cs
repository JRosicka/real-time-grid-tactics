using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwnerInteractBehavior : IInteractBehavior {
    public void Select(GridEntity entity) {
        GameManager.Instance.SelectedEntity = entity;
    }

    public void TargetCellWithUnit(GridEntity thisEntity, Vector3Int targetCell) {
        if (thisEntity == null) {
            return;
        } 
        
        GridEntity targetEntity = GameManager.Instance.GetEntityAtLocation(targetCell);
        
        // See if we should move this entity
        if (targetEntity == null) {
            TryMoveEntity(thisEntity, targetCell);
        } else {
            TryTargetEntity(thisEntity, targetEntity, targetCell);
        }
    }

    private void TryMoveEntity(GridEntity thisEntity, Vector3Int targetCell) {
        if (thisEntity.CanMove()) {
            thisEntity.MoveToCell(targetCell);
        }
    }

    private void TryTargetEntity(GridEntity thisEntity, GridEntity targetEntity, Vector3Int targetCell) {
        if (!thisEntity.CanTargetThings())
            return;

        thisEntity.TryTargetEntity(targetEntity, targetCell);
    }
}
