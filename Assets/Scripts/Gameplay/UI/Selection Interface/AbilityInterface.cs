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
        public TooltipView TooltipView;
        
        /// <summary>
        /// True if displaying the build menu after an active selection (i.e. the build ability was not auto-selected)
        /// </summary>
        public bool BuildMenuOpenFromSelection { get; private set; }

        private AbilitySlot _selectedSlot;
        private GridEntity SelectedEntity => GameManager.Instance.EntitySelectionManager.SelectedEntity;
        
        public void SetUpForEntity(GridEntity entity) {
            ClearInfo();

            // Set up each ability slot
            foreach (IAbilityData abilityData in entity.Abilities.Where(a => a.Selectable)) {
                AbilitySlot slot = AbilitySlots.First(s => s.SlotLocation == abilityData.SlotLocation);
                DefaultAbilitySlotBehavior abilityBehavior = new DefaultAbilitySlotBehavior(abilityData, entity);
                slot.SetUpSlot(abilityBehavior, entity);
            }
        }

        public void ClearInfo() {
            BuildMenuOpenFromSelection = false;
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
            if (!slot.Selectable) return;
            
            // Don't allow interaction with the slot unless the local player owns this entity
            GridEntity selectedEntity = SelectedEntity;
            if (selectedEntity == null || !selectedEntity.InteractBehavior!.IsLocalTeam) return;
            
            // Deselect current targetable ability
            GameManager.Instance.EntitySelectionManager.DeselectTargetableAbility();

            _selectedSlot = slot;
            _selectedSlot.SelectAbility();
            
            DeselectUnselectedSlots();
        }

        public void SelectBuildAbility(BuildAbilityData buildData, GridEntity selectedEntity) {
            ClearInfo();
            if (!buildData.AutoSelect) {
                BuildMenuOpenFromSelection = true;
            }

            foreach (BuildAbilityData.PurchasableDataWithSelectionKey purchasableDataWithSelectionKey in buildData.Buildables) {
                AbilitySlot slot = AbilitySlots.FirstOrDefault(s => string.Equals(s.Hotkey, purchasableDataWithSelectionKey.selectionKey, StringComparison.CurrentCultureIgnoreCase));
                if (slot == null) {
                    throw new Exception("Found build ability with an invalid selection key");
                }

                BuildAbilitySlotBehavior buildBehavior = new BuildAbilitySlotBehavior(buildData, purchasableDataWithSelectionKey.data, selectedEntity);
                slot.SetUpSlot(buildBehavior, selectedEntity);
            }
        }

        private void DeselectUnselectedSlots() {
            // Deselect other slots
            AbilitySlots.Where(a => a != _selectedSlot).ForEach(s => s.MarkSelected(false));
        }
    }
}