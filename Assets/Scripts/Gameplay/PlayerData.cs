using UnityEngine;

/// <summary>
/// Configurable data for a single player
/// </summary>
[CreateAssetMenu(menuName = "PlayerData", fileName = "PlayerData", order = 0)]
public class PlayerData : ScriptableObject {
    public GridEntity.Team Team;
    public Color TeamColor;
    public Vector3Int SpawnLocation;
}