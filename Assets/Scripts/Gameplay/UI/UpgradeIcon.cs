using System;
using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Icon that gets displayed for an upgrade owned by a player
    /// </summary>
    public class UpgradeIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _background;
        [SerializeField] private Image _timerBackground;
        [SerializeField] private HoverableUpgradeTooltip _tooltip;
        [SerializeField] private NetworkableTimerView _timerView;
        
        private IUpgrade _upgrade;
        private PlayerOwnedPurchasablesController _ownedPurchasables;
        
        public bool UpgradeActive { get; private set; }
        public event Action UpgradeStatusChanged;
        
        public void Initialize(IUpgrade upgrade, IGamePlayer player) {
            _upgrade = upgrade;
            _icon.sprite = upgrade.UpgradeData.BaseSpriteIconOverride == null ? upgrade.UpgradeData.BaseSprite : upgrade.UpgradeData.BaseSpriteIconOverride;
            _background.sprite = player.Data.ColoredButtonData.Normal;
            _timerBackground.sprite = player.Data.ColoredButtonData.Normal;
            
            _tooltip.Initialize(upgrade);
            gameObject.SetActive(false);

            if (upgrade.UpgradeData.Timed) {
                SetUpTimer(upgrade);
            }

            PlayerOwnedPurchasablesController ownedPurchasables = player.OwnedPurchasablesController;
            ownedPurchasables.OwnedPurchasablesChangedEvent += UpdateAvailability;
        }
        
        private void UpdateAvailability() {
            gameObject.SetActive(_upgrade.Status == UpgradeStatus.Owned);
            if (_upgrade.Status != UpgradeStatus.Owned) {
                _tooltip.HideTooltip();
            }

            bool wasActive = UpgradeActive;
            UpgradeActive = _upgrade.Status == UpgradeStatus.Owned;
            if (wasActive != UpgradeActive) {
                UpgradeStatusChanged?.Invoke();
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData) {
            _tooltip.ShowTooltip();
        }
        
        public void OnPointerExit(PointerEventData eventData) {
            _tooltip.HideTooltip();
        }
        
        private void SetUpTimer(IUpgrade upgrade) {
            upgrade.UpgradeTimerStarted += TimerStarted;
        }

        private void TimerStarted(UpgradeDurationTimer timer) {
            _timerView.Initialize(timer, false, false, true);
        }
        
        private void OnDestroy() {
            if (!_ownedPurchasables) return;
            _ownedPurchasables.OwnedPurchasablesChangedEvent -= UpdateAvailability;
        }
    }
}