using System;
using System.Collections.Generic;
using Gameplay.Entities;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles business logic and state for control groups
    /// </summary>
    public class ControlGroupsManager {
        private readonly Dictionary<int, ControlGroup> _controlGroups = new Dictionary<int, ControlGroup>();
        private ICommandManager _commandManager;
        private GameTeam _localTeam;
        
        public event Action<int, bool> ControlGroupSelected;

        public void Initialize(ICommandManager commandManager, GameTeam localTeam) {
            for (int i = 0; i < 10; i++) {
                _controlGroups[i] = new ControlGroup();
            }

            _commandManager = commandManager;
            _localTeam = localTeam;
            commandManager.EntityCollectionChangedEvent += UpdateControlGroups;
        }

        public ControlGroup GetControlGroup(int index) {
            return _controlGroups[index];
        }
        
        public void SelectControlGroup(int index, bool pressed, bool updateButtonView) {
            if (pressed) {
                GetControlGroup(index).SelectControlGroup();
            }

            if (updateButtonView) {
                ControlGroupSelected?.Invoke(index, pressed);
            }
        }

        public void SnapCameraToControlGroup(int index) {
            GetControlGroup(index).SnapCameraToControlGroup();
        }

        public void AssignControlGroup(int index) {
            GetControlGroup(index).AssignControlGroup();
        }

        private void UpdateControlGroups() {
            List<GridEntity> entities = _commandManager.EntitiesOnGrid.ActiveEntitiesForTeam(_localTeam);
            foreach (ControlGroup controlGroup in _controlGroups.Values) {
                controlGroup.UnassignGroupIfEntityUnregistered(entities);
            }
        }
    }
}