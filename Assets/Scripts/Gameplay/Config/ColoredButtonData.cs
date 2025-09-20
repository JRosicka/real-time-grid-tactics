using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configurable sprite data for a colored button
    /// </summary>
    [CreateAssetMenu(menuName = "ColoredButtonData", fileName = "ColoredButtonData", order = 0)]
    public class ColoredButtonData : ScriptableObject {
        public Sprite Normal;
        public Sprite Hovered;
        public Sprite Pressed;
    }
}