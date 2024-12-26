using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using TMPro;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Animation and view logic for <see cref="GridEntityView"/>s that deal with earning resources
    /// </summary>
    public class IncomeAnimationBehavior : MonoBehaviour {
        [SerializeField] private Animator _animator;
        [SerializeField] private TMP_Text _incomeText;
        
        [SerializeField] private Color _textColor;
        [SerializeField] private string _incomeTextFormat = "+{0}";

        [SerializeField] private GameObject _outOfResourcesIcon;

        private GridEntity _entity;

        public void Initialize(GridEntity entity) {
            _entity = entity;
            ToggleOutOfResourcesIcon();
        }
        
        public void DoIncomeAnimation(IncomeAbilityData data) {
            _incomeText.text = string.Format(_incomeTextFormat, data.ResourceAmountIncome.Amount);
            _incomeText.color = _textColor;
            _animator.Play("ShowIncome");
            
            ToggleOutOfResourcesIcon();
        }

        /// <summary>
        /// See if we should show the out-of-resources icon, and show/hide it accordingly
        /// </summary>
        private void ToggleOutOfResourcesIcon() {
            if (_entity.Location == null) {
                Debug.LogWarning("No performer location? This should not happen.");
                return;
            }
            GridEntity resourceEntity = GameManager.Instance.GetEntitiesAtLocation(_entity.Location.Value)
                .Entities
                .Select(e => e.Entity)
                .FirstOrDefault(e => e.Tags.Contains(EntityData.EntityTag.Resource));
            if (resourceEntity == null) {
                Debug.LogWarning("No resource entity found when doing income animation. This should not happen.");
                return;
            }

            _outOfResourcesIcon.SetActive(resourceEntity.CurrentResources.Value.Amount <= 0);
        }
    }
}