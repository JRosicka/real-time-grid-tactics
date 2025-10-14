using System.Collections.Generic;
using System.Linq;
using Audio;
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
        public enum TargetPriority {
            Structure = 0,
            Worker = 1,
            Fighter = 2
        }
        
        // Must be private so that Weaver does not try to make a reader and writer for this type. Mirror does this for all public fields, thanks Mirror. 
        [SerializeField]
        private GridEntityView _viewPrefab;
        public GridEntityView ViewPrefab => _viewPrefab;

        [Header("Audio")] 
        public List<AudioFile> SelectionSounds;
        public List<AudioFile> OrderSounds;
        public List<AudioFile> AttackSounds;
        public AudioFile EntityFinishedBuildingSound;
        
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
        [Tooltip("How much time is added to the entity's movement cooldown timer after attacking. " +
                 "If there is an active cooldown timer for movement when an attack is performed, then this amount " +
                 "is added to that timer. Otherwise, a new cooldown timer is added with this amount.")]
        [Range(0f, 5f)]
        public float AddedMovementTimeFromAttacking;

        [Space] 
        public List<EntityTag> Tags;
        public TargetPriority AttackerTargetPriority;
        public List<AbilityDataScriptableObject> Abilities;
        [Tooltip("Whether friendly (non-structure) entities can enter (spawn, move, etc) a cell with this entity")]
        public bool FriendlyUnitsCanShareCell;

        public bool AttackByDefault;

        public bool IsStructure => Tags.Contains(EntityTag.Structure);
        public bool IsResourceExtractor;
        public EntityData ResourceThatThisCanExtract;
        public bool MovementAndAttackUI;
        public bool DisplaySelectionOutline = true;

        public int BuildQueueSize = 5;
        public bool CanBuild => Abilities.Any(a => a.Content is BuildAbilityData);
        public bool NeverShowBuildQueue;
        public bool ControllableByAllPlayers;
        
        [Header("Structure config")]
        [Tooltip("Where this can be build. Relevant for structures only.")]
        public List<GameplayTile> EligibleStructureLocations;
        [Tooltip("Armor bonus for units on the structure who are taking damage. Relevant for structures only.")]
        [Range(0, 6)]
        public int SharedUnitArmorBonus;
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
        }
    }

    public static class EntityDataSerializer {
        public static void WriteEntityData(this NetworkWriter writer, EntityData data) { 
            writer.WriteString(data.ID);
        }

        public static EntityData ReadEntityData(this NetworkReader reader) {
            return (EntityData) GameManager.Instance.Configuration.GetPurchasable(reader.ReadString());
        }
    }
}