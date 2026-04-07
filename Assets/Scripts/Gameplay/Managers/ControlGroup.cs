using System;
using System.Collections.Generic;
using Gameplay.Entities;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles state and business logic for a single control group
    /// </summary>
    public class ControlGroup {
        private GridEntity _entity;

        public event Action<GridEntity> ControlGroupAssigned;
        public event Action ControlGroupUnassigned;

        public void SelectControlGroup() {
            if (!_entity || _entity.DeadOrDying) return;
            _entity.Select();
        }

        public void SnapCameraToControlGroup() {
            if (!_entity || _entity.Location == null) {
                Debug.LogWarning("Tried to snap the camera to a null unit");
                return;
            }
            Vector2 location = GameManager.Instance.GridController.GetWorldPosition(_entity.Location.Value);
            GameManager.Instance.CameraManager.SnapToPosition(location);
        }
        
        public void AssignControlGroup() {
            GameTeam localTeam = GameManager.Instance.LocalTeam;
            if (localTeam is not (GameTeam.Player1 or GameTeam.Player2)) return;
            
            GridEntity selectedEntity = GameManager.Instance.EntitySelectionManager.SelectedEntity;
            if (!selectedEntity
                || selectedEntity.DeadOrDying
                || selectedEntity.Team != localTeam
                || selectedEntity == _entity) return;
            
            _entity = GameManager.Instance.EntitySelectionManager.SelectedEntity;
            ControlGroupAssigned?.Invoke(_entity);
        }
        
        public void UnassignGroupIfEntityUnregistered(List<GridEntity> entities) {
            if (entities.Contains(_entity)) return;
            
            _entity = null;
            ControlGroupUnassigned?.Invoke();
        }
    }
}