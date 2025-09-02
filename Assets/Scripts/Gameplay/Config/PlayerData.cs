using Gameplay.Entities;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configurable data for a single player
    /// </summary>
    [CreateAssetMenu(menuName = "PlayerData", fileName = "PlayerData", order = 0)]
    public class PlayerData : ScriptableObject {
        public GameTeam Team;
        public Color TeamColor;
        public Sprite SlotTeamSprite;
        public Color TeamBannerColor;
        public Color DeathParticlesColor1;
        public Color DeathParticlesColor2;
        public Vector2Int SpawnLocation;
    }
}