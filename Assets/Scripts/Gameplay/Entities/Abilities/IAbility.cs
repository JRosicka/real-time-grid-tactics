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
        /// Try to do the effect for when this ability has just started
        /// </summary>
        /// <returns>True if successful, otherwise false if this could not be done for whatever reason (i.e. can not afford)</returns>
        bool TryDoAbilityStartEffect();
        IAbilityData AbilityData { get; }
        IAbilityParameters BaseParameters { get; }
        string UID { get; set; }
        GridEntity Performer { get; }
        /// <summary>
        /// This can be different from the performer entity's team if the performing entity is a shared neutral entity
        /// </summary>
        GameTeam PerformerTeam { get; set; }
        AbilityExecutionType ExecutionType { get; }
        float CooldownDuration { get; }
        bool CompleteCooldown();
        /// <summary>
        /// Client-side method to determine whether the cooldown timer should be shown for the ability
        /// </summary>
        bool ShouldShowAbilityTimer { get; }
        bool Cancelable { get; }
        void Cancel();
        /// <summary>
        /// Start performing the ability after this ability (by id) finishes
        /// </summary>
        string QueuedAfterAbilityID { get; set; }

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
            writer.WriteString(ability.UID);
            writer.WriteString(ability.QueuedAfterAbilityID);
            writer.WriteInt((int)ability.PerformerTeam);
            ability.SerializeParameters(writer);
        }

        public static IAbility ReadAbility(this NetworkReader reader) {
            AbilityDataScriptableObject dataAsset = GameManager.Instance.Configuration.GetAbility(reader.ReadString());
            string uid = reader.ReadString();
            string abilityUIDThisIsQueuedAfter = reader.ReadString();
            GameTeam team = (GameTeam)reader.ReadInt();
            
            // Re-create the ability instance using the data asset we loaded
            IAbility abilityInstance = dataAsset.Content.DeserializeAbility(reader);
            abilityInstance.UID = uid;
            abilityInstance.QueuedAfterAbilityID = abilityUIDThisIsQueuedAfter; 
            abilityInstance.PerformerTeam = team;
            return abilityInstance;
        }
    }
}