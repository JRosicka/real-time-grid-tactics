using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// A blinking "YOU" indicator that points to the local player's keep
    /// </summary>
    public class StartLocationIndicator : MonoBehaviour {
        [SerializeField] private Image _background;
        [SerializeField] private Animator _animator;

        [SerializeField] private float _topPadding;
        
        public void PlayBlinkingAnimation(Vector2 worldPosition, Color teamColor, float countdownTimeSeconds) {
            gameObject.SetActive(true);
            transform.position = Camera.main!.WorldToScreenPoint(worldPosition + new Vector2(0, _topPadding));
            _animator.Play("StartLocationIndicatorBlinking");
            
            _background.color = teamColor;
            
            HideAfterDelay(countdownTimeSeconds);
        }

        private async void HideAfterDelay(float seconds) {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            _animator.Play("StartLocationIndicatorIdle");
            gameObject.SetActive(false);
        }
    }
}