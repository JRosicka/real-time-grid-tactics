using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Handles displaying a selected <see cref="GridEntity"/>'s <see cref="IAbility"/>s and allowing the player to use them
    /// </summary>
    public class AbilityInterface : MonoBehaviour {
        public List<AbilitySlot> AbilitySlots;

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
            AbilitySlots.First(s => s.Hotkey == input).SelectAbility();
        }
    }
}