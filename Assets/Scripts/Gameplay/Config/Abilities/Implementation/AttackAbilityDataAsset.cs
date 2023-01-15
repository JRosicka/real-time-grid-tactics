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
    public class AttackAbilityData : AbilityDataBase<AttackAbilityParameters>, ITargetableAbilityData {

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.SelectionInterface.SelectTargetableAbility(this);
        }

        public override bool AbilityLegalImpl(AttackAbilityParameters parameters, GridEntity entity) {
            // TODO
            return base.AbilityLegalImpl(parameters, entity);
        }

        protected override IAbility CreateAbilityImpl(AttackAbilityParameters parameters, GridEntity performer) {
            return new AttackAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selector, GridEntity target) {
            // TODO range
            return target != null && selector != null && target.MyTeam != GridEntity.Team.Neutral && target.MyTeam != selector.MyTeam;
        }

        public void CreateAbility(Vector2Int cellPosition, GridEntity selector, GridEntity target) {
            selector.DoAbility(this, new AttackAbilityParameters {Attacker = selector, Target = target});
        }
    }
}