using Gameplay.Config.Abilities;
using TMPro;
using UnityEngine;

namespace Gameplay.Entities {
    public class IncomeAnimationBehavior : MonoBehaviour {
        [SerializeField] private Animator _animator;
        [SerializeField] private TMP_Text _incomeText;
        
        [SerializeField] private Color _textColor;
        [SerializeField] private string _incomeTextFormat = "+{0}";
        
        public void DoIncomeAnimation(IncomeAbilityData data) {
            _incomeText.text = string.Format(_incomeTextFormat, data.ResourceAmountIncome.Amount);
            _incomeText.color = _textColor;
            _animator.Play("ShowIncome");
        }
    }
}