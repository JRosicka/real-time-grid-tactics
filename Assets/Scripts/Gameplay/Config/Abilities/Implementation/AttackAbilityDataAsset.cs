using System;
using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/AttackAbilityData")]
    public class AttackAbilityDataAsset : BaseAbilityDataAsset<AttackAbilityData, AttackAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for attacking an entity
    /// </summary>
    [Serializable]
    public class AttackAbilityData : AbilityDataBase<AttackAbilityParameters> {

        public override void SelectAbility(GridEntity selector) {
            Debug.Log(nameof(SelectAbility)); // TODO actually make this do anything
        }

        public override bool AbilityLegalImpl(AttackAbilityParameters parameters, GridEntity entity) {
            // TODO
            return base.AbilityLegalImpl(parameters, entity);
        }

        protected override IAbility CreateAbilityImpl(AttackAbilityParameters parameters, GridEntity performer) {
            return new AttackAbility(this, parameters, performer);
        }
    }
}