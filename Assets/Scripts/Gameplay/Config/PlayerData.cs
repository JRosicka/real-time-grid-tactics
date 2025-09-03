using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
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
    
    public static class PlayerDataSerializer {
        public static void WritePlayerData(this NetworkWriter writer, PlayerData playerData) {
            writer.WriteString(playerData.name);
        }

        public static PlayerData ReadPlayerData(this NetworkReader reader) {
            PlayerData data = GameManager.Instance.Configuration.GetPlayer(reader.ReadString());
            return data;
        }
    }
}