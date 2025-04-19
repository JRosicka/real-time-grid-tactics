using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Util {
    /// <summary>
    /// Applies a triggerable color flash effect to specified image components using an animation curve
    /// </summary>
    public class ColorFlashBehaviour : MonoBehaviour {
        [Header("Config")]
        [Tooltip("The length of the flash-in portion")][SerializeField] private float _flashInLengthSeconds = .1f;
        [Tooltip("The length of the flash-out portion")][SerializeField] private float _flashOutLengthSeconds = .5f;
        [Tooltip("The color we flash-in to")][SerializeField] private Color _targetColor = Color.red;
        
        [Header("Animation Curves")]
        [SerializeField] private AnimationCurve _flashInCurve;
        [SerializeField] private AnimationCurve _flashOutCurve;
        
        [Header("Images")]
        [SerializeField] private List<Image> _images = new();

        private float _currentFlashTime;
        private bool _flashActive;
        private readonly List<Color> _startColors = new();

        [Button]
        public void Flash() {
            if (_flashActive) return;
            Reset();
            _flashActive = true;
            foreach (Image image in _images) {
                _startColors.Add(image.color);
            }
        }
        
        private void Update () {
            if (!_flashActive) return;
            
            _currentFlashTime += Time.deltaTime;

            if (_currentFlashTime > _flashInLengthSeconds + _flashOutLengthSeconds) {
                // Done
                Reset();
            } else if (_currentFlashTime < _flashInLengthSeconds) {
                // Flashing in
                float flashInProportion = _currentFlashTime / _flashInLengthSeconds;
                float colorChangeAmount = _flashInCurve.Evaluate(flashInProportion);
                SetColor(colorChangeAmount);
            } else {
                // Flashing out
                float flashOutTime = _currentFlashTime - _flashInLengthSeconds;
                float flashOutProportion = flashOutTime / _flashOutLengthSeconds;
                float colorChangeAmount = _flashOutCurve.Evaluate(flashOutProportion);
                SetColor(colorChangeAmount);
            }
        }

        private void SetColor(float colorChangeAmount) {
            for (int i = 0; i < _images.Count; i++) {
                if (_startColors.Count <= i) return;
                _images[i].color = Color.Lerp(_startColors[i], _targetColor, colorChangeAmount);
            }
        }

        private void Reset() {
            _currentFlashTime = 0;
            _flashActive = false;
            SetColor(0);
            _startColors.Clear();
        }
    }
}