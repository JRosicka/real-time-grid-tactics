using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Upgrades;
using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using JetBrains.Annotations;
using Mirror;
using Scenes;
using UnityEngine;

/// <summary>
/// Monitors a single <see cref="IGamePlayer"/>'s owned purchasables (entities and upgrades), updating when any are bought or destroyed
/// </summary>
public class PlayerOwnedPurchasablesController : NetworkBehaviour {
    /// <summary>
    /// The currently active owned purchasables has been updated (something added or removed).
    /// Triggers on all clients. 
    /// </summary>
    public event Action OwnedPurchasablesChangedEvent;
    /// <summary>
    /// An upgrade has been completed.
    /// Triggers on all clients. 
    /// </summary>
    public event Action<UpgradeData, GridEntity, GameTeam> UpgradeCompletedEvent;

    public UpgradesCollection Upgrades { get; private set; }
    public List<UpgradeData> InProgressUpgrades => Upgrades.GetInProgressUpgrades();
    
    private IGamePlayer _player;

    /// <summary>
    /// Client call
    /// </summary>
    public void Initialize(IGamePlayer player, List<UpgradeData> upgradesToRegister) {
        _player = player;
        Upgrades = new UpgradesCollection(_player.Team);
        Upgrades.RegisterUpgrades(upgradesToRegister);
        
        if (GameTypeTracker.Instance.HostForNetworkedGame || !GameTypeTracker.Instance.GameIsNetworked) {
            // MP server or SP
            GameManager.Instance.CommandManager.EntityRegisteredEvent += OwnedPurchasablesMayHaveChanged;
            GameManager.Instance.CommandManager.EntityUnregisteredEvent += OwnedPurchasablesMayHaveChanged;
        }
    }

    private void Update() {
        Upgrades?.UpdateUpgradeTimers(Time.deltaTime);
    }

    public List<PurchasableData> OwnedPurchasables {
        get {
            List<PurchasableData> entityData = GameManager.Instance.CommandManager.EntitiesOnGrid
                .ActiveEntitiesForTeam(_player.Team).Select(e => e.EntityData).Cast<PurchasableData>().ToList();
            return entityData.Concat(Upgrades.GetOwnedUpgradeDatas()).ToList();
        }
    }

    /// <summary>
    /// Whether we can legally purchase the purchasable based on it requirements.
    /// <see cref="buildLocation"/> can be provided or null. If null, we don't care about the location. If provided,
    /// then any adjacency requirements will use it to determine if adjacent entities exist.
    /// </summary>
    public bool HasRequirementsForPurchase(PurchasableData purchasable, Vector2Int? buildLocation, out string whyNot) {
        List<PurchasableData> ownedPurchasables = OwnedPurchasables;
        foreach (PurchasableRequirement requirement in purchasable.Requirements) {
            if (!ownedPurchasables.Contains(requirement.Purchasable) && (requirement.AlternativePurchasable == null || !ownedPurchasables.Contains(requirement.AlternativePurchasable))) {
                whyNot = requirement.FailedRequirementExplanation;
                return false;
            }
            if (requirement.MustBeAdjacent && buildLocation != null) {
                if (requirement.Purchasable == GameManager.Instance.Configuration.KingEntityData) {
                    // An adjacent King is required
                    if (!GameManager.Instance.LeaderTracker.IsAdjacentToFriendlyLeader(buildLocation.Value, _player.Team)) {
                        whyNot = requirement.FailedRequirementExplanation;
                        return false;
                    }
                } else {
                    // TODO check for requirement adjacency to buildLocation
                }
            }
        }

        whyNot = null;
        return true;
    }

    public bool HasUpgrade(UpgradeData upgrade) {
        return Upgrades.GetOwnedUpgradeDatas().Contains(upgrade);
    }
    
    public void UpdateUpgradeStatus(UpgradeData upgradeData, [CanBeNull] GridEntity performer, GameTeam team, UpgradeStatus newStatus) {
        IUpgrade upgrade = Upgrades.GetUpgrade(upgradeData);
        upgrade.UpdateStatus(newStatus);
        OwnedPurchasablesChangedEvent?.Invoke();
        if (newStatus == UpgradeStatus.Owned) {
            UpgradeCompletedEvent?.Invoke(upgradeData, performer, team);
        }
    }
    
    public void ExpireUpgradeTimer(UpgradeData upgradeData) {
        if (Upgrades.ExpireUpgradeTimer(upgradeData)) {
            OwnedPurchasablesChangedEvent?.Invoke();
        }
    }

    private void OwnedPurchasablesMayHaveChanged(GameTeam team) {
        if (team != _player.Team) return;
        
        NotifyOwnedPurchasablesChanged();
    }

    private void NotifyOwnedPurchasablesChanged() {
        if (GameTypeTracker.Instance.GameIsNetworked) {
            RpcOwnedPurchasablesChanged();
        } else {
            // SP, so trigger manually.
            OwnedPurchasablesChangedEvent?.Invoke();
        }
    }

    [ClientRpc]
    private void RpcOwnedPurchasablesChanged() {
        OwnedPurchasablesChangedEvent?.Invoke();
    }
}
