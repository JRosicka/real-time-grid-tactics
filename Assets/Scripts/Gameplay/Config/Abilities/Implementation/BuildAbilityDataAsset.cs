using System;
using System.Collections.Generic;
using GamePlay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/BuildAbilityData")]
    public class BuildAbilityDataAsset : BaseAbilityDataAsset<BuildAbilityData, BuildAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to build stuff
    /// </summary>
    [Serializable]
    public class BuildAbilityData : AbilityDataBase<BuildAbilityParameters> {
        public List<PurchasableData> Buildables;

        public void Build(PurchasableData itemToBuild) {
            if (!Buildables.Contains(itemToBuild)) {
                Debug.LogError($"Attempted to build item that is not configured to be buildable by this entity: {itemToBuild}");
                return;
            }
            
            Debug.Log(nameof(Build));    // TODO
        }
        
        public override void SelectAbility(GridEntity selector) {
            Debug.Log(nameof(SelectAbility)); // TODO
        }

        protected override IAbility CreateAbilityImpl(BuildAbilityParameters parameters) {
            return new BuildAbility(this, parameters);
        }
    }
}