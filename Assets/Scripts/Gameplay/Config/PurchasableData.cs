using System.Collections.Generic;
using Gameplay.Entities;
using Mirror;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Base configurable data class for a purchasable thing in the game, like a unit or an upgrade. 
    /// </summary>
    public abstract class PurchasableData : ScriptableObject {

        public string ID => name;
        public string ShortDescription;
        public string Description;

        [Header("Build Requirements")]
        public List<ResourceAmount> Cost;
        /// <summary>
        /// At least one of each of these must be completed in order to build this. An empty list means no requirements.
        /// Note that this is distinct from the build source for this. These required items must exist somewhere, and additionally
        /// some <see cref="GridEntity"/> needs to be able to build this. 
        /// </summary>
        public List<PurchasableData> Requirements;
        public bool RequirementNeedsToBeAdjacent;
        public float BuildTime;
        
        [Header("References")]
        public Sprite BaseSprite;
        public Sprite TeamColorSprite;
        [Tooltip("Optional sprite to use for the base unit icon instead of the BaseSprite")]
        public Sprite BaseSpriteIconOverride;
    }
    
    public static class PurchasableDataSerializer {
        public static void WritePurchasableData(this NetworkWriter writer, PurchasableData data) {
            writer.WriteString(data.name);
        }

        public static PurchasableData ReadPurchasableData(this NetworkReader reader) {
            return GameManager.Instance.Configuration.GetPurchasable(reader.ReadString());
        }
    }
}