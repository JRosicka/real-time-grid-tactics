using System.Collections.Generic;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Mirror;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configuration for a <see cref="GridEntity"/>
    /// </summary>
    [CreateAssetMenu(menuName = "Grid Entities/EntityData", fileName = "EntityData", order = 0)]
    public class EntityData : PurchasableData {
        public enum EntityTag {
            Cavalry, 
            Flying
        }
        
        public Sprite TeamColorSprite;
        
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
    }

    public static class EntityDataSerializer {
        public static void WriteEntityData(this NetworkWriter writer, EntityData data) {    // TODO currently the asset needs to be stored at the root of a Resources folder for this to work. Would be nice to record the path somewhere so that we could nest these. 
            writer.WriteString(data.name);
        }

        public static EntityData ReadEntityData(this NetworkReader reader) {    // TODO cache the loaded resource here and everywhere else
            string id = reader.ReadString();
            EntityData data = (EntityData)Resources.Load(id);
            return data;
        }
    }
}