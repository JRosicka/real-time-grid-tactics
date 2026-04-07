using System.Collections.Generic;
using System.Linq;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// View logic for <see cref="ControlGroupButton"/>s
    /// </summary>
    public class ControlGroupsViewZone : MonoBehaviour {
        private ControlGroupsManager _controlGroupsManager;
        [SerializeField] private List<ControlGroupButton> _buttons;

        public void Initialize(ControlGroupsManager controlGroupsManager) {
            _controlGroupsManager = controlGroupsManager;
            controlGroupsManager.ControlGroupSelected += ControlGroupSelected;
            
            foreach (ControlGroupButton b in _buttons) {
                b.Initialize(controlGroupsManager);
                b.ControlGroupUpdated += UpdateControlGroupRows;
            }
        }

        private void UpdateControlGroupRows() {
            // TODO do we want to hide empty rows?
        }

        private void ControlGroupSelected(int groupIndex, bool pressed) {
            _buttons.First(b => b.ControlGroup == groupIndex).ControlGroupHotkeyPressed(pressed);
        }
    }
}