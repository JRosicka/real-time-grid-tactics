using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Upgrades;
using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using Mirror;
using UnityEngine;

/// <summary>
/// Monitors a single <see cref="IGamePlayer"/>'s owned purchasables (entities and upgrades), updating when any are bought or destroyed
/// </summary>
public class PlayerOwnedPurchasablesController : NetworkBehaviour {
    /// <summary>
    /// The currently active owned purchasables has been updated (something added or removed).
    /// Triggers on clients. 
    /// </summary>
    public event Action OwnedPurchasablesChangedEvent;

    public UpgradesCollection Upgrades { get; private set; }
    public List<UpgradeData> InProgressUpgrades => Upgrades.GetInProgressUpgrades();
    
    private IGamePlayer _player;

    /// <summary>
    /// Client call
    /// </summary>
    public void Initialize(IGamePlayer player, List<UpgradeData> upgradesToRegister) {
        _player = player;
        Upgrades = new UpgradesCollection(_player.Data.Team);
        Upgrades.RegisterUpgrades(upgradesToRegister);
        
        if (NetworkServer.active || !NetworkClient.active) {
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
                .ActiveEntitiesForTeam(_player.Data.Team).Select(e => e.EntityData).Cast<PurchasableData>().ToList();
            return entityData.Concat(Upgrades.GetOwnedUpgrades()).ToList();
        }
    }

    public bool HasRequirementsForPurchase(PurchasableData purchasable, GridEntity purchaser, out string whyNot) {
        List<PurchasableData> ownedPurchasables = OwnedPurchasables;
        foreach (PurchasableData requiredPurchasable in purchasable.Requirements) {
            if (!ownedPurchasables.Contains(requiredPurchasable)) {
                whyNot = $"Requires a {requiredPurchasable.ID}.";
                return false;
            }
            if (purchasable.RequirementNeedsToBeAdjacent) {
                if (requiredPurchasable != GameManager.Instance.Configuration.KingEntityData) {
                    throw new Exception("Game does not support a non-King adjacent required purchasable");
                }

                if (!GameManager.Instance.LeaderTracker.IsAdjacentToFriendlyLeader(purchaser.Location!.Value, _player.Data.Team)) {
                    whyNot = $"Your {requiredPurchasable.ID} must be adjacent.";
                    return false;
                }
            }
        }

        whyNot = null;
        return true;
    }

    public bool HasUpgrade(UpgradeData upgrade) {
        return Upgrades.GetOwnedUpgrades().Contains(upgrade);
    }
    
    public void UpdateUpgradeStatus(UpgradeData upgradeData, UpgradeStatus newStatus) {
        IUpgrade upgrade = Upgrades.GetUpgrade(upgradeData);
        upgrade.UpdateStatus(newStatus);
    }
    
    public void ExpireUpgradeTimer(UpgradeData upgradeData) {
        if (Upgrades.ExpireUpgradeTimer(upgradeData)) {
            OwnedPurchasablesChangedEvent?.Invoke();
        }
    }

    private void OwnedPurchasablesMayHaveChanged(GameTeam team) {
        if (team != _player.Data.Team) return;
        
        NotifyOwnedPurchasablesChanged();
    }

    private void NotifyOwnedPurchasablesChanged() {
        if (NetworkClient.active) {
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
