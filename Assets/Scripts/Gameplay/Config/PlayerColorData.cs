using Mirror;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configurable data for a player color
    /// </summary>
    [CreateAssetMenu(menuName = "PlayerColorData", fileName = "PlayerColorData", order = 0)]
    public class PlayerColorData : ScriptableObject {
        public string ID;
        public Color TeamColor;
        public ColoredButtonData ColoredButtonData;
        public Color TeamBannerColor;
        public Color DeathParticlesColor1;
        public Color DeathParticlesColor2;
        public Material SelectionMaterial;
    }
    
    public static class PlayerColorDataSerializer {
        public static void WritePlayerData(this NetworkWriter writer, PlayerColorData playerData) {
            writer.WriteString(playerData.ID);
        }

        public static PlayerColorData ReadPlayerData(this NetworkReader reader) {
            PlayerColorData data = GameManager.Instance.Configuration.GetPlayerColor(reader.ReadString());
            return data;
        }
    }
}