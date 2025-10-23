using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class KingView : GridEntityParticularView {
        [SerializeField] private Animator _paradeTextAnimator;
        [SerializeField] private int _minSecondsBetweenUnderAttackAlerts = 30;

        private GridEntity _entity;
        private float _timeOfLastDamageReceived;

        public override void Initialize(GridEntity entity) {
            _entity = entity;
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

        private void DoParadeAnimation() {
            _paradeTextAnimator.Play("ParadeActive");
        }
    }
}