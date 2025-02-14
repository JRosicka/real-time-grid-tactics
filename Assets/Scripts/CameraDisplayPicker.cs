using UnityEngine;

/// <summary>
/// Handles choosing the display for the attacked camera on start
/// </summary>
public class CameraDisplayPicker : MonoBehaviour {
    [SerializeField] private Camera _camera;

    private void Awake() {
        _camera.targetDisplay = PlayerPrefs.GetInt(PlayerPrefsKeys.ChosenDisplayKey, 0);
    }
}