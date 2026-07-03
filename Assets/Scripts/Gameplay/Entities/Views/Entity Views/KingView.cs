using System.Collections.Generic;
using Audio;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using TMPro;
using UnityEngine;
using Util;

namespace Gameplay.Entities {
    public class KingView : GridEntityParticularView {
        [SerializeField] private Animator _paradeTextAnimator;
        [SerializeField] private List<ParticleSystem> _particles;
        [SerializeField] private ParticleSystem _hexParticle;
        [SerializeField] private AnimationEventListener _eventListener;
        [SerializeField] private TextMeshProUGUI _incomeAmountPrevious;
        [SerializeField] private TextMeshProUGUI _incomeAmountNext;
        [SerializeField] private int _minSecondsBetweenUnderAttackAlerts = 30;

        private GridEntity _entity;
        private float _timeOfLastDamageReceived;

        public override void Initialize(GridEntity entity) {
            _entity = entity;
            _eventListener.EventTriggered += PlayUpgradeSound;
            
            PlayerColorData colorData = GameManager.Instance.GetPlayerForTeam(entity).ColorData;
            ParticleSystem.MainModule main = _hexParticle.main;
            main.startColor = colorData.TeamColor;
        }
        
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() {
            if (_entity.Team != GameManager.Instance.LocalTeam) return;
            if (Time.time - _timeOfLastDamageReceived < _minSecondsBetweenUnderAttackAlerts) return;
            
            _timeOfLastDamageReceived = Time.time;
            GameManager.Instance.AlertTextDisplayer.DisplayAlert("Your King is under attack!");
        }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability.AbilityData) {
                case ParadeAbilityData _:
                    DoParadeAnimation();
                    return false;
                default:
                    return true;
            }
        }
        
        public void ToggleParadeAnimation(bool active) {
            _paradeTextAnimator.gameObject.SetActive(active);
        }

        private void DoParadeAnimation() {
            if (_entity.Location == null) return;
            
            GridEntity target = GameManager.Instance.ResourceEntityFinder.GetResourceCollectorAtLocation(_entity.Location.Value);
            _incomeAmountPrevious.text = $"+{target.IncomeRate - 1}";
            _incomeAmountNext.text = $"+{target.IncomeRate.ToString()}";

            _paradeTextAnimator.Play("ParadeActive");
            _particles.ForEach(p => p.Play());
            GameAudio.Instance.ParadeStartSound();
        }

        private static void PlayUpgradeSound() {
            GameAudio.Instance.ParadeUpgradeSound();
        }
    }
}