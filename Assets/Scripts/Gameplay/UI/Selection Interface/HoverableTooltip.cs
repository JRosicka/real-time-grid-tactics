using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Task = System.Threading.Tasks.Task;

namespace Gameplay.UI {
    /// <summary>
    /// A tooltip that appears at a given location to display text info. Not to be confused with <see cref="TooltipView"/>
    /// </summary>
    public class HoverableTooltip : MonoBehaviour {
        private const float ExtraAdjustmentSpace = 8f;
        
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private VerticalLayoutGroup _layoutGroup;
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        [SerializeField] private ContentSizeFitter _tooltipContentSizeFitter;
        [SerializeField] private Transform _arrow;

        protected bool Showing;
        
        public async void Initialize(string text, bool adjustTooltipWindow) {
            _text.text = text;
            
            _contentSizeFitter.SetLayoutVertical();
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
            if (!this || !_layoutGroup) return;
            _layoutGroup.CalculateLayoutInputVertical();
            _tooltipContentSizeFitter.SetLayoutVertical();
            
            if (adjustTooltipWindow) {
                AdjustTooltipWindow();
            }
        }

        public void ShowTooltip() {
            _canvasGroup.alpha = 1;
            Showing = true;
        }
        
        public void HideTooltip() {
            _canvasGroup.alpha = 0;
            Showing = false;
        }

        /// <summary>
        /// Move the window if it would otherwise appear off-screen. Only considers width. 
        /// </summary>
        private void AdjustTooltipWindow() {
            float requiredAdjustment = GetRequiredWidthAdjustment();
            if (requiredAdjustment == 0) return;
            
            float additionalAdjustment = requiredAdjustment < 0 ? -ExtraAdjustmentSpace : ExtraAdjustmentSpace;
            requiredAdjustment += additionalAdjustment;
            
            transform.position += Vector3.right * requiredAdjustment;
            _arrow.position -= Vector3.right * requiredAdjustment;
        }
        
        private float GetRequiredWidthAdjustment() {
            RectTransform rectTransform = (RectTransform)transform;
            
            // Get the 4 corners of the RectTransform in world/screen space
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // corners[0] = bottom-left, corners[2] = top-right
            float minX = corners[0].x;
            float maxX = corners[2].x;

            // Clamp the coordinates to the screen boundaries
            float visibleMinX = Mathf.Clamp(minX, 0f, Screen.width);
            float visibleMaxX = Mathf.Clamp(maxX, 0f, Screen.width);

            // Determine if the left or right bound need to be adjusted
            if (visibleMinX > minX) {
                return visibleMinX - minX;
            }

            if (visibleMaxX < maxX) {
                return visibleMaxX - maxX;
            }

            return 0;
        }
    }
}