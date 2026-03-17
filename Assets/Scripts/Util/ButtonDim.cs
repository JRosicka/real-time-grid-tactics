using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Util {
    /// <summary>
    /// When applied to an object, this component will dim all its image and text child objects when pressed. The objects to
    /// dim are determined each time the button is pressed in case its children should change. 
    /// </summary>
    public class ButtonDim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
        [Range(0F, 1F)] public float DimFactor = 0.75f;

        private List<Dimmable> _dimmedChildren;

        private void Awake() {
            _dimmedChildren = new List<Dimmable>();
        }
        
        public bool Interactable { get; set; } = true;

        public void UnDim() {
            if (!this) return;
            foreach (Dimmable child in _dimmedChildren) {
                child.Restore();
            }

            _dimmedChildren.Clear();
        }

        public void TryUpdateDimmable(Image image) {
            DimmableImage old = _dimmedChildren.OfType<DimmableImage>().FirstOrDefault(e => e.TargetImage == image);
            TryUpdateDimmable(old, () => new DimmableImage(image, DimFactor));
        }

        public void TryUpdateDimmable(TextMeshProUGUI text) {
            DimmableText old = _dimmedChildren.OfType<DimmableText>().FirstOrDefault(e => e.Text == text);
            TryUpdateDimmable(old, () => new DimmableText(text, DimFactor));
        }

        private void TryUpdateDimmable(Dimmable old, Func<Dimmable> getNew) {
            if (old == null) return;
            _dimmedChildren.Remove(old);
            Dimmable dimmable = getNew();
            dimmable.Dim();
            _dimmedChildren.Add(dimmable);
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (!Interactable) return;
            
            // Get child components to darken
            Image[] imagesToDim = GetComponentsInChildren<Image>();
            RawImage[] rawImagesToDim = GetComponentsInChildren<RawImage>();
            TextMeshProUGUI[] textToDim = GetComponentsInChildren<TextMeshProUGUI>();

            // Darken image components
            foreach (Image image in imagesToDim) {
                Dimmable dimImage = new DimmableImage(image, DimFactor);
                dimImage.Dim();
                _dimmedChildren.Add(dimImage);
            }

            // Darken raw image components
            foreach (RawImage rawImage in rawImagesToDim) {
                Dimmable dimRawImage = new DimmableRawImage(rawImage, DimFactor);
                dimRawImage.Dim();
                _dimmedChildren.Add(dimRawImage);
            }

            // Darken text components
            foreach (TextMeshProUGUI text in textToDim) {
                Dimmable dimText = new DimmableText(text, DimFactor);
                dimText.Dim();
                _dimmedChildren.Add(dimText);
            }
        }

        public void OnPointerUp(PointerEventData eventData) {
            UnDim();
        }

        /// <summary>
        /// Interface for visible child objects that can be dimmed and restored to their original appearance.
        /// </summary>
        private abstract class Dimmable {
            protected readonly Color OriginalColor;
            protected readonly Color DimmedColor;

            protected Dimmable(Color originalColor, float dimFactor) {
                OriginalColor = originalColor;
                DimmedColor = new Color(originalColor.r * dimFactor, originalColor.g * dimFactor,
                    originalColor.b * dimFactor, originalColor.a);
            }

            public abstract void Dim();

            public abstract void Restore();
        }

        /// <summary>
        /// An Image that can be dimmed and restored to its original color.
        /// </summary>
        private class DimmableImage : Dimmable {
            public readonly Image TargetImage;

            public DimmableImage(Image targetImage, float dimFactor) : base(targetImage.color, dimFactor) {
                TargetImage = targetImage;
            }

            public override void Dim() {
                TargetImage.color = DimmedColor;
            }

            public override void Restore() {
                if (!TargetImage) return;
                TargetImage.color = OriginalColor;
            }
        }

        /// <summary>
        /// A RawImage that can be dimmed and restored to its original color.
        /// </summary>
        private class DimmableRawImage : Dimmable {
            public readonly RawImage RawImage;

            public DimmableRawImage(RawImage rawImage, float dimFactor) : base(rawImage.color, dimFactor) {
                RawImage = rawImage;
            }

            public override void Dim() {
                RawImage.color = DimmedColor;
            }

            public override void Restore() {
                if (!RawImage) return;
                RawImage.color = OriginalColor;
            }
        }

        /// <summary>
        /// A TextMeshProUGUI that can be dimmed and restored to its original color. 
        /// </summary>
        private class DimmableText : Dimmable {
            public readonly TextMeshProUGUI Text;
            private bool _originalTintAllSprites;

            public DimmableText(TextMeshProUGUI text, float dimFactor) : base(text.color, dimFactor) {
                Text = text;
            }

            public override void Dim() {
                Text.tintAllSprites = true;
                Text.color = DimmedColor;
            }

            public override void Restore() {
                if (!Text) return;
                Text.tintAllSprites = _originalTintAllSprites;
                Text.color = OriginalColor;
            }
        }
    }
}