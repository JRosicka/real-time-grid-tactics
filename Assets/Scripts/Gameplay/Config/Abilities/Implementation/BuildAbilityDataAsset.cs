using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
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
        public List<PurchasableDataWithSelectionKey> Buildables;

        [Serializable]
        public struct PurchasableDataWithSelectionKey {
            public PurchasableData data;
            public string selectionKey;
        }

        public override void SelectAbility(GridEntity selector) {
            Debug.Log(nameof(SelectAbility)); 
            GameManager.Instance.SelectionInterface.SelectBuildAbility(this);
        }

        public override bool AbilityLegalImpl(BuildAbilityParameters parameters, GridEntity entity) {
            // if (parameters.Buildable.Cost is too damn high) return false;    // TODO
            return base.AbilityLegalImpl(parameters, entity);
        }

        protected override IAbility CreateAbilityImpl(BuildAbilityParameters parameters, GridEntity performer) {
            return new BuildAbility(this, parameters, performer);
        }
    }
}