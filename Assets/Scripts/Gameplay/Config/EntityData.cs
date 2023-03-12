using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.UI;
using Mirror;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configuration for a <see cref="GridEntity"/>
    /// </summary>
    [CreateAssetMenu(menuName = "Grid Entities/EntityData", fileName = "EntityData", order = 0)]
    public class EntityData : PurchasableData {
        public enum EntityTag {
            Structure = 1,
            Cavalry = 2, 
            Flying = 4,
        }
        
        // Must be private so that Weaver does not try to make a reader and writer for this type. Mirror does this for all public fields, thanks Mirror. 
        [SerializeField]
        private GridEntityViewBase _viewPrefab;
        public GridEntityViewBase ViewPrefab => _viewPrefab;
        
        [Header("Stats")] 
        public int HP;
        public int MaxMove;
        public int Range;
        public int Damage;

        [Space] 
        public List<EntityTag> Tags;
        public List<AbilityDataScriptableObject> Abilities;
        [Tooltip("Whether friendly (non-structure) entities can enter (spawn, move, etc) a cell with this entity")]
        public bool FriendlyUnitsCanShareCell;

        /// <summary>
        /// The order that this should appear and be selectable compared to other entities at the same location.
        /// Lower values appear on top of higher values and are selected first. 
        /// </summary>
        public int GetStackOrder() {
            return Tags.Contains(EntityTag.Structure) ? CanvasSortingOrderMap.GridEntity_Structure : CanvasSortingOrderMap.GridEntity_Unit;
        }
        
        private void OnValidate() {
            if (Abilities.Select(a => a.Content.Channel).Distinct().ToList().Count < Abilities.Count) {
                string channelNames = Abilities.Select(a => a.Content.Channel).Aggregate("", (current, channel) => current + channel.name + ", ");
                Debug.LogError($"{name}: Detected abilities with the same channel, don't do that! {channelNames}");
            } 

            if (FriendlyUnitsCanShareCell && !Tags.Contains(EntityTag.Structure)) {
                Debug.LogError($"{name}: Woah woah woah buddy, if you want this entity to be able to share a cell with other entities, then it's gotta be a structure!");
            }
        }
    }

    public static class EntityDataSerializer {
        public static void WriteEntityData(this NetworkWriter writer, EntityData data) { 
            writer.WriteString(data.name);
        }

        public static EntityData ReadEntityData(this NetworkReader reader) {
            return (EntityData) GameManager.Instance.Configuration.GetPurchasable(reader.ReadString());
        }
    }
}