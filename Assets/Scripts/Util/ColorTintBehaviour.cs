using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Util {
    /// <summary>
    /// Lerps the colors of a set of images to a target color
    /// </summary>
    public class ColorTintBehaviour : MonoBehaviour {
        [Header("Config")]
        [Tooltip("The color we tint to")][SerializeField] private Color _targetColor = Color.gray;
        [Tooltip("Normalized amount that we lerp to the tint color")][Range(0, 1)][SerializeField] private float _lerpAmount01 = .5f;
        
        [Header("Images")]
        [SerializeField] private List<Image> _images = new();

        private bool _tintActive;
        private readonly List<Color> _startColors = new();
        
        public void ApplyTint(List<Color> overrideColors = null) {
            if (_tintActive) return;
            Reset();
            _tintActive = true;
            bool useOverrides = overrideColors != null && overrideColors.Count == _images.Count;
            for (int i = 0; i < _images.Count; i++) {
                _startColors.Add(useOverrides ? overrideColors[i] : _images[i].color);
            }
            SetColor(true);
        }

        private void SetColor(bool toTint) {
            for (int i = 0; i < _images.Count; i++) {
                if (_startColors.Count <= i) return;
                _images[i].color = Color.Lerp(_startColors[i], _targetColor, toTint ? _lerpAmount01 : 0);
            }
        }

        public void Reset() {
            _tintActive = false;
            SetColor(false);
            _startColors.Clear();
        }
        
        [Button]
        public void TestApplyTint() {
            ApplyTint();
        }
    }
}