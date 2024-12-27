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
            ToggleOutOfResourcesIcon(false);
        }
        
        public void DoIncomeAnimation(IncomeAbilityData data) {
            _incomeText.text = string.Format(_incomeTextFormat, data.ResourceAmountIncome.Amount);
            _incomeText.color = _textColor;
            _animator.Play("ShowIncome");
            
            ToggleOutOfResourcesIcon(_entity.InteractBehavior is { IsLocalTeam: true });
        }

        /// <summary>
        /// See if we should show the out-of-resources icon, and show/hide it accordingly
        /// </summary>
        private void ToggleOutOfResourcesIcon(bool displayAlertIfOut) {
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

            bool outOfResources = resourceEntity.CurrentResourcesValue.Amount <= 0;
            _outOfResourcesIcon.SetActive(outOfResources);
            if (outOfResources && displayAlertIfOut) {
                GameManager.Instance.AlertTextDisplayer.DisplayAlert($"One of your {_entity.DisplayName.ToLower()}s has harvested all of its resources.");
            }
        }
    }
}