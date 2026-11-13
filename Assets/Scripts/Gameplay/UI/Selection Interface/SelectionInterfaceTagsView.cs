using System.Collections.Generic;
using Gameplay.Config;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Sets up <see cref="HoverableTag"/>s for a given unit
    /// </summary>
    public class SelectionInterfaceTagsView : MonoBehaviour {
        [SerializeField] private HoverableTag _hoverableTagPrefab;
        
        private readonly List<HoverableTag> _activeTags = new List<HoverableTag>();

        private void Awake() {
            _hoverableTagPrefab.gameObject.SetActive(false);
        }

        public async void SetUpTags(List<EntityTag> tags) {
            ClearTags();
            
            EntityTagDatabase entityTags = GameManager.Instance.Configuration.EntityTags;
            foreach (EntityTag entityTag in tags) {
                EntityTagData data = entityTags.GetEntityTag(entityTag);
                if (data is not { Visible: true }) continue;
                
                HoverableTag newTagView = Instantiate(_hoverableTagPrefab, transform);
                newTagView.Initialize(data);
                _activeTags.Add(newTagView);
            }
        }

        private void ClearTags() {
            _activeTags.ForEach(t => Destroy(t.gameObject));
            _activeTags.Clear();
        }
    }
}