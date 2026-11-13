using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// A collection of <see cref="EntityTag"/>s and their associated data
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/EntityTagDatabase", fileName = "EntityTagDatabase")]
    public class EntityTagDatabase : ScriptableObject {
        public List<EntityTagData> EntityTags;

        public EntityTagData GetEntityTag(EntityTag tag) {
            return EntityTags.FirstOrDefault(d => d.Tag == tag);
        }
    }

    [Serializable]
    public class EntityTagData {
        public EntityTag Tag;
        public string Description;
        public bool Visible;    // Whether this gets shown in the selection interface
    }
}