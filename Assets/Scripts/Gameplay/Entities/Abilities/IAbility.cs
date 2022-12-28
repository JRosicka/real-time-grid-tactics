using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// An instance of an ability being used. Gets its data from a corresponding <see cref="AbilityDataBase{T}"/>.
    /// </summary>
    public interface IAbility {
        /// <summary>
        /// Actually perform the ability. Returns false if we were not able to do so (<see cref="IAbilityData.AbilityLegal"/> false).
        /// </summary>
        bool PerformAbility();
        IAbilityData AbilityData { get; }
        int UID { get; set; }
        GridEntity Performer { get; }
        /// <summary>
        /// Write the <see cref="IAbility"/>'s parameters to the provided writer so that it can be properly networked. Other than the data
        /// which is retrieved by loading the asset from Resources, the parameters are the only thing holding the state of
        /// this <see cref="IAbility"/>
        /// </summary>
        /// <param name="writer"></param>
        void SerializeParameters(NetworkWriter writer);
    }
 
    public static class AbilitySerializer {
        public static void WriteAbility(this NetworkWriter writer, IAbility ability) {
            writer.WriteString(ability.AbilityData.ContentResourceID);
            writer.WriteInt(ability.UID);
            ability.SerializeParameters(writer);
        }

        public static IAbility ReadAbility(this NetworkReader reader) {
            AbilityDataScriptableObject dataAsset = Resources.Load<AbilityDataScriptableObject>(reader.ReadString());
            int uid = reader.ReadInt();
            // Re-create the ability instance using the data asset we loaded
            IAbility abilityInstance = dataAsset.Content.DeserializeAbility(reader);
            abilityInstance.UID = uid;
            return abilityInstance;
        }
    }
}