using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles choosing the display for the attacked camera on start
/// </summary>
public class CameraDisplayPicker : MonoBehaviour {
    [SerializeField] private Camera _camera;

    private void Awake() {
        int preferredDisplayIndex = PlayerPrefs.GetInt(PlayerPrefsKeys.ChosenDisplayKey, 0);

        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);

        if (displays.Count == 0) {
            Debug.LogWarning("No displays found from Screen.GetDisplayLayout.");
            return;
        }

        preferredDisplayIndex = Mathf.Clamp(preferredDisplayIndex, 0, displays.Count - 1);

        DisplayInfo currentDisplay = Screen.mainWindowDisplayInfo;
        DisplayInfo preferredDisplay = displays[preferredDisplayIndex];

        // Only move if the current main window is on a different display.
        if (currentDisplay.name != preferredDisplay.name) {
            Screen.MoveMainWindowTo(preferredDisplay, Vector2Int.zero);
            Debug.Log($"Moved main window from display: {currentDisplay.name} to display: {preferredDisplay.name}");
        } else {
            Debug.Log($"Main window already on preferred display: {preferredDisplay.name}");
        }
    }
}