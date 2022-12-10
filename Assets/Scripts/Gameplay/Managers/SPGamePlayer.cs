using UnityEngine;

public class SPGamePlayer : MonoBehaviour, IGamePlayer {
    public PlayerData Data { get; set; }
    public string DisplayName { get; set; }
}