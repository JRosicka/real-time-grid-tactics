using Gameplay.Config;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// A hoverable view that displays an <see cref="EntityTag"/> for an entity and provides additional info on that
    /// tag when hovered
    /// </summary>
    public class HoverableTag : HoverableInfoIcon {
        [SerializeField] private TMP_Text _tagText;

        public void Initialize(EntityTagData entityTagData) {
            _tagText.text = entityTagData.Tag.ToString();
            ShowIcon(entityTagData.Description);
        }
    }
}