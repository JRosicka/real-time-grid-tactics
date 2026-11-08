using System;
using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/SiegeAbilityData")]
    public class SiegeDataAsset : BaseAbilityDataAsset<SiegeAbilityData, NullAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for entering siege mode
    /// </summary>
    [Serializable]
    public class SiegeAbilityData : AbilityDataBase<NullAbilityParameters> {
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

            // Not legal if already sieged
            if (entity.SiegeState == SiegeState.Sieged) {
                return AbilityLegality.IndefinitelyIllegal;
            }
            
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new SiegeAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new NullAbilityParameters();
        }
    }
}