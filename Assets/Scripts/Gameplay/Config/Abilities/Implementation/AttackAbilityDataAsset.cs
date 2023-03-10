using System;
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
            GameManager.Instance.GridController.SelectTargetableAbility(this);
        }

        protected override bool AbilityLegalImpl(AttackAbilityParameters parameters, GridEntity entity) {
            return CanAttackTarget(parameters.Target, parameters.Attacker);
        }

        protected override IAbility CreateAbilityImpl(AttackAbilityParameters parameters, GridEntity performer) {
            return new AttackAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity entity, GridEntity.Team selectorTeam) {
            GridEntity target = GameManager.Instance.GetEntitiesAtLocation(cellPosition).GetTopEntity().Entity;
            return CanAttackTarget(target, entity);
        }

        private bool CanAttackTarget(GridEntity target, GridEntity selector) {
            // TODO range
            return target != null && selector != null && target.MyTeam != GridEntity.Team.Neutral && target.MyTeam != selector.MyTeam;
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity entity, GridEntity.Team selectorTeam) {
            GridEntity target = GameManager.Instance.GetEntitiesAtLocation(cellPosition).GetTopEntity().Entity;    // Only able to target the top entity!
            entity.DoAbility(this, new AttackAbilityParameters {Attacker = entity, Target = target});
        }
    }
}