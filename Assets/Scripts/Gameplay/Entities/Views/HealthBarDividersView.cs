using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// Handles setting up vertical divider visuals on an entity health bar
    /// </summary>
    public class HealthBarDividersView : MonoBehaviour {
        [SerializeField] private Image _dividerTemplate;
        [SerializeField] private RectTransform _dividerBounds;
        [SerializeField] private int _hpInterval = 5;
        [SerializeField] private float _outerBound = .05f;

        public void Initialize(float maxHP) {
            // Skip the first divider
            for (int i = _hpInterval; i < maxHP; i += _hpInterval) {
                Image divider = Instantiate(_dividerTemplate, _dividerBounds.transform);
                Color dividerColor = divider.color;
                dividerColor.a = 1;
                divider.color = dividerColor;

                if (i / maxHP < _outerBound) {
                    // This divider is close to the outer edge, so reduce its height so that it doesn't overlap the icon
                    divider.rectTransform.sizeDelta = new Vector2(divider.rectTransform.sizeDelta.x, divider.rectTransform.sizeDelta.y / 1.5f);
                }
            }
        }
    }
}