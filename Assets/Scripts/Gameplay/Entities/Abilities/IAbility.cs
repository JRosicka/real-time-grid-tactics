using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// An instance of an ability being used. Gets its data from a corresponding <see cref="AbilityDataBase{T}"/>.
    /// </summary>
    public interface IAbility {
        /// <summary>
        /// Try to actually perform the ability
        /// </summary>
        AbilityResult PerformAbility();
        /// <summary>
        /// Try to pay the associated up-front cost (i.e. when we try to perform the ability) for the ability
        /// </summary>
        /// <returns>True if payed, otherwise false if could not be payed (i.e. can not afford)</returns>
        bool TryPayUpFrontCost();
        IAbilityData AbilityData { get; }
        IAbilityParameters BaseParameters { get; }
        int UID { get; set; }
        GridEntity Performer { get; }
        AbilityExecutionType ExecutionType { get; }
        float CooldownDuration { get; }
        bool CompleteCooldown();
        /// <summary>
        /// Client-side method to determine whether the cooldown timer should be shown for the ability
        /// </summary>
        bool ShouldShowCooldownTimer { get; }
        void Cancel();
        /// <summary>
        /// Start performing the ability after this ability (by id) finishes
        /// </summary>
        int QueuedAfterAbilityID { get; set; }

        /// <summary>
        /// Write the <see cref="IAbility"/>'s parameters to the provided writer so that it can be properly networked. Other than the data
        /// which is retrieved by loading the asset from Resources, the parameters are the only thing holding the state of
        /// this <see cref="IAbility"/>
        /// </summary>
        /// <param name="writer"></param>
        void SerializeParameters(NetworkWriter writer);
        void DeserializeImpl(NetworkReader reader);
    }
 
    public static class AbilitySerializer {
        public static void WriteAbility(this NetworkWriter writer, IAbility ability) {
            writer.WriteString(ability.AbilityData.ContentResourceID);
            writer.WriteInt(ability.UID);
            writer.WriteInt(ability.QueuedAfterAbilityID);
            ability.SerializeParameters(writer);
        }

        public static IAbility ReadAbility(this NetworkReader reader) {
            AbilityDataScriptableObject dataAsset = GameManager.Instance.Configuration.GetAbility(reader.ReadString());
            int uid = reader.ReadInt();
            int abilityUIDThisIsQueuedAfter = reader.ReadInt();
            
            // Re-create the ability instance using the data asset we loaded
            IAbility abilityInstance = dataAsset.Content.DeserializeAbility(reader);
            abilityInstance.UID = uid;
            abilityInstance.QueuedAfterAbilityID = abilityUIDThisIsQueuedAfter; 
            return abilityInstance;
        }
    }
}