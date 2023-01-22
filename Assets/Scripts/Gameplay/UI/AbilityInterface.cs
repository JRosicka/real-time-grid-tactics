using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Sirenix.Utilities;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Handles displaying a selected <see cref="GridEntity"/>'s <see cref="IAbility"/>s and allowing the player to use them
    /// </summary>
    public class AbilityInterface : MonoBehaviour {
        public List<AbilitySlot> AbilitySlots;

        private AbilitySlot _selectedSlot;
        
        public void SetUpForEntity(GridEntity entity) {
            ClearInfo();
            
            if (entity.MyTeam != GameManager.Instance.LocalPlayer.Data.Team) {
                // Don't display anything here
                return;
            }

            // Set up each ability slot
            foreach (IAbilityData abilityData in entity.Abilities.Select(a => a.Content)) {
                AbilitySlots.First(s => s.Channel == abilityData.Channel).SetUpForAbility(abilityData, entity);
            }
        }

        public void ClearInfo() {
            AbilitySlots.ForEach(s => s.Clear());
        }

        public void HandleHotkey(string input) {
            SelectAbility(AbilitySlots.First(s => s.Hotkey == input));
        }
        
        public void DeselectActiveAbility() {
            _selectedSlot = null;
            DeselectUnselectedSlots();
        }
        
        public void SelectAbility(AbilitySlot slot) {
            _selectedSlot = slot;
            if (!_selectedSlot.SelectAbility()) {
                _selectedSlot = null;
            }
            DeselectUnselectedSlots();
        }

        private void DeselectUnselectedSlots() {
            // Deselect other slots
            AbilitySlots.Where(a => a != _selectedSlot).ForEach(s => s.MarkSelected(false));
        }
    }
}