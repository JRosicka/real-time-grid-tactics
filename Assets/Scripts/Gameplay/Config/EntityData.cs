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
            Infantry = 3,
            Flying = 4,
            HomeBase = 5,
            Worker = 6,
            Resource = 7,
        }
        
        // Must be private so that Weaver does not try to make a reader and writer for this type. Mirror does this for all public fields, thanks Mirror. 
        [SerializeField]
        private GridEntityView _viewPrefab;
        public GridEntityView ViewPrefab => _viewPrefab;
        
        [Header("Stats")] 
        public int HP;
        public int Range;
        public int Damage;
        public int BonusDamage;
        public List<EntityTag> TagsToApplyBonusDamageTo;

        [Header("Movement")]
        // NOTE: if we add faster movement times here, then update GridNode._fastestEnterTime calculation accordingly
        // These should have values for production structures even though those can't move. This is so that the pathfinding 
        // logic works correctly when setting rally points.
        public float NormalMoveTime;
        public float SlowMoveTime;

        [Space] 
        public List<EntityTag> Tags;
        public List<AbilityDataScriptableObject> Abilities;
        [Tooltip("Whether friendly (non-structure) entities can enter (spawn, move, etc) a cell with this entity")]
        public bool FriendlyUnitsCanShareCell;

        public bool AttackByDefault;

        public bool IsStructure => Tags.Contains(EntityTag.Structure);
        public bool IsResourceExtractor;

        public int BuildQueueSize = 5;
        public bool CanBuild => Abilities.Any(a => a.Content is BuildAbilityData);
        
        [Header("Structure config")]
        [Tooltip("Where this can be build. Relevant for structures only.")]
        public List<GameplayTile> EligibleStructureLocations;
        [Tooltip("Damage multiplier for units on the structure who are taking damage. Relevant for structures only.")]
        [Range(0, 1)]
        public float SharedUnitDamageTakenModifier = 1;
        [Tooltip("Tags that shared unit damage taken modifier gets applied to, for units sharing this cell. An empty list makes the damage modifier get applied to everyone. Relevant for structures only.")]
        public List<EntityTag> SharedUnitDamageTakenModifierTags;
        public bool CanRally;
        public ResourceAmount StartingResourceSet;
        
        /// <summary>
        /// The order that this should appear and be selectable compared to other entities at the same location.
        /// Lower values appear on top of higher values and are selected first. 
        /// </summary>
        public int GetStackOrder() {
            return Tags.Contains(EntityTag.Resource) ? CanvasSortingOrderMap.GridEntity_Resource
                : IsStructure ? CanvasSortingOrderMap.GridEntity_Structure 
                : CanvasSortingOrderMap.GridEntity_Unit;
        }
        
        private void OnValidate() {
            if (Abilities.Select(a => a.Content.SlotLocation)
                    .Distinct()
                    .Where(s => s != AbilitySlotLocation.Unpicked)
                    .ToList()
                    .Count < Abilities.Where(a => a.Content.SlotLocation != AbilitySlotLocation.Unpicked)
                    .ToList().Count) {
                string channelNames = Abilities.Select(a => a.Content.SlotLocation).Aggregate("", (current, slotLocation) => current + slotLocation + ", ");
                Debug.LogError($"{name}: Detected abilities with the same slot location, don't do that! {channelNames}");
            } 

            if (FriendlyUnitsCanShareCell && !IsStructure) {
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