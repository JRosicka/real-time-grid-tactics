using System;
using System.Threading.Tasks;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using Mirror;
using UnityEngine;

public class SPCommandManager : AbstractCommandManager {
    public override void Initialize(Transform spawnBucketPrefab, GameEndManager gameEndManager, AbilityAssignmentManager abilityAssignmentManager) {
        SpawnBucket = Instantiate(spawnBucketPrefab);
        AbilityExecutor.Initialize(this, gameEndManager, abilityAssignmentManager);
    }

    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GameTeam team, GridEntity spawnerEntity, bool movementOnCooldown) {
        DoSpawnEntity(data, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(GridEntityPrefab, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            
            entityInstance.ServerInitialize(data, team, spawnLocation); 
            entityInstance.ClientInitialize(data, team);
            
            return entityInstance;
        }, team, spawnerEntity, movementOnCooldown); 
    }
 
    public override void AddUpgrade(UpgradeData data, GameTeam team) {
        DoAddUpgrade(data, team);
    }

    protected override void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        DoRegisterEntity(entity, data, position, entityToIgnore);
    }

    public override async void UnRegisterEntity(GridEntity entity, bool showDeathAnimation) {
        await Task.Delay(TimeSpan.FromSeconds(AbilityExecutor.UpdateFrequency * 2));
        DoMarkEntityUnregistered(entity, showDeathAnimation);
        DoUnRegisterEntity(entity);
    }

    public override void DestroyEntity(GridEntity entity) {
        Destroy(entity.gameObject);
    }

    public override void PerformAbility(IAbility ability, bool clearOtherAbilities, bool fromInput) {
        DoPerformAbility(ability, clearOtherAbilities, fromInput);
        DoUpdateInProgressAbilities(ability.Performer, ability.Performer.InProgressAbilities);     // TODO-abilities is this necessary?
    }

    public override void AbilityEffectPerformed(IAbility ability) {
        DoAbilityEffectPerformed(ability);
    }
    public override void AbilityFailed(IAbility ability) {
        DoAbilityFailed(ability);
    }

    public override void UpdateInProgressAbilities(GridEntity entity) {
        DoUpdateInProgressAbilities(entity, entity.InProgressAbilities);
    }

    public override void ClearAbilities(GridEntity entity) {
        DoClearAbilities(entity);
        DoUpdateInProgressAbilities(entity, entity.InProgressAbilities);
    }

    public override void MarkAbilityCooldownExpired(IAbility ability) {
        DoMarkAbilityCooldownExpired(ability, false);
    }

    public override void CancelAbility(IAbility ability) {
        bool success = DoCancelAbility(ability);
        if (success) {
            DoMarkAbilityCooldownExpired(ability, true);
        }
    }

    public override void UpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metadata) {
        DoUpdateNetworkableField(parent, fieldName, newValue, metadata);
    }
}