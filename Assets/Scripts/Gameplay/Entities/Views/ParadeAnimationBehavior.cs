using System.Collections.Generic;
using System.Linq;
using Audio;
using Gameplay.Config;
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
        
        public void Initialize(GridEntity entity) {
            PlayerColorData colorData = GameManager.Instance.GetPlayerForTeam(entity).ColorData;
            ParticleSystem.MainModule main = _hexParticle.main;
            main.startColor = colorData.TeamColor;
            
            GridEntity target = GameManager.Instance.ResourceEntityFinder.GetResourceCollectorAtLocation(entity.Location!.Value);
            CurrencyConfiguration.Currency currency = GameManager.Instance.Configuration.CurrencyConfiguration.Currencies.First(c => c.Type == target.EntityData.AssociatedResource);
            _currencyIcon.sprite = currency.Icon;
            
            _incomeAmountPrevious.text = $"+{target.IncomeRate - 1}";
            _incomeAmountNext.text = $"+{target.IncomeRate.ToString()}";

            _paradeTextAnimator.Play("ParadeActive");
            _particles.ForEach(p => p.Play());
            GameAudio.Instance.ParadeStartSound();
        }
        
        public void EndParadeAnimation() {
            Destroy(gameObject);
        }

        public void PlayUpgradeSound() {
            GameAudio.Instance.ParadeUpgradeSound();
        }
    }
}