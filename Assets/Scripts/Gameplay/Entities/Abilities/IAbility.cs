using Gameplay.Config.Abilities;
using Mirror;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// An instance of an ability being used. Gets its data from a corresponding <see cref="AbilityDataBase{T}"/>.
    /// </summary>
    public interface IAbility {
        /// <summary>
        /// Actually perform the ability. Returns false if we were not able to do so (<see cref="IAbilityData.AbilityLegal"/> false).
        /// </summary>
        bool PerformAbility();
        void PayCost(bool justSpecificCost);
        IAbilityData AbilityData { get; }
        IAbilityParameters BaseParameters { get; }
        int UID { get; set; }
        GridEntity Performer { get; }
        /// <summary>
        /// Whether we should wait until the ability is legal before executing. If false, discards the ability when attempting
        /// to execute.
        ///
        /// By "wait", I mean "keep the ability queued, blocking other queued abilities from executing". 
        /// </summary>
        bool WaitUntilLegal { get; set; }
        float CooldownDuration { get; }
        bool CompleteCooldown();
        /// <summary>
        /// Client-side method to determine whether the cooldown timer should be shown for the ability
        /// </summary>
        bool ShouldShowCooldownTimer { get; }
        void Cancel();
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
            writer.WriteBool(ability.WaitUntilLegal);
            ability.SerializeParameters(writer);
        }

        public static IAbility ReadAbility(this NetworkReader reader) {
            AbilityDataScriptableObject dataAsset = GameManager.Instance.Configuration.GetAbility(reader.ReadString());
            int uid = reader.ReadInt();
            bool waitUntilLegal = reader.ReadBool();

            // Re-create the ability instance using the data asset we loaded
            IAbility abilityInstance = dataAsset.Content.DeserializeAbility(reader);
            abilityInstance.UID = uid;
            abilityInstance.WaitUntilLegal = waitUntilLegal;
            return abilityInstance;
        }
    }
}