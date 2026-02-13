using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// UI logic for updating an <see cref="NetworkableTimerView"/> fill
    /// </summary>
    public abstract class AbilityTimerFill : MonoBehaviour {
        public abstract void UpdateFillAmount01(float amount);
    }
}