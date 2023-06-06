using Gameplay.Entities.Abilities;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    /// <summary>
    /// A base asset that holds the configured content for a particular <see cref="IAbility"/>.
    /// </summary>
    public abstract class BaseAbilityDataAsset<T, P> : AbilityDataScriptableObject where T : AbilityDataBase<P> where P : IAbilityParameters, new() {
        public T Data;
        public override IAbilityData Content => Data;
    }

    /// <summary>
    /// Represents the content for a particular <see cref="IAbility"/>. This only exists so that we can serialize a collection of
    /// these in the editor for convenient configuration purposes. We can not do this with <see cref="AbilityBase{T,P}"/>
    /// because that uses a generic type.
    /// </summary>
    public abstract class AbilityDataScriptableObject : ScriptableObject {
        public abstract IAbilityData Content { get; }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(AbilityDataScriptableObject), true)]
    public class NetworkBehaviourInspector : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            // Set the data content ID to be the name of the ScriptableObject asset.
            AbilityDataScriptableObject abilityData = (AbilityDataScriptableObject) target;
            abilityData.Content.ContentResourceID = abilityData.name;
            
            EditorUtility.SetDirty(abilityData);
        }
    }
#endif

    public static class AbilityDataSerializer {
        public static void WriteAbilityDataScriptableObject(this NetworkWriter writer, AbilityDataScriptableObject abilityData) {
            writer.WriteString(abilityData.name);
        }

        public static AbilityDataScriptableObject ReadAbilityDataScriptableObject(this NetworkReader reader) {
            return GameManager.Instance.Configuration.GetAbility(reader.ReadString());
        }
    }
}