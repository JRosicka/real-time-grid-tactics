using System;
using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/UnsiegeAbilityData")]
    public class UnsiegeDataAsset : BaseAbilityDataAsset<UnsiegeAbilityData, NullAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for unsieging
    /// </summary>
    [Serializable]
    public class UnsiegeAbilityData : AbilityDataBase<NullAbilityParameters> {
        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => false;
        public override bool ManuallyCancelable => false;

        public override void SelectAbility(GridEntity selector, object selectData) {
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selector, this, new NullAbilityParameters(), true, false, false, true); 
        }

        public override bool TryUseAbilityAutomatically(GridEntity selector, GridEntity target, GridEntity.TargetType targetType, Vector2Int targetCell) {
            throw new NotImplementedException();
        }

        protected override AbilityLegality AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity, GameTeam team, out string failureReason) {
            failureReason = null;
            return AbilityLegality.Legal;
        }

        public override AbilityLegality CanEntityUseAbility(GridEntity entity, GameTeam team, out string failureReason) {
            failureReason = null;
            if (!entity.IsActive()) {
                return AbilityLegality.IndefinitelyIllegal;
            }

            // Only legal if unsieged (or sieging)
            if (entity.SiegeState is SiegeState.Sieged or SiegeState.Unsieging) {
                return AbilityLegality.Legal;
            }
            
            // Otherwise illegal
            return AbilityLegality.IndefinitelyIllegal;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new UnsiegeAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new NullAbilityParameters();
        }
    }
}