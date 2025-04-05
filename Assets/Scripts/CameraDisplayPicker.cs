using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles choosing the display for the attacked camera on start
/// </summary>
public class CameraDisplayPicker : MonoBehaviour {
    [SerializeField] private Camera _camera;

    private void Awake() {
        int displayIndex = PlayerPrefs.GetInt(PlayerPrefsKeys.ChosenDisplayKey, 0);
        
        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);
        Screen.MoveMainWindowTo(displays[displayIndex], new Vector2Int(0, 0));
    }
}