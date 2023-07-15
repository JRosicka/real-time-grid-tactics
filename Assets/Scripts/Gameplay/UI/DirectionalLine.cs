using System;
using Sirenix.Utilities;
using UnityEngine;

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

        public void SetMask(LineType lineType) {
            switch (lineType) {
                case LineType.Full:
                    MaskTransform.anchorMin = new Vector2(.5f, .5f);
                    MaskTransform.anchorMax = new Vector2(.5f, .5f);
                    MaskTransform.sizeDelta = new Vector2(.88f, MaskTransform.rect.height);
                    break;
                case LineType.StartHalf:
                    MaskTransform.anchorMin = new Vector2(1f, .5f);
                    MaskTransform.anchorMax = new Vector2(1f, .5f);
                    MaskTransform.sizeDelta = new Vector2(.44f, MaskTransform.rect.height);
                    break;
                case LineType.EndHalf:
                    MaskTransform.anchorMin = new Vector2(.5f, .5f);
                    MaskTransform.anchorMax = new Vector2(.5f, .5f);
                    MaskTransform.sizeDelta = new Vector2(.44f, MaskTransform.rect.height);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lineType), lineType, null);
            }
        }
    }
}
