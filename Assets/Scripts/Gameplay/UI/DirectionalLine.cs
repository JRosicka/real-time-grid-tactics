using System;
using System.Collections.Generic;
using Gameplay.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Controls and animates a directional line of arrows that can be used to indicate movement
    /// </summary>
    public class DirectionalLine : MonoBehaviour {

        public enum LineType {
            Full,
            StartHalf,
            EndHalf
        }
        
        public RectTransform MaskTransform;
        public Image EndDot;
        public Image AttackDestinationIcon;
        public Image MoveDestinationIcon;

        public List<Image> ImageColorsToChange = new();
        public Color DefaultColor;
        public Color AttackColor;
        public Color MoveColor;

        private bool _destination;

        public void SetMask(LineType lineType) {
            switch (lineType) {
                case LineType.Full:
                    MaskTransform.sizeDelta = new Vector2(GridController.CellWidth, MaskTransform.rect.height);
                    MaskTransform.localPosition = new Vector2(GridController.CellWidth, 0);
                    break;
                case LineType.StartHalf:
                    MaskTransform.sizeDelta = new Vector2(GridController.CellWidth / 2f, MaskTransform.rect.height);
                    MaskTransform.localPosition = new Vector2(GridController.CellWidth, 0);
                    break;
                case LineType.EndHalf:
                    MaskTransform.sizeDelta = new Vector2(GridController.CellWidth / 2f, MaskTransform.rect.height);
                    MaskTransform.localPosition = new Vector2(GridController.CellWidth / 2, 0);
                    ToggleEndDot(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lineType), lineType, null);
            }
        }

        public void SetRotation(float angle) {
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Rotate the icons back - we don't want their global rotation to change
            EndDot.transform.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        public void SetColor(bool attack) {
            UpdateColor(attack ? AttackColor : MoveColor);
        }

        public void ToggleEndDot(bool toggle) {
            bool actuallyToggle = toggle && !_destination;
            Color color = EndDot.color;
            color.a = actuallyToggle ? 1 : 0;
            EndDot.color = color;
        }

        public void ShowDestinationIcon(bool attack) {
            _destination = true;
            ToggleEndDot(false);
            
            AttackDestinationIcon.gameObject.SetActive(attack);
            MoveDestinationIcon.gameObject.SetActive(!attack);
        }

        public void Discard() {
            _destination = false;
            ToggleEndDot(true);
            AttackDestinationIcon.gameObject.SetActive(false);
            MoveDestinationIcon.gameObject.SetActive(false);
            SetRotation(0);
            UpdateColor(DefaultColor);
        }

        private void UpdateColor(Color color) {
            foreach (Image image in ImageColorsToChange) {
                image.color = color;
            }
        }
    }
}
