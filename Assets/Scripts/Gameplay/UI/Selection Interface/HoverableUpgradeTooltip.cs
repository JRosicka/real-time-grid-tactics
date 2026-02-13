using System;
using System.Globalization;
using Gameplay.Entities.Upgrades;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// A tooltip that displays info for an <see cref="UpgradeIcon"/>
    /// </summary>
    public class HoverableUpgradeTooltip : HoverableTooltip {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private GameObject _timerTextContainer;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private string _timerTextFormat = "{0}s";
        
        private UpgradeDurationTimer _timer;
        
        public void Initialize(IUpgrade upgrade) {
            _titleText.text = upgrade.UpgradeData.ID;
            if (upgrade.UpgradeData.Timed) {
                SetUpTimer(upgrade);
                _timerTextContainer.gameObject.SetActive(true);
            } else {
                _timerTextContainer.gameObject.SetActive(false);
            }
            base.Initialize(upgrade.UpgradeData.Description);
        }
        
        private void SetUpTimer(IUpgrade upgrade) {
            upgrade.UpgradeTimerStarted += TimerStarted;
        }

        private void TimerStarted(UpgradeDurationTimer timer) {
            _timer = timer;
        }
        
        private void Update() {
            if (!Showing) return;
            if (_timer == null) return;
            
            _timerText.text = string.Format(_timerTextFormat, Math.Ceiling(_timer.TimeRemaining).ToString(CultureInfo.InvariantCulture));
        }
    }
}