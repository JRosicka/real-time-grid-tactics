using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Mirror;
using UnityEngine;

public enum ResourceType {
    Basic,
    Advanced
}

public static class ResourceTypeExtensions {
    public static string DisplayName(this ResourceType resourceType) {
        return resourceType switch {
            ResourceType.Basic => "Gold",
            ResourceType.Advanced => "Spirit-Amber",
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null)
        };
    }
    
    public static string DisplayIcon(this ResourceType resourceType) {
        return resourceType switch {
            ResourceType.Basic => TextIcons.Gold,
            ResourceType.Advanced => TextIcons.Amber,
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null)
        };
    }
}
    
[Serializable]
public class ResourceAmount : INetworkableFieldValue {
    public string ID => nameof(ResourceAmount);

    public ResourceType Type;
    public int Amount;

    public ResourceAmount() {}

    public ResourceAmount(ResourceAmount other) {
        Type = other.Type;
        Amount = other.Amount;
    }
    
    public void SerializeValue(NetworkWriter writer) {
        writer.WriteInt((int)Type);
        writer.WriteInt(Amount);
    }

    public static ResourceAmount Deserialize(NetworkReader reader) {
        return new ResourceAmount {
            Type = (ResourceType)reader.ReadInt(),
            Amount = reader.ReadInt()
        };
    }
}

/// <summary>
/// Manages a single <see cref="IGamePlayer"/>'s resources. Resources can be earned and spent here. 
/// </summary>
public class PlayerResourcesController : NetworkBehaviour {
    public event Action<List<ResourceAmount>> BalanceChangedEvent;
    
    [SyncVar(hook = nameof(OnBalanceChanged))]
    private List<ResourceAmount> _balances = new List<ResourceAmount> {
        new ResourceAmount {Type = ResourceType.Basic},
        new ResourceAmount {Type = ResourceType.Advanced}
    };

    private void OnBalanceChanged(List<ResourceAmount> oldValue, List<ResourceAmount> newValue) {
        BalanceChangedEvent?.Invoke(newValue);
    }

    public void Initialize(CurrencyConfiguration currencyConfiguration) {
        int startingGold = currencyConfiguration.StartingGoldAmount;
        if (startingGold > 0) {
            Earn(new ResourceAmount {
                Type = ResourceType.Basic,
                Amount = startingGold
            });
        }

        int startingAmber = currencyConfiguration.StartingAmberAmount;
        if (startingAmber > 0) {
            Earn(new ResourceAmount {
                Type = ResourceType.Advanced,
                Amount = startingAmber
            });
        }
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
        if (!NetworkClient.active) {
            // SP
            DoSetBalance(newBalance);
        } else {
            // MP
            CmdSetBalance(newBalance);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdSetBalance(ResourceAmount newBalance) {
        DoSetBalance(newBalance);
    }

    private void DoSetBalance(ResourceAmount newBalance) {
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
            // MP host.  
            CmdUpdateBalances();
        }
    }
    
    [Command(requiresAuthority = false)]
    private void CmdUpdateBalances() {
        // Reset the reference for <see cref="_balances"/> to force a sync across clients. Just updating fields in the class
        // is not enough to get the sync to occur...
        _balances = new List<ResourceAmount> {_balances[0], _balances[1]};
    }
}