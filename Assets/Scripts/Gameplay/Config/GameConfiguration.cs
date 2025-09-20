using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// A collection of all of the game's statically-configured data assets
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/GameConfiguration", fileName = "GameConfiguration")]
    public class GameConfiguration : ScriptableObject {
        [Header("Config")]
        public AudioFileConfiguration AudioConfiguration;
        public CurrencyConfiguration CurrencyConfiguration;
        
        [Tooltip("How much extra F-cost (in seconds to move) past the generated ignoring-other-entities path when constructing an account-for-other-entities path. This will be divided by the entity's normal move time.")]
        public float MaxPathFindingFCostBuffer;
        
        [Header("Player Configs")] 
        public List<PlayerData> PlayerConfigs;
        
        [Header("Misc Sprites")]
        public Sprite CancelButtonSprite;
        public Sprite CancelButtonSlotSprite;
        public Color NeutralBannerColor;
        public Material TargetedMaterial;
        
        [Header("Gameplay ScriptableObjects")]
        public List<PurchasableData> Purchasables;
        public List<AbilityDataScriptableObject> Abilities;
        public List<GameplayTile> Tiles;

        private void OnValidate() {
            if (Purchasables.Select(d => d.ID).Distinct().ToList().Count < Purchasables.Count) {
                Debug.LogError($"Detected entries with duplicate IDs in {Purchasables}. Don't do that!");
            } 
            
            if (Abilities.Select(d => d.name).Distinct().ToList().Count < Abilities.Count) {
                Debug.LogError($"Detected entries with duplicate IDs in {Abilities}. Don't do that!");
            }
            
            if (Tiles.Distinct().ToList().Count < Tiles.Count) {
                Debug.LogError($"Detected duplicate entries in {Tiles}. Don't do that!");
            }
        }

        public PurchasableData GetPurchasable(string id) {
            return Purchasables.First(d => d.ID == id);
        }

        public List<UpgradeData> GetUpgrades() {
            return Purchasables.Where(p => p is UpgradeData).Cast<UpgradeData>().ToList();
        }

        public AbilityDataScriptableObject GetAbility(string id) {
            return Abilities.First(d => d.name == id); 
        }

        public PlayerData GetPlayer(string id) {
            return PlayerConfigs.First(c => c.name == id);
        }
    }
}