using System;
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

        public void ToggleEndDot(bool toggle) {
            EndDot.gameObject.SetActive(toggle);
        }
    }
}
