using System;

namespace Gameplay.UI {
    /// <summary>
    /// Provides runtime data for a <see cref="VisualBar"/> view. 
    /// </summary>
    public interface IBarLogic {
        /// <summary>
        /// The current value for the given instance
        /// </summary>
        float CurrentValue { get; }
        /// <summary>
        /// The max value for the given instance
        /// </summary>
        float MaxValue { get; }
        /// <summary>
        /// The static min value for this type
        /// </summary>
        float MinConfigurableValue { get; }
        /// <summary>
        /// The static max value for this type
        /// </summary>
        float MaxConfigurableValue { get; }
        event Action BarUpdateEvent;
        event Action BarDestroyEvent;
        void UnsubscribeFromEvents();
    }
}