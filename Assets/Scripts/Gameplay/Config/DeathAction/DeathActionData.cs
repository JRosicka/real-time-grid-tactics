using Gameplay.Entities;
using Gameplay.Entities.DeathAction;
using UnityEngine;

namespace Gameplay.Config.DeathAction {
    /// <summary>
    /// On-death action configuration for some <see cref="IDeathAction"/> that gets executed on the server when a <see cref="GridEntity"/> dies.
    /// No-op on clients. 
    /// </summary>
    public abstract class DeathActionData : ScriptableObject {
        public abstract IDeathAction CreateDeathAction();
    }
}