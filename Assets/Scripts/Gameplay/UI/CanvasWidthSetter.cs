using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Allows for limiting the size of an otherwise scalable rect transform
/// </summary>
public class CanvasWidthSetter : MonoBehaviour {
    private const float MaxCanvasAspectRatio = 1.77778f; // 16/9

    [SerializeField] private RectTransform _rectTransform;

    private float CanvasWidth {
        get {
            float cameraHeight = _rectTransform.rect.height;
            // Make the camera as wide as needed, but don't go over the standard 16/9
            return cameraHeight * Mathf.Min(Camera.main.aspect, MaxCanvasAspectRatio);
        }
    }

    public void Initialize() {
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, CanvasWidth);
        LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
    }
}
