using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public enum ResourceType {
    Basic,
    Advanced
}
    
[Serializable]
public struct ResourceAmount {
    public ResourceType Type;
    public int Amount;
}

/// <summary>
/// Manages a single <see cref="IGamePlayer"/>'s resources. Resources can be earned and spent here. 
/// </summary>
public class PlayerResourcesController : NetworkBehaviour {
    public event Action<List<ResourceAmount>> BalanceChangedEvent;
    
    [SyncVar(hook = nameof(OnBalanceChanged))]
    private SyncList<ResourceAmount> _balances = new SyncList<ResourceAmount> {
        new ResourceAmount {Type = ResourceType.Basic, Amount = 0},
        new ResourceAmount {Type = ResourceType.Advanced, Amount = 0}
    };

    private void OnBalanceChanged(List<ResourceAmount> oldValue, List<ResourceAmount> newValue) {
        BalanceChangedEvent?.Invoke(newValue);
    }

    public ResourceAmount GetBalance(ResourceType type) {
        return _balances.First(b => b.Type == type);
    }

    public bool CanAfford(List<ResourceAmount> cost) {
        if (cost.Select(c => c.Type).Distinct().Count() < cost.Count) {
            Debug.LogWarning("Woah, you're giving a cost collection containing multiple elements with the same resource type? This probably isn't going to be accurate then. Don't do that.");
        }

        return cost.All(resourceAmount => GetBalance(resourceAmount.Type).Amount >= resourceAmount.Amount);
    }

    public void Earn(ResourceAmount amountToEarn) {
        if (amountToEarn.Amount <= 0) {
            Debug.LogWarning($"Tried to earn {amountToEarn.Amount} resources - no op.");
            return;
        }

        ResourceAmount currentBalance = GetBalance(amountToEarn.Type);
        currentBalance.Amount += amountToEarn.Amount;
        SetBalance(currentBalance);

        UpdateBalances();
    }

    public void Spend(List<ResourceAmount> amountToSpend) {
        if (amountToSpend.Any(a => a.Amount < 0)) {
            Debug.LogWarning("Tried to spend a negative amount of resources - no op.");
            return;
        }

        if (!CanAfford(amountToSpend)) {
            throw new Exception("Tried to spend resources, but we have an insufficient amount!");
        }

        foreach (ResourceAmount resourceAmount in amountToSpend) {
            ResourceAmount currentBalance = GetBalance(resourceAmount.Type);
            currentBalance.Amount -= resourceAmount.Amount;
            SetBalance(currentBalance);
        }
        
        UpdateBalances();
    }
    
    private void SetBalance(ResourceAmount newBalance) {
        ResourceAmount currentBalance = _balances.First(b => b.Type == newBalance.Type);
        _balances.Remove(currentBalance);

        currentBalance.Amount = newBalance.Amount;
        _balances.Add(currentBalance);
    }

    private void UpdateBalances() {
        if (!NetworkClient.active) {
            // SP, so syncvars won't work... Trigger manually.
            BalanceChangedEvent?.Invoke(_balances.ToList());
        } else {
            // MP. Reset the reference for <see cref="_balances"/> to force a sync across clients. Just updating fields in the class
            // is not enough to get the sync to occur... 
            _balances = new SyncList<ResourceAmount> {_balances[0], _balances[1]};
        }

        foreach (ResourceAmount resourceAmount in _balances) {
            Debug.Log($"Balance for {resourceAmount.Type}: {resourceAmount.Amount}");
        }
    }
}