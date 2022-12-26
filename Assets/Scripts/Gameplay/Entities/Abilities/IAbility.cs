using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// An instance of an ability being used. Gets its data from a corresponding <see cref="AbilityDataBase{T}"/>.
    /// </summary>
    public interface IAbility {
        void PerformAbility();
        IAbilityData AbilityData { get; }
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
            ability.SerializeParameters(writer);
        }

        public static IAbility ReadAbility(this NetworkReader reader) {
            AbilityDataScriptableObject dataAsset = Resources.Load<AbilityDataScriptableObject>(reader.ReadString());
            // Re-create the ability instance using the data asset we loaded
            IAbility abilityInstance = dataAsset.Content.DeserializeAbility(reader);
            return abilityInstance;
        }
    }
}