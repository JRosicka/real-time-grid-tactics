using System.Collections.Generic;
using System.Linq;
using Audio;
using Gameplay.Config;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// Plays an animation for the parade ability
    /// </summary>
    public class ParadeAnimationBehavior : MonoBehaviour {
        [SerializeField] private Animator _paradeTextAnimator;
        [SerializeField] private List<ParticleSystem> _particles;
        [SerializeField] private ParticleSystem _hexParticle;
        [SerializeField] private TextMeshProUGUI _incomeAmountPrevious;
        [SerializeField] private TextMeshProUGUI _incomeAmountNext;
        [SerializeField] private Image _currencyIcon;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Vector2Int _location;
        private TeamFogOfWarTracker _fowTracker;
        
        public void Initialize(GridEntity entity) {
            PlayerColorData colorData = GameManager.Instance.GetPlayerForTeam(entity).ColorData;
            ParticleSystem.MainModule main = _hexParticle.main;
            main.startColor = colorData.TeamColor;
            _location = entity.Location!.Value;
            
            GridEntity target = GameManager.Instance.ResourceEntityFinder.GetResourceCollectorAtLocation(_location);
            CurrencyConfiguration.Currency currency = GameManager.Instance.Configuration.CurrencyConfiguration.Currencies.First(c => c.Type == target.EntityData.AssociatedResource);
            _currencyIcon.sprite = currency.Icon;
            
            _incomeAmountPrevious.text = $"+{target.IncomeRate - 1}";
            _incomeAmountNext.text = $"+{target.IncomeRate.ToString()}";

            // Fog of war
            _fowTracker = GameManager.Instance.FogOfWarManager!.GetLocalTeamTracker();
            if (_fowTracker != null) {
                ToggleFoWHiddenState(_fowTracker.IsLocationHidden(_location));
                _fowTracker.FoWUpdated += FogOfWarUpdated;
            }

            _paradeTextAnimator.Play("ParadeActive");
            _particles.ForEach(p => p.Play());
            GameAudio.Instance.ParadeStartSound();
        }
        
        public void EndParadeAnimation() {
            if (_fowTracker != null) {
                _fowTracker.FoWUpdated -= FogOfWarUpdated;
            }
            Destroy(gameObject);
        }

        public void PlayUpgradeSound() {
            GameAudio.Instance.ParadeUpgradeSound();
        }

        private void ToggleFoWHiddenState(bool hidden) {
            _canvasGroup.alpha = hidden ? 0 : 1;
        }

        private void FogOfWarUpdated(List<TeamFogOfWarTracker.FoWCell> foWCells) {
            TeamFogOfWarTracker.FoWCell cell = foWCells.FirstOrDefault(c => c.Position == _location);
            if (cell != null) {
                ToggleFoWHiddenState(cell.Hidden);
            }
        }
    }
}