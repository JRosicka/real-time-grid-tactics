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
        IAbilityParameters BaseParameters { get; }
    }
 
    public static class AbilitySerializer {
        public static void WriteAbility(this NetworkWriter writer, IAbility ability) {
            writer.WriteString(ability.AbilityData.ContentResourceID);
            writer.Write(ability.BaseParameters);
        }

        public static IAbility ReadAbility(this NetworkReader reader) {
            AbilityDataScriptableObject dataAsset = Resources.Load<AbilityDataScriptableObject>(reader.ReadString());
            // Re-create the ability instance using the data asset we loaded
            IAbility abilityInstance = dataAsset.Content.CreateAbility(reader.Read<IAbilityParameters>());
            return abilityInstance;
        }
    }
}