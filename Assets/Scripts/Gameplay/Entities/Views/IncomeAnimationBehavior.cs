using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// Animation and view logic for <see cref="GridEntityView"/>s that deal with earning resources
    /// </summary>
    public class IncomeAnimationBehavior : MonoBehaviour {
        [SerializeField] private Animator _animator;
        [SerializeField] private TMP_Text _incomeText;
        
        [SerializeField] private Color _textColor;
        [SerializeField] private string _incomeTextFormat = "+{0} <sprite name=\"{1}\">";

        [SerializeField] private GameObject _outOfResourcesIcon;
        [SerializeField] private Image _outOfResourcesImage;

        private GridEntity _entity;
        private string _textIconGlyph;

        public void Initialize(GridEntity entity, ResourceType resourceType) {
            _entity = entity;
            CurrencyConfiguration.Currency currency = GameManager.Instance.Configuration.CurrencyConfiguration.Currencies.First(c => c.Type == resourceType);
            _outOfResourcesImage.sprite = currency.Icon;
            _textIconGlyph = currency.TextIconGlyph;
            
            ToggleOutOfResourcesIcon(false);
        }
        
        public void DoIncomeAnimation() {
            _incomeText.text = string.Format(_incomeTextFormat, _entity.IncomeRate, _textIconGlyph);
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

            GridEntity resourceEntity = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(_entity, _entity.EntityData);
            if (resourceEntity == null) {
                if (GameManager.Instance.GameSetupManager.GameInitialized) {
                    Debug.LogWarning("No resource entity found when doing income animation. This should not happen.");
                }
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