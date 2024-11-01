using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Mirror;

/// <summary>
/// Monitors a single <see cref="IGamePlayer"/>'s owned purchasables (entities and upgrades), updating when any are bought or destroyed
/// </summary>
public class PlayerOwnedPurchasablesController : NetworkBehaviour {
    /// <summary>
    /// The currently active owned purchasables has been updated (something added or removed).
    /// Triggers on clients. TODO test this. 
    /// </summary>
    public event Action OwnedPurchasablesChangedEvent;

    private GridEntity.Team _team;

    [SyncVar]
    private UpgradesCollection _upgrades = new UpgradesCollection();

    /// <summary>
    /// Server call
    /// </summary>
    public void Initialize(GridEntity.Team team, List<UpgradeData> upgradesToRegister) {
        _team = team;
        _upgrades.RegisterUpgrades(upgradesToRegister);
        GameManager.Instance.CommandManager.EntityRegisteredEvent += OwnedPurchasablesMayHaveChanged;
        GameManager.Instance.CommandManager.EntityUnregisteredEvent += OwnedPurchasablesMayHaveChanged;
    }
    
    public List<PurchasableData> OwnedPurchasables {
        get {
            List<PurchasableData> entityData = GameManager.Instance.CommandManager.EntitiesOnGrid
                .ActiveEntitiesForTeam(_team).Select(e => e.EntityData).Cast<PurchasableData>().ToList();
            return entityData.Concat(_upgrades.GetOwnedUpgrades()).ToList();
        }
    }

    public List<UpgradeData> InProgressUpgrades => _upgrades.GetInProgressUpgrades();

    public void AddUpgrade(UpgradeData upgrade) {
        if (_upgrades.AddUpgrade(upgrade)) {
            SyncUpgrades();
            OwnedPurchasablesMayHaveChanged(_team);
        }
    }

    public void AddInProgressUpgrade(UpgradeData upgrade) {
        if (_upgrades.AddInProgressUpgrade(upgrade)) {
            SyncUpgrades();
            OwnedPurchasablesMayHaveChanged(_team);
        }
    }
    
    public void CancelInProgressUpgrade(UpgradeData upgrade) {
        if (_upgrades.CancelInProgressUpgrade(upgrade)) {
            SyncUpgrades();
            OwnedPurchasablesMayHaveChanged(_team);
        }
    }

    /// <summary>
    /// Reset the reference for <see cref="_upgrades"/> to force a sync across clients. Just updating fields in the class
    /// is not enough to get the sync to occur. 
    /// </summary>
    private void SyncUpgrades() {    // TODO: Kinda yucky. 
        _upgrades = new UpgradesCollection(_upgrades.UpgradesDict);
    }

    private void OwnedPurchasablesMayHaveChanged(GridEntity.Team team) {
        if (team != _team) return;
        
        if (!NetworkClient.active) {
            // SP, so trigger manually.
            OwnedPurchasablesChangedEvent?.Invoke();
        } else {
            // MP, so update on each client. 
            RpcOwnedPurchasablesChanged();
        }
    }

    [ClientRpc]
    private void RpcOwnedPurchasablesChanged() {
        OwnedPurchasablesChangedEvent?.Invoke();
    }
}
