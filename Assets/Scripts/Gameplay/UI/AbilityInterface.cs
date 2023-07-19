using System;
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
            // TODO if all this entity does is build stuff (i.e. is a building), then immediately select its build ability here instead of normal setup. 
            ClearInfo();
            
            if (entity.MyTeam != GameManager.Instance.LocalPlayer.Data.Team) {
                // Don't display anything here
                return;
            }

            // Set up each ability slot
            foreach (IAbilityData abilityData in entity.Abilities.Select(a => a.Content).Where(a => a.Selectable)) {
                AbilitySlots.First(s => s.Channel == abilityData.Channel).SetUpForAbility(abilityData, entity);
            }
        }

        public void ClearInfo() {
            AbilitySlots.ForEach(s => s.Clear());
        }

        public void HandleHotkey(string input) {
            SelectAbility(AbilitySlots.First(s => string.Equals(s.Hotkey, input, StringComparison.CurrentCultureIgnoreCase)));
        }
        
        public void DeselectActiveAbility() {
            _selectedSlot = null;
            
            DeselectUnselectedSlots();
        }
        
        public void SelectAbility(AbilitySlot slot) {
            // Deselect current targetable ability
            GameManager.Instance.EntitySelectionManager.DeselectTargetableAbility();

            _selectedSlot = slot;
            if (!_selectedSlot.SelectAbility()) {
                _selectedSlot = null;
            }
            DeselectUnselectedSlots();
        }

        public void SelectBuildAbility(BuildAbilityData buildData, GridEntity selectedEntity) {
            ClearInfo();

            foreach (BuildAbilityData.PurchasableDataWithSelectionKey purchasableDataWithSelectionKey in buildData.Buildables) {
                AbilitySlot slot = AbilitySlots.FirstOrDefault(s => string.Equals(s.Hotkey, purchasableDataWithSelectionKey.selectionKey, StringComparison.CurrentCultureIgnoreCase));
                if (slot == null) {
                    throw new Exception("Found build ability with an invalid selection key");
                }
                
                slot.SetUpForBuildTarget(buildData, purchasableDataWithSelectionKey.data, selectedEntity);
            }
        }

        private void DeselectUnselectedSlots() {
            // Deselect other slots
            AbilitySlots.Where(a => a != _selectedSlot).ForEach(s => s.MarkSelected(false));
        }
    }
}