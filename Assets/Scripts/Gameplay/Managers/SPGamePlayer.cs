using System.Collections.Generic;
using Gameplay.Config;
using UnityEngine;

public class SPGamePlayer : MonoBehaviour, IGamePlayer {
    public PlayerData Data { get; set; }
    public string DisplayName { get; set; }
    public List<PurchasableData> OwnedPurchasables { get; } = new List<PurchasableData>();
}