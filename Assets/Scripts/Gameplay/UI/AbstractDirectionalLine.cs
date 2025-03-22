using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Generic line for visualizing part of a path via <see cref="PathVisualizer"/>
    /// </summary>
    public abstract class AbstractDirectionalLine : MonoBehaviour {
        public enum LineType {
            Full,
            StartHalf,
            EndHalf
        }
        
        [SerializeField] protected Color DefaultColor;
        [SerializeField] private Color _attackColor;
        [SerializeField] private Color _moveColor;
        
        [SerializeField] private RectTransform _maskTransform;
        [SerializeField] private List<Image> _imageColorsToChange = new();

        protected abstract float CellWidth { get; }
        
        public void SetMask(LineType lineType) {
            switch (lineType) {
                case LineType.Full:
                    _maskTransform.sizeDelta = new Vector2(CellWidth, _maskTransform.rect.height);
                    _maskTransform.localPosition = new Vector2(CellWidth, 0);
                    break;
                case LineType.StartHalf:
                    _maskTransform.sizeDelta = new Vector2(CellWidth / 2f, _maskTransform.rect.height);
                    _maskTransform.localPosition = new Vector2(CellWidth, 0);
                    break;
                case LineType.EndHalf:
                    _maskTransform.sizeDelta = new Vector2(CellWidth / 2f, _maskTransform.rect.height);
                    _maskTransform.localPosition = new Vector2(CellWidth / 2, 0);
                    SetUpEndHalfImpl();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lineType), lineType, null);
            }
        }

        public void SetRotation(float angle) {
            transform.rotation = Quaternion.Euler(0, 0, angle);
            SetRotationImpl(angle);
        }

        public abstract void ShowDestinationIcon(bool attack);

        public void SetColor(bool attack) {
            UpdateColor(attack ? _attackColor : _moveColor);
        }

        public abstract void Discard();

        public abstract void ToggleEndDot(bool toggle);
        
        protected abstract void SetUpEndHalfImpl();
        protected abstract void SetRotationImpl(float angle);
        
        protected void UpdateColor(Color color) {
            foreach (Image image in _imageColorsToChange) {
                color.a = image.color.a;
                image.color = color;
            }
        }
    }
}